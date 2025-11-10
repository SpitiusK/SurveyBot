using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SurveyBot.Core.DTOs.Survey;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Simple in-memory cache for surveys and frequently accessed data.
/// Reduces database queries and improves bot response time.
/// </summary>
public class SurveyCache
{
    private const int DEFAULT_TTL_MINUTES = 5;
    private const int CLEANUP_INTERVAL_MINUTES = 2;

    private readonly ILogger<SurveyCache> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry<SurveyDto>> _surveyCache;
    private readonly ConcurrentDictionary<string, CacheEntry<List<SurveyDto>>> _surveyListCache;
    private readonly Timer _cleanupTimer;
    private readonly SemaphoreSlim _cleanupLock;

    public SurveyCache(ILogger<SurveyCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _surveyCache = new ConcurrentDictionary<string, CacheEntry<SurveyDto>>();
        _surveyListCache = new ConcurrentDictionary<string, CacheEntry<List<SurveyDto>>>();
        _cleanupLock = new SemaphoreSlim(1, 1);

        // Start cleanup timer
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredEntriesAsync(),
            null,
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES),
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES));

        _logger.LogInformation("SurveyCache initialized with TTL={TTL} minutes", DEFAULT_TTL_MINUTES);
    }

    /// <summary>
    /// Gets or adds a survey to the cache.
    /// </summary>
    public async Task<SurveyDto?> GetOrAddSurveyAsync(
        int surveyId,
        Func<Task<SurveyDto?>> factory,
        TimeSpan? ttl = null)
    {
        var key = GetSurveyKey(surveyId);
        var actualTtl = ttl ?? TimeSpan.FromMinutes(DEFAULT_TTL_MINUTES);

        // Try to get from cache
        if (_surveyCache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            _logger.LogDebug("Cache HIT for survey {SurveyId}", surveyId);
            entry.LastAccessTime = DateTime.UtcNow;
            entry.AccessCount++;
            return entry.Value;
        }

        // Cache miss - fetch from source
        _logger.LogDebug("Cache MISS for survey {SurveyId}", surveyId);
        var survey = await factory();

        if (survey != null)
        {
            var cacheEntry = new CacheEntry<SurveyDto>
            {
                Value = survey,
                ExpiresAt = DateTime.UtcNow.Add(actualTtl),
                LastAccessTime = DateTime.UtcNow,
                AccessCount = 1
            };

            _surveyCache.AddOrUpdate(key, cacheEntry, (_, _) => cacheEntry);
            _logger.LogDebug("Cached survey {SurveyId} with TTL={TTL} minutes", surveyId, actualTtl.TotalMinutes);
        }

        return survey;
    }

    /// <summary>
    /// Gets or adds a list of surveys to the cache.
    /// </summary>
    public async Task<List<SurveyDto>> GetOrAddSurveyListAsync(
        string listKey,
        Func<Task<List<SurveyDto>>> factory,
        TimeSpan? ttl = null)
    {
        var actualTtl = ttl ?? TimeSpan.FromMinutes(DEFAULT_TTL_MINUTES);

        // Try to get from cache
        if (_surveyListCache.TryGetValue(listKey, out var entry) && !entry.IsExpired)
        {
            _logger.LogDebug("Cache HIT for survey list {ListKey}", listKey);
            entry.LastAccessTime = DateTime.UtcNow;
            entry.AccessCount++;
            return entry.Value;
        }

        // Cache miss - fetch from source
        _logger.LogDebug("Cache MISS for survey list {ListKey}", listKey);
        var surveys = await factory();

        var cacheEntry = new CacheEntry<List<SurveyDto>>
        {
            Value = surveys,
            ExpiresAt = DateTime.UtcNow.Add(actualTtl),
            LastAccessTime = DateTime.UtcNow,
            AccessCount = 1
        };

        _surveyListCache.AddOrUpdate(listKey, cacheEntry, (_, _) => cacheEntry);
        _logger.LogDebug(
            "Cached survey list {ListKey} with {Count} items, TTL={TTL} minutes",
            listKey,
            surveys.Count,
            actualTtl.TotalMinutes);

        return surveys;
    }

    /// <summary>
    /// Invalidates a specific survey from the cache.
    /// </summary>
    public void InvalidateSurvey(int surveyId)
    {
        var key = GetSurveyKey(surveyId);
        if (_surveyCache.TryRemove(key, out _))
        {
            _logger.LogDebug("Invalidated cache for survey {SurveyId}", surveyId);
        }
    }

    /// <summary>
    /// Invalidates a survey list from the cache.
    /// </summary>
    public void InvalidateSurveyList(string listKey)
    {
        if (_surveyListCache.TryRemove(listKey, out _))
        {
            _logger.LogDebug("Invalidated cache for survey list {ListKey}", listKey);
        }
    }

    /// <summary>
    /// Invalidates all cached surveys for a specific user.
    /// </summary>
    public void InvalidateUserSurveys(long userId)
    {
        var userListKey = GetUserSurveysKey(userId);
        InvalidateSurveyList(userListKey);
        _logger.LogDebug("Invalidated all surveys for user {UserId}", userId);
    }

    /// <summary>
    /// Invalidates all active surveys cache.
    /// </summary>
    public void InvalidateActiveSurveys()
    {
        InvalidateSurveyList("active_surveys");
        _logger.LogDebug("Invalidated active surveys cache");
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void ClearAll()
    {
        _surveyCache.Clear();
        _surveyListCache.Clear();
        _logger.LogInformation("Cache cleared: all entries removed");
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var totalEntries = _surveyCache.Count + _surveyListCache.Count;
        var expiredEntries = _surveyCache.Values.Count(e => e.IsExpired) +
                            _surveyListCache.Values.Count(e => e.IsExpired);

        var totalAccess = _surveyCache.Values.Sum(e => e.AccessCount) +
                         _surveyListCache.Values.Sum(e => e.AccessCount);

        return new CacheStatistics
        {
            TotalEntries = totalEntries,
            SurveyEntries = _surveyCache.Count,
            SurveyListEntries = _surveyListCache.Count,
            ExpiredEntries = expiredEntries,
            TotalAccesses = totalAccess,
            CacheHitRate = CalculateHitRate()
        };
    }

    private string GetSurveyKey(int surveyId) => $"survey_{surveyId}";
    private string GetUserSurveysKey(long userId) => $"user_surveys_{userId}";

    private async Task CleanupExpiredEntriesAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            var expiredSurveys = _surveyCache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            var expiredLists = _surveyListCache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredSurveys)
            {
                _surveyCache.TryRemove(key, out _);
            }

            foreach (var key in expiredLists)
            {
                _surveyListCache.TryRemove(key, out _);
            }

            var totalRemoved = expiredSurveys.Count + expiredLists.Count;
            if (totalRemoved > 0)
            {
                _logger.LogDebug(
                    "Cache cleanup: removed {Count} expired entries (surveys: {SurveyCount}, lists: {ListCount})",
                    totalRemoved,
                    expiredSurveys.Count,
                    expiredLists.Count);
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    private double CalculateHitRate()
    {
        // This is a simplified calculation
        // In production, you'd want to track hits/misses more precisely
        var totalEntries = _surveyCache.Count + _surveyListCache.Count;
        if (totalEntries == 0) return 0;

        var totalAccesses = _surveyCache.Values.Sum(e => e.AccessCount) +
                           _surveyListCache.Values.Sum(e => e.AccessCount);

        // Estimate: if average access count > 1, there were cache hits
        return totalAccesses > totalEntries ? (double)(totalAccesses - totalEntries) / totalAccesses * 100 : 0;
    }

    private class CacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime LastAccessTime { get; set; }
        public long AccessCount { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int SurveyEntries { get; set; }
    public int SurveyListEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public long TotalAccesses { get; set; }
    public double CacheHitRate { get; set; }
}

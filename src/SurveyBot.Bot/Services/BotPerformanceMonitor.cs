using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SurveyBot.Bot.Services;

/// <summary>
/// Monitors and logs bot performance metrics to ensure response times stay within targets.
/// Tracks operation durations and alerts on slow operations.
/// </summary>
public class BotPerformanceMonitor
{
    private const int SLOW_OPERATION_THRESHOLD_MS = 1000; // 1 second
    private const int WARNING_THRESHOLD_MS = 800; // 800ms
    private const int TARGET_RESPONSE_TIME_MS = 2000; // 2 seconds

    private readonly ILogger<BotPerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;

    public BotPerformanceMonitor(ILogger<BotPerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
    }

    /// <summary>
    /// Tracks the execution time of an operation and logs if it exceeds thresholds.
    /// </summary>
    public async Task<T> TrackOperationAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        string? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();

            LogOperationTime(operationName, stopwatch.ElapsedMilliseconds, context, isSuccess: true);
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, isSuccess: true);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogOperationTime(operationName, stopwatch.ElapsedMilliseconds, context, isSuccess: false);
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, isSuccess: false);

            _logger.LogError(
                ex,
                "Operation {OperationName} failed after {ElapsedMs}ms. Context: {Context}",
                operationName,
                stopwatch.ElapsedMilliseconds,
                context);

            throw;
        }
    }

    /// <summary>
    /// Tracks the execution time of an operation without a return value.
    /// </summary>
    public async Task TrackOperationAsync(
        string operationName,
        Func<Task> operation,
        string? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await operation();
            stopwatch.Stop();

            LogOperationTime(operationName, stopwatch.ElapsedMilliseconds, context, isSuccess: true);
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, isSuccess: true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogOperationTime(operationName, stopwatch.ElapsedMilliseconds, context, isSuccess: false);
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, isSuccess: false);

            _logger.LogError(
                ex,
                "Operation {OperationName} failed after {ElapsedMs}ms. Context: {Context}",
                operationName,
                stopwatch.ElapsedMilliseconds,
                context);

            throw;
        }
    }

    /// <summary>
    /// Creates a performance scope that automatically tracks duration on dispose.
    /// </summary>
    public IDisposable BeginScope(string operationName, string? context = null)
    {
        return new PerformanceScope(this, operationName, context);
    }

    /// <summary>
    /// Gets performance metrics for a specific operation.
    /// </summary>
    public PerformanceMetrics? GetMetrics(string operationName)
    {
        _metrics.TryGetValue(operationName, out var metrics);
        return metrics;
    }

    /// <summary>
    /// Gets all tracked performance metrics.
    /// </summary>
    public ConcurrentDictionary<string, PerformanceMetrics> GetAllMetrics()
    {
        return new ConcurrentDictionary<string, PerformanceMetrics>(_metrics);
    }

    /// <summary>
    /// Clears all tracked metrics.
    /// </summary>
    public void ClearMetrics()
    {
        _metrics.Clear();
        _logger.LogInformation("Performance metrics cleared");
    }

    private void LogOperationTime(
        string operationName,
        long elapsedMs,
        string? context,
        bool isSuccess)
    {
        var status = isSuccess ? "completed" : "failed";
        var contextInfo = !string.IsNullOrEmpty(context) ? $" [{context}]" : "";

        if (elapsedMs >= SLOW_OPERATION_THRESHOLD_MS)
        {
            _logger.LogWarning(
                "SLOW OPERATION: {OperationName}{Context} {Status} in {ElapsedMs}ms (threshold: {Threshold}ms)",
                operationName,
                contextInfo,
                status,
                elapsedMs,
                SLOW_OPERATION_THRESHOLD_MS);
        }
        else if (elapsedMs >= WARNING_THRESHOLD_MS)
        {
            _logger.LogWarning(
                "Operation {OperationName}{Context} {Status} in {ElapsedMs}ms (approaching threshold)",
                operationName,
                contextInfo,
                status,
                elapsedMs);
        }
        else
        {
            _logger.LogDebug(
                "Operation {OperationName}{Context} {Status} in {ElapsedMs}ms",
                operationName,
                contextInfo,
                status,
                elapsedMs);
        }
    }

    private void RecordMetric(string operationName, long elapsedMs, bool isSuccess)
    {
        _metrics.AddOrUpdate(
            operationName,
            _ => new PerformanceMetrics
            {
                OperationName = operationName,
                TotalCalls = 1,
                SuccessfulCalls = isSuccess ? 1 : 0,
                FailedCalls = isSuccess ? 0 : 1,
                TotalDurationMs = elapsedMs,
                MinDurationMs = elapsedMs,
                MaxDurationMs = elapsedMs,
                LastCallTimestamp = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.TotalCalls++;
                if (isSuccess)
                    existing.SuccessfulCalls++;
                else
                    existing.FailedCalls++;

                existing.TotalDurationMs += elapsedMs;
                existing.MinDurationMs = Math.Min(existing.MinDurationMs, elapsedMs);
                existing.MaxDurationMs = Math.Max(existing.MaxDurationMs, elapsedMs);
                existing.LastCallTimestamp = DateTime.UtcNow;

                return existing;
            });
    }

    private class PerformanceScope : IDisposable
    {
        private readonly BotPerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string? _context;
        private readonly Stopwatch _stopwatch;

        public PerformanceScope(BotPerformanceMonitor monitor, string operationName, string? context)
        {
            _monitor = monitor;
            _operationName = operationName;
            _context = context;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.LogOperationTime(_operationName, _stopwatch.ElapsedMilliseconds, _context, isSuccess: true);
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds, isSuccess: true);
        }
    }
}

/// <summary>
/// Contains performance metrics for a specific operation.
/// </summary>
public class PerformanceMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public long TotalCalls { get; set; }
    public long SuccessfulCalls { get; set; }
    public long FailedCalls { get; set; }
    public long TotalDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastCallTimestamp { get; set; }

    public double AverageDurationMs => TotalCalls > 0 ? (double)TotalDurationMs / TotalCalls : 0;
    public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;
}

using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.DTOs.Common;

/// <summary>
/// Data transfer object for pagination query parameters.
/// </summary>
public class PaginationQueryDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size (number of items per page).
    /// </summary>
    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Gets or sets the optional search term for filtering results.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Search term cannot exceed 200 characters")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the sort field name.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Sort field cannot exceed 50 characters")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort in descending order.
    /// Default is false (ascending).
    /// </summary>
    public bool SortDescending { get; set; } = false;
}

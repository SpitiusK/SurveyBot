using System.ComponentModel.DataAnnotations;

namespace SurveyBot.Core.Entities;

/// <summary>
/// Represents a survey with metadata and configuration.
/// Follows DDD principles with private setters, encapsulated collections, and factory methods.
/// </summary>
public class Survey : BaseEntity
{
    private readonly List<Question> _questions = new();
    private readonly List<Response> _responses = new();

    /// <summary>
    /// Gets the survey title.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the survey description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the unique survey code for easy sharing.
    /// </summary>
    [MaxLength(10)]
    public string? Code { get; private set; }

    /// <summary>
    /// Gets the ID of the user who created this survey.
    /// </summary>
    [Required]
    public int CreatorId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the survey is active and accepting responses.
    /// </summary>
    [Required]
    public bool IsActive { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether users can submit multiple responses.
    /// </summary>
    [Required]
    public bool AllowMultipleResponses { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether results should be shown to respondents.
    /// </summary>
    [Required]
    public bool ShowResults { get; private set; } = true;

    // Navigation properties

    /// <summary>
    /// Gets the user who created this survey.
    /// </summary>
    public User Creator { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of questions in this survey.
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    /// <summary>
    /// Gets the collection of responses to this survey.
    /// Returns a read-only view of the internal collection.
    /// </summary>
    public IReadOnlyCollection<Response> Responses => _responses.AsReadOnly();

    #region Constructors

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// Use Create() or CreateDraft() factory methods for application code.
    /// </summary>
    private Survey() : base()
    {
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new survey with full configuration.
    /// </summary>
    /// <param name="title">Survey title (3-500 characters)</param>
    /// <param name="creatorId">ID of the user creating the survey (must be positive)</param>
    /// <param name="description">Optional survey description</param>
    /// <param name="code">Optional survey code (auto-generated if null)</param>
    /// <param name="isActive">Whether the survey is active (default: false)</param>
    /// <param name="allowMultipleResponses">Whether multiple responses are allowed (default: false)</param>
    /// <param name="showResults">Whether results are shown to respondents (default: true)</param>
    /// <returns>New survey instance with validated data</returns>
    /// <exception cref="ArgumentException">If title is empty/too long or creatorId is not positive</exception>
    public static Survey Create(
        string title,
        int creatorId,
        string? description = null,
        string? code = null,
        bool isActive = false,
        bool allowMultipleResponses = false,
        bool showResults = true)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length < 3)
            throw new ArgumentException("Title must be at least 3 characters", nameof(title));
        if (title.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(title));
        if (creatorId <= 0)
            throw new ArgumentException("Creator ID must be positive", nameof(creatorId));

        var survey = new Survey
        {
            Title = title.Trim(),
            CreatorId = creatorId,
            Description = description?.Trim(),
            Code = code?.ToUpperInvariant(),
            IsActive = isActive,
            AllowMultipleResponses = allowMultipleResponses,
            ShowResults = showResults
        };

        return survey;
    }

    /// <summary>
    /// Creates a new draft survey with minimal configuration.
    /// Useful when title and creator are the only known values initially.
    /// </summary>
    /// <param name="title">Survey title (3-500 characters)</param>
    /// <param name="creatorId">ID of the user creating the survey (must be positive)</param>
    /// <returns>New draft survey instance (inactive, no multiple responses, shows results)</returns>
    /// <exception cref="ArgumentException">If title is empty/too long or creatorId is not positive</exception>
    public static Survey CreateDraft(string title, int creatorId)
    {
        return Create(
            title: title,
            creatorId: creatorId,
            isActive: false,
            allowMultipleResponses: false,
            showResults: true);
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Activates the survey, allowing responses.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsModified();
    }

    /// <summary>
    /// Deactivates the survey, preventing new responses.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsModified();
    }

    /// <summary>
    /// Updates survey metadata.
    /// </summary>
    /// <param name="title">New survey title (3-500 characters)</param>
    /// <param name="description">Optional new description</param>
    /// <param name="allowMultiple">Whether to allow multiple responses</param>
    /// <param name="showResults">Whether to show results to respondents</param>
    /// <exception cref="ArgumentException">If title is empty or too long</exception>
    public void UpdateMetadata(string title, string? description, bool allowMultiple, bool showResults)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length < 3)
            throw new ArgumentException("Title must be at least 3 characters", nameof(title));
        if (title.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        AllowMultipleResponses = allowMultiple;
        ShowResults = showResults;
        MarkAsModified();
    }

    /// <summary>
    /// Assigns a unique code to this survey.
    /// </summary>
    /// <param name="code">The survey code (will be uppercased)</param>
    public void AssignCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        Code = code.Trim().ToUpperInvariant();
        MarkAsModified();
    }

    #endregion

    #region Internal Methods (for testing and EF Core)

    /// <summary>
    /// Adds a question to the survey's collection. Internal use only.
    /// </summary>
    internal void AddQuestionInternal(Question question)
    {
        _questions.Add(question);
    }

    /// <summary>
    /// Adds a response to the survey's collection. Internal use only.
    /// </summary>
    internal void AddResponseInternal(Response response)
    {
        _responses.Add(response);
    }

    /// <summary>
    /// Sets the creator navigation property. Internal use only.
    /// </summary>
    internal void SetCreatorInternal(User creator)
    {
        Creator = creator;
    }

    /// <summary>
    /// Sets the survey title. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateMetadata().
    /// </summary>
    internal void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length > 500)
            throw new ArgumentException("Title cannot exceed 500 characters", nameof(title));
        Title = title;
    }

    /// <summary>
    /// Sets the survey description. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateMetadata().
    /// </summary>
    internal void SetDescription(string? description)
    {
        Description = description;
    }

    /// <summary>
    /// Sets the unique survey code. Used by tests only.
    /// For normal use, prefer Create() factory or AssignCode().
    /// </summary>
    internal void SetCode(string? code)
    {
        Code = code?.ToUpperInvariant();
    }

    /// <summary>
    /// Sets the creator ID. Used by tests only.
    /// For normal use, prefer Create() factory method.
    /// </summary>
    internal void SetCreatorId(int creatorId)
    {
        if (creatorId <= 0)
            throw new ArgumentException("Creator ID must be positive", nameof(creatorId));
        CreatorId = creatorId;
    }

    /// <summary>
    /// Sets whether the survey is active. Used by tests only.
    /// For normal use, prefer Activate() or Deactivate() methods.
    /// </summary>
    internal void SetIsActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Sets whether multiple responses are allowed. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateMetadata().
    /// </summary>
    internal void SetAllowMultipleResponses(bool allow)
    {
        AllowMultipleResponses = allow;
    }

    /// <summary>
    /// Sets whether results should be shown to respondents. Used by tests only.
    /// For normal use, prefer Create() factory or UpdateMetadata().
    /// </summary>
    internal void SetShowResults(bool show)
    {
        ShowResults = show;
    }

    #endregion
}

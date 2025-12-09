using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Question entity with specific query methods.
/// </summary>
public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public QuestionRepository(SurveyBotDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetBySurveyIdAsync(int surveyId)
    {
        var questions = await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .Include(q => q.Options.OrderBy(o => o.OrderIndex))  // EAGER LOAD OPTIONS for OptionDetails mapping
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        // 🔍 DEBUG LOGGING: Log what was loaded from database
        Console.WriteLine($"[QuestionRepository.GetBySurveyIdAsync] Loaded {questions.Count()} questions for survey {surveyId}");

        foreach (var question in questions)
        {
            Console.WriteLine(
                $"[QuestionRepository.GetBySurveyIdAsync] QuestionId={question.Id}, " +
                $"Type={question.QuestionType}, Options.Count={question.Options?.Count ?? 0}");

            if (question.QuestionType == QuestionType.Rating && question.Options != null && question.Options.Any())
            {
                Console.WriteLine($"[QuestionRepository.GetBySurveyIdAsync] ⭐ Rating question {question.Id} HAS {question.Options.Count} options:");
                foreach (var opt in question.Options)
                {
                    Console.WriteLine($"  - OptionId={opt.Id}, Text='{opt.Text}', OrderIndex={opt.OrderIndex}");
                }
            }
        }

        return questions;
    }

    /// <inheritdoc />
    public async Task<Question?> GetByIdWithAnswersAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Answers)
                .ThenInclude(a => a.Response)
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <inheritdoc />
    public async Task<Question?> GetByIdWithOptionsAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> ReorderQuestionsAsync(Dictionary<int, int> questionOrders)
    {
        if (questionOrders == null || questionOrders.Count == 0)
        {
            return false;
        }

        // Only use transaction for real databases (not InMemory)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (!isInMemory)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var (questionId, newOrderIndex) in questionOrders)
                {
                    var question = await GetByIdAsync(questionId);

                    if (question == null)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    question.SetOrderIndex(newOrderIndex);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            // InMemory database: skip transaction (operations are atomic by default)
            foreach (var (questionId, newOrderIndex) in questionOrders)
            {
                var question = await GetByIdAsync(questionId);

                if (question == null)
                {
                    return false;
                }

                question.SetOrderIndex(newOrderIndex);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetNextOrderIndexAsync(int surveyId)
    {
        var maxOrderIndex = await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .MaxAsync(q => (int?)q.OrderIndex);

        return (maxOrderIndex ?? -1) + 1;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetRequiredQuestionsBySurveyIdAsync(int surveyId)
    {
        return await _dbSet
            .Where(q => q.SurveyId == surveyId && q.IsRequired)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Question>> GetByTypeAsync(int surveyId, QuestionType questionType)
    {
        return await _dbSet
            .Where(q => q.SurveyId == surveyId && q.QuestionType == questionType)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteBySurveyIdAsync(int surveyId)
    {
        var questions = await _dbSet
            .Where(q => q.SurveyId == surveyId)
            .ToListAsync();

        if (questions.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(questions);
        await _context.SaveChangesAsync();

        return questions.Count;
    }

    /// <inheritdoc />
    public async Task<bool> BelongsToSurveyAsync(int questionId, int surveyId)
    {
        return await _dbSet
            .AnyAsync(q => q.Id == questionId && q.SurveyId == surveyId);
    }

    /// <inheritdoc />
    public override async Task<Question?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Survey)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<Question>> GetAllAsync()
    {
        return await _dbSet
            .Include(q => q.Survey)
            .OrderBy(q => q.SurveyId)
            .ThenBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Question>> GetWithFlowConfigurationAsync(int surveyId)
    {
        return await _dbSet
            .AsNoTracking() // ✅ No tracking needed for read-only validation queries
            .Where(q => q.SurveyId == surveyId)
            .Include(q => q.Options.OrderBy(o => o.OrderIndex)) // Eager load options
            // Note: DefaultNext is an owned type (NextQuestionDeterminant), automatically included
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int?> GetNextQuestionIdAsync(int questionId, string? selectedOptionText)
    {
        var question = await _dbSet
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null)
        {
            return null;
        }

        // For branching questions (SingleChoice, Rating with options)
        if (question.SupportsBranching && question.Options != null && question.Options.Any())
        {
            // Find the option by text
            var option = question.Options.FirstOrDefault(o => o.Text == selectedOptionText);

            if (option != null && option.Next != null)
            {
                // Return the question ID from the value object, or null if EndSurvey
                return option.Next.Type == Core.Enums.NextStepType.GoToQuestion
                    ? option.Next.NextQuestionId
                    : null;
            }

            // If no matching option or option has no Next, fall back to default
            return question.DefaultNext?.Type == Core.Enums.NextStepType.GoToQuestion
                ? question.DefaultNext.NextQuestionId
                : null;
        }

        // For non-branching questions (Text, MultipleChoice)
        return question.DefaultNext?.Type == Core.Enums.NextStepType.GoToQuestion
            ? question.DefaultNext.NextQuestionId
            : null;
    }

    /// <inheritdoc />
    public async Task<Question?> GetByIdWithFlowConfigAsync(int questionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()  // Read-only query for flow determination
            .Include(q => q.Options)  // Need options for conditional flow
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
    }
}

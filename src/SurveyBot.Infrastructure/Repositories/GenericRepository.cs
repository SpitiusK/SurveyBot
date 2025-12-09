using Microsoft.EntityFrameworkCore;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;

namespace SurveyBot.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing basic CRUD operations for entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly SurveyBotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public GenericRepository(SurveyBotDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<T> CreateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method explicitly marks the entity as Modified to ensure changes persist
    /// in both real databases (PostgreSQL) and in-memory databases (integration tests).
    /// The explicit state management pattern works reliably across all EF Core providers,
    /// unlike _dbSet.Update() which has known issues with in-memory change tracking.
    /// </remarks>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Explicitly mark entity as Modified for reliable state tracking
        // This pattern works correctly with both real and in-memory databases
        _context.Entry(entity).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);

        if (entity == null)
        {
            return false;
        }

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        return entity != null;
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}

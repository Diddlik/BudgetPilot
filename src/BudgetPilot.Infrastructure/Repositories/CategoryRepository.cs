using BudgetPilot.Application.Abstractions;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Repositories;

/// <summary>
/// EF Core Implementierung von <see cref="ICategoryRepository"/>.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly BudgetPilotDbContext _db;

    public CategoryRepository(BudgetPilotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.BudgetItems)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Categories
            .Include(c => c.BudgetItems)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    /// <summary>
    /// True, wenn der Kategorie mindestens ein BudgetItem zugeordnet ist
    /// (verhindert Hard-Delete gemäß Spec §6.4).
    /// </summary>
    public async Task<bool> HasItemsAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await _db.BudgetItems
            .AnyAsync(b => b.CategoryId == categoryId, ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        await _db.Categories.AddAsync(category, ct);
    }

    public void Update(Category category)
    {
        _db.Categories.Update(category);
    }
}

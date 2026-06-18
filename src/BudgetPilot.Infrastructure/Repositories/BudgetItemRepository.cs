using BudgetPilot.Application.Abstractions;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Repositories;

/// <summary>
/// EF Core Implementierung von <see cref="IBudgetItemRepository"/>.
/// Items werden stets MIT Versionen geladen (Include), genau eine Abfrage, kein N+1.
/// </summary>
public class BudgetItemRepository : IBudgetItemRepository
{
    private readonly BudgetPilotDbContext _db;

    public BudgetItemRepository(BudgetPilotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BudgetItem>> GetAllWithVersionsAsync(CancellationToken ct = default)
    {
        return await _db.BudgetItems
            .Include(b => b.Versions)
            .Include(b => b.Category)
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
    }

    public async Task<BudgetItem?> GetByIdWithVersionsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.BudgetItems
            .Include(b => b.Versions)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<BudgetItem>> GetByCategoryWithVersionsAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await _db.BudgetItems
            .Include(b => b.Versions)
            .Include(b => b.Category)
            .AsNoTracking()
            .Where(b => b.CategoryId == categoryId)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(BudgetItem item, CancellationToken ct = default)
    {
        await _db.BudgetItems.AddAsync(item, ct);
    }

    public void Update(BudgetItem item)
    {
        _db.BudgetItems.Update(item);
    }

    public void Remove(BudgetItem item)
    {
        _db.BudgetItems.Remove(item);
    }
}

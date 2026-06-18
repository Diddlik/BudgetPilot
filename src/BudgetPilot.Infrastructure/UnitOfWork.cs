using BudgetPilot.Application.Abstractions;
using BudgetPilot.Infrastructure.Data;

namespace BudgetPilot.Infrastructure;

/// <summary>
/// IUnitOfWork-Implementierung: delegiert an DbContext.SaveChangesAsync.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BudgetPilotDbContext _db;

    public UnitOfWork(BudgetPilotDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}

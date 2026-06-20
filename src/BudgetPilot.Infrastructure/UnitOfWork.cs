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

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // In Blazor Server lebt der DbContext über den gesamten Circuit. Eine
            // fehlgeschlagene Operation würde sonst getrackte Entitäten zurücklassen,
            // die JEDES folgende SaveChanges derselben Sitzung mitversuchen lässt und
            // damit „vergiftet" (z. B. „expected 1 row, affected 0"). Tracker leeren,
            // damit die nächste Operation sauber startet.
            _db.ChangeTracker.Clear();
            throw;
        }
    }
}

using BudgetPilot.Application.Abstractions;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Repositories;

public sealed class ActualTransactionStore(BudgetPilotDbContext db) : IActualTransactionStore
{
    public async Task<IReadOnlyList<ActualTransaction>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) =>
        await db.ActualTransactions
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

    public async Task<ActualTransactionWriteResult> AddAsync(
        ActualTransaction transaction,
        CancellationToken ct = default)
    {
        db.ActualTransactions.Add(transaction);
        try
        {
            await db.SaveChangesAsync(ct);
            return new ActualTransactionWriteResult(transaction, true);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var existing = await db.ActualTransactions
                .AsNoTracking()
                .Include(x => x.Category)
                .SingleOrDefaultAsync(x => x.Id == transaction.Id, ct);
            if (existing is not null)
                return new ActualTransactionWriteResult(existing, false);
            throw;
        }
    }
}

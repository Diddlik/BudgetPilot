using BudgetPilot.Application.Abstractions;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Repositories;

public sealed class ResolvedOccurrenceStore(BudgetPilotDbContext db) : IResolvedOccurrenceStore
{
    public async Task<IReadOnlySet<string>> GetResolvedKeysAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) =>
        (await db.OccurrenceResolutions
            .AsNoTracking()
            .Where(resolution => resolution.PlannedDueDate >= from
                && resolution.PlannedDueDate <= to)
            .Select(resolution => resolution.OccurrenceKey)
            .ToListAsync(ct))
        .ToHashSet(StringComparer.Ordinal);

    public Task<OccurrenceResolution?> GetAsync(string occurrenceKey, CancellationToken ct = default) =>
        db.OccurrenceResolutions
            .AsNoTracking()
            .Include(x => x.ActualTransaction)
            .SingleOrDefaultAsync(x => x.OccurrenceKey == occurrenceKey, ct);

    public async Task<OccurrenceResolution> AddAsync(
        OccurrenceResolution resolution,
        ActualTransaction? transaction,
        CancellationToken ct = default)
    {
        if (transaction is not null)
        {
            db.ActualTransactions.Add(transaction);
        }
        db.OccurrenceResolutions.Add(resolution);
        try
        {
            await db.SaveChangesAsync(ct);
            return resolution;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await GetAsync(resolution.OccurrenceKey, ct) is { } existing)
            {
                return existing;
            }
            throw;
        }
    }

    public async Task<IReadOnlyList<OccurrenceResolution>> GetRecentAsync(
        int max,
        CancellationToken ct = default) =>
        await db.OccurrenceResolutions
            .AsNoTracking()
            .Include(x => x.ActualTransaction)
            .OrderByDescending(x => x.CreatedAt)
            .Take(max)
            .ToListAsync(ct);

    public async Task<OccurrenceResolution?> UpdateConfirmedAsync(
        string occurrenceKey,
        decimal amount,
        DateOnly date,
        string? description,
        CancellationToken ct = default)
    {
        var resolution = await db.OccurrenceResolutions
            .Include(x => x.ActualTransaction)
            .SingleOrDefaultAsync(x => x.OccurrenceKey == occurrenceKey, ct);
        if (resolution?.Kind != BudgetPilot.Domain.Enums.OccurrenceResolutionKind.Confirmed
            || resolution.ActualTransaction is null)
        {
            return null;
        }

        resolution.ActualTransaction.Amount = amount;
        resolution.ActualTransaction.Date = date;
        resolution.ActualTransaction.Description = description;
        resolution.Note = description;
        await db.SaveChangesAsync(ct);
        return resolution;
    }

    public async Task<bool> RemoveAsync(string occurrenceKey, CancellationToken ct = default)
    {
        var resolution = await db.OccurrenceResolutions
            .Include(x => x.ActualTransaction)
            .SingleOrDefaultAsync(x => x.OccurrenceKey == occurrenceKey, ct);
        if (resolution is null)
        {
            return false;
        }

        db.OccurrenceResolutions.Remove(resolution);
        if (resolution.ActualTransaction is not null)
        {
            db.ActualTransactions.Remove(resolution.ActualTransaction);
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}

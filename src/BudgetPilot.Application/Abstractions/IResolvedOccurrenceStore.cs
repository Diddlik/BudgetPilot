namespace BudgetPilot.Application.Abstractions;

public interface IResolvedOccurrenceStore
{
    Task<IReadOnlySet<string>> GetResolvedKeysAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    Task<Domain.Entities.OccurrenceResolution?> GetAsync(
        string occurrenceKey,
        CancellationToken ct = default);

    Task<Domain.Entities.OccurrenceResolution> AddAsync(
        Domain.Entities.OccurrenceResolution resolution,
        Domain.Entities.ActualTransaction? transaction,
        CancellationToken ct = default);

    Task<IReadOnlyList<Domain.Entities.OccurrenceResolution>> GetRecentAsync(
        int max,
        CancellationToken ct = default);

    Task<Domain.Entities.OccurrenceResolution?> UpdateConfirmedAsync(
        string occurrenceKey,
        decimal amount,
        DateOnly date,
        string? description,
        CancellationToken ct = default);

    Task<bool> RemoveAsync(string occurrenceKey, CancellationToken ct = default);
}

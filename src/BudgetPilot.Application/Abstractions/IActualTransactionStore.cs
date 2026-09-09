using BudgetPilot.Domain.Entities;

namespace BudgetPilot.Application.Abstractions;

public interface IActualTransactionStore
{
    Task<IReadOnlyList<ActualTransaction>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    Task<ActualTransactionWriteResult> AddAsync(
        ActualTransaction transaction,
        CancellationToken ct = default);
}

public sealed record ActualTransactionWriteResult(ActualTransaction Transaction, bool Created);

namespace BudgetPilot.Application.Abstractions;

/// <summary>Schreibt alle ausstehenden Änderungen der Repositories in einem Vorgang.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

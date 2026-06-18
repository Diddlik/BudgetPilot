using BudgetPilot.Domain.Entities;

namespace BudgetPilot.Application.Abstractions;

/// <summary>
/// Persistenz-Zugriff auf <see cref="BudgetItem"/> inkl. Versionen. Implementierung in
/// der Infrastructure-Schicht (Track B). Items werden stets MIT Versionen geladen
/// (eine Abfrage, kein N+1).
/// </summary>
public interface IBudgetItemRepository
{
    Task<IReadOnlyList<BudgetItem>> GetAllWithVersionsAsync(CancellationToken ct = default);

    Task<BudgetItem?> GetByIdWithVersionsAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BudgetItem>> GetByCategoryWithVersionsAsync(Guid categoryId, CancellationToken ct = default);

    Task AddAsync(BudgetItem item, CancellationToken ct = default);

    void Update(BudgetItem item);

    void Remove(BudgetItem item);
}

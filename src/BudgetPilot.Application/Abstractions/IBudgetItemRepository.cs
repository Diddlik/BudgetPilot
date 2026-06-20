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

    /// <summary>
    /// Markiert eine NEUE Version explizit als hinzuzufügen. Nötig, weil EF Guid-PKs
    /// per Konvention als wertgeneriert behandelt: eine über die Navigations-Collection
    /// angehängte Version mit vorab gesetzter Guid würde sonst als UPDATE statt INSERT
    /// interpretiert (0 Zeilen betroffen).
    /// </summary>
    void AddVersion(BudgetItemVersion version);

    void Update(BudgetItem item);

    void Remove(BudgetItem item);
}

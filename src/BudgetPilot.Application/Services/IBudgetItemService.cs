using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;

namespace BudgetPilot.Application.Services;

/// <summary>
/// Anwendungsfälle rund um Budgetpositionen und ihre Versionen (§9). Kapselt die
/// Versionierungs-Invarianten serverseitig — die UI verlässt sich darauf.
/// </summary>
public interface IBudgetItemService
{
    Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct = default);

    Task<BudgetItemDto> UpdateMetadataAsync(Guid id, UpdateBudgetItemMetadataRequest request, CancellationToken ct = default);

    /// <summary>Versionsflow §4.3: neue Version ab Datum, alte Version wird beendet.</summary>
    Task<BudgetItemVersionDto> AddVersionAsync(Guid budgetItemId, CreateBudgetItemVersionRequest request, CancellationToken ct = default);

    /// <summary>In-place-Korrektur der aktuellen (jüngsten, offenen) Version.</summary>
    Task UpdateCurrentVersionAsync(Guid budgetItemId, UpdateVersionRequest request, CancellationToken ct = default);

    /// <summary>
    /// In-place-Korrektur einer BELIEBIGen (auch historischen) Version – z. B. um eine
    /// falsch eingetragene Position rückwirkend zu berichtigen. Die Gültig-bis-Grenze der
    /// Version bleibt erhalten; Überschneidungen mit anderen Versionen werden abgewiesen.
    /// </summary>
    Task UpdateVersionAsync(Guid budgetItemId, Guid versionId, UpdateVersionRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<BudgetItemDto>> GetAllAsync(CancellationToken ct = default);

    Task<BudgetItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);

    Task ReactivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Hard-Delete der Position inkl. aller Versionen.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

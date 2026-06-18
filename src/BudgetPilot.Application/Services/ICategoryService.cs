using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;

namespace BudgetPilot.Application.Services;

/// <summary>Anwendungsfälle rund um Kategorien (§9), inkl. Drilldown auf zugeordnete Positionen.</summary>
public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Alle Positionen einer Kategorie (Kategorie-Detailansicht §6.5).</summary>
    Task<IReadOnlyList<BudgetItemDto>> GetItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task RenameAsync(Guid id, string newName, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}

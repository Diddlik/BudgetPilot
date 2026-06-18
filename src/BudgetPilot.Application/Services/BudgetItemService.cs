using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;

namespace BudgetPilot.Application.Services;

/// <summary>
/// STUB (Wave 0). Implementierung in Track A: CRUD, Versionsflow §4.3, Validierung §7.
/// </summary>
public sealed class BudgetItemService : IBudgetItemService
{
    private readonly IBudgetItemRepository _items;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;

    public BudgetItemService(IBudgetItemRepository items, ICategoryRepository categories, IUnitOfWork uow)
    {
        _items = items;
        _categories = categories;
        _uow = uow;
    }

    public Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<BudgetItemDto> UpdateMetadataAsync(Guid id, UpdateBudgetItemMetadataRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<BudgetItemVersionDto> AddVersionAsync(Guid budgetItemId, CreateBudgetItemVersionRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task UpdateCurrentVersionAsync(Guid budgetItemId, UpdateVersionRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<IReadOnlyList<BudgetItemDto>> GetAllAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<BudgetItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task ReactivateAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");
}

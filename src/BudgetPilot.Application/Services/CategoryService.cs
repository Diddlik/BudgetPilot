using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;

namespace BudgetPilot.Application.Services;

/// <summary>STUB (Wave 0). Implementierung in Track A.</summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IBudgetItemRepository _items;
    private readonly IUnitOfWork _uow;

    public CategoryService(ICategoryRepository categories, IBudgetItemRepository items, IUnitOfWork uow)
    {
        _categories = categories;
        _items = items;
        _uow = uow;
    }

    public Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task<IReadOnlyList<BudgetItemDto>> GetItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException("Track A");
}

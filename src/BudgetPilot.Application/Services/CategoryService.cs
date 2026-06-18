using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services.Mapping;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Application.Services;

/// <summary>Anwendungsfälle rund um Kategorien (§9, §6.5). Eine Kategorie mit Items wird nie hart gelöscht (nur deaktiviert).</summary>
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

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Kategoriename darf nicht leer sein.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _categories.AddAsync(category, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return DtoMapper.ToDto(category, itemCount: 0);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _categories.GetAllAsync(ct).ConfigureAwait(false);
        var items = await _items.GetAllWithVersionsAsync(ct).ConfigureAwait(false);

        var counts = items
            .GroupBy(i => i.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return categories
            .Select(c => DtoMapper.ToDto(c, counts.TryGetValue(c.Id, out var n) ? n : 0))
            .ToList();
    }

    public async Task<IReadOnlyList<BudgetItemDto>> GetItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        var items = await _items.GetByCategoryWithVersionsAsync(categoryId, ct).ConfigureAwait(false);
        return items.Select(DtoMapper.ToDto).ToList();
    }

    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Kategoriename darf nicht leer sein.");

        var category = await GetCategoryOrThrowAsync(id, ct).ConfigureAwait(false);
        category.Name = newName.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        _categories.Update(category);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var category = await GetCategoryOrThrowAsync(id, ct).ConfigureAwait(false);
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        _categories.Update(category);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<Category> GetCategoryOrThrowAsync(Guid id, CancellationToken ct)
        => await _categories.GetByIdAsync(id, ct).ConfigureAwait(false)
           ?? throw new DomainException($"Kategorie {id} wurde nicht gefunden.");
}

using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services.Mapping;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Exceptions;
using BudgetPilot.Domain.Rules;

namespace BudgetPilot.Application.Services;

/// <summary>
/// CRUD + Versionsflow (§4.3) + Validierung (§7) für Budgetpositionen. Kapselt die
/// Versionierungs-Invarianten serverseitig (§9).
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

    public async Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct = default)
    {
        BudgetValidation.ValidateItemMetadata(request.Name);
        BudgetValidation.ValidateVersionValues(
            request.Amount, request.ValidFrom, validTo: null, request.PaymentDay, request.PaymentMonth);

        await EnsureActiveCategoryAsync(request.CategoryId, ct).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var item = new BudgetItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Type = request.Type,
            CategoryId = request.CategoryId,
            IsActive = true,
            CreatedAt = now,
            Versions =
            {
                new BudgetItemVersion
                {
                    Id = Guid.NewGuid(),
                    Amount = request.Amount,
                    Frequency = request.Frequency,
                    ValidFrom = request.ValidFrom,
                    ValidTo = null,
                    PaymentDay = request.PaymentDay,
                    PaymentMonth = request.PaymentMonth,
                    Note = request.Note,
                    CreatedAt = now
                }
            }
        };
        item.Versions[0].BudgetItemId = item.Id;

        await _items.AddAsync(item, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        // Kategoriename für das DTO nachladen, falls die Navigation nicht gesetzt ist.
        await EnsureCategoryNameAsync(item, ct).ConfigureAwait(false);
        return DtoMapper.ToDto(item);
    }

    public async Task<BudgetItemDto> UpdateMetadataAsync(
        Guid id, UpdateBudgetItemMetadataRequest request, CancellationToken ct = default)
    {
        BudgetValidation.ValidateItemMetadata(request.Name);

        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        await EnsureActiveCategoryAsync(request.CategoryId, ct).ConfigureAwait(false);

        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.Type = request.Type;
        item.CategoryId = request.CategoryId;
        item.UpdatedAt = DateTime.UtcNow;

        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await EnsureCategoryNameAsync(item, ct).ConfigureAwait(false);
        return DtoMapper.ToDto(item);
    }

    public async Task<BudgetItemVersionDto> AddVersionAsync(
        Guid budgetItemId, CreateBudgetItemVersionRequest request, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(budgetItemId, ct).ConfigureAwait(false);

        BudgetValidation.ValidateVersionValues(
            request.Amount, request.ValidFrom, validTo: null, request.PaymentDay, request.PaymentMonth);

        // §4.3: bisher offene Version beenden (ValidTo = D - 1 Tag), neue offene Version anhängen.
        var previousDay = request.ValidFrom.AddDays(-1);
        var open = item.Versions.Where(v => v.ValidTo is null).ToList();
        foreach (var prev in open)
        {
            if (prev.ValidFrom > previousDay)
                throw new DomainException(
                    "Die neue Version beginnt vor oder am Beginn der bisher offenen Version – kein lückenloser, überschneidungsfreier Anschluss möglich.");
            prev.ValidTo = previousDay;
        }

        // Überschneidungsfreiheit gegen ALLE bestehenden Versionen prüfen (§4.1.1).
        BudgetValidation.EnsureNoOverlap(item.Versions, request.ValidFrom, validTo: null);

        var now = DateTime.UtcNow;
        var version = new BudgetItemVersion
        {
            Id = Guid.NewGuid(),
            BudgetItemId = item.Id,
            Amount = request.Amount,
            Frequency = request.Frequency,
            ValidFrom = request.ValidFrom,
            ValidTo = null,
            PaymentDay = request.PaymentDay,
            PaymentMonth = request.PaymentMonth,
            Note = request.Note,
            CreatedAt = now
        };
        item.Versions.Add(version);
        item.UpdatedAt = now;

        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return DtoMapper.ToDto(version, isCurrent: true);
    }

    public async Task UpdateCurrentVersionAsync(
        Guid budgetItemId, UpdateVersionRequest request, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(budgetItemId, ct).ConfigureAwait(false);

        var current = DtoMapper.CurrentVersion(item)
            ?? throw new DomainException("Position besitzt keine Version, die in-place aktualisiert werden könnte.");

        BudgetValidation.ValidateVersionValues(
            request.Amount, request.ValidFrom, current.ValidTo, request.PaymentDay, request.PaymentMonth);

        // In-place Änderung darf keine Überschneidung mit den anderen Versionen erzeugen.
        BudgetValidation.EnsureNoOverlap(item.Versions, request.ValidFrom, current.ValidTo, ignoreVersionId: current.Id);

        current.Amount = request.Amount;
        current.Frequency = request.Frequency;
        current.ValidFrom = request.ValidFrom;
        current.PaymentDay = request.PaymentDay;
        current.PaymentMonth = request.PaymentMonth;
        current.Note = request.Note;
        item.UpdatedAt = DateTime.UtcNow;

        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BudgetItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _items.GetAllWithVersionsAsync(ct).ConfigureAwait(false);
        return items.Select(DtoMapper.ToDto).ToList();
    }

    public async Task<BudgetItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _items.GetByIdWithVersionsAsync(id, ct).ConfigureAwait(false);
        return item is null ? null : DtoMapper.ToDto(item);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        _items.Remove(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<BudgetItem> GetItemOrThrowAsync(Guid id, CancellationToken ct)
        => await _items.GetByIdWithVersionsAsync(id, ct).ConfigureAwait(false)
           ?? throw new DomainException($"Budgetposition {id} wurde nicht gefunden.");

    private async Task EnsureActiveCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(categoryId, ct).ConfigureAwait(false)
            ?? throw new DomainException($"Kategorie {categoryId} wurde nicht gefunden.");
        if (!category.IsActive)
            throw new DomainException($"Kategorie '{category.Name}' ist inaktiv und kann nicht zugewiesen werden.");
    }

    private async Task EnsureCategoryNameAsync(BudgetItem item, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(item.Category?.Name))
            return;
        var category = await _categories.GetByIdAsync(item.CategoryId, ct).ConfigureAwait(false);
        if (category is not null)
            item.Category = category;
    }
}

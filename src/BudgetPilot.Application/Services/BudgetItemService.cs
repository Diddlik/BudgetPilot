using System.Globalization;
using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services.Mapping;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using BudgetPilot.Domain.Rules;

namespace BudgetPilot.Application.Services;

/// <summary>
/// CRUD + Versionsflow (§4.3) + Validierung (§7) für Budgetpositionen. Kapselt die
/// Versionierungs-Invarianten serverseitig (§9).
/// </summary>
public sealed class BudgetItemService : IBudgetItemService
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private readonly IBudgetItemRepository _items;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLog _audit;

    public BudgetItemService(IBudgetItemRepository items, ICategoryRepository categories, IUnitOfWork uow, IAuditLog audit)
    {
        _items = items;
        _categories = categories;
        _uow = uow;
        _audit = audit;
    }

    private static string Money(decimal value) => value.ToString("C", De);

    private static string Freq(BudgetFrequency frequency) => frequency switch
    {
        BudgetFrequency.Monthly => "monatlich",
        BudgetFrequency.Quarterly => "quartalsweise",
        BudgetFrequency.Yearly => "jährlich",
        BudgetFrequency.Once => "einmalig",
        _ => frequency.ToString()
    };

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
            Owner = string.IsNullOrWhiteSpace(request.Owner) ? null : request.Owner.Trim(),
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

        await _audit.RecordAsync(AuditAction.Created, "BudgetItem", item.Id, item.Name,
            $"{Money(request.Amount)} · {Freq(request.Frequency)}, gültig ab {request.ValidFrom:dd.MM.yyyy}", ct)
            .ConfigureAwait(false);

        return DtoMapper.ToDto(item);
    }

    public async Task<BudgetItemDto> UpdateMetadataAsync(
        Guid id, UpdateBudgetItemMetadataRequest request, CancellationToken ct = default)
    {
        BudgetValidation.ValidateItemMetadata(request.Name);

        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        await EnsureActiveCategoryAsync(request.CategoryId, ct).ConfigureAwait(false);

        var oldName = item.Name;
        var oldType = item.Type;
        var oldCategoryId = item.CategoryId;

        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.Owner = string.IsNullOrWhiteSpace(request.Owner) ? null : request.Owner.Trim();
        item.Type = request.Type;
        item.CategoryId = request.CategoryId;
        item.UpdatedAt = DateTime.UtcNow;

        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await EnsureCategoryNameAsync(item, ct).ConfigureAwait(false);

        var changes = new List<string>();
        if (!string.Equals(oldName, item.Name, StringComparison.Ordinal))
            changes.Add($"Name: '{oldName}' → '{item.Name}'");
        if (oldType != item.Type)
            changes.Add($"Typ: {(oldType == BudgetItemType.Income ? "Einnahme" : "Ausgabe")} → {(item.Type == BudgetItemType.Income ? "Einnahme" : "Ausgabe")}");
        if (oldCategoryId != item.CategoryId)
            changes.Add($"Kategorie geändert zu '{item.Category?.Name}'");

        await _audit.RecordAsync(AuditAction.Updated, "BudgetItem", item.Id, item.Name,
            changes.Count > 0 ? string.Join(" · ", changes) : "Stammdaten aktualisiert", ct)
            .ConfigureAwait(false);

        return DtoMapper.ToDto(item);
    }

    public async Task<BudgetItemVersionDto> AddVersionAsync(
        Guid budgetItemId, CreateBudgetItemVersionRequest request, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(budgetItemId, ct).ConfigureAwait(false);

        BudgetValidation.ValidateVersionValues(
            request.Amount, request.ValidFrom, validTo: null, request.PaymentDay, request.PaymentMonth);

        var previousAmount = DtoMapper.CurrentVersion(item)?.Amount;

        // §4.3: Neue Version einfügen.
        // Vorwärts (Standard): bisher offene Version schließen (ValidTo = D-1), neue bleibt offen.
        // Rückwärts (retroaktiv): neue Version wird mit ValidTo = offeneVersion.ValidFrom-1 geschlossen,
        // die bestehende offene Version bleibt unverändert.
        var openVersions = item.Versions.Where(v => v.ValidTo is null).ToList();
        DateOnly? newValidTo = null;

        foreach (var prev in openVersions)
        {
            if (request.ValidFrom == prev.ValidFrom)
                throw new DomainException("Es existiert bereits eine Version mit diesem Startdatum.");

            if (request.ValidFrom < prev.ValidFrom)
            {
                // Rückwirkend: neue Version läuft bis zum Tag vor der bestehenden offenen Version.
                newValidTo = prev.ValidFrom.AddDays(-1);
            }
            else
            {
                // Standard vorwärts: bestehende offene Version schließen.
                prev.ValidTo = request.ValidFrom.AddDays(-1);
            }
        }

        // Überschneidungsfreiheit gegen ALLE bestehenden Versionen prüfen (§4.1.1).
        BudgetValidation.EnsureNoOverlap(item.Versions, request.ValidFrom, newValidTo);

        var now = DateTime.UtcNow;
        var version = new BudgetItemVersion
        {
            Id = Guid.NewGuid(),
            BudgetItemId = item.Id,
            Amount = request.Amount,
            Frequency = request.Frequency,
            ValidFrom = request.ValidFrom,
            ValidTo = newValidTo,
            PaymentDay = request.PaymentDay,
            PaymentMonth = request.PaymentMonth,
            Note = request.Note,
            CreatedAt = now
        };
        item.Versions.Add(version);
        item.UpdatedAt = now;

        // Neue Version explizit als Added markieren (EF behandelt Guid-PKs als wertgeneriert;
        // über die Collection angehängt würde sie sonst als UPDATE statt INSERT laufen). Die
        // Änderung an der Vorgänger-Version (ValidTo) und an item.UpdatedAt trackt EF selbst.
        _items.AddVersion(version);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var change = previousAmount is { } prevAmt
            ? $"{Money(prevAmt)} → {Money(request.Amount)}"
            : Money(request.Amount);
        await _audit.RecordAsync(AuditAction.VersionAdded, "BudgetItem", item.Id, item.Name,
            $"{change} · {Freq(request.Frequency)}, neue Version ab {request.ValidFrom:dd.MM.yyyy}", ct)
            .ConfigureAwait(false);

        return DtoMapper.ToDto(version, isCurrent: newValidTo is null);
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

        var beforeAmount = current.Amount;

        current.Amount = request.Amount;
        current.Frequency = request.Frequency;
        current.ValidFrom = request.ValidFrom;
        current.PaymentDay = request.PaymentDay;
        current.PaymentMonth = request.PaymentMonth;
        current.Note = request.Note;
        item.UpdatedAt = DateTime.UtcNow;

        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var change = beforeAmount != request.Amount
            ? $"{Money(beforeAmount)} → {Money(request.Amount)}"
            : Money(request.Amount);
        await _audit.RecordAsync(AuditAction.Updated, "BudgetItem", item.Id, item.Name,
            $"{change} · {Freq(request.Frequency)} (laufende Version geändert)", ct)
            .ConfigureAwait(false);
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

        await _audit.RecordAsync(AuditAction.Deactivated, "BudgetItem", item.Id, item.Name, null, ct)
            .ConfigureAwait(false);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        _items.Update(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await _audit.RecordAsync(AuditAction.Reactivated, "BudgetItem", item.Id, item.Name, null, ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetItemOrThrowAsync(id, ct).ConfigureAwait(false);
        var name = item.Name;
        _items.Remove(item);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await _audit.RecordAsync(AuditAction.Deleted, "BudgetItem", id, name, "Position mit allen Versionen entfernt", ct)
            .ConfigureAwait(false);
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

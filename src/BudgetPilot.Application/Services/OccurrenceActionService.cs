using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Application.Services;

public interface IOccurrenceActionService
{
    Task<OccurrenceResolutionDto> ConfirmAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        ConfirmOccurrenceRequest request,
        CancellationToken ct = default);

    Task<OccurrenceResolutionDto> SkipAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        SkipOccurrenceRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<OccurrenceResolutionDto>> GetRecentAsync(int max = 20, CancellationToken ct = default);

    Task<OccurrenceResolutionDto> CorrectAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        CorrectOccurrenceRequest request,
        CancellationToken ct = default);

    Task ReopenAsync(Guid budgetItemId, DateOnly plannedDueDate, CancellationToken ct = default);
}

public sealed class OccurrenceActionService(
    IPlannedOccurrenceService plannedOccurrences,
    IResolvedOccurrenceStore resolutions,
    IAuditLog audit) : IOccurrenceActionService
{
    public async Task<OccurrenceResolutionDto> ConfirmAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        ConfirmOccurrenceRequest request,
        CancellationToken ct = default)
    {
        var occurrenceKey = Key(budgetItemId, plannedDueDate);
        if (await resolutions.GetAsync(occurrenceKey, ct) is { } existing)
        {
            return Existing(existing, OccurrenceResolutionKind.Confirmed);
        }

        var planned = await FindOpenAsync(budgetItemId, plannedDueDate, ct);
        var amount = request.Amount ?? planned.PlannedAmount;
        if (amount < 0)
        {
            throw new DomainException("Der tatsächliche Betrag darf nicht negativ sein.");
        }

        var description = CleanNote(request.Description);
        var transaction = new ActualTransaction
        {
            Id = Guid.NewGuid(),
            Date = request.Date ?? plannedDueDate,
            Amount = amount,
            Type = planned.Type,
            CategoryId = planned.CategoryId,
            BudgetItemId = planned.BudgetItemId,
            PlannedDueDate = plannedDueDate,
            OccurrenceKey = occurrenceKey,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        var resolution = Resolution(planned, OccurrenceResolutionKind.Confirmed, description);
        resolution.ActualTransactionId = transaction.Id;
        resolution.ActualTransaction = transaction;
        var stored = await resolutions.AddAsync(resolution, transaction, ct);
        await audit.RecordAsync(
            AuditAction.Created, "Occurrence", stored.Id, stored.BudgetItemName,
            $"Fälligkeit {plannedDueDate:dd.MM.yyyy} als bezahlt bestätigt: {amount:0.00} EUR am {transaction.Date:dd.MM.yyyy}", ct);
        return Existing(stored, OccurrenceResolutionKind.Confirmed);
    }

    public async Task<OccurrenceResolutionDto> SkipAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        SkipOccurrenceRequest request,
        CancellationToken ct = default)
    {
        var occurrenceKey = Key(budgetItemId, plannedDueDate);
        if (await resolutions.GetAsync(occurrenceKey, ct) is { } existing)
        {
            return Existing(existing, OccurrenceResolutionKind.Skipped);
        }

        var planned = await FindOpenAsync(budgetItemId, plannedDueDate, ct);
        var resolution = Resolution(planned, OccurrenceResolutionKind.Skipped, CleanNote(request.Reason));
        var stored = await resolutions.AddAsync(resolution, null, ct);
        await audit.RecordAsync(
            AuditAction.Deactivated, "Occurrence", stored.Id, stored.BudgetItemName,
            $"Fälligkeit {plannedDueDate:dd.MM.yyyy} als entfallen markiert", ct);
        return Existing(stored, OccurrenceResolutionKind.Skipped);
    }

    public async Task<IReadOnlyList<OccurrenceResolutionDto>> GetRecentAsync(
        int max = 20,
        CancellationToken ct = default) =>
        (await resolutions.GetRecentAsync(Math.Clamp(max, 1, 100), ct)).Select(ToDto).ToList();

    public async Task<OccurrenceResolutionDto> CorrectAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        CorrectOccurrenceRequest request,
        CancellationToken ct = default)
    {
        if (request.Amount < 0)
        {
            throw new DomainException("Der tatsächliche Betrag darf nicht negativ sein.");
        }

        var key = Key(budgetItemId, plannedDueDate);
        var updated = await resolutions.UpdateConfirmedAsync(
            key, request.Amount, request.Date, CleanNote(request.Description), ct)
            ?? throw new DomainException("Nur eine bestätigte Zahlung kann korrigiert werden.");
        await audit.RecordAsync(
            AuditAction.Updated, "Occurrence", updated.Id, updated.BudgetItemName,
            $"Zahlung für {plannedDueDate:dd.MM.yyyy} korrigiert: {request.Amount:0.00} EUR am {request.Date:dd.MM.yyyy}", ct);
        return ToDto(updated);
    }

    public async Task ReopenAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        CancellationToken ct = default)
    {
        var key = Key(budgetItemId, plannedDueDate);
        var existing = await resolutions.GetAsync(key, ct)
            ?? throw new DomainException("Diese Fälligkeit ist bereits offen.");
        if (!await resolutions.RemoveAsync(key, ct))
        {
            throw new DomainException("Diese Fälligkeit ist bereits offen.");
        }
        await audit.RecordAsync(
            AuditAction.Reactivated, "Occurrence", existing.Id, existing.BudgetItemName,
            $"Fälligkeit {plannedDueDate:dd.MM.yyyy} wieder geöffnet", ct);
    }

    private async Task<PlannedOccurrenceDto> FindOpenAsync(
        Guid budgetItemId,
        DateOnly plannedDueDate,
        CancellationToken ct) =>
        (await plannedOccurrences.GetOpenAsync(plannedDueDate, plannedDueDate, ct))
            .SingleOrDefault(x => x.BudgetItemId == budgetItemId)
        ?? throw new DomainException("Diese Fälligkeit existiert nicht oder ist nicht mehr offen.");

    private static OccurrenceResolution Resolution(
        PlannedOccurrenceDto planned,
        OccurrenceResolutionKind kind,
        string? note) => new()
    {
        Id = Guid.NewGuid(),
        OccurrenceKey = planned.OccurrenceKey,
        BudgetItemId = planned.BudgetItemId,
        BudgetItemName = planned.BudgetItemName,
        CategoryName = planned.CategoryName,
        PlannedDueDate = planned.DueDate,
        PlannedAmount = planned.PlannedAmount,
        Kind = kind,
        Note = note,
        CreatedAt = DateTime.UtcNow
    };

    private static OccurrenceResolutionDto Existing(
        OccurrenceResolution resolution,
        OccurrenceResolutionKind expected)
    {
        if (resolution.Kind != expected)
        {
            throw new DomainException(resolution.Kind == OccurrenceResolutionKind.Confirmed
                ? "Diese Fälligkeit wurde bereits als bezahlt bestätigt."
                : "Diese Fälligkeit wurde bereits als entfallen markiert.");
        }
        return ToDto(resolution);
    }

    private static OccurrenceResolutionDto ToDto(OccurrenceResolution resolution) => new(
        resolution.OccurrenceKey,
        resolution.Kind,
        resolution.BudgetItemId,
        resolution.BudgetItemName,
        resolution.CategoryName,
        resolution.PlannedDueDate,
        resolution.PlannedAmount,
        resolution.ActualTransactionId,
        resolution.ActualTransaction?.Date,
        resolution.ActualTransaction?.Amount,
        resolution.Note);

    private static string Key(Guid budgetItemId, DateOnly plannedDueDate) =>
        $"{budgetItemId:N}:{plannedDueDate:yyyy-MM-dd}";

    private static string? CleanNote(string? note)
    {
        var result = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (result?.Length > 500)
        {
            throw new DomainException("Die Notiz darf höchstens 500 Zeichen lang sein.");
        }
        return result;
    }
}

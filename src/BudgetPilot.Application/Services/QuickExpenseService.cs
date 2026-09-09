using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Application.Services;

public interface IQuickExpenseService
{
    Task<ActualTransactionDto> CreateAsync(
        CreateQuickExpenseRequest request,
        CancellationToken ct = default);
}

public sealed class QuickExpenseService(
    IActualTransactionStore transactions,
    ICategoryRepository categories,
    IBudgetItemRepository budgetItems,
    IHouseholdSettingsService household,
    IAuditLog audit) : IQuickExpenseService
{
    public async Task<ActualTransactionDto> CreateAsync(
        CreateQuickExpenseRequest request,
        CancellationToken ct = default)
    {
        if (request.RequestId == Guid.Empty)
            throw new DomainException("Die Buchungs-ID fehlt.");
        if (request.Amount <= 0m)
            throw new DomainException("Der Betrag muss größer als 0 sein.");

        var category = await categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new DomainException("Die ausgewählte Kategorie wurde nicht gefunden.");
        if (!category.IsActive)
            throw new DomainException("Die ausgewählte Kategorie ist nicht aktiv.");

        if (request.BudgetItemId is { } budgetItemId)
        {
            var item = await budgetItems.GetByIdWithVersionsAsync(budgetItemId, ct)
                ?? throw new DomainException("Die ausgewählte Budgetposition wurde nicht gefunden.");
            if (!item.IsActive || item.Type != BudgetItemType.Expense || item.CategoryId != category.Id)
                throw new DomainException("Die Budgetposition passt nicht zur ausgewählten Ausgabenkategorie.");
        }

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        if (description?.Length > 500)
            throw new DomainException("Die Notiz darf höchstens 500 Zeichen lang sein.");

        var transaction = new ActualTransaction
        {
            Id = request.RequestId,
            Date = request.Date ?? await household.GetTodayAsync(ct),
            Amount = request.Amount,
            Type = BudgetItemType.Expense,
            CategoryId = category.Id,
            Category = category,
            BudgetItemId = request.BudgetItemId,
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };
        var stored = await transactions.AddAsync(transaction, ct);
        if (stored.Created)
        {
            await audit.RecordAsync(
                AuditAction.Created,
                "ActualTransaction",
                stored.Transaction.Id,
                "Schnellausgabe",
                $"{stored.Transaction.Amount:0.00} EUR in {category.Name} am {stored.Transaction.Date:dd.MM.yyyy}",
                ct);
        }

        return ToDto(stored.Transaction, stored.Transaction.Category?.Name ?? category.Name);
    }

    private static ActualTransactionDto ToDto(ActualTransaction transaction, string categoryName) => new(
        transaction.Id,
        transaction.Date,
        transaction.Amount,
        transaction.Type,
        transaction.CategoryId,
        categoryName,
        transaction.BudgetItemId,
        transaction.Description);
}

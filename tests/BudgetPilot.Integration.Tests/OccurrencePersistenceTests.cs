using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Integration.Tests;

public sealed class OccurrencePersistenceTests
{
    [Fact]
    public async Task QuickExpense_WithSameRequestId_IsStoredOnlyOnce()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>()
            .UseSqlite(connection)
            .Options;
        var categoryId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        await using (var setup = new BudgetPilotDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Categories.Add(new Category { Id = categoryId, Name = "Alltag", CreatedAt = DateTime.UtcNow });
            await setup.SaveChangesAsync();
        }

        await using (var firstDb = new BudgetPilotDbContext(options))
        {
            var first = await new ActualTransactionStore(firstDb).AddAsync(
                Transaction(requestId, categoryId, occurrenceKey: null));
            first.Created.Should().BeTrue();
        }

        await using var retryDb = new BudgetPilotDbContext(options);
        var retry = await new ActualTransactionStore(retryDb).AddAsync(
            Transaction(requestId, categoryId, occurrenceKey: null));

        retry.Created.Should().BeFalse();
        retry.Transaction.Id.Should().Be(requestId);
        (await retryDb.ActualTransactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ResolvedOccurrence_IsFound_AndCannotBeStoredTwice()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>()
            .UseSqlite(connection)
            .Options;
        var categoryId = Guid.NewGuid();
        var budgetItemId = Guid.NewGuid();
        const string occurrenceKey = "11111111111111111111111111111111:2026-02-05";

        await using (var db = new BudgetPilotDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Categories.Add(new Category
            {
                Id = categoryId,
                Name = "Wohnen",
                CreatedAt = DateTime.UtcNow
            });
            var store = new ResolvedOccurrenceStore(db);
            var transaction = Transaction(Guid.NewGuid(), categoryId, occurrenceKey);
            await store.AddAsync(Resolution(budgetItemId, occurrenceKey, transaction), transaction);
            (await store.GetResolvedKeysAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)))
                .Should().ContainSingle(occurrenceKey);
        }

        await using var duplicateDb = new BudgetPilotDbContext(options);
        var duplicateStore = new ResolvedOccurrenceStore(duplicateDb);
        var duplicateTransaction = Transaction(Guid.NewGuid(), categoryId, occurrenceKey);
        var existing = await duplicateStore.AddAsync(
            Resolution(budgetItemId, occurrenceKey, duplicateTransaction),
            duplicateTransaction);

        existing.OccurrenceKey.Should().Be(occurrenceKey);
        (await duplicateDb.OccurrenceResolutions.CountAsync()).Should().Be(1);
        (await duplicateDb.ActualTransactions.CountAsync()).Should().Be(1);

        var corrected = await duplicateStore.UpdateConfirmedAsync(
            occurrenceKey, 1188m, new DateOnly(2026, 2, 8), "Kontoauszug");
        corrected!.ActualTransaction!.Amount.Should().Be(1188m);
        corrected.ActualTransaction.Date.Should().Be(new DateOnly(2026, 2, 8));

        var actuals = await new ActualTransactionStore(duplicateDb).GetByDateRangeAsync(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));
        actuals.Should().ContainSingle();
        actuals.Single().Category.Name.Should().Be("Wohnen");

        (await duplicateStore.RemoveAsync(occurrenceKey)).Should().BeTrue();
        (await duplicateDb.OccurrenceResolutions.CountAsync()).Should().Be(0);
        (await duplicateDb.ActualTransactions.CountAsync()).Should().Be(0);
    }

    private static ActualTransaction Transaction(Guid id, Guid categoryId, string? occurrenceKey) => new()
    {
        Id = id,
        Date = new DateOnly(2026, 2, 5),
        PlannedDueDate = new DateOnly(2026, 2, 5),
        OccurrenceKey = occurrenceKey,
        Amount = 1200m,
        Type = BudgetItemType.Expense,
        CategoryId = categoryId,
        CreatedAt = DateTime.UtcNow
    };

    private static OccurrenceResolution Resolution(
        Guid budgetItemId,
        string occurrenceKey,
        ActualTransaction transaction) => new()
    {
        Id = Guid.NewGuid(),
        OccurrenceKey = occurrenceKey,
        BudgetItemId = budgetItemId,
        BudgetItemName = "Miete",
        CategoryName = "Wohnen",
        PlannedDueDate = new DateOnly(2026, 2, 5),
        PlannedAmount = 1200m,
        Kind = OccurrenceResolutionKind.Confirmed,
        ActualTransactionId = transaction.Id,
        ActualTransaction = transaction,
        CreatedAt = DateTime.UtcNow
    };
}

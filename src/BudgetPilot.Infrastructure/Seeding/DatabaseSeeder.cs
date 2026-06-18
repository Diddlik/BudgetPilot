using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetPilot.Infrastructure.Seeding;

/// <summary>
/// Seed-Daten gemäß Spec §12. Idempotent: prüft ob DB leer ist, bevor
/// Demo-Daten eingefügt werden.
/// </summary>
public class DatabaseSeeder
{
    private readonly BudgetPilotDbContext _db;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(BudgetPilotDbContext db, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with demo data from Spec §12 if the database is empty.
    /// Idempotent: skips seeding if any categories already exist.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Apply pending migrations
        await _db.Database.MigrateAsync(ct);

        // Idempotent check: skip if data already exists
        if (await _db.Categories.AnyAsync(ct))
        {
            _logger.LogDebug("Database already contains data — skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding database with demo data (Spec §12)...");

        var now = DateTime.UtcNow;

        // ── Categories ──────────────────────────────────────────────────
        var einkommen = new Category { Id = Guid.NewGuid(), Name = "Einkommen", IsActive = true, CreatedAt = now };
        var wohnen = new Category { Id = Guid.NewGuid(), Name = "Wohnen", IsActive = true, CreatedAt = now };
        var energie = new Category { Id = Guid.NewGuid(), Name = "Energie", IsActive = true, CreatedAt = now };
        var abos = new Category { Id = Guid.NewGuid(), Name = "Abos", IsActive = true, CreatedAt = now };
        var versicherungen = new Category { Id = Guid.NewGuid(), Name = "Versicherungen", IsActive = true, CreatedAt = now };
        var haushalt = new Category { Id = Guid.NewGuid(), Name = "Haushalt", IsActive = true, CreatedAt = now };

        _db.Categories.AddRange(einkommen, wohnen, energie, abos, versicherungen, haushalt);

        // ── BudgetItems + Versions (Spec §12) ──────────────────────────

        // 1. Gehalt: 3500 € monatlich ab 01.01.2026
        var gehalt = CreateItem("Gehalt", null, BudgetItemType.Income, einkommen.Id, now);
        gehalt.Versions.Add(CreateVersion(gehalt.Id, 3500.00m, BudgetFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, null, now));

        // 2. Miete: 1200 € monatlich ab 01.01.2026
        var miete = CreateItem("Miete", null, BudgetItemType.Expense, wohnen.Id, now);
        miete.Versions.Add(CreateVersion(miete.Id, 1200.00m, BudgetFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, null, now));

        // 3. Strom: 120 € monatlich ab 01.01.2026, Änderung auf 145 € ab 01.03.2026 (2 Versionen)
        var strom = CreateItem("Strom", null, BudgetItemType.Expense, energie.Id, now);
        strom.Versions.Add(CreateVersion(strom.Id, 120.00m, BudgetFrequency.Monthly,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28), null, null, now));
        strom.Versions.Add(CreateVersion(strom.Id, 145.00m, BudgetFrequency.Monthly,
            new DateOnly(2026, 3, 1), null, null, null, now));

        // 4. Netflix: 15,99 € monatlich ab 01.01.2026
        var netflix = CreateItem("Netflix", null, BudgetItemType.Expense, abos.Id, now);
        netflix.Versions.Add(CreateVersion(netflix.Id, 15.99m, BudgetFrequency.Monthly,
            new DateOnly(2026, 1, 1), null, null, null, now));

        // 5. Amazon Prime: 89,90 € jährlich ab 01.02.2026, PaymentMonth = 2 (Februar)
        var prime = CreateItem("Amazon Prime", null, BudgetItemType.Expense, abos.Id, now);
        prime.Versions.Add(CreateVersion(prime.Id, 89.90m, BudgetFrequency.Yearly,
            new DateOnly(2026, 2, 1), null, null, 2, now));

        // 6. Kfz-Versicherung: 720 € jährlich, PaymentMonth = 3 (März)
        var kfz = CreateItem("Kfz-Versicherung", null, BudgetItemType.Expense, versicherungen.Id, now);
        kfz.Versions.Add(CreateVersion(kfz.Id, 720.00m, BudgetFrequency.Yearly,
            new DateOnly(2026, 1, 1), null, null, 3, now));

        // 7. Haftpflicht: 90 € jährlich, PaymentMonth = 1 (Januar)
        var haftpflicht = CreateItem("Haftpflicht", null, BudgetItemType.Expense, versicherungen.Id, now);
        haftpflicht.Versions.Add(CreateVersion(haftpflicht.Id, 90.00m, BudgetFrequency.Yearly,
            new DateOnly(2026, 1, 1), null, null, 1, now));

        // 8. Waschmaschine: 600 € einmalig am 15.05.2026
        var waschmaschine = CreateItem("Waschmaschine", null, BudgetItemType.Expense, haushalt.Id, now);
        waschmaschine.Versions.Add(CreateVersion(waschmaschine.Id, 600.00m, BudgetFrequency.Once,
            new DateOnly(2026, 5, 15), null, 15, null, now));

        _db.BudgetItems.AddRange(gehalt, miete, strom, netflix, prime, kfz, haftpflicht, waschmaschine);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Database seeded successfully with {CategoryCount} categories and {ItemCount} budget items.",
            6, 8);
    }

    private static BudgetItem CreateItem(string name, string? description,
        BudgetItemType type, Guid categoryId, DateTime createdAt)
    {
        return new BudgetItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    private static BudgetItemVersion CreateVersion(Guid budgetItemId,
        decimal amount, BudgetFrequency frequency,
        DateOnly validFrom, DateOnly? validTo,
        int? paymentDay, int? paymentMonth, DateTime createdAt)
    {
        return new BudgetItemVersion
        {
            Id = Guid.NewGuid(),
            BudgetItemId = budgetItemId,
            Amount = amount,
            Frequency = frequency,
            ValidFrom = validFrom,
            ValidTo = validTo,
            PaymentDay = paymentDay,
            PaymentMonth = paymentMonth,
            CreatedAt = createdAt
        };
    }
}

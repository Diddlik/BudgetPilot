using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Tests.Fakes;

/// <summary>
/// Baut den Demo-Datensatz aus requirements.md §12 als in-memory Store. Wird von den
/// Projektions-Tests genutzt, um die Erwartungswerte aus §12 zu verifizieren.
/// </summary>
public static class SeedData
{
    public static InMemoryStore Build()
    {
        var store = new InMemoryStore();

        var einkommen = Category("Einkommen");
        var wohnen = Category("Wohnen");
        var energie = Category("Energie");
        var abos = Category("Abos");
        var versicherungen = Category("Versicherungen");
        var haushalt = Category("Haushalt");

        store.Categories.AddRange(new[] { einkommen, wohnen, energie, abos, versicherungen, haushalt });

        // Gehalt: 3500 € monatlich ab 01.01.2026
        store.Items.Add(Item("Gehalt", BudgetItemType.Income, einkommen,
            Version(3500m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        // Miete: 1200 € monatlich ab 01.01.2026
        store.Items.Add(Item("Miete", BudgetItemType.Expense, wohnen,
            Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        // Strom: 120 € monatlich ab 01.01.2026 (beendet 28.02.2026); 145 € ab 01.03.2026 → zwei Versionen
        store.Items.Add(Item("Strom", BudgetItemType.Expense, energie,
            Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), validTo: new DateOnly(2026, 2, 28)),
            Version(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1))));

        // Netflix: 15,99 € monatlich ab 01.01.2026
        store.Items.Add(Item("Netflix", BudgetItemType.Expense, abos,
            Version(15.99m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        // Amazon Prime: 89,90 € jährlich ab 01.02.2026, PaymentMonth = Februar
        store.Items.Add(Item("Amazon Prime", BudgetItemType.Expense, abos,
            Version(89.90m, BudgetFrequency.Yearly, new DateOnly(2026, 2, 1), paymentMonth: 2)));

        // Kfz-Versicherung: 720 € jährlich, PaymentMonth = März
        store.Items.Add(Item("Kfz-Versicherung", BudgetItemType.Expense, versicherungen,
            Version(720m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), paymentMonth: 3)));

        // Haftpflicht: 90 € jährlich, PaymentMonth = Januar
        store.Items.Add(Item("Haftpflicht", BudgetItemType.Expense, versicherungen,
            Version(90m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), paymentMonth: 1)));

        // Waschmaschine: 600 € einmalig am 15.05.2026
        store.Items.Add(Item("Waschmaschine", BudgetItemType.Expense, haushalt,
            Version(600m, BudgetFrequency.Once, new DateOnly(2026, 5, 15))));

        store.Link();
        return store;
    }

    public static Category Category(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public static BudgetItem Item(string name, BudgetItemType type, Category category, params BudgetItemVersion[] versions)
    {
        var item = new BudgetItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            CategoryId = category.Id,
            Category = category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Versions = versions.ToList()
        };
        foreach (var v in item.Versions)
        {
            v.BudgetItemId = item.Id;
            v.BudgetItem = item;
        }
        return item;
    }

    public static BudgetItemVersion Version(
        decimal amount,
        BudgetFrequency frequency,
        DateOnly validFrom,
        DateOnly? validTo = null,
        int? paymentDay = null,
        int? paymentMonth = null) => new()
    {
        Id = Guid.NewGuid(),
        Amount = amount,
        Frequency = frequency,
        ValidFrom = validFrom,
        ValidTo = validTo,
        PaymentDay = paymentDay,
        PaymentMonth = paymentMonth,
        CreatedAt = DateTime.UtcNow
    };
}

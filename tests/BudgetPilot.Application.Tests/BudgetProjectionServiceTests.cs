using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

/// <summary>
/// Pflichttests aus requirements.md §8 (Given/When/Then) + Invariante §4.1.2, verifiziert
/// gegen die Erwartungswerte aus §12. Tests laufen gegen die echten Services + In-memory Fakes.
/// </summary>
public sealed class BudgetProjectionServiceTests
{
    private static BudgetProjectionService Service(InMemoryStore store)
        => new(new FakeBudgetItemRepository(store));

    private static decimal AmountFor(BudgetPilot.Application.Dtos.MonthlyBudgetProjectionDto dto, string itemName)
        => dto.Lines.Single(l => l.BudgetItemName == itemName).ProjectedMonthlyAmount;

    // ── §8.1 Monthly: Miete 1200 €/Monat ab 01.01.2026 → März 2026 = 1200 € ──
    [Fact]
    public async Task Monthly_Rent_IsFullAmountEveryMonth()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Wohnen");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Miete", BudgetItemType.Expense, cat,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        var march = await Service(store).GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        AmountFor(march, "Miete").Should().Be(1200m);
    }

    // ── §8.2 Yearly Budget-Sicht: Kfz 720 €/Jahr ab 01.01.2026 → März Budget = 60 € ──
    [Fact]
    public async Task Yearly_Budget_IsAmountDividedBy12()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Versicherungen");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Kfz", BudgetItemType.Expense, cat,
            SeedData.Version(720m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), paymentMonth: 3)));

        var march = await Service(store).GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        AmountFor(march, "Kfz").Should().Be(60m);
    }

    // ── §8.3 Yearly Cashflow: Kfz 720 €, PaymentMonth=März → März = 720 €, April = 0 € ──
    [Fact]
    public async Task Yearly_Cashflow_IsFullAmountInPaymentMonthElseZero()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Versicherungen");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Kfz", BudgetItemType.Expense, cat,
            SeedData.Version(720m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), paymentMonth: 3)));
        var svc = Service(store);

        var march = await svc.GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Cashflow);
        var april = await svc.GetMonthlyProjectionAsync(2026, 4, BudgetViewMode.Cashflow);

        AmountFor(march, "Kfz").Should().Be(720m);
        AmountFor(april, "Kfz").Should().Be(0m);
    }

    // ── §8.4 Quarterly Budget-Sicht: 300 €/Quartal → Februar Budget = 100 € ──
    [Fact]
    public async Task Quarterly_Budget_IsAmountDividedBy3()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Sonstiges");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Quartalsbeitrag", BudgetItemType.Expense, cat,
            SeedData.Version(300m, BudgetFrequency.Quarterly, new DateOnly(2026, 1, 1))));

        var feb = await Service(store).GetMonthlyProjectionAsync(2026, 2, BudgetViewMode.Budget);

        AmountFor(feb, "Quartalsbeitrag").Should().Be(100m);
    }

    // ── §8.5 Quarterly Cashflow (Start Januar): Jan = 300, Feb = 0, April = 300 ──
    [Fact]
    public async Task Quarterly_Cashflow_FullAmountEveryThirdMonthFromValidFrom()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Sonstiges");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Quartalsbeitrag", BudgetItemType.Expense, cat,
            SeedData.Version(300m, BudgetFrequency.Quarterly, new DateOnly(2026, 1, 1))));
        var svc = Service(store);

        var jan = await svc.GetMonthlyProjectionAsync(2026, 1, BudgetViewMode.Cashflow);
        var feb = await svc.GetMonthlyProjectionAsync(2026, 2, BudgetViewMode.Cashflow);
        var apr = await svc.GetMonthlyProjectionAsync(2026, 4, BudgetViewMode.Cashflow);

        AmountFor(jan, "Quartalsbeitrag").Should().Be(300m);
        AmountFor(feb, "Quartalsbeitrag").Should().Be(0m);
        AmountFor(apr, "Quartalsbeitrag").Should().Be(300m);
    }

    // ── §8.6 Versionierung: Strom 120 ab 01.01, 145 ab 01.03 → Feb = 120, März = 145 (§12) ──
    [Fact]
    public async Task Versioning_Strom_UsesValidVersionPerMonth()
    {
        var store = SeedData.Build();
        var svc = Service(store);

        var jan = await svc.GetMonthlyProjectionAsync(2026, 1, BudgetViewMode.Budget);
        var feb = await svc.GetMonthlyProjectionAsync(2026, 2, BudgetViewMode.Budget);
        var march = await svc.GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        AmountFor(jan, "Strom").Should().Be(120m);
        AmountFor(feb, "Strom").Should().Be(120m);
        AmountFor(march, "Strom").Should().Be(145m);
    }

    // ── §8.7 Once: Waschmaschine 600 € am 15.05.2026 → Mai = 600, Juni = 0 ──
    [Fact]
    public async Task Once_WashingMachine_OnlyInEventMonth()
    {
        var store = SeedData.Build();
        var svc = Service(store);

        var may = await svc.GetMonthlyProjectionAsync(2026, 5, BudgetViewMode.Budget);
        var june = await svc.GetMonthlyProjectionAsync(2026, 6, BudgetViewMode.Budget);

        AmountFor(may, "Waschmaschine").Should().Be(600m);
        // Die offene Once-Version bleibt zwar ab Mai gültig (§4.2), ihr projizierter Betrag ist
        // außerhalb des Ereignismonats aber 0 (§5.1) – die Zeile wird gedämpft dargestellt (§6.2).
        AmountFor(june, "Waschmaschine").Should().Be(0m);
    }

    // ── §8.8 Keine Überschneidung (data error) → Exception ──
    // (Der konstruktive Versionsflow wird in BudgetItemServiceTests geprüft; hier der reine
    //  Datenfehler: zwei manuell überlappende Versionen werfen beim Projizieren.)
    [Fact]
    public async Task OverlappingVersions_InData_ThrowDomainException()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        // Jan–März UND eine zweite ab Februar offen → Februar/März doppelt gültig.
        store.Items.Add(SeedData.Item("Strom", BudgetItemType.Expense, cat,
            SeedData.Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), validTo: new DateOnly(2026, 3, 31)),
            SeedData.Version(145m, BudgetFrequency.Monthly, new DateOnly(2026, 2, 1))));

        var act = () => Service(store).GetMonthlyProjectionAsync(2026, 2, BudgetViewMode.Budget);

        await act.Should().ThrowAsync<DomainException>();
    }

    // ── §8.9 Aktiv/Inaktiv: inaktives Item taucht in zukünftiger Projektion nicht auf ──
    [Fact]
    public async Task Inactive_Item_IsExcludedFromProjection()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Abos");
        store.Categories.Add(cat);
        var item = SeedData.Item("Netflix", BudgetItemType.Expense, cat,
            SeedData.Version(15.99m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1)));
        item.IsActive = false;
        store.Items.Add(item);

        var march = await Service(store).GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        march.Lines.Any(l => l.BudgetItemName == "Netflix").Should().BeFalse();
    }

    // ── Zusätzliche Invariante §4.1.2: zwei gleichzeitig gültige Versionen → Exception ──
    [Fact]
    public async Task TwoSimultaneouslyValidVersions_ThrowDomainException()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        // Beide offen ab Januar → in jedem Monat zwei gültige Versionen.
        store.Items.Add(SeedData.Item("Strom", BudgetItemType.Expense, cat,
            SeedData.Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1)),
            SeedData.Version(145m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        var act = () => Service(store).GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*gleichzeitig gültige Versionen*");
    }

    // ── §12 Verifikation: März 2026 Budget-Sicht ──
    [Fact]
    public async Task March2026_BudgetView_MatchesSpecExpectations()
    {
        var store = SeedData.Build();
        var march = await Service(store).GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        march.TotalIncome.Should().Be(3500m);

        // Miete 1200 + Strom 145 + Netflix 15,99 + Kfz 60 + Haftpflicht 7,50 + Prime 7,49(16) ≈ 1.435,98
        AmountFor(march, "Miete").Should().Be(1200m);
        AmountFor(march, "Strom").Should().Be(145m);
        AmountFor(march, "Netflix").Should().Be(15.99m);
        AmountFor(march, "Kfz-Versicherung").Should().Be(60m);
        AmountFor(march, "Haftpflicht").Should().Be(7.50m);
        AmountFor(march, "Amazon Prime").Should().Be(89.90m / 12m);

        // Summe ≈ 1.435,98 € (gerundet auf 2 Nachkommastellen), Saldo ≈ 2.064,02 €.
        Math.Round(march.TotalExpense, 2).Should().Be(1435.98m);
        Math.Round(march.Balance, 2).Should().Be(2064.02m);
    }

    // ── §12 Verifikation: Kfz Cashflow nur im März ──
    [Fact]
    public async Task Kfz_Cashflow_OnlyInMarch()
    {
        var store = SeedData.Build();
        var svc = Service(store);

        for (var m = 1; m <= 12; m++)
        {
            var dto = await svc.GetMonthlyProjectionAsync(2026, m, BudgetViewMode.Cashflow);
            var amount = dto.Lines.Single(l => l.BudgetItemName == "Kfz-Versicherung").ProjectedMonthlyAmount;
            if (m == 3)
                amount.Should().Be(720m);
            else
                amount.Should().Be(0m);
        }
    }

    // ── Jahresübersicht: 12 Monate + Jahressummen, eine einzige Datenbasis ──
    [Fact]
    public async Task Yearly_Aggregates12MonthsAndYearTotals()
    {
        var store = SeedData.Build();
        var year = await Service(store).GetYearlyProjectionAsync(2026, BudgetViewMode.Budget);

        year.Months.Should().HaveCount(12);
        year.TotalIncome.Should().Be(3500m * 12);
        year.Balance.Should().Be(year.TotalIncome - year.TotalExpense);

        // Budget-Jahressumme der Ausgaben entspricht der Summe der 12 Monats-Ausgaben.
        year.TotalExpense.Should().Be(year.Months.Sum(m => m.TotalExpense));
    }
}

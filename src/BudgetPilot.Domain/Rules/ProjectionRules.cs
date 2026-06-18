using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Domain.Rules;

/// <summary>
/// Deterministische Projektionsregeln (requirements.md §4.2 + §5.1). Reine Funktionen ohne
/// Persistenz, damit Berechnung und Versionierungs-Invarianten unit-testbar sind.
/// </summary>
public static class ProjectionRules
{
    /// <summary>Erster Tag von Monat M.</summary>
    public static DateOnly FirstDayOfMonth(int year, int month) => new(year, month, 1);

    /// <summary>Letzter Tag von Monat M.</summary>
    public static DateOnly LastDayOfMonth(int year, int month)
        => new(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>§4.2: Eine Version ist in M gültig, wenn ValidFrom &lt;= letzter Tag von M und (ValidTo == null oder ValidTo &gt;= erster Tag von M).</summary>
    public static bool IsValidInMonth(BudgetItemVersion version, int year, int month)
    {
        var first = FirstDayOfMonth(year, month);
        var last = LastDayOfMonth(year, month);
        return version.ValidFrom <= last && (version.ValidTo is null || version.ValidTo >= first);
    }

    /// <summary>
    /// Wählt die einzige in M gültige Version eines Items (§4.2). Existieren zwei gleichzeitig
    /// gültige Versionen, ist das ein Datenfehler (§4.1.2) → <see cref="DomainException"/>.
    /// Gibt <c>null</c> zurück, wenn keine Version in M gültig ist.
    /// </summary>
    public static BudgetItemVersion? SelectValidVersion(BudgetItem item, int year, int month)
    {
        BudgetItemVersion? selected = null;
        foreach (var v in item.Versions)
        {
            if (!IsValidInMonth(v, year, month))
                continue;

            if (selected is not null)
                throw new DomainException(
                    $"Item '{item.Name}' ({item.Id}) hat im Monat {year}-{month:00} zwei gleichzeitig gültige Versionen (Invariante §4.1.2 verletzt).");

            selected = v;
        }

        return selected;
    }

    /// <summary>MonatsabstandSeit(ValidFrom, y, m) = (y - VF.Year) * 12 + (m - VF.Month).</summary>
    public static int MonthsSince(DateOnly validFrom, int year, int month)
        => (year - validFrom.Year) * 12 + (month - validFrom.Month);

    /// <summary>Effektiver Zahlungsmonat: PaymentMonth, sonst Monat aus ValidFrom.</summary>
    public static int PaymentMonthOf(BudgetItemVersion version)
        => version.PaymentMonth ?? version.ValidFrom.Month;

    /// <summary>
    /// Projizierter Monatsbetrag der Version in M je Sicht (§5.1). <paramref name="isDue"/> kennzeichnet,
    /// ob in der Cashflow-Sicht eine Zahlung fällig ist bzw. (Budget) ein lumpy Betrag anteilig läuft.
    /// </summary>
    public static decimal ProjectedMonthlyAmount(
        BudgetItemVersion version, int year, int month, BudgetViewMode viewMode, out bool isDue)
    {
        switch (version.Frequency)
        {
            case BudgetFrequency.Monthly:
                isDue = true;
                return version.Amount;

            case BudgetFrequency.Quarterly:
                if (viewMode == BudgetViewMode.Budget)
                {
                    isDue = true;
                    return version.Amount / 3m;
                }
                // Cashflow: voller Betrag, wenn MonatsabstandSeit(ValidFrom) % 3 == 0.
                var dueQuarter = MonthsSince(version.ValidFrom, year, month) % 3 == 0;
                isDue = dueQuarter;
                return dueQuarter ? version.Amount : 0m;

            case BudgetFrequency.Yearly:
                if (viewMode == BudgetViewMode.Budget)
                {
                    isDue = true;
                    return version.Amount / 12m;
                }
                // Cashflow: voller Betrag im Zahlungsmonat, sonst 0.
                var dueYear = PaymentMonthOf(version) == month;
                isDue = dueYear;
                return dueYear ? version.Amount : 0m;

            case BudgetFrequency.Once:
                // Beide Sichten identisch: Betrag im ValidFrom-Monat, sonst 0.
                var dueOnce = version.ValidFrom.Year == year && version.ValidFrom.Month == month;
                isDue = dueOnce;
                return dueOnce ? version.Amount : 0m;

            default:
                throw new DomainException($"Unbekannte Frequenz: {version.Frequency}.");
        }
    }
}

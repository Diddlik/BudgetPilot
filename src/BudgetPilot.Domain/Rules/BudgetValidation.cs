using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Domain.Rules;

/// <summary>
/// Fachliche Validierungsregeln (requirements.md §7). Verstöße werfen <see cref="DomainException"/>;
/// die Aufrufer (Application-Services) persistieren dann nicht.
/// </summary>
public static class BudgetValidation
{
    /// <summary>Validiert die Werte einer Version (§7.2/§7.3) – unabhängig vom Persistenzkontext.</summary>
    public static void ValidateVersionValues(
        decimal amount,
        DateOnly validFrom,
        DateOnly? validTo,
        int? paymentDay,
        int? paymentMonth)
    {
        if (amount < 0m)
            throw new DomainException("Betrag darf nicht negativ sein (Amount >= 0).");

        if (validTo.HasValue && validTo.Value < validFrom)
            throw new DomainException("ValidTo darf nicht vor ValidFrom liegen.");

        if (paymentDay.HasValue && (paymentDay.Value < 1 || paymentDay.Value > 31))
            throw new DomainException("PaymentDay muss zwischen 1 und 31 liegen.");

        if (paymentMonth.HasValue && (paymentMonth.Value < 1 || paymentMonth.Value > 12))
            throw new DomainException("PaymentMonth muss zwischen 1 und 12 liegen.");
    }

    /// <summary>Validiert die Stammdaten einer Position (§7.1). Kategorie-Existenz/-Aktivität prüft der Service.</summary>
    public static void ValidateItemMetadata(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name darf nicht leer sein.");
    }

    /// <summary>
    /// Stellt sicher, dass eine neue/geänderte Version sich mit keiner bestehenden Version
    /// desselben Items überschneidet (§4.1.1 / §7.2). <paramref name="ignoreVersionId"/> erlaubt
    /// den Selbstvergleich beim In-place-Update zu überspringen.
    /// </summary>
    public static void EnsureNoOverlap(
        IEnumerable<BudgetItemVersion> existingVersions,
        DateOnly validFrom,
        DateOnly? validTo,
        Guid? ignoreVersionId = null)
    {
        foreach (var v in existingVersions)
        {
            if (ignoreVersionId.HasValue && v.Id == ignoreVersionId.Value)
                continue;

            if (IntervalsOverlap(validFrom, validTo, v.ValidFrom, v.ValidTo))
                throw new DomainException(
                    "Die Version überschneidet sich zeitlich mit einer bestehenden Version desselben Items.");
        }
    }

    /// <summary>
    /// Prüft die Versionskette eines Items auf MONATSebene (§4.1.2): kein Monat darf von zwei
    /// Versionen berührt werden (die Projektion rechnet monatsweise), und keine Version darf
    /// durch eine Grenzverschiebung leer werden.
    /// </summary>
    public static void EnsureNoMonthOverlap(IEnumerable<BudgetItemVersion> versions)
    {
        var ordered = versions.OrderBy(v => v.ValidFrom).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var v = ordered[i];

            if (v.ValidTo is { } to && MonthKey(to) < MonthKey(v.ValidFrom))
                throw new DomainException(
                    $"Die Version ab {v.ValidFrom:dd.MM.yyyy} hätte danach keinen gültigen Monat mehr. Wähle einen anderen Zeitraum.");

            if (i == 0)
                continue;

            var prev = ordered[i - 1];
            if (prev.ValidTo is null || MonthKey(prev.ValidTo.Value) >= MonthKey(v.ValidFrom))
                throw new DomainException(
                    $"Die Versionen ab {prev.ValidFrom:dd.MM.yyyy} und ab {v.ValidFrom:dd.MM.yyyy} würden sich denselben Monat teilen.");
        }
    }

    private static int MonthKey(DateOnly d) => d.Year * 12 + d.Month;

    /// <summary>Halboffene/geschlossene Intervalle [from, to] (to == null = unendlich) überschneiden sich?</summary>
    private static bool IntervalsOverlap(DateOnly aFrom, DateOnly? aTo, DateOnly bFrom, DateOnly? bTo)
    {
        // Überschneidung, wenn aFrom <= bEnd UND bFrom <= aEnd.
        var aEnd = aTo ?? DateOnly.MaxValue;
        var bEnd = bTo ?? DateOnly.MaxValue;
        return aFrom <= bEnd && bFrom <= aEnd;
    }
}

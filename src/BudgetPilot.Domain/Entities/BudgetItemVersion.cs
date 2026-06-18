using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Domain.Entities;

/// <summary>
/// Zeitlich gültige Ausprägung eines <see cref="BudgetItem"/>: Betrag, Frequenz und
/// Gültigkeitszeitraum. Versionen desselben Items dürfen sich nicht überschneiden.
/// </summary>
public class BudgetItemVersion
{
    public Guid Id { get; set; }
    public Guid BudgetItemId { get; set; }
    public BudgetItem BudgetItem { get; set; } = null!;

    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; }

    /// <summary>Erster gültiger Tag (inklusive).</summary>
    public DateOnly ValidFrom { get; set; }

    /// <summary>Letzter gültiger Tag (inklusive); <c>null</c> = offen/unbegrenzt.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Optionaler Zahltag im Monat (1..31).</summary>
    public int? PaymentDay { get; set; }

    /// <summary>Optionaler Zahlungsmonat (1..12), v. a. für Yearly/Quarterly.</summary>
    public int? PaymentMonth { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

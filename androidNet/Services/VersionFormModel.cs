using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Mobile.Services;

public sealed class VersionFormModel
{
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; } = BudgetFrequency.Monthly;
    public DateOnly ValidFrom { get; set; }

    /// <summary>Nur bei der Korrektur relevant; null = offen (wird serverseitig am Nachfolger geklemmt).</summary>
    public DateOnly? ValidTo { get; set; }

    public int? PaymentDay { get; set; }
    public int? PaymentMonth { get; set; }
    public string? Note { get; set; }

    public void Reset(DateOnly validFrom)
    {
        Amount = 0m;
        Frequency = BudgetFrequency.Monthly;
        ValidFrom = validFrom;
        ValidTo = null;
        PaymentDay = null;
        PaymentMonth = null;
        Note = null;
    }

    public void Load(BudgetItemVersionDto version)
    {
        Amount = version.Amount;
        Frequency = version.Frequency;
        ValidFrom = version.ValidFrom;
        ValidTo = version.ValidTo;
        PaymentDay = version.PaymentDay;
        PaymentMonth = version.PaymentMonth;
        Note = version.Note;
    }
}

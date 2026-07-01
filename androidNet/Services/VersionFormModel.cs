using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Mobile.Services;

public sealed class VersionFormModel
{
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; } = BudgetFrequency.Monthly;
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int? PaymentDay { get; set; }
    public int? PaymentMonth { get; set; }
    public string? Note { get; set; }

    public void Reset(DateOnly validFrom)
    {
        Amount = 0m;
        Frequency = BudgetFrequency.Monthly;
        ValidFrom = validFrom;
        PaymentDay = null;
        PaymentMonth = null;
        Note = null;
    }

    public void Load(BudgetItemVersionDto version)
    {
        Amount = version.Amount;
        Frequency = version.Frequency;
        ValidFrom = version.ValidFrom;
        PaymentDay = version.PaymentDay;
        PaymentMonth = version.PaymentMonth;
        Note = version.Note;
    }
}

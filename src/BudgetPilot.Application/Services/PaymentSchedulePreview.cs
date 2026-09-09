using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Rules;

namespace BudgetPilot.Application.Services;

/// <summary>Berechnet eine Vorschau aus noch nicht gespeicherten Formulardaten.</summary>
public static class PaymentSchedulePreview
{
    public static IReadOnlyList<PaymentSchedulePreviewEntry> Build(
        CreateBudgetItemVersionRequest request,
        int count = 3)
    {
        BudgetValidation.ValidateVersionValues(
            request.Amount,
            request.ValidFrom,
            validTo: null,
            request.PaymentDay,
            request.PaymentMonth);

        count = Math.Clamp(count, 1, 12);
        if (request.Frequency == BudgetFrequency.Once)
        {
            return [new PaymentSchedulePreviewEntry(request.ValidFrom, request.Amount)];
        }

        var dates = new List<PaymentSchedulePreviewEntry>(count);
        var candidate = FirstDueDate(request);
        var step = request.Frequency == BudgetFrequency.Monthly ? 1 :
            request.Frequency == BudgetFrequency.Quarterly ? 3 : 12;

        while (dates.Count < count)
        {
            if (candidate >= request.ValidFrom)
            {
                dates.Add(new PaymentSchedulePreviewEntry(candidate, request.Amount));
            }

            candidate = AddMonths(candidate, step, request.PaymentDay ?? request.ValidFrom.Day);
        }

        return dates;
    }

    private static DateOnly FirstDueDate(CreateBudgetItemVersionRequest request)
    {
        var month = request.Frequency == BudgetFrequency.Yearly
            ? request.PaymentMonth ?? request.ValidFrom.Month
            : request.ValidFrom.Month;
        var day = request.PaymentDay ?? request.ValidFrom.Day;
        var candidate = InMonth(request.ValidFrom.Year, month, day);

        if (candidate >= request.ValidFrom)
        {
            return candidate;
        }

        var step = request.Frequency == BudgetFrequency.Monthly ? 1 :
            request.Frequency == BudgetFrequency.Quarterly ? 3 : 12;
        return AddMonths(candidate, step, day);
    }

    private static DateOnly AddMonths(DateOnly date, int months, int preferredDay)
    {
        var target = new DateOnly(date.Year, date.Month, 1).AddMonths(months);
        return InMonth(target.Year, target.Month, preferredDay);
    }

    private static DateOnly InMonth(int year, int month, int preferredDay) =>
        new(year, month, Math.Min(preferredDay, DateTime.DaysInMonth(year, month)));
}

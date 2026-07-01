namespace BudgetPilot.Mobile.Services;

public static class BudgetItemInputValidator
{
    public static void ValidateMetadata(string name, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Bitte einen Namen eingeben.");
        }
        if (categoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Bitte eine Kategorie auswählen.");
        }
    }

    public static void ValidateVersion(VersionFormModel model)
    {
        if (model.Amount < 0)
        {
            throw new InvalidOperationException("Der Betrag darf nicht negativ sein.");
        }
        if (model.PaymentDay is < 1 or > 31)
        {
            throw new InvalidOperationException("Der Zahlungstag muss zwischen 1 und 31 liegen.");
        }
        if (model.PaymentMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Der Zahlungsmonat muss zwischen 1 und 12 liegen.");
        }
    }
}

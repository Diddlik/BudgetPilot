namespace BudgetPilot.Mobile.Services;

public static class InstanceAddress
{
    public static string Normalize(string value)
    {
        var candidate = value.Trim();
        if (string.IsNullOrWhiteSpace(candidate) || candidate is "https://" or "http://")
        {
            throw InvalidAddress();
        }

        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = $"https://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw InvalidAddress();
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static InvalidOperationException InvalidAddress() =>
        new("Bitte einen gültigen Hostnamen und HTTP oder HTTPS auswählen.");
}

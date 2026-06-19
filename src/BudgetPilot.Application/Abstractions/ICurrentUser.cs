namespace BudgetPilot.Application.Abstractions;

/// <summary>Der zur Laufzeit angemeldete Benutzer (für das Änderungsprotokoll).</summary>
public interface ICurrentUser
{
    Task<CurrentUserInfo> GetAsync(CancellationToken ct = default);
}

/// <summary>Identität des Akteurs; <see cref="DisplayName"/> ist "System", wenn niemand angemeldet ist.</summary>
public sealed record CurrentUserInfo(string? UserId, string DisplayName)
{
    public static readonly CurrentUserInfo System = new(null, "System");
}

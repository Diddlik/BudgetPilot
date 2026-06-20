using System.Globalization;
using System.Reflection;

namespace BudgetPilot.Web.Services;

/// <summary>
/// Liest die zur Buildzeit eingebetteten Versions-/Commit-Informationen aus den
/// Assembly-Attributen. Wird im UI angezeigt, damit erkennbar ist, welche Version
/// gerade läuft (relevant bei automatischen Watchtower-Updates).
/// </summary>
public static class AppVersion
{
    public static string Version { get; }
    public static string? CommitHash { get; }
    public static DateTimeOffset? CommitDate { get; }

    static AppVersion()
    {
        var asm = typeof(AppVersion).Assembly;
        var meta = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Value is not null)
            .ToDictionary(a => a.Key, a => a.Value!, StringComparer.OrdinalIgnoreCase);

        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = info?.Split('+')[0];
        Version = string.IsNullOrWhiteSpace(version)
            ? asm.GetName().Version?.ToString(3) ?? "0.0.0"
            : version;

        CommitHash = Clean(meta.GetValueOrDefault("CommitHash"));

        if (Clean(meta.GetValueOrDefault("CommitDate")) is { } raw
            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            CommitDate = dt;
        }
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "unknown" ? null : value.Trim();

    /// <summary>Kompakte Anzeige, z. B. "v1.0.0 · a1b2c3d · 20.06.2026".</summary>
    public static string Display(CultureInfo culture)
    {
        var parts = new List<string> { $"v{Version}" };
        if (CommitHash is not null) parts.Add(CommitHash);
        if (CommitDate is { } d) parts.Add(d.ToLocalTime().ToString("dd.MM.yyyy", culture));
        return string.Join(" · ", parts);
    }

    /// <summary>Ausführlicher Tooltip-Text mit Uhrzeit.</summary>
    public static string Tooltip(CultureInfo culture)
    {
        if (CommitHash is null && CommitDate is null)
            return $"Version {Version}";
        var commit = CommitHash ?? "?";
        return CommitDate is { } d
            ? $"Commit {commit} vom {d.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)}"
            : $"Commit {commit}";
    }
}

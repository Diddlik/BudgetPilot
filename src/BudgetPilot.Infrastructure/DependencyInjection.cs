using BudgetPilot.Application.Abstractions;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using BudgetPilot.Infrastructure.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetPilot.Infrastructure;

/// <summary>
/// Registriert alle Infrastructure-Dienste: DbContext (Provider-abhängig),
/// Repositories, UnitOfWork und DatabaseSeeder.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registriert die Infrastructure-Schicht im DI-Container.
    /// <para>
    /// Konfiguration via <c>Database:Provider</c> (<c>Sqlite</c> | <c>Postgres</c>)
    /// und <c>Database:ConnectionString</c> in appsettings.json oder Umgebungsvariablen.
    /// </para>
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Provider-Switch (Spec §10) ──────────────────────────────────
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration["Database:ConnectionString"]
            ?? "Data Source=data/budgetpilot.db";

        services.AddDbContext<BudgetPilotDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(connectionString, postgres =>
                        postgres.MigrationsAssembly(typeof(BudgetPilotDbContext).Assembly.FullName));
                    break;

                case "sqlite":
                default:
                    // SQLite legt die Datei an, aber nicht das übergeordnete Verzeichnis.
                    EnsureSqliteDirectoryExists(connectionString);
                    options.UseSqlite(connectionString, sqlite =>
                        sqlite.MigrationsAssembly(typeof(BudgetPilotDbContext).Assembly.FullName));
                    break;
            }
        });

        // ── Repositories ────────────────────────────────────────────────
        services.AddScoped<IBudgetItemRepository, BudgetItemRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // ── Unit of Work ────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Änderungsprotokoll ──────────────────────────────────────────
        services.AddScoped<IAuditLog, Auditing.AuditLog>();

        // ── Seeder ──────────────────────────────────────────────────────
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    /// <summary>Stellt sicher, dass das Verzeichnis der SQLite-Datei existiert (Migrate/Create scheitert sonst).</summary>
    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
            return;

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}

using BudgetPilot.Application.Abstractions;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using BudgetPilot.Infrastructure.Seeding;
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

        // ── Seeder ──────────────────────────────────────────────────────
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

using BudgetPilot.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetPilot.Application.DependencyInjection;

/// <summary>
/// Registriert die Application-Services. Besitzer: Track A. <c>Program.cs</c> (Track C) ruft nur
/// <see cref="AddApplication"/> auf; die Repository-Implementierungen liefert <c>AddInfrastructure</c> (Track B).
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IBudgetProjectionService, BudgetProjectionService>();
        services.AddScoped<IBudgetItemService, BudgetItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IHouseholdSettingsService, HouseholdSettingsService>();
        services.AddScoped<IPlannedOccurrenceService, PlannedOccurrenceService>();
        services.AddScoped<IOccurrenceActionService, OccurrenceActionService>();
        services.AddScoped<IPlanActualComparisonService, PlanActualComparisonService>();
        services.AddScoped<IQuickExpenseService, QuickExpenseService>();
        return services;
    }
}

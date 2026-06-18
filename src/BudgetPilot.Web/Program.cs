using System.Globalization;
using System.Reflection;
using BudgetPilot.Application.DependencyInjection;
using BudgetPilot.Application.Services;
using BudgetPilot.Web.Components;
using BudgetPilot.Web.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

var culture = CultureInfo.GetCultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication();

if (!TryAddInfrastructure(builder.Services, builder.Configuration))
{
    // TEMP: Track B is not merged in this worktree yet, so AddInfrastructure(...) is not available.
    // TEMP: These Web-local fakes keep the Track C UI renderable and are removed in Wave 2.
    builder.Services.RemoveAll<IBudgetItemService>();
    builder.Services.RemoveAll<ICategoryService>();
    builder.Services.RemoveAll<IBudgetProjectionService>();
    builder.Services.AddSingleton<TemporaryBudgetStore>();
    builder.Services.AddScoped<IBudgetItemService, TemporaryBudgetItemService>();
    builder.Services.AddScoped<ICategoryService, TemporaryCategoryService>();
    builder.Services.AddScoped<IBudgetProjectionService, TemporaryBudgetProjectionService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool TryAddInfrastructure(IServiceCollection services, IConfiguration configuration)
{
    try
    {
        var assembly = Assembly.Load("BudgetPilot.Infrastructure");
        var method = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: true, IsSealed: true })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(method =>
            {
                if (method.Name != "AddInfrastructure")
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(IServiceCollection)
                    && parameters[1].ParameterType == typeof(IConfiguration);
            });

        if (method is null)
        {
            return false;
        }

        method.Invoke(null, [services, configuration]);
        return true;
    }
    catch
    {
        return false;
    }
}

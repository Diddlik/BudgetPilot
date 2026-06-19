using System.Globalization;
using BudgetPilot.Application.DependencyInjection;
using BudgetPilot.Infrastructure;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Seeding;
using BudgetPilot.Web.Components;
using BudgetPilot.Web.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var culture = CultureInfo.GetCultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Behind a TLS-terminating reverse proxy (Caddy): honour X-Forwarded-Proto/For so
// HTTPS redirects and Secure cookies work. Only the proxy on the internal Docker
// network talks to the app, so known proxies/networks are cleared.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Razor components (Interactive Server).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Authentication & Identity ────────────────────────────────────────────────
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<BudgetPilotDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddApiEndpoints(); // foundation for the future Android bearer-token API

// All routed pages require authentication unless they opt out with [AllowAnonymous].
builder.Services.AddAuthorization();

// Composition root: each layer registers itself via one extension method.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Aktueller Benutzer für das Änderungsprotokoll (liest den Blazor-Circuit-Auth-State).
builder.Services.AddScoped<BudgetPilot.Application.Abstractions.ICurrentUser, BudgetPilot.Web.Services.CurrentUser>();

var app = builder.Build();

// Apply migrations, seed demo data (Spec §12) and the single login account on startup.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<DatabaseSeeder>().SeedAsync();
    await SeedLoginUserAsync(sp, app.Configuration, app.Environment);
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Creates the single private user from Auth:Email / Auth:Password (env: Auth__Email / Auth__Password).
// Public registration stays disabled; in Development a default account is seeded for convenience.
static async Task SeedLoginUserAsync(IServiceProvider sp, IConfiguration config, IWebHostEnvironment env)
{
    var users = sp.GetRequiredService<UserManager<IdentityUser>>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Auth");

    if (await users.Users.AnyAsync())
    {
        return; // account already exists
    }

    var email = config["Auth:Email"] ?? (env.IsDevelopment() ? "admin@budgetpilot.local" : null);
    var password = config["Auth:Password"] ?? (env.IsDevelopment() ? "ChangeMe!2026" : null);

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogWarning("Kein Auth:Email/Auth:Password gesetzt – es wurde KEIN Login-Konto angelegt. " +
                          "Setze die Umgebungsvariablen Auth__Email und Auth__Password und starte neu.");
        return;
    }

    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
    var result = await users.CreateAsync(user, password);
    if (result.Succeeded)
    {
        logger.LogInformation("Login-Konto für {Email} angelegt.", email);
    }
    else
    {
        logger.LogError("Login-Konto konnte nicht angelegt werden: {Errors}",
            string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

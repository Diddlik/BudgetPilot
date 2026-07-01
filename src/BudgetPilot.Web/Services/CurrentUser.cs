using System.Security.Claims;
using BudgetPilot.Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace BudgetPilot.Web.Services;

/// <summary>
/// Liefert den im aktuellen Blazor-Circuit angemeldeten Benutzer für das Änderungsprotokoll.
/// Außerhalb eines angemeldeten Kontexts (z. B. Startup-Seeding) wird "System" gemeldet.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly AuthenticationStateProvider _auth;

    public CurrentUser(IHttpContextAccessor httpContext, AuthenticationStateProvider auth)
    {
        _httpContext = httpContext;
        _auth = auth;
    }

    public async Task<CurrentUserInfo> GetAsync(CancellationToken ct = default)
    {
        var requestUser = _httpContext.HttpContext?.User;
        if (requestUser?.Identity?.IsAuthenticated == true)
        {
            return FromPrincipal(requestUser);
        }

        var user = (await _auth.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated == true)
        {
            return FromPrincipal(user);
        }

        return CurrentUserInfo.System;
    }

    private static CurrentUserInfo FromPrincipal(ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? "Unbekannt";
        return new CurrentUserInfo(id, name);
    }
}

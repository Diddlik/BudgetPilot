using System.Security.Claims;
using BudgetPilot.Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace BudgetPilot.Web.Services;

/// <summary>
/// Liefert den im aktuellen Blazor-Circuit angemeldeten Benutzer für das Änderungsprotokoll.
/// Außerhalb eines angemeldeten Kontexts (z. B. Startup-Seeding) wird "System" gemeldet.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly AuthenticationStateProvider _auth;

    public CurrentUser(AuthenticationStateProvider auth) => _auth = auth;

    public async Task<CurrentUserInfo> GetAsync(CancellationToken ct = default)
    {
        var state = await _auth.GetAuthenticationStateAsync();
        var user = state.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = user.Identity.Name
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? "Unbekannt";
            return new CurrentUserInfo(id, name);
        }

        return CurrentUserInfo.System;
    }
}

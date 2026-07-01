using System.Security.Claims;
using BudgetPilot.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace BudgetPilot.Integration.Tests;

public sealed class CurrentUserTests
{
    [Fact]
    public async Task GetAsync_UsesAuthenticatedHttpRequestPrincipalWithoutBlazorCircuit()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "api-user-id"),
            new Claim(ClaimTypes.Email, "api@example.test")
        ], "Bearer", ClaimTypes.Email, ClaimTypes.Role));
        var context = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var blazorState = new ThrowingAuthenticationStateProvider();
        var currentUser = new CurrentUser(accessor, blazorState);

        var result = await currentUser.GetAsync();

        result.UserId.Should().Be("api-user-id");
        result.DisplayName.Should().Be("api@example.test");
        blazorState.WasCalled.Should().BeFalse();
    }

    private sealed class ThrowingAuthenticationStateProvider : AuthenticationStateProvider
    {
        public bool WasCalled { get; private set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            WasCalled = true;
            throw new InvalidOperationException("Für API-Requests existiert kein Blazor-Circuit.");
        }
    }
}

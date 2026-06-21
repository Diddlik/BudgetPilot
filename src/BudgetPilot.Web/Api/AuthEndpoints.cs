using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BudgetPilot.Web.Api;

/// <summary>
/// Bearer-Token-Endpoints für die Android-App: <c>/api/auth/login</c> und
/// <c>/api/auth/refresh</c>. Bewusst OHNE <c>/register</c> – Konten legt der Admin
/// im Web an (Single-/Privat-User-Modell). Die Logik ist aus <c>MapIdentityApi</c>
/// gespiegelt, aber auf den benötigten Umfang reduziert.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth").DisableAntiforgery();

        group.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>> (
            [FromBody] LoginRequest login,
            [FromServices] SignInManager<IdentityUser> signInManager) =>
        {
            // Token-Modus (kein Cookie): der Bearer-Handler schreibt bei Erfolg die
            // AccessTokenResponse (Access- + Refresh-Token) in den Response-Body.
            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

            var result = await signInManager.PasswordSignInAsync(
                login.Email, login.Password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
            }

            return TypedResults.Empty;
        });

        group.MapPost("/refresh", async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>> (
            [FromBody] RefreshRequest refreshRequest,
            [FromServices] SignInManager<IdentityUser> signInManager,
            [FromServices] IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
            [FromServices] TimeProvider timeProvider) =>
        {
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

            // Abgelaufen, ungültig oder Security-Stamp passt nicht mehr → erneuter Login nötig.
            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc
                || timeProvider.GetUtcNow() >= expiresUtc
                || await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not { } user)
            {
                return TypedResults.Challenge();
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        });

        return group;
    }
}

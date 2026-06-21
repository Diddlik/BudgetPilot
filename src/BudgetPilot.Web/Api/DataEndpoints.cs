using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BudgetPilot.Web.Api;

/// <summary>
/// Versionierte JSON-API (<c>/api/v1</c>) für die Android-App. Dünne Schicht über die
/// bestehenden Application-Services; gibt die vorhandenen DTOs zurück. Geschützt mit
/// dem Bearer-Schema (Web nutzt weiter Cookies). Fachliche <see cref="DomainException"/>
/// wird als RFC-7807 ProblemDetails (400) ausgegeben.
/// </summary>
public static class DataEndpoints
{
    public static IEndpointRouteBuilder MapDataApi(this IEndpointRouteBuilder routes)
    {
        var v1 = routes.MapGroup("/api/v1")
            .WithTags("BudgetPilot")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                .RequireAuthenticatedUser())
            .DisableAntiforgery();

        // Fachliche Validierungsfehler einheitlich als 400 ProblemDetails ausliefern.
        v1.AddEndpointFilter(async (ctx, next) =>
        {
            try
            {
                return await next(ctx);
            }
            catch (DomainException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        // ── Kategorien ──────────────────────────────────────────────────
        v1.MapGet("/categories", (ICategoryService s, CancellationToken ct) => s.GetAllAsync(ct));
        v1.MapPost("/categories", async (CreateCategoryRequest r, ICategoryService s, CancellationToken ct)
            => Results.Ok(await s.CreateAsync(r, ct)));
        v1.MapPut("/categories/{id:guid}", async (Guid id, RenameCategoryRequest r, ICategoryService s, CancellationToken ct) =>
        {
            await s.RenameAsync(id, r.Name, ct);
            return Results.NoContent();
        });
        v1.MapPost("/categories/{id:guid}/deactivate", async (Guid id, ICategoryService s, CancellationToken ct) =>
        {
            await s.DeactivateAsync(id, ct);
            return Results.NoContent();
        });

        // ── Budgetpositionen ────────────────────────────────────────────
        v1.MapGet("/budget-items", (IBudgetItemService s, CancellationToken ct) => s.GetAllAsync(ct));
        v1.MapGet("/budget-items/{id:guid}", async (Guid id, IBudgetItemService s, CancellationToken ct)
            => await s.GetByIdAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());
        v1.MapPost("/budget-items", async (CreateBudgetItemRequest r, IBudgetItemService s, CancellationToken ct)
            => Results.Ok(await s.CreateAsync(r, ct)));
        v1.MapPut("/budget-items/{id:guid}", async (Guid id, UpdateBudgetItemMetadataRequest r, IBudgetItemService s, CancellationToken ct)
            => Results.Ok(await s.UpdateMetadataAsync(id, r, ct)));
        v1.MapPost("/budget-items/{id:guid}/deactivate", async (Guid id, IBudgetItemService s, CancellationToken ct) =>
        {
            await s.DeactivateAsync(id, ct);
            return Results.NoContent();
        });
        v1.MapPost("/budget-items/{id:guid}/reactivate", async (Guid id, IBudgetItemService s, CancellationToken ct) =>
        {
            await s.ReactivateAsync(id, ct);
            return Results.NoContent();
        });
        v1.MapDelete("/budget-items/{id:guid}", async (Guid id, IBudgetItemService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // ── Versionen ───────────────────────────────────────────────────
        v1.MapPost("/budget-items/{id:guid}/versions", async (Guid id, CreateBudgetItemVersionRequest r, IBudgetItemService s, CancellationToken ct)
            => Results.Ok(await s.AddVersionAsync(id, r, ct)));
        v1.MapPut("/budget-items/{id:guid}/versions/current", async (Guid id, UpdateVersionRequest r, IBudgetItemService s, CancellationToken ct) =>
        {
            await s.UpdateCurrentVersionAsync(id, r, ct);
            return Results.NoContent();
        });
        v1.MapPut("/budget-items/{id:guid}/versions/{versionId:guid}", async (Guid id, Guid versionId, UpdateVersionRequest r, IBudgetItemService s, CancellationToken ct) =>
        {
            await s.UpdateVersionAsync(id, versionId, r, ct);
            return Results.NoContent();
        });

        // ── Projektionen ────────────────────────────────────────────────
        v1.MapGet("/projections/monthly", (int year, int month, BudgetViewMode mode, IBudgetProjectionService p, CancellationToken ct)
            => p.GetMonthlyProjectionAsync(year, month, mode, ct));
        v1.MapGet("/projections/yearly", (int year, BudgetViewMode mode, IBudgetProjectionService p, CancellationToken ct)
            => p.GetYearlyProjectionAsync(year, mode, ct));
        v1.MapGet("/projections/multi-year", (int from, int to, BudgetViewMode mode, IBudgetProjectionService p, CancellationToken ct)
            => p.GetMultiYearSummaryAsync(from, to, mode, ct));
        v1.MapGet("/budget-items/{id:guid}/schedule", (Guid id, int from, int to, IBudgetProjectionService p, CancellationToken ct)
            => p.GetPaymentScheduleAsync(id, from, to, ct));

        // ── Änderungsprotokoll ──────────────────────────────────────────
        v1.MapGet("/audit", (int? max, IAuditLog a, CancellationToken ct) => a.GetRecentAsync(max ?? 200, ct));

        return v1;
    }
}

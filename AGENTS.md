# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project state

**MVP is complete and integrated on `main`:** all layers are implemented and
merged — Domain rules + Application services/projection (Track A), EF Core/SQLite
+ migration + §12 seeding (Track B), Blazor UI for all screens (Track C). Full
build is green and **74 tests pass** (Domain, Application with fakes, plus
`BudgetPilot.Integration.Tests` exercising the real EF Core/SQLite/DateOnly-
converter/seeder/projection stack against the §12 values). The app boots,
migrates, seeds and serves locally **and** as a Docker container (`docker compose
up --build` verified: serves on :8080, DB in the mounted `./data` volume). PWA
scaffolding (manifest + icons) is in place; offline/service worker is deferred.
Specs are the source of truth: the original `BudgetPilot_Requirements.md` and the
binding implementation spec `Docs/BudgetPilot – Technical Specification/requirements.md`.
The parallel build plan is `Docs/IMPLEMENTATION_PLAN.md`. The UI theme follows the
prototype in `Docs/BudgetPilot – Technical Specification/BudgetApp.dc.html`
(Plus Jakarta Sans, accent `#C2410C`).

**Auth:** ASP.NET Core Identity (cookie) protects all routed pages —
`BudgetPilotDbContext` is now an `IdentityDbContext<IdentityUser>`; single private
user, public registration disabled, account seeded on startup from
`Auth:Email`/`Auth:Password` (Development seeds `admin@budgetpilot.local`/`ChangeMe!2026`).
Login/logout are static-SSR pages under `Components/Account` (cookie sign-in needs an
HTTP context, not a SignalR circuit); every other page gets `[Authorize]` via
`Components/_Imports.razor`, while account/error pages opt out with `[AllowAnonymous]`.
**JSON API (for the Android app):** a versioned REST API lives in
`src/BudgetPilot.Web/Api/` — `AuthEndpoints.cs` maps `/api/auth/login` + `/refresh`
(ASP.NET Identity **bearer tokens**, `IdentityConstants.BearerScheme`; deliberately
no `/register` — accounts are admin-created), and `DataEndpoints.cs` maps
`/api/v1/...` (categories, budget-items, versions, projections, audit) as a thin
layer over the existing Application services/DTOs, protected with the bearer scheme
(`RequireAuthorization`), antiforgery disabled, `DomainException` → RFC-7807
ProblemDetails (400) via an endpoint filter. Enums serialize as strings
(`ConfigureHttpJsonOptions`). Swagger/OpenAPI is served at `/swagger` in Development (the contract for mobile/API clients). The web UI keeps using cookies;
the API uses bearer. Android app requirements/plan: `Docs/ANDROID_APP_REQUIREMENTS.md`.
Internet exposure: Caddy reverse proxy (`docker-compose.prod.yml` + `Caddyfile`,
TLS via Let's Encrypt); the app honours `X-Forwarded-Proto` (`UseForwardedHeaders`).
Secrets live in `.env` (gitignored; see `.env.example`).

**Android app experiments (P1 scaffolds):** there are two deliberately separate
Android client tracks. `android/` is the Kotlin / Jetpack Compose Gradle project
(currently the Claude Code track). `androidNet/` is the .NET MAUI Blazor Hybrid
project (`BudgetPilot.Mobile.csproj`, currently the Codex track). Keep edits in the
assigned folder so the approaches do not collide. Both clients talk to the JSON API:
Setup (instance URL) → Login (bearer token) → Dashboard. The MAUI track also
includes P2 read paths for yearly/multi-year projections, budget-item and
category lists, plus the audit log. The MAUI track also implements online write
paths, an offline read cache, optional local PIN lock, mobile tests, and a
keystore-driven APK/AAB release script. The PIN lock can use AndroidX
`BiometricPrompt` (fingerprint/face) with PIN fallback; `/settings` manages
security, account/session, instance URL, default view/start page and offline
cache. The login accepts a hostname with an explicit HTTPS/HTTP selector; HTTPS
is the default and HTTP is marked as unencrypted. Release builds derive Android
`versionCode` from the Git commit count and `versionName` as `1.0.<count>`. It reuses
existing Application DTOs and Domain enums via ProjectReference; projection,
versioning and validation stay server-side. The MAUI project uses its own
`androidNet/global.json` and targets .NET 10 / Android API 36.1
(`net10.0-android36.1`) for Google Play compliance. Neither Android project is part of
`BudgetPilot.sln` or the .NET/Docker CI. Build the MAUI track from `androidNet/`:
`dotnet workload restore`, then `dotnet build BudgetPilot.Mobile.csproj -f
net10.0-android36.1`. Requirements/plan: `Docs/ANDROID_APP_REQUIREMENTS.md`.
The public privacy policy is deployed from `site/` via
`.github/workflows/pages.yml` to
`https://diddlik.github.io/BudgetPilot/privacy/` and linked from mobile settings.

**CI/CD & auto-update:** `.github/workflows/docker.yml` builds the image on every
push to `main` and publishes it to GHCR as `ghcr.io/diddlik/budgetpilot:latest`
(+ a `sha-<commit>` tag for rollback). The production server does **not** build —
it runs `docker-compose.deploy.yml`, which pulls the prebuilt image and runs a
**Watchtower** sidecar that polls GHCR every 5 min, backs up the SQLite db
(pre-update lifecycle hook), pulls the new `:latest` and recreates the container
(the `./data` volume persists). EF migrations run on startup, so updates are
schema-self-applying. The older "build on the server" path (`docker-compose.npm.yml`
/ `docker-compose.prod.yml` with `build: .` + `scripts/deploy.sh`) still works and
is the fallback. GHCR package visibility must be set to **public** once (anonymous
pull, no registry login on the server).

Code, identifiers, and enums are English; user-facing UI text and the specs are
German. Money is `decimal`, business dates are `DateOnly`.

BudgetPilot is a self-hosted household budgeting web app (.NET 8 / Blazor) that
projects recurring/one-off income and expenses into monthly and yearly views.

## This file is living documentation

AGENTS.md is a **living file**. Whenever something architecturally significant
changes — a new project/layer, a changed dependency direction, a new DB
provider, a revised projection or versioning rule, renamed core entities/enums,
or new build/test/run commands — update this file in the same change so it stays
accurate. Treat an out-of-date AGENTS.md as a bug.

## Stack & structure

Layered solution `BudgetPilot.sln` (classic format). Target `net8.0` for all
projects, pinned via `global.json` to .NET SDK 8.0.4xx. NuGet versions are
centralized in `Directory.Packages.props` (Central Package Management) — add
packages as `<PackageReference Include="X" />` **without** a `Version`, and put
the version in that file. Shared build props are in `Directory.Build.props`.

```
src/BudgetPilot.Web             Blazor Web App (Interactive Server); UI + Program.cs composition root
src/BudgetPilot.Application      Service interfaces + impls, DTOs, requests, repository interfaces, AddApplication()
src/BudgetPilot.Domain           Entities, enums, DomainException, domain rules (no dependencies)
src/BudgetPilot.Infrastructure   EF Core DbContext, repo impls, migrations, seeding, AddInfrastructure()
tests/BudgetPilot.Domain.Tests
tests/BudgetPilot.Application.Tests   xUnit + FluentAssertions
```

(There is no `BudgetPilot.Shared` — the binding spec §2 dropped it.)

Dependency direction: `Web → Application → Domain`; `Infrastructure →
Application + Domain`. **Never reference EF Core / `DbContext` from the UI, and
never put projection/calculation logic in components.** Services are abstracted
behind interfaces; UI/API talk in DTOs, never raw entities.

**DI wiring pattern (avoids merge conflicts across parallel tracks):** each layer
exposes one extension method — `AddApplication()` (Application) and
`AddInfrastructure(IConfiguration)` (Infrastructure). `Program.cs` calls both and
nothing else touches cross-layer registration.

Database: SQLite for MVP, PostgreSQL later. The provider must be configurable
via `Database:Provider` + `Database:ConnectionString` (spec §10), so
Infrastructure must not hardcode SQLite.

## Commands

```bash
dotnet build                                           # whole solution (green as of Wave 0)
dotnet run --project src/BudgetPilot.Web
dotnet test                                            # all tests
dotnet test tests/BudgetPilot.Domain.Tests             # one project
dotnet test --filter "FullyQualifiedName~Quarterly"    # one test / group
dotnet ef migrations add <Name> -p src/BudgetPilot.Infrastructure -s src/BudgetPilot.Web
dotnet ef database update -p src/BudgetPilot.Infrastructure -s src/BudgetPilot.Web
cd androidNet && dotnet workload restore && dotnet build BudgetPilot.Mobile.csproj -f net10.0-android36.1
dotnet test androidNet.Tests/BudgetPilot.Mobile.Tests.csproj -m:1
androidNet/start-screenshot-instance.ps1 -Reset       # isolated dummy DB on :8089 for screenshots
```

Docker (spec §11): container exposes port 8080; SQLite db lives in a mounted
`/app/data` volume.

## Core domain model (the heart of the app)

- **BudgetItem** — the logical position (e.g. "Miete", "Gehalt"). Metadata only:
  name, description, `Type` (Income/Expense), category, active flag.
- **BudgetItemVersion** — the financial facts that change over time: `Amount`,
  `Frequency`, `ValidFrom`/`ValidTo`, optional `PaymentDay`/`PaymentMonth`. A
  BudgetItem has **one or more** versions.
- **Category**, **ActualTransaction** (latter is prepared for future plan-vs-actual; no MVP UI required).
- Enums: `BudgetItemType {Income=1, Expense=2}`, `BudgetFrequency {Monthly=1,
  Quarterly=2, Yearly=3, Once=4}`, `BudgetViewMode {Budget=1, Cashflow=2}`.

Full entity definitions are in spec §9.

### Versioning — the single most important rule

Editing a running position must **never** change historical months. The default
(and only MVP) edit behavior is "create a new version from a date": the old
version gets a `ValidTo`, the new one a `ValidFrom`. Versions of the same item
**must not overlap** (§6, §8.3, §12.2). Adding an overlapping version must
either correctly close the previous version or be rejected.

A version is valid for month `M` when:
`ValidFrom <= end_of_month AND (ValidTo is null OR ValidTo >= start_of_month)`.
If two versions match the same month, that is a data error.

## Projection logic (must be deterministic & unit-tested)

Lives in Application behind `IBudgetProjectionService` (monthly + yearly,
parameterized by `BudgetViewMode`). The two view modes differ only for
non-monthly frequencies:

| Frequency | Budget view (spread)          | Cashflow view (actual due month)                                  |
|-----------|-------------------------------|-------------------------------------------------------------------|
| Monthly   | `Amount`                      | `Amount` (every valid month)                                      |
| Quarterly | `Amount / 3` every month      | full `Amount` only when months-since-`ValidFrom` is divisible by 3 |
| Yearly    | `Amount / 12` every month     | full `Amount` only in `PaymentMonth` (falls back to `ValidFrom` month) |
| Once      | `Amount` in `ValidFrom` month, else 0 | same as Budget                                            |

Detailed rules: spec §10–§11. Mandatory test scenarios (each frequency in both
views, versioning across a change date, once-only-in-its-month, no overlapping
versions) are enumerated in spec §17 — implement these as the test baseline.

## Conventions (from spec §18, §24)

- `decimal` for all money; `DateOnly` for business dates (`DateTime` only for audit timestamps like `CreatedAt`).
- Locale is German: currency EUR, dates `dd.MM.yyyy`, numbers `de-DE`.
- Validation (spec §12): amount ≥ 0; `ValidTo` not before `ValidFrom`;
  `PaymentDay` 1–31; `PaymentMonth` 1–12; no overlapping versions per item.
- Deactivating an item/category removes it from future planning but must keep historical data intact.
- Current app is private/single-household by default but uses ASP.NET Core Identity for Web cookies and API bearer tokens. Keep services small; build simple UI first.

# AGENT_INSTRUCTIONS.md

**Single point of truth (SPOT) for every coding agent working in this repository**
— Claude Code, Codex, and any other CLI/IDE agent. `CLAUDE.md` and `AGENTS.md`
are thin pointers to this file and must stay that way: never duplicate content
into them, always edit **this** file.

## This file is living documentation

AGENT_INSTRUCTIONS.md is a **living file**. Whenever something architecturally
significant changes — a new project/layer, a changed dependency direction, a new
DB provider, a revised projection or versioning rule, renamed core
entities/enums, a changed API contract, or new build/test/run commands — update
this file **in the same change** so it stays accurate. Treat an out-of-date
AGENT_INSTRUCTIONS.md as a bug.

Do **not** re-add project state to `CLAUDE.md` / `AGENTS.md`. If an agent
harness needs its own file, it gets a pointer only.

## Client parity — androidNet is not optional

`androidNet/` (.NET MAUI) references `BudgetPilot.Application` directly and
re-implements the editing UI. **Every change to the Application layer, the JSON
API contract, or an editing rule must be carried into `androidNet/` in the same
change** — otherwise the mobile client silently loses the feature or breaks.

Checklist when you touch domain/API behaviour:

| Change | Also update |
|---|---|
| new/changed field in `Application/Requests` or `Dtos` | `androidNet/Services/*FormModel.cs`, `androidNet/Components/**`, `androidNet/Services/BudgetItemInputValidator.cs` |
| new/changed `/api/v1/...` endpoint | `androidNet/Services/BudgetPilotApiClient.cs` |
| new/changed validation or versioning rule | mobile pre-validation + the hint texts in both UIs |
| new/changed build, test or release command | the Commands section below |

`androidNet` cannot be built in every environment (it needs the
`net10.0-android36.1` workload; a plain `dotnet build` fails with `NETSDK1139`).
If you cannot compile it, still make the change and **say explicitly that it is
unverified**.

`android/` (Kotlin) is a retained prototype and is no longer developed. Do not
mirror features or API changes into it. All Android product work continues only
in `androidNet/`.

## Project state

**MVP is complete and integrated on `main`:** all layers are implemented and
merged — Domain rules + Application services/projection (Track A), EF Core/SQLite
+ migration + §12 seeding (Track B), Blazor UI for all screens (Track C). Full
build is green and **76 tests pass** (23 Domain, 36 Application with fakes, 17
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
`BudgetPilotDbContext` is an `IdentityDbContext<IdentityUser>`; single private
user, public registration disabled, account seeded on startup from
`Auth:Email`/`Auth:Password` (Development seeds `admin@budgetpilot.local`/`ChangeMe!2026`).
Login/logout are static-SSR pages under `Components/Account` (cookie sign-in needs an
HTTP context, not a SignalR circuit); every other page gets `[Authorize]` via
`Components/_Imports.razor`, while account/error pages opt out with `[AllowAnonymous]`.
Docker deployments persist ASP.NET Core Data Protection keys under
`/app/data/keys`, so cookie and bearer refresh tokens remain valid across container
updates. An expired or otherwise invalid refresh token returns `401` without a
cookie-login redirect; mobile clients clear the session and request a new login.

**JSON API (for the mobile clients):** a versioned REST API lives in
`src/BudgetPilot.Web/Api/` — `AuthEndpoints.cs` maps `/api/auth/login` + `/refresh`
(ASP.NET Identity **bearer tokens**, `IdentityConstants.BearerScheme`; deliberately
no `/register` — accounts are admin-created), and `DataEndpoints.cs` maps
`/api/v1/...` (categories, budget-items, versions, projections, open/resolve occurrences, audit, household settings) as a thin
layer over the existing Application services/DTOs, protected with the bearer scheme
(`RequireAuthorization`), antiforgery disabled, `DomainException` → RFC-7807
ProblemDetails (400) via an endpoint filter. Enums serialize as strings
(`ConfigureHttpJsonOptions`). Swagger/OpenAPI is served at `/swagger` in
Development (the contract for mobile/API clients). The web UI keeps using cookies;
the API uses bearer.
Internet exposure: Caddy reverse proxy (`docker-compose.prod.yml` + `Caddyfile`,
TLS via Let's Encrypt); the app honours `X-Forwarded-Proto` (`UseForwardedHeaders`).
Secrets live in `.env` (gitignored; see `.env.example`).

**Android client:** only `androidNet/` is continued as the product client. It is
the .NET MAUI Blazor Hybrid project (`BudgetPilot.Mobile.csproj`) and talks to the
JSON API: Setup (instance URL) → Login (bearer token) → Dashboard. It has P2 read paths for
yearly/multi-year projections, budget-item and category lists, the audit log,
online write paths, an offline read cache, optional local PIN lock (AndroidX
`BiometricPrompt` with PIN fallback), mobile tests, and a keystore-driven
APK/AAB release script. `/settings` manages security, account/session, instance
URL, default view/start page, the shared household time zone and offline cache. The login accepts a hostname with
an explicit HTTPS/HTTP selector; HTTPS is the default and HTTP is marked as
unencrypted. Release builds derive Android `versionCode` from the Git commit count
and `versionName` as `1.0.<count>`. It reuses existing Application DTOs and Domain
enums via ProjectReference; projection, versioning and validation stay
server-side. The MAUI project uses its own `androidNet/global.json` and targets
.NET 10 / Android API 36.1 (`net10.0-android36.1`) for Google Play compliance.
`android/` remains in the repository only as an unmaintained Kotlin/Compose
prototype and is not a parity target. Neither Android project is part of
`BudgetPilot.sln` or the .NET/Docker CI.
Requirements/plan: `Docs/ANDROID_APP_REQUIREMENTS.md`. The public privacy policy
is deployed from `site/` via `.github/workflows/pages.yml` to
`https://diddlik.github.io/BudgetPilot/privacy/` and linked from mobile settings.

**Play Store status:** `Budget Pilot` / `de.budgetpilot.mobile` is live in
**closed testing (Alpha)** with `1.0.40` / versionCode 40; production is
inactive and still blocked on the 12-testers-for-14-days requirement. Releases
are built with `androidNet/build-release.ps1`, which needs the four
`BUDGETPILOT_*` keystore environment variables (never committed) and derives the
version from the Git commit count. Uploading and rolling out is a manual Play
Console step — never publish on the user's behalf without explicit confirmation.
Keep `Docs/GooglePlay/PLAY_CONSOLE_CHECKLIST.md` in sync with the console.

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
German. Money is `decimal`, business dates are `DateOnly`. Calendar decisions
use `IHouseholdSettingsService`, never the host/device-local `DateTime.Today`.
The singleton `HouseholdSettings` row stores a portable IANA time-zone id
(`Europe/Berlin` by default); UTC remains mandatory for audit/creation timestamps.

BudgetPilot is a self-hosted household budgeting web app (.NET 8 / Blazor) that
projects recurring/one-off income and expenses into monthly and yearly views.

## Stack & structure

Layered solution `BudgetPilot.sln` (classic format). Target `net8.0` for all
projects, pinned via `global.json` to .NET SDK 8.0.4xx. NuGet versions are
centralized in `Directory.Packages.props` (Central Package Management) — add
packages as `<PackageReference Include="X" />` **without** a `Version`, and put
the version in that file. Shared build props are in `Directory.Build.props`.

```
src/BudgetPilot.Web              Blazor Web App (Interactive Server); UI + Api/ + Program.cs composition root
src/BudgetPilot.Application      Service interfaces + impls, DTOs, requests, repository interfaces, AddApplication()
src/BudgetPilot.Domain           Entities, enums, DomainException, domain rules (no dependencies)
src/BudgetPilot.Infrastructure   EF Core DbContext, repo impls, migrations, seeding, AddInfrastructure()
tests/BudgetPilot.Domain.Tests
tests/BudgetPilot.Application.Tests      xUnit + FluentAssertions
tests/BudgetPilot.Integration.Tests      real EF Core/SQLite stack
android/                         archived Kotlin/Compose prototype; no further feature work
androidNet/                      .NET MAUI Blazor Hybrid client (own global.json, not in the sln)
androidNet.Tests/                tests for the MAUI client
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
dotnet build                                           # whole solution (green)
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
edit behavior is "create a new version from a date": the old version gets a
`ValidTo`, the new one a `ValidFrom`. Versions of the same item **must not
overlap** (§6, §8.3, §12.2). Adding an overlapping version must either correctly
close the previous version or be rejected.

A version is valid for month `M` when:
`ValidFrom <= end_of_month AND (ValidTo is null OR ValidTo >= start_of_month)`.
If two versions match the same month, that is a data error.

**Correcting a version** (`UpdateVersionAsync` / `UpdateCurrentVersionAsync`, both
routed through the private `ApplyVersionUpdate` in `BudgetItemService`) may change
**both** `ValidFrom` and `ValidTo`. The corrected period wins and the direct
neighbours are shifted automatically: the predecessor ends the month before, the
successor starts the month after. `ValidTo == null` in `UpdateVersionRequest`
means "open" and is clamped to the successor's previous month. Boundaries are
always month-aligned, and `BudgetValidation.EnsureNoMonthOverlap` verifies the
whole chain afterwards — no month may belong to two versions, because the
projection is monthly.

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
- Resolve "today" through `IHouseholdSettingsService`; web and androidNet must use the server-provided household date.
- Creation forms expose only name, type, category, amount, frequency and first due date by default. Keep owner, notes and payment overrides progressive; build the three-date preview through `PaymentSchedulePreview`, not inside UI components.
- Recurring rules remain versioned `BudgetItemVersion` templates. `PlannedOccurrenceService` derives every due instance from the first valid version through the requested horizon, so unopened months remain visible. `OccurrenceResolution` persists one unique terminal state (`Confirmed` or `Skipped`) per `OccurrenceKey` (`{BudgetItemId:N}:{yyyy-MM-dd}`); confirmed instances additionally create an `ActualTransaction` with the possibly adjusted date and amount. Never persist future planned rows eagerly, and never alter the plan when resolving an occurrence.
- Corrections update only the linked `ActualTransaction`; reopening removes both the `OccurrenceResolution` and its optional transaction so the derived occurrence becomes open again. Confirm, skip, correct and reopen actions must be recorded through `IAuditLog`.
- Plan/actual comparisons use cashflow projections for the selected calendar period and actual transaction dates for reality. Expense variance is `actual - planned`; categories are the union of planned and actually booked expense categories. Keep this read-only contract consistent across Web and androidNet.
- Quick expenses use the client-generated `RequestId` as `ActualTransaction.Id`; retries must reuse it so double taps and uncertain network responses stay idempotent. Omit the date unless the user explicitly chose one, so the server resolves the current household date at write time. An optional budget-item assignment must reference an active expense item in the selected category. Keep amount and category as the only required UI inputs across Web and androidNet.
- Locale is German: currency EUR, dates `dd.MM.yyyy`, numbers `de-DE`.
- Validation (spec §12): amount ≥ 0; `ValidTo` not before `ValidFrom`;
  `PaymentDay` 1–31; `PaymentMonth` 1–12; no overlapping versions per item.
- Deactivating an item/category removes it from future planning but must keep historical data intact. `BudgetItem.DeactivatedOn` is the household-local cutoff: unresolved occurrences through that date remain visible; later ones are excluded. Reactivation clears the cutoff.
- The app is private/single-household by default but uses ASP.NET Core Identity
  for web cookies and API bearer tokens. Keep services small; build simple UI first.

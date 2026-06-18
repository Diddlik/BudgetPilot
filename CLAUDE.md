# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

**Wave 0 (foundation) is done:** the layered solution `BudgetPilot.sln` exists,
the `Domain` layer is complete, and the `Application` **contracts are frozen**
(service interfaces, DTOs, requests, repository interfaces, `AddApplication()`).
Service/repository bodies are `NotImplementedException` stubs filled by parallel
tracks. The build is green. Specs are the source of truth: the original
`BudgetPilot_Requirements.md` and the binding implementation spec
`Docs/BudgetPilot – Technical Specification/requirements.md`. The parallel build
plan is `Docs/IMPLEMENTATION_PLAN.md`.

Code, identifiers, and enums are English; user-facing UI text and the specs are
German. Money is `decimal`, business dates are `DateOnly`.

BudgetPilot is a self-hosted household budgeting web app (.NET 8 / Blazor) that
projects recurring/one-off income and expenses into monthly and yearly views.

## This file is living documentation

CLAUDE.md is a **living file**. Whenever something architecturally significant
changes — a new project/layer, a changed dependency direction, a new DB
provider, a revised projection or versioning rule, renamed core entities/enums,
or new build/test/run commands — update this file in the same change so it stays
accurate. Treat an out-of-date CLAUDE.md as a bug.

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
- MVP is single-user, single-budget, no auth. Keep services small; build simple UI first.

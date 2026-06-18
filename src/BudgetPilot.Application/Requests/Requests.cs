using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Requests;

/// <summary>Legt eine neue Position samt erster Version an (§6.7 Create-Modus).</summary>
public record CreateBudgetItemRequest(
    string Name,
    string? Description,
    BudgetItemType Type,
    Guid CategoryId,
    decimal Amount,
    BudgetFrequency Frequency,
    DateOnly ValidFrom,
    int? PaymentDay,
    int? PaymentMonth,
    string? Note,
    string? Owner = null);

/// <summary>Ändert nur die Stammdaten einer Position (Edit-Modus, ohne Versionierung).</summary>
public record UpdateBudgetItemMetadataRequest(
    string Name,
    string? Description,
    BudgetItemType Type,
    Guid CategoryId,
    string? Owner = null);

/// <summary>Hängt eine neue Version ab Stichtag an und beendet die bisher offene Version (§4.3).</summary>
public record CreateBudgetItemVersionRequest(
    decimal Amount,
    BudgetFrequency Frequency,
    DateOnly ValidFrom,
    int? PaymentDay,
    int? PaymentMonth,
    string? Note);

/// <summary>Aktualisiert die aktuelle (jüngste, offene) Version in-place (Korrektur ohne neue Version).</summary>
public record UpdateVersionRequest(
    decimal Amount,
    BudgetFrequency Frequency,
    DateOnly ValidFrom,
    int? PaymentDay,
    int? PaymentMonth,
    string? Note);

/// <summary>Legt eine neue Kategorie an.</summary>
public record CreateCategoryRequest(string Name);

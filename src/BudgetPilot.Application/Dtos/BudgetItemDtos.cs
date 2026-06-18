using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

/// <summary>Stammdaten einer Budgetposition inkl. aller Versionen (für Liste & Detail).</summary>
public class BudgetItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BudgetItemType Type { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Alle Versionen, absteigend nach <see cref="BudgetItemVersionDto.ValidFrom"/> (neueste zuerst).</summary>
    public List<BudgetItemVersionDto> Versions { get; set; } = new();
}

/// <summary>Eine zeitlich gültige Version einer Budgetposition.</summary>
public class BudgetItemVersionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int? PaymentDay { get; set; }
    public int? PaymentMonth { get; set; }
    public string? Note { get; set; }

    /// <summary>True für die aktuell offene (jüngste) Version.</summary>
    public bool IsCurrent { get; set; }
}

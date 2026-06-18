using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Entities;

namespace BudgetPilot.Application.Services.Mapping;

/// <summary>Mappt Domain-Entities auf DTOs. Entities werden nie direkt an die UI gereicht (§9).</summary>
internal static class DtoMapper
{
    /// <summary>
    /// Bestimmt die aktuelle Version eines Items: die jüngste offene (ValidTo == null) Version;
    /// fällt zurück auf die Version mit dem spätesten ValidFrom.
    /// </summary>
    public static BudgetItemVersion? CurrentVersion(BudgetItem item)
    {
        if (item.Versions.Count == 0)
            return null;

        return item.Versions
            .OrderByDescending(v => v.ValidTo is null)   // offene zuerst
            .ThenByDescending(v => v.ValidFrom)
            .First();
    }

    public static BudgetItemDto ToDto(BudgetItem item)
    {
        var current = CurrentVersion(item);

        return new BudgetItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Type = item.Type,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Versions = item.Versions
                .OrderByDescending(v => v.ValidFrom)   // neueste zuerst
                .ThenByDescending(v => v.CreatedAt)
                .Select(v => ToDto(v, isCurrent: current is not null && v.Id == current.Id))
                .ToList()
        };
    }

    public static BudgetItemVersionDto ToDto(BudgetItemVersion v, bool isCurrent) => new()
    {
        Id = v.Id,
        Amount = v.Amount,
        Frequency = v.Frequency,
        ValidFrom = v.ValidFrom,
        ValidTo = v.ValidTo,
        PaymentDay = v.PaymentDay,
        PaymentMonth = v.PaymentMonth,
        Note = v.Note,
        IsCurrent = isCurrent
    };

    public static CategoryDto ToDto(Category category, int itemCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
        ItemCount = itemCount
    };
}

using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetPilot.Infrastructure.Data.Configurations;

/// <summary>
/// EF-Konfiguration für <see cref="BudgetItemVersion"/> gemäß Spec §3.3.
/// DateOnly → string (ISO yyyy-MM-dd) ValueConverter für SQLite-Kompatibilität.
/// </summary>
public class BudgetItemVersionConfiguration : IEntityTypeConfiguration<BudgetItemVersion>
{
    public void Configure(EntityTypeBuilder<BudgetItemVersion> builder)
    {
        builder.HasKey(v => v.Id);

        // Money precision (18,2)
        builder.Property(v => v.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        // Required fields
        builder.Property(v => v.Frequency)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(v => v.ValidFrom)
            .IsRequired();

        // DateOnly → string ValueConverter for SQLite (ISO yyyy-MM-dd)
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.ParseExact(s, "yyyy-MM-dd"));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, string?>(
            d => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : null,
            s => s != null ? DateOnly.ParseExact(s, "yyyy-MM-dd") : null);

        builder.Property(v => v.ValidFrom)
            .HasConversion(dateOnlyConverter);

        builder.Property(v => v.ValidTo)
            .HasConversion(nullableDateOnlyConverter);

        // Composite index: (BudgetItemId, ValidFrom)
        builder.HasIndex(v => new { v.BudgetItemId, v.ValidFrom });

        // Relationship configured in BudgetItemConfiguration (the parent side)
    }
}

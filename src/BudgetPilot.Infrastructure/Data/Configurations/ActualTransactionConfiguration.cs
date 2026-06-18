using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetPilot.Infrastructure.Data.Configurations;

/// <summary>
/// EF-Konfiguration für <see cref="ActualTransaction"/> gemäß Spec §3.3.
/// Prepared for future plan-vs-actual; no MVP UI required.
/// </summary>
public class ActualTransactionConfiguration : IEntityTypeConfiguration<ActualTransaction>
{
    public void Configure(EntityTypeBuilder<ActualTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        // Money precision (18,2)
        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        // Enum as integer
        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<int>();

        // DateOnly → string ValueConverter for SQLite
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.ParseExact(s, "yyyy-MM-dd"));

        builder.Property(t => t.Date)
            .IsRequired()
            .HasConversion(dateOnlyConverter);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        // Index on Date
        builder.HasIndex(t => t.Date);

        // Relationship: ActualTransaction → Category
        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: ActualTransaction → BudgetItem (optional)
        builder.HasOne(t => t.BudgetItem)
            .WithMany()
            .HasForeignKey(t => t.BudgetItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

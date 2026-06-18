using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetPilot.Infrastructure.Data.Configurations;

/// <summary>
/// EF-Konfiguration für <see cref="BudgetItem"/> gemäß Spec §3.3.
/// </summary>
public class BudgetItemConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.HasKey(b => b.Id);

        // Required fields
        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Type)
            .IsRequired();

        builder.Property(b => b.CategoryId)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.Property(b => b.Owner)
            .HasMaxLength(100);

        // Relationship: Category 1→* BudgetItem
        // Category darf NICHT gelöscht werden solange Items existieren (Restrict)
        builder.HasOne(b => b.Category)
            .WithMany(c => c.BudgetItems)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: BudgetItem 1→* BudgetItemVersion (cascade delete)
        builder.HasMany(b => b.Versions)
            .WithOne(v => v.BudgetItem)
            .HasForeignKey(v => v.BudgetItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on CategoryId
        builder.HasIndex(b => b.CategoryId);

        // Enum as integer
        builder.Property(b => b.Type)
            .HasConversion<int>();
    }
}

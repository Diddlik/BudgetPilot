using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetPilot.Infrastructure.Data.Configurations;

/// <summary>
/// EF-Konfiguration für <see cref="Category"/> gemäß Spec §3.3.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        // Required fields
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Relationship: Category 1→* BudgetItem (configured on the BudgetItem side)
        // Restrict delete is configured in BudgetItemConfiguration
    }
}

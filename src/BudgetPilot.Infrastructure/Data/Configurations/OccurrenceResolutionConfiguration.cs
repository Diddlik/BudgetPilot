using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetPilot.Infrastructure.Data.Configurations;

public sealed class OccurrenceResolutionConfiguration : IEntityTypeConfiguration<OccurrenceResolution>
{
    public void Configure(EntityTypeBuilder<OccurrenceResolution> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OccurrenceKey).IsRequired().HasMaxLength(80);
        builder.Property(x => x.BudgetItemName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PlannedAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.OccurrenceKey).IsUnique();
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.PlannedDueDate).HasConversion(new ValueConverter<DateOnly, string>(
            date => date.ToString("yyyy-MM-dd"),
            value => DateOnly.ParseExact(value, "yyyy-MM-dd")));
        builder.HasOne(x => x.ActualTransaction)
            .WithOne()
            .HasForeignKey<OccurrenceResolution>(x => x.ActualTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

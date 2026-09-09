using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetPilot.Infrastructure.Data.Configurations;

public sealed class HouseholdSettingsConfiguration : IEntityTypeConfiguration<HouseholdSettings>
{
    public void Configure(EntityTypeBuilder<HouseholdSettings> builder)
    {
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.TimeZoneId).IsRequired().HasMaxLength(100);
        builder.Property(settings => settings.OccurrenceTrackingStartDate)
            .HasConversion(
                date => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null,
                value => value == null ? null : DateOnly.ParseExact(value, "yyyy-MM-dd"));
        builder.HasData(new HouseholdSettings());
    }
}

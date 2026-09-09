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
        builder.HasData(new HouseholdSettings());
    }
}

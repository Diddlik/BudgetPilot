using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetPilot.Infrastructure.Data.Configurations;

/// <summary>EF-Konfiguration für das Änderungsprotokoll <see cref="AuditEntry"/>.</summary>
public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(64);
        builder.Property(e => e.EntityName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Details).HasMaxLength(1000);

        // Anzeige erfolgt neueste zuerst – Index auf den Zeitstempel.
        builder.HasIndex(e => e.TimestampUtc);
    }
}

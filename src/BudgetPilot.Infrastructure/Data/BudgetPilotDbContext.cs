using BudgetPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Data;

/// <summary>
/// EF Core DbContext für BudgetPilot. Konfiguriert alle Entitäten via
/// IEntityTypeConfiguration-Klassen im Configurations-Ordner.
/// </summary>
public class BudgetPilotDbContext : DbContext
{
    public BudgetPilotDbContext(DbContextOptions<BudgetPilotDbContext> options)
        : base(options)
    {
    }

    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();
    public DbSet<BudgetItemVersion> BudgetItemVersions => Set<BudgetItemVersion>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ActualTransaction> ActualTransactions => Set<ActualTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetPilotDbContext).Assembly);
    }
}

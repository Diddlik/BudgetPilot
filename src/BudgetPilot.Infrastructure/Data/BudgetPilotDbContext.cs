using BudgetPilot.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Data;

/// <summary>
/// EF Core DbContext für BudgetPilot. Enthält die Domänentabellen sowie die ASP.NET Core
/// Identity-Tabellen (gemeinsamer User-Store für Web-Login und spätere Token-API).
/// </summary>
public class BudgetPilotDbContext : IdentityDbContext<IdentityUser>
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BudgetPilot.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tooling (dotnet ef migrations).
/// Used when the startup project doesn't register the DbContext via DI
/// (e.g. before Program.cs calls AddInfrastructure).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetPilotDbContext>
{
    public BudgetPilotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BudgetPilotDbContext>();
        optionsBuilder.UseSqlite("Data Source=data/budgetpilot.db", sqlite =>
            sqlite.MigrationsAssembly(typeof(BudgetPilotDbContext).Assembly.FullName));

        return new BudgetPilotDbContext(optionsBuilder.Options);
    }
}

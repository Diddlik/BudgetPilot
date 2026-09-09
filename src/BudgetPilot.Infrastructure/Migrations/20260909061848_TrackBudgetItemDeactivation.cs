using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrackBudgetItemDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeactivatedOn",
                table: "BudgetItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE BudgetItems
                SET DeactivatedOn = substr(COALESCE(UpdatedAt, CreatedAt), 1, 10)
                WHERE IsActive = 0 AND DeactivatedOn IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedOn",
                table: "BudgetItems");
        }
    }
}

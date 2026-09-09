using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrenceTrackingStartDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OccurrenceTrackingStartDate",
                table: "HouseholdSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE HouseholdSettings
                SET OccurrenceTrackingStartDate = strftime('%Y-%m-01', 'now')
                WHERE OccurrenceTrackingStartDate IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccurrenceTrackingStartDate",
                table: "HouseholdSettings");
        }
    }
}

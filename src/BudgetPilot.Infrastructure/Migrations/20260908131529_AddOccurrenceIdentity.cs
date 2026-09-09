using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrenceIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OccurrenceKey",
                table: "ActualTransactions",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannedDueDate",
                table: "ActualTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualTransactions_OccurrenceKey",
                table: "ActualTransactions",
                column: "OccurrenceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActualTransactions_OccurrenceKey",
                table: "ActualTransactions");

            migrationBuilder.DropColumn(
                name: "OccurrenceKey",
                table: "ActualTransactions");

            migrationBuilder.DropColumn(
                name: "PlannedDueDate",
                table: "ActualTransactions");
        }
    }
}

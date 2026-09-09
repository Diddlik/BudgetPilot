using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrenceResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OccurrenceResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    BudgetItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannedDueDate = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ActualTransactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccurrenceResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OccurrenceResolutions_ActualTransactions_ActualTransactionId",
                        column: x => x.ActualTransactionId,
                        principalTable: "ActualTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OccurrenceResolutions_ActualTransactionId",
                table: "OccurrenceResolutions",
                column: "ActualTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OccurrenceResolutions_OccurrenceKey",
                table: "OccurrenceResolutions",
                column: "OccurrenceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OccurrenceResolutions");
        }
    }
}

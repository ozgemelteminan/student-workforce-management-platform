using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWorkforceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTaskOccurrenceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringTaskOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTaskOccurrences", x => x.Id);
                    table.CheckConstraint("CK_RecurringTaskOccurrences_Attempts", "\"Attempts\" >= 0");
                    table.ForeignKey(
                        name: "FK_RecurringTaskOccurrences_RecurringTasks_RecurringTaskId",
                        column: x => x.RecurringTaskId,
                        principalTable: "RecurringTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTaskOccurrences_Tasks_GeneratedTaskId",
                        column: x => x.GeneratedTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTaskOccurrences_CreatedAt",
                table: "RecurringTaskOccurrences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTaskOccurrences_GeneratedTaskId",
                table: "RecurringTaskOccurrences",
                column: "GeneratedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTaskOccurrences_RecurringTaskId_ScheduledRunAt",
                table: "RecurringTaskOccurrences",
                columns: new[] { "RecurringTaskId", "ScheduledRunAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTaskOccurrences_Status",
                table: "RecurringTaskOccurrences",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringTaskOccurrences");
        }
    }
}

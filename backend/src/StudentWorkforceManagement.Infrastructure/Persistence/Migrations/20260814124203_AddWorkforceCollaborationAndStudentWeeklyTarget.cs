using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWorkforceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkforceCollaborationAndStudentWeeklyTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskAssignmentHistory_TaskId",
                table: "TaskAssignmentHistory");

            migrationBuilder.AddColumn<int>(
                name: "PlannedEffortMinutes",
                table: "TaskAssignmentHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyTargetMinutes",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedStartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedEndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Agenda = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.CheckConstraint("CK_Meetings_ConfirmedRange", "\"ConfirmedEndAt\" IS NULL OR \"ConfirmedStartAt\" IS NULL OR \"ConfirmedEndAt\" > \"ConfirmedStartAt\"");
                    table.ForeignKey(
                        name: "FK_Meetings_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskNudges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderStudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientStudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskNudges_Students_RecipientStudentId",
                        column: x => x.RecipientStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskNudges_Students_SenderStudentId",
                        column: x => x.SenderStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskNudges_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemporaryUnavailability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryUnavailability", x => x.Id);
                    table.CheckConstraint("CK_TemporaryUnavailability_Range", "\"EndAt\" > \"StartAt\"");
                    table.ForeignKey(
                        name: "FK_TemporaryUnavailability_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetWeeks", x => x.Id);
                    table.CheckConstraint("CK_TimesheetWeeks_TargetMinutes", "\"TargetMinutes\" >= 0");
                    table.ForeignKey(
                        name: "FK_TimesheetWeeks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetWeeks_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssignedStudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Students_AssignedStudentId",
                        column: x => x.AssignedStudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingActionItems_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampusPresence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AvailableRangesJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetWeekId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Minutes = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetEntries", x => x.Id);
                    table.CheckConstraint("CK_TimesheetEntries_Minutes", "\"Minutes\" > 0");
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_TimesheetWeeks_TimesheetWeekId",
                        column: x => x.TimesheetWeekId,
                        principalTable: "TimesheetWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignmentHistory_TaskId",
                table: "TaskAssignmentHistory",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignmentHistory_TaskId_StudentId",
                table: "TaskAssignmentHistory",
                columns: new[] { "TaskId", "StudentId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaskAssignmentHistory_PlannedEffortMinutes",
                table: "TaskAssignmentHistory",
                sql: "\"PlannedEffortMinutes\" IS NULL OR \"PlannedEffortMinutes\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Students_WeeklyTargetMinutes",
                table: "Students",
                sql: "\"WeeklyTargetMinutes\" IS NULL OR \"WeeklyTargetMinutes\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_AssignedStudentId",
                table: "MeetingActionItems",
                column: "AssignedStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_CreatedAt",
                table: "MeetingActionItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_MeetingId",
                table: "MeetingActionItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingActionItems_TaskId",
                table: "MeetingActionItems",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_CreatedAt",
                table: "MeetingParticipants",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_MeetingId_StudentId",
                table: "MeetingParticipants",
                columns: new[] { "MeetingId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_StudentId",
                table: "MeetingParticipants",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_CreatedAt",
                table: "Meetings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_CreatedByUserId",
                table: "Meetings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_ResponseDeadline",
                table: "Meetings",
                column: "ResponseDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_Status",
                table: "Meetings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNudges_CreatedAt",
                table: "TaskNudges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNudges_RecipientStudentId",
                table: "TaskNudges",
                column: "RecipientStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNudges_SenderStudentId",
                table: "TaskNudges",
                column: "SenderStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNudges_TaskId_SenderStudentId_RecipientStudentId_SentAt",
                table: "TaskNudges",
                columns: new[] { "TaskId", "SenderStudentId", "RecipientStudentId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryUnavailability_CreatedAt",
                table: "TemporaryUnavailability",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryUnavailability_StudentId_StartAt_EndAt",
                table: "TemporaryUnavailability",
                columns: new[] { "StudentId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_CreatedAt",
                table: "TimesheetEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TaskId",
                table: "TimesheetEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetWeekId",
                table: "TimesheetEntries",
                column: "TimesheetWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_CreatedAt",
                table: "TimesheetWeeks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_ReviewedByUserId",
                table: "TimesheetWeeks",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_Status",
                table: "TimesheetWeeks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_StudentId_WeekStartDate",
                table: "TimesheetWeeks",
                columns: new[] { "StudentId", "WeekStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingActionItems");

            migrationBuilder.DropTable(
                name: "MeetingParticipants");

            migrationBuilder.DropTable(
                name: "TaskNudges");

            migrationBuilder.DropTable(
                name: "TemporaryUnavailability");

            migrationBuilder.DropTable(
                name: "TimesheetEntries");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropTable(
                name: "TimesheetWeeks");

            migrationBuilder.DropIndex(
                name: "IX_TaskAssignmentHistory_TaskId",
                table: "TaskAssignmentHistory");

            migrationBuilder.DropIndex(
                name: "IX_TaskAssignmentHistory_TaskId_StudentId",
                table: "TaskAssignmentHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaskAssignmentHistory_PlannedEffortMinutes",
                table: "TaskAssignmentHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Students_WeeklyTargetMinutes",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PlannedEffortMinutes",
                table: "TaskAssignmentHistory");

            migrationBuilder.DropColumn(
                name: "WeeklyTargetMinutes",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignmentHistory_TaskId",
                table: "TaskAssignmentHistory",
                column: "TaskId",
                unique: true,
                filter: "\"IsActive\" = true");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWorkforceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportRequestLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ArtifactStorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ArtifactFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ArtifactFileSize = table.Column<long>(type: "bigint", nullable: true),
                    ArtifactMimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ArtifactContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportRequests", x => x.Id);
                    table.CheckConstraint("CK_ExportRequests_ArtifactFileSize", "\"ArtifactFileSize\" IS NULL OR \"ArtifactFileSize\" >= 0");
                    table.ForeignKey(
                        name: "FK_ExportRequests_Users_RequestingUserId",
                        column: x => x.RequestingUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_AuthorizedUserId",
                table: "ExportRequests",
                column: "AuthorizedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_CreatedAt",
                table: "ExportRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_ExpiresAt",
                table: "ExportRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_IdempotencyKey",
                table: "ExportRequests",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_RequestedAt",
                table: "ExportRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_RequestingUserId",
                table: "ExportRequests",
                column: "RequestingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_Status",
                table: "ExportRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportRequests");
        }
    }
}

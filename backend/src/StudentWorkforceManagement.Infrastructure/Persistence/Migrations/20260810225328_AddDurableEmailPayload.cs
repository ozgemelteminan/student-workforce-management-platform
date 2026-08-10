using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWorkforceManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableEmailPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "EmailDeliveries",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateDataJson",
                table: "EmailDeliveries",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subject",
                table: "EmailDeliveries");

            migrationBuilder.DropColumn(
                name: "TemplateDataJson",
                table: "EmailDeliveries");
        }
    }
}

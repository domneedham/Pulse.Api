using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MomentScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                table: "moments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "connections",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_moments_connection_id_sequence_number",
                table: "moments",
                columns: new[] { "connection_id", "sequence_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_moments_connection_id_sequence_number",
                table: "moments");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                table: "moments");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "connections");
        }
    }
}

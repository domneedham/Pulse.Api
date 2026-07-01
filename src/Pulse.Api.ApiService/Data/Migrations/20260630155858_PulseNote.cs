using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class PulseNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "pulses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "note",
                table: "pulses");
        }
    }
}

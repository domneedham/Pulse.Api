using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class PulseTouch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pulse_touches",
                columns: table => new
                {
                    pulse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stroke_data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pulse_touches", x => x.pulse_id);
                    table.ForeignKey(
                        name: "fk_pulse_touches_pulses_pulse_id",
                        column: x => x.pulse_id,
                        principalTable: "pulses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pulse_touches");
        }
    }
}

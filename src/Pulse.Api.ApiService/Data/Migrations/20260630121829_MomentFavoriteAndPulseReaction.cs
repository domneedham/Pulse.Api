using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MomentFavoriteAndPulseReaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reaction",
                table: "pulses",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_favorite",
                table: "moments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reaction",
                table: "pulses");

            migrationBuilder.DropColumn(
                name: "is_favorite",
                table: "moments");
        }
    }
}

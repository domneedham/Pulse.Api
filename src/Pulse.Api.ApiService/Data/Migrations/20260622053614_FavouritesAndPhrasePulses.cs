using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FavouritesAndPhrasePulses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "message",
                table: "pulse_thoughts");

            migrationBuilder.DropColumn(
                name: "need_type",
                table: "pulse_needs");

            migrationBuilder.DropColumn(
                name: "mood_type",
                table: "pulse_moods");

            migrationBuilder.AddColumn<string>(
                name: "emoji",
                table: "pulse_thoughts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "pulse_thoughts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "emoji",
                table: "pulse_needs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "pulse_needs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "emoji",
                table: "pulse_moods",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "pulse_moods",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "user_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    text = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_favorites", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_favorites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_favorites_user_id_category_sort_order",
                table: "user_favorites",
                columns: new[] { "user_id", "category", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_favorites");

            migrationBuilder.DropColumn(
                name: "emoji",
                table: "pulse_thoughts");

            migrationBuilder.DropColumn(
                name: "text",
                table: "pulse_thoughts");

            migrationBuilder.DropColumn(
                name: "emoji",
                table: "pulse_needs");

            migrationBuilder.DropColumn(
                name: "text",
                table: "pulse_needs");

            migrationBuilder.DropColumn(
                name: "emoji",
                table: "pulse_moods");

            migrationBuilder.DropColumn(
                name: "text",
                table: "pulse_moods");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "pulse_thoughts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "need_type",
                table: "pulse_needs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "mood_type",
                table: "pulse_moods",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }
    }
}

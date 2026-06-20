using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConnectionsAndPulses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_b_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    connected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_connections_users_user_a_id",
                        column: x => x.user_a_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_users_user_b_id",
                        column: x => x.user_b_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pulses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pulses", x => x.id);
                    table.ForeignKey(
                        name: "fk_pulses_connections_connection_id",
                        column: x => x.connection_id,
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pulses_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pulse_moods",
                columns: table => new
                {
                    pulse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mood_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pulse_moods", x => x.pulse_id);
                    table.ForeignKey(
                        name: "fk_pulse_moods_pulses_pulse_id",
                        column: x => x.pulse_id,
                        principalTable: "pulses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pulse_needs",
                columns: table => new
                {
                    pulse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pulse_needs", x => x.pulse_id);
                    table.ForeignKey(
                        name: "fk_pulse_needs_pulses_pulse_id",
                        column: x => x.pulse_id,
                        principalTable: "pulses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pulse_thoughts",
                columns: table => new
                {
                    pulse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pulse_thoughts", x => x.pulse_id);
                    table.ForeignKey(
                        name: "fk_pulse_thoughts_pulses_pulse_id",
                        column: x => x.pulse_id,
                        principalTable: "pulses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_connections_invite_code",
                table: "connections",
                column: "invite_code",
                unique: true,
                filter: "invite_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_connections_user_a_id",
                table: "connections",
                column: "user_a_id");

            migrationBuilder.CreateIndex(
                name: "ix_connections_user_b_id",
                table: "connections",
                column: "user_b_id");

            migrationBuilder.CreateIndex(
                name: "ix_pulses_connection_id_created_at",
                table: "pulses",
                columns: new[] { "connection_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pulses_sender_id",
                table: "pulses",
                column: "sender_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pulse_moods");

            migrationBuilder.DropTable(
                name: "pulse_needs");

            migrationBuilder.DropTable(
                name: "pulse_thoughts");

            migrationBuilder.DropTable(
                name: "pulses");

            migrationBuilder.DropTable(
                name: "connections");
        }
    }
}

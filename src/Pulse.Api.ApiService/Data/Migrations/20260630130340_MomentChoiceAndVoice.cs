using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MomentChoiceAndVoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "options",
                table: "moment_templates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "choice_index",
                table: "moment_responses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voice_path",
                table: "moment_responses",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voice_url",
                table: "moment_responses",
                type: "character varying(800)",
                maxLength: 800,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("0294f0f6-258e-b0fe-86c6-973a844710cd"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("064285a4-baa0-b0a0-bfba-781325551437"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("0a36c1e8-b0b3-2566-782d-b25812db995d"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("0dfdcbdb-9843-ac8b-a07f-7657dc1d6ce7"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("194fc97a-0a52-a9e8-4a71-cf7748dc3f0d"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("1c6a4473-523f-3e6b-3967-2f5b1df1f6b9"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("1cb2df20-21d4-e065-b97b-4056de6f7892"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("269a7c76-5193-3b40-a9b5-24a98b2c2e26"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("291c025f-6a80-e81d-a51c-b18f7ac5a54e"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("291d775d-4efe-9faa-8481-053ebdf38f05"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("3d7daa2f-1ef8-f7b9-da36-9b18d18e45c8"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("40a64f3a-739d-24b8-5ac8-2f9c78631c56"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("43fb2e70-5a08-aaa0-9f57-c46adb043bd4"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("44e97470-c9df-9816-d2eb-daac7b00cbc0"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("45c3b95d-837b-7cbc-40ca-d2f50e3018c7"),
                columns: new[] { "options", "prompt", "response_kind", "title" },
                values: new object[] { "[\"\\u26F0\\uFE0F Mountains\",\"\\uD83C\\uDFD6\\uFE0F Beach\",\"\\uD83C\\uDFD9\\uFE0F City\"]", "Pick your favourite!", "Choice", "Would You Rather?" });

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("489c1a9a-d137-452b-0194-e37e93d5b5b9"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("4f32102f-7280-d6a9-52ff-a55adf11bd07"),
                columns: new[] { "options", "prompt", "response_kind" },
                values: new object[] { "[\"\\uD83C\\uDF55 Pizza\",\"\\uD83C\\uDF54 Burger\"]", "Pizza or Burger? Pick your favourite.", "Choice" });

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("54642194-9e81-d090-9903-c352705f1900"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("55d333df-d4bc-2f86-8485-c66d3abe06a2"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("56cb0745-9534-6816-b5c5-cbffc6ba9b00"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("63d5face-5ad3-b7d6-8b6b-24f6bf6bf177"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("65976f8d-b3b0-8673-8e76-f49cac7f6c2f"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("673a8d11-222e-2f77-5bf6-b9a8dffa31b1"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("6b396ea2-b3df-1ea3-0cf0-b97dd8b2b13b"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("7164603c-29cb-16a2-edaf-d3a67d715301"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("77a8adec-d0a6-c05c-7a3d-71b5a3fc8f2b"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("7a089057-0f95-4a65-f0c7-a28a7a9e136c"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("7b41d0a2-4f28-f2f3-05de-e96d59c2b9da"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("84d1823d-145c-07a2-593f-f38e4d35e225"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("8d033758-505f-b30d-7657-8eb24f3c6f1e"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("b7124984-4a33-3a96-6b33-23a2fab1ca81"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("bdbe1627-849f-4004-e5f1-5d43e316dfb2"),
                columns: new[] { "options", "prompt", "response_kind" },
                values: new object[] { "[\"\\uD83D\\uDC31 Cats\",\"\\uD83D\\uDC36 Dogs\"]", "Cats or dogs?", "Choice" });

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("c0ac7185-e523-5ff6-716b-0d0f79ae6aec"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("cf86d4bd-aaa0-8d97-4042-65cc5fb4a41f"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("d1709834-2a51-7e15-c535-f73992153eb4"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("d3899992-5ddb-651c-8c95-326794de7502"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("d6e73489-f577-cea7-73af-d4b3b3fbd39d"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("e4b85faa-b4c1-f8e4-24de-822be82fd25a"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("eb4a957b-1338-ba4a-c0db-d96f6e3a4417"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("eb79632f-20cb-3e89-8c40-5a4e65b81457"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("ebe43843-2a65-0da5-67aa-3aab91231ffe"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("edc5ab2f-7a97-aee9-ca95-dfbd48339d8f"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("eefbe0f2-60d5-c07d-fb40-cb3aa6347126"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("f2713a6d-04df-7129-b6f2-ae21bd251539"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("fcc50a36-a32c-886d-84d8-1e7911bba401"),
                column: "options",
                value: null);

            migrationBuilder.InsertData(
                table: "moment_templates",
                columns: new[] { "id", "category", "options", "pack_id", "prompt", "response_kind", "title" },
                values: new object[,]
                {
                    { new Guid("a46b7677-e097-cdd9-87ca-85771a488ba8"), "Voice", null, new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Record your best laugh.", "Voice", "Laugh" },
                    { new Guid("e7ed1cbc-821a-2e93-97c0-d9eaffff245a"), "Voice", null, new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Leave a 20-second morning message.", "Voice", "Good morning" },
                    { new Guid("f0512d14-d8ba-7e37-0bca-89064a6f9c0a"), "Voice", null, new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Share a story you've never told.", "Voice", "Tell me something" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("a46b7677-e097-cdd9-87ca-85771a488ba8"));

            migrationBuilder.DeleteData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("e7ed1cbc-821a-2e93-97c0-d9eaffff245a"));

            migrationBuilder.DeleteData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("f0512d14-d8ba-7e37-0bca-89064a6f9c0a"));

            migrationBuilder.DropColumn(
                name: "options",
                table: "moment_templates");

            migrationBuilder.DropColumn(
                name: "choice_index",
                table: "moment_responses");

            migrationBuilder.DropColumn(
                name: "voice_path",
                table: "moment_responses");

            migrationBuilder.DropColumn(
                name: "voice_url",
                table: "moment_responses");

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("45c3b95d-837b-7cbc-40ca-d2f50e3018c7"),
                columns: new[] { "prompt", "response_kind", "title" },
                values: new object[] { "Who would survive a zombie apocalypse?", "Text", "Who's more likely?" });

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("4f32102f-7280-d6a9-52ff-a55adf11bd07"),
                columns: new[] { "prompt", "response_kind" },
                values: new object[] { "Pizza 🍕 or Burger 🍔? Reveal together.", "Text" });

            migrationBuilder.UpdateData(
                table: "moment_templates",
                keyColumn: "id",
                keyValue: new Guid("bdbe1627-849f-4004-e5f1-5d43e316dfb2"),
                columns: new[] { "prompt", "response_kind" },
                values: new object[] { "Cats or dogs? Reveal.", "Text" });
        }
    }
}

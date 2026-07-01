using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pulse.Api.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Moments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_pro = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moment_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    prompt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    response_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moment_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_moment_templates_packs_pack_id",
                        column: x => x.pack_id,
                        principalTable: "packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moments", x => x.id);
                    table.ForeignKey(
                        name: "fk_moments_connections_connection_id",
                        column: x => x.connection_id,
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_moments_moment_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "moment_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "moment_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    moment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    text = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    stroke_data = table.Column<string>(type: "jsonb", nullable: true),
                    photo_path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moment_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_moment_responses_moments_moment_id",
                        column: x => x.moment_id,
                        principalTable: "moments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_moment_responses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "packs",
                columns: new[] { "id", "emoji", "is_pro", "key", "sort_order", "title" },
                values: new object[,]
                {
                    { new Guid("2d45b383-1800-e3d8-1d40-cd0473582204"), "😂", true, "fun", 3, "Fun" },
                    { new Guid("4a1394b9-499d-15e5-78c6-afe1634365c4"), "🌍", true, "adventure", 2, "Adventure" },
                    { new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "✨", false, "core", 0, "Core" },
                    { new Guid("dac70586-bd06-9979-eab7-58ea6620321c"), "🧠", true, "reflection", 4, "Reflection" },
                    { new Guid("deb2a561-3bc4-b63e-ffc9-ef5f1e1424cd"), "💌", true, "romance", 5, "Romance" },
                    { new Guid("e172da09-9a11-fa64-c9af-ea552326293b"), "📸", true, "photography", 1, "Photography" },
                    { new Guid("e7a80ae9-5030-1bb0-0767-89342d30b30a"), "🌱", true, "garden", 6, "Garden" }
                });

            migrationBuilder.InsertData(
                table: "moment_templates",
                columns: new[] { "id", "category", "pack_id", "prompt", "response_kind", "title" },
                values: new object[,]
                {
                    { new Guid("0294f0f6-258e-b0fe-86c6-973a844710cd"), "Capture", new Guid("e7a80ae9-5030-1bb0-0767-89342d30b30a"), "Photograph something growing.", "Photo", "Growing" },
                    { new Guid("064285a4-baa0-b0a0-bfba-781325551437"), "Micro", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Describe today in one word.", "Text", "One word" },
                    { new Guid("0a36c1e8-b0b3-2566-782d-b25812db995d"), "Capture", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Find beauty in something most people would walk past.", "Photo", "Tiny detail" },
                    { new Guid("0dfdcbdb-9843-ac8b-a07f-7657dc1d6ce7"), "Capture", new Guid("e172da09-9a11-fa64-c9af-ea552326293b"), "Capture an interesting shadow.", "Photo", "Shadows" },
                    { new Guid("194fc97a-0a52-a9e8-4a71-cf7748dc3f0d"), "LoveLetter", new Guid("deb2a561-3bc4-b63e-ffc9-ef5f1e1424cd"), "Write a short letter to future us.", "Text", "Future letter" },
                    { new Guid("1c6a4473-523f-3e6b-3967-2f5b1df1f6b9"), "Capture", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Capture something that's your favourite colour.", "Photo", "Favourite colour" },
                    { new Guid("1cb2df20-21d4-e065-b97b-4056de6f7892"), "Adventure", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Try a new café.", "Photo", "Coffee" },
                    { new Guid("269a7c76-5193-3b40-a9b5-24a98b2c2e26"), "Reflection", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "What's one thing you're proud of today?", "Text", "Win" },
                    { new Guid("291c025f-6a80-e81d-a51c-b18f7ac5a54e"), "Reflection", new Guid("dac70586-bd06-9979-eab7-58ea6620321c"), "Something you're grateful for.", "Text", "Grateful" },
                    { new Guid("291d775d-4efe-9faa-8481-053ebdf38f05"), "Draw", new Guid("2d45b383-1800-e3d8-1d40-cd0473582204"), "Draw each other from memory.", "Drawing", "Draw from memory" },
                    { new Guid("3d7daa2f-1ef8-f7b9-da36-9b18d18e45c8"), "Capture", new Guid("e172da09-9a11-fa64-c9af-ea552326293b"), "Capture the light at golden hour.", "Photo", "Golden hour" },
                    { new Guid("40a64f3a-739d-24b8-5ac8-2f9c78631c56"), "Draw", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Draw your perfect Sunday.", "Drawing", "Perfect Sunday" },
                    { new Guid("43fb2e70-5a08-aaa0-9f57-c46adb043bd4"), "Micro", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Draw one thing.", "Drawing", "One doodle" },
                    { new Guid("44e97470-c9df-9816-d2eb-daac7b00cbc0"), "Reflection", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "What was today's best moment?", "Text", "Best part" },
                    { new Guid("45c3b95d-837b-7cbc-40ca-d2f50e3018c7"), "Fun", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Who would survive a zombie apocalypse?", "Text", "Who's more likely?" },
                    { new Guid("489c1a9a-d137-452b-0194-e37e93d5b5b9"), "Micro", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "One photo. No caption.", "Photo", "One photo" },
                    { new Guid("4f32102f-7280-d6a9-52ff-a55adf11bd07"), "Fun", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Pizza 🍕 or Burger 🍔? Reveal together.", "Text", "This or That" },
                    { new Guid("54642194-9e81-d090-9903-c352705f1900"), "Draw", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Both draw an animal. Reveal together.", "Drawing", "Guess" },
                    { new Guid("55d333df-d4bc-2f86-8485-c66d3abe06a2"), "Adventure", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Walk somewhere you've never been.", "Photo", "Walk" },
                    { new Guid("56cb0745-9534-6816-b5c5-cbffc6ba9b00"), "LoveLetter", new Guid("deb2a561-3bc4-b63e-ffc9-ef5f1e1424cd"), "Share a favourite memory of us.", "Text", "Favourite memory" },
                    { new Guid("63d5face-5ad3-b7d6-8b6b-24f6bf6bf177"), "Capture", new Guid("e7a80ae9-5030-1bb0-0767-89342d30b30a"), "Find a bee.", "Photo", "Find a bee" },
                    { new Guid("65976f8d-b3b0-8673-8e76-f49cac7f6c2f"), "LoveLetter", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Something you've never thanked them for.", "Text", "Thank you" },
                    { new Guid("673a8d11-222e-2f77-5bf6-b9a8dffa31b1"), "Adventure", new Guid("4a1394b9-499d-15e5-78c6-afe1634365c4"), "Walk a street you've never walked.", "Photo", "New street" },
                    { new Guid("6b396ea2-b3df-1ea3-0cf0-b97dd8b2b13b"), "LoveLetter", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "My favourite thing about you this week.", "Text", "Favourite" },
                    { new Guid("7164603c-29cb-16a2-edaf-d3a67d715301"), "Reflection", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "What's one thing you're excited for?", "Text", "Tomorrow" },
                    { new Guid("77a8adec-d0a6-c05c-7a3d-71b5a3fc8f2b"), "LoveLetter", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "One thing I'm looking forward to.", "Text", "Future" },
                    { new Guid("7a089057-0f95-4a65-f0c7-a28a7a9e136c"), "Fun", new Guid("2d45b383-1800-e3d8-1d40-cd0473582204"), "Finish: \"The best part of us is…\"", "Text", "Finish the sentence" },
                    { new Guid("7b41d0a2-4f28-f2f3-05de-e96d59c2b9da"), "LoveLetter", new Guid("deb2a561-3bc4-b63e-ffc9-ef5f1e1424cd"), "Three compliments, right now.", "Text", "Three compliments" },
                    { new Guid("84d1823d-145c-07a2-593f-f38e4d35e225"), "Adventure", new Guid("4a1394b9-499d-15e5-78c6-afe1634365c4"), "Find street art.", "Photo", "Street art" },
                    { new Guid("8d033758-505f-b30d-7657-8eb24f3c6f1e"), "Capture", new Guid("e7a80ae9-5030-1bb0-0767-89342d30b30a"), "Photograph your favourite flower today.", "Photo", "Favourite flower" },
                    { new Guid("b7124984-4a33-3a96-6b33-23a2fab1ca81"), "Capture", new Guid("e172da09-9a11-fa64-c9af-ea552326293b"), "Photograph a reflection.", "Photo", "Reflections" },
                    { new Guid("bdbe1627-849f-4004-e5f1-5d43e316dfb2"), "Fun", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Cats or dogs? Reveal.", "Text", "Secret vote" },
                    { new Guid("c0ac7185-e523-5ff6-716b-0d0f79ae6aec"), "Capture", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Photograph something that made you smile today.", "Photo", "Smile" },
                    { new Guid("cf86d4bd-aaa0-8d97-4042-65cc5fb4a41f"), "Reflection", new Guid("dac70586-bd06-9979-eab7-58ea6620321c"), "Best decision you made today?", "Text", "Best decision" },
                    { new Guid("d1709834-2a51-7e15-c535-f73992153eb4"), "Capture", new Guid("e172da09-9a11-fa64-c9af-ea552326293b"), "Find leading lines in your surroundings.", "Photo", "Leading lines" },
                    { new Guid("d3899992-5ddb-651c-8c95-326794de7502"), "Adventure", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Watch today's sunset.", "Photo", "Sunset" },
                    { new Guid("d6e73489-f577-cea7-73af-d4b3b3fbd39d"), "Capture", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Capture today's weather.", "Photo", "Seasons" },
                    { new Guid("e4b85faa-b4c1-f8e4-24de-822be82fd25a"), "Draw", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Draw yourself from memory.", "Drawing", "Self portrait" },
                    { new Guid("eb4a957b-1338-ba4a-c0db-d96f6e3a4417"), "Adventure", new Guid("4a1394b9-499d-15e5-78c6-afe1634365c4"), "Visit a park.", "Photo", "Park" },
                    { new Guid("eb79632f-20cb-3e89-8c40-5a4e65b81457"), "Micro", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "How was today?", "Text", "One emoji" },
                    { new Guid("ebe43843-2a65-0da5-67aa-3aab91231ffe"), "Reflection", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "What was difficult today?", "Text", "Challenge" },
                    { new Guid("edc5ab2f-7a97-aee9-ca95-dfbd48339d8f"), "LoveLetter", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "One thing I appreciated today.", "Text", "Appreciation" },
                    { new Guid("eefbe0f2-60d5-c07d-fb40-cb3aa6347126"), "Draw", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Sketch your dream home.", "Drawing", "Dream house" },
                    { new Guid("f2713a6d-04df-7129-b6f2-ae21bd251539"), "Fun", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Describe your day using only emojis.", "Text", "Emoji story" },
                    { new Guid("fcc50a36-a32c-886d-84d8-1e7911bba401"), "Adventure", new Guid("a07a6b6e-62f5-dd5c-5c66-1a8d085aa450"), "Find something heart-shaped outside.", "Photo", "Find" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_moment_responses_moment_id_user_id",
                table: "moment_responses",
                columns: new[] { "moment_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moment_responses_user_id",
                table: "moment_responses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_moment_templates_pack_id",
                table: "moment_templates",
                column: "pack_id");

            migrationBuilder.CreateIndex(
                name: "ix_moments_connection_id_date",
                table: "moments",
                columns: new[] { "connection_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moments_template_id",
                table: "moments",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_packs_key",
                table: "packs",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moment_responses");

            migrationBuilder.DropTable(
                name: "moments");

            migrationBuilder.DropTable(
                name: "moment_templates");

            migrationBuilder.DropTable(
                name: "packs");
        }
    }
}

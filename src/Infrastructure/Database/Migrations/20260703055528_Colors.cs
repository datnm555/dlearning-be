using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Colors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HexValue = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    ExampleEmoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colors", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "colors",
                columns: new[] { "Id", "Code", "DisplayOrder", "ExampleEmoji", "HexValue" },
                values: new object[,]
                {
                    { new Guid("c0100000-0000-4000-8000-000000000001"), "red", 1, "🍎", "#EF4444" },
                    { new Guid("c0100000-0000-4000-8000-000000000002"), "orange", 2, "🍊", "#F97316" },
                    { new Guid("c0100000-0000-4000-8000-000000000003"), "yellow", 3, "🍌", "#FACC15" },
                    { new Guid("c0100000-0000-4000-8000-000000000004"), "green", 4, "🍃", "#22C55E" },
                    { new Guid("c0100000-0000-4000-8000-000000000005"), "blue", 5, "🫐", "#3B82F6" },
                    { new Guid("c0100000-0000-4000-8000-000000000006"), "purple", 6, "🍇", "#A855F7" },
                    { new Guid("c0100000-0000-4000-8000-000000000007"), "pink", 7, "🌸", "#EC4899" },
                    { new Guid("c0100000-0000-4000-8000-000000000008"), "brown", 8, "🍫", "#92573B" },
                    { new Guid("c0100000-0000-4000-8000-000000000009"), "black", 9, "🐈‍⬛", "#1F2937" },
                    { new Guid("c0100000-0000-4000-8000-00000000000a"), "white", 10, "☁️", "#F9FAFB" },
                    { new Guid("c0100000-0000-4000-8000-00000000000b"), "gray", 11, "🐘", "#9CA3AF" }
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000003"),
                column: "IsAvailable",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_colors_Code",
                table: "colors",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colors");

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000003"),
                column: "IsAvailable",
                value: false);
        }
    }
}

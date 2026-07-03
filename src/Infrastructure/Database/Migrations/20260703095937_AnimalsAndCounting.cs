using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AnimalsAndCounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "animals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "counting_numbers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counting_numbers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "animals",
                columns: new[] { "Id", "Code", "DisplayOrder", "Emoji" },
                values: new object[,]
                {
                    { new Guid("a11a0000-0000-4000-8000-000000000001"), "cat", 1, "🐱" },
                    { new Guid("a11a0000-0000-4000-8000-000000000002"), "dog", 2, "🐶" },
                    { new Guid("a11a0000-0000-4000-8000-000000000003"), "cow", 3, "🐄" },
                    { new Guid("a11a0000-0000-4000-8000-000000000004"), "chicken", 4, "🐔" },
                    { new Guid("a11a0000-0000-4000-8000-000000000005"), "duck", 5, "🦆" },
                    { new Guid("a11a0000-0000-4000-8000-000000000006"), "pig", 6, "🐷" },
                    { new Guid("a11a0000-0000-4000-8000-000000000007"), "elephant", 7, "🐘" },
                    { new Guid("a11a0000-0000-4000-8000-000000000008"), "lion", 8, "🦁" },
                    { new Guid("a11a0000-0000-4000-8000-000000000009"), "monkey", 9, "🐵" },
                    { new Guid("a11a0000-0000-4000-8000-00000000000a"), "bird", 10, "🐦" },
                    { new Guid("a11a0000-0000-4000-8000-00000000000b"), "fish", 11, "🐟" },
                    { new Guid("a11a0000-0000-4000-8000-00000000000c"), "frog", 12, "🐸" }
                });

            migrationBuilder.InsertData(
                table: "counting_numbers",
                columns: new[] { "Id", "Emoji", "Value" },
                values: new object[,]
                {
                    { new Guid("c0117000-0000-4000-8000-000000000001"), "🍎", 1 },
                    { new Guid("c0117000-0000-4000-8000-000000000002"), "🍌", 2 },
                    { new Guid("c0117000-0000-4000-8000-000000000003"), "🐟", 3 },
                    { new Guid("c0117000-0000-4000-8000-000000000004"), "🌸", 4 },
                    { new Guid("c0117000-0000-4000-8000-000000000005"), "⭐", 5 },
                    { new Guid("c0117000-0000-4000-8000-000000000006"), "🍇", 6 },
                    { new Guid("c0117000-0000-4000-8000-000000000007"), "🐞", 7 },
                    { new Guid("c0117000-0000-4000-8000-000000000008"), "🐙", 8 },
                    { new Guid("c0117000-0000-4000-8000-000000000009"), "🎈", 9 },
                    { new Guid("c0117000-0000-4000-8000-00000000000a"), "🍭", 10 }
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000002"),
                column: "IsAvailable",
                value: true);

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000004"),
                column: "IsAvailable",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_animals_Code",
                table: "animals",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_counting_numbers_Value",
                table: "counting_numbers",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "animals");

            migrationBuilder.DropTable(
                name: "counting_numbers");

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000002"),
                column: "IsAvailable",
                value: false);

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: new Guid("9d0d0000-0000-4000-8000-000000000004"),
                column: "IsAvailable",
                value: false);
        }
    }
}

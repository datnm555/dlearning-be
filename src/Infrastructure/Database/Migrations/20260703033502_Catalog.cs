using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IconKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "Code", "DisplayOrder", "IconKey" },
                values: new object[,]
                {
                    { new Guid("ca7e0000-0000-4000-8000-000000000001"), "preschool", 1, "🧸" },
                    { new Guid("ca7e0000-0000-4000-8000-000000000002"), "primary", 2, "✏️" },
                    { new Guid("ca7e0000-0000-4000-8000-000000000003"), "secondary", 3, "📐" },
                    { new Guid("ca7e0000-0000-4000-8000-000000000004"), "highschool", 4, "🎓" },
                    { new Guid("ca7e0000-0000-4000-8000-000000000005"), "university", 5, "🏛️" }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "CategoryId", "Code", "DisplayOrder", "IconKey", "IsAvailable" },
                values: new object[,]
                {
                    { new Guid("9d0d0000-0000-4000-8000-000000000001"), new Guid("ca7e0000-0000-4000-8000-000000000001"), "alphabet", 1, "🔤", true },
                    { new Guid("9d0d0000-0000-4000-8000-000000000002"), new Guid("ca7e0000-0000-4000-8000-000000000001"), "animals", 2, "🐾", false },
                    { new Guid("9d0d0000-0000-4000-8000-000000000003"), new Guid("ca7e0000-0000-4000-8000-000000000001"), "colors", 3, "🎨", false },
                    { new Guid("9d0d0000-0000-4000-8000-000000000004"), new Guid("ca7e0000-0000-4000-8000-000000000001"), "counting", 4, "🔢", false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_Code",
                table: "categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId_Code",
                table: "products",
                columns: new[] { "CategoryId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}

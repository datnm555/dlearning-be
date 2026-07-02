using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alphabet_letters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpperCase = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    LowerCase = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Sound = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExampleWord = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExampleEmoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alphabet_letters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "alphabet_letters",
                columns: new[] { "Id", "DisplayOrder", "ExampleEmoji", "ExampleWord", "LowerCase", "Name", "Sound", "UpperCase" },
                values: new object[,]
                {
                    { new Guid("d1ea0000-0000-4000-8000-000000000001"), 1, "👕", "áo", "a", "a", "a", "A" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000002"), 2, "🍚", "ăn", "ă", "á", "á", "Ă" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000003"), 3, "🫖", "ấm", "â", "ớ", "ớ", "Â" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000004"), 4, "🐄", "bò", "b", "bê", "bờ", "B" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000005"), 5, "🐟", "cá", "c", "xê", "cờ", "C" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000006"), 6, "🩴", "dép", "d", "dê", "dờ", "D" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000007"), 7, "💡", "đèn", "đ", "đê", "đờ", "Đ" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000008"), 8, "👶", "em bé", "e", "e", "e", "E" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000009"), 9, "🐸", "ếch", "ê", "ê", "ê", "Ê" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000a"), 10, "🐔", "gà", "g", "giê", "gờ", "G" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000b"), 11, "🌸", "hoa", "h", "hát", "hờ", "H" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000c"), 12, "🤫", "im lặng", "i", "i", "i", "I" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000d"), 13, "🍬", "kẹo", "k", "ca", "cờ", "K" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000e"), 14, "🍃", "lá", "l", "e-lờ", "lờ", "L" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000000f"), 15, "🐱", "mèo", "m", "em-mờ", "mờ", "M" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000010"), 16, "👒", "nón", "n", "en-nờ", "nờ", "N" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000011"), 17, "🐝", "ong", "o", "o", "o", "O" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000012"), 18, "🚗", "ô tô", "ô", "ô", "ô", "Ô" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000013"), 19, "🍑", "quả mơ", "ơ", "ơ", "ơ", "Ơ" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000014"), 20, "🔋", "pin", "p", "pê", "pờ", "P" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000015"), 21, "🎁", "quà", "q", "quy", "quờ", "Q" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000016"), 22, "🐢", "rùa", "r", "e-rờ", "rờ", "R" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000017"), 23, "⭐", "sao", "s", "ét-sì", "sờ", "S" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000018"), 24, "🍎", "táo", "t", "tê", "tờ", "T" },
                    { new Guid("d1ea0000-0000-4000-8000-000000000019"), 25, "🥤", "uống nước", "u", "u", "u", "U" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000001a"), 26, "🦁", "sư tử", "ư", "ư", "ư", "Ư" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000001b"), 27, "🐘", "voi", "v", "vê", "vờ", "V" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000001c"), 28, "🚲", "xe đạp", "x", "ích-xì", "xờ", "X" },
                    { new Guid("d1ea0000-0000-4000-8000-00000000001d"), 29, "❤️", "yêu", "y", "i dài", "i", "Y" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "DisplayName", "Email", "PasswordHash", "Username" },
                values: new object[] { new Guid("d1ea0000-0000-4000-8000-000000000100"), "Bé Demo", "demo@dlearning.vn", "pbkdf2-sha256$600000$zmz5Jrdt8HVqMz3413OHZQ==$6f+Gd25WENriwukIIkMp+VFRVxMvbzPNjZ+11YdJXPw=", "demo" });

            migrationBuilder.CreateIndex(
                name: "IX_alphabet_letters_DisplayOrder",
                table: "alphabet_letters",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alphabet_letters");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

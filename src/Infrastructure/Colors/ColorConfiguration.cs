using System.Globalization;
using Domain.Colors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Colors;

internal sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("colors");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.HexValue).HasMaxLength(9).IsRequired();
        builder.Property(c => c.ExampleEmoji).HasMaxLength(16).IsRequired();
        builder.Property(c => c.DisplayOrder).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.Ignore(c => c.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Hex, string Emoji)[] rows =
        [
            ("red", "#EF4444", "🍎"),
            ("orange", "#F97316", "🍊"),
            ("yellow", "#FACC15", "🍌"),
            ("green", "#22C55E", "🍃"),
            ("blue", "#3B82F6", "🫐"),
            ("purple", "#A855F7", "🍇"),
            ("pink", "#EC4899", "🌸"),
            ("brown", "#92573B", "🍫"),
            ("black", "#1F2937", "🐈‍⬛"),
            ("white", "#F9FAFB", "☁️"),
            ("gray", "#9CA3AF", "🐘")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("c0100000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                HexValue = rows[i].Hex,
                ExampleEmoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}

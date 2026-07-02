using System.Globalization;
using Domain.Alphabets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Alphabets;

internal sealed class AlphabetLetterConfiguration : IEntityTypeConfiguration<AlphabetLetter>
{
    public void Configure(EntityTypeBuilder<AlphabetLetter> builder)
    {
        builder.ToTable("alphabet_letters");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UpperCase).HasMaxLength(4).IsRequired();
        builder.Property(l => l.LowerCase).HasMaxLength(4).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Sound).HasMaxLength(32).IsRequired();
        builder.Property(l => l.ExampleWord).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ExampleEmoji).HasMaxLength(16).IsRequired();
        builder.Property(l => l.DisplayOrder).IsRequired();

        builder.HasIndex(l => l.DisplayOrder).IsUnique();

        builder.Ignore(l => l.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Upper, string Lower, string Name, string Sound, string Word, string Emoji)[] rows =
        [
            ("A", "a", "a", "a", "áo", "👕"),
            ("Ă", "ă", "á", "á", "ăn", "🍚"),
            ("Â", "â", "ớ", "ớ", "ấm", "🫖"),
            ("B", "b", "bê", "bờ", "bò", "🐄"),
            ("C", "c", "xê", "cờ", "cá", "🐟"),
            ("D", "d", "dê", "dờ", "dép", "🩴"),
            ("Đ", "đ", "đê", "đờ", "đèn", "💡"),
            ("E", "e", "e", "e", "em bé", "👶"),
            ("Ê", "ê", "ê", "ê", "ếch", "🐸"),
            ("G", "g", "giê", "gờ", "gà", "🐔"),
            ("H", "h", "hát", "hờ", "hoa", "🌸"),
            ("I", "i", "i", "i", "im lặng", "🤫"),
            ("K", "k", "ca", "cờ", "kẹo", "🍬"),
            ("L", "l", "e-lờ", "lờ", "lá", "🍃"),
            ("M", "m", "em-mờ", "mờ", "mèo", "🐱"),
            ("N", "n", "en-nờ", "nờ", "nón", "👒"),
            ("O", "o", "o", "o", "ong", "🐝"),
            ("Ô", "ô", "ô", "ô", "ô tô", "🚗"),
            ("Ơ", "ơ", "ơ", "ơ", "quả mơ", "🍑"),
            ("P", "p", "pê", "pờ", "pin", "🔋"),
            ("Q", "q", "quy", "quờ", "quà", "🎁"),
            ("R", "r", "e-rờ", "rờ", "rùa", "🐢"),
            ("S", "s", "ét-sì", "sờ", "sao", "⭐"),
            ("T", "t", "tê", "tờ", "táo", "🍎"),
            ("U", "u", "u", "u", "uống nước", "🥤"),
            ("Ư", "ư", "ư", "ư", "sư tử", "🦁"),
            ("V", "v", "vê", "vờ", "voi", "🐘"),
            ("X", "x", "ích-xì", "xờ", "xe đạp", "🚲"),
            ("Y", "y", "i dài", "i", "yêu", "❤️")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("d1ea0000-0000-4000-8000-0000000000" + hex),
                UpperCase = rows[i].Upper,
                LowerCase = rows[i].Lower,
                Name = rows[i].Name,
                Sound = rows[i].Sound,
                ExampleWord = rows[i].Word,
                ExampleEmoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}

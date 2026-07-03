using System.Globalization;
using Domain.Counting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Counting;

internal sealed class CountingNumberConfiguration : IEntityTypeConfiguration<CountingNumber>
{
    public void Configure(EntityTypeBuilder<CountingNumber> builder)
    {
        builder.ToTable("counting_numbers");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Value).IsRequired();
        builder.Property(n => n.Emoji).HasMaxLength(16).IsRequired();

        builder.HasIndex(n => n.Value).IsUnique();
        builder.Ignore(n => n.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        string[] emojis = ["🍎", "🍌", "🐟", "🌸", "⭐", "🍇", "🐞", "🐙", "🎈", "🍭"];

        var seed = new object[emojis.Length];
        for (int i = 0; i < emojis.Length; i++)
        {
            int value = i + 1;
            string hex = value.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("c0117000-0000-4000-8000-0000000000" + hex),
                Value = value,
                Emoji = emojis[i]
            };
        }

        return seed;
    }
}

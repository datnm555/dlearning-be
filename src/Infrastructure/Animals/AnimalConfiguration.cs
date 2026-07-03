using System.Globalization;
using Domain.Animals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Animals;

internal sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("animals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Emoji).HasMaxLength(16).IsRequired();
        builder.Property(a => a.DisplayOrder).IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();
        builder.Ignore(a => a.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Emoji)[] rows =
        [
            ("cat", "🐱"),
            ("dog", "🐶"),
            ("cow", "🐄"),
            ("chicken", "🐔"),
            ("duck", "🦆"),
            ("pig", "🐷"),
            ("elephant", "🐘"),
            ("lion", "🦁"),
            ("monkey", "🐵"),
            ("bird", "🐦"),
            ("fish", "🐟"),
            ("frog", "🐸")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("a11a0000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                Emoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}

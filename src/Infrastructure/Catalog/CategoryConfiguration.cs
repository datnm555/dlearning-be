using System.Globalization;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.IconKey).HasMaxLength(16).IsRequired();
        builder.Property(c => c.DisplayOrder).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.Ignore(c => c.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Icon)[] rows =
        [
            ("preschool", "🧸"),
            ("primary", "✏️"),
            ("secondary", "📐"),
            ("highschool", "🎓"),
            ("university", "🏛️")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("ca7e0000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                IconKey = rows[i].Icon,
                DisplayOrder = order
            };
        }

        return seed;
    }
}

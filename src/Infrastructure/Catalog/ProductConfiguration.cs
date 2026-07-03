using System.Globalization;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Catalog;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    // Products all belong to the preschool category (seed GUID order 01).
    private static readonly Guid PreschoolId = new("ca7e0000-0000-4000-8000-000000000001");

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.IconKey).HasMaxLength(16).IsRequired();
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.Property(p => p.IsAvailable).IsRequired();

        builder.HasIndex(p => new { p.CategoryId, p.Code }).IsUnique();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Icon, bool Available)[] rows =
        [
            ("alphabet", "🔤", true),
            ("animals", "🐾", false),
            ("colors", "🎨", true),
            ("counting", "🔢", false)
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("9d0d0000-0000-4000-8000-0000000000" + hex),
                CategoryId = PreschoolId,
                Code = rows[i].Code,
                IconKey = rows[i].Icon,
                DisplayOrder = order,
                IsAvailable = rows[i].Available
            };
        }

        return seed;
    }
}

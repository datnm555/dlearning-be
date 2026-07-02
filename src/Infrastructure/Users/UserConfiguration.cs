using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(30).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Ignore(u => u.DomainEvents);

        builder.HasData(new
        {
            Id = new Guid("d1ea0000-0000-4000-8000-000000000100"),
            Email = "demo@dlearning.vn",
            Username = "demo",
            DisplayName = "Bé Demo",
            PasswordHash = "pbkdf2-sha256$600000$zmz5Jrdt8HVqMz3413OHZQ==$6f+Gd25WENriwukIIkMp+VFRVxMvbzPNjZ+11YdJXPw="
        });
    }
}

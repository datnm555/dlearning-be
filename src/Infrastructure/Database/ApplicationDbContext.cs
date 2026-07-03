using Application.Abstractions.Data;
using Domain.Alphabets;
using Domain.Catalog;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<AlphabetLetter> AlphabetLetters => Set<AlphabetLetter>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

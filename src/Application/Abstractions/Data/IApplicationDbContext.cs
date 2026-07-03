using Domain.Alphabets;
using Domain.Animals;
using Domain.Catalog;
using Domain.Colors;
using Domain.Counting;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<AlphabetLetter> AlphabetLetters { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<Color> Colors { get; }

    DbSet<Animal> Animals { get; }

    DbSet<CountingNumber> CountingNumbers { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

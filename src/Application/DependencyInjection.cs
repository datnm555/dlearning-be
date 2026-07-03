using Application.Abstractions.Messaging;
using Application.Alphabets;
using Application.Alphabets.Data;
using Application.Animals;
using Application.Animals.Data;
using Application.Catalog;
using Application.Catalog.Data;
using Application.Colors;
using Application.Colors.Data;
using Application.Counting;
using Application.Counting.Data;
using Application.Users;
using Application.Users.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, Guid>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
        services.AddScoped<IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>>, GetAlphabetQueryHandler>();
        services.AddScoped<IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>, GetCategoriesQueryHandler>();
        services.AddScoped<IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>>, GetProductsByCategoryQueryHandler>();
        services.AddScoped<IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>>, GetColorsQueryHandler>();
        services.AddScoped<IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>>, GetAnimalsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>>, GetCountingQueryHandler>();
        return services;
    }
}

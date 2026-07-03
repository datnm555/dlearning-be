using Application.Abstractions.Messaging;
using Application.Alphabets;
using Application.Alphabets.Data;
using Application.Catalog;
using Application.Catalog.Data;
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
        return services;
    }
}

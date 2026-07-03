using Application.Abstractions.Messaging;
using Application.Animals;
using Application.Animals.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Animals;

internal sealed class GetAnimals : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/animals", async (
            IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<AnimalDto>> result =
                await handler.Handle(new GetAnimalsQuery(), cancellationToken);

            return result.ToHttpResult(animals => Results.Ok(
                animals.Select(a => new
                {
                    a.Code,
                    name = localizer["Animal." + a.Code].Value,
                    a.Emoji,
                    sound = localizer["AnimalSound." + a.Code].Value,
                    a.DisplayOrder
                })));
        })
        .RequireAuthorization();
    }
}

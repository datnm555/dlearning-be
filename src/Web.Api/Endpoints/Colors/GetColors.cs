using Application.Abstractions.Messaging;
using Application.Colors;
using Application.Colors.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Colors;

internal sealed class GetColors : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/colors", async (
            IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ColorDto>> result =
                await handler.Handle(new GetColorsQuery(), cancellationToken);

            return result.ToHttpResult(colors => Results.Ok(
                colors.Select(c => new
                {
                    c.Code,
                    name = localizer["Color." + c.Code].Value,
                    c.HexValue,
                    exampleWord = localizer["ColorExample." + c.Code].Value,
                    c.ExampleEmoji,
                    c.DisplayOrder
                })));
        })
        .RequireAuthorization();
    }
}

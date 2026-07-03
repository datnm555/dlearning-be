using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Counting;
using Application.Counting.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Counting;

internal sealed class GetCounting : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/counting", async (
            IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<CountingNumberDto>> result =
                await handler.Handle(new GetCountingQuery(), cancellationToken);

            return result.ToHttpResult(numbers => Results.Ok(
                numbers.Select(n => new
                {
                    n.Value,
                    word = localizer["Number." + n.Value.ToString(CultureInfo.InvariantCulture)].Value,
                    n.Emoji
                })));
        })
        .RequireAuthorization();
    }
}

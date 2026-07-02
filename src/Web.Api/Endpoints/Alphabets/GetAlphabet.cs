using Application.Abstractions.Messaging;
using Application.Alphabets;
using Application.Alphabets.Data;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Alphabets;

internal sealed class GetAlphabet : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/alphabet", async (
            IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<AlphabetLetterResponse>> result =
                await handler.Handle(new GetAlphabetQuery(), cancellationToken);

            return result.ToHttpResult();
        })
        .RequireAuthorization();
    }
}

using Application.Abstractions.Messaging;
using Application.Catalog;
using Application.Catalog.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Catalog;

internal sealed class GetCategories : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", async (
            IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<CategoryDto>> result =
                await handler.Handle(new GetCategoriesQuery(), cancellationToken);

            return result.ToHttpResult(categories => Results.Ok(
                categories.Select(c => new
                {
                    c.Code,
                    name = localizer["Category." + c.Code].Value,
                    c.IconKey,
                    c.DisplayOrder
                })));
        });
    }
}

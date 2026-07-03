using Application.Abstractions.Messaging;
using Application.Catalog;
using Application.Catalog.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Catalog;

internal sealed class GetProductsByCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories/{code}/products", async (
            string code,
            IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ProductDto>> result =
                await handler.Handle(new GetProductsByCategoryQuery(code), cancellationToken);

            return result.ToHttpResult(products => Results.Ok(
                products.Select(p => new
                {
                    p.Code,
                    name = localizer["Product." + p.Code].Value,
                    p.IconKey,
                    p.DisplayOrder,
                    p.IsAvailable
                })));
        });
    }
}

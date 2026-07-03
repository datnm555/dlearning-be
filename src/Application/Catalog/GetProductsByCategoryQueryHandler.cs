using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Catalog;

internal sealed class GetProductsByCategoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsByCategoryQuery query,
        CancellationToken cancellationToken)
    {
        string code = (query.CategoryCode ?? string.Empty).Trim().ToLowerInvariant();

        // Correlated EXISTS: keep products whose category has the requested code.
        // Translates to SQL EXISTS in real EF; runs as an in-memory Any() under MockQueryable.
        List<ProductDto> products = await dbContext.Products
            .Where(p => dbContext.Categories.Any(c => c.Id == p.CategoryId && c.Code == code))
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new ProductDto(p.Code, p.IconKey, p.DisplayOrder, p.IsAvailable))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProductDto>>(products);
    }
}

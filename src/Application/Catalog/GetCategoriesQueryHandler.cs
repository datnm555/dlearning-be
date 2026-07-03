using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Catalog;

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        List<CategoryDto> categories = await dbContext.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Code, c.IconKey, c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CategoryDto>>(categories);
    }
}

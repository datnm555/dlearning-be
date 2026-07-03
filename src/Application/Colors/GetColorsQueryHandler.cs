using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Colors.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Colors;

internal sealed class GetColorsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>>
{
    public async Task<Result<IReadOnlyList<ColorDto>>> Handle(
        GetColorsQuery query,
        CancellationToken cancellationToken)
    {
        List<ColorDto> colors = await dbContext.Colors
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ColorDto(c.Code, c.HexValue, c.ExampleEmoji, c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ColorDto>>(colors);
    }
}

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Counting.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Counting;

internal sealed class GetCountingQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>>
{
    public async Task<Result<IReadOnlyList<CountingNumberDto>>> Handle(
        GetCountingQuery query,
        CancellationToken cancellationToken)
    {
        List<CountingNumberDto> numbers = await dbContext.CountingNumbers
            .OrderBy(n => n.Value)
            .Select(n => new CountingNumberDto(n.Value, n.Emoji))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CountingNumberDto>>(numbers);
    }
}

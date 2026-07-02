using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Alphabets.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Alphabets;

internal sealed class GetAlphabetQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>>
{
    public async Task<Result<IReadOnlyList<AlphabetLetterResponse>>> Handle(
        GetAlphabetQuery query,
        CancellationToken cancellationToken)
    {
        List<AlphabetLetterResponse> letters = await dbContext.AlphabetLetters
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new AlphabetLetterResponse(
                l.Id,
                l.UpperCase,
                l.LowerCase,
                l.Name,
                l.Sound,
                l.ExampleWord,
                l.ExampleEmoji,
                l.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AlphabetLetterResponse>>(letters);
    }
}

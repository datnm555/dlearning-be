using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Animals.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Animals;

internal sealed class GetAnimalsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>>
{
    public async Task<Result<IReadOnlyList<AnimalDto>>> Handle(
        GetAnimalsQuery query,
        CancellationToken cancellationToken)
    {
        List<AnimalDto> animals = await dbContext.Animals
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new AnimalDto(a.Code, a.Emoji, a.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AnimalDto>>(animals);
    }
}

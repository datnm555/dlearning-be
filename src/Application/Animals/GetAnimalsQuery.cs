using Application.Abstractions.Messaging;
using Application.Animals.Data;

namespace Application.Animals;

public sealed record GetAnimalsQuery : IQuery<IReadOnlyList<AnimalDto>>;

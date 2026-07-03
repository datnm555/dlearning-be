using Application.Abstractions.Messaging;
using Application.Colors.Data;

namespace Application.Colors;

public sealed record GetColorsQuery : IQuery<IReadOnlyList<ColorDto>>;

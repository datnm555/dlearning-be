using Application.Abstractions.Messaging;
using Application.Alphabets.Data;

namespace Application.Alphabets;

public sealed record GetAlphabetQuery : IQuery<IReadOnlyList<AlphabetLetterResponse>>;

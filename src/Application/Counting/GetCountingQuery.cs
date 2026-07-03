using Application.Abstractions.Messaging;
using Application.Counting.Data;

namespace Application.Counting;

public sealed record GetCountingQuery : IQuery<IReadOnlyList<CountingNumberDto>>;

using SharedKernel;

namespace Domain.Counting;

public sealed class CountingNumber : Entity
{
    public int Value { get; init; }

    public string Emoji { get; init; } = string.Empty;
}

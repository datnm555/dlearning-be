using SharedKernel;

namespace Domain.Alphabets;

public sealed class AlphabetLetter : Entity
{
    public string UpperCase { get; init; } = string.Empty;

    public string LowerCase { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Sound { get; init; } = string.Empty;

    public string ExampleWord { get; init; } = string.Empty;

    public string ExampleEmoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}

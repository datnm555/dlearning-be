using SharedKernel;

namespace Domain.Colors;

public sealed class Color : Entity
{
    public string Code { get; init; } = string.Empty;

    public string HexValue { get; init; } = string.Empty;

    public string ExampleEmoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}

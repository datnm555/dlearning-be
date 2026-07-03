using SharedKernel;

namespace Domain.Animals;

public sealed class Animal : Entity
{
    public string Code { get; init; } = string.Empty;

    public string Emoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}

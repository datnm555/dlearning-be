using SharedKernel;

namespace Domain.Catalog;

public sealed class Product : Entity
{
    public Guid CategoryId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string IconKey { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsAvailable { get; init; }
}

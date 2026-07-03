using SharedKernel;

namespace Domain.Catalog;

public sealed class Category : AggregateRoot
{
    public string Code { get; init; } = string.Empty;

    public string IconKey { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}

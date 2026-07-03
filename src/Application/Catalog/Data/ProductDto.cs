namespace Application.Catalog.Data;

public sealed record ProductDto(string Code, string IconKey, int DisplayOrder, bool IsAvailable);

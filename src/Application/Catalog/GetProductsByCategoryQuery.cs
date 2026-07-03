using Application.Abstractions.Messaging;
using Application.Catalog.Data;

namespace Application.Catalog;

public sealed record GetProductsByCategoryQuery(string CategoryCode) : IQuery<IReadOnlyList<ProductDto>>;

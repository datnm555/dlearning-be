using Application.Abstractions.Messaging;
using Application.Catalog.Data;

namespace Application.Catalog;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;


namespace Feryal.Application.Events.Categories;

public sealed record CategoryCacheInvalidationRequested(
    int? CategoryId);
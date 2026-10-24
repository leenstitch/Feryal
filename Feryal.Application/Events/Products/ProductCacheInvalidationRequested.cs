namespace Feryal.Application.Events.Products;

public sealed record ProductCacheInvalidationRequested(
    int? ProductId);
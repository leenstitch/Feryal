namespace Feryal.Application.Events.Products;

// Represents a request to invalidate cached product data.
public sealed record ProductCacheInvalidationRequested(
    // Identifies the product to invalidate, or null to target all products.
    int? ProductId);
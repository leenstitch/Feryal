using Feryal.Application.DTOs.Cart;

// Defines the contract for cart-related business operations.
public interface ICartService
{
    // Retrieves the user's paginated cart items and pagination details.
    Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Adds a product variant to the user's cart with the specified quantity.
    Task AddItemAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Increases the quantity of a product variant in the user's cart.
    Task IncreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Decreases the quantity of a product variant in the user's cart.
    Task DecreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    // Removes a product variant from the user's cart.
    Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);
}
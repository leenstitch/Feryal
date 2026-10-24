using Feryal.Application.DTOs.Cart;
using Feryal.Domain.Entities;

// Defines the contract for accessing and managing cart data in the database.
public interface ICartRepository
{
    // Retrieves the user's cart for update operations.
    Task<Cart?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken cancellationToken);

    // Retrieves a product variant with the information needed to add it to a cart.
    Task<ProductVariant?> GetVariantForCartAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    // Retrieves paginated cart items along with the total item count and total quantity.
    Task<(List<CartItemResponse> Items, int TotalCount, int TotalQuantity)>
        GetPagedItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

    // Adds a new cart to the database.
    Task AddCartAsync(
        Cart cart,
        CancellationToken cancellationToken);

    // Adds a new item to the cart.
    Task AddItemAsync(
        CartItem item,
        CancellationToken cancellationToken);

    // Removes an item from the cart.
    Task RemoveItemAsync(
        CartItem item,
        CancellationToken cancellationToken);
}
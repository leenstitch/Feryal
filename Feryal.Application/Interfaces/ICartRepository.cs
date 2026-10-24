using Feryal.Application.DTOs.Cart;
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<ProductVariant?> GetVariantForCartAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    Task<(List<CartItemResponse> Items, int TotalCount, int TotalQuantity)>
        GetPagedItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

    Task AddCartAsync(
        Cart cart,
        CancellationToken cancellationToken);

    Task AddItemAsync(
        CartItem item,
        CancellationToken cancellationToken);

    Task RemoveItemAsync(
        CartItem item,
        CancellationToken cancellationToken);
}
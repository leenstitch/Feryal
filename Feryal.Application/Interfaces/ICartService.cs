using Feryal.Application.DTOs.Cart;

namespace Feryal.Application.Interfaces;

public interface ICartService
{
    Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddItemAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task IncreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task DecreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken);

    Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);
}
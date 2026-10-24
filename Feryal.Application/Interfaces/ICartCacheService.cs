using Feryal.Application.DTOs.Cart;

namespace Feryal.Application.Interfaces;

public interface ICartCacheService
{
    Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);
}
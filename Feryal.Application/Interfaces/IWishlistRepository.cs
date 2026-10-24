
using Feryal.Application.DTOs.Wishlist;
using WishlistEntity = Feryal.Domain.Entities.Wishlist;
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

public interface IWishlistRepository
{
    Task<WishlistEntity?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<ProductVariant?> GetVariantForWishlistAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    Task<bool> ContainsVariantAsync(
        int wishlistId,
        int productVariantId,
        CancellationToken cancellationToken);

    Task<(List<WishlistItemResponse> Items, int TotalCount)>
        GetPagedVisibleItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

    Task AddWishlistAsync(
        WishlistEntity? wishlist,
        CancellationToken cancellationToken);

    Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken);

    Task RemoveItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken);
}

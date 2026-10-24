namespace Feryal.Domain.Entities;

public class WishlistItem
{
    private WishlistItem()
    {
    }

    public WishlistItem(
        int wishlistId,
        int productVariantId)
    {
        if (wishlistId <= 0)
            throw new ArgumentException(
                "Wishlist is required.",
                nameof(wishlistId));

        if (productVariantId <= 0)
            throw new ArgumentException(
                "Product variant is required.",
                nameof(productVariantId));

        WishlistId = wishlistId;
        ProductVariantId = productVariantId;
        AddedAt = DateTimeOffset.UtcNow;
    }

    public WishlistItem(
        Wishlist wishlist,
        int productVariantId)
    {
        ArgumentNullException.ThrowIfNull(wishlist);

        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        Wishlist = wishlist;
        WishlistId = wishlist.Id;
        ProductVariantId = productVariantId;
        AddedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int WishlistId { get; private set; }

    public int ProductVariantId { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    public Wishlist Wishlist { get; private set; } = null!;

    public ProductVariant ProductVariant { get; private set; } = null!;
}
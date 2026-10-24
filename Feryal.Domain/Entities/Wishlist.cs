namespace Feryal.Domain.Entities;

public class Wishlist
{
    private readonly List<WishlistItem> _items = new();

    private Wishlist()
    {
    }

    public Wishlist(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<WishlistItem> Items =>
        _items.AsReadOnly();

    public void AddItem(WishlistItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Any(x => x.ProductVariantId == item.ProductVariantId))
        {
            throw new InvalidOperationException(
                "This variant is already in the wishlist.");
        }

        _items.Add(item);
        Touch();
    }

    public void RemoveItem(WishlistItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Remove(item))
        {
            Touch();
        }
    }

    public void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
namespace Feryal.Domain.Entities;

public class Cart
{
    private readonly List<CartItem> _items = new();

    private Cart()
    {
    }

    public Cart(int userId)
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

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public void AddItem(CartItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.CartId != Id)
        {
            throw new InvalidOperationException(
                "The cart item belongs to a different cart.");
        }

        var existingItem = _items.FirstOrDefault(
            x => x.ProductVariantId == item.ProductVariantId);

        if (existingItem is not null)
        {
            throw new InvalidOperationException(
                "This product variant already exists in the cart.");
        }

        _items.Add(item);
        Touch();
    }

    public void RemoveItem(CartItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_items.Remove(item))
        {
            throw new InvalidOperationException(
                "The item does not belong to this cart.");
        }

        Touch();
    }

    public void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
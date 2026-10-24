namespace Feryal.Domain.Entities;

public class CartItem
{
    private CartItem()
    {
    }

    public CartItem(
        int cartId,
        int productVariantId,
        int quantity)
    {
        if (cartId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cartId));
        }

        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        CartId = cartId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }

    public int Id { get; private set; }

    public int CartId { get; private set; }

    public int ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public Cart Cart { get; private set; } = null!;

    public ProductVariant ProductVariant { get; private set; } = null!;

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        Quantity = checked(Quantity + quantity);
    }

    public void DecreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (quantity >= Quantity)
        {
            throw new InvalidOperationException(
                "Quantity cannot become zero. Remove the item instead.");
        }

        Quantity -= quantity;
    }
}
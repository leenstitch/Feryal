namespace Feryal.Domain.Entities;

public class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(
        int orderId,
        int productVariantId,
        int quantity,
        decimal unitPrice,
        string productName,
        string? variantDescription)
    {
        if (orderId <= 0)
            throw new ArgumentException("Order is required.", nameof(orderId));

        if (productVariantId <= 0)
            throw new ArgumentException(
                "Product variant is required.",
                nameof(productVariantId));

        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than zero.",
                nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException(
                "Unit price cannot be negative.",
                nameof(unitPrice));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        OrderId = orderId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        UnitPrice = unitPrice;

        TotalPrice = unitPrice * quantity;

        ProductName = productName.Trim();
        VariantDescription = variantDescription?.Trim();
    }

    public int Id { get; private set; }

    public int OrderId { get; private set; }

    public int ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string? VariantDescription { get; private set; }
    
    public Order Order { get; private set; } = null!;

    public ProductVariant ProductVariant { get; private set; } = null!;
}
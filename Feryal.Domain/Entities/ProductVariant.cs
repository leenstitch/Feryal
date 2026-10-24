namespace Feryal.Domain.Entities;

public class ProductVariant
{
    private ProductVariant()
    {
    }

    public ProductVariant(
        int? scentId,
        int? sizeId,
        int? colorId,
        string? description,
        string sku,
        decimal? price,
        int? stockQuantity)
    {
        if (scentId.HasValue && scentId.Value <= 0)
            throw new ArgumentException(
                "Scent ID must be greater than zero.", nameof(scentId));

        if (sizeId.HasValue && sizeId.Value <= 0)
            throw new ArgumentException(
                "Size ID must be greater than zero.", nameof(sizeId));

        if (colorId.HasValue && colorId.Value <= 0)
            throw new ArgumentException(
                "Color ID must be greater than zero.", nameof(colorId));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException(
                "SKU is required.", nameof(sku));

        if (price.HasValue && price.Value < 0)
            throw new ArgumentException(
                "Price cannot be negative.", nameof(price));

        if (stockQuantity.HasValue && stockQuantity.Value < 0)
            throw new ArgumentException(
                "Stock quantity cannot be negative.", nameof(stockQuantity));

        ScentId = scentId;
        SizeId = sizeId;
        ColorId = colorId;
        Description = NormalizeDescription(description);
        SKU = sku.Trim();
        Price = price;
        StockQuantity = stockQuantity;

        DiscountPercentage = 0;
        IsActive = true;
        IsConfigured = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public int? ScentId { get; private set; }

    public int? SizeId { get; private set; }

    public int? ColorId { get; private set; }

    public string? Description { get; private set; }

    public string SKU { get; private set; } = null!;

    public decimal? Price { get; private set; }

    public decimal DiscountPercentage { get; private set; }

    public decimal? DiscountAmount
    {
        get
        {
            if (!Price.HasValue)
                return null;

            return Math.Round(
                Price.Value * DiscountPercentage / 100m,
                2,
                MidpointRounding.AwayFromZero);
        }
    }

    public decimal? FinalPrice
    {
        get
        {
            if (!Price.HasValue)
                return null;

            return Price.Value - DiscountAmount!.Value;
        }
    }

    public int? StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsConfigured { get; private set; }

    public bool HasCompleteSellingDetails =>
        Price.HasValue &&
        StockQuantity.HasValue;

    public bool IsInStock =>
        StockQuantity.HasValue &&
        StockQuantity.Value > 0;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    public Scent? Scent { get; private set; }

    public Size? Size { get; private set; }

    public Color? Color { get; private set; }

    internal void SetProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (Product is not null &&
            !ReferenceEquals(Product, product))
        {
            throw new InvalidOperationException(
                "This variant already belongs to another product.");
        }

        Product = product;
    }

    //public void UpdateDetails(
    //   string? description,
    //   decimal? price,
    //   int? stockQuantity)
    //{
    //    UpdateDetails(
    //        description,
    //        price,
    //        stockQuantity,
    //        DiscountPercentage);
    //}

    public void UpdateDetails(
     string? description,
     decimal? price,
     int? stockQuantity,
     decimal discountPercentage)
    {
        if (price.HasValue && price.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price cannot be negative.");
        }

        if (stockQuantity.HasValue && stockQuantity.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity),
                "Stock quantity cannot be negative.");
        }

        ValidateDiscountPercentage(discountPercentage);

        Description = NormalizeDescription(description);
        Price = price;
        StockQuantity = stockQuantity;
        DiscountPercentage = discountPercentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDiscountPercentage(decimal discountPercentage)
    {
        ValidateDiscountPercentage(discountPercentage);

        DiscountPercentage = discountPercentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ShowToCustomers()
    {
        if (IsConfigured)
            return;

        IsConfigured = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void HideFromCustomers()
    {
        if (!IsConfigured)
            return;

        IsConfigured = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (!HasCompleteSellingDetails)
            throw new InvalidOperationException(
                "Configure the variant's price and stock first.");

        StockQuantity = checked(StockQuantity!.Value + quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (!HasCompleteSellingDetails)
            throw new InvalidOperationException(
                "Configure the variant's price and stock first.");

        if (quantity > StockQuantity!.Value)
            throw new InvalidOperationException("Insufficient stock.");

        StockQuantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

  


    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateDiscountPercentage(
        decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage),
                "Discount percentage must be between 0 and 100.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var normalized = description.Trim();

        if (normalized.Length > 2000)
            throw new ArgumentException(
                "Description cannot exceed 2000 characters.",
                nameof(description));

        return normalized;
    }
    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity =>
        (StockQuantity ?? 0) - ReservedQuantity;

    public void SetReservedQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ReservedQuantity = quantity;
    }


}

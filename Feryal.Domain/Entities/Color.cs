
namespace Feryal.Domain.Entities;

public class Color
{
    private readonly List<ProductVariant> _productVariants = new();

    private Color()
    {
    }

    public Color(int productId, string name, string? hexCode)
    {
        if (productId <= 0)
            throw new ArgumentException(
                "Product is required.", nameof(productId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Color name is required.", nameof(name));

        ProductId = productId;
        Name = name.Trim();
        HexCode = hexCode?.Trim();

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? HexCode { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    public IReadOnlyCollection<ProductVariant> ProductVariants =>
        _productVariants.AsReadOnly();

    public void Update(string name, string? hexCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Color name is required.", nameof(name));

        Name = name.Trim();
        HexCode = hexCode?.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
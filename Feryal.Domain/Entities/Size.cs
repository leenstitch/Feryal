
namespace Feryal.Domain.Entities;

public class Size
{
    private readonly List<ProductVariant> _productVariants = new();

    private Size()
    {
    }

    public Size(int productId, string name)
    {
        if (productId <= 0)
            throw new ArgumentException(
                "Product is required.", nameof(productId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Size name is required.", nameof(name));

        ProductId = productId;
        Name = name.Trim();

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    public IReadOnlyCollection<ProductVariant> ProductVariants =>
        _productVariants.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Size name is required.", nameof(name));

        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
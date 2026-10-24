namespace Feryal.Domain.Entities;

public class Product
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductImage> _images = new();
    private readonly List<Color> _colors = new();
    private readonly List<Size> _sizes = new();
    private readonly List<Scent> _scents = new();

    private Product()
    {
    }

    public Product(
        int categoryId,
        int productGroupId,
        string name,
        string? description)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException(
                "Category is required.",
                nameof(categoryId));
        }

        if (productGroupId <= 0)
        {
            throw new ArgumentException(
                "Product group is required.",
                nameof(productGroupId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));
        }

        CategoryId = categoryId;
        ProductGroupId = productGroupId;
        Name = name.Trim();
        Description = description?.Trim();

        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int CategoryId { get; private set; }

    public int ProductGroupId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Category Category { get; private set; } = null!;

    public ProductGroup ProductGroup { get; private set; } = null!;

    public IReadOnlyCollection<ProductVariant> Variants =>
        _variants.AsReadOnly();

    public IReadOnlyCollection<ProductImage> Images =>
        _images.AsReadOnly();

    public IReadOnlyCollection<Color> Colors =>
        _colors.AsReadOnly();

    public IReadOnlyCollection<Size> Sizes =>
        _sizes.AsReadOnly();

    public IReadOnlyCollection<Scent> Scents =>
        _scents.AsReadOnly();

    public void Update(
        int categoryId,
        int productGroupId,
        string name,
        string? description)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException(
                "Category is required.",
                nameof(categoryId));
        }

        if (productGroupId <= 0)
        {
            throw new ArgumentException(
                "Product group is required.",
                nameof(productGroupId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));
        }

        CategoryId = categoryId;
        ProductGroupId = productGroupId;
        Name = name.Trim();
        Description = description?.Trim();

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

    public void AddVariant(ProductVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);

        variant.SetProduct(this);
        _variants.Add(variant);

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddImage(ProductImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.ProductId != 0 &&
            Id != 0 &&
            image.ProductId != Id)
        {
            throw new ArgumentException(
                "Image belongs to a different product.",
                nameof(image));
        }

        _images.Add(image);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveImage(ProductImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (_images.Remove(image))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
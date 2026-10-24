
namespace Feryal.Domain.Entities;

public class ProductImage
{
    private ProductImage()
    {
    }

    public ProductImage(
        int productId,
        string imageUrl,
        string publicId,
        bool isPrimary,
        int displayOrder)
    {
        if (productId <= 0)
            throw new ArgumentException(
                "Product is required.",
                nameof(productId));

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));

        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "Cloudinary public ID is required.",
                nameof(publicId));

        if (displayOrder < 0)
            throw new ArgumentException(
                "Display order cannot be negative.",
                nameof(displayOrder));

        ProductId = productId;
        ImageUrl = imageUrl.Trim();
        PublicId = publicId.Trim();
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public string ImageUrl { get; private set; } = null!;

    public string PublicId { get; private set; } = null!;

    public bool IsPrimary { get; private set; }

    public int DisplayOrder { get; private set; }

    public Product Product { get; private set; } = null!;

    public void Update(
        string imageUrl,
        string publicId,
        bool isPrimary,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(
                "Image URL is required.",
                nameof(imageUrl));

        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "Cloudinary public ID is required.",
                nameof(publicId));

        if (displayOrder < 0)
            throw new ArgumentException(
                "Display order cannot be negative.",
                nameof(displayOrder));

        ImageUrl = imageUrl.Trim();
        PublicId = publicId.Trim();
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentException(
                "Display order cannot be negative.",
                nameof(displayOrder));

        DisplayOrder = displayOrder;
    }
}
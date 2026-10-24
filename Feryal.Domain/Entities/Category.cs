
namespace Feryal.Domain.Entities;

public class Category
{
    private readonly List<Product> _products = new();

    private Category()
    {
    }

    public Category(
        string name,
        string? description,
        bool isActive)
    {
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Product> Products =>
        _products.AsReadOnly();

    public void Update(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
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

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 100)
        {
            throw new ArgumentException(
                "Category name cannot exceed 100 characters.",
                nameof(name));
        }

        return normalizedName;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length > 2000)
        {
            throw new ArgumentException(
                "Category description cannot exceed 2000 characters.",
                nameof(description));
        }

        return normalizedDescription;
    }
}
using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;

namespace Feryal.Infrastructure.Products;

// Handles adding product options and creating the corresponding product variant combinations.
public sealed class ProductOptionService : IProductOptionService
{
    // Provides database operations for retrieving and updating products.
    private readonly IProductRepository _products;

    // Provides database operations for colors.
    private readonly IRepository<Color> _colors;

    // Provides database operations for sizes.
    private readonly IRepository<Size> _sizes;

    // Provides database operations for scents.
    private readonly IRepository<Scent> _scents;

    // Stores product cache invalidation events in the outbox.
    private readonly IOutboxMessageRepository _outbox;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Executes operations within database transactions.
    private readonly ITransactionManager _transactions;

    // Initializes the service with its required dependencies.
    public ProductOptionService(
        IProductRepository products,
        IRepository<Color> colors,
        IRepository<Size> sizes,
        IRepository<Scent> scents,
        IOutboxMessageRepository outbox,
        IUnitOfWork unitOfWork,
        ITransactionManager transactions)
    {
        _products = products;
        _colors = colors;
        _sizes = sizes;
        _scents = scents;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _transactions = transactions;
    }

    // Adds a color to a product, validates its details, and creates any missing variant combinations.
    public Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(request.Name, "Color");

        // Ensures the color hex code does not exceed the allowed length.
        if (request.HexCode?.Length > 20)
        {
            throw new ArgumentException(
                "HexCode cannot exceed 20 characters.");
        }

        return AddOptionAsync(
            productId,
            name,
            async (product, token) =>
            {
                // Prevents adding a color with a name that already exists for this product.
                EnsureUnique(
                    product.Colors.Select(item => item.Name),
                    name,
                    "color");

                var color = new Color(
                    product.Id,
                    name,
                    request.HexCode);

                await _colors.AddAsync(color, token);
                await _unitOfWork.SaveChangesAsync(token);

                // Creates missing variants that include the newly added color.
                await CreateMissingVariantsAsync(
                    product,
                    color,
                    null,
                    null,
                    token);

                return new AddedProductOptionResponse
                {
                    Id = color.Id,
                    ProductId = product.Id,
                    Name = color.Name,
                    HexCode = color.HexCode
                };
            },
            cancellationToken);
    }

    // Adds a size to a product and creates any missing variant combinations that include it.
    public Task<AddedProductOptionResponse?> AddSizeAsync(
        int productId,
        CreateProductSizeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(request.Name, "Size");

        return AddOptionAsync(
            productId,
            name,
            async (product, token) =>
            {
                // Prevents adding a size with a name that already exists for this product.
                EnsureUnique(
                    product.Sizes.Select(item => item.Name),
                    name,
                    "size");

                var size = new Size(product.Id, name);

                await _sizes.AddAsync(size, token);
                await _unitOfWork.SaveChangesAsync(token);

                // Creates missing variants that include the newly added size.
                await CreateMissingVariantsAsync(
                    product,
                    null,
                    size,
                    null,
                    token);

                return new AddedProductOptionResponse
                {
                    Id = size.Id,
                    ProductId = product.Id,
                    Name = size.Name
                };
            },
            cancellationToken);
    }

    // Adds a scent to a product and creates any missing variant combinations that include it.
    public Task<AddedProductOptionResponse?> AddScentAsync(
        int productId,
        CreateProductScentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(request.Name, "Scent");

        return AddOptionAsync(
            productId,
            name,
            async (product, token) =>
            {
                // Prevents adding a scent with a name that already exists for this product.
                EnsureUnique(
                    product.Scents.Select(item => item.Name),
                    name,
                    "scent");

                var scent = new Scent(product.Id, name);

                await _scents.AddAsync(scent, token);
                await _unitOfWork.SaveChangesAsync(token);

                // Creates missing variants that include the newly added scent.
                await CreateMissingVariantsAsync(
                    product,
                    null,
                    null,
                    scent,
                    token);

                return new AddedProductOptionResponse
                {
                    Id = scent.Id,
                    ProductId = product.Id,
                    Name = scent.Name
                };
            },
            cancellationToken);
    }

    // Executes a product option addition within a transaction and schedules cache invalidation.
    private async Task<AddedProductOptionResponse?> AddOptionAsync(
        int productId,
        string name,
        Func<Product, CancellationToken, Task<AddedProductOptionResponse>> add,
        CancellationToken cancellationToken)
    {
        // Validates that the product ID is positive.
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        return await _transactions.ExecuteAsync(
            async token =>
            {
                // Retrieves the product and its related details for update.
                var product =
                    await _products.GetByIdWithDetailsForUpdateAsync(
                        productId,
                        token);

                if (product is null)
                {
                    return null;
                }

                // Adds the requested option and obtains its response data.
                var result = await add(product, token);

                // Schedules invalidation of the product cache.
                AddInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return result;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Generates all missing combinations of active colors, sizes, and scents for a product.
    private async Task CreateMissingVariantsAsync(
        Product product,
        Color? addedColor,
        Size? addedSize,
        Scent? addedScent,
        CancellationToken cancellationToken)
    {
        // Retrieves the product's active color options.
        var colors = product.Colors
            .Where(item => item.IsActive)
            .ToList();

        // Retrieves the product's active size options.
        var sizes = product.Sizes
            .Where(item => item.IsActive)
            .ToList();

        // Retrieves the product's active scent options.
        var scents = product.Scents
            .Where(item => item.IsActive)
            .ToList();

        // Includes the newly added option if it is not already present in the corresponding collection.
        AddOptionIfMissing(colors, addedColor);
        AddOptionIfMissing(sizes, addedSize);
        AddOptionIfMissing(scents, addedScent);

        // Uses a null option when no colors are available.
        Color?[] colorOptions = colors.Count == 0
            ? new Color?[] { null }
            : colors.Select(item => (Color?)item).ToArray();

        // Uses a null option when no sizes are available.
        Size?[] sizeOptions = sizes.Count == 0
            ? new Size?[] { null }
            : sizes.Select(item => (Size?)item).ToArray();

        // Uses a null option when no scents are available.
        Scent?[] scentOptions = scents.Count == 0
            ? new Scent?[] { null }
            : scents.Select(item => (Scent?)item).ToArray();

        // Records existing variant combinations to prevent creating duplicates.
        var existingCombinations = product.Variants
            .Select(variant => (
                variant.ColorId,
                variant.SizeId,
                variant.ScentId))
            .ToHashSet();

        // Iterates through every possible color, size, and scent combination.
        foreach (Color? color in colorOptions)
        {
            foreach (Size? size in sizeOptions)
            {
                foreach (Scent? scent in scentOptions)
                {
                    var combination = (
                        color?.Id,
                        size?.Id,
                        scent?.Id);

                    // Skips combinations that already exist for the product.
                    if (!existingCombinations.Add(combination))
                    {
                        continue;
                    }

                    // Generates a SKU based on the product and selected option IDs.
                    string sku =
                        $"PR{product.Id}" +
                        $"-C{color?.Id.ToString() ?? "0"}" +
                        $"-SZ{size?.Id.ToString() ?? "0"}" +
                        $"-SC{scent?.Id.ToString() ?? "0"}";

                    // Adds a new variant with the selected options and initially unset selling details.
                    product.AddVariant(
                        new ProductVariant(
                            scent?.Id,
                            size?.Id,
                            color?.Id,
                            null,
                            sku,
                            null,
                            null));
                }
            }
        }

        // Persists the newly created variant combinations.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Adds a newly created option to a collection if its ID is not already present.
    private static void AddOptionIfMissing<T>(
        List<T> options,
        T? addedOption)
        where T : class
    {
        if (addedOption is null)
        {
            return;
        }

        // Retrieves the ID of the newly added option based on its concrete type.
        int addedId = addedOption switch
        {
            Color color => color.Id,
            Size size => size.Id,
            Scent scent => scent.Id,
            _ => throw new InvalidOperationException(
                "Unsupported product option type.")
        };

        // Checks whether the option already exists in the collection.
        bool alreadyExists = options.Any(option =>
        {
            int optionId = option switch
            {
                Color color => color.Id,
                Size size => size.Id,
                Scent scent => scent.Id,
                _ => throw new InvalidOperationException(
                    "Unsupported product option type.")
            };

            return optionId == addedId;
        });

        // Adds the option only when it is not already included.
        if (!alreadyExists)
        {
            options.Add(addedOption);
        }
    }

    // Creates an outbox message requesting cache invalidation for the specified product.
    private void AddInvalidation(int productId)
    {
        string payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outbox.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

    // Validates and normalizes an option name.
    private static string ValidateName(
        string name,
        string optionType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                $"{optionType} name is required.",
                nameof(name));
        }

        string normalizedName = name.Trim();

        // Ensures the normalized name does not exceed 100 characters.
        if (normalizedName.Length > 100)
        {
            throw new ArgumentException(
                $"{optionType} name cannot exceed 100 characters.");
        }

        return normalizedName;
    }

    // Checks whether the requested option name already exists, ignoring surrounding whitespace and letter casing.
    private static void EnsureUnique(
        IEnumerable<string> existingNames,
        string requestedName,
        string optionType)
    {
        if (existingNames.Any(name =>
                string.Equals(
                    name.Trim(),
                    requestedName,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                $"This {optionType} already exists for the product.");
        }
    }
}
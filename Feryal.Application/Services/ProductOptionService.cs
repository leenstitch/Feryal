
using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;

namespace Feryal.Infrastructure.Products;

public sealed class ProductOptionService : IProductOptionService
{
    private readonly IProductRepository _products;
    private readonly IRepository<Color> _colors;
    private readonly IRepository<Size> _sizes;
    private readonly IRepository<Scent> _scents;
    private readonly IOutboxMessageRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactions;

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

    public Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = ValidateName(request.Name, "Color");

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
                EnsureUnique(
                    product.Sizes.Select(item => item.Name),
                    name,
                    "size");

                var size = new Size(product.Id, name);

                await _sizes.AddAsync(size, token);
                await _unitOfWork.SaveChangesAsync(token);

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
                EnsureUnique(
                    product.Scents.Select(item => item.Name),
                    name,
                    "scent");

                var scent = new Scent(product.Id, name);

                await _scents.AddAsync(scent, token);
                await _unitOfWork.SaveChangesAsync(token);

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

    private async Task<AddedProductOptionResponse?> AddOptionAsync(
        int productId,
        string name,
        Func<Product, CancellationToken, Task<AddedProductOptionResponse>> add,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        return await _transactions.ExecuteAsync(
            async token =>
            {
                var product =
                    await _products.GetByIdWithDetailsForUpdateAsync(
                        productId,
                        token);

                if (product is null)
                {
                    return null;
                }

                var result = await add(product, token);

                AddInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return result;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    private async Task CreateMissingVariantsAsync(
        Product product,
        Color? addedColor,
        Size? addedSize,
        Scent? addedScent,
        CancellationToken cancellationToken)
    {
        var colors = product.Colors
            .Where(item => item.IsActive)
            .ToList();

        var sizes = product.Sizes
            .Where(item => item.IsActive)
            .ToList();

        var scents = product.Scents
            .Where(item => item.IsActive)
            .ToList();

        AddOptionIfMissing(colors, addedColor);
        AddOptionIfMissing(sizes, addedSize);
        AddOptionIfMissing(scents, addedScent);

        Color?[] colorOptions = colors.Count == 0
            ? new Color?[] { null }
            : colors.Select(item => (Color?)item).ToArray();

        Size?[] sizeOptions = sizes.Count == 0
            ? new Size?[] { null }
            : sizes.Select(item => (Size?)item).ToArray();

        Scent?[] scentOptions = scents.Count == 0
            ? new Scent?[] { null }
            : scents.Select(item => (Scent?)item).ToArray();

        var existingCombinations = product.Variants
            .Select(variant => (
                variant.ColorId,
                variant.SizeId,
                variant.ScentId))
            .ToHashSet();

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

                    if (!existingCombinations.Add(combination))
                    {
                        continue;
                    }

                    string sku =
                        $"PR{product.Id}" +
                        $"-C{color?.Id.ToString() ?? "0"}" +
                        $"-SZ{size?.Id.ToString() ?? "0"}" +
                        $"-SC{scent?.Id.ToString() ?? "0"}";

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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void AddOptionIfMissing<T>(
        List<T> options,
        T? addedOption)
        where T : class
    {
        if (addedOption is null)
        {
            return;
        }

        int addedId = addedOption switch
        {
            Color color => color.Id,
            Size size => size.Id,
            Scent scent => scent.Id,
            _ => throw new InvalidOperationException(
                "Unsupported product option type.")
        };

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

        if (!alreadyExists)
        {
            options.Add(addedOption);
        }
    }

    private void AddInvalidation(int productId)
    {
        string payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outbox.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

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

        if (normalizedName.Length > 100)
        {
            throw new ArgumentException(
                $"{optionType} name cannot exceed 100 characters.");
        }

        return normalizedName;
    }

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

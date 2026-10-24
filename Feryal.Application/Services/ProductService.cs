using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Images;
using Feryal.Domain.Entities;

namespace Feryal.Application.Products.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Color> _colorRepository;
    private readonly IRepository<Size> _sizeRepository;
    private readonly IRepository<Scent> _scentRepository;
    private readonly IRepository<ProductVariant> _variantRepository;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactionManager;

    public ProductService(
        IProductRepository productRepository,
        IRepository<Color> colorRepository,
        IRepository<Size> sizeRepository,
        IRepository<Scent> scentRepository,
        IRepository<ProductVariant> variantRepository,
        IOutboxMessageRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ITransactionManager transactionManager)
    {
        _productRepository = productRepository;
        _colorRepository = colorRepository;
        _sizeRepository = sizeRepository;
        _scentRepository = scentRepository;
        _variantRepository = variantRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _transactionManager = transactionManager;
    }

    public async Task<PagedResponse<ProductResponse>> GetPagedProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var (products, totalCount) =
            await _productRepository.GetPagedAsync(
                parameters,
                cancellationToken);

        var items = products
            .Select(MapProduct)
            .ToList();

        return new PagedResponse<ProductResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = Math.Max(1, parameters.PageNumber),
            PageSize = Math.Clamp(parameters.PageSize, 1, 100)
        };
    }
    private static ProductVariantResponse MapVariant(
        ProductVariant variant)
    {
        return new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,

            ScentId = variant.ScentId,
            ScentName = variant.Scent?.Name,

            SizeId = variant.SizeId,
            SizeName = variant.Size?.Name,

            ColorId = variant.ColorId,
            ColorName = variant.Color?.Name,
            ColorHexCode = variant.Color?.HexCode,

            SKU = variant.SKU,
            Description = variant.Description,

            Price = variant.Price,
            StockQuantity = variant.StockQuantity,

            IsActive = variant.IsActive,
            IsConfigured = variant.IsConfigured,
            HasCompleteSellingDetails =
                variant.HasCompleteSellingDetails,
            

            DiscountPercentage = variant.DiscountPercentage,
            DiscountAmount = variant.DiscountAmount,
            FinalPrice = variant.FinalPrice
        };
    }

    private static ProductResponse MapProduct(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            ProductGroupId = product.ProductGroupId,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,

            Colors = product.Colors
                .Select(color => new ProductColorResponse
                {
                    Id = color.Id,
                    Name = color.Name,
                    HexCode = color.HexCode
                })
                .ToList(),

            Sizes = product.Sizes
                .Select(size => new ProductSizeResponse
                {
                    Id = size.Id,
                    Name = size.Name
                })
                .ToList(),

            Scents = product.Scents
                .Select(scent => new ProductScentResponse
                {
                    Id = scent.Id,
                    Name = scent.Name
                })
                .ToList(),

            Variants = product.Variants
                .Select(MapVariant)
                .ToList(),

            Images = product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new ProductImageResponse
                {
                    Id = image.Id,
                    ProductId = image.ProductId,
                    ImageUrl = image.ImageUrl,
                    IsPrimary = image.IsPrimary,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        };
    }

    public async Task<PagedResponse<ProductResponse>> GetPagedActiveProductsAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var (products, totalCount) =
            await _productRepository.GetPagedActiveAsync(
                parameters,
                cancellationToken);

        return new PagedResponse<ProductResponse>
        {
            Items = products.Select(MapProduct).ToList(),
            TotalCount = totalCount,
            PageNumber = Math.Max(1, parameters.PageNumber),
            PageSize = Math.Clamp(parameters.PageSize, 1, 100)
        };
    }

    public async Task<ProductResponse?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "Product ID must be greater than zero.");
        }

        var product =
            await _productRepository.GetByIdWithDetailsAsync(
                productId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        return MapProduct(product);
    }

    public async Task<CreatedProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Colors);
        ArgumentNullException.ThrowIfNull(request.Sizes);
        ArgumentNullException.ThrowIfNull(request.Scents);

        ValidateUniqueNames(
            request.Colors.Select(x => x.Name),
            "color");

        ValidateUniqueNames(
            request.Sizes.Select(x => x.Name),
            "size");

        ValidateUniqueNames(
            request.Scents.Select(x => x.Name),
            "scent");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Product name is required.");
        }

        string name = request.Name.Trim();

        if (name.Length > 200)
        {
            throw new ArgumentException(
                "Product name cannot exceed 200 characters.");
        }

        if (request.Description?.Length > 2000)
        {
            throw new ArgumentException(
                "Product description cannot exceed 2000 characters.");
        }

        return await _transactionManager.ExecuteAsync<CreatedProductResponse>(
            async token =>
            {
                if (!await _productRepository.IsCategoryAvailableAsync(
                        request.CategoryId,
                        token))
                {
                    throw new InvalidOperationException(
                        "The selected category does not exist or is inactive.");
                }

                if (!await _productRepository.IsProductGroupAvailableAsync(
                        request.ProductGroupId,
                        token))
                {
                    throw new InvalidOperationException(
                        "The selected product group does not exist or is inactive.");
                }

                if (await _productRepository.ExistsByNameAsync(
                        name,
                        null,
                        token))
                {
                    throw new InvalidOperationException(
                        "A product with this name already exists.");
                }

                var product = new Product(
                    request.CategoryId,
                    request.ProductGroupId,
                    name,
                    request.Description);

                await _productRepository.AddAsync(product, token);

                await _unitOfWork.SaveChangesAsync(token);

                var colors = request.Colors
                    .Select(x => new Color(
                        product.Id,
                        x.Name.Trim(),
                        x.HexCode))
                    .ToList();

                var sizes = request.Sizes
                    .Select(x => new Size(
                        product.Id,
                        x.Name.Trim()))
                    .ToList();

                var scents = request.Scents
                    .Select(x => new Scent(
                        product.Id,
                        x.Name.Trim()))
                    .ToList();

                foreach (var color in colors)
                {
                    await _colorRepository.AddAsync(color, token);
                }

                foreach (var size in sizes)
                {
                    await _sizeRepository.AddAsync(size, token);
                }

                foreach (var scent in scents)
                {
                    await _scentRepository.AddAsync(scent, token);
                }

                await _unitOfWork.SaveChangesAsync(token);

                Color?[] colorOptions = colors.Count == 0
                    ? new Color?[] { null }
                    : colors.Select(x => (Color?)x).ToArray();

                Size?[] sizeOptions = sizes.Count == 0
                    ? new Size?[] { null }
                    : sizes.Select(x => (Size?)x).ToArray();

                Scent?[] scentOptions = scents.Count == 0
                    ? new Scent?[] { null }
                    : scents.Select(x => (Scent?)x).ToArray();

                foreach (Color? color in colorOptions)
                {
                    foreach (Size? size in sizeOptions)
                    {
                        foreach (Scent? scent in scentOptions)
                        {
                            var variant = new ProductVariant(
                                scent?.Id,
                                size?.Id,
                                color?.Id,
                                null,
                                GenerateSku(),
                                null,
                                null);

                            // New combinations are not purchasable until configured
                            // and explicitly activated by the administrator.
                            variant.Deactivate();
                            variant.HideFromCustomers();
                            product.AddVariant(variant);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(token);

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return new CreatedProductResponse
                {
                    Id = product.Id,
                    CategoryId = product.CategoryId,
                    ProductGroupId = product.ProductGroupId,
                    Name = product.Name,
                    Description = product.Description,
                    IsActive = product.IsActive,

                    Colors = colors
                        .Select(x => new ProductColorResponse
                        {
                            Id = x.Id,
                            Name = x.Name,
                            HexCode = x.HexCode,
                            IsActive = x.IsActive
                        })
                        .ToList(),

                    Sizes = sizes
                        .Select(x => new ProductSizeResponse
                        {
                            Id = x.Id,
                            Name = x.Name,
                            IsActive = x.IsActive
                        })
                        .ToList(),

                    Scents = scents
                        .Select(x => new ProductScentResponse
                        {
                            Id = x.Id,
                            Name = x.Name,
                            IsActive = x.IsActive
                        })
                        .ToList(),

                    Variants = product.Variants
                        .Select(x => new ProductVariantResponse
                        {
                            Id = x.Id,
                            ProductId = x.ProductId,

                            ColorId = x.ColorId,
                            ColorName = colors
                                .FirstOrDefault(c => c.Id == x.ColorId)?.Name,
                            ColorHexCode = colors
                                .FirstOrDefault(c => c.Id == x.ColorId)?.HexCode,

                            SizeId = x.SizeId,
                            SizeName = sizes
                                .FirstOrDefault(s => s.Id == x.SizeId)?.Name,

                            ScentId = x.ScentId,
                            ScentName = scents
                                .FirstOrDefault(s => s.Id == x.ScentId)?.Name,

                            SKU = x.SKU,
                            Description = x.Description,
                            Price = x.Price,
                            StockQuantity = x.StockQuantity,
                            IsActive = x.IsActive,
                            IsConfigured = x.IsConfigured
                        })
                        .ToList()
                };
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<ProductResponse?> UpdateAsync(
        int productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (!request.CategoryId.HasValue || request.CategoryId.Value <= 0)
        {
            throw new ArgumentException("CategoryId must be a positive integer.");
        }

        if (!request.ProductGroupId.HasValue || request.ProductGroupId.Value <= 0)
        {
            throw new ArgumentException("ProductGroupId must be a positive integer.");
        }

        if (request.Name.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("Name is required and must be a string.");
        }

        string? requestName = request.Name.GetString();
        if (string.IsNullOrWhiteSpace(requestName))
        {
            throw new ArgumentException("Product name is required.");
        }

        string name = requestName.Trim();
        if (name.Length > 200)
        {
            throw new ArgumentException("Product name cannot exceed 200 characters.");
        }

        string? description;
        if (request.Description.ValueKind == JsonValueKind.Undefined ||
            request.Description.ValueKind == JsonValueKind.Null)
        {
            description = null;
        }
        else if (request.Description.ValueKind == JsonValueKind.String)
        {
            description = request.Description.GetString();
        }
        else
        {
            throw new ArgumentException("Description must be a string or null.");
        }

        if (description?.Length > 2000)
        {
            throw new ArgumentException("Product description cannot exceed 2000 characters.");
        }

        return await _transactionManager.ExecuteAsync<ProductResponse?>(
            async token =>
            {
                var product = await _productRepository.GetByIdAsync(productId, token);
                if (product is null)
                {
                    return null;
                }

                int categoryId = request.CategoryId.Value;
                int productGroupId = request.ProductGroupId.Value;

                if (!await _productRepository.IsCategoryAvailableAsync(categoryId, token))
                {
                    throw new InvalidOperationException(
                        "The selected category does not exist or is inactive.");
                }

                if (!await _productRepository.IsProductGroupAvailableAsync(productGroupId, token))
                {
                    throw new InvalidOperationException(
                        "The selected product group does not exist or is inactive.");
                }

                if (await _productRepository.ExistsByNameAsync(name, productId, token))
                {
                    throw new InvalidOperationException(
                        "A product with this name already exists.");
                }

                product.Update(categoryId, productGroupId, name, description);
                _productRepository.Update(product);
                AddProductCacheInvalidation(product.Id);
                await _unitOfWork.SaveChangesAsync(token);

                var updatedProduct = await _productRepository.GetByIdWithDetailsAsync(
                    productId, token);

                return updatedProduct is null ? null : MapProductResponse(updatedProduct);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }


    public async Task<ProductResponse?> PatchAsync(
        int productId,
        PatchProductRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "Product ID must be greater than zero.");
        }

        return await _transactionManager.ExecuteAsync<ProductResponse?>(
            async token =>
            {
                var product = await _productRepository.GetByIdAsync(
                    productId,
                    token);

                if (product is null)
                {
                    return null;
                }

                int categoryId = product.CategoryId;
                int productGroupId = product.ProductGroupId;
                string name = product.Name;
                string? description = product.Description;

                // CategoryId
                // Undefined = keep the current category.
                // Otherwise, require a positive integer.
                if (request.CategoryId.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.CategoryId.ValueKind != JsonValueKind.Number ||
                        !request.CategoryId.TryGetInt32(out int newCategoryId) ||
                        newCategoryId <= 0)
                    {
                        throw new ArgumentException(
                            "CategoryId must be a positive integer.");
                    }

                    if (!await _productRepository.IsCategoryAvailableAsync(
                            newCategoryId,
                            token))
                    {
                        throw new ArgumentException(
                            "The selected category does not exist or is inactive.");
                    }

                    categoryId = newCategoryId;
                }

                // ProductGroupId
                // Undefined = keep the current product group.
                // Otherwise, require a positive integer.
                if (request.ProductGroupId.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.ProductGroupId.ValueKind != JsonValueKind.Number ||
                        !request.ProductGroupId.TryGetInt32(out int newProductGroupId) ||
                        newProductGroupId <= 0)
                    {
                        throw new ArgumentException(
                            "ProductGroupId must be a positive integer.");
                    }

                    if (!await _productRepository.IsProductGroupAvailableAsync(
                            newProductGroupId,
                            token))
                    {
                        throw new ArgumentException(
                            "The selected product group does not exist or is inactive.");
                    }

                    productGroupId = newProductGroupId;
                }

                // Name
                // Undefined = keep the current name.
                // Null, non-string, empty or whitespace = invalid.
                if (request.Name.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Name.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException(
                            "Name must be a non-empty string.");
                    }

                    string? newName = request.Name.GetString();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        throw new ArgumentException(
                            "Name cannot be empty.");
                    }

                    newName = newName.Trim();

                    if (newName.Length > 200)
                    {
                        throw new ArgumentException(
                            "Name cannot exceed 200 characters.");
                    }

                    bool nameExists = await _productRepository.ExistsByNameAsync(
                        newName,
                        productId,
                        token);

                    if (nameExists)
                    {
                        throw new InvalidOperationException(
                            "A product with this name already exists.");
                    }

                    name = newName;
                }

                // Description
                // Undefined = keep the current description.
                // Null = clear the description.
                // String = replace the description.
                if (request.Description.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Description.ValueKind == JsonValueKind.Null)
                    {
                        description = null;
                    }
                    else if (request.Description.ValueKind == JsonValueKind.String)
                    {
                        description = request.Description.GetString();

                        if (description?.Length > 2000)
                        {
                            throw new ArgumentException(
                                "Description cannot exceed 2000 characters.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Description must be a string or null.");
                    }
                }

                product.Update(
                    categoryId,
                    productGroupId,
                    name,
                    description);

                _productRepository.Update(product);

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                var updatedProduct =
                    await _productRepository.GetByIdWithDetailsAsync(
                        productId,
                        token);

                return updatedProduct is null
                    ? null
                    : MapProductResponse(updatedProduct);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }


    public async Task<AddedProductOptionResponse?> AddColorAsync(
        int productId,
        CreateProductColorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Color name is required.");
        }

        string name = request.Name.Trim();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Color name cannot exceed 100 characters.");
        }

        if (request.HexCode?.Length > 20)
        {
            throw new ArgumentException(
                "HexCode cannot exceed 20 characters.");
        }

        return await _transactionManager.ExecuteAsync<AddedProductOptionResponse?>(
            async token =>
            {
                var product = await _productRepository
                    .GetByIdWithDetailsForUpdateAsync(
                        productId,
                        token);

                if (product is null)
                {
                    return null;
                }

                if (product.Colors.Any(color =>
                        string.Equals(
                            color.Name.Trim(),
                            name,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "This color already exists for the product.");
                }

                var color = new Color(
                    product.Id,
                    name,
                    request.HexCode);

                await _colorRepository.AddAsync(color, token);

                await _unitOfWork.SaveChangesAsync(token);

                Size?[] sizes = product.Sizes
                    .Where(size => size.IsActive)
                    .Select(size => (Size?)size)
                    .ToArray();

                if (sizes.Length == 0)
                {
                    sizes = new Size?[] { null };
                }

                Scent?[] scents = product.Scents
                    .Where(scent => scent.IsActive)
                    .Select(scent => (Scent?)scent)
                    .ToArray();

                if (scents.Length == 0)
                {
                    scents = new Scent?[] { null };
                }

                foreach (Size? size in sizes)
                {
                    foreach (Scent? scent in scents)
                    {
                        if (product.Variants.Any(variant =>
                                variant.ColorId == color.Id &&
                                variant.SizeId == size?.Id &&
                                variant.ScentId == scent?.Id))
                        {
                            continue;
                        }

                        var variant = new ProductVariant(
                            scent?.Id,
                            size?.Id,
                            color.Id,
                            null,
                            GenerateSku(),
                            null,
                            null);

                        variant.Deactivate();
                        variant.HideFromCustomers();
                        product.AddVariant(variant);
                    }
                }

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                var updatedProduct =
                    await _productRepository.GetByIdWithDetailsAsync(
                        productId,
                        token);

                if (updatedProduct is null)
                {
                    throw new InvalidOperationException(
                        "The product could not be reloaded after adding the color.");
                }

                return new AddedProductOptionResponse
                {
                    Id = color.Id,
                    ProductId = product.Id,
                  
                    Name = color.Name,
                    HexCode = color.HexCode,


                    CreatedVariants = updatedProduct.Variants
                        .Where(variant => variant.ColorId == color.Id)
                        .Select(MapVariantResponse)
                        .ToList()
                };
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<AddedProductOptionResponse?> AddSizeAsync(
        int productId,
        CreateProductSizeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Size name is required.");
        }

        string name = request.Name.Trim();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Size name cannot exceed 100 characters.");
        }

        return await _transactionManager.ExecuteAsync<AddedProductOptionResponse?>(
            async token =>
            {
                var product = await _productRepository
                    .GetByIdWithDetailsForUpdateAsync(
                        productId,
                        token);

                if (product is null)
                {
                    return null;
                }

                if (product.Sizes.Any(size =>
                        string.Equals(
                            size.Name.Trim(),
                            name,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "This size already exists for the product.");
                }

                var size = new Size(product.Id, name);

                await _sizeRepository.AddAsync(size, token);

                await _unitOfWork.SaveChangesAsync(token);

                Color?[] colors = product.Colors
                    .Where(color => color.IsActive)
                    .Select(color => (Color?)color)
                    .ToArray();

                if (colors.Length == 0)
                {
                    colors = new Color?[] { null };
                }

                Scent?[] scents = product.Scents
                    .Where(scent => scent.IsActive)
                    .Select(scent => (Scent?)scent)
                    .ToArray();

                if (scents.Length == 0)
                {
                    scents = new Scent?[] { null };
                }

                foreach (Color? color in colors)
                {
                    foreach (Scent? scent in scents)
                    {
                        if (product.Variants.Any(variant =>
                                variant.ColorId == color?.Id &&
                                variant.SizeId == size.Id &&
                                variant.ScentId == scent?.Id))
                        {
                            continue;
                        }

                        var variant = new ProductVariant(
                            scent?.Id,
                            size.Id,
                            color?.Id,
                            null,
                            GenerateSku(),
                            null,
                            null);

                        variant.Deactivate();
                        variant.HideFromCustomers();
                        product.AddVariant(variant);
                    }
                }

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                var updatedProduct =
                    await _productRepository.GetByIdWithDetailsAsync(
                        productId,
                        token);

                if (updatedProduct is null)
                {
                    throw new InvalidOperationException(
                        "The product could not be reloaded after adding the size.");
                }

                return new AddedProductOptionResponse
                {
                    Id = size.Id,
                    ProductId = product.Id,
                   
                    Name = size.Name,
                    HexCode = null,


                    CreatedVariants = updatedProduct.Variants
                        .Where(variant => variant.SizeId == size.Id)
                        .Select(MapVariantResponse)
                        .ToList()
                };
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<AddedProductOptionResponse?> AddScentAsync(
        int productId,
        CreateProductScentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Scent name is required.");
        }

        string name = request.Name.Trim();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Scent name cannot exceed 100 characters.");
        }

        return await _transactionManager.ExecuteAsync<AddedProductOptionResponse?>(
            async token =>
            {
                var product = await _productRepository
                    .GetByIdWithDetailsForUpdateAsync(
                        productId,
                        token);

                if (product is null)
                {
                    return null;
                }

                if (product.Scents.Any(scent =>
                        string.Equals(
                            scent.Name.Trim(),
                            name,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "This scent already exists for the product.");
                }

                var scent = new Scent(product.Id, name);

                await _scentRepository.AddAsync(scent, token);

                await _unitOfWork.SaveChangesAsync(token);

                Color?[] colors = product.Colors
                    .Where(color => color.IsActive)
                    .Select(color => (Color?)color)
                    .ToArray();

                if (colors.Length == 0)
                {
                    colors = new Color?[] { null };
                }

                Size?[] sizes = product.Sizes
                    .Where(size => size.IsActive)
                    .Select(size => (Size?)size)
                    .ToArray();

                if (sizes.Length == 0)
                {
                    sizes = new Size?[] { null };
                }

                foreach (Color? color in colors)
                {
                    foreach (Size? size in sizes)
                    {
                        if (product.Variants.Any(variant =>
                                variant.ColorId == color?.Id &&
                                variant.SizeId == size?.Id &&
                                variant.ScentId == scent.Id))
                        {
                            continue;
                        }

                        var variant = new ProductVariant(
                            scent.Id,
                            size?.Id,
                            color?.Id,
                            null,
                            GenerateSku(),
                            null,
                            null);

                        variant.Deactivate();
                        variant.HideFromCustomers();
                        product.AddVariant(variant);
                    }
                }

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                var updatedProduct =
                    await _productRepository.GetByIdWithDetailsAsync(
                        productId,
                        token);

                if (updatedProduct is null)
                {
                    throw new InvalidOperationException(
                        "The product could not be reloaded after adding the scent.");
                }

                return new AddedProductOptionResponse
                {
                    Id = scent.Id,
                    ProductId = product.Id,
                   
                    Name = scent.Name,
                    HexCode = null,


                    CreatedVariants = updatedProduct.Variants
                        .Where(variant => variant.ScentId == scent.Id)
                        .Select(MapVariantResponse)
                        .ToList()
                };
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<bool> ActivateAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await ChangeProductStatusAsync(
            productId,
            true,
            cancellationToken);
    }

    public async Task<bool> DeactivateAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await ChangeProductStatusAsync(
            productId,
            false,
            cancellationToken);
    }

    public async Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId));
        }

        // Price
        if (request.Price.ValueKind != JsonValueKind.Undefined &&
            request.Price.ValueKind != JsonValueKind.Null)
        {
            if (request.Price.ValueKind != JsonValueKind.Number ||
                !request.Price.TryGetDecimal(out decimal price) ||
                price <= 0)
            {
                throw new ArgumentException(
                    "Price must be a number greater than zero.");
            }
        }

        // StockQuantity
        if (request.StockQuantity.ValueKind != JsonValueKind.Undefined &&
            request.StockQuantity.ValueKind != JsonValueKind.Null)
        {
            if (request.StockQuantity.ValueKind != JsonValueKind.Number ||
                !request.StockQuantity.TryGetInt32(out int stockQuantity) ||
                stockQuantity < 0)
            {
                throw new ArgumentException(
                    "StockQuantity must be a non-negative integer.");
            }
        }

        return await _transactionManager.ExecuteAsync<ProductVariantResponse?>(
            async token =>
            {
                var variant = await _variantRepository.GetByIdAsync(
                    variantId,
                    token);

                if (variant is null)
                {
                    return null;
                }

                //string? description = variant.Description;

                //if (request.Description.ValueKind == JsonValueKind.Null)
                //{
                //    description = null;
                //}
                //else if (request.Description.ValueKind == JsonValueKind.String)
                //{
                //    description = request.Description.GetString();
                //}
                //else if (request.Description.ValueKind != JsonValueKind.Undefined)
                //{
                //    throw new ArgumentException(
                //        "Variant description must be a string or null.");
                //}

                //if (description?.Length > 2000)
                //{
                //    throw new ArgumentException(
                //        "Variant description cannot exceed 2000 characters.");
                //}

                string? description = null;
                bool descriptionProvided =
                    request.Description.ValueKind != JsonValueKind.Undefined;

                if (descriptionProvided)
                {
                    if (request.Description.ValueKind == JsonValueKind.Null)
                    {
                        description = null;
                    }
                    else if (request.Description.ValueKind == JsonValueKind.String)
                    {
                        description = request.Description.GetString();

                        if (description?.Length > 2000)
                        {
                            throw new ArgumentException(
                                "Variant description cannot exceed 2000 characters.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Description must be a string or null.");
                    }
                }

                decimal? price = null;
                bool priceProvided =
                    request.Price.ValueKind != JsonValueKind.Undefined;

                if (priceProvided)
                {
                    if (request.Price.ValueKind != JsonValueKind.Number ||
                        !request.Price.TryGetDecimal(out decimal parsedPrice) ||
                        parsedPrice <= 0)
                    {
                        throw new ArgumentException(
                            "Price must be a number greater than zero.");
                    }

                    price = parsedPrice;
                }

                int? stockQuantity = null;
                bool stockProvided =
                    request.StockQuantity.ValueKind != JsonValueKind.Undefined;

                if (stockProvided)
                {
                    if (request.StockQuantity.ValueKind != JsonValueKind.Number ||
                        !request.StockQuantity.TryGetInt32(out int parsedStock) ||
                        parsedStock < 0)
                    {
                        throw new ArgumentException(
                            "StockQuantity must be a non-negative integer.");
                    }

                    stockQuantity = parsedStock;
                }
                decimal discountPercentage = variant.DiscountPercentage;

                if (request.DiscountPercentage.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.DiscountPercentage.ValueKind != JsonValueKind.Number ||
                        !request.DiscountPercentage.TryGetDecimal(out decimal parsedDiscount) ||
                        parsedDiscount < 0 ||
                        parsedDiscount > 100)
                    {
                        throw new ArgumentException(
                            "DiscountPercentage must be a number between 0 and 100.");
                    }

                    discountPercentage = parsedDiscount;
                }
                variant.UpdateDetails(
       descriptionProvided ? description : variant.Description,
       priceProvided ? price : variant.Price,
       stockProvided ? stockQuantity : variant.StockQuantity,
       discountPercentage);

                _variantRepository.Update(variant);

                AddProductCacheInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                var product =
                    await _productRepository.GetByIdWithDetailsAsync(
                        variant.ProductId,
                        token);

                var updatedVariant = product?.Variants
                    .FirstOrDefault(x => x.Id == variantId);

                return updatedVariant is null
                    ? null
                    : MapVariantResponse(updatedVariant);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<bool> ActivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return await ChangeVariantStatusAsync(
            variantId,
            true,
            cancellationToken);
    }

    public async Task<bool> DeactivateVariantAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return await ChangeVariantStatusAsync(
            variantId,
            false,
            cancellationToken);
    }

    private async Task<bool> ChangeProductStatusAsync(
        int productId,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId));
        }

        return await _transactionManager.ExecuteAsync<bool>(
            async token =>
            {
                var product = await _productRepository.GetByIdAsync(
                    productId,
                    token);

                if (product is null)
                {
                    return false;
                }

                if (product.IsActive == activate)
                {
                    return true;
                }

                if (activate)
                {
                    product.Activate();
                }
                else
                {
                    product.Deactivate();
                }

                _productRepository.Update(product);

                AddProductCacheInvalidation(product.Id);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    private async Task<bool> ChangeVariantStatusAsync(
        int variantId,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId));
        }

        return await _transactionManager.ExecuteAsync<bool>(
            async token =>
            {
                var variant = await _variantRepository.GetByIdAsync(
                    variantId,
                    token);

                if (variant is null)
                {
                    return false;
                }

                if (variant.IsActive == activate)
                {
                    return true;
                }

                if (activate)
                {
                    variant.Activate();
                }
                else
                {
                    variant.Deactivate();
                }

                _variantRepository.Update(variant);

                AddProductCacheInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<List<ProductVariantResponse>?> GetVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId));
        }

        var variants =
            await _productRepository.GetVariantsByProductIdAsync(
                productId,
                cancellationToken);

        if (variants is null)
        {
            return null;
        }

        return variants
            .Select(MapVariantResponse)
            .ToList();
    }

    public Task<bool> ShowVariantAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeVariantVisibilityAsync(
            variantId,
            true,
            cancellationToken);
    }

    public Task<bool> HideVariantAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeVariantVisibilityAsync(
            variantId,
            false,
            cancellationToken);
    }

    private async Task<bool> ChangeVariantVisibilityAsync(
        int variantId,
        bool show,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId));
        }

        return await _transactionManager.ExecuteAsync<bool>(
            async token =>
            {
                var variant = await _variantRepository.GetByIdAsync(
                    variantId,
                    token);

                if (variant is null)
                {
                    return false;
                }

                if (variant.IsConfigured == show)
                {
                    return true;
                }

                if (show)
                {
                    variant.ShowToCustomers();
                }
                else
                {
                    variant.HideFromCustomers();
                }

                _variantRepository.Update(variant);

                AddProductCacheInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<List<ProductVariantResponse>?> GetVisibleVariantsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId));
        }

        var variants =
            await _productRepository.GetVisibleVariantsByProductIdAsync(
                productId,
                cancellationToken);

        if (variants is null)
        {
            return null;
        }

        return variants
            .Select(MapVariantResponse)
            .ToList();
    }

    private void AddProductCacheInvalidation(int productId)
    {
        var cacheEvent =
            new ProductCacheInvalidationRequested(productId);

        string payload = JsonSerializer.Serialize(cacheEvent);

        var outboxMessage = new OutboxMessage(
            typeof(ProductCacheInvalidationRequested).FullName!,
            payload);

        _outboxRepository.Add(outboxMessage);
    }

    private static string GenerateSku()
    {
        return $"FER-{Guid.NewGuid():N}"[..20]
            .ToUpperInvariant();
    }

    private static void ValidateUniqueNames(
        IEnumerable<string> names,
        string optionType)
    {
        List<string> namesList = names.ToList();

        List<string> normalizedNames = namesList
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToList();

        if (normalizedNames.Count != namesList.Count)
        {
            throw new ArgumentException(
                $"One or more {optionType} names are empty.");
        }

        bool hasDuplicates = normalizedNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != normalizedNames.Count;

        if (hasDuplicates)
        {
            throw new ArgumentException(
                $"Duplicate {optionType} names are not allowed within the same product.");
        }
    }

    private static ProductResponse MapProductResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            ProductGroupId = product.ProductGroupId,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,

            Colors = product.Colors
                .OrderBy(color => color.Name)
                .Select(color => new ProductColorResponse
                {
                    Id = color.Id,
                    Name = color.Name,
                    HexCode = color.HexCode,
                    IsActive = color.IsActive
                })
                .ToList(),

            Sizes = product.Sizes
                .OrderBy(size => size.Name)
                .Select(size => new ProductSizeResponse
                {
                    Id = size.Id,
                    Name = size.Name,
                    IsActive = size.IsActive
                })
                .ToList(),

            Scents = product.Scents
                .OrderBy(scent => scent.Name)
                .Select(scent => new ProductScentResponse
                {
                    Id = scent.Id,
                    Name = scent.Name,
                    IsActive = scent.IsActive
                })
                .ToList(),

            Variants = product.Variants
                .OrderBy(variant => variant.Id)
                .Select(MapVariantResponse)
                .ToList(),

            Images = product.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new ProductImageResponse
                {
                    Id = image.Id,
                    ProductId = image.ProductId,
                    ImageUrl = image.ImageUrl,
                    IsPrimary = image.IsPrimary,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        };
    }

    private static ProductVariantResponse MapVariantResponse(
        ProductVariant variant)
    {
        return new ProductVariantResponse
        {
            Id = variant.Id,
            ProductId = variant.ProductId,

            ColorId = variant.ColorId,
            ColorName = variant.Color?.Name,
            ColorHexCode = variant.Color?.HexCode,

            SizeId = variant.SizeId,
            SizeName = variant.Size?.Name,

            ScentId = variant.ScentId,
            ScentName = variant.Scent?.Name,

            SKU = variant.SKU,
            Description = variant.Description,
            Price = variant.Price,
            StockQuantity = variant.StockQuantity,
            IsActive = variant.IsActive,
            IsConfigured = variant.IsConfigured
        };
    }
}
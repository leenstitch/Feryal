using System.Data;
using System.Text.Json;
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Request;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;

namespace Feryal.Infrastructure.Products;

// Manages product variant retrieval, updates, status changes, and customer visibility.
public sealed class ProductVariantService : IProductVariantService
{
    // Provides database operations for products and their variants.
    private readonly IProductRepository _products;

    // Stores cache invalidation events in the outbox.
    private readonly IOutboxMessageRepository _outbox;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Executes operations within database transactions.
    private readonly ITransactionManager _transactions;

    // Initializes the service with its required dependencies.
    public ProductVariantService(
        IProductRepository products,
        IOutboxMessageRepository outbox,
        IUnitOfWork unitOfWork,
        ITransactionManager transactions)
    {
        _products = products;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _transactions = transactions;
    }

    // Retrieves a paginated list of variants for a product.
    public async ValueTask<PagedResponse<ProductVariantResponse>?>
        GetVariantsAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        var result = await _products.GetPagedVariantsByProductIdAsync(
            productId,
            parameters,
            cancellationToken);

        // Return null when the requested product does not exist.
        if (!result.ProductExists)
        {
            return null;
        }

        // Convert variant entities into response DTOs.
        var items = result.Items
            .Select(MapVariant)
            .ToList();

        return new PagedResponse<ProductVariantResponse>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = result.TotalCount
        };
    }

    // Retrieves a paginated list of available variants for a product.
    public async ValueTask<PagedResponse<ProductVariantResponse>?>
        GetAvailableVariantsAsync(
            int productId,
            ProductVariantQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        var result =
            await _products.GetPagedAvailableVariantsByProductIdAsync(
                productId,
                parameters,
                cancellationToken);

        // Return null when the requested product does not exist.
        if (!result.ProductExists)
        {
            return null;
        }

        // Convert the available variant entities into response DTOs.
        var items = result.Items
            .Select(MapVariant)
            .ToList();

        return new PagedResponse<ProductVariantResponse>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = result.TotalCount
        };
    }


    // Updates the details of an existing product variant.
    public async Task<ProductVariantResponse?> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variantId),
                "Variant ID must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(request);

        return await _transactions.ExecuteAsync(
            async token =>
            {
                // Retrieve the variant for update within the transaction.
                var variant = await _products.GetVariantByIdForUpdateAsync(
                    variantId,
                    token);

                if (variant is null)
                {
                    return null;
                }

                // Description:
                // Undefined = keep current value.
                // Null = clear description.
                // String = replace description.
                string? description = variant.Description;

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

                // Price:
                // Undefined = keep current value.
                // Null = clear price.
                // Number = set a positive price.
                decimal? price = variant.Price;

                if (request.Price.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.Price.ValueKind == JsonValueKind.Null)
                    {
                        price = null;
                    }
                    else if (request.Price.ValueKind == JsonValueKind.Number &&
                             request.Price.TryGetDecimal(out decimal newPrice))
                    {
                        if (newPrice <= 0 || newPrice > 9999999999999999m)
                        {
                            throw new ArgumentException(
                                "Price must be greater than zero and within the allowed range.");
                        }

                        price = newPrice;
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Price must be a valid number or null.");
                    }
                }

                // StockQuantity:
                // Undefined = keep current value.
                // Null = clear quantity.
                // Number = set a non-negative integer.
                int? stockQuantity = variant.StockQuantity;

                if (request.StockQuantity.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.StockQuantity.ValueKind == JsonValueKind.Null)
                    {
                        stockQuantity = null;
                    }
                    else if (request.StockQuantity.ValueKind == JsonValueKind.Number &&
                             request.StockQuantity.TryGetInt32(out int newStockQuantity))
                    {
                        if (newStockQuantity < 0)
                        {
                            throw new ArgumentException(
                                "Stock quantity cannot be negative.");
                        }

                        stockQuantity = newStockQuantity;
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Stock quantity must be a non-negative integer or null.");
                    }
                }

                // DiscountPercentage:
                // Undefined = keep current value.
                // Number = set a value between 0 and 100.
                // Null and other types are invalid.
                decimal discountPercentage = variant.DiscountPercentage;

                if (request.DiscountPercentage.ValueKind != JsonValueKind.Undefined)
                {
                    if (request.DiscountPercentage.ValueKind != JsonValueKind.Number ||
                        !request.DiscountPercentage.TryGetDecimal(
                            out decimal newDiscountPercentage))
                    {
                        throw new ArgumentException(
                            "Discount percentage must be a number between 0 and 100.");
                    }

                    if (newDiscountPercentage < 0 || newDiscountPercentage > 100)
                    {
                        throw new ArgumentException(
                            "Discount percentage must be between 0 and 100.");
                    }

                    discountPercentage = newDiscountPercentage;
                }

                // Apply the validated values to the variant.
                variant.UpdateDetails(
                    description,
                    price,
                    stockQuantity,
                    discountPercentage);

                // Request cache invalidation for the related product.
                AddInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return MapVariant(variant);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }


    // Activates a product variant.
    public Task<bool> ActivateAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.Activate(),
            cancellationToken);
    }

    // Deactivates a product variant.
    public Task<bool> DeactivateAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.Deactivate(),
            cancellationToken);
    }

    // Makes a product variant visible to customers.
    public Task<bool> ShowAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.ShowToCustomers(),
            cancellationToken);
    }

    // Hides a product variant from customers.
    public Task<bool> HideAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.HideFromCustomers(),
            cancellationToken);
    }

    // Applies the requested state change to a variant within a transaction.
    private async Task<bool> ChangeStateAsync(
        int variantId,
        Action<ProductVariant> change,
        CancellationToken cancellationToken)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(variantId));
        }

        return await _transactions.ExecuteAsync(
            async token =>
            {
                // Retrieve the variant for update.
                var variant =
                    await _products.GetVariantByIdForUpdateAsync(
                        variantId,
                        token);

                if (variant is null)
                {
                    return false;
                }

                // Execute the supplied activation, deactivation, or visibility action.
                change(variant);

                // Request cache invalidation for the related product.
                AddInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Creates an outbox message requesting cache invalidation for a product.
    private void AddInvalidation(int productId)
    {
        var payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outbox.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

    // Maps a product variant entity to its response DTO.
    public static ProductVariantResponse MapVariant(
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
            DiscountPercentage = variant.DiscountPercentage,
            DiscountAmount = variant.DiscountAmount,
            FinalPrice = variant.FinalPrice,

            HasCompleteSellingDetails = variant.HasCompleteSellingDetails,
            StockQuantity = variant.StockQuantity,

            IsActive = variant.IsActive,
            IsConfigured = variant.IsConfigured,

        };
    }
}
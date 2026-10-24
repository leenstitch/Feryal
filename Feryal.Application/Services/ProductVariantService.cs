
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

public sealed class ProductVariantService : IProductVariantService
{
    private readonly IProductRepository _products;
    private readonly IOutboxMessageRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactions;

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

        if (!result.ProductExists)
        {
            return null;
        }

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

        if (!result.ProductExists)
        {
            return null;
        }

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

                variant.UpdateDetails(
                    description,
                    price,
                    stockQuantity,
                    discountPercentage);

                AddInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return MapVariant(variant);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }


    public Task<bool> ActivateAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.Activate(),
            cancellationToken);
    }

    public Task<bool> DeactivateAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.Deactivate(),
            cancellationToken);
    }

    public Task<bool> ShowAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.ShowToCustomers(),
            cancellationToken);
    }

    public Task<bool> HideAsync(
        int variantId,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            variantId,
            variant => variant.HideFromCustomers(),
            cancellationToken);
    }

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
                var variant =
                    await _products.GetVariantByIdForUpdateAsync(
                        variantId,
                        token);

                if (variant is null)
                {
                    return false;
                }

                change(variant);

                AddInvalidation(variant.ProductId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    private void AddInvalidation(int productId)
    {
        var payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outbox.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

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

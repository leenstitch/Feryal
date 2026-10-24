using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;

namespace Feryal.Application.Services;

// Handles cart operations, including retrieving items, managing quantities, and removing products.
public sealed class CartService : ICartService
{
    // Provides database operations for retrieving and modifying cart data.
    private readonly ICartRepository _cartRepository;

    // Manages cached cart data and invalidates it when changes occur.
    private readonly ICartCacheService _cartCacheService;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Initializes the cart service with its required dependencies.
    public CartService(
        ICartRepository cartRepository,
        ICartCacheService cartCacheService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _cartCacheService = cartCacheService;
        _unitOfWork = unitOfWork;
    }

    // Retrieves the user's paginated cart items after validating the user ID and pagination parameters.
    public async Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        return await _cartCacheService.GetItemsAsync(
            userId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    // Adds a product variant to the user's cart or increases its quantity if it already exists.
    public async Task AddItemAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidatePositiveQuantity(quantity);
        ValidateProductVariantId(productVariantId);

        var variant = await GetSellableVariantAsync(
            productVariantId,
            cancellationToken);

        EnsureStockAvailable(variant, quantity);

        var cart = await _cartRepository.GetByUserIdForUpdateAsync(
            userId,
            cancellationToken);

        if (cart is null)
        {
            cart = new Cart(userId);

            await _cartRepository.AddCartAsync(
                cart,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        var existingItem = cart.Items.FirstOrDefault(
            item => item.ProductVariantId == productVariantId);

        if (existingItem is not null)
        {
            var newQuantity = checked(existingItem.Quantity + quantity);

            EnsureStockAvailable(variant, newQuantity);

            existingItem.ChangeQuantity(newQuantity);
            cart.Touch();
        }
        else
        {
            var item = new CartItem(
                cart.Id,
                productVariantId,
                quantity);

            cart.AddItem(item);

            await _cartRepository.AddItemAsync(
                item,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _cartCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Increases the quantity of an existing cart item after validating availability and stock limits.
    public async Task IncreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateProductVariantId(productVariantId);
        ValidatePositiveQuantity(quantity);

        var cart = await GetExistingCartAsync(
            userId,
            cancellationToken);

        var item = FindCartItem(cart, productVariantId);

        var variant = await GetSellableVariantAsync(
            productVariantId,
            cancellationToken);

        var newQuantity = checked(item.Quantity + quantity);

        EnsureStockAvailable(variant, newQuantity);

        item.ChangeQuantity(newQuantity);
        cart.Touch();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _cartCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Decreases the quantity of an existing cart item and saves the updated cart.
    public async Task DecreaseQuantityAsync(
        int userId,
        int productVariantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateProductVariantId(productVariantId);
        ValidatePositiveQuantity(quantity);

        var cart = await GetExistingCartAsync(
            userId,
            cancellationToken);

        var item = FindCartItem(cart, productVariantId);

        item.DecreaseQuantity(quantity);
        cart.Touch();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _cartCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Removes a product variant from the user's cart and invalidates the cached cart data.
    public async Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateProductVariantId(productVariantId);

        var cart = await GetExistingCartAsync(
            userId,
            cancellationToken);

        var item = FindCartItem(cart, productVariantId);

        cart.RemoveItem(item);

        await _cartRepository.RemoveItemAsync(
            item,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await _cartCacheService.InvalidateUserAsync(
            userId,
            cancellationToken);
    }

    // Retrieves the user's existing cart or throws an exception if no cart is found.
    private async Task<Cart> GetExistingCartAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdForUpdateAsync(
            userId,
            cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException(
                "The cart was not found.");
        }

        return cart;
    }

    // Finds a cart item by its product variant ID or throws an exception if it does not exist.
    private static CartItem FindCartItem(
        Cart cart,
        int productVariantId)
    {
        var item = cart.Items.FirstOrDefault(
            x => x.ProductVariantId == productVariantId);

        if (item is null)
        {
            throw new KeyNotFoundException(
                "The product variant was not found in the cart.");
        }

        return item;
    }

    // Retrieves a product variant and verifies that it is configured, active, and available for sale.
    private async Task<ProductVariant> GetSellableVariantAsync(
        int productVariantId,
        CancellationToken cancellationToken)
    {
        var variant = await _cartRepository.GetVariantForCartAsync(
            productVariantId,
            cancellationToken);

        if (variant is null)
        {
            throw new KeyNotFoundException(
                "The product variant was not found.");
        }

        if (variant.Product is null ||
            !variant.Product.IsActive ||
            !variant.IsActive ||
            !variant.IsConfigured)
        {
            throw new InvalidOperationException(
                "This product variant is not available for purchase.");
        }

        if (!variant.Price.HasValue)
        {
            throw new InvalidOperationException(
                "This product variant does not have a selling price.");
        }

        if (!variant.StockQuantity.HasValue)
        {
            throw new InvalidOperationException(
                "The stock quantity for this product variant is not configured.");
        }

        return variant;
    }

    // Ensures that the requested quantity does not exceed the available stock.
    private static void EnsureStockAvailable(
        ProductVariant variant,
        int requestedQuantity)
    {
        if (!variant.StockQuantity.HasValue)
        {
            throw new InvalidOperationException(
                "The stock quantity is not configured.");
        }

        if (requestedQuantity > variant.StockQuantity.Value)
        {
            throw new InvalidOperationException(
                $"Only {variant.StockQuantity.Value} units are available.");
        }
    }

    // Validates that the user ID is a positive integer.
    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }
    }

    // Validates that the product variant ID is a positive integer.
    private static void ValidateProductVariantId(int productVariantId)
    {
        if (productVariantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productVariantId));
        }
    }

    // Validates that the requested quantity is greater than zero.
    private static void ValidatePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }
}
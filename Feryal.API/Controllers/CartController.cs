using System.Security.Claims;
using Feryal.Application.DTOs.Cart;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    // Initializes the controller with the cart service.
    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // Retrieves the authenticated user's cart items with pagination.
    [HttpGet]
    [ProducesResponseType(
        typeof(CartPageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartPageResponse>> GetItems(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 12;
        }

        try
        {
            var result = await _cartService.GetItemsAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    // Adds a product variant with the specified quantity to the authenticated user's cart.
    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            await _cartService.AddItemAsync(
                userId,
                request.ProductVariantId,
                request.Quantity,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (OverflowException)
        {
            return BadRequest(new
            {
                message = "The requested quantity is too large."
            });
        }
    }

    // Increases the quantity of a product variant in the authenticated user's cart.
    [HttpPatch("items/{productVariantId:int}/increase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IncreaseQuantity(
        int productVariantId,
        [FromBody] ChangeCartQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            await _cartService.IncreaseQuantityAsync(
                userId,
                productVariantId,
                request.Quantity,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (OverflowException)
        {
            return BadRequest(new
            {
                message = "The requested quantity is too large."
            });
        }
    }

    // Decreases the quantity of a product variant in the authenticated user's cart.
    [HttpPatch("items/{productVariantId:int}/decrease")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DecreaseQuantity(
        int productVariantId,
        [FromBody] ChangeCartQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            await _cartService.DecreaseQuantityAsync(
                userId,
                productVariantId,
                request.Quantity,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    // Removes a product variant from the authenticated user's cart.
    [HttpDelete("items/{productVariantId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        int productVariantId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            await _cartService.RemoveItemAsync(
                userId,
                productVariantId,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    // Retrieves the authenticated user's ID from the claims principal.
    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : 0;
    }
}
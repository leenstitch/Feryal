using System.Security.Claims;
using Feryal.Application.DTOs.Wishlist;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlist")]
public sealed class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    // Initializes the controller with the wishlist service.
    public WishlistController(
        IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    // Retrieves the authenticated user's wishlist items with pagination.
    [HttpGet]
    [ProducesResponseType(
        typeof(WishlistItemsPageResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistItemsPageResponse>> GetItems(
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

        var result = await _wishlistService.GetItemsAsync(
            userId,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    // Adds a product variant to the authenticated user's wishlist.
    [HttpPost("{productVariantId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
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
            await _wishlistService.AddItemAsync(
                userId,
                productVariantId,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Removes a product variant from the authenticated user's wishlist.
    [HttpDelete("{productVariantId:int}")]
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
            await _wishlistService.RemoveItemAsync(
                userId,
                productVariantId,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Retrieves the authenticated user's ID from the claims principal.
    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId)
            ? userId
            : 0;
    }
}
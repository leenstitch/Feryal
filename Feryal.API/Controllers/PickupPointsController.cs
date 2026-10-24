using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/pickup-points")]
public sealed class PickupPointsController : ControllerBase
{
    private readonly IDeliverySettingsService _deliverySettingsService;

    // Initializes the controller with the delivery settings service.
    public PickupPointsController(
        IDeliverySettingsService deliverySettingsService)
    {
        _deliverySettingsService = deliverySettingsService;
    }

    // Creates a new pickup point and returns its location and details.
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePickupPointRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deliverySettingsService.CreatePickupPointAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    // Updates the specified pickup point using the provided patch request.
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(
        int id,
        [FromBody] PatchPickupPointRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deliverySettingsService.PatchPickupPointAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = $"Pickup point with ID {id} was not found."
            });
        }

        return Ok(result);
    }

    // Retrieves all pickup points, including active and inactive points, for administration.
    // Admin: all pickup points, active and inactive.
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllForAdmin(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (!IsValidPagination(pageNumber, pageSize))
        {
            return BadRequest(new
            {
                message = "pageNumber must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        var result = await _deliverySettingsService.GetPickupPointsAsync(
            pageNumber,
            pageSize,
            false,
            cancellationToken);

        return Ok(result);
    }

    // Retrieves only active pickup points for customers with pagination.
    // Customer: active pickup points only.
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveForCustomers(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (!IsValidPagination(pageNumber, pageSize))
        {
            return BadRequest(new
            {
                message = "pageNumber must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        var result = await _deliverySettingsService.GetPickupPointsAsync(
            pageNumber,
            pageSize,
            true,
            cancellationToken);

        return Ok(result);
    }

    // Retrieves a pickup point by its ID.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _deliverySettingsService.GetPickupPointByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = $"Pickup point with ID {id} was not found."
            });
        }

        return Ok(result);
    }

    // Validates that the page number is at least 1 and the page size is between 1 and 100.
    private static bool IsValidPagination(
        int pageNumber,
        int pageSize)
    {
        return pageNumber >= 1 &&
               pageSize >= 1 &&
               pageSize <= 100;
    }
}
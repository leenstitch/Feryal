
using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/pickup-points")]
public sealed class PickupPointsController : ControllerBase
{
    private readonly IDeliverySettingsService _deliverySettingsService;

    public PickupPointsController(
        IDeliverySettingsService deliverySettingsService)
    {
        _deliverySettingsService = deliverySettingsService;
    }

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

    private static bool IsValidPagination(
        int pageNumber,
        int pageSize)
    {
        return pageNumber >= 1 &&
               pageSize >= 1 &&
               pageSize <= 100;
    }
}


using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/delivery-cities")]
public sealed class DeliveryCitiesController : ControllerBase
{
    private readonly IDeliverySettingsService _deliverySettingsService;

    public DeliveryCitiesController(
        IDeliverySettingsService deliverySettingsService)
    {
        _deliverySettingsService = deliverySettingsService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeliveryCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deliverySettingsService.CreateCityAsync(
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
        [FromBody] PatchDeliveryCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deliverySettingsService.PatchCityAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = $"Delivery city with ID {id} was not found."
            });
        }

        return Ok(result);
    }

    // Admin: all cities, active and inactive.
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

        var result = await _deliverySettingsService.GetCitiesAsync(
            pageNumber,
            pageSize,
            false,
            cancellationToken);

        return Ok(result);
    }

    // Customer: active cities only.
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

        var result = await _deliverySettingsService.GetCitiesAsync(
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
        var result = await _deliverySettingsService.GetCityByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = $"Delivery city with ID {id} was not found."
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

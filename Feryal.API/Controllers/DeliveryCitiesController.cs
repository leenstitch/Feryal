using Feryal.Application.DTOs.Delivery;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/delivery-cities")]
public sealed class DeliveryCitiesController : ControllerBase
{
    private readonly IDeliverySettingsService _deliverySettingsService;

    // Initializes the controller with the delivery settings service.
    public DeliveryCitiesController(
        IDeliverySettingsService deliverySettingsService)
    {
        _deliverySettingsService = deliverySettingsService;
    }

    // Creates a new delivery city and returns its location and details.
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

    // Updates the specified delivery city's properties using the provided patch request.
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

    // Retrieves all delivery cities, including active and inactive cities, for administration.
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

    // Retrieves only active delivery cities for customers with pagination.
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

    // Retrieves a delivery city by its ID.
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
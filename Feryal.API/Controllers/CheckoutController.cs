using System.Security.Claims;
using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public sealed class CheckoutController : ControllerBase
{
    private readonly IOrderService _orderService;

    public CheckoutController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCheckout(
        CancellationToken cancellationToken)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var response = await _orderService.GetCheckoutAsync(
            userId.Value,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("reservations")]
    [ProducesResponseType(
     typeof(CreateReservationResponse),
     StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReservation(
     [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
     CancellationToken cancellationToken)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new
            {
                message = "Idempotency-Key header is required."
            });
        }

        var response = await _orderService.CreateReservationAsync(
            userId.Value,
            idempotencyKey,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("reservations/{reservationId:int}")]
    [ProducesResponseType(
        typeof(ReservationStatusResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservation(
        int reservationId,
        CancellationToken cancellationToken)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var response = await _orderService.GetReservationStatusAsync(
            userId.Value,
            reservationId,
            cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("reservations/{reservationId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReleaseReservation(
        int reservationId,
        CancellationToken cancellationToken)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await _orderService.ReleaseReservationAsync(
            userId.Value,
            reservationId,
            cancellationToken);

        return NoContent();
    }

    private int? GetUserId()
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(value, out int userId) && userId > 0
            ? userId
            : null;
    }
}
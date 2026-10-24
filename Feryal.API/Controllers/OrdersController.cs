using System.Security.Claims;
using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> ConfirmOrder(
        [FromBody] ConfirmOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
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
                message = "The Idempotency-Key header is required."
            });
        }

        var order = await _orderService.ConfirmOrderAsync(
            userId.Value,
            request,
            idempotencyKey,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetMyOrder),
            new { orderId = order.Id },
            order);
    }
    [HttpGet("my")]
    [ProducesResponseType(
    typeof(PagedOrdersResponse),
    StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var response = await _orderService.GetMyOrdersAsync(
            userId.Value,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(response);
    }
    //[HttpGet("my")]
    //[ProducesResponseType(
    //    typeof(PagedOrdersResponse),
    //    StatusCodes.Status200OK)]
    //public async Task<IActionResult> GetMyOrders(
    //    [FromQuery] int pageNumber = 1,
    //    [FromQuery] int pageSize = 10,
    //    CancellationToken cancellationToken = default)
    //{
    //    int? userId = GetUserId();

    //    if (!userId.HasValue)
    //    {
    //        return Unauthorized();
    //    }

    //    var response = await _orderService.GetMyOrdersAsync(
    //        userId.Value,
    //        pageNumber,
    //        pageSize,
    //        cancellationToken);

    //    return Ok(response);
    //}

    [HttpGet("my/{orderId:int}")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrder(
        int orderId,
        CancellationToken cancellationToken)
    {
        int? userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var order = await _orderService.GetMyOrderAsync(
            userId.Value,
            orderId,
            cancellationToken);

        return order is null ? NotFound() : Ok(order);
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
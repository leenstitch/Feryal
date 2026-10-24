using Feryal.Application.DTOs.Orders;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedOrdersResponse),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] OrderStatus? status,
        [FromQuery] string? search,
        [FromQuery] int? deliveryCityId,
        [FromQuery] int? pickupPointId,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetAdminOrdersAsync(
            pageNumber,
            pageSize,
            status,
            search,
            deliveryCityId,
            pickupPointId,
            cancellationToken);

        var response = new PagedOrdersResponse(
            result.Items.Select(MapOrder).ToList(),
            pageNumber,
            pageSize,
            result.TotalCount,
            (int)Math.Ceiling(
                result.TotalCount / (double)pageSize));

        return Ok(response);
    }

    [HttpGet("{orderId:int}")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetAdminOrderAsync(
            orderId,
            cancellationToken);

        return order is null
            ? NotFound()
            : Ok(order);
    }

    [HttpPatch("{orderId:int}/status")]
    [ProducesResponseType(
        typeof(OrderDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        int orderId,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.UpdateOrderStatusAsync(
            orderId,
            request.Status,
            cancellationToken);

        return order is null
            ? NotFound()
            : Ok(order);
    }

    private static OrderDetailsResponse MapOrder(Order order)
    {
        var items = order.Items.Select(item =>
            new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.ProductName,
                item.VariantDescription,
                item.ProductVariant?.Product?.Images
                    .FirstOrDefault(image => image.IsPrimary)?.ImageUrl,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice))
            .ToList();

        return new OrderDetailsResponse(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.CustomerName,
            order.CustomerPhoneNumber,
            order.DeliveryType,
            order.DeliveryCityId,
            order.DeliveryCityName,
            order.DeliveryArea,
            order.DeliveryStreet,
            order.DeliveryBuildingNumber,
            order.DeliveryAdditionalDetails,
            order.PickupPointId,
            order.PickupPointName,
            order.PickupPointAddress,
            order.OrderDate,
            order.Subtotal,
            order.DiscountAmount,
            order.DeliveryFee,
            order.TotalAmount,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            items);
    }
}
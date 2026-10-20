using System.Security.Claims;
using Marketplace.Orders.Application;
using Marketplace.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Orders.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController(OrderService orders) : ControllerBase
{
    private string UserId => User.FindFirstValue("sub")!;

    private bool IsAdmin => User.IsInRole("admin");

    [HttpPost]
    [Authorize(Roles = "buyer")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(UserId, request, ct);
        var response = ApiResponse<OrderResponse>.Ok(OrderResponse.From(order), "Pedido criado");

        return CreatedAtAction(nameof(Get), new { id = order.Id }, response);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "buyer")]
    public async Task<ApiResponse<PagedResult<OrderResponse>>> Mine(
        [FromQuery] int page = 1, [FromQuery] int perPage = 20, CancellationToken ct = default)
    {
        var result = await orders.ListMineAsync(UserId, page, perPage, ct);

        return ApiResponse<PagedResult<OrderResponse>>.Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin,seller")]
    public async Task<ApiResponse<PagedResult<OrderResponse>>> List(
        [FromQuery] string? status,
        [FromQuery] string? sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        CancellationToken ct = default)
    {
        var result = await orders.ListAsync(status, sellerId, UserId, IsAdmin, page, perPage, ct);

        return ApiResponse<PagedResult<OrderResponse>>.Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<OrderResponse>> Get(string id, CancellationToken ct)
    {
        var order = await orders.GetAsync(id, UserId, IsAdmin, ct);

        return ApiResponse<OrderResponse>.Ok(OrderResponse.From(order));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "buyer")]
    public async Task<ApiResponse<OrderResponse>> Cancel(string id, CancellationToken ct)
    {
        var order = await orders.CancelAsync(id, UserId, ct);

        return ApiResponse<OrderResponse>.Ok(OrderResponse.From(order), "Pedido cancelado");
    }
}

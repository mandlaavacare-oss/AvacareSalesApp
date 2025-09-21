using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;
using Server.Infrastructure.Authentication;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Services;

namespace Server.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, IDatabaseContext databaseContext, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [HttpPost]
    public async Task<ActionResult<SalesOrder>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var order = await _orderService.CreateOrderAsync(request, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(order);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Domain error when creating order for {CustomerId}", request.CustomerId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error when creating order for {CustomerId}", request.CustomerId);
            return Problem("An unexpected error occurred while creating the order.");
        }
    }
}

using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Transactions.OrderEntry.Adapters;
using Server.Transactions.OrderEntry.Models;

namespace Server.Transactions.OrderEntry.Services;

public interface IOrderService
{
    Task<SalesOrder> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}

public class OrderService : IOrderService
{
    private readonly IOrderEntryAdapter _adapter;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderEntryAdapter adapter, ILogger<OrderService> logger)
    {
        _adapter = adapter;
        _logger = logger;
    }

    public async Task<SalesOrder> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            throw new DomainException("Orders must contain at least one line.");
        }

        var order = new SalesOrder(Guid.NewGuid().ToString(), request.CustomerId, request.OrderDate, request.Lines, "Pending");

        try
        {
            return await _adapter.CreateOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
            throw new DomainException("Unable to create order in Sage.", ex);
        }
    }
}

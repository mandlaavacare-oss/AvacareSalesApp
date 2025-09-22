using System.Linq;
using Server.Common.Exceptions;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Sdk;

namespace Server.Transactions.OrderEntry.Adapters;

public class SageOrderEntryAdapter : IOrderEntryAdapter
{
    private readonly ISageOrderEntrySdk _orderEntrySdk;

    public SageOrderEntryAdapter(ISageOrderEntrySdk orderEntrySdk)
    {
        _orderEntrySdk = orderEntrySdk;
    }

    public async Task<SalesOrder> CreateOrderAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        try
        {
            var request = new SageOrderRequest(order.CustomerId, order.OrderDate, order.Lines.Select(MapLineToSdk).ToList());
            var response = await _orderEntrySdk.CreateOrderAsync(request, cancellationToken);

            if (response is null)
            {
                throw new DomainException("Sage returned no order response.");
            }

            var id = string.IsNullOrWhiteSpace(response.OrderNumber) ? order.Id : response.OrderNumber;
            var status = MapStatus(response.Status);
            var lines = response.Lines?.Select(MapLineFromSdk).ToList() ?? order.Lines.ToList();

            return new SalesOrder(id, order.CustomerId, order.OrderDate, lines, status);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException("Unable to create order in Sage.", ex);
        }
    }

    private static SageOrderLine MapLineToSdk(SalesOrderLine line) =>
        new(line.ProductId, line.Quantity, line.UnitPrice);

    private static SalesOrderLine MapLineFromSdk(SageOrderLine line) =>
        new(line.ProductCode, line.Quantity, line.UnitPrice);

    private static string MapStatus(SageOrderStatus status) => status switch
    {
        SageOrderStatus.Pending => "Pending",
        SageOrderStatus.Released => "Released",
        SageOrderStatus.Completed => "Completed",
        SageOrderStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
}

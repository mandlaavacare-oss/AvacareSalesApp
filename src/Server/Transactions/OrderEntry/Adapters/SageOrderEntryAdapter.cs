using Server.Transactions.OrderEntry.Models;

namespace Server.Transactions.OrderEntry.Adapters;

public class SageOrderEntryAdapter : IOrderEntryAdapter
{
    public Task<SalesOrder> CreateOrderAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate order creation with Sage SDK.");
    }
}

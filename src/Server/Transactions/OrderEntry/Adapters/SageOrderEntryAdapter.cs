using Server.Transactions.OrderEntry.Models;
using Server.Sage;

namespace Server.Transactions.OrderEntry.Adapters;

public class SageOrderEntryAdapter : IOrderEntryAdapter
{
    public Task<SalesOrder> CreateOrderAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(SageSdkStub.SaveOrder(order));
    }
}

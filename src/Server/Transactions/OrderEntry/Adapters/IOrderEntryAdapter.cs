using Server.Transactions.OrderEntry.Models;

namespace Server.Transactions.OrderEntry.Adapters;

public interface IOrderEntryAdapter
{
    Task<SalesOrder> CreateOrderAsync(SalesOrder order, CancellationToken cancellationToken);
}

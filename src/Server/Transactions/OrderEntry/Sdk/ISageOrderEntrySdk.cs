namespace Server.Transactions.OrderEntry.Sdk;

public interface ISageOrderEntrySdk
{
    Task<SageOrderResponse> CreateOrderAsync(SageOrderRequest request, CancellationToken cancellationToken);
}

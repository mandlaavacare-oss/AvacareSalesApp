namespace Server.Transactions.Inventory.Adapters;

public class SageInventoryClient : ISageInventoryClient
{
    public Task<IReadOnlyCollection<SageInventoryProduct>> GetInventoryAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate Sage inventory client with the SDK.");
    }
}

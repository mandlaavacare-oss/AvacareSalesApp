namespace Server.Transactions.Inventory.Adapters;

public interface ISageInventoryClient
{
    Task<IReadOnlyCollection<SageInventoryProduct>> GetInventoryAsync(CancellationToken cancellationToken);
}

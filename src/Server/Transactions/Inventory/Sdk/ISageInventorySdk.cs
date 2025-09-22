namespace Server.Transactions.Inventory.Sdk;

public interface ISageInventorySdk
{
    Task<IReadOnlyCollection<SageInventoryItem>> GetProductsAsync(CancellationToken cancellationToken);
}

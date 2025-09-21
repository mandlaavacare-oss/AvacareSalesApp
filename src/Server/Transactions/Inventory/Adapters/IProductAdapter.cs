using Server.Infrastructure.InventoryCache;

namespace Server.Transactions.Inventory.Adapters;

public interface IProductAdapter
{
    Task<IReadOnlyCollection<CacheInventoryItem>> GetProductsAsync(CancellationToken cancellationToken);
}

using Server.Transactions.Inventory.Models;

namespace Server.Transactions.Inventory.Adapters;

public interface IProductAdapter
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken);
}

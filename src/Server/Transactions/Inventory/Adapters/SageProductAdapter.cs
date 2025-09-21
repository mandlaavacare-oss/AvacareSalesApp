using Server.Transactions.Inventory.Models;

namespace Server.Transactions.Inventory.Adapters;

public class SageProductAdapter : IProductAdapter
{
    public Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate product retrieval with Sage SDK.");
    }
}

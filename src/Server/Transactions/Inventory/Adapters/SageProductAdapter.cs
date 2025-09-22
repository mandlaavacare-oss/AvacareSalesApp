using Server.Transactions.Inventory.Models;
using Server.Sage;

namespace Server.Transactions.Inventory.Adapters;

public class SageProductAdapter : IProductAdapter
{
    public Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SageSdkStub.GetProducts());
    }
}

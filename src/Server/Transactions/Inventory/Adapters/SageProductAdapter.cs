using System.Linq;
using Server.Common.Exceptions;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Sdk;

namespace Server.Transactions.Inventory.Adapters;

public class SageProductAdapter : IProductAdapter
{
    private readonly ISageInventorySdk _inventorySdk;

    public SageProductAdapter(ISageInventorySdk inventorySdk)
    {
        _inventorySdk = inventorySdk;
    }

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var products = await _inventorySdk.GetProductsAsync(cancellationToken);

            if (products is null)
            {
                throw new DomainException("Sage returned an empty product list.");
            }

            return products.Select(MapProduct).ToList();
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException("Unable to retrieve products from Sage.", ex);
        }
    }

    private static Product MapProduct(SageInventoryItem item) =>
        new(item.Code, item.Name, item.Description, item.UnitPrice, item.QuantityOnHand);
}

using System.Collections.Concurrent;

namespace AvacareSalesApp.Transactions.OrderEntry.Adapters;

public sealed class InMemoryProductCatalogAdapter : IProductCatalogAdapter
{
    private readonly ConcurrentDictionary<string, decimal> priceLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ITEM-1001"] = 1250.00m,
        ["ITEM-2002"] = 499.99m,
        ["ITEM-3003"] = 89.50m
    };

    public decimal GetUnitPrice(string sku)
    {
        ArgumentException.ThrowIfNullOrEmpty(sku);

        if (!priceLookup.TryGetValue(sku, out var unitPrice))
        {
            throw new KeyNotFoundException($"Product with SKU '{sku}' was not found in the catalog.");
        }

        return unitPrice;
    }
}

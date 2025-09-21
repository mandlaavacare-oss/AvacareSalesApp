using Microsoft.Extensions.Logging;
using Server.Infrastructure.InventoryCache;

namespace Server.Transactions.Inventory.Adapters;

public class SageProductAdapter : IProductAdapter
{
    private readonly ISageInventoryClient _client;
    private readonly ILogger<SageProductAdapter> _logger;
    private readonly TimeProvider _timeProvider;

    public SageProductAdapter(ISageInventoryClient client, ILogger<SageProductAdapter> logger, TimeProvider? timeProvider = null)
    {
        _client = client;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyCollection<CacheInventoryItem>> GetProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var products = await _client.GetInventoryAsync(cancellationToken);
            var snapshotTime = _timeProvider.GetUtcNow();

            return products
                .Select(product => new CacheInventoryItem
                {
                    Sku = product.StockCode,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.UnitPrice,
                    QuantityOnHand = product.QuantityOnHand,
                    SyncedAt = snapshotTime,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve inventory from Sage.");
            throw;
        }
    }
}

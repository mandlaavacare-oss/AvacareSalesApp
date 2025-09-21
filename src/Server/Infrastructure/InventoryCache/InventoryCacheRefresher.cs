using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Transactions.Inventory.Adapters;

namespace Server.Infrastructure.InventoryCache;

public class InventoryCacheRefresher : IInventoryCacheRefresher
{
    private readonly IProductAdapter _productAdapter;
    private readonly ICacheInventoryRepository _repository;
    private readonly ILogger<InventoryCacheRefresher> _logger;
    private readonly InventoryCacheOptions _options;

    public InventoryCacheRefresher(
        IProductAdapter productAdapter,
        ICacheInventoryRepository repository,
        IOptions<InventoryCacheOptions> options,
        ILogger<InventoryCacheRefresher> logger)
    {
        _productAdapter = productAdapter;
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing inventory cache from Sage.");

        var items = await _productAdapter.GetProductsAsync(cancellationToken);
        _logger.LogInformation("Retrieved {Count} inventory items from Sage.", items.Count);

        await _repository.ReplaceInventoryAsync(items, _options.BatchSize, cancellationToken);

        _logger.LogInformation("Inventory cache refreshed successfully.");
    }
}

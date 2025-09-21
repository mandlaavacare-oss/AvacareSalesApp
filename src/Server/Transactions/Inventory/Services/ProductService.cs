using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Microsoft.Extensions.Options;
using Server.Infrastructure.InventoryCache;
using Server.Transactions.Inventory.Models;

namespace Server.Transactions.Inventory.Services;

public interface IProductService
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken);
}

public class ProductService : IProductService
{
    private readonly ICacheInventoryRepository _repository;
    private readonly InventoryCacheOptions _options;
    private readonly ILogger<ProductService> _logger;
    private readonly TimeProvider _timeProvider;

    public ProductService(
        ICacheInventoryRepository repository,
        IOptions<InventoryCacheOptions> options,
        ILogger<ProductService> logger,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _repository.GetItemsAsync(cancellationToken);

            if (items.Count == 0)
            {
                _logger.LogWarning("Inventory cache is empty.");
                throw new DomainException("Inventory cache is not available. Please try again later.");
            }

            await EnsureCacheIsFreshAsync(cancellationToken);

            return items
                .Select(item => new Product(item.Sku, item.Name, item.Description, item.Price, item.QuantityOnHand))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load products from cache");
            throw new DomainException("Unable to retrieve cached products.", ex);
        }
    }

    private async Task EnsureCacheIsFreshAsync(CancellationToken cancellationToken)
    {
        if (!_options.StaleAfter.HasValue)
        {
            return;
        }

        var lastSyncedAt = await _repository.GetLastSyncedAtAsync(cancellationToken);

        if (!lastSyncedAt.HasValue)
        {
            _logger.LogWarning("Inventory cache has no synchronization timestamp.");
            throw new DomainException("Inventory cache has not been synchronized yet.");
        }

        var now = _timeProvider.GetUtcNow();
        var age = now - lastSyncedAt.Value;
        if (age > _options.StaleAfter.Value)
        {
            _logger.LogWarning(
                "Inventory cache is stale. Last sync {LastSync} exceeds threshold {Threshold}.",
                lastSyncedAt,
                _options.StaleAfter);
            throw new DomainException("Cached inventory data is stale. Please try again later.");
        }
    }
}

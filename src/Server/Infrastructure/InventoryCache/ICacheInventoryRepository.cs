namespace Server.Infrastructure.InventoryCache;

public interface ICacheInventoryRepository
{
    Task<IReadOnlyCollection<CacheInventoryItem>> GetItemsAsync(CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastSyncedAtAsync(CancellationToken cancellationToken);

    Task ReplaceInventoryAsync(IEnumerable<CacheInventoryItem> items, int batchSize, CancellationToken cancellationToken);
}

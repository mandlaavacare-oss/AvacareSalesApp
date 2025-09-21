namespace Server.Infrastructure.InventoryCache;

public interface IInventoryCacheRefresher
{
    Task RefreshAsync(CancellationToken cancellationToken);
}

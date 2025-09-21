using Microsoft.EntityFrameworkCore;

namespace Server.Infrastructure.InventoryCache;

public class CacheInventoryRepository : ICacheInventoryRepository
{
    private readonly CacheInventoryDbContext _dbContext;

    public CacheInventoryRepository(CacheInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CacheInventoryItem>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.CacheInventory
            .AsNoTracking()
            .OrderBy(item => item.Sku)
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastSyncedAtAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.CacheInventory
            .AsNoTracking()
            .Select(item => (DateTimeOffset?)item.SyncedAt)
            .OrderByDescending(value => value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ReplaceInventoryAsync(IEnumerable<CacheInventoryItem> items, int batchSize, CancellationToken cancellationToken)
    {
        var list = items.Select(item => item.Clone()).ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                await _dbContext.CacheInventory.ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                var existing = await _dbContext.CacheInventory.ToListAsync(cancellationToken);
                _dbContext.CacheInventory.RemoveRange(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            if (batchSize <= 0)
            {
                await _dbContext.CacheInventory.AddRangeAsync(list, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
                return;
            }

            for (var i = 0; i < list.Count; i += batchSize)
            {
                var batch = list.Skip(i).Take(batchSize).ToList();
                await _dbContext.CacheInventory.AddRangeAsync(batch, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

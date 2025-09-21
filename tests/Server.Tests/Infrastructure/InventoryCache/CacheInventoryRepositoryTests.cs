using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.InventoryCache;

namespace Server.Tests.Infrastructure.InventoryCache;

public class CacheInventoryRepositoryTests
{
    [Fact]
    public async Task ReplaceInventoryAsync_ReplacesExistingRecords()
    {
        await using var context = CreateContext();
        context.CacheInventory.Add(new CacheInventoryItem
        {
            Sku = "OLD",
            Name = "Old Item",
            Description = "Old",
            Price = 5m,
            QuantityOnHand = 1,
            SyncedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });
        await context.SaveChangesAsync();

        var repository = new CacheInventoryRepository(context);
        var newItems = new List<CacheInventoryItem>
        {
            new()
            {
                Sku = "NEW",
                Name = "New Item",
                Description = "New",
                Price = 10m,
                QuantityOnHand = 3,
                SyncedAt = DateTimeOffset.UtcNow,
            },
        };

        await repository.ReplaceInventoryAsync(newItems, batchSize: 1, CancellationToken.None);

        var saved = await context.CacheInventory.AsNoTracking().ToListAsync();
        saved.Should().HaveCount(1);
        saved.Should().ContainEquivalentOf(newItems.Single());
    }

    [Fact]
    public async Task GetLastSyncedAtAsync_ReturnsLatestTimestamp()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.CacheInventory.AddRange(new List<CacheInventoryItem>
        {
            new()
            {
                Sku = "SKU-1",
                Name = "One",
                Description = string.Empty,
                Price = 1m,
                QuantityOnHand = 1,
                SyncedAt = now.AddHours(-2),
            },
            new()
            {
                Sku = "SKU-2",
                Name = "Two",
                Description = string.Empty,
                Price = 2m,
                QuantityOnHand = 2,
                SyncedAt = now.AddHours(-1),
            },
        });
        await context.SaveChangesAsync();

        var repository = new CacheInventoryRepository(context);

        var lastSyncedAt = await repository.GetLastSyncedAtAsync(CancellationToken.None);

        lastSyncedAt.Should().BeCloseTo(now.AddHours(-1), TimeSpan.FromSeconds(1));
    }

    private static CacheInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CacheInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CacheInventoryDbContext(options);
    }
}

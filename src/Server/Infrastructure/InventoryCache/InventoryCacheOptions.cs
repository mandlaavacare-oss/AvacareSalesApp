namespace Server.Infrastructure.InventoryCache;

public class InventoryCacheOptions
{
    public const string SectionName = "InventoryCache";

    public TimeSpan SyncTimeUtc { get; set; } = new(2, 0, 0);

    public int BatchSize { get; set; } = 500;

    public TimeSpan? StaleAfter { get; set; }
        = TimeSpan.FromHours(24);
}

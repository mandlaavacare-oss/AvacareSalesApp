namespace Server.Infrastructure.Database.Entities;

public class CacheInventoryItem
{
    public int Id { get; set; }

    public string StockCode { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAllocated { get; set; }

    public DateTime CachedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Infrastructure.InventoryCache;

public class CacheInventoryItem
{
    [Key]
    [MaxLength(64)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int QuantityOnHand { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset SyncedAt { get; set; }

    public CacheInventoryItem Clone()
    {
        return new CacheInventoryItem
        {
            Sku = Sku,
            Name = Name,
            Description = Description,
            Price = Price,
            QuantityOnHand = QuantityOnHand,
            SyncedAt = SyncedAt,
        };
    }
}

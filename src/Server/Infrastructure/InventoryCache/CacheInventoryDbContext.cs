using Microsoft.EntityFrameworkCore;

namespace Server.Infrastructure.InventoryCache;

public class CacheInventoryDbContext(DbContextOptions<CacheInventoryDbContext> options) : DbContext(options)
{
    public virtual DbSet<CacheInventoryItem> CacheInventory => Set<CacheInventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CacheInventoryItem>();
        entity.ToTable("CacheInventory");
        entity.HasKey(x => x.Sku);
        entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(255);
        entity.Property(x => x.Description).HasMaxLength(2048);
        entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
        entity.Property(x => x.SyncedAt).HasColumnType("datetimeoffset");
    }
}

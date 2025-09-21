using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Database.Entities;

namespace Server.Infrastructure.Database;

public interface IDatabaseContext
{
    DbSet<OrderEntity> Orders { get; }

    DbSet<CacheInventoryItem> CacheInventory { get; }

    void BeginTran();

    void CommitTran();

    void RollbackTran();
}

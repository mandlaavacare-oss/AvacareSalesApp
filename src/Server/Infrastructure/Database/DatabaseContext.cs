using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Server.Infrastructure.Database.Entities;

namespace Server.Infrastructure.Database;

public class DatabaseContext : DbContext, IDatabaseContext
{
    private readonly ILogger<DatabaseContext> _logger;
    private IDbContextTransaction? _currentTransaction;

    public DatabaseContext(DbContextOptions<DatabaseContext> options, ILogger<DatabaseContext> logger)
        : base(options)
    {
        _logger = logger;
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<CacheInventoryItem> CacheInventory => Set<CacheInventoryItem>();

    public void BeginTran()
    {
        if (_currentTransaction is not null)
        {
            _logger.LogWarning("BeginTran called while a transaction is already active.");
            return;
        }

        _currentTransaction = Database.BeginTransaction();
        _logger.LogDebug("Starting database transaction.");
    }

    public void CommitTran()
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("CommitTran called without an active transaction.");
            return;
        }

        _currentTransaction.Commit();
        _currentTransaction.Dispose();
        _currentTransaction = null;
        _logger.LogDebug("Committing database transaction.");
    }

    public void RollbackTran()
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("RollbackTran called without an active transaction.");
            return;
        }

        _currentTransaction.Rollback();
        _currentTransaction.Dispose();
        _currentTransaction = null;
        _logger.LogDebug("Rolling back database transaction.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("OrderNumber");

            entity.Property(e => e.CustomerId)
                .HasColumnName("CustomerId");

            entity.Property(e => e.OrderDate)
                .HasColumnName("OrderDate");

            entity.Property(e => e.TotalAmount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("Status");
        });

        modelBuilder.Entity<CacheInventoryItem>(entity =>
        {
            entity.ToTable("CacheInventory");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.StockCode)
                .HasMaxLength(50)
                .HasColumnName("StockCode");

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("Description");

            entity.Property(e => e.QuantityOnHand)
                .HasColumnName("QuantityOnHand");

            entity.Property(e => e.QuantityAllocated)
                .HasColumnName("QuantityAllocated");

            entity.Property(e => e.CachedAt)
                .HasColumnName("CachedAt");
        });
    }
}

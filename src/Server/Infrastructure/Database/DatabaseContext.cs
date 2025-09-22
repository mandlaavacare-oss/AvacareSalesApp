using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Server.Infrastructure.Authentication.Database;

namespace Server.Infrastructure.Database;

public class DatabaseContext : IDatabaseContext
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseContext> _logger;
    private IDbContextTransaction? _currentTransaction;

    public DatabaseContext(ApplicationDbContext dbContext, ILogger<DatabaseContext> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void BeginTran()
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("BeginTran called while a transaction is already active.");
            return;
        }

        _logger.LogDebug("Starting database transaction.");
        _currentTransaction = _dbContext.Database.BeginTransaction();
    }

    public void CommitTran()
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("CommitTran called without an active transaction.");
            return;
        }

        try
        {
            _currentTransaction.Commit();
            _logger.LogDebug("Committed database transaction.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction. Rolling back.");
            _currentTransaction.Rollback();
            throw;
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }

    public void RollbackTran()
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("RollbackTran called without an active transaction.");
            return;
        }

        try
        {
            _currentTransaction.Rollback();
            _logger.LogDebug("Rolled back database transaction.");
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }
}

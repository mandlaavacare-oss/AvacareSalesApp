using Microsoft.Extensions.Logging;

namespace Server.Infrastructure.Database;

public class DatabaseContext : IDatabaseContext
{
    private readonly ILogger<DatabaseContext> _logger;
    private bool _transactionStarted;
    private int? _branchId;

    public DatabaseContext(ILogger<DatabaseContext> logger)
    {
        _logger = logger;
    }

    public void BeginTran()
    {
        if (_transactionStarted)
        {
            _logger.LogWarning("BeginTran called while a transaction is already active.");
            return;
        }

        _logger.LogDebug("Starting database transaction.");
        _transactionStarted = true;
    }

    public void CommitTran()
    {
        if (!_transactionStarted)
        {
            _logger.LogWarning("CommitTran called without an active transaction.");
            return;
        }

        _logger.LogDebug("Committing database transaction.");
        _transactionStarted = false;
    }

    public void RollbackTran()
    {
        if (!_transactionStarted)
        {
            _logger.LogWarning("RollbackTran called without an active transaction.");
            return;
        }

        _logger.LogDebug("Rolling back database transaction.");
        _transactionStarted = false;
    }

    public void SetBranchContext(int branchId)
    {
        if (_branchId == branchId)
        {
            _logger.LogDebug("Branch context already set to {BranchId}.", branchId);
            return;
        }

        _branchId = branchId;
        _logger.LogInformation("Branch context switched to {BranchId}.", branchId);
    }
}

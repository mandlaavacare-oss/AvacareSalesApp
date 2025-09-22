using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Common.Exceptions;

namespace Server.Infrastructure.Database;

public class DatabaseContext : IDatabaseContext
{
    private readonly ILogger<DatabaseContext> _logger;
    private readonly ISageSessionFactory _sessionFactory;
    private readonly SageSessionOptions _options;
    private ISageSession? _session;
    private ISageTransaction? _transaction;
    private bool _branchApplied;

    public DatabaseContext(
        ILogger<DatabaseContext> logger,
        ISageSessionFactory sessionFactory,
        IOptions<SageSessionOptions> options)
    {
        _logger = logger;
        _sessionFactory = sessionFactory;
        _options = options.Value;
    }

    public void BeginTran()
    {
        if (_transaction != null)
        {
            _logger.LogWarning("BeginTran called while a transaction is already active.");
            return;
        }

        try
        {
            _logger.LogDebug("Starting Sage SDK database transaction.");
            var session = EnsureSession();
            _transaction = session.BeginTransaction();
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin Sage SDK transaction.");
            throw new DomainException("Unable to begin a Sage SDK transaction.", ex);
        }
    }

    public void CommitTran()
    {
        if (_transaction == null)
        {
            _logger.LogWarning("CommitTran called without an active transaction.");
            return;
        }

        try
        {
            _logger.LogDebug("Committing Sage SDK transaction.");
            _transaction.Commit();
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit Sage SDK transaction.");
            throw new DomainException("Unable to commit the Sage SDK transaction.", ex);
        }
        finally
        {
            CleanupTransaction();
        }
    }

    public void RollbackTran()
    {
        if (_transaction == null)
        {
            _logger.LogWarning("RollbackTran called without an active transaction.");
            return;
        }

        try
        {
            _logger.LogDebug("Rolling back Sage SDK transaction.");
            _transaction.Rollback();
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to roll back Sage SDK transaction.");
            throw new DomainException("Unable to roll back the Sage SDK transaction.", ex);
        }
        finally
        {
            CleanupTransaction();
        }
    }

    private ISageSession EnsureSession()
    {
        _session ??= _sessionFactory.CreateSession();

        if (!_session.IsConnected)
        {
            _session.Connect();
            _branchApplied = false;
        }

        if (!_branchApplied)
        {
            var branchCode = _options.BranchCode;
            if (!string.IsNullOrWhiteSpace(branchCode))
            {
                _session.SetBranch(branchCode);
            }

            _branchApplied = true;
        }

        return _session;
    }

    private void CleanupTransaction()
    {
        if (_transaction != null)
        {
            try
            {
                _transaction.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose Sage SDK transaction.");
            }
            finally
            {
                _transaction = null;
            }
        }

        if (_session != null)
        {
            try
            {
                if (_session.IsConnected)
                {
                    _session.Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect Sage SDK session.");
            }
            finally
            {
                _session.Dispose();
                _session = null;
                _branchApplied = false;
            }
        }
    }
}

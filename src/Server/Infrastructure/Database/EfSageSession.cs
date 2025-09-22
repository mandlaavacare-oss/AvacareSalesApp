using System.Data;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Authentication.Database;

namespace Server.Infrastructure.Database;

public class EfSageSession : ISageSession
{
    private readonly ApplicationDbContext _dbContext;
    private bool _isConnected;

    public EfSageSession(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsConnected => _isConnected;

    public void Connect()
    {
        if (_isConnected)
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            _dbContext.Database.OpenConnection();
        }

        _isConnected = true;
    }

    public void Disconnect()
    {
        if (!_isConnected)
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
        {
            _dbContext.Database.CloseConnection();
        }

        _isConnected = false;
    }

    public void Dispose()
    {
        try
        {
            Disconnect();
        }
        catch
        {
            // No-op: disposal should not throw to callers.
        }
    }

    public ISageTransaction BeginTransaction()
    {
        var transaction = _dbContext.Database.BeginTransaction();
        return new EfSageTransaction(transaction);
    }

    public void SetBranch(string branchCode)
    {
        // Entity Framework does not support branch scoping directly. This method exists so the
        // DatabaseContext can maintain parity with the Sage SDK behaviour. When integrating with the
        // actual SDK, the branch context should be applied via the session APIs.
    }
}

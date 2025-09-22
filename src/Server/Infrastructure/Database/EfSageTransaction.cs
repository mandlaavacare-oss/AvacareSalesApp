using Microsoft.EntityFrameworkCore.Storage;

namespace Server.Infrastructure.Database;

public class EfSageTransaction(IDbContextTransaction transaction) : ISageTransaction
{
    public void Commit() => transaction.Commit();

    public void Dispose() => transaction.Dispose();

    public void Rollback() => transaction.Rollback();
}

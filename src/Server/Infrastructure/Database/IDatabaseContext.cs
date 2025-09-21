namespace Server.Infrastructure.Database;

public interface IDatabaseContext
{
    void BeginTran();

    void CommitTran();

    void RollbackTran();
}

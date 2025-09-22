using System;

namespace Server.Infrastructure.Database;

public interface ISageTransaction : IDisposable
{
    void Commit();

    void Rollback();
}

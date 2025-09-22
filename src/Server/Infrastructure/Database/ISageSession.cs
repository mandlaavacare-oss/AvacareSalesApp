using System;

namespace Server.Infrastructure.Database;

public interface ISageSession : IDisposable
{
    bool IsConnected { get; }

    void Connect();

    void Disconnect();

    void SetBranch(string branchCode);

    ISageTransaction BeginTransaction();
}

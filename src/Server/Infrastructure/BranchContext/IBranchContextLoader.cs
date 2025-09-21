namespace Server.Infrastructure.BranchContext;

public interface IBranchContextLoader
{
    void ApplyBranchContext(string? branchCode);
}

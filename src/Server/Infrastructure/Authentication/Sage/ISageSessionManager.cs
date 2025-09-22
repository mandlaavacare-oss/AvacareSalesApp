using System.Threading;
using System.Threading.Tasks;

namespace Server.Infrastructure.Authentication.Sage;

public interface ISageSessionManager
{
    Task<SageSession> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}

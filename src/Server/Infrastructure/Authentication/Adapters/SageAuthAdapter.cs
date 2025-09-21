using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Adapters;

public class SageAuthAdapter : IAuthAdapter
{
    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Integrate with Sage SDK authentication using the supplied Sage customer code.");
    }
}

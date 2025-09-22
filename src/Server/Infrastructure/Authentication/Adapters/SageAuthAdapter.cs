using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Sage;

namespace Server.Infrastructure.Authentication.Adapters;

public class SageAuthAdapter : IAuthAdapter
{
    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (SageSdkStub.TryLogin(request.Username, request.Password, out var result))
        {
            return Task.FromResult(result);
        }

        throw new DomainException("Invalid Sage credentials supplied.");
    }
}

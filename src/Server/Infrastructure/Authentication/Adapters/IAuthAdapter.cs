using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Adapters;

public interface IAuthAdapter
{
    Task<LoginResult> LoginAsync(LoginRequest request, string sageCustomerCode, CancellationToken cancellationToken);
}

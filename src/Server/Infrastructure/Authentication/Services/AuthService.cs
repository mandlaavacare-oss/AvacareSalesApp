using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly IAuthAdapter _authAdapter;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthAdapter authAdapter, ILogger<AuthService> logger)
    {
        _authAdapter = authAdapter;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _authAdapter.LoginAsync(request, cancellationToken);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate user {Username}", request.Username);
            throw new DomainException("Unable to authenticate with Sage.", ex);
        }
    }
}

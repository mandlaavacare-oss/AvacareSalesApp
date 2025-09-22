using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Sage;

namespace Server.Infrastructure.Authentication.Adapters;

public class SageAuthAdapter : IAuthAdapter
{
    private readonly ISageSessionManager _sessionManager;
    private readonly ILogger<SageAuthAdapter> _logger;

    public SageAuthAdapter(ISageSessionManager sessionManager, ILogger<SageAuthAdapter> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionManager.AuthenticateAsync(request.Username, request.Password, cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Token))
            {
                _logger.LogError("Sage session token was empty for {Username}", request.Username);
                throw new SageAuthenticationException("Sage did not return a session token.");
            }

            return new LoginResult(request.Username, session.Token);
        }
        catch (SageAuthenticationException ex)
        {
            _logger.LogWarning(ex, "Invalid Sage credentials supplied for {Username}", request.Username);
            throw new DomainException("Invalid Sage username or password.", ex);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            _logger.LogError(ex, "Failed to authenticate with Sage for {Username}", request.Username);
            throw new DomainException("Unable to authenticate with Sage.", ex);
        }
    }
}

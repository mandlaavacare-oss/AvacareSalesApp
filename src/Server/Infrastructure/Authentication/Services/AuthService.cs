using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthAdapter authAdapter, UserManager<ApplicationUser> userManager, ILogger<AuthService> logger)
    {
        _authAdapter = authAdapter;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
            {
                _logger.LogWarning("Login failed - user {Username} not found", request.Username);
                throw new DomainException("Invalid username or password.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Login failed - invalid password for user {Username}", request.Username);
                throw new DomainException("Invalid username or password.");
            }

            var adapterRequest = request with { SageCustomerCode = user.SageCustomerCode };
            var result = await _authAdapter.LoginAsync(adapterRequest, cancellationToken);

            return result with { SageCustomerCode = user.SageCustomerCode };
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

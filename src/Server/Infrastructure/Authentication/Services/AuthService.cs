using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly IAuthAdapter _authAdapter;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthAdapter authAdapter,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthService> logger)
    {
        _authAdapter = authAdapter;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            _logger.LogWarning("User {Username} was not found during login.", request.Username);
            throw new DomainException("Invalid username or password.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Invalid credentials supplied for {Username}.", request.Username);
            throw new DomainException("Invalid username or password.");
        }

        try
        {
            var result = await _authAdapter.LoginAsync(request, user.SageCustomerCode, cancellationToken);
            return result with { SageCustomerCode = user.SageCustomerCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate user {Username}", request.Username);
            throw new DomainException("Unable to authenticate with Sage.", ex);
        }
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SageCustomerCode))
        {
            _logger.LogWarning("Registration for {Username} is missing a Sage customer code.", request.Username);
            throw new DomainException("A Sage customer code is required to register.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Username,
            SageCustomerCode = request.SageCustomerCode
        };

        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(error => error.Description));
                _logger.LogWarning("Failed to register user {Username}: {Error}", request.Username, errorMessage);
                throw new DomainException($"Unable to register user: {errorMessage}");
            }
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while registering user {Username}", request.Username);
            throw new DomainException("Unable to register user.", ex);
        }
    }
}

using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Services;

public interface IUserOnboardingService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}

public class UserOnboardingService : IUserOnboardingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserOnboardingService> _logger;

    public UserOnboardingService(UserManager<ApplicationUser> userManager, ILogger<UserOnboardingService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.SageCustomerCode))
        {
            throw new DomainException("Sage customer code is required.");
        }

        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser is not null)
        {
            throw new DomainException("A user with the provided username already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            SageCustomerCode = request.SageCustomerCode
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(error => error.Description));
            _logger.LogWarning("Failed to register user {Username}: {Errors}", request.Username, errors);
            throw new DomainException($"Failed to create user: {errors}");
        }

        _logger.LogInformation("Registered user {Username} with Sage customer code {SageCustomerCode}", request.Username, request.SageCustomerCode);

        return new RegistrationResult(user.Id, user.UserName!, user.SageCustomerCode);
    }
}

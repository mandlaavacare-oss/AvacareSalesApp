using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Server.Infrastructure.Authentication.Services;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}

public class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySeedOptions> options,
    ILogger<IdentitySeeder> logger) : IIdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IdentitySeedOptions _options = options.Value;
    private readonly ILogger<IdentitySeeder> _logger = logger;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureRoleExistsAsync(RoleNames.Admin, cancellationToken);
        await EnsureRoleExistsAsync(RoleNames.Customer, cancellationToken);

        await EnsureUserAsync(_options.Admin, RoleNames.Admin, cancellationToken);
        await EnsureUserAsync(_options.Customer, RoleNames.Customer, cancellationToken);
    }

    private async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("Created role {Role}", roleName);
    }

    private async Task EnsureUserAsync(IdentitySeedOptions.SeedUserOptions userOptions, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userOptions.UserName) || string.IsNullOrWhiteSpace(userOptions.Password))
        {
            _logger.LogWarning("Seed user for role {Role} is not configured.", roleName);
            return;
        }

        var user = await _userManager.FindByNameAsync(userOptions.UserName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userOptions.UserName,
                Email = userOptions.Email,
                CustomerId = userOptions.CustomerId
            };

            var result = await _userManager.CreateAsync(user, userOptions.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user {userOptions.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            _logger.LogInformation("Created seed user {User}", userOptions.UserName);
        }
        else if (!string.Equals(user.CustomerId, userOptions.CustomerId, StringComparison.Ordinal))
        {
            user.CustomerId = userOptions.CustomerId;
            await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign role {roleName} to {userOptions.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            _logger.LogInformation("Assigned role {Role} to user {User}", roleName, userOptions.UserName);
        }
    }
}

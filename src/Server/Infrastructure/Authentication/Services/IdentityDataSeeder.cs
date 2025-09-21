using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Services;

public class IdentityDataSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<IdentityDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await EnsureRoleExistsAsync(RoleNames.Admin);
        await EnsureRoleExistsAsync(RoleNames.Customer);

        await EnsureAdminUserAsync();
        await EnsureSampleCustomerAsync();
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create role {Role}: {Errors}", roleName, errors);
            throw new InvalidOperationException($"Unable to create role '{roleName}'.");
        }

        _logger.LogInformation("Seeded role {Role}", roleName);
    }

    private async Task EnsureAdminUserAsync()
    {
        const string username = "admin";
        const string email = "admin@local";
        const string password = "Admin123$";

        var user = await _userManager.FindByNameAsync(username);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create admin user: {Errors}", errors);
                throw new InvalidOperationException("Unable to seed admin user.");
            }

            _logger.LogInformation("Seeded admin user {Username}", username);
        }

        if (!await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            var result = await _userManager.AddToRoleAsync(user, RoleNames.Admin);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign admin role: {Errors}", errors);
                throw new InvalidOperationException("Unable to assign admin role.");
            }
        }
    }

    private async Task EnsureSampleCustomerAsync()
    {
        const string username = "customer";
        const string email = "customer@local";
        const string password = "Customer123$";
        const string customerId = "CUST-0001";

        var user = await _userManager.FindByNameAsync(username);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                CustomerId = customerId
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create sample customer: {Errors}", errors);
                throw new InvalidOperationException("Unable to seed sample customer user.");
            }

            _logger.LogInformation("Seeded customer user {Username}", username);
        }

        if (!await _userManager.IsInRoleAsync(user, RoleNames.Customer))
        {
            var result = await _userManager.AddToRoleAsync(user, RoleNames.Customer);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign customer role: {Errors}", errors);
                throw new InvalidOperationException("Unable to assign customer role.");
            }
        }
    }
}

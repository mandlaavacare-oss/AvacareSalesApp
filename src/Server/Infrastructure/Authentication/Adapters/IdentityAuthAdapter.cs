using Microsoft.AspNetCore.Identity;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;

namespace Server.Infrastructure.Authentication.Adapters;

public class IdentityAuthAdapter(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService) : IAuthAdapter
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ITokenService _tokenService = tokenService;

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        var token = await _tokenService.CreateTokenAsync(user, cancellationToken);
        return new LoginResult(user.UserName ?? request.Username, token);
    }
}

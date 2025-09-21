using Microsoft.AspNetCore.Identity;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;

namespace Server.Infrastructure.Authentication.Adapters;

public class SageAuthAdapter : IAuthAdapter
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public SageAuthAdapter(UserManager<ApplicationUser> userManager, IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            throw new DomainException("Invalid username or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new DomainException("Invalid username or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenGenerator.GenerateToken(user, roles);

        return new LoginResult(user.UserName ?? request.Username, token);
    }
}

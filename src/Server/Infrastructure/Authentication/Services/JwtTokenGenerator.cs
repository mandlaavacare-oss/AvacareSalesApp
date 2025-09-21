using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Infrastructure.Authentication.Models;

namespace Server.Infrastructure.Authentication.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}

public class JwtTokenGenerator(IOptions<JwtOptions> options)
    : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email!));
        }

        if (!string.IsNullOrWhiteSpace(user.CustomerId))
        {
            claims.Add(new Claim(CustomClaimTypes.CustomerId, user.CustomerId!));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

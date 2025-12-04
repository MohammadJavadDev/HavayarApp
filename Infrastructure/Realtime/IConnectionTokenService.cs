using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Realtime.Options;

namespace WebApp.Services.Realtime;

public interface IConnectionTokenService
{
    string Generate(long userId, string? userName = null);
}

public sealed class ConnectionTokenService(IOptions<ConnectionTokenOptions> options)
    : IConnectionTokenService
{
    private readonly ConnectionTokenOptions _options = options.Value;

    public string Generate(long userId, string? userName = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        if (!string.IsNullOrWhiteSpace(userName))
        {
            claims.Add(new Claim(ClaimTypes.Name, userName));
        }
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(_options.LifetimeSeconds),
            signingCredentials: creds
        );

        return handler.WriteToken(token);
    }
}



using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Entities.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Services.Auth;

public class AuthService(IConfiguration _configuration) : IAuthService
{
    public string GenerateToken(User user)
    {
		 

        var key = Encoding.ASCII.GetBytes(AppSettings.PrivateKey);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

        var claim =new  List<Claim>();
			 
        GenerateClaims(user, claim);

        var token = new JwtSecurityToken(
            claims: claim,
            signingCredentials: credentials,
            expires: DateTime.Now.AddDays(120),
            issuer: AppSettings.Issuer,
            audience: AppSettings.Audience
        );

		 

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void GenerateClaims(User user , List<Claim> claims)
    {	 
        claims.Add(new Claim(ClaimTypes.Name, user.Name));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));			 
    }
}
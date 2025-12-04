using Entities.Auth;

namespace Services.Auth;

public interface IAuthService
{
    public string GenerateToken(User user);
}
namespace Entities.Auth;

public class AuthenticateResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string Token { get; set; }


    public AuthenticateResponse(User user, string token)
    {
        Id =  (long)user.Id;
        Name = user.Name;
        Username = user.Username;
        Token = token;
    }
}
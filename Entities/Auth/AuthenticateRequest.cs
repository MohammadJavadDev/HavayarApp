using System.ComponentModel;

namespace Entities.Auth;

public class AuthenticateRequest
{
    [DefaultValue("System")]
    public required string Username { get; set; }

    [DefaultValue("System")]
    public required string Password { get; set; }
    public string? FName { get; set; }
}
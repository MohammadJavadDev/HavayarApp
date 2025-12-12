
using Entities.Auth;

namespace Entities.ViewModels;

public class CreateUserViewModel
{
    public long? Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string? Password { get; set; }
    public string[]? Roles { get; set; }
	public List<long>? RoleIds { get; set; }
	public string? FName { get; set; }
    public string? ProfileUrl { get; set; }
	public AuthorizationTypeEnum AuthorizationType   { get; set; }
	 
}
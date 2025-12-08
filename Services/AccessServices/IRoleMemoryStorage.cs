using Entities.Auth;

namespace Services.AccessServices;

public interface IRoleMemoryStorage
{
    void SetRoles(List<Role> roles);
    List<Role> GetRoles();
    Role GetRoleByName(string roleName);
    Role? GetRoleBy(Func<Role, bool> predicate);
    bool ExistPath(string path);
    void AddRole(Role role);
    void UpdateRoleAccessPaths(long roleId, List<string> accessPaths);
    public bool HaveAccessByRole(string path, string roleName);
    public Role GetRoleById(long roleId);
}
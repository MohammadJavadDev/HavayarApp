using Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AccessServices
{
    public class RoleMemoryStorage : IRoleMemoryStorage
    {
        private List<Role> _roles = new ();

        public void SetRoles(List<Role> roles)
        {
            _roles = roles;
        }

        public List<Role> GetRoles()
        {
            return _roles;
        }

        public bool HaveAccessByRole(string path  ,string roleName)
        {
            if(roleName =="admin")
            {
                return true;
            }
          return  _roles?.FirstOrDefault(c => c.Name == roleName)
                ?.RoleAccesses?.Any(c => c.Path == path) ?? false;
		}

        public Role GetRoleByName(string roleName)
        {
            return _roles.FirstOrDefault(r => r.Name == roleName);
        }

        public Role GetRoleById(long roleId)
        {
            return _roles.FirstOrDefault(r => r.Id == roleId);
        }
        public bool ExistPath(string path)
        {
           return _roles.Any(c => c.RoleAccesses.Any(z => z.Path == path));
        }

        public void AddRole(Role role)
        {
            _roles.Add(role);
        }

        public void UpdateRoleAccessPaths(long roleId, List<string> accessPaths)
        {
               throw new NotImplementedException();
            //var role = GetRoleById(roleId);
            //if (role != null)
            //{
            //    role.AccessPath = accessPaths;
            //}
        }

		Role? IRoleMemoryStorage.GetRoleBy(Func<Role, bool> predicate)
		{
			throw new NotImplementedException();
		}
	}

}

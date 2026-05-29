using Common.System;
using Common.Utilities;
using Data.Contracts;
 
using Entities.Auth;
using Entities.Base.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Attributes;
using Common.Auth.Enums;
using Data.SystemAuth;

namespace Services.AccessServices
{
    public class MenuBuilderService(
         IRoleMemoryStorage roleMemoryStorage 
         , IAccessMemoryStorage accessMemoryStorage) : IMenuBuilderService
    {
        private readonly List<SystemMenu> _systemMenus = new();
        private readonly Dictionary<long, SystemMenu> _systemMenusById = new();
        private readonly object _lockObj = new();

        public List<SystemMenuItem> GetMenuItems(long? id ,ISdk sdk)
        {

               var roles = sdk.CurrentUser.RoleIds;

		  if (sdk.CurrentUser.IsAdministrator && !id.HasValue)
            {
                return CreateMenuItemFromSystemPaths();
            }

            SystemMenu menu = null;
            
            if (id.HasValue)
            {
               
                lock (_lockObj)
                {
                    if (!_systemMenusById.TryGetValue(id.Value, out menu))
                    {
                        menu = _systemMenus.FirstOrDefault(c => c.Id == id);
                        if (menu != null)
                        {
                            _systemMenusById[id.Value] = menu;
                        }
                    }
                }
            }
            else
            {

               var menus = GetMenuByRole(sdk);
			 menu = menus.FirstOrDefault();
            }

    
            if (menu == null)
            {
                return new List<SystemMenuItem>();
            }

            // Deserialize and filter items by access
            var items = menu.Content.JsonDeserialize<List<SystemMenuItem>>();
            return AccessedItems(items, roles.ToArray());
        }

        private List<SystemMenuItem> CreateMenuItemFromSystemPaths()
        {

           var accessControllers = accessMemoryStorage.GetAllAccessControllers();
           var r = new List<SystemMenuItem>();

			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			if (env == "Production")
			{
		     	var fb =	accessControllers.FindIndex(c=>c.Name == "FormBuilder");
                    accessControllers.RemoveRange(fb,1);
			}

			foreach (var ac in accessControllers)
            {
                
                var nit = new SystemMenuItem()
                {
                    Text = ac.DisplayName,
                    Path = ac.Path,
                    show = false,
                    Children = new ()
                };

                foreach (var aa in ac.Actions)
                {
                    if(aa.ActionAccessType == ActionAccessType.View)
                        nit.Children.Add(
                            new()
                            {
                                Path = aa.Path,
                                show = false,
                                Text = aa.DisplayName,
                                
                            });

                }
               r.Add(nit);

            }

            return r;
             
        }
        private List<SystemMenuItem> AccessedItems(List<SystemMenuItem> items , long[] roles)
        {
            var returnList = new List<SystemMenuItem>();
            foreach(var i in items)
            {
                if(i.Children.Count > 0)
                {
					i.Children =  AccessedItems(i.Children , roles);

				}
                foreach (var r in roles)
                {
                    if(i.Path.HasValue(true) && i.Path != "#")
                    {
	                    if (!roleMemoryStorage.HaveAccessByRole(i.Path, r)) continue;
	                    if(!returnList.Any(c=>c.Path == i.Path && c.Text == i.Text))
		                    returnList.Add(i);
                    }
                    else
                    {
						if (!returnList.Any(c => c.Path == i.Path && c.Text == i.Text))
							returnList.Add(i);
					}
                    
                }
                if(i is { Children.Count: 0 } && (!i.Path.HasValue(true) || i.Path == "#"))
                {
                    returnList.Remove(i);

				}
                 
            }
            return returnList;
        }

        public List<SystemMenu> GetMenuByRole( ISdk sdk)
        {
               var roles = sdk.CurrentUser.RoleIds;
			// Early exit for admin role (role ID 1)
			if (sdk.CurrentUser.IsAdministrator)
			{
				return _systemMenus;
			}

			// Use HashSet for O(1) lookups when checking for duplicates
			var accessMenu = new List<SystemMenu>();
            var addedMenuIds = new HashSet<long>();

               
            if (roles != null)
            {
                foreach (var roleId in roles)
                {
                    // Find menus that have this role ID in their AccessRoleIds
                    var menusWithRole = _systemMenus.Where(c => c.AccessRoleIds != null && c.AccessRoleIds.Contains(roleId));
                    
                    foreach (var menu in menusWithRole)
                    {
                        if (menu.Id.HasValue && addedMenuIds.Add(menu.Id.Value))
                        {
                            accessMenu.Add(menu);
                        }
                    }
                }
            }

            return accessMenu;
		}
        public void UpdateSystemMenu(SystemMenu systemMenu)
        {
            lock (_lockObj)
            {
                var menu = _systemMenus.FirstOrDefault(c => c.Id == systemMenu.Id);
                if (menu != null)
                {
                    // Update existing menu
                    menu.Title = systemMenu.Title;
                    menu.Content = systemMenu.Content;
                    menu.Name = systemMenu.Name;
                    menu.AccessRoles = systemMenu.AccessRoles;
                    menu.AccessRoleIds = systemMenu.AccessRoleIds;
                    
                    // Update cache
                    _systemMenusById[systemMenu.Id.Value] = menu;
                }
                else
                {
                    // Add new menu
                    _systemMenus.Add(systemMenu);
                    if (systemMenu.Id.HasValue)
                    {
                        _systemMenusById[systemMenu.Id.Value] = systemMenu;
                    }
                }
            }
        }

        public string GetHtmlItems(List<SystemMenuItem> items ,string? url,ISdk sdk)
        {
            return RenderMenuItems(items, url, sdk);
        }

        public bool MenuIsActive(List<SystemMenuItem> items , string? url)
        {
            var active = false;
           foreach(var i in items)
            {
                if(i.Children != null && i.Children.Any(c => c.Path == url))
                {
                    i.show = true;
                    return true;
                }

                if (i.Children !=null && i.Children.Any())
                {
                    active = MenuIsActive(i.Children , url);
                    i.show = active;
                }

            }
           return active;
        }

		public string RenderMenuItems(List<SystemMenuItem> items, string? url, ISdk sdk)
		{
			var currentUserRoleAccess = sdk.CurrentUser.RoleAccess;
			var isAdmin = sdk.CurrentUser.IsAdministrator;

			// فیلتر کردن منوها بر اساس دسترسی
			items = FilterMenuItems(items, currentUserRoleAccess, isAdmin);

			var html = new StringBuilder();
			MenuIsActive(items, url);

			foreach (var c in items)
			{
				if (c.Children != null && c.Children.Any())
				{
					var show = "";

					if (c.show)
					{
						show = "show here";
					}

					var icon = c.Icon;
					if (icon != null && !icon.Contains("<i"))
					{
						var styleColor = "";
						if (c.iconColor.HasValue())
						{
							styleColor = $"style='color:{c.iconColor}'";
						}
						icon = $"<i class='{c.Icon}' {styleColor}></i>";
					}

					html.Append(
					    $"""
                 <div data-kt-menu-trigger="click" class="menu-item menu-accordion {show}">
                     <span class="menu-link">
                         <span class="menu-icon">
                             {icon}
                         </span>
                         <span class="menu-title">{c.Text}</span>
                         <span class="menu-arrow"></span>
                     </span>
                     <div class="menu-sub menu-sub-accordion">
                         {RenderMenuItems(c.Children, url, sdk)}
                     </div>
                 </div>
                 """
					);
				}
				else
				{
					var iconString = "";
					var isActive = "";

					var icon = c.Icon;
					if (icon != null && !icon.Contains("<i"))
					{
						var styleColor = "";
						if (c.iconColor.HasValue())
						{
							styleColor = $"style='color:{c.iconColor}'";
						}
						icon = $"<i class='{c.Icon}' {styleColor}></i>";
					}

					if (c.Icon.HasValue(true))
					{
						iconString = $"""
                              <span class="menu-icon">
                                  {icon}
                              </span>
                              """;
					}
					else
					{
						iconString = """
                             <span class="menu-bullet">
                                 <span class="bullet bullet-dot"></span>
                             </span>
                             """;
					}

					if (c.Path == url)
					{
						isActive = "active";
					}

					html.Append(
					    $"""
                 <div class="menu-item">
                     <a class="menu-link {isActive}" href="{c.Path}">
                         {iconString}
                         <span class="menu-title">{c.Text}</span>
                     </a>
                 </div>
                 """
					);
				}
			}

			return html.ToString();
		}

		private List<SystemMenuItem> FilterMenuItems(List<SystemMenuItem> items, List<RoleAccessDto> roleAccess, bool isAdmin)
		{
			if (isAdmin)
				return items;

			var filtered = new List<SystemMenuItem>();

			foreach (var item in items)
			{
				if (item.Children != null && item.Children.Any())
				{
					// فیلتر کردن فرزندان به صورت بازگشتی
					var filteredChildren = FilterMenuItems(item.Children, roleAccess, isAdmin);
					if (filteredChildren.Any())
					{
						// کپی کردن آیتم و جایگزینی فرزندان فیلتر شده
						var newItem = CloneMenuItem(item);
						newItem.Children = filteredChildren;
						filtered.Add(newItem);
					}
				}
				else
				{
					// آیتم نهایی: فقط در صورتی مجاز است که Path آن در roleAccess وجود داشته باشد
					if (!string.IsNullOrEmpty(item.Path) && roleAccess.Any(ra => ra.Path == item.Path))
					{
						filtered.Add(item);
					}
				}
			}

			return filtered;
		}

		// متد کمکی برای clone کردن SystemMenuItem (با فرض اینکه یک کپی سطحی کافی است)
		private SystemMenuItem CloneMenuItem(SystemMenuItem original)
		{
			return new SystemMenuItem
			{
				Text = original.Text,
				Path = original.Path,
				Icon = original.Icon,
				iconColor = original.iconColor,
				Children = original.Children, // بعداً جایگزین می‌شود
				show = original.show
				// دیگر پراپرتی‌های مورد نیاز را نیز کپی کنید
			};
		}

		public void SetSystemMenu(List<SystemMenu> systemMenus)
        {
            lock (_lockObj)
            {
                _systemMenus.Clear();
                _systemMenusById.Clear();
                
                if (systemMenus != null)
                {
                    _systemMenus.AddRange(systemMenus);
                    
                    // Populate the dictionary cache for O(1) lookups
                    foreach (var menu in systemMenus)
                    {
                        if (menu.Id.HasValue)
                        {
                            _systemMenusById[menu.Id.Value] = menu;
                        }
                    }
                }
            }
        }
        public List<SystemMenu> GetSystemMenu()
        {
           return _systemMenus;
        }
    }

    public interface IMenuBuilderService
    {
        public List<SystemMenuItem> GetMenuItems(long? id, ISdk sdk);
        public void SetSystemMenu(List<SystemMenu> systemMenus);
        public List<SystemMenu> GetMenuByRole(ISdk sdk);
        public List<SystemMenu> GetSystemMenu();
        public void UpdateSystemMenu(SystemMenu systemMenu);
          public string GetHtmlItems(List<SystemMenuItem> items, string? url, ISdk sdk);
    }

}

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

namespace Services.AccessServices
{
    public class MenuBuilderService(IRoleMemoryStorage roleMemoryStorage , IAccessMemoryStorage accessMemoryStorage) : IMenuBuilderService
    {
        private List<SystemMenu> _systemMenus = new();

        public List<SystemMenuItem> GetMenuItems(long? id , string[] roles)
        {
            SystemMenu menu;

			if (id is null)
            {
                  menu = _systemMenus.FirstOrDefault();

			}
            else
            {
				menu = _systemMenus.FirstOrDefault(c => c.Id == id);
			}

            if(menu == null && roles.All(c=>c != "admin"))
            {
                return new List<SystemMenuItem>();
            }
            else if (roles.Any(c => c == "admin"))
            {
                return CreateMenuItemFromSystemPaths();
            }
          

            var items =  menu?.Content.JsonDeserialize<List<SystemMenuItem>>();
            var re = AccessedItems(items, roles);
            return re;
        }

        private List<SystemMenuItem> CreateMenuItemFromSystemPaths()
        {

           var accessControllers = accessMemoryStorage.GetAllAccessControllers();
           var r = new List<SystemMenuItem>();

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
        private List<SystemMenuItem> AccessedItems(List<SystemMenuItem> items , string[] roles)
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

        public List<SystemMenu> GetMenuByRole(string[] roles)
        {
            var accessMenu = new  List<SystemMenu>();

                if(roles.Any(c=>c == "admin"))
                {
                    return _systemMenus;
                }

                foreach (var r in roles)
                {
                   var am = _systemMenus.FirstOrDefault(c => c.AccessRoles.Contains(r));
                    if(am != null && accessMenu.All(z => z.Id != am.Id))
                    {
                       accessMenu.Add(am);
					}
				}
                return accessMenu;
		}
        public void UpdateSystemMenu(SystemMenu systemMenu)
        {
            var menu = _systemMenus.FirstOrDefault(c=>c.Id == systemMenu.Id);
            if(menu != null)
            {
                menu.Title = systemMenu.Title;
                menu.Content = systemMenu.Content;
                menu.Name = systemMenu.Name;
                menu.AccessRoles = systemMenu.AccessRoles;
            }
            else
            {
                _systemMenus.Add(systemMenu);
            }
        }

        public string GetHtmlItems(List<SystemMenuItem> items ,string url)
        {
            return RenderMenuItems(items, url);
        }

        public bool MenuIsActive(List<SystemMenuItem> items , string url)
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

        public string RenderMenuItems(List<SystemMenuItem> items, string url)
        {
            var html = new StringBuilder();
            MenuIsActive(items, url);

            foreach (var c in items)
            {
                 if(c.Children != null && c.Children.Any())
                 {
	                 var show = "";
                   

                     if (c.show)
	                 {
		                 show = "show here";
	                 }
	                 html.Append(
		                 $"""
		                     <div data-kt-menu-trigger="click" class="menu-item menu-accordion {show}">
		                  
		                                      <span class="menu-link">
		                                          <span class="menu-icon">
		                                             {c.Icon}
		                                          </span>
		                                          <span class="menu-title">{c.Text}</span>
		                                          <span class="menu-arrow"></span>
		                                      </span>
		                  
		                                      <div class="menu-sub menu-sub-accordion">
		                                     {RenderMenuItems(c.Children , url)}
		                           
		                                      </div>
		                  
		                                  </div>

		                  """
	                 );
                 }
                 else
                 {
	                 var iconString = "";
	                 var isActive = "";
	                 if (c.Icon.HasValue(true))
	                 {
		                 iconString = $"""
		                               
		                                                                    <span class="menu-icon">
		                                                                        {c.Icon}
		                                                                    </span>
		                               """;
	                 }
	                 else
	                 {
		                 iconString = $"""
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
		                  
		                           <a class="menu-link  {isActive}" href="{c.Path}">
		                               
		                               
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

        public void SetSystemMenu(List<SystemMenu> systemMenus)
        {
            _systemMenus = systemMenus;
        }
        public List<SystemMenu> GetSystemMenu()
        {
           return _systemMenus;
        }
    }

    public interface IMenuBuilderService
    {
        public List<SystemMenuItem> GetMenuItems(long? id, string[] roles);
        public void SetSystemMenu(List<SystemMenu> systemMenus);

        public List<SystemMenu> GetMenuByRole(string[] roles);
        public List<SystemMenu> GetSystemMenu();
        public void UpdateSystemMenu(SystemMenu systemMenu);
        public string GetHtmlItems(List<SystemMenuItem> items ,string url);
    }

}

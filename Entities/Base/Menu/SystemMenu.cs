using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.Menu
{
	[Table(name: "SystemMenu", Schema = "system")]
	public class SystemMenu :BaseEntity
    {
        [MaxLength(150)]
        public string Title { get; set; }
        [MaxLength(150)]
        public string Name { get; set; }
        public string Content { get; set; }

        public List<string> AccessRoles { get; set; }
        [NotMapped] 
        public List<SystemMenuItem>? SystemMenuItems { get; set; }
    }
	[Table(name: "SystemMenuItem", Schema = "system")]

	public class SystemMenuItem 
    {
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Path { get; set; }
        public List<SystemMenuItem> Children { get; set; }
        [NotMapped] public bool show { get; set; } = false;

    }

   
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.DataTable
{
	[Table(name: "SystemDataTableProfile", Schema = "system")]
	public class SystemDataTableProfile :BaseEntity
    {
        public string? EntityName { get; set; }
        public string? EntitySchema { get; set; }
        public string Title { get; set; }
        public string? Columns { get; set; }
        public string? Filters { get; set; }
        public string? SelectQuery { get; set; }
        public string? FromQuery { get; set; }
        public string? FilterQuery { get; set; }
        [NotMapped] 
        public List<SystemDataTableProfileSelectViewModel> SystemDataTableProfileSelectViewModels { get; set; }

        [NotMapped]
        public List<SystemDataTableProfileSelectViewModel> SystemDataTableProfileFilterViewModels { get; set; }

    }
 
	public class SystemDataTableProfileSelectViewModel
    {
        public string? PropName { get; set; }
        public string? Alliance { get; set; }
        public string? Label { get; set; }
        public string? TableName { get; set; }
        public string? ForgesKeyName { get; set; }
        public string? Type { get; set; }
        public string? JoinString { get; set; }
        public int Level { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public bool Filter { get; set; } = false;
        public string? Criteria { get; set; }
        public List<string>? Value { get; set; }
        public string FilterString { get; set; }
        public string Render { get; set; }
        public string showTitle { get; set; }
          public bool? Visible { get; set; } = true;
          public List<OptionViewModel>? options { get; set; }
    }

    public class OptionViewModel
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }
}

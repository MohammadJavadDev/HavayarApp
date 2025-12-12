using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Attributes;
using Common.Entities;
using Entities.Base;
 

namespace Entities.Auth;
[Table(name: "Role", Schema = "system")]
public class Role:BaseEntity
{
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نام")]
    [DisplayInfo(null, true, type: SystemType.String)]
    [DisplayName("نام")]
    public string Name { get; set; }
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "عنوان")]
    [DisplayInfo(null, true, type: SystemType.String ,showInRelationData:true)]
    [DisplayName("عنوان")]
    public string Title { get; set; }
 
 
    public List<RoleAccess> RoleAccesses { get; set; } = new();
}

 
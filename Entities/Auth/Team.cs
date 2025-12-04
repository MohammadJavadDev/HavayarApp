using System.ComponentModel.DataAnnotations;
using Common.Entities;
using Entities.Base;

namespace Entities.Auth;

public class Team:BaseEntity
{
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "نام")]
    public string Name { get; set; }
    [Required(ErrorMessage = AppMessages.ReqiredMessage + "عنوان")]
    public string Title { get; set; }
    public string[] UserIdTeam { get; set; }
}
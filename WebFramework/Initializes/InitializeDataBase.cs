using Data.Contracts;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFramework.Initializes
{
    public class InitializeDataBase(IUnitOfWork unitOfWork , IUserService userService):
        IInitializeDataBase
    {
        public void InitAdminUser()
        {
          var roleAdmin = unitOfWork.Repository<Role>().TableNoTracking
                .FirstOrDefault(t => t.Name == "admin");

            if (roleAdmin == null)
            {
                unitOfWork.Repository<Role>().Add(
                    new Role()
                    {
                        Name = "admin",
                        Title = "مدیرسیستم",
                    });
            }

                var userAdmin = userService.Context.Users.FirstOrDefault(c => c.Username == "admin");
            if (userAdmin == null) {


                userService.CreateUser(new()
                {
                    Username = "admin", 
                    FName = "admin",
                    Id = 1,
                    Name = "مدیر سیستم",
                    Password = "nimda123456",
                    Roles = new string[1]
                    {
                        "admin"
                    }
                     
                });
            }
        }
    }

    public interface IInitializeDataBase
    {
        public void InitAdminUser();
    }
}

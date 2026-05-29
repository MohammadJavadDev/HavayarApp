using Data;
using Data.Contracts;
using Data.Repositories;
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
    public class InitializeDataBase(IUnitOfWork unitOfWork 
         , IUserService userService,
         ApplicationDbContext applicationDbContext):
        IInitializeDataBase
    {
        public void InitAdminUser()
        {
		  applicationDbContext.Database.Migrate();


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
                    Roles = new ()
                    {
                        "admin"
                    },
				RoleIds  = new()
				{
				 1
				},
				 AuthorizationType = AuthorizationTypeEnum.System,

			 });
            }
        }
    }

    public interface IInitializeDataBase
    {
        public void InitAdminUser();
    }
}

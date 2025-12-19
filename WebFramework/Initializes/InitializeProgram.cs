using Data.Contracts;
using Data.Repositories;
using Entities.Auth;

using Entities.Base.DataTable;
using Entities.Base.Menu;
using Entities.Base.Menu;
using Entities.Services;
using Microsoft.EntityFrameworkCore;
using Services.AccessServices;

namespace WebFramework.Initializes
{
    public class InitializeProgram(IUnitOfWork unitOfWork
         , IRoleMemoryStorage roleMemoryStorage,
        EndpointService endpointService ,
        IAccessMemoryStorage accessMemoryStorage,
        IMenuBuilderService menuBuilderService,
        IDataTableProfileService profilesService,
	   IEntityMetadataCache entityMetadataCache) : IInitializeProgram
    {

      public  void InitializeRoles()
        {
                var roles = unitOfWork.Repository<Role>().TableNoTracking
                    .Include(c=>c.RoleAccesses).ToList();
                roleMemoryStorage.SetRoles(roles);
          
        }
        public void InitializeAccessControllers( )
        {
         
                // Fetch all access controllers and store them in memory
                var accessControllers = endpointService.GetAllEndpoints();
                accessMemoryStorage.SetAccessControllers(accessControllers);
			accessMemoryStorage.SetAccessControllersRedis(accessControllers);
		}



        public void InitializeMenu( )
        {
           
                var menus = unitOfWork.Repository<SystemMenu>().TableNoTracking.ToList();

                menuBuilderService.SetSystemMenu(menus);
       
        }

        public void InitializeDataProfiles()
        {
           
                var profiles = unitOfWork.Repository<SystemDataTableProfile>().TableNoTracking.ToList();

                profilesService.SetDataTableProfile(profiles);
          
        }

		public void InitializeEntityMetadataCache()
		{
               entityMetadataCache.Refresh();
		}
	}

    public interface IInitializeProgram
    {
     public   void InitializeRoles();
        public void InitializeAccessControllers();
        public void InitializeMenu();
        public void InitializeDataProfiles();
          public void InitializeEntityMetadataCache();
    }
}

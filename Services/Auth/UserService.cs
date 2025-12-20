using Common.System;
using Common.Utilities;
using Data;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;


namespace Services.Auth;

public class UserService(
     IAuthService _authService,
     ApplicationDbContext db,
     ISdk? sdk,
	IActiveDirectoryService activeDirectoryService,
     IOnlineUserService onlineUserService) : IUserService
{

     public virtual ApplicationDbContext Context => db;
     public virtual IQueryable<User> Table => db.Users;
     public virtual IQueryable<User> TableNoTracking => db.Users.AsNoTracking();
     public async Task<AuthenticateResponse?> Authenticate(AuthenticateRequest model,CancellationToken ct = default)
     {
 
		var user = await db.Users.FirstOrDefaultAsync(x => x.Username == model.Username);

          if(user == null)
          {

			var activeDirectoryValidateCredentials = await activeDirectoryService.ValidateCredentialsAsync(model.Username, model.Password);

               if(activeDirectoryValidateCredentials == true)
               {
				var userInfoFromAd = activeDirectoryService.GetUserInfo(model.Username, model.Password);

                    string? folderPathImage = null;
				if (userInfoFromAd != null  )
				{

					   if(userInfoFromAd.ProfileImage != null && userInfoFromAd.ProfileImage.Length != 0)
                         {
						folderPathImage = Path.Combine(
					    Directory.GetCurrentDirectory(),
					    "wwwroot",
					    "UserProfileImage"
					);

						if (!Directory.Exists(folderPathImage))
							Directory.CreateDirectory(folderPathImage);

						var fileName = $"{model.Username}.jpg";
						var filePath = Path.Combine(folderPathImage, fileName);

						File.WriteAllBytes(filePath, userInfoFromAd.ProfileImage);

						folderPathImage = Path.Combine("UserProfileImage", fileName);
					}

					

					user = await AddUserAsync(new()
					{
						AuthorizationType = AuthorizationTypeEnum.ActiveDirectory,
						ProfileUrl = folderPathImage,
						IsActive = IsActiveEnum.Active,
						Name = userInfoFromAd.DisplayName,
						Username = model.Username,
						RoleIds = new(),
						Roles = []

					}, ct);
				}


				 
			}
		     if(user == null)
                 return null;
          }

		if (user.IsActive != IsActiveEnum.Active)
		{
               throw new Exception("حساب کاربری شما غیر فعال می باشد.");
		}

		if (user.AuthorizationType == AuthorizationTypeEnum.ActiveDirectory )
          {
               var activeDirectoryValidateCredentials = await activeDirectoryService.ValidateCredentialsAsync(model.Username, model.Password);
			if(!activeDirectoryValidateCredentials)
               {
				return null;
			}
		}
           
          else if (!PasswordHasher.VerifyPassword((user?.Password ?? ""), model.Password)) return null;

          await onlineUserService.AddOnlineUserAsync((long)user.Id,null);

		var token = _authService.GenerateToken(user);
           
		return new AuthenticateResponse(user, token);
     }
     public async Task<IEnumerable<User>> GetAll()
     {
          return await db.Users.Where(x => x.IsActive == IsActiveEnum.Active).ToListAsync();
     }
     public User? AddUser(User user)
     {


          user.ModifiedDateMiladiDateTime = DateTime.Now;
          user.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();


          user.CreatedOnMiladiDateTime = DateTime.Now;
          user.CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime();
          user.IsActive = IsActiveEnum.Active;
          if (sdk?.CurrentUser != null)
          {
               user.ModifiedById = sdk.CurrentUser.Id;

               user.ModifiedByName = sdk.CurrentUser.FullName;
               user.CreatedByName = sdk.CurrentUser.FullName;

               user.CreatedById = sdk.CurrentUser.Id;

          }

          if (db.Users.Any(c => c.Username == user.Username))
          {
               throw new Exception("نام کاربری تکراری میباشد.");
          }

          var entity = db.Users.Add(user);
          db.SaveChanges();

          return entity.Entity;


     }
     public async Task<User?> UpdateUserAsync(User userObj, CancellationToken cn)
     {
          var user = new User();
          var oldUser = db.Users.AsNoTracking().FirstOrDefault(c => c.Id == userObj.Id);

          user.Password = oldUser.Password;


          if (!string.IsNullOrEmpty(userObj.Password))
          {
               string hashedPassword = PasswordHasher.HashPassword(userObj.Password);
               user.Password = hashedPassword;
          }

          if (userObj.Email.HasValue())
          {
               user.Email = userObj.Email;
          }

          user.CreatedById = oldUser.CreatedById;
          user.CreatedByName = oldUser.CreatedByName;
          user.CreatedOnMiladiDateTime = oldUser.CreatedOnMiladiDateTime;
          user.CreatedOnShamsiDateTime = oldUser.CreatedOnShamsiDateTime;

          user.Username = userObj.Username;
          user.Name = userObj.Name;
          user.Roles = userObj.Roles;
          user.RoleIds = userObj.RoleIds;
          user.AuthorizationType = userObj.AuthorizationType;
          user.Id = (long)userObj.Id;

          user.ProfileUrl = userObj.ProfileUrl;


          var entity = db.Users.Update(user);
          await db.SaveChangesAsync(cn);
          return entity.Entity;
     }
     public User? UpdateUser(User user)
     {
          user.ModifiedDateMiladiDateTime = DateTime.Now;
          user.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();

          if (sdk?.CurrentUser != null)
          {
               user.ModifiedById = sdk.CurrentUser.Id;
               user.ModifiedByName = sdk.CurrentUser.FullName;


          }


          var entity = db.Users.Update(user);
          db.SaveChanges();
          return entity.Entity;
     }
     public async Task<User?> AddAndUpdateUserAsync(User userObj, CancellationToken cn)
     {
          if (userObj.Id != null)
          {

               return await UpdateUserAsync(userObj, cn);
          }
          else
          {
               return await AddUserAsync(userObj, cn);
          }

     }
     public User AddAndUpdateUser(User userObj)
     {
          if (userObj.Id != null)
          {

               return UpdateUser(userObj);
          }
          else
          {
               return AddUser(userObj);
          }
     }
     public async Task<User?> GetById(long id)
     {
          return await db.Users.FirstOrDefaultAsync(x => x.Id == id);
     }
     public async Task<User?> CreateUserAsync(CreateUserViewModel userObj, CancellationToken cn)
     {
          var user = new User();
          string hashedPassword = PasswordHasher.HashPassword(userObj.Password);
          user.Password = hashedPassword;
          user.Username = userObj.Username;
          user.Name = userObj.Name;
          user.Roles = userObj.Roles;
          user.Email = userObj.Email;

          user.ProfileUrl = userObj.ProfileUrl;
          return await AddUserAsync(user, cn);

     }
     public User? CreateUser(CreateUserViewModel userObj)
     {
          var user = new User();
          if(user.AuthorizationType != AuthorizationTypeEnum.ActiveDirectory)
          {
			string hashedPassword = PasswordHasher.HashPassword(userObj.Password);
			user.Password = hashedPassword;
		}
        
          user.Username = userObj.Username;
          user.Name = userObj.Name;
          user.Roles = userObj.Roles;
          user.AuthorizationType = userObj.AuthorizationType;
          user.RoleIds = userObj?.RoleIds ?? [];
          user.Email = userObj.Email;   


          user.ProfileUrl = userObj.ProfileUrl;
          return AddUser(user);

     }
     private async Task<User?> AddUserAsync(User user, CancellationToken cn)
     {

          user.ModifiedDateMiladiDateTime = DateTime.Now;
          user.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();


          user.CreatedOnMiladiDateTime = DateTime.Now;
          user.CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime();
          user.IsActive = IsActiveEnum.Active;
          if (sdk?.CurrentUser != null)
          {
               user.ModifiedById = sdk.CurrentUser.Id;

               user.ModifiedByName = sdk.CurrentUser.FullName;
               user.CreatedByName = sdk.CurrentUser.FullName;

               user.CreatedById = sdk.CurrentUser.Id;

          }

          if (db.Users.Any(c => c.Username == user.Username))
          {
               throw new Exception("نام کاربری تکراری میباشد.");
          }

          var entity = await db.Users.AddAsync(user, cn);
          await db.SaveChangesAsync(cn);

          return entity.Entity;

     }
     public async Task<User?> UpdateUser(CreateUserViewModel userObj, CancellationToken cn)
     {
          var user = new User();
          var oldUser = db.Users.AsNoTracking().FirstOrDefault(c => c.Id == userObj.Id);

          user.Password = oldUser.Password;


          if (!string.IsNullOrEmpty(userObj.Password) && userObj.AuthorizationType  != AuthorizationTypeEnum.ActiveDirectory)
          {
               string hashedPassword = PasswordHasher.HashPassword(userObj.Password);
               user.Password = hashedPassword;
          }

		if (userObj.Email.HasValue())
		{
			user.Email = userObj.Email;
		}


		user.CreatedById = oldUser.CreatedById;
          user.CreatedByName = oldUser.CreatedByName;
          user.CreatedOnMiladiDateTime = oldUser.CreatedOnMiladiDateTime;
          user.CreatedOnShamsiDateTime = oldUser.CreatedOnShamsiDateTime;

          user.Username = userObj.Username;
          user.Name = userObj.Name;
          user.Roles = userObj.Roles;
          user.Id = (long)userObj.Id;

          user.ProfileUrl = userObj.ProfileUrl;
          return await UpdateUserAsync(user, cn);

     }

     public async Task<IEnumerable<User>> SearchByName(string name)
     {
          return await db.Users.Select(c => new User() { Id = c.Id, Name = c.Name }).Where(c => c.Name.Contains(name)).ToArrayAsync();
     }


}
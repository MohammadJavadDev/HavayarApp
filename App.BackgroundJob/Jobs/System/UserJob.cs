using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.Auth;
using Entities.Base;
using Entities.Rahkaran.SLS3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.BackgroundJob.Jobs.System
{
	public class UserJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork, ApplicationDbContext appContext)
	{

	[JobHandler("افزودن کاربران از راهکاران")]
	public async Task AddContractFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
	{
			 var rahKaranUsers = await Rdb.RahkaranUsers.AsNoTracking()
				.Where(c=>
				c.DomainUserName != null 
				&& c.Type ==2 
				&& c.Status == 1
				).ToArrayAsync();

			var listParties = rahKaranUsers.Select(c=>c.PartyRef).ToList();


			var appParties = await unitOfWork.Repository<Party>()
				.TableNoTracking
				.Where(c => listParties.Contains(c.HamkaranId))
				.ToListAsync(cn);

			var appUsers = await appContext.Users.ToListAsync();

			var usersMap = appUsers.ToDictionary(x => x.Username, x => x.Id);
			var pariesMap = appParties.ToDictionary(x => x.HamkaranId, x => new{ Id = x.Id , FullName= x.FullName });


			foreach (var rahKaranUser in rahKaranUsers)
			{
			


				if (usersMap.TryGetValue(rahKaranUser.DomainUserName, out var foundUserId))
				{
					appUsers.First(c=>c.Id == foundUserId).HamkaranId = rahKaranUser.UserID;

					if (pariesMap.TryGetValue(rahKaranUser.PartyRef, out var foundParty))
					{
						appUsers.First(c => c.Id == foundUserId).PartyId = foundParty.Id;
						appUsers.First(c => c.Id == foundUserId).NameFa = foundParty.FullName;

					}

				}
				else
				{
					var user = new User();
				  
					user.Username = rahKaranUser.DomainUserName;
					user.Name = rahKaranUser.Name;
					user.Roles = [];
					user.RoleIds = [];
					user.AuthorizationType =AuthorizationTypeEnum.ActiveDirectory;
					user.HamkaranId = rahKaranUser.UserID;
					user.IsActive = IsActiveEnum.Active;
					user.Email = rahKaranUser.DomainUserName+"@havayar.com";
					appContext.Users.Add(user);
				}
				 
			}

		  await	appContext.SaveChangesAsync(cn);
		}
	}
}


using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Data.Repositories;
using Entities.App.Edms;
using Entities.App.Hrm;

namespace WebApp.Actions.Edms
{

	public class ProjectAction(IUnitOfWork unitOfWork) : IEntityAction<Project>
	{
		[EntityAction(typeof(Project), EntityActionTrigger.BeforeSave, "CheckCode", "بررسی و تولید کد پروژه", Priority = 1)]
		public async Task CheckCode(Project entity, CancellationToken ct)
		{
		  
			if (!entity.Code.HasValue())
				entity.Code = GetProjectCode(entity);

			await Task.CompletedTask;
		}


		private string GetOrganizationUnit(long organizationUnitId)
		{

			var orgUnitCode = unitOfWork.Repository<OrgUnit>()
				.TableNoTracking
				.Select(c => new { c.Id, c.ProjectPrefixCode })
				.FirstOrDefault(c => c.Id == organizationUnitId);

			if (orgUnitCode == null)
				throw new Exception("واحد سازمانی مورد نظر یافت نشد.");


			if (!orgUnitCode.ProjectPrefixCode.HasValue())
				throw new Exception("برای واحد سازمانی انتخاب شده پیش کد پروژه تعریف نشده .");

			return orgUnitCode.ProjectPrefixCode;



			//    1   مهندسی فروش NULL    121000002
			//    24  هئیت مدیره  NULL    121000082
			//    30  فروش گازهای صنعتی NULL    121000090
			//    31  فروش کمپرسورهای فرآیندی NULL    121000091
			//    32  فروش کمپرسورهای صنعتی NULL    121000092
			//    33  فروش تجهیزات پزشکی NULL    121000093
			//    140 فروش کمپرسورهای مهندسی NULL    121000133
			//    142 فروش توربو ماشین NULL    121000135
			//    225 فروش صنعتی  NULL    121000149

			switch (organizationUnitId)
			{
				case 1:
				case 140:
					return "EC";
				case 24:
					return "EN";
				case 30:
					return "IG";
				case 31:
					return "EN";
				case 32:
					return "IC";
				case 33:
					return "ME";
				case 41:
					return "TC";
				case 131:
					return "IG";
				case 142:
					return "TM";
				case 225:
					return "IS";
				case 226:
					return "HGI";
			}


		}

		private string GetProjectCode(Project projectEntity)
		{
			//XX / XX / XX / 00
			//SP , VP / شمارنده اتوماتیک / سال / مخفف واحدهای فروش

			//SP: Special project
			//VP: Vendor project
			//TP: Takvin project

			var latestOrganizationUnit = unitOfWork.Repository<Project>().TableNoTracking
				.Where(p => p.SubjectUnitId == projectEntity.SubjectUnitId)
			    .OrderByDescending(p => p.Id)
			    .FirstOrDefault();

			var latestOrganizationUnitCounterInText = latestOrganizationUnit != null && latestOrganizationUnit.Code.HasValue() ? latestOrganizationUnit.Code.Split('/').Last() : "00";

			var now = DateTime.Now;
			var year = now.GetShamsiYear();

			if (latestOrganizationUnit != null && latestOrganizationUnit.Code.HasValue())
			{
				var latestOrganizationUnitCodeYear = latestOrganizationUnit.Code.Split('/')[2];
				if (latestOrganizationUnitCodeYear != year.ToString())
				{
					latestOrganizationUnitCounterInText = "00";
				}
			}


			var projectType = projectEntity.IsTakvinProject ? "TP" : projectEntity.ProjectIsVendoriType ? "VP" : "SP";
			var counter = $"{(Convert.ToInt32(latestOrganizationUnitCounterInText) + 1):00}";

		SetAgian:

			var projectCode = $"{projectType}/{GetOrganizationUnit(projectEntity.SubjectUnitId)}/{now.GetShamsiYear()}/{counter}";

			var isAlreadyExist = unitOfWork.Repository<Project>().TableNoTracking.
				Any(p => p.Code == projectCode);
			if (!isAlreadyExist)
				return projectCode;


			counter += 1;
			goto SetAgian;


		}


	}
}

using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Gnr;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebApp.Actions.Sale;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مناقصه خدمات پس از فروش", typeof(TenderManagement))]
	public class TenderManagementController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IEntityRepository entityRepository,
		IUserService userService) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TenderManagement model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<TenderManagement>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TenderManagement model, CancellationToken cn)
		{
			ClearNav(model);
			try
			{
				return Ok(await unitOfWork.Repository<TenderManagement>().SaveAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TenderManagement model, CancellationToken cn)
		{
			ClearNav(model);
			try
			{
				return Ok(await unitOfWork.Repository<TenderManagement>().UpdateAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TenderManagement>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<TenderManagement>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var entity = (id != null && id != 0)
				? await unitOfWork.Repository<TenderManagement>().TableNoTracking
					.Include(c => c.Company)
					.FirstOrDefaultAsync(c => c.Id == id, cn)
				: new TenderManagement
				{
					CurrentStatus = TenderStatusEnum.FollowingUp,
					InCartable = true,
					HasPrimitiveConfirm = true,
					HasSecondaryConfirm = false
				};
			entity ??= new TenderManagement();
			await FillCompanyDisplayAsync(entity, cn);
			FillAccessBags(entity);
			return View(@"\Views\Panel\Sale\TenderManagement\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var entity = new TenderManagement
			{
				CurrentStatus = TenderStatusEnum.FollowingUp,
				InCartable = true,
				HasPrimitiveConfirm = true,
				HasSecondaryConfirm = false
			};
			FillAccessBags(entity);
			return View(@"\Views\Panel\Sale\TenderManagement\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\TenderManagement\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			if (await IsApproveQueueProfile(request.profileId, cn))
				ApplyApproveQueueAcl(request);
			else
				ApplyListAcl(request);
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await entityRepository.ExportToExcelProfile(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			if (await IsApproveQueueProfile(request.profileId, cn))
				ApplyApproveQueueAcl(request);
			else
				ApplyListAcl(request);
			return Ok(await entityRepository.FetchDataProfile(request, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("بررسی و تایید مناقصات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ApproveQueue()
			=> RedirectToAction(nameof(List));

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت کارتابل تایید مناقصه", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchApproveQueue(DataTableRequest request, CancellationToken cn)
		{
			ApplyApproveQueueAcl(request);
			return Ok(await entityRepository.FetchDataProfile(request, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال به تایید", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendForApprove(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TenderManagement>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("مناقصه یافت نشد");
			if (entity.InCartable != true) return BadRequest("این مناقصه قبلاً از کارتابل خارج شده است");
			entity.InCartable = false;
			try
			{
				return Ok(await unitOfWork.Repository<TenderManagement>().UpdateAsync(entity, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید اولیه مناقصه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> PrimaryConfirm(long id, CancellationToken cn)
		{
			if (!HasTenderAccess("tendersextraoperations") && !HasTenderAccess("tendersmanagerapprove")
				&& !CurrentUserHasRole(TenderManagementAction.RoleApprover) && !CurrentUserHasRole(TenderManagementAction.RoleManager)
				&& !IsAdministrator)
				return BadRequest("دسترسی تایید اولیه را ندارید");

			var entity = await unitOfWork.Repository<TenderManagement>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("مناقصه یافت نشد");
			entity.HasPrimitiveConfirm = entity.HasPrimitiveConfirm != true;
			entity.InCartable = true;
			entity.SkipCartableEmail = true;
			try
			{
				return Ok(await unitOfWork.Repository<TenderManagement>().UpdateAsync(entity, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید نهایی مناقصه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SecondaryConfirm(long id, CancellationToken cn)
		{
			if (!HasTenderAccess("tendersmanagerapprove") && !CurrentUserHasRole(TenderManagementAction.RoleManager) && !IsAdministrator)
				return BadRequest("دسترسی تایید نهایی را ندارید");

			var entity = await unitOfWork.Repository<TenderManagement>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("مناقصه یافت نشد");

			if (entity.HasSecondaryConfirm == true)
			{
				entity.HasSecondaryConfirm = false;
				entity.HasPrimitiveConfirm = true;
				entity.FinalResult = null;
				entity.FailureReasonText = null;
				entity.InCartable = true;
			}
			else if (entity.HasPrimitiveConfirm == true && entity.HasSecondaryConfirm != true)
			{
				entity.HasSecondaryConfirm = true;
				entity.InCartable = false;
			}
			else
				return BadRequest("تایید اولیه انجام نشده است");

			entity.SkipCartableEmail = true;
			TenderManagement saved;
			try
			{
				saved = await unitOfWork.Repository<TenderManagement>().UpdateAsync(entity, cn, true);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}

			if (entity.HasSecondaryConfirm == true && entity.CreatedById != null)
			{
				var creator = await userService.GetById(entity.CreatedById.Value);
				if (creator?.Id != null)
				{
					await unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Email,
						Title = "اعلان تایید مناقصه",
						Body = "<div style='direction:rtl;text-align:right'>مناقصه ثبت شده تایید شد.<br/>شماره مرجع مناقصه : "
							+ entity.Id + "</div>",
						EntityId = entity.Id,
						OwnerId = creator.Id.Value,
						ViewPath = "/Panel/Sale/TenderManagement/Edit?id=" + entity.Id,
						IsRead = false,
						IsSend = false,
						ToEmails = string.IsNullOrWhiteSpace(creator.Email) ? null : new List<string> { creator.Email }
					}, cn);
				}
			}
			return Ok(saved);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات شرکت هلدینگ", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetCompanyInfo(long companyId, CancellationToken cn)
		{
			var empty = new
			{
				phone = (string?)null,
				address = (string?)null,
				companyIndustryItemId = (long?)null,
				companyIndustryTitle = (string?)null,
				companyIndustryItemTitle = (string?)null
			};
			var party = await unitOfWork.Repository<Party>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == companyId, cn);
			if (party == null)
				return Ok(empty);

			var industryItemId = await unitOfWork.Repository<TenderManagement>().TableNoTracking
				.Where(t => t.CompanyId == companyId && t.CompanyIndustryItemId != null)
				.OrderByDescending(t => t.Id)
				.Select(t => t.CompanyIndustryItemId)
				.FirstOrDefaultAsync(cn);

			string? itemTitle = null;
			string? headerTitle = null;
			if (industryItemId != null)
			{
				var item = await unitOfWork.Repository<PartyIndustryItem>().TableNoTracking
					.Include(i => i.PartyIndustry)
					.FirstOrDefaultAsync(i => i.Id == industryItemId, cn);
				itemTitle = item?.Title;
				headerTitle = item?.PartyIndustry?.Title;
			}

			if (string.IsNullOrWhiteSpace(headerTitle))
			{
				var customer = await unitOfWork.Repository<Customer>().TableNoTracking
					.FirstOrDefaultAsync(c => c.PartyId == companyId, cn);
				if (customer?.IndustryId != null)
				{
					headerTitle = await unitOfWork.Repository<PartyIndustry>().TableNoTracking
						.Where(i => i.Id == customer.IndustryId)
						.Select(i => i.Title)
						.FirstOrDefaultAsync(cn);
				}
			}

			return Ok(new
			{
				phone = party.Phone,
				address = party.Address,
				companyIndustryItemId = industryItemId,
				companyIndustryTitle = headerTitle,
				companyIndustryItemTitle = itemTitle
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات ریز صنعت مشتری", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetIndustryItemInfo(long id, CancellationToken cn)
		{
			var item = await unitOfWork.Repository<PartyIndustryItem>().TableNoTracking
				.Include(i => i.PartyIndustry)
				.FirstOrDefaultAsync(i => i.Id == id, cn);
			if (item == null)
				return Ok(new { companyIndustryTitle = (string?)null, companyIndustryItemTitle = (string?)null });
			return Ok(new
			{
				companyIndustryTitle = item.PartyIndustry?.Title,
				companyIndustryItemTitle = item.Title
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دسترسی‌های اضافی مناقصات", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult TendersExtraOperations() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("مناقصات هوای فشرده", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult TendersCompressedAir() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("مناقصات نفت و گاز", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult TendersOilGas() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("تایید نهایی مناقصات", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult TendersManagerApprove() => Ok();

		private static void ClearNav(TenderManagement model)
		{
			model.Company = null;
			model.Parent = null;
			model.SaleExpert = null;
			model.InstallCountry = null;
			model.InstallProvince = null;
			model.CompanyIndustryItem = null;
		}

		private async Task FillCompanyDisplayAsync(TenderManagement entity, CancellationToken cn)
		{
			if (entity.CompanyId == null || entity.CompanyId == 0)
				return;

			if (string.IsNullOrWhiteSpace(entity.CompanyPhone) || string.IsNullOrWhiteSpace(entity.CompanyAddress))
			{
				var party = entity.Company ?? await unitOfWork.Repository<Party>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == entity.CompanyId, cn);
				if (party != null)
				{
					if (string.IsNullOrWhiteSpace(entity.CompanyPhone))
						entity.CompanyPhone = party.Phone;
					if (string.IsNullOrWhiteSpace(entity.CompanyAddress))
						entity.CompanyAddress = party.Address;
				}
			}

			if (entity.CompanyIndustryItemId == null || entity.CompanyIndustryItemId == 0)
				return;

			var item = await unitOfWork.Repository<PartyIndustryItem>().TableNoTracking
				.Include(i => i.PartyIndustry)
				.FirstOrDefaultAsync(i => i.Id == entity.CompanyIndustryItemId, cn);
			if (item == null)
				return;
			entity.CompanyIndustryItemTitle = item.Title;
			entity.CompanyIndustryTitle = item.PartyIndustry?.Title;
		}

		private void FillAccessBags(TenderManagement entity)
		{
			var isManager = IsAdministrator || HasTenderAccess("tendersmanagerapprove")
				|| CurrentUserHasRole(TenderManagementAction.RoleManager);
			ViewBag.IsTenderManager = isManager;
			ViewBag.ShowCompressor = entity.Organization == TenderOrganizationEnum.CompressedAir
				|| HasTenderAccess("tenderscompressedair")
				|| CurrentUserHasRole(TenderManagementAction.RoleCompressedAir);
			ViewBag.CanSendForApprove = entity.Id != null && entity.Id != 0 && entity.InCartable == true;
			ViewBag.FieldsLocked = entity.Id != null && entity.InCartable != true && !isManager;
		}

		private bool HasTenderAccess(string action)
		{
			if (IsAdministrator) return true;
			var needle = "/panel/sale/tendermanagement/" + action;
			return sdk.CurrentUser?.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals(needle, StringComparison.OrdinalIgnoreCase)) == true;
		}

		private async Task<bool> IsApproveQueueProfile(long? profileId, CancellationToken cn)
		{
			if (profileId is null or 0)
				return false;
			var name = await unitOfWork.Repository<SavedQuery>().TableNoTracking
				.Where(x => x.Id == profileId)
				.Select(x => x.Name)
				.FirstOrDefaultAsync(cn);
			return string.Equals(name, "AfterSales_TenderManagement_ApproveQueue", StringComparison.OrdinalIgnoreCase);
		}

		private static void ApplyApproveQueueAcl(DataTableRequest request)
		{
			request.searchBuilder ??= new DataTableSearchBuilder { logic = "AND", criteria = new List<DataTableCriterion>() };
			request.searchBuilder.criteria ??= new List<DataTableCriterion>();
			request.searchBuilder.logic = "AND";

			request.searchBuilder.criteria.Add(new DataTableCriterion
			{
				name = "t1_ParentId",
				origData = "ParentId",
				condition = "null",
				type = "num",
				value = new List<string>()
			});
			request.searchBuilder.criteria.Add(new DataTableCriterion
			{
				name = "t1_InCartable",
				origData = "InCartable",
				condition = "=",
				type = "num",
				value = ["0"]
			});
			request.searchBuilder.criteria.Add(new DataTableCriterion
			{
				name = "t1_HasSecondaryConfirm",
				origData = "HasSecondaryConfirm",
				condition = "!=",
				type = "num",
				value = ["1"]
			});
		}

		private void ApplyListAcl(DataTableRequest request)
		{
			request.searchBuilder ??= new DataTableSearchBuilder { logic = "AND", criteria = new List<DataTableCriterion>() };
			request.searchBuilder.criteria ??= new List<DataTableCriterion>();
			request.searchBuilder.logic = "AND";

			request.searchBuilder.criteria.Add(new DataTableCriterion
			{
				name = "t1_ParentId",
				origData = "ParentId",
				condition = "null",
				type = "num",
				value = new List<string>()
			});

			if (IsAdministrator
				|| HasTenderAccess("tendersextraoperations")
				|| CurrentUserHasRole(TenderManagementAction.RoleShowAll)
				|| CurrentUserHasRole(TenderManagementAction.RoleManager))
				return;

			if (HasTenderAccess("tenderscompressedair") || CurrentUserHasRole(TenderManagementAction.RoleCompressedAir))
			{
				request.searchBuilder.criteria.Add(Eq("t1_Organization", "Organization", ((int)TenderOrganizationEnum.CompressedAir).ToString()));
				return;
			}
			if (HasTenderAccess("tendersoilgas") || CurrentUserHasRole(TenderManagementAction.RoleOilGas))
			{
				request.searchBuilder.criteria.Add(Eq("t1_Organization", "Organization", ((int)TenderOrganizationEnum.OilGasPetrochemical).ToString()));
				return;
			}

			var uid = (CurrentUserId ?? 0).ToString();
			request.searchBuilder.criteria.Add(new DataTableCriterion
			{
				logic = "OR",
				criteria =
				[
					Eq("t1_CreatedById", "CreatedById", uid),
					Eq("t1_SaleExpertId", "SaleExpertId", uid)
				]
			});
		}

		private static DataTableCriterion Eq(string name, string orig, string value)
			=> new()
			{
				name = name,
				origData = orig,
				condition = "=",
				type = "num",
				value = [value]
			};
	}
}

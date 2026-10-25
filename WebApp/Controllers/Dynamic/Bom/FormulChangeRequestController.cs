using Common;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bom;
using Entities.App.Bom.Enums;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.ProductionOrderServices;
using System.Text;
using WebApp.Framework;
using WebFramework.Api;
using WebFramework.Filtters;
using WebFramework.Page;


namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Bom/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست ویرایش اقلام فرمول", typeof(FormulChangeRequest))]
	public class FormulChangeRequestController(IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment,
		ApplicationDbContext _context
		) : BaseController
	{
		/// <summary>معادل HTS SendChangePartNotificationEmail — گیرندگان «ایجاد/تغییر در اقلام»</summary>
		private const string FormulChangeAppliedGroup = "Bom.FormulChange.Applied";
		/// <summary>گیرندگان اضافه وقتی قطعه در BOM قلم سفارش ساخت باز هم هست</summary>
		private const string FormulChangeAppliedExtraGroup = "Bom.FormulChange.AppliedExtra";
		/// <summary>CC ثابت اعلان اعمال تغییر BOM محصول</summary>
		private const string FormulChangeAppliedCcGroup = "Bom.FormulChange.AppliedCc";
		/// <summary>معادل HTS SendEmailNotificationForEngineeringRequests — To</summary>
		private const string FormulChangeCartableGroup = "Bom.FormulChange.Cartable";
		/// <summary>معادل HTS SendEmailNotificationForEngineeringRequests — CC صنایع</summary>
		private const string FormulChangeCartableCcGroup = "Bom.FormulChange.CartableCc";
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{


			if (formulChangeRequest.Id == null || formulChangeRequest.Id == 0)
			{
				return await Add(formulChangeRequest, cn);
			}
			var exist = await unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.AnyAsync(c => c.Id == formulChangeRequest.Id);
			if (exist)
			{
				return await Update(formulChangeRequest, cn);
			}
			return await Add(formulChangeRequest, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			// درخواست جدید همیشه از «ثبت اولیه» شروع می‌شود؛ وضعیت ارسالی کلاینت نادیده گرفته می‌شود
			formulChangeRequest.Status = FormulChangeRequestStatusEnum.InitialRegistration;
			var entity = await unitOfWork.Repository<FormulChangeRequest>().SaveAsync(formulChangeRequest, cn, true);
			await SaveComment(entity);
			await SendEngineeringCartableEmailAsync(entity, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(FormulChangeRequest formulChangeRequest, CancellationToken cn)
		{
			#region کنترل دسترسی و مجاز بودن تغییر وضعیت (معادل PermissionType های 80/81/82/84 در HTS)

			var stored = await unitOfWork.Repository<FormulChangeRequest>().TableNoTracking
				.Where(c => c.Id == formulChangeRequest.Id)
				.Select(c => new { c.Status })
				.FirstOrDefaultAsync(cn);
			if (stored == null)
				return BadRequest("درخواست مورد نظر یافت نشد");

			var oldStatus = stored.Status;
			var newStatus = formulChangeRequest.Status;
			Func<string, bool> hasRole = CurrentUserHasRole;

			if (newStatus != oldStatus
				&& (!newStatus.HasValue || !FormulChangeRequestWorkflow.GetNextStatuses(oldStatus).Contains(newStatus.Value)))
				return BadRequest($"تغییر وضعیت از «{oldStatus?.ToDisplay()}» به «{newStatus?.ToDisplay()}» مجاز نیست");

			if (!FormulChangeRequestWorkflow.HasStageRole(IsAdministrator, hasRole, oldStatus))
				return AccessDenied("شما دسترسی لازم برای اقدام روی این مرحله از درخواست را ندارید");

			#endregion

			var entity = await unitOfWork.Repository<FormulChangeRequest>().UpdateAsync(formulChangeRequest, cn, true);
			await SaveComment(entity);
			if (entity.Status == FormulChangeRequestStatusEnum.EndOperation)
				 await ChangeBom(entity, cn);
			else if (entity.Status.HasValue && entity.Status != oldStatus)
				await SendStatusChangeEmailAsync(entity, oldStatus, entity.Status.Value, cn);
			return Ok(entity);
		}

		/// <summary>پاسخ استاندارد عدم دسترسی: { isSuccess:false, statusCode:UnAuthorized, message } با HTTP 403.</summary>
		private IActionResult AccessDenied(string message)
			=> new JsonResult(new ApiResult(false, ApiResultStatusCode.UnAuthorized, message))
			{
				StatusCode = StatusCodes.Status403Forbidden
			};

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<FormulChangeRequest>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<FormulChangeRequest>().TableNoTracking
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.Include(c => c.Part)
					.Include(c => c.ReplacementPart)
					.Include(c => c.Comments)
					.AsSplitQuery()
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", entity);
			}
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new FormulChangeRequest();
			return View(@"\Views\Panel\Bom\FormulChangeRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Bom\FormulChangeRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<FormulChangeRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<FormulChangeRequest>().FetchDataAsync(request, cn));
		}

		public async Task SaveComment(FormulChangeRequest entity, CancellationToken cn = default)
		{

			if (entity.AttachmentId != null)
			{
				var fileAttachment = unitOfWork.Repository<FileEntity>()
				.Table
				.First(c => c.Id == entity.AttachmentId);

				if (fileAttachment != null)
				{
					fileAttachment.EntityPropName = "Attachment";
					fileAttachment.EntityType = typeof(FormulChangeRequestComment).FullName;
				}
			}

			var newComment = new FormulChangeRequestComment()
			{
				AttachmentId = entity.AttachmentId,
				Description = entity.Description,
				Status = entity.Status,
				FormulChangeRequestId = entity.Id.Value
			};


			await unitOfWork.Repository<FormulChangeRequestComment>().
				AddAsync(newComment, cn, false);
		}

		public async Task ChangeBom(FormulChangeRequest entity, CancellationToken cn = default)
		{
			var hasChangedGroups = entity.ChangedProductGroupIds.HasValue();
			var hasChangedProducts = entity.ChangedProductFormulIds.HasValue();

			if (!hasChangedGroups && !hasChangedProducts)
				throw new Exception("هیچ گروه محصول یا محصولی برای ویرایش انتخاب نشده است.");

			var productFormulIdSet = new HashSet<long>();

			if (hasChangedGroups)
			{
				var productGroupIds = entity.ChangedProductGroupIds
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => x.ToLong())
					.Where(id => id > 0)
					.Distinct()
					.ToList();

				if (productGroupIds.Count > 0)
				{
					var fromGroups = unitOfWork.Repository<ProductFormul>().TableNoTracking
						.Where(p => p.ProductNameGroupId != null && productGroupIds.Contains(p.ProductNameGroupId.Value))
						.Select(p => p.Id!.Value)
						.ToList();

					foreach (var id in fromGroups)
						productFormulIdSet.Add(id);
				}
			}

			if (hasChangedProducts)
			{
				var selectedProductFormulIds = entity.ChangedProductFormulIds
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(x => x.ToLong())
					.Where(id => id > 0);

				foreach (var id in selectedProductFormulIds)
					productFormulIdSet.Add(id);
			}

			if (productFormulIdSet.Count == 0)
				return;

			var productFormulIds = productFormulIdSet.ToList();

			var itemRepo = unitOfWork.Repository<ProductFormulItem>();
			var usingRate = entity.ChangedUsingRate.HasValue
				? (decimal?)entity.ChangedUsingRate.Value
				: null;

			if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Add)
			{
				if (!entity.PartId.HasValue)
					throw new Exception("قطعه برای افزودن مشخص نشده است.");

				foreach (var productFormulId in productFormulIds)
				{
					var existing = itemRepo.Table
						.FirstOrDefault(p => p.ProductFormulId == productFormulId && p.PartId == entity.PartId.Value);

					if (existing != null)
					{
						if (usingRate.HasValue)
							existing.UsingRate = usingRate;
					}
					else
					{
						await itemRepo.AddAsync(new ProductFormulItem
						{
							ProductFormulId = productFormulId,
							PartId = entity.PartId.Value,
							UsingRate = usingRate ?? 1
						}, cn, false);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
			}
			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Replacement)
			{
				if (!entity.PartId.HasValue || !entity.ReplacementPartId.HasValue)
					throw new Exception("قطعه و قطعه جایگزین برای جایگزینی مشخص نشده‌اند.");

				var items = itemRepo.Table
					.Where(p => productFormulIds.Contains(p.ProductFormulId) && p.PartId == entity.PartId.Value)
					.ToList();

				if (items.Count == 0)
					return;

				foreach (var item in items)
				{
					var duplicate = itemRepo.Table
						.FirstOrDefault(p =>
							p.ProductFormulId == item.ProductFormulId
							&& p.PartId == entity.ReplacementPartId.Value
							&& p.Id != item.Id);

					if (duplicate != null)
					{
						duplicate.UsingRate = (duplicate.UsingRate ?? 0) + (usingRate ?? item.UsingRate ?? 0);
						await itemRepo.DeleteAsync(item, cn, false);
					}
					else
					{
						item.PartId = entity.ReplacementPartId.Value;
						item.Part = null;
						if (usingRate.HasValue && usingRate.Value != 0)
							item.UsingRate = usingRate;
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
			}
			else if (entity.OperationType == FormulChangeRequestOperationTypeEnum.Deleted)
			{
				if (!entity.PartId.HasValue)
					throw new Exception("قطعه برای حذف مشخص نشده است.");

				await itemRepo.Table
					.Where(p => productFormulIds.Contains(p.ProductFormulId) && p.PartId == entity.PartId.Value)
					.ExecuteDeleteAsync(cn);
			}

			#region ارسال ایمیل اطلاع رسانی نهایی

			await SendChangePartNotificationEmailAsync(entity, cn);

			#endregion

			await UpdateDataSheets(entity.PartId.Value, entity.ReplacementPartId, cn);
		}

		private async Task UpdateDataSheets(long partId , long? replacePartId,CancellationToken cn)
		{

			var partIds = new List<long> { partId };

			if (replacePartId.HasValue)
				partIds.Add(replacePartId.Value);

			var productIds = _context.vw_ProductItems
			    .Where(p => partIds.Contains(p.PartItemId))
			    .Select(p => p.PartId)
			    .Distinct()
			    .ToArray();

			if (!productIds.Any())
				return;

			var partExtraInfos = unitOfWork.Repository<PartExtraInfo>()
							.TableNoTracking. Where(p => productIds.Contains(p.ProductId))
							.GroupBy(p => p.ProductId)
							.Select(p => p.OrderByDescending(o => o.Id).FirstOrDefault())
							.ToList();

			if (!partExtraInfos.Any())
				return;

			 foreach(var partExtraInfo in partExtraInfos)
			{
				partExtraInfo.Id = null;
				partExtraInfo.Product = null;
				partExtraInfo.CreatedById = null;
				partExtraInfo.CreatedByName = null;
				partExtraInfo.CreatedOnMiladiDateTime = null;
				partExtraInfo.CreatedOnShamsiDateTime = null;
				partExtraInfo.ModifiedById = null;
				partExtraInfo.ModifiedByName = null;
				partExtraInfo.Revision = null;

				await new PartExtraInfoController(unitOfWork, _context, _webHostEnvironment)
					.Add(partExtraInfo, cn);
			}
		}

		/// <summary>
		/// معادل HTS SendChangePartNotificationEmail: پس از افزودن/ویرایش/حذف قلم BOM محصول، ایمیل «ایجاد/تغییر در اقلام».
		/// </summary>
		private async Task SendChangePartNotificationEmailAsync(FormulChangeRequest entity, CancellationToken cn)
		{
			try
			{
				var content = GetNotificationEmailMessage(entity, out var partCode, out var hasProductionOrderItemRecord, true);
				if (!content.HasValue())
					return;

				var to = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeAppliedGroup, cn);
				if (hasProductionOrderItemRecord)
					to.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeAppliedExtraGroup, cn));

				var cc = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeAppliedCcGroup, cn);
				cc.AddRange(await GetPartFamilyEmailsAsync(partCode, cn));

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(
					unitOfWork,
					"ایجاد/تغییر در اقلام",
					content,
					to,
					cc,
					entity.Id,
					CurrentUserId ?? 0,
					entity.Id is > 0 ? $"Panel/Bom/FormulChangeRequest/Edit/{entity.Id}" : null,
					cn);
			}
			catch
			{
				// شکست اعلان نباید اعمال BOM را متوقف کند
			}
		}

		/// <summary>
		/// معادل HTS SendEmailNotificationForEngineeringRequests: با ثبت درخواست، مهندسی (To) و صنایع (CC) کارتابل را می‌بینند.
		/// </summary>
		private async Task SendEngineeringCartableEmailAsync(FormulChangeRequest entity, CancellationToken cn)
		{
			try
			{
				var content = GetNotificationEmailMessage(entity, out _, out _, false);
				if (!content.HasValue())
					return;

				var to = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeCartableGroup, cn);
				var cc = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeCartableCcGroup, cn);

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(
					unitOfWork,
					"درخواست مشاهده کارتابل",
					content,
					to,
					cc,
					entity.Id,
					CurrentUserId ?? 0,
					entity.Id is > 0 ? $"Panel/Bom/FormulChangeRequest/Edit/{entity.Id}" : null,
					cn);
			}
			catch
			{
				// شکست اعلان نباید ثبت درخواست را متوقف کند
			}
		}

		/// <summary>
		/// معادل HTS SendCommentNotificationEmail: پس از هر تغییر وضعیت، کاربران نقشِ مرحلهٔ بعد (برنامه‌ریزی / QC / مهندسی / اعمال نهایی)
		/// ایمیل «درخواست مشاهده کارتابل» می‌گیرند تا در سیستم اقدام کنند. در عدم تایید برنامه‌ریزی گیرنده ایجادکنندهٔ درخواست است.
		/// ایجادکننده در CC همهٔ این ایمیل‌ها است و شخص اقدام‌کننده از گیرندگان حذف می‌شود.
		/// </summary>
		private async Task SendStatusChangeEmailAsync(FormulChangeRequest entity, FormulChangeRequestStatusEnum? from, FormulChangeRequestStatusEnum to, CancellationToken cn)
		{
			try
			{
				// ارسال مجدد پس از «عدم تایید برنامه ریزی» مثل ثبت درخواست تازه به کارتابل مهندسی می‌رود
				if (to == FormulChangeRequestStatusEnum.InitialRegistration)
				{
					await SendEngineeringCartableEmailAsync(entity, cn);
					return;
				}

				var content = GetNotificationEmailMessage(entity, out _, out _, false, from, to);
				if (!content.HasValue())
					return;

				var creatorId = await unitOfWork.Repository<FormulChangeRequest>().TableNoTracking
					.Where(c => c.Id == entity.Id)
					.Select(c => c.CreatedById)
					.FirstOrDefaultAsync(cn);
				string? creatorEmail = null;
				if (creatorId.HasValue)
				{
					creatorEmail = await _context.Users.AsNoTracking()
						.Where(u => u.Id == creatorId)
						.Select(u => u.Email)
						.FirstOrDefaultAsync(cn);
				}

				var toList = new List<string>();
				var notifyRole = FormulChangeRequestWorkflow.GetNotifyRole(to);
				if (notifyRole != null)
					toList.AddRange(await ProductionOrderItemNotificationHelper.GetRoleEmailsAsync(_context, notifyRole, cn));
				else if (creatorEmail.HasValue())
					toList.Add(creatorEmail!);

				var cc = new List<string>();
				if (creatorEmail.HasValue())
					cc.Add(creatorEmail!);
				cc.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, FormulChangeCartableCcGroup, cn));

				// شخص اقدام‌کننده نیازی به ایمیل ندارد
				var actorEmail = CurrentUserEmail;
				if (actorEmail.HasValue())
				{
					toList.RemoveAll(e => string.Equals(e, actorEmail, StringComparison.OrdinalIgnoreCase));
					cc.RemoveAll(e => string.Equals(e, actorEmail, StringComparison.OrdinalIgnoreCase));
				}

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(
					unitOfWork,
					"درخواست مشاهده کارتابل",
					content,
					toList,
					cc,
					entity.Id,
					CurrentUserId ?? 0,
					entity.Id is > 0 ? $"Panel/Bom/FormulChangeRequest/Edit/{entity.Id}" : null,
					cn);
			}
			catch
			{
				// شکست اعلان نباید تغییر وضعیت را متوقف کند
			}
		}

		private string BuildAbsoluteRequestUrl(long? id)
		{
			var config = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
			var baseUrl = config["WebAppBaseUrl"];
			if (!baseUrl.HasValue())
				baseUrl = "https://portal.havayar.com";
			return $"{baseUrl!.TrimEnd('/')}/Panel/Bom/FormulChangeRequest/Edit?id={id}";
		}

		/// <summary>معادل HTS PartService.GetEmailAddressByPartCode — CC بر اساس پیشوند کد محصول مادر.</summary>
		private async Task<List<string>> GetPartFamilyEmailsAsync(string? partCode, CancellationToken cn)
		{
			if (!partCode.HasValue())
				return new List<string>();

			var productCodes = await (
				from item in _context.vw_ProductItems.AsNoTracking()
				join formul in _context.Set<ProductFormul>().AsNoTracking() on item.ProductFormulId equals formul.Id
				join product in _context.Set<Part>().AsNoTracking() on formul.ProductId equals product.Id
				where item.PardCode == partCode && product.Code != null
				select product.Code!
			).Distinct().ToListAsync(cn);

			var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var code in productCodes)
			{
				if (code.Length < 6)
					continue;
				if (PartFamilyUsernames.TryGetValue(code[..6], out var family))
				{
					foreach (var username in family)
						usernames.Add(username);
				}
			}

			if (usernames.Count == 0)
				return new List<string>();

			var nameList = usernames.ToList();
			return await _context.Users.AsNoTracking()
				.Where(u => u.IsActive == IsActiveEnum.Active && u.Username != null && nameList.Contains(u.Username))
				.Select(u => u.Email != null && u.Email != "" ? u.Email : u.Username + "@havayar.com")
				.ToListAsync(cn);
		}

		private static readonly Dictionary<string, string[]> PartFamilyUsernames = new(StringComparer.OrdinalIgnoreCase)
		{
			["180110"] = ["kadkhodaei.v", "Mirlohi.a", "vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["180117"] = ["m.nikpour", "marashi.m", "azami.s", "Asgari.v", "norouzi.m", "Sazvar.m"],
			["180143"] = ["m.nikpour", "marashi.m", "azami.s", "Asgari.v", "norouzi.m", "Sazvar.m"],
			["180141"] = ["mahani.b", "golestaneh.m", "Ahmadpour.s", "ebrahimi.f"],
			["180190"] = ["javadian.p", "Samii.m", "Saeedi.m", "Bamimohammadi.gh", "kadkhodaei.v"],
			["180195"] = ["javadian.p", "Samii.m", "Saeedi.m", "Bamimohammadi.gh", "kadkhodaei.v"],
			["180112"] = ["javadian.p", "Samii.m", "Saeedi.m", "Bamimohammadi.gh", "kadkhodaei.v"],
			["180130"] = ["javadian.p", "Samii.m", "Saeedi.m", "Bamimohammadi.gh", "kadkhodaei.v"],
			["180214"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180215"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180213"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180223"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180224"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180225"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180610"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180611"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180612"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180613"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180614"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180711"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180710"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180812"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["180910"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["181012"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["181110"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["181510"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["181610"] = ["Ghorbani.z", "Neysari.n", "Kondori.v", "Erfani.m", "Nikzaban.a", "kadkhodaei.v"],
			["182110"] = ["bagheri.m", "noroozi.s", "sharifi.mo", "Rostampoor.a"],
			["400810"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["180410"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["180217"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["180140"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["180310"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
			["181115"] = ["vatanchi.a", "kalhor.m", "khalili.s", "masoudi.l", "piri.m", "masoumi.s"],
		};

		private string GetNotificationEmailMessage(FormulChangeRequest entity, out string partCode, out bool hasProductionOrderItemRecord, bool changedPart = false,
			FormulChangeRequestStatusEnum? statusFrom = null, FormulChangeRequestStatusEnum? statusTo = null)
		{
			var isStatusChange = statusTo.HasValue;
			partCode = null;
			hasProductionOrderItemRecord = false;

			if (entity == null)
				return null;

			var item = unitOfWork.Repository<FormulChangeRequest>()
							.TableNoTracking
			    .Where(p => p.Id == entity.Id)
			    .Include(p => p.Part)
			    .Include(p => p.ReplacementPart)
			    .AsSplitQuery()
			    .FirstOrDefault();

			if (item == null)
				return null;


			var content = new StringBuilder();

			content.AppendLine("<div style='text-align:center;direction:rtl'>");
			content.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			content.AppendLine("<tr style='width: 7.0in; background: #000aa0'>");
			content.AppendLine("<td colspan='2'>");
			content.AppendLine("<div  style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>");
			content.AppendLine("گروه صنعتی هوایار");
			content.AppendLine("</div>");
			content.AppendLine("</td>");
			content.AppendLine("</tr>");

			content.AppendLine("<tr>");
			content.AppendLine("<td colspan='2'>");
			content.AppendLine("<div  style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			content.AppendLine("با سلام و احترام <br />");

			if (isStatusChange)
			{
				content.Append($"وضعیت درخواست ویرایش اقلام فرمول شماره <strong>'{item.Id}'</strong> ");
				content.Append($"از <strong>'{statusFrom?.ToDisplay()}'</strong> به <strong>'{statusTo?.ToDisplay()}'</strong> تغییر کرد.");
				content.Append("<br /><br />");
			}

			var hasAlternativePart = item.ReplacementPartId.HasValue;

			partCode = hasAlternativePart ? item.ReplacementPart?.Code : item.Part?.Code;

			if (hasAlternativePart)
			{
				content.Append(changedPart ? "تغییر قطعه " : "درخواست تغییر قطعه ");

				content.Append($"<strong>'{item.Part?.Code} - {item.Part?.Name}'</strong> ");
				content.Append("<br/>");

				content.Append("با قطعه ");
				content.Append($"<strong>'{item.ReplacementPart?.Code} - {item.ReplacementPart?.Name}'</strong> ");
			}
			else
			{
				if (item.OperationType == FormulChangeRequestOperationTypeEnum.Deleted)
					content.Append(changedPart ? "حذف قطعه " : "درخواست حذف قطعه ");
				else
					content.Append(changedPart ? "ایجاد قطعه " : "درخواست ایجاد قطعه ");

				content.Append($"<strong>'{item.Part?.Code} - {item.Part?.Name}'</strong> ");
				content.Append("<br/>");
			}

			var groupNames = changedPart ? item.ChangedProductGroupNames : item.ProductGroupNames;
			var productNames = changedPart ? item.ChangedProductFormulNames : item.ProductFormulNames;
			var formulaNames = changedPart ? item.ChangedFormulNames : null;

			if (groupNames.HasValue())
			{
				content.Append("در گروه محصول(های) ");
				content.Append("<ul>");
				foreach (var groupItem in groupNames.Split(","))
				{
					content.Append("<li>");
					content.Append($"<strong>{groupItem}</strong>");
					content.Append("</li>");
				}
				content.Append("</ul>");
			}

			if (productNames.HasValue())
			{
				content.Append("در محصول(های) ");
				content.Append("<ul>");
				foreach (var productItem in productNames.Split(","))
				{
					content.Append("<li>");
					content.Append($"<strong>{productItem}</strong>");
					content.Append("</li>");
				}
				content.Append("</ul>");
			}
			else if (!groupNames.HasValue() && formulaNames.HasValue())
			{
				content.Append("در فرمول(های) ");
				content.Append("<ul>");
				foreach (var formulaItem in formulaNames.Split(","))
				{
					content.Append("<li>");
					content.Append($"<strong>{formulaItem}</strong>");
					content.Append("</li>");
				}
				content.Append("</ul>");
			}
				content.Append("<br />");

				content.Append($"بواسطه <strong>'{CurrentUserFullNameFn}'</strong> ");
				content.Append($"در تاریخ <strong>'{DateTime.Now.ToShamsiDateTime()}'</strong> ");
				if (isStatusChange)
					content.Append("ثبت گردید ");
				else
					content.Append(changedPart ? "انجام گردید " : "ایجاد گردید ");

			 

			#region  اقلام سفارش ساخت جهت اعلان به مهندسی BOM بررسی قلم تغییر یافته در  

			var partId = item.ReplacementPartId ?? item.PartId;
				var productionOrderItemBomList = unitOfWork.Repository<ProductionOrderItemBom>()
						.TableNoTracking
						.Where(p => !p.ProductionOrderItem.IsDeleted &&
								p.ProductionOrderItem.IsLatestVersion &&
								p.ProductionOrderItem.Status == ProductionOrderItemStatusEnum.NotCompleted && // وضعیت 0
								!p.ProductionOrderItem.PreparationMiladiDate.HasValue && // وضعیت 0
								p.ProductionOrderItem.Serial != null &&
								p.ProductionOrderItem.Serial.Length > 3 &&
								p.ProductionOrderItem.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal &&
								p.ProductionOrderItem.ProductionOrderId != 10529 && // 10529 -> ProductionOrderNumber == 0 
								p.PartId == partId)
											 .Select(p => new
											 {
												 p.ProductionOrderItem.Serial,
												 p.ProductionOrderItem.Part.Code,
												 p.ProductionOrderItem.Part.Name,
												 p.ProductionOrderItem.ProductionOrder.Number,
												 p.ProductionOrderItem.ProductionOrder.Contract.Customer.Party.FullName,
											 })
											 .GroupBy(p => new { p.Code, p.Number })
											 .Select(s => s.FirstOrDefault())
											 .ToList();

				if (productionOrderItemBomList.Any())
				{
					hasProductionOrderItemRecord = true;

					content.Append("<br />");
					content.Append("<br />");
					content.Append("<strong style='color:red;text-align:center'>***توجه***</strong>");
					content.Append("<br />");
					content.Append("<br />");

					content.Append("همچنین کد تغییر کرده در BOM اقلام سفارش ساخت با جزییات ذیل نیز وجود دارد که بایستی توسط کاربر مهندسی مربوطه، تغییرات لازم در این قسمت نیز لحاظ گردد");
					content.Append("<br />");

					foreach (var productionOrderItemBom in productionOrderItemBomList)
					{
						content.Append($"ش سفارش ساخت : <strong>'{productionOrderItemBom.Number}'</strong> <br />");
						content.Append($"مشتری : <strong>'{productionOrderItemBom.FullName}'</strong> <br />");
						content.Append($"کد کالا : <strong>'{productionOrderItemBom.Code}'</strong> <br />");
						content.Append($"عنوان کالا : <strong>'{productionOrderItemBom.Name}'</strong> <br />");
						content.Append($"سریال : <strong>'{productionOrderItemBom.Serial}'</strong> <br />");
						content.Append("<br />");
					}
				}

				#endregion

			if (isStatusChange)
			{
				if (entity.Description.HasValue())
				{
					content.Append("<br /><br />");
					content.Append($"توضیحات : <strong>{System.Net.WebUtility.HtmlEncode(entity.Description)}</strong>");
				}

				content.Append("<br /><br />");
				content.Append("لطفاً جهت بررسی و اقدام، درخواست را در سیستم مشاهده فرمایید.");
				content.Append("<br />");
				content.Append($"<a href='{System.Net.WebUtility.HtmlEncode(BuildAbsoluteRequestUrl(item.Id))}'>مشاهده درخواست</a>");
			}

			content.Append("</div>");
			content.Append("</td>");
			content.Append("</tr>");
			content.Append("</table>");
			content.Append("</div>");

			return content.ToString();

		}
	}
}

using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System.Text;
using System.Text.RegularExpressions;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Inv
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("دسته بندی کد کالا", typeof(PartCodingCategory))]
	public class PartCodingCategoryController(
		IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment webHostEnvironment,
		IUserService userService) : BaseController
	{
		// TODO: نام نقش گیرنده درخواست کد کالا را در سیستم تعریف کنید
		private const string RequestRecipientRoleName = "InvPartCodingRequestRecipient";

		private const int MinPartCodePrefixLength = 6;

		#region Page

		[HttpGet("[action]")]
		[ActionDisplayName("کدینگ کالا", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult Index()
		{
			ViewBag.HasAddPermission = IsAuthenticated;
			return View(@"\Views\Panel\Inv\PartCodingCategory\Index.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست درخواست ها", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListRequest()
		{
			ViewBag.HasAddPermission = IsAuthenticated;
			return View(@"\Views\Panel\Inv\PartCodingCategory\ListRequest.cshtml");
		}

		

		#endregion

		#region Tree

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت درخت دسته بندی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetTreeData(CancellationToken cn)
		{
			var categories = await unitOfWork.Repository<PartCodingCategory>()
				.TableNoTracking
				.OrderBy(c => c.Code)
				.Select(c => new CategoryFlatDto
				{
					Id = c.Id!.Value,
					ParentId = c.ParentId,
					Code = c.Code,
					Title = c.Title,
					Atributies = c.Atributies,
					Description = c.Description
				})
				.ToListAsync(cn);

			var nodeIdsWithChildren = categories
				.Where(c => c.ParentId.HasValue)
				.Select(c => c.ParentId!.Value)
				.ToHashSet();

			var nodes = categories.Select(c => new JsTreeNodeDto
			{
				id = c.Id.ToString(),
				parent = c.ParentId.HasValue ? c.ParentId.Value.ToString() : "#",
				text = BuildNodeText(c, nodeIdsWithChildren.Contains(c.Id)),
				data = new JsTreeNodeDataDto
				{
					code = c.Code,
					title = c.Title,
					atributies = c.Atributies ?? string.Empty,
					description = c.Description ?? string.Empty,
					parentId = c.ParentId,
					hasChildren = nodeIdsWithChildren.Contains(c.Id)
				}
			}).ToList();

			return Ok(nodes);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت پیشوند کد", ActionAccessType.Api)]
		public async Task<IActionResult> GetPartCodePrefix(long categoryId, CancellationToken cn)
		{
			var prefix = await BuildPartCodePrefixAsync(categoryId, cn);
			return Ok(new { prefix, canSearch = prefix.Length >= MinPartCodePrefixLength });
		}

		#endregion

		#region Parts Grid

		public class DataTableRequestGridData: DataTableRequest
		{
			public string? partCodePrefix { get; set; }

			public string? searchContent { get; set; }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت کالاهای مرتبط", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchPartsGridData(
			DataTableRequestGridData request,
			CancellationToken cn)
		{
			//if (!request.partCodePrefix.HasValue())
			//{
			//	return Ok(new DataTableResponse
			//	{
			//		Draw = request.draw,
			//		RecordsTotal = 0,
			//		RecordsFiltered = 0,
			//		Data = Array.Empty<object>()
			//	});
			//}

			var partQuery = unitOfWork.Repository<Part>().TableNoTracking
				.Include(p => p.Unit)
				.Where(p => p.Code != null && !p.Code.StartsWith("5"));

			if (request.partCodePrefix.HasValue())
				partQuery = partQuery.Where(p => p.Code!.StartsWith(request.partCodePrefix));

			var parts = await partQuery
				.Select(p => new PartGridRowDto
				{
					Id = p.Id,
					Code = p.Code,
					Name = p.Name,
					ModifiedDateShamsiDateTime = p.ModifiedDateShamsiDateTime,
					Number = p.Number,
					Description = p.Description,
					Type = p.Type,
					UnitTitle = p.Unit != null ? p.Unit.Title : null,
					Foreign = p.Foreign,
					EngineeringRoutine = p.EngineeringRoutine
				})
				.ToListAsync(cn);

			parts = ApplyAdvancedSearchFilter(parts, request.searchContent);

			var recordsTotal = parts.Count;
			var recordsFiltered = parts.Count;

			var orderColumn = request.order?.FirstOrDefault()?.column;
			var orderDir = request.order?.FirstOrDefault()?.dir ?? "asc";
			var columnName = orderColumn != null && request.columns != null && int.TryParse(orderColumn, out var colIndex) && colIndex < request.columns.Count
				? request.columns[colIndex].data
				: "Code";

			parts = ApplyOrdering(parts, columnName, orderDir);

			var pageData = parts
				.Skip(request.start)
				.Take(request.length > 0 ? request.length : 50)
				.Select(p => new
				{
					p.Id,
					p.Code,
					p.Name,
					p.ModifiedDateShamsiDateTime,
					p.Number,
					p.Description,
					TypeTitle = p.Type.HasValue ? p.Type.Value.ToDisplay() : string.Empty,
					p.UnitTitle,
					p.Foreign,
					p.EngineeringRoutine
				})
				.ToList();

			return Ok(new DataTableResponse
			{
				Draw = request.draw,
				RecordsTotal = recordsTotal,
				RecordsFiltered = recordsFiltered,
				Data = pageData
			});
		}

		#endregion

		#region Request

		[HttpPost("[action]")]
		[ActionDisplayName("درخواست کد جدید", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> RequestNewPart(RequestNewPartDto model, CancellationToken cn)
		{
			if (model.PartCodingCategoryId <= 0)
				return BadRequest("دسته بندی انتخاب نشده است.");

			if (!model.RequestPartCodePrefix.HasValue())
				return BadRequest("کد کالا نمی‌تواند خالی باشد.");

			if (!model.Title.HasValue())
				return BadRequest("نام کالا نمی‌تواند خالی باشد.");

			var category = await unitOfWork.Repository<PartCodingCategory>()
				.Table
				.FirstOrDefaultAsync(c => c.Id == model.PartCodingCategoryId, cn);

			if (category == null)
				return BadRequest("دسته بندی یافت نشد.");

			var attributeValidation = ValidateRequiredAttributes(category.Atributies, model.Atributies);
			if (!attributeValidation.IsValid)
				return BadRequest(attributeValidation.ErrorMessage);

			var partRepo = unitOfWork.Repository<Part>().TableNoTracking;
			var latestSimilarPart = await partRepo
				.Where(p => p.Code != null && p.Code.StartsWith(model.RequestPartCodePrefix))
				.OrderByDescending(p => p.Code)
				.FirstOrDefaultAsync(cn);

			var newPartCode = latestSimilarPart?.Code == null
				? model.RequestPartCodePrefix
				: (Convert.ToInt64(latestSimilarPart.Code) + 1).ToString();

			var requestEntity = new PartCodingCategoryRequest
			{
				PartCodingCategoryId = model.PartCodingCategoryId,
				Code = newPartCode,
				Title = model.Title.Trim(),
				Atributies = model.Atributies?.Trim(),
				Description = model.Description?.Trim(),
				AttachmentId = model.AttachmentId  
			};

			 

			var saved = await unitOfWork.Repository<PartCodingCategoryRequest>().SaveAsync(requestEntity, cn, true);

			category.IsRequested = true;
			await unitOfWork.Repository<PartCodingCategory>().UpdateAsync(category, cn, true);

			await SendRequestNotificationAsync(saved, category, model.RequestPartCodePrefix, cn);

			return Ok(new { saved.Id, generatedCode = newPartCode });
		}

		#endregion

		#region Approval API


		public class ApplyCreationPartViewModel
		{
			public long requestId { get; set; }
			public long createdPartId { get; set; }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید و ایجاد کالا", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> ApplyCreationPart(ApplyCreationPartViewModel model, CancellationToken cn)
		{
			var requestRepo = unitOfWork.Repository<PartCodingCategoryRequest>();
			var entity = await requestRepo.Table
				.Include(r => r.PartCodingCategory)
				.FirstOrDefaultAsync(r => r.Id == model.requestId, cn);

			if (entity == null)
				return BadRequest("درخواست یافت نشد.");

			var partExists = await unitOfWork.Repository<Part>().TableNoTracking
				.AnyAsync(p => p.Id == model.createdPartId, cn);

			if (!partExists)
				return BadRequest("کالای ایجاد شده یافت نشد.");

			entity.CreatedPartId = model.createdPartId;
			await requestRepo.UpdateAsync(entity, cn, true);

			await SendApplyCreationPartNotificationAsync(entity, cn);

			return Ok(new { entity.Id, entity.CreatedPartId });
		}

		#endregion

		#region Notifications

		private async Task SendRequestNotificationAsync(
			PartCodingCategoryRequest entity,
			PartCodingCategory category,
			string requestPartCodePrefix,
			CancellationToken cn)
		{
			var recipientEmails = await GetRecipientEmailsByRoleAsync();
			var ccEmails = new List<string>();

			if (CurrentUserEmail.HasValue())
				ccEmails.Add(CurrentUserEmail);

			var body = BuildRequestEmailBody(entity, category, requestPartCodePrefix);
			var ownerId = CurrentUserId ?? 0;

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعلان درخواست ایجاد کد کالا",
				Body = body,
				EntityId = entity.Id,
				OwnerId = ownerId,
				ViewPath = $"/Panel/Inv/PartCodingCategory/Index",
				IsRead = false,
				IsSend = false,
				ToEmails = recipientEmails,
				CcEmails = ccEmails
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task SendApplyCreationPartNotificationAsync(PartCodingCategoryRequest entity, CancellationToken cn)
		{
			var part = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == entity.CreatedPartId, cn);

			var recipientEmails = new List<string>();
			if (entity.CreatedById.HasValue)
			{
				var creator = await userService.GetById(entity.CreatedById.Value);
				if (creator?.Email.HasValue() == true)
					recipientEmails.Add(creator.Email);
			}

			var ccEmails = await GetRecipientEmailsByRoleAsync();
			if (CurrentUserEmail.HasValue())
				ccEmails.Add(CurrentUserEmail);

			var body = BuildApplyCreationPartEmailBody(entity, part, categoryTitle: entity.PartCodingCategory?.Title);

			var ownerId = entity.CreatedById ?? CurrentUserId ?? 0;

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعلان ایجاد کد کالا",
				Body = body,
				EntityId = entity.Id,
				OwnerId = ownerId,
				ViewPath = $"/Panel/Inv/PartCodingCategory/Index",
				IsRead = false,
				IsSend = false,
				ToEmails = recipientEmails,
				CcEmails = ccEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}

		private async Task<List<string>> GetRecipientEmailsByRoleAsync()
		{
			var users = await userService.GetUsersByRoleName(RequestRecipientRoleName);
			if (users == null || users.Length == 0)
				return [];

			return users
				.Where(u => u.Email.HasValue())
				.Select(u => u.Email!.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static string BuildRequestEmailBody(
			PartCodingCategoryRequest entity,
			PartCodingCategory category,
			string requestPartCodePrefix)
		{
			var content = new StringBuilder();
			content.AppendLine("<div style='width:100%;text-align:center;direction:rtl'>");
			content.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='100%'>");
			content.AppendLine("<tr style='background:#000aa0'><td><div style='font-size:14pt;font-family:Zar;color:#fff;text-align:center;direction:rtl'>گروه صنعتی هوایار</div></td></tr>");
			content.AppendLine("<tr style='text-align:right;direction:rtl;font-size:14pt;font-family:Zar'><td>");
			content.AppendLine("باسلام و احترام<br/>");
			content.AppendLine($"درخواست ایجاد کالای جدید با مشخصات ذیل، بواسطه <strong>{entity.CreatedByName ?? "کاربر"}</strong> ");
			content.AppendLine($"در تاریخ <strong>{entity.CreatedOnShamsiDateTime ?? "-"}</strong> ثبت گردید<br/><br/>");
			content.AppendLine("<ul>");
			content.AppendLine($"<li>مربوط به دسته: <strong>{category.Title}</strong></li>");
			content.AppendLine($"<li>پیشوند کد: <strong style='color:green'>{requestPartCodePrefix}</strong></li>");
			content.AppendLine($"<li>کد پیشنهادی: <strong style='color:green'>{entity.Code}</strong></li>");
			content.AppendLine($"<li>عنوان کالای وارد شده: <strong style='color:green'>{entity.Title}</strong></li>");

			if (entity.Atributies.HasValue())
			{
				content.AppendLine("<li>پارامترها / ویژگی ها</li><ol>");
				foreach (var item in entity.Atributies.Split('|', StringSplitOptions.RemoveEmptyEntries))
				{
					var parts = item.Split('=', 2);
					if (parts.Length == 2)
						content.AppendLine($"<li>{parts[0].Trim()} : <strong>{parts[1].Trim()}</strong></li>");
				}
				content.AppendLine("</ol>");
			}

			content.AppendLine($"<li style='margin-top:10px'>توضیحات کاربر: <strong>{entity.Description ?? "-"}</strong></li>");
			content.AppendLine("</ul></td></tr></table></div>");
			return content.ToString();
		}

		private static string BuildApplyCreationPartEmailBody(
			PartCodingCategoryRequest entity,
			Part? createdPart,
			string? categoryTitle)
		{
			var content = new StringBuilder();
			content.AppendLine("<div style='width:100%;text-align:center;direction:rtl'>");
			content.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='100%'>");
			content.AppendLine("<tr style='background:#000aa0'><td><div style='font-size:14pt;font-family:Zar;color:#fff;text-align:center;direction:rtl'>گروه صنعتی هوایار</div></td></tr>");
			content.AppendLine("<tr style='text-align:right;direction:rtl;font-size:14pt;font-family:Zar'><td>");
			content.AppendLine("باسلام و احترام<br/>");
			content.AppendLine($"ایجاد کالای جدید با مشخصات ذیل، بواسطه <strong>{entity.ModifiedByName ?? "کاربر"}</strong> ");
			content.AppendLine($"در تاریخ <strong>{entity.ModifiedDateShamsiDateTime ?? "-"}</strong> انجام گردید<br/><br/>");
			content.AppendLine("<ul>");
			content.AppendLine($"<li>مربوط به دسته: <strong>{categoryTitle ?? "-"}</strong></li>");
			content.AppendLine($"<li>کد پیشنهادی: <strong style='color:green'>{entity.Code}</strong></li>");
			content.AppendLine($"<li>کد تولید شده: <strong style='color:green'>{createdPart?.Code ?? "-"}</strong></li>");
			content.AppendLine($"<li>عنوان کالای ایجاد شده: <strong style='color:green'>{createdPart?.Name ?? "-"}</strong></li>");
			content.AppendLine("</ul></td></tr></table></div>");
			return content.ToString();
		}

		#endregion

		#region Private Helpers

		private async Task<string> BuildPartCodePrefixAsync(long categoryId, CancellationToken cn)
		{
			var categories = await unitOfWork.Repository<PartCodingCategory>()
				.TableNoTracking
				.Select(c => new { c.Id, c.ParentId, c.Code })
				.ToListAsync(cn);

			var lookup = categories.ToDictionary(c => c.Id!.Value);
			if (!lookup.ContainsKey(categoryId))
				return string.Empty;

			var codes = new List<string>();
			long? currentId = categoryId;

			while (currentId.HasValue && lookup.TryGetValue(currentId.Value, out var current))
			{
				codes.Add(current.Code);
				currentId = current.ParentId;
			}

			codes.Reverse();
			return string.Concat(codes);
		}

		private static string BuildNodeText(CategoryFlatDto category, bool hasChildren)
		{
			var text = new StringBuilder();
			text.Append(category.Title);
			text.Append($" [{category.Code}]");

			if (category.Atributies.HasValue())
			{
				var normalized = category.Atributies.Replace(",", " - ");
				text.Append($" [{normalized}]");
			}

			return text.ToString();
		}

		private static List<PartGridRowDto> ApplyAdvancedSearchFilter(List<PartGridRowDto> parts, string? searchContent)
		{
			if (!searchContent.HasValue())
				return parts;

			if (searchContent.Contains('\n'))
				searchContent = searchContent.ToLower().Replace("\r\n", "|").Replace("\n", "|").Replace("\r", "|");

			var splittedContents = searchContent.ToLower().Split('|').Where(p => p.Length > 0).Select(p => p.Trim()).ToArray();
			if (splittedContents.Length == 0)
				return parts;

			var partCodes = new List<string>();
			var partNames = new List<string>();

			foreach (var content in splittedContents)
			{
				if (Regex.IsMatch(content, @"^\d") && content.Length == 10)
					partCodes.Add(content);
				else
					partNames.Add(content);
			}

			return parts.Where(p =>
				(partCodes.Count > 0 && partCodes.Any(code => (p.Code ?? string.Empty).Contains(code, StringComparison.OrdinalIgnoreCase)))
				|| (partNames.Count > 0 && (partNames.All(name => (p.Name ?? string.Empty).ToLower().Contains(name))))
			).ToList();
		}

		private static List<PartGridRowDto> ApplyOrdering(List<PartGridRowDto> parts, string? columnName, string orderDir)
		{
			var isDesc = orderDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

			return columnName switch
			{
				"Name" => isDesc ? parts.OrderByDescending(p => p.Name).ToList() : parts.OrderBy(p => p.Name).ToList(),
				"ModifiedDateShamsiDateTime" => isDesc ? parts.OrderByDescending(p => p.ModifiedDateShamsiDateTime).ToList() : parts.OrderBy(p => p.ModifiedDateShamsiDateTime).ToList(),
				"Number" => isDesc ? parts.OrderByDescending(p => p.Number).ToList() : parts.OrderBy(p => p.Number).ToList(),
				"Description" => isDesc ? parts.OrderByDescending(p => p.Description).ToList() : parts.OrderBy(p => p.Description).ToList(),
				"Foreign" => isDesc ? parts.OrderByDescending(p => p.Foreign).ToList() : parts.OrderBy(p => p.Foreign).ToList(),
				"EngineeringRoutine" => isDesc ? parts.OrderByDescending(p => p.EngineeringRoutine).ToList() : parts.OrderBy(p => p.EngineeringRoutine).ToList(),
				_ => isDesc ? parts.OrderByDescending(p => p.Code).ToList() : parts.OrderBy(p => p.Code).ToList()
			};
		}

		private static (bool IsValid, string ErrorMessage) ValidateRequiredAttributes(string? categoryAttributes, string? requestAttributes)
		{
			if (!categoryAttributes.HasValue())
				return (true, string.Empty);

			var requiredTitles = categoryAttributes
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(a => a.Trim())
				.Where(a => a.Length > 0)
				.ToList();

			if (requiredTitles.Count == 0)
				return (true, string.Empty);

			var provided = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (requestAttributes.HasValue())
			{
				foreach (var item in requestAttributes.Split('|', StringSplitOptions.RemoveEmptyEntries))
				{
					var parts = item.Split('=', 2);
					if (parts.Length == 2 && parts[0].Trim().HasValue())
						provided[parts[0].Trim()] = parts[1].Trim();
				}
			}

			foreach (var title in requiredTitles)
			{
				if (!provided.TryGetValue(title, out var value) || !value.HasValue())
					return (false, $"{title} نمی‌تواند خالی باشد.");
			}

			return (true, string.Empty);
		}

		#endregion

		#region DTOs

		private sealed class CategoryFlatDto
		{
			public long Id { get; set; }
			public long? ParentId { get; set; }
			public string Code { get; set; } = string.Empty;
			public string Title { get; set; } = string.Empty;
			public string? Atributies { get; set; }
			public string? Description { get; set; }
		}

		private sealed class JsTreeNodeDto
		{
			public string id { get; set; } = string.Empty;
			public string parent { get; set; } = "#";
			public string text { get; set; } = string.Empty;
			public JsTreeNodeDataDto data { get; set; } = new();
		}

		private sealed class JsTreeNodeDataDto
		{
			public string code { get; set; } = string.Empty;
			public string title { get; set; } = string.Empty;
			public string atributies { get; set; } = string.Empty;
			public string description { get; set; } = string.Empty;
			public long? parentId { get; set; }
			public bool hasChildren { get; set; }
		}

		private sealed class PartGridRowDto
		{
			public long? Id { get; set; }
			public string? Code { get; set; }
			public string? Name { get; set; }
			public string? ModifiedDateShamsiDateTime { get; set; }
			public string? Number { get; set; }
			public string? Description { get; set; }
			public PartTypeEnum? Type { get; set; }
			public string? UnitTitle { get; set; }
			public bool Foreign { get; set; }
			public bool EngineeringRoutine { get; set; }
		}

		public sealed class RequestNewPartDto
		{
			public long PartCodingCategoryId { get; set; }
			public string RequestPartCodePrefix { get; set; } = string.Empty;
			public string Title { get; set; } = string.Empty;
			public string? Atributies { get; set; }
			public string? Description { get; set; }
			public long? AttachmentId { get; set; }
		}

		#endregion
	}
}

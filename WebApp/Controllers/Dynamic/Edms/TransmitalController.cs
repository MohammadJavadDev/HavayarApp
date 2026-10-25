using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Actions.Edms;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Edms/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ترانسمیتال", typeof(Transmital))]
	public class TransmitalController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Transmital transmital, CancellationToken cn)
		{
			if (transmital.Id == null || transmital.Id == 0)
			{
				return await Add(transmital, cn);
			}
			var exist = await unitOfWork.Repository<Transmital>().TableNoTracking.AnyAsync(c => c.Id == transmital.Id);
			if (exist)
			{
				return await Update(transmital, cn);
			}
			return await Add(transmital, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Transmital transmital, CancellationToken cn)
		{
			var documentIds = ParseDocumentIds(transmital.DocumentIds);
			var userNumber = NormalizeUserNumber(transmital.DefinedByUserNumber);

			if (documentIds.Count == 0)
			{
				if (!transmital.ReserveNumberOnly)
					throw new Exception("انتخاب حداقل یک مدرک الزامی است.");

				return await AddNumberOnlyAsync(transmital, userNumber, cn);
			}

			var documents = await LoadAndValidateDocumentsAsync(documentIds, transmital.ProjectId, cn);
			var replySheetIds = await LoadReplySheetDocumentIdsAsync(documentIds, cn);
			var hasReplySheet = ResolveReplySheetMode(documents, replySheetIds);

			string? comments = transmital.Comments;
			string? generatedNumber = null;
			if (hasReplySheet)
			{
				if (string.IsNullOrEmpty(comments))
					comments = "is for reply sheet";
			}
			else
			{
				if (!IsAdministrator)
				{
					if (!IsCurrentUserDcc())
						throw new Exception("فقط کاربر DCC مجاز به ترانسمیتال مدرک است.");
					if (documents.Any(d => d.Status != DocumentStatusEnums.ApprovedByDcc))
						throw new Exception("ترانسمیتال فقط برای مدرک تاییدشده توسط DCC مجاز است.");
				}

				var project = await unitOfWork.Repository<Project>()
					.TableNoTracking
					.FirstOrDefaultAsync(p => p.Id == transmital.ProjectId, cn)
					?? throw new Exception("پروژه ترانسمیتال یافت نشد.");

				generatedNumber = await GenerateNextTransmittalNumberAsync(project, transmital.ProjectId, cn);
			}

			var number = ResolveTransmittalNumber(hasReplySheet, transmital, existingNumber: null, generatedNumber);

			Transmital? firstSaved = null;
			foreach (var document in documents)
			{
				var row = new Transmital
				{
					ProjectId = transmital.ProjectId,
					DocumentId = document.Id!.Value,
					Number = number,
					Comments = comments,
					AttachmentId = transmital.AttachmentId,
					EmployerRelated = transmital.EmployerRelated,
					DeliveryDateToEmployer = transmital.DeliveryDateToEmployer,
					IsForReplySheet = hasReplySheet,
					DefinedByUserNumber = userNumber
				};

				var saved = await unitOfWork.Repository<Transmital>().SaveAsync(row, cn, true);
				firstSaved ??= saved;
				await AddNotReviewCommentIfNeeded(document, cn);
			}

			await unitOfWork.SaveChangesAsync(cn);

			firstSaved!.DocumentIds = string.Join(",", documentIds);
			firstSaved.Number = number;
			firstSaved.Comments = comments;
			firstSaved.DefinedByUserNumber = userNumber;
			firstSaved.IsForReplySheet = hasReplySheet;
			firstSaved.ReserveNumberOnly = false;
			return Ok(firstSaved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Transmital transmital, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<Transmital>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == transmital.Id, cn)
				?? throw new Exception("ترانسمیتال یافت نشد.");

			var documentIds = ParseDocumentIds(transmital.DocumentIds);
			var userNumber = NormalizeUserNumber(transmital.DefinedByUserNumber);
			var siblings = await LoadSiblingRowsAsync(existing, cn);
			var linkedIds = siblings
				.Where(s => s.DocumentId is > 0)
				.Select(s => s.DocumentId!.Value)
				.ToHashSet();

			if (documentIds.Count == 0)
			{
				if (!transmital.ReserveNumberOnly || linkedIds.Count > 0)
					throw new Exception("انتخاب حداقل یک مدرک الزامی است.");

				return await UpdateNumberOnlyAsync(transmital, existing, siblings, userNumber, cn);
			}

			var documents = await LoadAndValidateDocumentsAsync(documentIds, transmital.ProjectId, cn, linkedIds);
			var replySheetIds = await LoadReplySheetDocumentIdsAsync(documentIds, cn);
			var hasReplySheet = ResolveReplySheetMode(documents, replySheetIds);

			if (!hasReplySheet && !IsAdministrator)
			{
				if (!IsCurrentUserDcc())
					throw new Exception("فقط کاربر DCC مجاز به ترانسمیتال مدرک است.");
				if (documents.Any(d => d.Id is > 0 && !linkedIds.Contains(d.Id.Value) && d.Status != DocumentStatusEnums.ApprovedByDcc))
					throw new Exception("ترانسمیتال فقط برای مدرک تاییدشده توسط DCC مجاز است.");
			}

			string? comments = transmital.Comments;
			if (hasReplySheet && string.IsNullOrEmpty(comments))
				comments = "is for reply sheet";

			// شماره خالیِ فرم، شماره ذخیره‌شده را پاک نمی‌کند
			var number = ResolveTransmittalNumber(hasReplySheet, transmital, existing.Number, generatedNumber: null);

			var existingByDocId = siblings
				.Where(s => s.DocumentId is > 0)
				.GroupBy(s => s.DocumentId!.Value)
				.ToDictionary(g => g.Key, g => g.First());
			var selectedSet = documentIds.ToHashSet();

			foreach (var sibling in siblings.Where(s => s.DocumentId is not > 0 || !selectedSet.Contains(s.DocumentId!.Value)).ToList())
			{
				await unitOfWork.Repository<Transmital>().DeleteAsync(sibling, cn, true);
			}

			Transmital? returnRow = null;
			foreach (var document in documents)
			{
				var docId = document.Id!.Value;
				if (existingByDocId.TryGetValue(docId, out var sibling))
				{
					sibling.ProjectId = transmital.ProjectId;
					sibling.DocumentId = docId;
					sibling.Number = number;
					sibling.Comments = comments;
					sibling.AttachmentId = transmital.AttachmentId;
					sibling.EmployerRelated = transmital.EmployerRelated;
					sibling.DeliveryDateToEmployer = transmital.DeliveryDateToEmployer;
					sibling.IsForReplySheet = hasReplySheet;
					sibling.DefinedByUserNumber = userNumber;
					var updated = await unitOfWork.Repository<Transmital>().UpdateAsync(sibling, cn, true);
					if (sibling.Id == existing.Id || returnRow == null)
						returnRow = updated;
				}
				else
				{
					var row = new Transmital
					{
						ProjectId = transmital.ProjectId,
						DocumentId = docId,
						Number = number,
						Comments = comments,
						AttachmentId = transmital.AttachmentId,
						EmployerRelated = transmital.EmployerRelated,
						DeliveryDateToEmployer = transmital.DeliveryDateToEmployer,
						IsForReplySheet = hasReplySheet,
						DefinedByUserNumber = userNumber
					};
					var saved = await unitOfWork.Repository<Transmital>().SaveAsync(row, cn, true);
					returnRow ??= saved;
					await AddNotReviewCommentIfNeeded(document, cn);
				}
			}

			await unitOfWork.SaveChangesAsync(cn);

			returnRow ??= existing;
			returnRow.DocumentIds = string.Join(",", documentIds);
			returnRow.Number = number;
			returnRow.Comments = comments;
			returnRow.DefinedByUserNumber = userNumber;
			returnRow.IsForReplySheet = hasReplySheet;
			returnRow.ReserveNumberOnly = false;
			return Ok(returnRow);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<Transmital>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model == null)
				return Ok();

			var rows = await LoadRowsToDeleteAsync(model, cn);
			foreach (var row in rows)
				await unitOfWork.Repository<Transmital>().DeleteAsync(row, cn, true);

			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = await unitOfWork.Repository<Transmital>().TableNoTracking
					.Include(c => c.Project)
					.Include(c => c.Document)
					.Include(c => c.Attachment)
					.FirstOrDefaultAsync(c => c.Id == id, cn);

				if (entity != null)
				{
					entity.DocumentIds = await GetSiblingDocumentIdsAsync(entity, cn);
					ViewData["DocumentIds"] = entity.DocumentIds;
				}

				return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", entity ?? new Transmital());
			}
			var newEntity = new Transmital();
			return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Transmital();
			return View(@"\Views\Panel\Edms\Transmital\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Edms\Transmital\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Transmital>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Transmital>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetNextNumber(long projectId, CancellationToken cn)
		{
			if (projectId <= 0)
				return Ok(new { number = "" });

			var project = await unitOfWork.Repository<Project>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == projectId, cn);
			if (project == null)
				return Ok(new { number = "" });

			var number = await GenerateNextTransmittalNumberAsync(project, projectId, cn);
			return Ok(new { number });
		}


		private const long DccRoleId = 200000;
		private const string DccRoleName = "EdmsDocumentsDccUsers";

		private bool IsCurrentUserDcc() =>
			CurrentUserHasRole(DccRoleId) || CurrentUserHasRole(DccRoleName);

		private static List<long> ParseDocumentIds(string? documentIds)
		{
			if (string.IsNullOrWhiteSpace(documentIds))
				return [];

			return documentIds
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(s => long.TryParse(s, out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}

		private static bool IsAllowedTransmittalDocument(Document document, bool isReplySheet) =>
			document.IsActive == IsActiveEnum.Active
			&& document.IsLatest
			&& (document.Status == DocumentStatusEnums.ApprovedByDcc || isReplySheet);

		private async Task<HashSet<long>> LoadReplySheetDocumentIdsAsync(List<long> documentIds, CancellationToken cn)
		{
			var ids = await unitOfWork.Repository<DocumentComment>().TableNoTracking
				.Where(c => documentIds.Contains(c.DocumentId) && c.Status == DocumentStatusEnums.ReplaySheet)
				.Select(c => c.DocumentId)
				.Distinct()
				.ToListAsync(cn);

			return ids.ToHashSet();
		}

		private async Task<List<Document>> LoadAndValidateDocumentsAsync(
			List<long> documentIds,
			long projectId,
			CancellationToken cn,
			HashSet<long>? alreadyLinkedIds = null)
		{
			var documents = await unitOfWork.Repository<Document>().TableNoTracking
				.Where(d => documentIds.Contains(d.Id!.Value))
				.ToListAsync(cn);

			if (documents.Count != documentIds.Count)
				throw new Exception("یکی از مدارک انتخاب‌شده یافت نشد.");

			if (documents.Any(d => d.ProjectId != projectId))
				throw new Exception("همه مدارک باید متعلق به پروژه انتخاب‌شده باشند.");

			var replySheetIds = await LoadReplySheetDocumentIdsAsync(documentIds, cn);
			foreach (var document in documents)
			{
				var id = document.Id!.Value;
				if (alreadyLinkedIds?.Contains(id) == true)
					continue;
				if (!IsAllowedTransmittalDocument(document, replySheetIds.Contains(id)))
					throw new Exception("مدرک انتخاب‌شده مجاز به ترانسمیتال نیست.");
			}

			return documents;
		}

		private static bool ResolveReplySheetMode(List<Document> documents, HashSet<long> replySheetIds)
		{
			var replyFlags = documents.Select(d => replySheetIds.Contains(d.Id!.Value)).ToList();
			var anyReply = replyFlags.Any(x => x);
			var allReply = replyFlags.All(x => x);
			if (anyReply && !allReply)
				throw new Exception("امکان انتخاب همزمان مدرک عادی و ReplySheet وجود ندارد.");
			return allReply;
		}

		private static string ResolveTransmittalNumber(bool hasReplySheet, Transmital posted, string? existingNumber, string? generatedNumber)
		{
			if (hasReplySheet)
			{
				if (!string.IsNullOrWhiteSpace(posted.DefinedByUserNumber))
					return posted.DefinedByUserNumber.Trim();
				if (!string.IsNullOrWhiteSpace(existingNumber))
					return existingNumber;
				throw new Exception("وارد کردن شماره تعریف‌شده توسط کاربر برای ReplySheet الزامی است.");
			}

			if (!string.IsNullOrWhiteSpace(generatedNumber))
				return generatedNumber;
			if (!string.IsNullOrWhiteSpace(posted.Number))
				return posted.Number.Trim();
			if (!string.IsNullOrWhiteSpace(existingNumber))
				return existingNumber;

			throw new Exception("شماره ترانسمیتال تولید نشد.");
		}

		private async Task<string> GenerateNextTransmittalNumberAsync(Project project, long projectId, CancellationToken cn)
		{
			// هر مدرک یک ردیف است؛ شمارش ردیف‌ها شماره را به اندازه تعداد مدارک ترانسمیتال‌های قبلی جلو می‌برد.
			// شماره بعدی = بزرگ‌ترین شماره همین پروژه + ۱.
			var numbers = await unitOfWork.Repository<Transmital>()
				.TableNoTracking
				.Where(c => c.ProjectId == projectId && !(c.IsForReplySheet ?? false) && c.Number != null)
				.Select(c => c.Number!)
				.Distinct()
				.ToListAsync(cn);

			var maxSequence = 0;
			foreach (var number in numbers)
			{
				if (TryReadTransmittalSequence(number, out var sequence) && sequence > maxSequence)
					maxSequence = sequence;
			}

			var prefix = project.TransmissionPrefix?.Trim() ?? "";
			return string.Concat(prefix, "-", (maxSequence + 1).ToString().PadLeft(4, '0'));
		}

		private static bool TryReadTransmittalSequence(string? number, out int sequence)
		{
			sequence = 0;
			if (string.IsNullOrWhiteSpace(number))
				return false;

			var trimmed = number.Trim();
			var dash = trimmed.LastIndexOf('-');
			if (dash < 0 || dash >= trimmed.Length - 1)
				return false;

			var tail = trimmed[(dash + 1)..];
			return tail.Length > 0
				&& tail.All(char.IsDigit)
				&& int.TryParse(tail, out sequence);
		}

		private async Task<List<Transmital>> LoadRowsToDeleteAsync(Transmital model, CancellationToken cn)
		{
			// شماره Reply Sheet تکراری است (مثل RDS). فقط همان ردیف حذف می‌شود.
			if (model.IsForReplySheet == true || string.IsNullOrWhiteSpace(model.Number))
				return [model];

			var rows = await unitOfWork.Repository<Transmital>().TableNoTracking
				.Where(c => c.ProjectId == model.ProjectId
					&& c.Number == model.Number
					&& !(c.IsForReplySheet ?? false))
				.ToListAsync(cn);

			return rows.Count > 0 ? rows : [model];
		}

		private async Task<List<Transmital>> LoadSiblingRowsAsync(Transmital existing, CancellationToken cn)
		{
			if (string.IsNullOrEmpty(existing.Number))
			{
				var single = await unitOfWork.Repository<Transmital>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == existing.Id, cn);
				return single != null ? [single] : [];
			}

			return await unitOfWork.Repository<Transmital>().TableNoTracking
				.Where(c => c.ProjectId == existing.ProjectId && c.Number == existing.Number)
				.ToListAsync(cn);
		}

		private async Task<string> GetSiblingDocumentIdsAsync(Transmital entity, CancellationToken cn)
		{
			if (string.IsNullOrEmpty(entity.Number))
				return entity.DocumentId is > 0 ? entity.DocumentId.Value.ToString() : "";

			var ids = await unitOfWork.Repository<Transmital>().TableNoTracking
				.Where(c => c.ProjectId == entity.ProjectId && c.Number == entity.Number && c.DocumentId != null)
				.Select(c => c.DocumentId!.Value)
				.Distinct()
				.ToListAsync(cn);

			return string.Join(",", ids);
		}

		private static string? NormalizeUserNumber(string? value) =>
			string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private async Task<IActionResult> AddNumberOnlyAsync(Transmital transmital, string? userNumber, CancellationToken cn)
		{
			if (transmital.ProjectId <= 0)
				throw new Exception("انتخاب پروژه الزامی است.");
			if (string.IsNullOrWhiteSpace(transmital.DeliveryDateToEmployer))
				throw new Exception("برای افزایش شماره بدون مدرک، تاریخ ارسال به کارفرما را وارد کنید.");
			if (!IsAdministrator && !IsCurrentUserDcc())
				throw new Exception("فقط کاربر DCC مجاز به ترانسمیتال مدرک است.");

			var project = await unitOfWork.Repository<Project>().TableNoTracking
				.FirstOrDefaultAsync(p => p.Id == transmital.ProjectId, cn)
				?? throw new Exception("پروژه ترانسمیتال یافت نشد.");

			var number = await GenerateNextTransmittalNumberAsync(project, transmital.ProjectId, cn);
			var saved = await unitOfWork.Repository<Transmital>().SaveAsync(new Transmital
			{
				ProjectId = transmital.ProjectId,
				DocumentId = null,
				Number = number,
				Comments = transmital.Comments,
				AttachmentId = transmital.AttachmentId,
				EmployerRelated = transmital.EmployerRelated,
				DeliveryDateToEmployer = transmital.DeliveryDateToEmployer,
				IsForReplySheet = false,
				DefinedByUserNumber = userNumber
			}, cn, true);

			await unitOfWork.SaveChangesAsync(cn);
			saved.DocumentIds = "";
			saved.Number = number;
			saved.DefinedByUserNumber = userNumber;
			saved.IsForReplySheet = false;
			saved.ReserveNumberOnly = true;
			return Ok(saved);
		}

		private async Task<IActionResult> UpdateNumberOnlyAsync(
			Transmital posted,
			Transmital existing,
			List<Transmital> siblings,
			string? userNumber,
			CancellationToken cn)
		{
			if (posted.ProjectId <= 0)
				throw new Exception("انتخاب پروژه الزامی است.");
			if (string.IsNullOrWhiteSpace(posted.DeliveryDateToEmployer))
				throw new Exception("برای افزایش شماره بدون مدرک، تاریخ ارسال به کارفرما را وارد کنید.");
			if (!IsAdministrator && !IsCurrentUserDcc())
				throw new Exception("فقط کاربر DCC مجاز به ترانسمیتال مدرک است.");

			string? number;
			if (posted.ProjectId != existing.ProjectId)
			{
				var project = await unitOfWork.Repository<Project>().TableNoTracking
					.FirstOrDefaultAsync(p => p.Id == posted.ProjectId, cn)
					?? throw new Exception("پروژه ترانسمیتال یافت نشد.");
				number = await GenerateNextTransmittalNumberAsync(project, posted.ProjectId, cn);
			}
			else
			{
				number = ResolveTransmittalNumber(false, posted, existing.Number, generatedNumber: null);
			}
			var rows = siblings.Count > 0 ? siblings : [existing];
			Transmital? returnRow = null;
			foreach (var sibling in rows)
			{
				sibling.ProjectId = posted.ProjectId;
				sibling.DocumentId = null;
				sibling.Number = number;
				sibling.Comments = posted.Comments;
				sibling.AttachmentId = posted.AttachmentId;
				sibling.EmployerRelated = posted.EmployerRelated;
				sibling.DeliveryDateToEmployer = posted.DeliveryDateToEmployer;
				sibling.IsForReplySheet = false;
				sibling.DefinedByUserNumber = userNumber;
				var updated = await unitOfWork.Repository<Transmital>().UpdateAsync(sibling, cn, true);
				if (sibling.Id == existing.Id || returnRow == null)
					returnRow = updated;
			}

			await unitOfWork.SaveChangesAsync(cn);
			returnRow ??= existing;
			returnRow.DocumentIds = "";
			returnRow.Number = number;
			returnRow.DefinedByUserNumber = userNumber;
			returnRow.IsForReplySheet = false;
			returnRow.ReserveNumberOnly = true;
			return Ok(returnRow);
		}

		private async Task AddNotReviewCommentIfNeeded(Document document, CancellationToken ct)
		{
			if (document.Id is not > 0)
				return;
			if (document.Status == DocumentStatusEnums.NotReview)
				return;

			await unitOfWork.Repository<DocumentComment>().AddAsync(new DocumentComment
			{
				DocumentId = document.Id.Value,
				Status = DocumentStatusEnums.NotReview,
				Comment = TransmitalAction.CreatedFromTransmittalComment
			}, ct, false);
		}
	}
}

using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Gnr/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اسناد سازمانی", typeof(OrganizationalDocument))]
	public class OrganizationalDocumentController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IFileService fileService) : BaseController
	{
		private const int MaxFilesPerAdd = 20;
		private const long MaxFileSizeBytes = 51200L * 1024L;

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OrganizationalDocument organizationalDocument, CancellationToken cn)
		{
			if (organizationalDocument.Id == null || organizationalDocument.Id == 0)
				return await Add(organizationalDocument, cn);

			var exist = await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
				.AnyAsync(c => c.Id == organizationalDocument.Id, cn);
			if (exist)
				return await Update(organizationalDocument, cn);

			return await Add(organizationalDocument, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OrganizationalDocument organizationalDocument, CancellationToken cn)
		{
			var validation = ValidateDocument(organizationalDocument, isAdd: true);
			if (validation != null)
				return BadRequest(validation);

			var fileIds = ParseIds(organizationalDocument.FileIds);
			if (organizationalDocument.FileId is > 0 && !fileIds.Contains(organizationalDocument.FileId.Value))
				fileIds.Insert(0, organizationalDocument.FileId.Value);

			if (fileIds.Count == 0)
				return BadRequest("انتخاب فایل الزامی است");
			if (fileIds.Count > MaxFilesPerAdd)
				return BadRequest($"حداکثر {MaxFilesPerAdd} فایل در هر ثبت مجاز است");

			var files = await unitOfWork.Repository<FileEntity>().TableNoTracking
				.Where(f => fileIds.Contains(f.Id!.Value))
				.ToListAsync(cn);

			if (files.Count == 0)
				return BadRequest("فایل انتخاب‌شده یافت نشد");

			foreach (var file in files)
			{
				if (file.Size > MaxFileSizeBytes)
					return BadRequest($"حجم فایل «{file.OriginalName}» بیشتر از ۵۰ مگابایت است");
			}

			var skipped = new List<string>();
			var saved = new List<OrganizationalDocument>();

			foreach (var fileId in fileIds)
			{
				var file = files.FirstOrDefault(f => f.Id == fileId);
				if (file == null)
					continue;

				if (await IsDuplicateFileName(organizationalDocument, file.OriginalName, excludeDocumentId: null, cn))
				{
					skipped.Add(file.OriginalName);
					continue;
				}

				var row = CreateDocumentRow(organizationalDocument, fileId);
				var entity = await unitOfWork.Repository<OrganizationalDocument>().SaveAsync(row, cn, true);
				saved.Add(entity);
			}

			if (saved.Count == 0)
			{
				if (skipped.Count > 0)
					return BadRequest("فایل تکراری ذخیره نشد: " + string.Join("، ", skipped));
				return BadRequest("هیچ سندی ذخیره نشد");
			}

			var first = await LoadDocumentForClient(saved[0].Id!.Value, cn);
			first.SaveWarning = BuildSaveWarning(saved.Count, skipped);
			return Ok(first);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OrganizationalDocument organizationalDocument, CancellationToken cn)
		{
			var validation = ValidateDocument(organizationalDocument, isAdd: false);
			if (validation != null)
				return BadRequest(validation);

			var existing = await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
				.Include(c => c.Folder).ThenInclude(f => f!.AllowedUsers)
				.Include(c => c.File)
				.FirstOrDefaultAsync(c => c.Id == organizationalDocument.Id, cn);
			if (existing == null)
				return BadRequest("سند یافت نشد");

			if (!CanViewDocument(existing.Folder))
				return BadRequest("دسترسی به این سند را ندارید");

			var newFileId = organizationalDocument.FileId;
			if (newFileId is > 0 && newFileId != existing.FileId)
			{
				var newFile = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == newFileId, cn);
				if (newFile == null)
					return BadRequest("فایل انتخاب‌شده یافت نشد");
				if (newFile.Size > MaxFileSizeBytes)
					return BadRequest($"حجم فایل «{newFile.OriginalName}» بیشتر از ۵۰ مگابایت است");
				if (await IsDuplicateFileName(organizationalDocument, newFile.OriginalName, existing.Id, cn))
					return BadRequest("فایل با این نام در همین مسیر قبلاً ذخیره شده است");
			}

			existing.FolderId = organizationalDocument.FolderId;
			existing.SecondLevelFolderId = organizationalDocument.SecondLevelFolderId;
			existing.ThirdLevelFolderId = organizationalDocument.ThirdLevelFolderId;
			existing.Title = organizationalDocument.Title?.Trim();
			existing.TitleInLatin = organizationalDocument.TitleInLatin?.Trim();
			existing.Brand = string.IsNullOrWhiteSpace(organizationalDocument.Brand) ? null : organizationalDocument.Brand.Trim();
			existing.Revision = organizationalDocument.Revision;
			existing.Year = NormalizeYear(organizationalDocument.Year);
			existing.Comment = organizationalDocument.Comment;
			if (newFileId is > 0)
				existing.FileId = newFileId;

			existing.Folder = null;
			existing.SecondLevelFolder = null;
			existing.ThirdLevelFolder = null;
			existing.File = null;

			await unitOfWork.Repository<OrganizationalDocument>().UpdateAsync(existing, cn, true);
			return Ok(await LoadDocumentForClient(existing.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
				.Include(c => c.Folder).ThenInclude(f => f!.AllowedUsers)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model == null)
				return Ok();

			if (!CanViewDocument(model.Folder))
				return BadRequest("دسترسی به این سند را ندارید");

			await unitOfWork.Repository<OrganizationalDocument>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
					.Include(c => c.Folder).ThenInclude(f => f!.OrganizationUnit)
					.Include(c => c.Folder).ThenInclude(f => f!.AllowedUsers)
					.Include(c => c.SecondLevelFolder)
					.Include(c => c.ThirdLevelFolder)
					.Include(c => c.File)
					.FirstOrDefault(c => c.Id == id);

				if (entity != null && !CanViewDocument(entity.Folder))
					throw new Exception("دسترسی به این سند را ندارید");

				return View(@"\Views\Panel\Gnr\OrganizationalDocument\Edit.cshtml", entity ?? new OrganizationalDocument());
			}

			return View(@"\Views\Panel\Gnr\OrganizationalDocument\Edit.cshtml", new OrganizationalDocument());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Gnr\OrganizationalDocument\Edit.cshtml", new OrganizationalDocument());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Gnr\OrganizationalDocument\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل", ActionAccessType.View)]
		public async Task<IActionResult> Download(long id, CancellationToken cn)
		{
			var document = await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
				.Include(c => c.Folder).ThenInclude(f => f!.AllowedUsers)
				.FirstOrDefaultAsync(c => c.Id == id, cn);

			if (document == null)
				return NotFound("سند یافت نشد");
			if (!CanViewDocument(document.Folder))
				return BadRequest("دسترسی به این سند را ندارید");
			if (document.FileId == null || document.FileId == 0)
				return NotFound("فایلی برای این سند ثبت نشده است");

			var (stream, contentType, fileName) = await fileService.DownloadAsync(document.FileId.Value, cn);
			return File(stream, contentType, fileName);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OrganizationalDocument>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			var query = unitOfWork.Repository<OrganizationalDocument>().TableNoTracking.AsQueryable();

			if (!IsAdministrator)
			{
				var userId = CurrentUserId;
				var orgUnitId = CurrentOrganizationUnitId;
				query = query.Where(c =>
					c.Folder != null && (
						!c.Folder.IsPrivate
						|| c.Folder.OrganizationUnitId == orgUnitId
						|| (userId != null && c.Folder.AllowedUsers.Any(u => u.UserId == userId))));
			}

			var total = await query.CountAsync(cn);
			var take = request.length > 0 ? request.length : 25;
			var items = await query
				.OrderByDescending(c => c.Id)
				.Skip(request.start)
				.Take(take)
				.Select(c => new
				{
					c.Id,
					c.Title,
					c.TitleInLatin,
					FolderTitle = c.Folder != null ? c.Folder.Title : null,
					c.Brand,
					c.Year,
					c.Revision,
					FileName = c.File != null ? c.File.OriginalName : null,
					FileSize = c.File != null ? c.File.Size : (long?)null,
					c.Comment,
					c.FileId
				})
				.ToListAsync(cn);

			return Ok(new DataTableResponse
			{
				Draw = request.draw,
				RecordsTotal = total,
				RecordsFiltered = total,
				Data = items
			});
		}

		private bool CanViewDocument(DocumentFolder? folder)
		{
			if (IsAdministrator)
				return true;
			if (folder == null)
				return false;
			if (!folder.IsPrivate)
				return true;
			if (folder.OrganizationUnitId != null && folder.OrganizationUnitId == CurrentOrganizationUnitId)
				return true;
			if (CurrentUserId != null && folder.AllowedUsers != null && folder.AllowedUsers.Any(u => u.UserId == CurrentUserId))
				return true;
			return false;
		}

		private static string? ValidateDocument(OrganizationalDocument document, bool isAdd)
		{
			if (document.FolderId == 0)
				return "پوشه الزامی است";
			if (string.IsNullOrWhiteSpace(document.Title))
				return "عنوان الزامی است";
			if (string.IsNullOrWhiteSpace(document.TitleInLatin))
				return "عنوان لاتین الزامی است";
			if (!IsEnglishText(document.TitleInLatin))
				return "عنوان لاتین باید فقط با حروف انگلیسی باشد";
			if (document.Year == 0)
				return "سال نامعتبر است";
			if (isAdd)
			{
				var fileIds = ParseIds(document.FileIds);
				if (document.FileId is > 0 && !fileIds.Contains(document.FileId.Value))
					fileIds.Add(document.FileId.Value);
				if (fileIds.Count == 0)
					return "انتخاب فایل الزامی است";
				if (fileIds.Count > MaxFilesPerAdd)
					return $"حداکثر {MaxFilesPerAdd} فایل در هر ثبت مجاز است";
			}

			return null;
		}

		private static OrganizationalDocument CreateDocumentRow(OrganizationalDocument source, long fileId)
		{
			return new OrganizationalDocument
			{
				FolderId = source.FolderId,
				SecondLevelFolderId = source.SecondLevelFolderId,
				ThirdLevelFolderId = source.ThirdLevelFolderId,
				Title = source.Title?.Trim(),
				TitleInLatin = source.TitleInLatin?.Trim(),
				Brand = string.IsNullOrWhiteSpace(source.Brand) ? null : source.Brand.Trim(),
				Revision = source.Revision,
				Year = NormalizeYear(source.Year),
				FileId = fileId,
				Comment = source.Comment,
				HtsId = 0
			};
		}

		private async Task<bool> IsDuplicateFileName(OrganizationalDocument document, string? originalName, long? excludeDocumentId, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(originalName))
				return false;

			var brand = string.IsNullOrWhiteSpace(document.Brand) ? null : document.Brand.Trim();
			var year = NormalizeYear(document.Year);

			return await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking.AnyAsync(d =>
				d.FolderId == document.FolderId
				&& d.Brand == brand
				&& d.Year == year
				&& d.Revision == document.Revision
				&& d.File != null
				&& d.File.OriginalName == originalName
				&& (excludeDocumentId == null || d.Id != excludeDocumentId), cn);
		}

		private async Task<OrganizationalDocument> LoadDocumentForClient(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking
				.Include(c => c.Folder).ThenInclude(f => f!.OrganizationUnit)
				.Include(c => c.SecondLevelFolder)
				.Include(c => c.ThirdLevelFolder)
				.Include(c => c.File)
				.FirstAsync(c => c.Id == id, cn);

			if (entity.Folder != null)
				entity.Folder.AllowedUsers = new List<DocumentFolderUser>();
			return entity;
		}

		private static string? BuildSaveWarning(int savedCount, List<string> skipped)
		{
			var parts = new List<string>();
			if (savedCount > 1)
				parts.Add($"{savedCount} سند ذخیره شد");
			if (skipped.Count > 0)
				parts.Add("فایل‌های تکراری ذخیره نشدند: " + string.Join("، ", skipped));
			return parts.Count == 0 ? null : string.Join(" — ", parts);
		}

		private static short? NormalizeYear(short? year) => year is null or 0 ? null : year;

		private static bool IsEnglishText(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;
			return text.All(c => c <= 0x7E);
		}

		private static List<long> ParseIds(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return [];

			return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}
	}
}

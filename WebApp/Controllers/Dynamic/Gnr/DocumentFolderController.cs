using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Gnr/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پوشه اسناد سازمانی", typeof(DocumentFolder))]
	public class DocumentFolderController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(DocumentFolder documentFolder, CancellationToken cn)
		{
			if (documentFolder.Id == null || documentFolder.Id == 0)
				return await Add(documentFolder, cn);

			var exist = await unitOfWork.Repository<DocumentFolder>().TableNoTracking.AnyAsync(c => c.Id == documentFolder.Id, cn);
			if (exist)
				return await Update(documentFolder, cn);

			return await Add(documentFolder, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(DocumentFolder documentFolder, CancellationToken cn)
		{
			var validation = ValidateRootFolder(documentFolder);
			if (validation != null)
				return BadRequest(validation);

			PrepareFolderForPersist(documentFolder, isRoot: true);
			documentFolder.FolderLevel = 1;
			documentFolder.ParentId = null;
			documentFolder.HtsId = 0;

			var allowedUserIds = ParseIds(documentFolder.AllowedUserIds);
			documentFolder.AllowedUsers = new List<DocumentFolderUser>();
			documentFolder.Children = new List<DocumentFolder>();

			var entity = await unitOfWork.Repository<DocumentFolder>().SaveAsync(documentFolder, cn, true);
			await ReplaceAllowedUsers(entity.Id!.Value, allowedUserIds, cn);
			return Ok(await LoadFolderForClient(entity.Id.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(DocumentFolder documentFolder, CancellationToken cn)
		{
			var validation = ValidateRootFolder(documentFolder);
			if (validation != null)
				return BadRequest(validation);

			var existing = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == documentFolder.Id, cn);
			if (existing == null)
				return BadRequest("پوشه یافت نشد");

			var allowedUserIds = ParseIds(documentFolder.AllowedUserIds);
			existing.Title = documentFolder.Title?.Trim();
			existing.FolderTitle = documentFolder.FolderTitle?.Trim();
			existing.OrganizationUnitId = documentFolder.OrganizationUnitId;
			existing.IsPrivate = documentFolder.IsPrivate;
			existing.Comment = documentFolder.Comment;
			existing.FolderLevel = existing.FolderLevel == 0 ? 1 : existing.FolderLevel;
			existing.AllowedUsers = null!;
			existing.Children = null!;
			existing.Parent = null;
			existing.OrganizationUnit = null;

			await unitOfWork.Repository<DocumentFolder>().UpdateAsync(existing, cn, true);
			await ReplaceAllowedUsers(existing.Id!.Value, allowedUserIds, cn);
			return Ok(await LoadFolderForClient(existing.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<DocumentFolder>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (model == null)
				return Ok();

			if (await FolderHasDocuments(id, cn))
				return BadRequest("این پوشه (یا پوشه‌های فرزند آن) دارای سند است و قابل حذف نیست");

			var childIds = await GetDescendantIds(id, cn);
			if (childIds.Count > 0)
			{
				childIds.Reverse();
				await unitOfWork.Repository<DocumentFolderUser>().DeleteWhereAsync(c => childIds.Contains(c.DocumentFolderId), cn);
				await unitOfWork.Repository<DocumentFolder>().DeleteWhereAsync(c => c.Id != null && childIds.Contains(c.Id.Value), cn);
			}

			await unitOfWork.Repository<DocumentFolderUser>().DeleteWhereAsync(c => c.DocumentFolderId == id, cn);
			await unitOfWork.Repository<DocumentFolder>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<DocumentFolder>().TableNoTracking
					.Include(c => c.OrganizationUnit)
					.Include(c => c.AllowedUsers).ThenInclude(u => u.User)
					.Include(c => c.Children)
					.FirstOrDefault(c => c.Id == id);

				if (entity != null)
					FillAllowedUserBindFields(entity);

				return View(@"\Views\Panel\Gnr\DocumentFolder\Edit.cshtml", entity ?? new DocumentFolder { FolderLevel = 1 });
			}

			return View(@"\Views\Panel\Gnr\DocumentFolder\Edit.cshtml", new DocumentFolder { FolderLevel = 1 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Gnr\DocumentFolder\Edit.cshtml", new DocumentFolder { FolderLevel = 1 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Gnr\DocumentFolder\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<DocumentFolder>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			var query = unitOfWork.Repository<DocumentFolder>().TableNoTracking
				.Where(c => c.FolderLevel == 1 || c.ParentId == null);

			var total = await query.CountAsync(cn);
			var take = request.length > 0 ? request.length : 25;
			var items = await query
				.OrderBy(c => c.Title)
				.Skip(request.start)
				.Take(take)
				.Select(c => new
				{
					c.Id,
					c.Title,
					c.FolderTitle,
					OrganizationUnitTitle = c.OrganizationUnit != null ? c.OrganizationUnit.Title : null,
					c.IsPrivate,
					c.Comment,
					c.FolderLevel,
					c.HtsId
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

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت پوشه‌های فرزند", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetChildren(long parentId, CancellationToken cn)
		{
			var children = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
				.Where(c => c.ParentId == parentId)
				.OrderBy(c => c.Title)
				.Select(c => new DocumentFolderChildVm
				{
					Id = c.Id,
					Title = c.Title,
					FolderTitle = c.FolderTitle,
					Comment = c.Comment,
					FolderLevel = c.FolderLevel
				})
				.ToListAsync(cn);

			var childIds = children.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();
			var grandchildren = childIds.Count == 0
				? new List<DocumentFolderChildVm>()
				: await unitOfWork.Repository<DocumentFolder>().TableNoTracking
					.Where(c => c.ParentId != null && childIds.Contains(c.ParentId.Value))
					.OrderBy(c => c.Title)
					.Select(c => new DocumentFolderChildVm
					{
						Id = c.Id,
						ParentId = c.ParentId,
						Title = c.Title,
						FolderTitle = c.FolderTitle,
						Comment = c.Comment,
						FolderLevel = c.FolderLevel
					})
					.ToListAsync(cn);

			foreach (var child in children)
			{
				child.Children = grandchildren.Where(g => g.ParentId == child.Id).ToList();
			}

			return Ok(children);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پوشه فرزند", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> SaveChild(DocumentFolder child, CancellationToken cn)
		{
			if (child.ParentId == null || child.ParentId == 0)
				return BadRequest("پوشه والد الزامی است");

			if (string.IsNullOrWhiteSpace(child.Title) || string.IsNullOrWhiteSpace(child.FolderTitle))
				return BadRequest("عنوان و عنوان پوشه الزامی است");

			var parent = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == child.ParentId, cn);
			if (parent == null)
				return BadRequest("پوشه والد یافت نشد");

			if (parent.FolderLevel >= 3)
				return BadRequest("امکان تعریف سطح پایین‌تر از ۳ وجود ندارد");

			if (child.Id != null && child.Id != 0)
			{
				var existing = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == child.Id, cn);
				if (existing == null)
					return BadRequest("پوشه فرزند یافت نشد");

				existing.Title = child.Title.Trim();
				existing.FolderTitle = child.FolderTitle.Trim();
				existing.Comment = child.Comment;
				existing.ParentId = parent.Id;
				existing.FolderLevel = parent.FolderLevel + 1;
				existing.OrganizationUnitId = parent.OrganizationUnitId;
				existing.IsPrivate = false;
				existing.AllowedUsers = null!;
				existing.Children = null!;
				existing.Parent = null;
				existing.OrganizationUnit = null;

				var updated = await unitOfWork.Repository<DocumentFolder>().UpdateAsync(existing, cn, true);
				return Ok(updated);
			}

			var entity = new DocumentFolder
			{
				ParentId = parent.Id,
				FolderLevel = parent.FolderLevel + 1,
				OrganizationUnitId = parent.OrganizationUnitId,
				Title = child.Title.Trim(),
				FolderTitle = child.FolderTitle.Trim(),
				Comment = child.Comment,
				IsPrivate = false,
				HtsId = 0
			};

			var saved = await unitOfWork.Repository<DocumentFolder>().SaveAsync(entity, cn, true);
			return Ok(saved);
		}

		private static string? ValidateRootFolder(DocumentFolder folder)
		{
			if (folder.OrganizationUnitId == null || folder.OrganizationUnitId == 0)
				return "واحد سازمانی الزامی است";
			if (string.IsNullOrWhiteSpace(folder.Title))
				return "عنوان الزامی است";
			if (string.IsNullOrWhiteSpace(folder.FolderTitle))
				return "عنوان پوشه الزامی است";
			if (folder.IsPrivate && ParseIds(folder.AllowedUserIds).Count == 0)
				return "برای پوشه خصوصی حداقل یک کاربر مجاز انتخاب کنید";
			return null;
		}

		private static void PrepareFolderForPersist(DocumentFolder folder, bool isRoot)
		{
			folder.Title = folder.Title?.Trim();
			folder.FolderTitle = folder.FolderTitle?.Trim();
			folder.Parent = null;
			folder.OrganizationUnit = null;
			folder.Children = new List<DocumentFolder>();
			if (isRoot)
				folder.ParentId = null;
		}

		private async Task ReplaceAllowedUsers(long folderId, List<long> userIds, CancellationToken cn)
		{
			await unitOfWork.Repository<DocumentFolderUser>().DeleteWhereAsync(c => c.DocumentFolderId == folderId, cn);
			if (userIds.Count == 0)
				return;

			var rows = userIds.Select(userId => new DocumentFolderUser
			{
				DocumentFolderId = folderId,
				UserId = userId
			}).ToList();

			await unitOfWork.Repository<DocumentFolderUser>().AddRangeAsync(rows, cn, true);
		}

		private async Task<DocumentFolder> LoadFolderForClient(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
				.Include(c => c.OrganizationUnit)
				.Include(c => c.AllowedUsers).ThenInclude(u => u.User)
				.FirstAsync(c => c.Id == id, cn);

			FillAllowedUserBindFields(entity);
			entity.Children = new List<DocumentFolder>();
			entity.Parent = null;
			entity.AllowedUsers = new List<DocumentFolderUser>();
			return entity;
		}

		private static void FillAllowedUserBindFields(DocumentFolder entity)
		{
			var users = entity.AllowedUsers?.Where(u => u.UserId != 0).ToList() ?? [];
			entity.AllowedUserIds = string.Join(",", users.Select(u => u.UserId));
			entity.AllowedUserNames = string.Join(" , ", users
				.Select(u => u.User?.NameFa ?? u.User?.Name)
				.Where(n => !string.IsNullOrWhiteSpace(n)));
		}

		private async Task<bool> FolderHasDocuments(long folderId, CancellationToken cn)
		{
			var ids = await GetDescendantIds(folderId, cn);
			ids.Add(folderId);
			return await unitOfWork.Repository<OrganizationalDocument>().TableNoTracking.AnyAsync(d =>
				ids.Contains(d.FolderId)
				|| (d.SecondLevelFolderId != null && ids.Contains(d.SecondLevelFolderId.Value))
				|| (d.ThirdLevelFolderId != null && ids.Contains(d.ThirdLevelFolderId.Value)), cn);
		}

		private async Task<List<long>> GetDescendantIds(long parentId, CancellationToken cn)
		{
			var result = new List<long>();
			var frontier = new List<long> { parentId };
			while (frontier.Count > 0)
			{
				var children = await unitOfWork.Repository<DocumentFolder>().TableNoTracking
					.Where(c => c.ParentId != null && frontier.Contains(c.ParentId.Value) && c.Id != null)
					.Select(c => c.Id!.Value)
					.ToListAsync(cn);
				result.AddRange(children);
				frontier = children;
			}

			return result;
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

	public class DocumentFolderChildVm
	{
		public long? Id { get; set; }
		public long? ParentId { get; set; }
		public string? Title { get; set; }
		public string? FolderTitle { get; set; }
		public string? Comment { get; set; }
		public int FolderLevel { get; set; }
		public List<DocumentFolderChildVm> Children { get; set; } = [];
	}
}

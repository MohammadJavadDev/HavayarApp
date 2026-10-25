using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Trn;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مشتریان حقوقی (شرکت‌ها)", typeof(Company))]
	public class CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public Task<IActionResult> Save(Company company, CancellationToken cn)
			=> ApplyAndSave(company, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(Company company, CancellationToken cn)
			=> ApplyAndSave(company, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(Company company, CancellationToken cn)
			=> ApplyAndSave(company, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Company>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Company>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = LoadCompany(id.Value);
				if (entity == null)
					return NotFound();
				return View(@"\Views\Panel\Trn\Company\Edit.cshtml", entity);
			}
			return View(@"\Views\Panel\Trn\Company\Edit.cshtml", new Company());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Trn\Company\Edit.cshtml", new Company());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\Company\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Company>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Company>().FetchDataAsync(request, cn));
		}

		private async Task<IActionResult> ApplyAndSave(Company company, CancellationToken cn)
		{
			var phoneBooks = company.PhoneBooks?.ToList() ?? new List<PhoneBook>();
			var keyParticipants = company.KeyParticipants?.ToList() ?? new List<KeyParticipant>();
			var connectors = company.ConnectorParticipants?.ToList() ?? new List<ConnectorParticipant>();
			var todoLists = company.TodoLists?.ToList() ?? new List<TodoList>();

			var validationError = ValidateChildren(keyParticipants, connectors, todoLists);
			if (validationError != null)
				return BadRequest(validationError);

			var repo = unitOfWork.Repository<Company>();
			var isNew = company.Id == null || company.Id == 0
				|| !await repo.TableNoTracking.AnyAsync(c => c.Id == company.Id, cn);
			Company? stored = null;
			if (!isNew)
				stored = await repo.TableNoTracking.FirstOrDefaultAsync(c => c.Id == company.Id, cn);

			if (stored != null)
			{
				company.Fax = stored.Fax;
				company.CeoName = stored.CeoName;
				company.Website = stored.Website;
				company.ActivityKind = stored.ActivityKind;
				company.Description = stored.Description;
				company.IsActive = stored.IsActive;
			}
			else
			{
				company.Fax = null;
				company.CeoName = null;
				company.Website = null;
				company.ActivityKind = null;
				company.Description = null;
			}

			if (company.PartyId is long partyId && partyId > 0)
			{
				if (isNew && await repo.TableNoTracking.AnyAsync(c => c.PartyId == partyId, cn))
					return BadRequest("امکان ثبت اطلاعات تکراری وجود ندارد");

				var party = await unitOfWork.Repository<Party>().TableNoTracking
					.FirstOrDefaultAsync(p => p.Id == partyId, cn);
				if (party == null)
					return BadRequest("طرف حساب انتخاب‌شده یافت نشد");

				var title = string.IsNullOrWhiteSpace(party.CompanyName) ? party.FullName : party.CompanyName;
				company.Title = Truncate(title, 256);
				company.Phone = Truncate(party.Phone, 50);
				company.Email = Truncate(party.Email, 256);
				company.Address = party.Address;
			}
			else if (stored != null)
			{
				company.Title = stored.Title;
				company.Phone = stored.Phone;
				company.Email = stored.Email;
				company.Address = stored.Address;
			}
			else
			{
				company.Title = null;
				company.Phone = null;
				company.Email = null;
				company.Address = null;
			}

			company.TitleInLatin = Truncate(company.TitleInLatin, 128);
			company.Party = null;
			company.PhoneBooks = null!;
			company.KeyParticipants = null!;
			company.ConnectorParticipants = null!;
			company.TodoLists = null!;

			var saved = isNew
				? await repo.SaveAsync(company, cn, true)
				: await repo.UpdateAsync(company, cn, true);
			var companyId = saved.Id ?? company.Id ?? 0;

			var now = DateTime.Now;
			var shamsi = now.ToShamsiDate();
			foreach (var item in todoLists)
			{
				item.TodoMiladiDate = now;
				item.TodoShamsiDate = shamsi;
			}

			await SyncPhoneBooks(companyId, phoneBooks, cn);
			await SyncKeyParticipants(companyId, keyParticipants, cn);
			await SyncConnectors(companyId, connectors, cn);
			await SyncTodoLists(companyId, todoLists, cn);

			var loaded = LoadCompany(companyId);
			return Ok(ToFormResult(loaded!));
		}

		private Company? LoadCompany(long id)
		{
			return unitOfWork.Repository<Company>().TableNoTracking
				.Include(c => c.Party)
				.Include(c => c.PhoneBooks)
				.Include(c => c.KeyParticipants)
				.Include(c => c.ConnectorParticipants)
				.Include(c => c.TodoLists)
				.FirstOrDefault(c => c.Id == id);
		}

		private static CompanyFormResult ToFormResult(Company company)
		{
			return new CompanyFormResult
			{
				Id = company.Id,
				PartyId = company.PartyId,
				Title = company.Title,
				TitleInLatin = company.TitleInLatin,
				Phone = company.Phone,
				Fax = company.Fax,
				Email = company.Email,
				Address = company.Address,
				CreatedByName = company.CreatedByName,
				CreatedOnShamsiDateTime = company.CreatedOnShamsiDateTime,
				ModifiedByName = company.ModifiedByName,
				ModifiedDateShamsiDateTime = company.ModifiedDateShamsiDateTime,
				PhoneBooks = company.PhoneBooks
					.OrderBy(p => p.Id)
					.Select(p => new PhoneBookRow
					{
						Id = p.Id,
						DepartmentTitle = p.DepartmentTitle,
						PhoneNumber = p.PhoneNumber,
						FaxNumber = p.FaxNumber,
						CreatedByName = p.CreatedByName,
						CreatedOnShamsiDateTime = p.CreatedOnShamsiDateTime,
						IsActive = p.IsActive
					}).ToList(),
				KeyParticipants = company.KeyParticipants
					.OrderBy(p => p.Id)
					.Select(p => new KeyParticipantRow
					{
						Id = p.Id,
						FirstName = p.FirstName,
						LastName = p.LastName,
						Position = p.Position,
						DirectPhoneNumber = p.DirectPhoneNumber,
						PhoneNumber = p.PhoneNumber,
						MobileNumber = p.MobileNumber,
						FaxNumber = p.FaxNumber,
						ConfirmedFaxNumber = p.ConfirmedFaxNumber,
						Email = p.Email,
						CreatedByName = p.CreatedByName,
						CreatedOnShamsiDateTime = p.CreatedOnShamsiDateTime,
						IsActive = p.IsActive
					}).ToList(),
				ConnectorParticipants = company.ConnectorParticipants
					.OrderBy(p => p.Id)
					.Select(p => new ConnectorParticipantRow
					{
						Id = p.Id,
						FirstName = p.FirstName,
						LastName = p.LastName,
						Position = p.Position,
						DirectPhoneNumber = p.DirectPhoneNumber,
						FaxNumber = p.FaxNumber,
						ConfirmedFaxNumber = p.ConfirmedFaxNumber,
						MobileNumber = p.MobileNumber,
						Email = p.Email,
						Description = p.Description,
						CreatedByName = p.CreatedByName,
						CreatedOnShamsiDateTime = p.CreatedOnShamsiDateTime,
						IsActive = p.IsActive
					}).ToList(),
				TodoLists = company.TodoLists
					.OrderBy(p => p.Id)
					.Select(p => new TodoListRow
					{
						Id = p.Id,
						Title = p.Title,
						Description = p.Description,
						IsDone = p.IsDone,
						CreatedByName = p.CreatedByName,
						CreatedOnShamsiDateTime = p.CreatedOnShamsiDateTime,
						CreatedOnMiladiDateTime = p.CreatedOnMiladiDateTime,
						IsActive = p.IsActive
					}).ToList()
			};
		}

		private static string? ValidateChildren(
			List<KeyParticipant> keyParticipants,
			List<ConnectorParticipant> connectors,
			List<TodoList> todoLists)
		{
			foreach (var item in keyParticipants)
			{
				var error = RequireContact(item.Position, item.DirectPhoneNumber, item.MobileNumber);
				if (error != null)
					return error;
			}

			foreach (var item in connectors)
			{
				var error = RequireContact(item.Position, item.DirectPhoneNumber, item.MobileNumber);
				if (error != null)
					return error;
			}

			foreach (var item in todoLists)
			{
				if (string.IsNullOrWhiteSpace(item.Title))
					return "(عنوان) نمیتواند خالی باشد";
				if (!string.IsNullOrEmpty(item.Description) && item.Description.Contains("09"))
					return "مجاز به وارد کردن شماره تلفن همراه در فیلد توضیحات نیستید!";
			}

			return null;
		}

		private static string? RequireContact(string? position, string? directPhone, string? mobile)
		{
			if (string.IsNullOrWhiteSpace(position))
				return "(سمت) نمیتواند خالی باشد";
			if (string.IsNullOrWhiteSpace(directPhone))
				return "(تلفن مستقیم) نمیتواند خالی باشد";
			if (string.IsNullOrWhiteSpace(mobile))
				return "(شماره موبایل) نمیتواند خالی باشد";
			return null;
		}

		private async Task SyncPhoneBooks(long companyId, List<PhoneBook> incoming, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<PhoneBook>();
			Normalize(incoming, p =>
			{
				p.DepartmentTitle = Truncate(p.DepartmentTitle, 256);
				p.PhoneNumber = Truncate(p.PhoneNumber, 50);
				p.FaxNumber = Truncate(p.FaxNumber, 50);
			});
			await DeleteMissing(repo, incoming.Select(p => p.Id), c => c.CompanyId == companyId, cn);
			foreach (var item in incoming)
			{
				item.CompanyId = companyId;
				item.Company = null;
				await SaveChild(repo, item, cn);
			}
		}

		private async Task SyncKeyParticipants(long companyId, List<KeyParticipant> incoming, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<KeyParticipant>();
			Normalize(incoming, p =>
			{
				p.FirstName = Truncate(p.FirstName, 50);
				p.LastName = Truncate(p.LastName, 50);
				p.Position = Truncate(p.Position, 128);
				p.DirectPhoneNumber = Truncate(p.DirectPhoneNumber, 50);
				p.PhoneNumber = Truncate(p.PhoneNumber, 50);
				p.MobileNumber = Truncate(p.MobileNumber, 50);
				p.FaxNumber = Truncate(p.FaxNumber, 50);
				p.ConfirmedFaxNumber = Truncate(p.ConfirmedFaxNumber, 50);
				p.Email = Truncate(p.Email, 50);
			});
			await DeleteMissing(repo, incoming.Select(p => p.Id), c => c.CompanyId == companyId, cn);
			foreach (var item in incoming)
			{
				item.CompanyId = companyId;
				item.Company = null;
				await SaveChild(repo, item, cn);
			}
		}

		private async Task SyncConnectors(long companyId, List<ConnectorParticipant> incoming, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<ConnectorParticipant>();
			Normalize(incoming, p =>
			{
				p.FirstName = Truncate(p.FirstName, 50);
				p.LastName = Truncate(p.LastName, 50);
				p.Position = Truncate(p.Position, 128);
				p.DirectPhoneNumber = Truncate(p.DirectPhoneNumber, 50);
				p.FaxNumber = Truncate(p.FaxNumber, 50);
				p.ConfirmedFaxNumber = Truncate(p.ConfirmedFaxNumber, 50);
				p.MobileNumber = Truncate(p.MobileNumber, 50);
				p.Email = Truncate(p.Email, 50);
				p.Description = Truncate(p.Description, 4000);
			});
			await DeleteMissing(repo, incoming.Select(p => p.Id), c => c.CompanyId == companyId, cn);
			foreach (var item in incoming)
			{
				item.CompanyId = companyId;
				item.Company = null;
				await SaveChild(repo, item, cn);
			}
		}

		private async Task SyncTodoLists(long companyId, List<TodoList> incoming, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<TodoList>();
			Normalize(incoming, p =>
			{
				p.Title = Truncate(p.Title, 256);
			});
			await DeleteMissing(repo, incoming.Select(p => p.Id), c => c.CompanyId == companyId, cn);
			foreach (var item in incoming)
			{
				item.CompanyId = companyId;
				item.Company = null;
				await SaveChild(repo, item, cn);
			}
		}

		private static void Normalize<T>(List<T> items, Action<T> apply)
		{
			foreach (var item in items)
				apply(item);
		}

		private static async Task DeleteMissing<T>(
			IRepository<T> repo,
			IEnumerable<long?> incomingIds,
			System.Linq.Expressions.Expression<Func<T, bool>> ofCompany,
			CancellationToken cn) where T : BaseEntity, new()
		{
			var keep = incomingIds.Where(id => id != null && id > 0).Select(id => id!.Value).ToList();
			var existingIds = await repo.TableNoTracking.Where(ofCompany).Select(c => c.Id).ToListAsync(cn);
			var remove = existingIds.Where(id => id != null && !keep.Contains(id.Value)).Select(id => id!.Value).ToList();
			if (remove.Count == 0)
				return;
			await repo.DeleteWhereAsync(c => c.Id != null && remove.Contains(c.Id.Value), cn);
		}

		private static async Task SaveChild<T>(IRepository<T> repo, T item, CancellationToken cn) where T : BaseEntity, new()
		{
			if (item.Id == null || item.Id == 0)
			{
				item.Id = null;
				await repo.SaveAsync(item, cn, true, true, false);
				return;
			}

			var stored = await repo.TableNoTracking.FirstOrDefaultAsync(c => c.Id == item.Id, cn);
			if (stored == null)
			{
				item.Id = null;
				await repo.SaveAsync(item, cn, true, true, false);
				return;
			}

			item.IsActive = stored.IsActive;
			await repo.UpdateAsync(item, cn, true, true, false);
		}

		private static string? Truncate(string? value, int max)
		{
			if (string.IsNullOrEmpty(value))
				return value;
			var trimmed = value.Trim();
			return trimmed.Length <= max ? trimmed : trimmed[..max];
		}
	}

	public class CompanyFormResult
	{
		public long? Id { get; set; }
		public long? PartyId { get; set; }
		public string? Title { get; set; }
		public string? TitleInLatin { get; set; }
		public string? Phone { get; set; }
		public string? Fax { get; set; }
		public string? Email { get; set; }
		public string? Address { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
		public string? ModifiedByName { get; set; }
		public string? ModifiedDateShamsiDateTime { get; set; }
		public List<PhoneBookRow> PhoneBooks { get; set; } = new();
		public List<KeyParticipantRow> KeyParticipants { get; set; } = new();
		public List<ConnectorParticipantRow> ConnectorParticipants { get; set; } = new();
		public List<TodoListRow> TodoLists { get; set; } = new();
	}

	public class PhoneBookRow
	{
		public long? Id { get; set; }
		public string? DepartmentTitle { get; set; }
		public string? PhoneNumber { get; set; }
		public string? FaxNumber { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
		public IsActiveEnum? IsActive { get; set; }
	}

	public class KeyParticipantRow
	{
		public long? Id { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Position { get; set; }
		public string? DirectPhoneNumber { get; set; }
		public string? PhoneNumber { get; set; }
		public string? MobileNumber { get; set; }
		public string? FaxNumber { get; set; }
		public string? ConfirmedFaxNumber { get; set; }
		public string? Email { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
		public IsActiveEnum? IsActive { get; set; }
	}

	public class ConnectorParticipantRow
	{
		public long? Id { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Position { get; set; }
		public string? DirectPhoneNumber { get; set; }
		public string? FaxNumber { get; set; }
		public string? ConfirmedFaxNumber { get; set; }
		public string? MobileNumber { get; set; }
		public string? Email { get; set; }
		public string? Description { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
		public IsActiveEnum? IsActive { get; set; }
	}

	public class TodoListRow
	{
		public long? Id { get; set; }
		public string? Title { get; set; }
		public string? Description { get; set; }
		public bool IsDone { get; set; }
		public string? CreatedByName { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
		public DateTime? CreatedOnMiladiDateTime { get; set; }
		public IsActiveEnum? IsActive { get; set; }
	}
}

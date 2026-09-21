using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ماموریت خدمات پس از فروش", typeof(Mission))]
	public class MissionController(IUnitOfWork unitOfWork, IWebHostEnvironment env, ApplicationDbContext db) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Mission model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<Mission>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Mission model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Mission>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Mission model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Mission>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<Mission>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<Mission>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? serviceRequestId = null)
		{
			Mission entity;
			if (id != null && id != 0)
				entity = unitOfWork.Repository<Mission>().TableNoTracking.FirstOrDefault(c => c.Id == id)
					?? new Mission { ServiceRequestId = serviceRequestId };
			else
				entity = new Mission { ServiceRequestId = serviceRequestId };

			ViewBag.PersonelIds = GetAssignedPersonelIds(serviceRequestId ?? entity.ServiceRequestId);

			return View(@"\Views\Panel\Sale\Mission\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId = null)
		{
			ViewBag.PersonelIds = GetAssignedPersonelIds(serviceRequestId);
			return View(@"\Views\Panel\Sale\Mission\Edit.cshtml", new Mission { ServiceRequestId = serviceRequestId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\Mission\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long serviceRequestId)
		{
			ViewBag.ParentId = serviceRequestId;
			return View(@"\Views\Panel\Sale\Mission\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? serviceRequestId, CancellationToken cn)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<Mission>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => new
				{
					c.Id,
					c.ServiceRequestId,
					FullName = (c.Personel.Name ?? "") != "" || (c.Personel.Family ?? "") != ""
						? ((c.Personel.Name ?? "") + " " + (c.Personel.Family ?? "")).Trim()
						: (c.Expert != null ? (c.Expert.NameFa ?? c.Expert.Name) : null),
					CostCenterTitle = c.CostCenter != null ? c.CostCenter.Title : null,
					TotalCost = (c.BreakfastFee ?? 0)
						+ (c.LunchFee ?? 0)
						+ (c.DinnerFee ?? 0)
						+ (c.NightRight ?? 0)
						+ (c.WorkOvertimeFee ?? 0)
						+ (c.MissionRight ?? 0)
						+ (c.MissionHolidayRight ?? 0)
						+ (c.TransportationPersonal ?? 0)
						+ (c.TransportationDriving ?? 0),
					c.HaveGuarantee,
					c.HokmNumber,
					c.DispatchShamsiDate,
					c.DispatchTime,
					c.ReturnShamsiDate,
					c.ReturnTime,
					c.BreakfastFee,
					c.Breakfast,
					c.LunchFee,
					c.Lunch,
					c.DinnerFee,
					c.Dinner,
					c.NightRight,
					c.NightRightFactor,
					c.WorkOvertimeFee,
					c.WorkOvertimeHour,
					c.MissionRight,
					c.MissionHolidayRightFactor,
					c.MissionHolidayRight,
					c.TransportationPersonal,
					c.TransportationDriving
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت حق ماموریت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetPersonelMissionSalaryInfo(long? personelId, long? expertId, CancellationToken cn)
		{
			if (personelId is not > 0 && expertId is not > 0)
				return BadRequest("هیچ کارشناسی انتخاب نشده است");

			var salary = await FindMissionSalaryAsync(personelId, expertId, cn);
			if (salary == null)
				return Ok((object?)null);

			return Ok(new
			{
				salary.BreakfastFee,
				salary.LunchFee,
				salary.DinnerFee,
				salary.NightRight,
				salary.WorkOvertimeFee,
				salary.MissionRight,
				salary.MissionHolidayRight
			});
		}

		[HttpGet("[action]")]
 
		public async Task<IActionResult> GetMissionInfo(
			long? personelId,
			long? expertId,
			string? dispatchShamsiDate,
			string? dispatchTime,
			string? returnShamsiDate,
			string? returnTime,
			CancellationToken cn)
		{
			if (!TryParseMissionDateTime(dispatchShamsiDate, dispatchTime, out var from)
				|| !TryParseMissionDateTime(returnShamsiDate, returnTime, out var to))
				return BadRequest("تاریخ و ساعت اعزام و بازگشت را کامل وارد کنید");
			if (to < from)
				return BadRequest("بازه تاریخ ماموریت نامعتبر است");

			var calc = CalculateMissionAllowances(from, to);
			var salary = await FindMissionSalaryAsync(personelId, expertId, cn);

			return Ok(new
			{
				calc.Breakfast,
				calc.Lunch,
				calc.Dinner,
				calc.NightRightFactor,
				calc.MissionRightFactor,
				calc.MissionDays,
				BreakfastFee = salary?.BreakfastFee,
				LunchFee = salary?.LunchFee,
				DinnerFee = salary?.DinnerFee,
				NightRight = salary?.NightRight,
				WorkOvertimeFee = salary?.WorkOvertimeFee,
				MissionRight = salary?.MissionRight,
				MissionHolidayRight = salary?.MissionHolidayRight
			});
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Mission>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Mission>().FetchDataAsync(request, cn));

		private List<long?> GetAssignedPersonelIds(long? serviceRequestId)
		{
			if (serviceRequestId is not > 0)
				return [];
			return unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => c.PersonelId)
				.Distinct()
				.ToList();
		}

		private async Task<MissionSalary?> FindMissionSalaryAsync(long? personelId, long? expertId, CancellationToken cn)
		{
			var salaries = unitOfWork.Repository<MissionSalary>().TableNoTracking;
			MissionSalary? salary = null;
			if (personelId is > 0)
				salary = await salaries.FirstOrDefaultAsync(c => c.PersonelId == personelId, cn);
			if (salary == null && expertId is > 0)
				salary = await salaries.FirstOrDefaultAsync(c => c.ExpertId == expertId, cn);
			if (salary == null && personelId is > 0)
			{
				var partyId = await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(c => c.Id == personelId)
					.Select(c => c.PartyId)
					.FirstOrDefaultAsync(cn);
				if (partyId is > 0)
				{
					var userIds = await db.Users.AsNoTracking()
						.Where(c => c.PartyId == partyId)
						.Select(c => c.Id)
						.ToListAsync(cn);
					if (userIds.Count > 0)
						salary = await salaries.FirstOrDefaultAsync(c => c.ExpertId != null && userIds.Contains(c.ExpertId.Value), cn);
				}
			}
			if (salary == null && expertId is > 0)
			{
				var user = await db.Users.AsNoTracking()
					.Select(c => new { c.Id, c.PartyId, c.HamkaranId })
					.FirstOrDefaultAsync(c => c.Id == expertId, cn);
				if (user?.PartyId != null)
				{
					var fromParty = await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(c => c.PartyId == user.PartyId)
						.Select(c => c.Id)
						.FirstOrDefaultAsync(cn);
					if (fromParty is > 0)
						salary = await salaries.FirstOrDefaultAsync(c => c.PersonelId == fromParty, cn);
				}
				if (salary == null && user?.HamkaranId != null)
				{
					var fromHamkaran = await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(c => c.HamkaranId == user.HamkaranId)
						.Select(c => c.Id)
						.FirstOrDefaultAsync(cn);
					if (fromHamkaran is > 0)
						salary = await salaries.FirstOrDefaultAsync(c => c.PersonelId == fromHamkaran, cn);
				}
			}
			return salary;
		}

		private static bool TryParseMissionDateTime(string? shamsiDate, string? time, out DateTime result)
		{
			result = default;
			if (string.IsNullOrWhiteSpace(shamsiDate) || string.IsNullOrWhiteSpace(time) || time.Contains('_'))
				return false;
			try
			{
				var date = shamsiDate.ToMiladiDate().Date;
				var parts = time.Trim().Split(':');
				if (parts.Length < 2)
					return false;
				if (!int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
					return false;
				if (hour is < 0 or > 23 || minute is < 0 or > 59)
					return false;
				result = date.AddHours(hour).AddMinutes(minute);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// HTS SaleServiceRequestManagementController.GetMissionInfo:
		/// breakfast before 07:00, lunch 12–14, dinner after 20:30,
		/// one mission-right day per calendar span (+1 extra day if departure is after 12:00).
		/// Night-right factor is the number of midnights crossed.
		/// </summary>
		internal static MissionAllowanceCalc CalculateMissionAllowances(DateTime from, DateTime to)
		{
			var missionDays = Math.Ceiling(to.Subtract(from).TotalDays);
			var breakfast = 0;
			var lunch = 0;
			var dinner = 0;

			if (missionDays <= 1)
			{
				if (from.Hour <= 7)
					breakfast += 1;
				if ((from.Hour > 7 && from.Hour <= 14) || to.Hour >= 12)
					lunch += 1;
				if (IsDinnerOrLater(to))
					dinner += 1;
				if (from.Hour > 12)
					missionDays += 1;
			}
			else if (missionDays <= 2)
			{
				if (from.Hour <= 7)
				{
					breakfast += 1;
					lunch += 1;
					dinner += 1;
				}
				else
				{
					if (from.Hour <= 14)
						lunch += 1;
					if (IsAtOrBeforeDinnerCutoff(from))
						dinner += 1;
				}

				breakfast += 1;
				if (to.Hour >= 12)
					lunch += 1;
				if (IsDinnerOrLater(to))
					dinner += 1;
				if (from.Hour > 12)
					missionDays += 1;
			}
			else
			{
				var eatCount = Convert.ToInt32(missionDays - 2);
				breakfast += eatCount + 1;
				lunch += eatCount;
				dinner += eatCount;

				if (from.Hour <= 7)
				{
					breakfast += 1;
					lunch += 1;
					dinner += 1;
				}
				else
				{
					if (from.Hour <= 14)
						lunch += 1;
					if (IsAtOrBeforeDinnerCutoff(from))
						dinner += 1;
				}

				if (to.Hour >= 12)
					lunch += 1;
				if (IsDinnerOrLater(to))
					dinner += 1;
				if (from.Hour > 12)
					missionDays += 1;
			}

			var nights = Math.Max(0, (to.Date - from.Date).Days);
			return new MissionAllowanceCalc(
				ToByteCount(breakfast),
				ToByteCount(lunch),
				ToByteCount(dinner),
				ToByteCount(nights),
				ToByteCount(missionDays),
				missionDays);
		}

		private static bool IsDinnerOrLater(DateTime dt)
			=> dt.Hour > 20 || (dt.Hour == 20 && dt.Minute >= 30);

		private static bool IsAtOrBeforeDinnerCutoff(DateTime dt)
			=> dt.Hour < 20 || (dt.Hour == 20 && dt.Minute <= 30);

		private static byte ToByteCount(double value)
			=> (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);

		internal readonly record struct MissionAllowanceCalc(
			byte Breakfast,
			byte Lunch,
			byte Dinner,
			byte NightRightFactor,
			byte MissionRightFactor,
			double MissionDays);
	}
}

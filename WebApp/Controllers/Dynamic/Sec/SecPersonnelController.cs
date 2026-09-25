using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sec;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sec/Personnel")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدیریت پرسنل دبیرخانه", typeof(Personnel))]
	public class SecPersonnelController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private const int ReduceImageKbSize = 200;

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Personnel model, CancellationToken cn)
		{
			ApplyPictureChanges(model);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);

			if (await unitOfWork.Repository<Personnel>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);

			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Personnel model, CancellationToken cn)
		{
			ApplyPictureChanges(model);
			model.IsActive ??= IsActiveEnum.Active;
			var entity = await unitOfWork.Repository<Personnel>().SaveAsync(model, cn, true);
			return Ok(StripBinaryForClient(entity));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Personnel model, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<Personnel>().Table
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (existing == null)
				return BadRequest("رکورد یافت نشد");

			existing.PersonelId = model.PersonelId;
			existing.NameDisplay = model.NameDisplay;
			existing.FamilyDisplay = model.FamilyDisplay;
			existing.BirthDate = model.BirthDate;
			existing.EmploymentDate = model.EmploymentDate;
			existing.ForceToSend = model.ForceToSend;
			existing.ReciversGroupId = model.ReciversGroupId;
			existing.IsActive = model.IsActive ?? existing.IsActive;

			if (model.RemovePicture)
				existing.Picture = null;
			else if (!string.IsNullOrWhiteSpace(model.PictureBase64))
				existing.Picture = DecodeAndResize(model.PictureBase64);

			if (model.RemoveSecondPicture)
				existing.SecondPicture = null;
			else if (!string.IsNullOrWhiteSpace(model.SecondPictureBase64))
				existing.SecondPicture = DecodeAndResize(model.SecondPictureBase64);

			var entity = await unitOfWork.Repository<Personnel>().UpdateAsync(existing, cn, true);
			return Ok(StripBinaryForClient(entity));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Personnel>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Personnel>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			Personnel entity;
			if (id != null && id != 0)
			{
				entity = unitOfWork.Repository<Personnel>().TableNoTracking
					.Include(c => c.Personel)
					.Include(c => c.ReciversGroup)
					.FirstOrDefault(c => c.Id == id) ?? new Personnel();
			}
			else
			{
				entity = new Personnel { IsActive = IsActiveEnum.Active };
			}

			return View(@"\Views\Panel\Sec\Personnel\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Sec\Personnel\Edit.cshtml", new Personnel { IsActive = IsActiveEnum.Active });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sec\Personnel\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Personnel>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Personnel>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		public async Task<IActionResult> GetPicture(long id, CancellationToken cn)
		{
			var pic = await unitOfWork.Repository<Personnel>().TableNoTracking
				.Where(c => c.Id == id)
				.Select(c => c.Picture)
				.FirstOrDefaultAsync(cn);
			if (pic == null || pic.Length == 0)
				return NotFound();
			return File(pic, "image/jpeg");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetSecondPicture(long id, CancellationToken cn)
		{
			var pic = await unitOfWork.Repository<Personnel>().TableNoTracking
				.Where(c => c.Id == id)
				.Select(c => c.SecondPicture)
				.FirstOrDefaultAsync(cn);
			if (pic == null || pic.Length == 0)
				return NotFound();
			return File(pic, "image/jpeg");
		}

		private static void ApplyPictureChanges(Personnel model)
		{
			if (model.RemovePicture)
				model.Picture = null;
			else if (!string.IsNullOrWhiteSpace(model.PictureBase64))
				model.Picture = DecodeAndResize(model.PictureBase64);

			if (model.RemoveSecondPicture)
				model.SecondPicture = null;
			else if (!string.IsNullOrWhiteSpace(model.SecondPictureBase64))
				model.SecondPicture = DecodeAndResize(model.SecondPictureBase64);
		}

		private static Personnel StripBinaryForClient(Personnel entity)
		{
			entity.Picture = null;
			entity.SecondPicture = null;
			entity.PictureBase64 = null;
			entity.SecondPictureBase64 = null;
			return entity;
		}

		private static byte[]? DecodeAndResize(string base64)
		{
			var raw = base64.Contains(',') ? base64[(base64.IndexOf(',') + 1)..] : base64;
			var bytes = Convert.FromBase64String(raw);
			return ResizetoKb(bytes, ReduceImageKbSize);
		}

		/// <summary>معادل HTS ResizetoKb — هدف حدود <paramref name="lengthKb"/> کیلوبایت.</summary>
		private static byte[]? ResizetoKb(byte[]? byteImageIn, int lengthKb)
		{
			if (byteImageIn == null || byteImageIn.Length == 0)
				return null;

			var maxBytes = lengthKb * 1000;
			if (byteImageIn.Length <= maxBytes)
				return byteImageIn;

			try
			{
				using var image = Image.Load(byteImageIn);
				var quality = 90;
				byte[] current = byteImageIn;

				while (current.Length > maxBytes && quality > 20)
				{
					using var ms = new MemoryStream();
					var encoder = new JpegEncoder { Quality = quality };
					if (current.Length > maxBytes * 2)
					{
						image.Mutate(x => x.Resize(new ResizeOptions
						{
							Mode = ResizeMode.Max,
							Size = new Size((int)(image.Width * 0.85), (int)(image.Height * 0.85))
						}));
					}
					image.Save(ms, encoder);
					current = ms.ToArray();
					quality -= 10;
				}

				return current;
			}
			catch
			{
				return byteImageIn;
			}
		}
	}
}

using Common.Attributes;
using Entities.Base;
using Entities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Services.FileServices;
using WebFramework.Filtters;

namespace WebApp.Controllers.SystemControllers
{
	[Route("[controller]")]
	[ApiResultFilter]
	[Authorize("AuthenticatedUser")]
	public class FileController(IFileService _fileService,
		IWebHostEnvironment _webHostEnvironment,
		IEntityMetadataCache entityMetadataCache,
		IWebHostEnvironment env,
		    IConfiguration config) : ControllerBase
	{
 
		[HttpPost("upload/{entityType}/{entityPropName}")]
		public async Task<IActionResult> Upload(IFormFile file, string entityType, string entityPropName, long? entityId, CancellationToken ct)
		{

			var entity = entityMetadataCache.Get(entityType);

			if (entity is null)
			{
				throw new Exception("نوع موجودیت یافت نشد.");
			}

			var propEn = entity.Properties.FirstOrDefault(c => c.Name == entityPropName);

			if (propEn is null)
			{
				throw new Exception("فیلد موجودیت یافت نشد.");
			}

			// بررسی نوع فایل
			if (!string.IsNullOrWhiteSpace(propEn.FileTypes))
			{
				var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
				var allowedExtensions = propEn.FileTypes
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(ext => ext.TrimStart('*').ToLowerInvariant())
					.ToList();

				if (!allowedExtensions.Contains(fileExtension))
				{
					throw new Exception($"نوع فایل مجاز نیست. انواع مجاز: {propEn.FileTypes}");
				}
			}

			// بررسی سایز فایل
			if (propEn.MaxFileSize > 0)
			{
				var fileSizeInMB = file.Length / (1024.0 * 1024.0);
				if (fileSizeInMB > propEn.MaxFileSize)
				{
					throw new Exception($"سایز فایل بیشتر از حد مجاز است. حداکثر سایز مجاز: {propEn.MaxFileSize} مگابایت");
				}
			}

			var fe = await _fileService.UploadAsync(file, entityType, entityPropName, entityId, ct);
			return Ok(new { fileId = fe.Id, path = fe.PhysicalPath });
		}


		[HttpDelete("file/{id}")]
		public async Task<IActionResult> DeleteFile(long id, CancellationToken ct)
		{
			await _fileService.DeleteAsync(id, ct);
			return NoContent();
		}

		[HttpGet("file/{id}")]

		public async Task<IActionResult> GetFile(long id, CancellationToken ct)
		{
			var file = await _fileService.GetAsync(id, ct);
			if (file == null)
				return NotFound();

			return Ok(new
			{
				id = file.Id,

				originalName = file.OriginalName,
				contentType = file.ContentType,
				size = file.Size,

			});
		}

		[HttpGet("download/{id}")]
		public async Task<IActionResult> DownloadFile(long id, CancellationToken ct)
		{
			 

			var (stream, contentType, fileName) = await _fileService.DownloadAsync(id, ct);
			return File(stream, contentType, fileName);
		}

		[HttpGet("profile-image/{filename}")]
		public IActionResult GetProfileImage(string filename)
		{

			var uploadsPath = config["Storage:ProfileImagesPath"]
					  ?? throw new Exception("Storage:ProfileImagesPath not configured");

			var _uploadsRoot = Path.IsPathRooted(uploadsPath)
				    ? uploadsPath
				    : Path.Combine(env.ContentRootPath, uploadsPath);

			var path = Path.Combine(_uploadsRoot, filename);
			if (!System.IO.File.Exists(path)) return NotFound();

			var ext = Path.GetExtension(filename);
			var contentType = ext switch
			{
				".jpg" => "image/jpeg",
				".png" => "image/png",
				_ => "application/octet-stream"
			};

			var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return File(stream, contentType);
		}

		[HttpPost("AddDataToFile")]

		public async Task<IActionResult> AddDataToFile(long fileId, string entityType, string entityPropName, long? entityId, CancellationToken ct)
		{
			//await _fileService.AddDataToFile(fileId, entityId, entityType, entityPropName, ct);
			return Ok();
		}
	}
}

using Data.Contracts;
using Entities.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Services.FileServices;

public class FileService : IFileService
{
	private readonly string _uploadsRoot;
	private readonly IUnitOfWork _unitOfWork;

	public FileService(
	    
	    IConfiguration config,
	    IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;

		var uploadsPath = config["Storage:UploadsPath"]
		    ?? throw new Exception("Storage:UploadsPath not configured");

		_uploadsRoot = Path.IsPathRooted(uploadsPath)
		    ? uploadsPath
		    : Path.Combine(uploadsPath);

		Directory.CreateDirectory(_uploadsRoot);
	}

	// ----------------------------------------------------
	// Upload
	// ----------------------------------------------------
	public async Task<FileEntity> UploadAsync(
	    IFormFile file,
	    string? entityType,
	    string? entityPropName,
	    long? entityId,
	    CancellationToken ct)
	{
		if (file == null || file.Length == 0)
			throw new ArgumentException("File is empty");

		var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
		var targetDir = Path.Combine(_uploadsRoot, datePart);
		Directory.CreateDirectory(targetDir);

		var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
		var physicalPath = Path.Combine(targetDir, uniqueName);

		await using (var fs = new FileStream(physicalPath, FileMode.Create))
		{
			await file.CopyToAsync(fs, ct);
		}

		var entity = new FileEntity
		{
			PhysicalPath = Path.Combine(datePart, uniqueName).Replace("\\", "/"),
			OriginalName = file.FileName,
			ContentType = file.ContentType,
			Size = file.Length,
			EntityType = entityType,
			EntityPropName = entityPropName,
			EntityId = entityId,
			IsActive = IsActiveEnum.Active
		};

		await _unitOfWork.Repository<FileEntity>().AddAsync(entity, ct);
		await _unitOfWork.SaveChangesAsync(ct);

		return entity;
	}

	// ----------------------------------------------------
	// Replace
	// ----------------------------------------------------
	public async Task<FileEntity> ReplaceAsync(
	    long? oldFileId,
	    IFormFile newFile,
	    Func<FileEntity, Task> updateReference,
	    CancellationToken ct)
	{
		var newEntity = await UploadAsync(newFile, null, null, null, ct);

		await _unitOfWork.BeginTransactionAsync(ct);
		try
		{
			await updateReference(newEntity);
			await _unitOfWork.CommitTransactionAsync(ct);
		}
		catch
		{
			await _unitOfWork.RollbackTransactionAsync(ct);
			TryDeletePhysical(newEntity.PhysicalPath);
			throw;
		}

		if (oldFileId.HasValue)
		{
			await DeleteAsync(oldFileId.Value, ct);
		}

		return newEntity;
	}

	// ----------------------------------------------------
	// Delete (Soft + Physical)
	// ----------------------------------------------------
	public async Task DeleteAsync(long fileId, CancellationToken ct)
	{
		var file = await _unitOfWork.Repository<FileEntity>()
		    .Table.FirstOrDefaultAsync(x => x.Id == fileId, ct);

		if (file == null) return;

		file.IsActive = IsActiveEnum.Deleted;
		await _unitOfWork.Repository<FileEntity>().UpdateAsync(file, ct, false);
		await _unitOfWork.SaveChangesAsync(ct);

		TryDeletePhysical(file.PhysicalPath);
	}

	public async Task<FileEntity> RegisterExistingAsync(
		string fullPath,
		string originalName,
		string? entityType,
		string? entityPropName,
		long? entityId,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(fullPath))
			throw new ArgumentException("File path is empty");

		var info = new FileInfo(fullPath);
		if (!info.Exists)
			info = new FileInfo(ToLongPath(fullPath));
		if (!info.Exists || info.Length == 0)
			throw new FileNotFoundException("File is missing or empty", fullPath);

		var storedPath = info.FullName;
		if (storedPath.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
			storedPath = @"\\" + storedPath[@"\\?\UNC\".Length..];
		else if (storedPath.StartsWith(@"\\?\", StringComparison.Ordinal))
			storedPath = storedPath[4..];

		var name = string.IsNullOrWhiteSpace(originalName) ? info.Name : originalName.Trim();
		var entity = new FileEntity
		{
			PhysicalPath = storedPath,
			OriginalName = name,
			ContentType = DetectContentType(name),
			Size = info.Length,
			EntityType = entityType,
			EntityPropName = entityPropName,
			EntityId = entityId,
			IsActive = IsActiveEnum.Active
		};

		await _unitOfWork.Repository<FileEntity>().AddAsync(entity, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		return entity;
	}

	// ----------------------------------------------------
	// Get
	// ----------------------------------------------------
	public Task<FileEntity?> GetAsync(long id, CancellationToken ct) =>
	    _unitOfWork.Repository<FileEntity>()
		   .TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, ct);

	// ----------------------------------------------------
	// Physical delete (safe)
	// ----------------------------------------------------
	public string GetFullPhysicalPath(string? physicalPath)
	{
		if (string.IsNullOrWhiteSpace(physicalPath))
			return string.Empty;

		var normalized = physicalPath.Replace('/', Path.DirectorySeparatorChar);
		if (Path.IsPathRooted(normalized))
			return normalized;

		return Path.Combine(_uploadsRoot, normalized);
	}

	private void TryDeletePhysical(string relativePath)
	{
		try
		{
			var fullPath = GetFullPhysicalPath(relativePath);
			if (!IsUnderUploadsRoot(fullPath) || !System.IO.File.Exists(fullPath))
				return;

			System.IO.File.Delete(fullPath);
		}
		catch
		{
			// Log & schedule cleanup
		}
	}

	private bool IsUnderUploadsRoot(string fullPath)
	{
		if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(_uploadsRoot))
			return false;

		var root = Path.GetFullPath(_uploadsRoot)
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			+ Path.DirectorySeparatorChar;
		var candidate = Path.GetFullPath(fullPath);
		return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
	}

	private static string ToLongPath(string path)
	{
		var full = Path.GetFullPath(path);
		if (full.StartsWith(@"\\?\", StringComparison.Ordinal))
			return full;
		if (full.StartsWith(@"\\", StringComparison.Ordinal))
			return @"\\?\UNC\" + full[2..];
		return @"\\?\" + full;
	}

	private static string DetectContentType(string fileName)
	{
		var mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ ".txt", "text/plain" },
			{ ".csv", "text/csv" },
			{ ".json", "application/json" },
			{ ".xml", "application/xml" },
			{ ".html", "text/html" },
			{ ".htm", "text/html" },
			{ ".jpg", "image/jpeg" },
			{ ".jpeg", "image/jpeg" },
			{ ".png", "image/png" },
			{ ".gif", "image/gif" },
			{ ".bmp", "image/bmp" },
			{ ".pdf", "application/pdf" },
			{ ".doc", "application/msword" },
			{ ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
			{ ".xls", "application/vnd.ms-excel" },
			{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
			{ ".ppt", "application/vnd.ms-powerpoint" },
			{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
			{ ".zip", "application/zip" },
			{ ".rar", "application/x-rar-compressed" },
			{ ".dwg", "application/acad" },
		};

		var ext = Path.GetExtension(fileName);
		if (!string.IsNullOrEmpty(ext) && mimeTypes.TryGetValue(ext, out var detectedType))
			return detectedType;

		return "application/octet-stream";
	}
	public async Task<(Stream Stream, string ContentType, string FileName)>
    DownloadAsync(long fileId, CancellationToken ct)
	{
		var file = await _unitOfWork.Repository<FileEntity>()
		    .TableNoTracking.FirstOrDefaultAsync(x => x.Id == fileId, ct);

		if (file == null || file.IsActive != IsActiveEnum.Active)
			throw new FileNotFoundException();

		var fullPath = GetFullPhysicalPath(file.PhysicalPath);
		if (!System.IO.File.Exists(fullPath))
			throw new FileNotFoundException();

		var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

		return (stream, file.ContentType, file.OriginalName);
	}

	public async Task<FileEntity> UploadAsync(
    byte[] file,
    string fileName,
    string? contentType,          // اختیاری
    string? entityType,
    string? entityPropName,
    long? entityId,
    CancellationToken ct)
	{
		if (file == null || file.Length == 0)
			throw new ArgumentException("File is empty");

		var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
		var targetDir = Path.Combine(_uploadsRoot, datePart);
		Directory.CreateDirectory(targetDir);

		var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
		var physicalPath = Path.Combine(targetDir, uniqueName);

		await using (var fs = new FileStream(physicalPath, FileMode.Create))
		{
			await fs.WriteAsync(file, 0, file.Length, ct);
		}

		// حدس زدن contentType بر اساس پسوند فایل
		if (string.IsNullOrWhiteSpace(contentType))
			contentType = DetectContentType(fileName);

		var entity = new FileEntity
		{
			PhysicalPath = Path.Combine(datePart, uniqueName).Replace("\\", "/"),
			OriginalName = fileName,
			ContentType = contentType,
			Size = file.Length,
			EntityType = entityType,
			EntityPropName = entityPropName,
			EntityId = entityId,
			IsActive = IsActiveEnum.Active
		};

		await _unitOfWork.Repository<FileEntity>().AddAsync(entity, ct);
		await _unitOfWork.SaveChangesAsync(ct);

		return entity;
	}


}

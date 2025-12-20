using Data.Contracts;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WebApp.Framework.File;

public class FileService : IFileService
{
	private readonly string _uploadsRoot;
	private readonly IUnitOfWork _unitOfWork;

	public FileService(
	    IWebHostEnvironment env,
	    IConfiguration config,
	    IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;

		var uploadsPath = config["Storage:UploadsPath"]
		    ?? throw new Exception("Storage:UploadsPath not configured");

		_uploadsRoot = Path.IsPathRooted(uploadsPath)
		    ? uploadsPath
		    : Path.Combine(env.ContentRootPath, uploadsPath);

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

	// ----------------------------------------------------
	// Get
	// ----------------------------------------------------
	public Task<FileEntity?> GetAsync(long id, CancellationToken ct) =>
	    _unitOfWork.Repository<FileEntity>()
		   .TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, ct);

	// ----------------------------------------------------
	// Physical delete (safe)
	// ----------------------------------------------------
	private void TryDeletePhysical(string relativePath)
	{
		try
		{
			var fullPath = Path.Combine(_uploadsRoot, relativePath);
			if (System.IO.File.Exists(fullPath))
				System.IO.File.Delete(fullPath);
		}
		catch
		{
			// Log & schedule cleanup
		}
	}
	public async Task<(Stream Stream, string ContentType, string FileName)>
    DownloadAsync(long fileId, CancellationToken ct)
	{
		var file = await _unitOfWork.Repository<FileEntity>()
		    .TableNoTracking.FirstOrDefaultAsync(x => x.Id == fileId, ct);

		if (file == null || file.IsActive != IsActiveEnum.Active)
			throw new FileNotFoundException();

		var fullPath = Path.Combine(_uploadsRoot, file.PhysicalPath);
		if (!System.IO.File.Exists(fullPath))
			throw new FileNotFoundException();

		var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

		return (stream, file.ContentType, file.OriginalName);
	}

}

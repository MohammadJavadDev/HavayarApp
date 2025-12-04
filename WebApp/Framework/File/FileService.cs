using Data.Contracts;
using Data.Repositories;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System;

namespace WebApp.Framework.File;

public class FileService(IWebHostEnvironment _env , IUnitOfWork _unitOfWork) : IFileService
{
 
	private readonly string _baseUploads = Path.Combine(_env.WebRootPath, "uploads"); 
	public Task<IFormFile> DownloadFile(string path, CancellationToken tn)
    {
        throw new NotImplementedException();
    }

	public async Task<FileEntity> UploadAsync(IFormFile file, string? entityType,string entityPropName, long? entityId, CancellationToken ct)
	{
		if (file == null || file.Length == 0) throw new ArgumentException("file empty");

		// validation (size, type) here

		var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
		var dir = Path.Combine(_baseUploads, datePart);
		Directory.CreateDirectory(dir);

		var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
		var physicalPath = Path.Combine(dir, uniqueName);
		// Use stream copy to avoid buffering entire file in memory
		await using (var fs = System.IO.File.Create(physicalPath))
		{
			await file.CopyToAsync(fs, ct);
		}

		var entity = new FileEntity
		{
			PhysicalPath = Path.Join("uploads", datePart, uniqueName).Replace("\\", "/"),
			OriginalName = file.FileName,
			ContentType = file.ContentType,
			Size = file.Length,
			EntityType = entityType,
			EntityPropName = entityPropName,
			EntityId = entityId
		};

		await _unitOfWork.Repository<FileEntity>().AddAsync(entity,ct);
		 
		return entity;
	}
	public async Task<FileEntity> ReplaceAsync(long? oldFileId, IFormFile newFile, Func<FileEntity, Task> updateReference, CancellationToken ct)
	{
		// 1. upload new file
		var newEntity = await UploadAsync(newFile, null, null,null, ct);

		// 2. update reference in DB inside a transaction
		await _unitOfWork.BeginTransactionAsync(ct);
		try
		{
			// call user-provided updateReference to point their entity to newEntity.Id
			await updateReference(newEntity);

			// optionally mark ownership in newEntity if needed
 
			await _unitOfWork.CommitTransactionAsync(ct);
			 
		}
		catch
		{
			await _unitOfWork.RollbackTransactionAsync(ct);
			// cleanup the newly uploaded file because DB update failed
			TryDeletePhysical(newEntity.PhysicalPath);
			throw;
		}

		// 3. delete old file (best-effort). اگر حذف فیزیکی ناموفق بود، لاگ و schedule cleanup کنید
		if (oldFileId.HasValue)
		{
			try
			{
				await DeleteAsync(oldFileId.Value, ct);
			}
			catch (Exception ex)
			{
				// لاگ کنید و در جدول دیگری برای cleanup ثبت کنید
			}
		}

		return newEntity;
	}
	public async Task DeleteAsync(long fileId, CancellationToken ct)
	{
		var f = await _unitOfWork.Repository<FileEntity>().Table.FirstOrDefaultAsync(x => x.Id == fileId, ct);
		if (f == null) return;
		f.IsActive = IsActiveEnum.Deleted;
		// soft delete or hard delete:
		// here hard-delete from DB and fs (be careful with references)
		await _unitOfWork.Repository<FileEntity>().UpdateAsync(f, ct,false);

		 //TryDeletePhysical(f.PhysicalPath);
	}

	private void TryDeletePhysical(string relativePath)
	{
		try
		{
			var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/', '\\'));
			if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
		}
		catch (Exception ex)
		{
			// لاگ و/یا درج در جدول cleanup
		}
	}

	public Task<FileEntity?> GetAsync(long id, CancellationToken ct) =>
	_unitOfWork.Repository<FileEntity>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == id, ct);

	public async Task<string> UploadFile(IFormFile files, string name, string? path,
        CancellationToken cancellationToken)
    {

        path = path ?? "fileUploader";

        var basePath = $"{_env.WebRootPath}/{path}/{DateTime.UtcNow.Date.ToString("dd/MM/yyyy").Replace("/", "")}";
              
        var filePath = $"/{path}/{DateTime.UtcNow.Date.ToString("dd/MM/yyyy").Replace("/", "")}/{name}";


        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        if (files.Length > 0)
        {

            using var stream = global::System.IO.File.Create($"{basePath}/{name}");
            await files.CopyToAsync(stream);
        }

        return filePath;
    }

	 public async Task  AddDataToFile(long fileId, long? entityId, string entityType, string entityPropName, CancellationToken ct)
	{
		var f = await _unitOfWork.Repository<FileEntity>().Table.FirstOrDefaultAsync(x => x.Id == fileId, ct);
		if (f == null) throw new Exception("فایلی با این شناسه یافت نشد");
		
		f.EntityId = entityId;
		f.EntityType = entityType;
		f.EntityPropName = entityPropName;
		await _unitOfWork.SaveChangesAsync(ct);
			
	}
}
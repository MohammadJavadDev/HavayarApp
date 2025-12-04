using Entities.Base;

namespace WebApp.Framework.File;

public interface IFileService
{
    Task<string> UploadFile(IFormFile file, string name, string? path, CancellationToken tn);

    Task<IFormFile> DownloadFile(string path, CancellationToken tn);
	Task<FileEntity> UploadAsync(IFormFile file, string? entityType, string entityPropName, long? entityId, CancellationToken ct);
	Task<FileEntity> ReplaceAsync(long? oldFileId, IFormFile newFile, Func<FileEntity, Task> updateReference, CancellationToken ct);
	Task AddDataToFile(long fileId, long? entityId, string entityType ,string entityPropName, CancellationToken ct);
	Task DeleteAsync(long fileId, CancellationToken ct);
	Task<FileEntity?> GetAsync(long id, CancellationToken ct);
}
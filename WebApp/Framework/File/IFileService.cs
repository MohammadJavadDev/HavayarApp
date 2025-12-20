using Entities.Base;

namespace WebApp.Framework.File;
 
 
public interface IFileService
{
	// Upload new file
	Task<FileEntity> UploadAsync(
	    IFormFile file,
	    string? entityType,
	    string? entityPropName,
	    long? entityId,
	    CancellationToken ct);

	// Replace existing file
	Task<FileEntity> ReplaceAsync(
	    long? oldFileId,
	    IFormFile newFile,
	    Func<FileEntity, Task> updateReference,
	    CancellationToken ct);

	// Soft delete + physical delete
	Task DeleteAsync(long fileId, CancellationToken ct);

	// Get metadata
	Task<FileEntity?> GetAsync(long id, CancellationToken ct);

 

	// Download as stream (not IFormFile)
	Task<(Stream Stream, string ContentType, string FileName)>
	    DownloadAsync(long fileId, CancellationToken ct);
}

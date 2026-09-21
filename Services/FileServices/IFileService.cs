using Entities.Base;
using Microsoft.AspNetCore.Http;

namespace Services.FileServices;
 
 
public interface IFileService
{
	// Upload new file
	Task<FileEntity> UploadAsync(
	    IFormFile file,
	    string? entityType,
	    string? entityPropName,
	    long? entityId,
	    CancellationToken ct);

	Task<FileEntity> UploadAsync(
    byte[] file,
    string fileName,
    string? contentType,         
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

	/// <summary>
	/// مسیر نسبی FileEntity را به مسیر دیسک/UNC کامل تبدیل می‌کند.
	/// اگر PhysicalPath خودش rooted باشد (از جمله UNC) همان برمی‌گردد.
	/// </summary>
	string GetFullPhysicalPath(string? physicalPath);
}

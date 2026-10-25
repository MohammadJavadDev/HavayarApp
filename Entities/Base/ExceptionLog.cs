using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base;

/// <summary>
/// خطای هندل‌شدهٔ درخواست HTTP. معادل لاگ Event Viewer، داخل دیتابیس.
/// </summary>
[Table("ExceptionLog", Schema = "system")]
public class ExceptionLog
{
	public long Id { get; set; }

	public DateTime CreatedOn { get; set; }

	[MaxLength(30)]
	public string? CreatedOnShamsi { get; set; }

	public string? Message { get; set; }

	[MaxLength(500)]
	public string? ExceptionType { get; set; }

	public string? StackTrace { get; set; }

	public string? InnerMessage { get; set; }

	public string? InnerStackTrace { get; set; }

	[MaxLength(16)]
	public string? HttpMethod { get; set; }

	[MaxLength(1000)]
	public string? RequestPath { get; set; }

	[MaxLength(2000)]
	public string? QueryString { get; set; }

	[MaxLength(64)]
	public string? TraceId { get; set; }

	[MaxLength(64)]
	public string? RequestId { get; set; }

	public int? StatusCode { get; set; }

	public long? UserId { get; set; }

	[MaxLength(256)]
	public string? UserName { get; set; }

	[MaxLength(64)]
	public string? UserIp { get; set; }
}

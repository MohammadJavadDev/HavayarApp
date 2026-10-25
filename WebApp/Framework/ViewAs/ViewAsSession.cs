namespace WebApp.Framework.ViewAs;

public sealed class ViewAsSession
{
	public required string Id { get; init; }
	public long AdminUserId { get; init; }
	public long TargetUserId { get; init; }
	public required string TargetName { get; init; }
	public required string Token { get; init; }
	public DateTime ExpiresUtc { get; init; }
}

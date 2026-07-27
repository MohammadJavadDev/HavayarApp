using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace WebFramework.Services;

public sealed class DynamicActionDescriptorChangeProvider : IActionDescriptorChangeProvider
{
	private CancellationTokenSource _tokenSource = new();

	public IChangeToken GetChangeToken() => new CancellationChangeToken(_tokenSource.Token);

	public void NotifyChanges()
	{
		var previous = Interlocked.Exchange(ref _tokenSource, new CancellationTokenSource());
		previous.Cancel();
		previous.Dispose();
	}
}

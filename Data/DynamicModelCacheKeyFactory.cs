using Entities.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Data;

public sealed class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
	public object Create(DbContext context, bool designTime)
	{
		if (context is ApplicationDbContext appContext)
			return (context.GetType(), designTime, appContext.DynamicModelVersion);

		return (context.GetType(), designTime);
	}
}

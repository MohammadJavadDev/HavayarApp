using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
	public class EntityHistoryRepository(ApplicationDbContext dbContext): IEntityHistoryRepository
	{
		public async Task<List<AuditLog>> GetEntityHistoryById(long id, string type)
		{
			var auditLogs = await dbContext.AuditLogs
				.Where(c => c.EntityId == id && c.TableName == type)
				.Include(c => c.AuditLogDetails)
				.ToListAsync();

			return auditLogs;
		}
	}

	public interface IEntityHistoryRepository
	{
		public Task<List<AuditLog>> GetEntityHistoryById(long id,string type);

	}

	 
}

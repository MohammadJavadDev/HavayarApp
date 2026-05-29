using Common.Attributes;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Entities.Base.ImportDefinitions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.ImportDefinitionServices
{
	public interface IImportRepository
	{
		// ImportDefinition CRUD
		List<ImportDefinition> GetAllDefinitions();
		ImportDefinition GetDefinitionById(long id);
		List<ImportDefinition> GetDefinitionById(List<long?> id);
		long SaveDefinition(ImportDefinition def);
		void DeleteDefinition(long id);

		// Columns
		List<ImportDefinitionColumn> GetColumnsByDefinitionId(long definitionId);
 

		// Logs
		long SaveImportLog(ImportLog log);
		void SaveImportLogDetails(long logId, List<ImportLogDetail> details);
		ImportLog GetImportLogById(long logId);
	}

	public class ImportRepository : IImportRepository
	{

		private readonly IUnitOfWork _unitOfWork;

		public ImportRepository( IUnitOfWork unitOfWork)
		{
	
			_unitOfWork= unitOfWork;
		}
 
		// -------------------------------------------------------
		// ImportDefinition
		// -------------------------------------------------------
		public List<ImportDefinition> GetAllDefinitions()
		{
			var list = new List<ImportDefinition>();
	 
			return _unitOfWork.Repository<ImportDefinition>()
				.TableNoTracking
				.ToList();
		}

		public ImportDefinition GetDefinitionById(long id)
		{
			return _unitOfWork.Repository<ImportDefinition>()
			    .TableNoTracking
			    .First(c=>c.Id == id);
			 
		}

		public long SaveDefinition(ImportDefinition def)
		{
			def =_unitOfWork.Repository<ImportDefinition>()
			  .Save(def);

			return def.Id.Value;
		}

		public void DeleteDefinition(long id)
		{
			_unitOfWork.Repository<ImportDefinition>()
			  .DeleteWhere(c=>c.Id == id);
		}

		// -------------------------------------------------------
		// Columns
		// -------------------------------------------------------
		public List<ImportDefinitionColumn> GetColumnsByDefinitionId(long definitionId)
		{
			var list = _unitOfWork.Repository<ImportDefinition>()
				.TableNoTracking
				.Where(c => c.Id == definitionId)
		          .First();
			var columns = list.Columns.JsonDeserialize<List<ImportDefinitionColumn>>();
			return columns;
		}

		 

		// -------------------------------------------------------
		// Logs
		// -------------------------------------------------------
		public long SaveImportLog(ImportLog log)
		{
			log = _unitOfWork.Repository<ImportLog>()
			   .Save(log);
			return log.Id.Value;
		}

		public void SaveImportLogDetails(long logId, List<ImportLogDetail> details)
		{
			details = _unitOfWork.Repository<ImportLogDetail>()
			  .AddRange(details);
		}

		public ImportLog GetImportLogById(long logId)
		{

			var log = _unitOfWork.Repository<ImportLog>()
				.TableNoTracking
				.Include(c => c.Details)
				.First(c => c.Id == logId);

			return log;
		}

        public List<ImportDefinition> GetDefinitionById(List<long?> id)
        {
			return _unitOfWork.Repository<ImportDefinition>()
			  .TableNoTracking
			  .Where(c => id.Contains(c.Id) ).ToList();
		}
    }
}

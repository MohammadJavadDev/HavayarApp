using Entities.Base.DataTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Contracts
{
    public interface IEntityRepository
    {
        Task<DataTableResponse> FetchData(DataTableRequest request);
        Task ExportToExcelProfile(DataTableRequest request, Stream outputStream, string licensePath);
        
        Task<DataTableResponse> FetchDataProfile(DataTableRequest request , CancellationToken ct);
        Task RemoveEntity(string entityName, long id);

        Task<object> AddEntity(string entityName, object entityData);
    }
}

using Entities.Base.DataTable;
 
using static Data.Repositories.DataTableQueryBuilder;

namespace Data.Contracts
{
    public interface IDataTableQueryBuilder
    {
        DataTableQueryBuilderResult BuildSqlServerQuery (DataTableRequest request);
         DataTableQueryBuilderResult BuildSqlServerQueryProfile(DataTableRequest request ,SystemDataTableProfile systemDataTableProfile);
    }
}

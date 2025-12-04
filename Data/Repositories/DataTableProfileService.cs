using Entities.Base.DataTable;

namespace Data.Repositories
{
    public class DataTableProfileService: IDataTableProfileService
    {
        private List<SystemDataTableProfile> _dataTableProfiles = new();
        public void SetDataTableProfile(List<SystemDataTableProfile> dataTableProfiles)
        {
            _dataTableProfiles = dataTableProfiles;
        }

        public List<SystemDataTableProfile> GetDataTableProfileListByEntityName(string entityName)
        {
            return _dataTableProfiles.Where(c => c.EntityName.ToLower() == entityName.ToLower()).ToList();
        }

        public SystemDataTableProfile? GetDataTableProfileById(long id)
        {
            return _dataTableProfiles.FirstOrDefault(c => c.Id == id);
        }
		public List<SystemDataTableProfile>? GetDataTableProfileById(long?[] ids)
		{
			return _dataTableProfiles.Where(c => ids.Contains(c.Id) ).ToList();
		}

		public void SaveDataProfile(SystemDataTableProfile dataTableProfile)
        {
            var exist = _dataTableProfiles.FirstOrDefault(c => c.Id
                                                               == dataTableProfile.Id);
            if (exist != null)
            {
                exist.Title = dataTableProfile.Title;
                exist.FilterQuery = dataTableProfile.FilterQuery;
                exist.EntityName = dataTableProfile.EntityName;
                exist.Columns = dataTableProfile.Columns;
                exist.Filters = dataTableProfile.Filters;
                exist.EntitySchema = dataTableProfile.EntitySchema;
                exist.SelectQuery = dataTableProfile.SelectQuery;
                exist.FromQuery = dataTableProfile.FromQuery;

            }
            else
            {
                _dataTableProfiles.Add(dataTableProfile);
            }
        }
    }

    public interface IDataTableProfileService
    {
        public void SetDataTableProfile(List<SystemDataTableProfile> dataTableProfiles);
        public List<SystemDataTableProfile> GetDataTableProfileListByEntityName(string entityName);
        public SystemDataTableProfile? GetDataTableProfileById(long id);
	 public List<SystemDataTableProfile>? GetDataTableProfileById(long?[] ids);
		public void SaveDataProfile(SystemDataTableProfile dataTableProfile);
    }
}

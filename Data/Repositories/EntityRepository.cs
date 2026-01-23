using Data.Contracts;
using Entities.Base.DataTable;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Common.System;
using Common.Utilities;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Aspose.Cells;
using License = Aspose.Cells.License;
using Data.SystemAuth;


namespace Data.Repositories
{
    public class EntityRepository(ApplicationDbContext dbContext, ISdk sdk   
         , IDataTableProfileService _dataTableProfileService  
         , IDataTableQueryBuilder _dataTableQuery) : IEntityRepository
    {
        private DataTableRequest currentRequest { get; set; }
        private string? ConnectionString { get; } = dbContext.Database.GetDbConnection().ConnectionString;
		public IEnumerable<Dictionary<string, object>> ExecuteQuery(
		   string query,
		   object? parameters = null)
		{
			var results = new List<Dictionary<string, object>>();
			var columnTypeMap = currentRequest?.columns?
                          .ToDictionary(c => c.data, c => c.type);


			try
			{
				using var connection = new SqlConnection(ConnectionString);
				using var command = new SqlCommand(query, connection);

				if (parameters != null)
				{
					foreach (var prop in parameters.GetType().GetProperties())
					{
						var value = prop.GetValue(parameters) ?? DBNull.Value;
						command.Parameters.Add(
						    new SqlParameter($"@{prop.Name}", value));
					}
				}

				connection.Open();

				using var reader = command.ExecuteReader();

				while (reader.Read())
				{
					var entity = new Dictionary<string, object>(reader.FieldCount);

					for (int i = 0; i < reader.FieldCount; i++)
					{
						var name = reader.GetName(i);

						if (reader.IsDBNull(i))
						{
							entity[name] = null;
							continue;
						}

						if (columnTypeMap == null || !columnTypeMap.TryGetValue(name, out var type))
						{
							entity[name] = reader.GetValue(i);
							continue;
						}

						object value;

						switch (type)
						{
							case "date":
								value = reader.GetDateTime(i).ToString("yyyy-MM-dd");
								break;

							case "datetime":
								value = reader.GetDateTime(i).ToString("yyyy-MM-dd HH:mm:ss");
								break;

							case "datetimeShamsi":
								{
									var fieldType = reader.GetFieldType(i);
									value = fieldType == typeof(DateTime)
									    ? reader.GetDateTime(i).ToShamsiDateTime()
									    : reader.GetValue(i);
									break;
								}

							case "dateShamsi":
								{
									var fieldType = reader.GetFieldType(i);
									value = fieldType == typeof(DateTime)
									    ? reader.GetDateTime(i).ToShamsiDate()
									    : reader.GetValue(i);
									break;
								}

							default:
								value = reader.GetValue(i);
								break;
						}

						entity[name] = value;
					}

					results.Add(entity);
				}
			}
			catch (SqlException ex) when (ex.Message.Contains("Invalid column name"))
			{
				throw new Exception("خطا سمت پایگاه داده: یکی از ستون‌ها حذف یا تغییر نام داده شده است.");
			}

			return results;
		}

		public async Task<DataTableResponse> FetchData(DataTableRequest request)
        {

            throw new Exception("MustBeChange");
             
        }

        public async Task<DataTableResponse> FetchDataProfile(DataTableRequest request)
        {
            var profile = _dataTableProfileService.GetDataTableProfileById((long)request.profileId!);
            if (profile == null)
            {
                throw new Exception("نمایه داده انتخاب شده یافت نشد.");
            }

            currentRequest = request;
            var resQuery = _dataTableQuery.BuildSqlServerQueryProfile(request, profile);

            var items = ExecuteQuery(resQuery.MainQuery);
            var recordsFiltered = ExecuteQuery(resQuery.CountFiltterdQuery).First()["TotalCount"];
            var recordsTotal = ExecuteQuery(resQuery.CountTotalQuery).First()["TotalCount"];


            var res = new DataTableResponse()
            {
                Data = items,
                Draw = request.draw,
                RecordsFiltered = recordsFiltered,
                RecordsTotal = recordsTotal
            };
            return res;
        }

        public async Task ExportToExcelProfile(DataTableRequest request, Stream outputStream, string licensePath)
        {
            var profile = _dataTableProfileService.GetDataTableProfileById((long)request.profileId!);
            if (profile == null)
            {
                throw new Exception("نمایه داده انتخاب شده یافت نشد.");
            }

            var removes =  request.columns.Where(c => c.type == "button").ToList();

              foreach (var r in removes)
              {
                  request.columns.Remove(r);
              }

              currentRequest = request;
            var workbook = new Workbook();

            if (!workbook.IsLicensed)
                new License().SetLicense(licensePath);


            var resQuery = _dataTableQuery.BuildSqlServerQueryProfile(request, profile);
            var query = resQuery.WithoutPagnationQuery;

            // Create a new workbook

            var worksheet = workbook.Worksheets[0];
            worksheet.Name = "LargeData";

            // Retrieve data headers from the first chunk
            var firstChunk = ExecuteQuery(query);
            if (firstChunk == null || !firstChunk.Any())
            {
                return;
            }

            // Add column headers dynamically based on the first record
            var headers = firstChunk.First().Keys.ToList();
            for (int i = 0; i < headers.Count; i++)
            {
                if (!request.columns.Any(c => c.name == headers[i]))
                {
                    headers.Remove( headers[i]);
                }
            }

            
            for (int i = 0; i < headers.Count; i++)
            {
                var header = request.columns[i].title;
                worksheet.Cells[0, i].PutValue(header);
            }

            int row = 1; // Start adding data from the second row

            // Add the first chunk data to the worksheet
            await AddChunkDataToWorksheet(worksheet, firstChunk, row, request);
            row += firstChunk.Count();

            // Add subsequent chunks dynamically
            //while (true)
            //{
            //    // Update query with pagination or limits if necessary
            //    var nextChunk = ExecuteQuery(query); // Update the query to fetch the next set of rows

            //    if (!nextChunk.Any()) break; // Exit if there are no more rows

            //    await AddChunkDataToWorksheet(worksheet, nextChunk, row);
            //    row += nextChunk.Count();
            //}


            worksheet.AutoFitColumns();


            if (!workbook.IsLicensed)
                new License().SetLicense(licensePath);
            workbook.Save(outputStream, SaveFormat.Xlsx);
            await Task.CompletedTask;
             
        }
        private Task AddChunkDataToWorksheet(Worksheet worksheet, IEnumerable<Dictionary<string, object>> chunkData,
            int startRow, DataTableRequest request)
        {
            int row = startRow;
            foreach (var record in chunkData)
            {
                int col = 0;
                foreach (var r in record)
                {
                    var colInfo = request.columns.FirstOrDefault(c=>c.name == r.Key);
                    if (colInfo == null)
                    {
                        continue;
                    }
                    var putValue = r.Value;
                    if (colInfo.type == "select")
                    {
                        if (putValue is not null)
                        {
                            putValue = colInfo.options?.FirstOrDefault(c => c.value == putValue.ToString())?.name ?? putValue;
                        }
                    }
                    worksheet.Cells[row, col].PutValue((putValue ?? string.Empty).ToString());
                    col++;
                }
                row++;
            }

            return Task.CompletedTask;
        }

        public async Task RemoveEntity(string tableName, long id)
        {
            var entityType = typeof(BaseEntity).Assembly.GetTypes().FirstOrDefault(t => t.Name.ToLower() == tableName.ToLower());
            if (entityType == null)
            {
                entityType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name.ToLower() == tableName.ToLower());
                if (entityType == null)
                    throw new Exception("Invalid table name.");
            }

            if (entityType == null)
            {
                throw new Exception($"Entity type '{tableName}' not found.");
            }

            // Get the DbSet for the entity type
            var dbSet = dbContext.GetType().GetMethod("Set", 1, Type.EmptyTypes).MakeGenericMethod(entityType).Invoke(dbContext, null);

            // Find the entity by ID
            var entity = ((IQueryable<object>)dbSet).FirstOrDefault(e => ((BaseEntity)e).Id == id);

            if (entity == null)
            {
                throw new Exception($"Entity with ID '{id}' not found.");
            }

            // Remove the entity
            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        public async Task<object> AddEntity(string entityName, object entityData)
        {
            // Find the entity type by name
            var entityType = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(t => t.Name == entityName);

            var dic = entityData.JsonSerialize().JsonDeserialize<Dictionary<string, object>>();


            if (entityType == null)
            {
                throw new Exception($"Entity type '{entityName}' not found.");
            }

            // Create an instance of the entity type
            var entity = Activator.CreateInstance(entityType);

            // Map the entityData to the entity instance
            foreach (var property in entityType.GetProperties())
            {

                switch (property.Name)
                {
                    case "ModifiedDateMiladiDateTime":
                        property.SetValue(entity, DateTime.Now);
                        break;
                    case "ModifiedDateShamsiDateTime":
                        property.SetValue(entity, DateTime.Now.ToShamsiDateTime());
                        break;
                    case "CreatedOnMiladiDateTime":
                        property.SetValue(entity, DateTime.Now);
                        break;
                    case "CreatedOnShamsiDateTime":
                        property.SetValue(entity, DateTime.Now.ToShamsiDateTime());
                        break;
                    case "IsActive":
                        property.SetValue(entity, 1);
                        break;
                    case "ModifiedById":
                        if (sdk?.CurrentUser != null)
                            property.SetValue(entity, sdk.CurrentUser.Id);
                        break;
                    case "ModifiedByName":
                        if (sdk?.CurrentUser != null)
                            property.SetValue(entity, sdk.CurrentUser.FullName);
                        break;

                    case "CreatedByName":
                        if (sdk?.CurrentUser != null)
                            property.SetValue(entity, sdk.CurrentUser.FullName);
                        break;
                    case "CreatedById":
                        if (sdk?.CurrentUser != null)
                            property.SetValue(entity, sdk.CurrentUser.Id);
                        break;
                    default:
                        if (dic.TryGetValue(property.Name, out var val))
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(property.PropertyType);
                            var d = val.ToString();
                            property.SetValue(entity, converter.ConvertFrom(d));
                        }
                        break;
                         
                }
                 
            }

            // Get the DbSet for the entity type
            var dbSet = dbContext.GetType().GetMethod("Set", 1, Type.EmptyTypes).MakeGenericMethod(entityType).Invoke(dbContext, null);

            // Add the entity
            dbSet.GetType().GetMethod("Add").Invoke(dbSet, new[] { entity });
            await dbContext.SaveChangesAsync();

            return   entity ;
        }
    }
}

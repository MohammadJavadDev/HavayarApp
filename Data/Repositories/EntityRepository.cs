using Aspose.Cells;
using Common.System;
using Common.Utilities;
using Data.Contracts;
using Data.Services.QueryBuilderServices;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportBuilder.Entities;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using License = Aspose.Cells.License;


namespace Data.Repositories
{
    public class EntityRepository(ApplicationDbContext dbContext, ISdk sdk   
         , IDataTableProfileService _dataTableProfileService ,
	    IQueryService queryService) : IEntityRepository
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



          public async Task<DataTableResponse> FetchDataProfile(DataTableRequest request, CancellationToken cn = default)
          {
                
               var savedQueryId = request.profileId;
               var paramValues = queryService.ExtractParameterValuesFromRequest(request);
               QueryResult result;


			result = await queryService.ExecuteReportAsync(request, paramValues, request.start, request.length, sdk.CurrentUser.Id.ToString(), sdk.CurrentUser.Username);

			var response = new DataTableResponse
               {
                    Draw = request.draw,
                    RecordsTotal = result.TotalRows,
                    RecordsFiltered = result.TotalFilterdRows,
                    Data = result.Rows
               };
               return response;
          }

		public async Task ExportToExcelProfile(DataTableRequest request, Stream outputStream, string licensePath)
		{
			var profile = await queryService.GetReportAsync((long)request.profileId!);
			if (profile == null)
				throw new Exception("نمایه داده انتخاب شده یافت نشد.");

			// حذف ستون‌های دکمه
			request.columns = request.columns.Where(c => c.type != "button").ToList();
			currentRequest = request;

			var workbook = new Workbook();
			if (!workbook.IsLicensed)
				new License().SetLicense(licensePath);

			var savedQueryId = request.profileId;
			var paramValues = queryService.ExtractParameterValuesFromRequest(request);

			request.start = 0;
			request.length = 10000;

			var worksheet = workbook.Worksheets[0];
			worksheet.Name = "LargeData";

			var firstChunk = await queryService.ExecuteReportAsync(
			    request, paramValues, request.start, request.length,
			    sdk.CurrentUser.Id.ToString(), sdk.CurrentUser.Username);

			if (firstChunk == null || !firstChunk.Rows.Any())
				return;

			var availableKeys = firstChunk.Rows.First().Keys.ToHashSet();
			var orderedHeaders = request.columns
			    .Where(c => availableKeys.Contains(c.name))
			    .Select(c => c.name)
			    .ToList();


			for (int i = 0; i < orderedHeaders.Count; i++)
			{
				var col = request.columns.First(c => c.name == orderedHeaders[i]);
				worksheet.Cells[0, i].PutValue(col.title);
			}

			int row = 1;
	
			await AddChunkDataToWorksheet(worksheet, firstChunk.Rows, row, request, orderedHeaders);
			row += firstChunk.Rows.Count();

			workbook.Save(outputStream, SaveFormat.Xlsx);
			await Task.CompletedTask;
		}


		private Task AddChunkDataToWorksheet(
		    Worksheet worksheet,
		    IEnumerable<Dictionary<string, object>> chunkData,
		    int startRow,
		    DataTableRequest request,
		    List<string> orderedHeaders)
		{
			int row = startRow;
			foreach (var record in chunkData)
			{

				for (int colIndex = 0; colIndex < orderedHeaders.Count; colIndex++)
				{
					var key = orderedHeaders[colIndex];
					var colInfo = request.columns.FirstOrDefault(c => c.name == key);
					if (colInfo == null) continue;

					record.TryGetValue(key, out var putValue);

					if (colInfo.type == "select" && putValue is not null)
					{
						putValue = colInfo.options?
						    .FirstOrDefault(c => c.value == putValue.ToString())?.name
						    ?? "";
					}

					var cell = worksheet.Cells[row, colIndex];
					var colType = (colInfo.type ?? string.Empty).Trim().ToLowerInvariant();

					// ستون اعشاری: اگر بخش اعشار صفر باشد به‌صورت عدد صحیح، وگرنه با اعشار معنادار
					if (colType == "decimal" && TryPutDecimalExcelValue(cell, putValue))
					{
						// مقدار عددی در سلول قرار گرفت
					}
					else
					{
						cell.PutValue((putValue ?? string.Empty).ToString());
					}
				}
				row++;
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// برای خروجی اکسل: اعداد صحیح (مثل 5.00) بدون اعشار، اعداد اعشاری با بخش کسری معنادار.
		/// </summary>
		private static bool TryPutDecimalExcelValue(Cell cell, object? value)
		{
			if (value == null || value == DBNull.Value)
				return false;

			decimal d;
			switch (value)
			{
				case decimal dec:
					d = dec;
					break;
				case double dbl when !double.IsNaN(dbl) && !double.IsInfinity(dbl):
					d = Convert.ToDecimal(dbl);
					break;
				case float fl when !float.IsNaN(fl) && !float.IsInfinity(fl):
					d = Convert.ToDecimal(fl);
					break;
				case int i:
					cell.PutValue(i);
					return true;
				case long l when l >= int.MinValue && l <= int.MaxValue:
					cell.PutValue((int)l);
					return true;
				case long l:
					cell.PutValue(Convert.ToDouble(l));
					return true;
				case short s16:
					cell.PutValue((int)s16);
					return true;
				case byte b:
					cell.PutValue((int)b);
					return true;
				default:
					var s = value.ToString()?.Trim();
					if (string.IsNullOrEmpty(s))
						return false;
					s = s.Replace(',', '.');
					if (!decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)
					    && !decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
						return false;
					break;
			}

			// حذف صفرهای انتهایی scale (مثل 5.00 → 5) با فرمت G29
			d = decimal.Parse(d.ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

			if (d == decimal.Truncate(d) && d >= int.MinValue && d <= int.MaxValue)
			{
				cell.PutValue(decimal.ToInt32(d));
			}
			else
			{
				cell.PutValue(Convert.ToDouble(d));
			}

			return true;
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

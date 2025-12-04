using System.Data;
using System.Text;
using Aspose.Cells;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportBuilder.Entities;
using ReportBuilder.Services.Contracts;
using static Data.Repositories.DataTableQueryBuilder;

namespace ReportBuilder.Services.Repositories
{
    public class ReportBuilderService(ApplicationDbContext dbContext,IUnitOfWork unitOfWork 
        , IDataTableQueryBuilder _dataTableQuery ) : IReportBuilderService
    {

        private string? ConnectionString { get;  } = dbContext.Database.GetDbConnection().ConnectionString;

        private DataTableRequest currentRequest { get; set; }


        public async Task<List<List<Dictionary<string, object>>>> ExecuteQueryGetHeadersAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
        {
            var results = new List<List<Dictionary<string, object>>>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                    {
                        if (parameters is Dictionary<string, object> outDictionary)
                        {
                            foreach (var prop in outDictionary)
                            {
                                command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                            }
                        }
                        else
                        {
                            foreach (var prop in parameters.GetType().GetProperties())
                            {
                                command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                            }
                        }
                    }

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        do
                        {
                            var resultSet = new List<Dictionary<string, object>>();

                            foreach (var dbColumn in reader.GetColumnSchema().ToList())
                            {
                                var entity = new Dictionary<string, object>
                            {
                                { "ColumnName", dbColumn.ColumnName },
                                { "DataType", dbColumn.DataTypeName }
                            };
                                resultSet.Add(entity);
                            }

                            results.Add(resultSet);
                        }
                        while (await reader.NextResultAsync(cancellationToken));
                    }
                }
            }

            return results;
        }

        public async Task<List<List<Dictionary<string, object>>>> ExecuteStoredProcedureAsync(string storedProcedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            var results = new List<List<Dictionary<string, object>>>();

            await using var connection = new SqlConnection(ConnectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(storedProcedureName, connection);

            command.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
            {
                if (parameters is Dictionary<string, object> outDictionary)
                {
                    foreach (var prop in outDictionary)
                    {
                        command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                    }
                }
                else
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            do
            {
                var selects = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = new Dictionary<string, object>();

                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var propertyName = reader.GetName(i);

                        var col = currentRequest?.columns?.FirstOrDefault(c => c.data == propertyName);


                        if (!reader.IsDBNull(i))
                        {
                            var value = reader.GetValue(i);
                            if (col != null)
                            {
                                if (col.type == "datetime")
                                {
                                    value = DateTime.Parse(value.ToString()).ToShamsiDateTime();

                                }
                                else if (col.type == "date")
                                {
                                    value = DateTime.Parse(value.ToString()).ToShamsiDate();
                                }
                            }

                            entity.Add(propertyName, value);
                        }
                        else
                        {
                            entity.Add(propertyName, null);
                        }
                    }

                    selects.Add(entity);
                }
                results.Add(selects);
            }
            while (await reader.NextResultAsync(cancellationToken));

            return results;
        }

        public List<List<Dictionary<string, object>>> ExecuteStoredProcedure(string storedProcedureName, object? parameters = null)
        {
            var results = new List<List<Dictionary<string, object>>>();

             using var connection = new SqlConnection(ConnectionString);

             connection.OpenAsync();

             using var command = new SqlCommand(storedProcedureName, connection);

            command.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
            {
                if (parameters is Dictionary<string, object> outDictionary)
                {
                    foreach (var prop in outDictionary)
                    {
                        command.Parameters.AddWithValue($"@{prop.Key}", prop.Value);
                    }
                }
                else
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                    }
                }
            }

            using var reader =  command.ExecuteReader();
            do
            {
                var selects = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    var entity = new Dictionary<string, object>();

                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var propertyName = reader.GetName(i);

                        var col = currentRequest?.columns?.FirstOrDefault(c => c.data == propertyName);


                        if (!reader.IsDBNull(i))
                        {
                            var value = reader.GetValue(i);
                            if (col != null)
                            {
                                if (col.type == "datetime")
                                {
                                    value = DateTime.Parse(value.ToString()).ToShamsiDateTime();

                                }
                                else if (col.type == "date")
                                {
                                    value = DateTime.Parse(value.ToString()).ToShamsiDate();
                                }
                            }

                            entity.Add(propertyName, value);
                        }
                        else
                        {
                            entity.Add(propertyName, null);
                        }
                    }

                    selects.Add(entity);
                }
                results.Add(selects);
            }
            while ( reader.NextResult());

            return results;
        }

 
        public IEnumerable<Dictionary<string, object>> ExecuteQuery(string query, object? parameters = null)
        {

            var results = new List<Dictionary<string, object>>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    if (parameters != null)
                    {
                        foreach (var prop in parameters.GetType().GetProperties())
                        {
                            command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                        }
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entity = new Dictionary<string, object>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var propertyName = reader.GetName(i);

                                var col = currentRequest?.columns?.FirstOrDefault(c=>c.data ==  propertyName);


								if (!reader.IsDBNull(i))
                                {
                                    var value = reader.GetValue(i);
                                    if(col != null)
                                    {
										if (col.type == "datetime")
										{
											value = DateTime.Parse(value.ToString()).ToShamsiDateTime();

										}
										else if (col.type == "date")
										{
											value = DateTime.Parse(value.ToString()).ToShamsiDate();
										}
									}
                                   
                                    entity.Add(propertyName, value);
                                }
                                else
                                {
                                    entity.Add(propertyName, null);
                                }
                            }

                            results.Add(entity);
                        }
                    }
                }
            }

            return results;
        }

        public async Task<object> FetchDataReportStoreProcAsync(FetchDataReportStoreProcViewModelReq request, CancellationToken cn)
        {


            var filters = new Dictionary<string, object>();
            foreach (var requestFilter in request.Filters)
            {
                filters.Add(requestFilter.ColumnName, requestFilter.Value);

            }


            var items = await ExecuteStoredProcedureAsync(request.StoreProcName, filters); ;

            return items;

        }

        public List<Dictionary<string, object>> FetchTableDataStimilSoft(FetchDataReportTableViewModelReq request)
        {
            
            var resQuery = BuildSqlServerQuery(request);

            var items = ExecuteQuery(resQuery.MainQuery);
         
            return items.ToList();
        }
        public DataTableQueryBuilderResult BuildSqlServerQuery(FetchDataReportTableViewModelReq request)
        {
            var res = new DataTableQueryBuilderResult();
            StringBuilder query = new StringBuilder($"SELECT * From {request.TableName} (NoLock)");

            if (request.Filters.Any())
            {
                query.AppendLine("Where");
                var queryParts = new List<string>();
                request.Filters.ForEach(z =>
                {
                    var propAddress = z.ColumnName;
                    var valuePlaceholders = string.Join(",", z.Values.Select(v => $"{v}"));
                    var part = "";
                    switch (z.Condition)
                    {
                        case "=":
                            part = $" {propAddress} = N'{valuePlaceholders}'";
                            break;
                        case "!=":
                            part = $"{propAddress} <> N'{valuePlaceholders}'";
                            break;
                        case ">":
                            part = $"{propAddress} > N'{valuePlaceholders}'";
                            break;
                        case ">=":
                            part = $"{propAddress} >= N'{valuePlaceholders}'";
                            break;
                        case "<":
                            part = $"{propAddress} < N'{valuePlaceholders}'";
                            break;
                        case "<=":
                            part = $"{propAddress} <= N'{valuePlaceholders}'";
                            break;
                        case "contains":
                            part = $"{propAddress} LIKE N'%{valuePlaceholders}%'";
                            break;
                        case "!contains":
                            part = $"{propAddress} NOT LIKE N'%{valuePlaceholders}%'";
                            break;
                        case "starts":
                            part = $"{propAddress} LIKE N'{valuePlaceholders}%'";
                            break;
                        case "!starts":
                            part = $"{propAddress} Not LIKE N'{valuePlaceholders}%'";
                            break;
                        case "ends":
                            part = $"{propAddress} LIKE N'%{valuePlaceholders}'";
                            break;
                        case "!ends":
                            part = $"{propAddress} Not LIKE N'%{valuePlaceholders}'";
                            break;
                        case "IN":
                            part = $"{propAddress} IN ({valuePlaceholders})";
                            break;
                        case "NOT IN":
                            part = $"{propAddress} NOT IN ({valuePlaceholders})";
                            break;
                        case "null":
                            part = $"{propAddress} IS NULL";
                            break;
                        case "!null":
                            part = $"{propAddress} IS NOT NULL";
                            break;
                        case "between":
                            part = $"{propAddress} BETWEEN N'{z.Values[0]}' And N'{z.Values[1]}'";
                            break;
                        case "!between":
                            part = $"{propAddress} NOT BETWEEN N'{z.Values[0]}' And N'{z.Values[1]}'";
                            break;
                        default:
                            throw new ArgumentException($"Unsupported operator: {z.Condition}");
                    }
                    queryParts.Add(part);
                });
                query.Append(string.Join(" And ", queryParts));
            }

            return new DataTableQueryBuilderResult()
            {
                MainQuery = query.ToString()
            };
        }

        public List<List<Dictionary<string, object>>> FetchDataStoreProc(FetchDataReportStoreProcViewModelReq request)
        {


            var filters = new Dictionary<string, object>();
            foreach (var requestFilter in request.Filters)
            {
                filters.Add(requestFilter.ColumnName , requestFilter.Value);
      
            }
              
              var items = ExecuteStoredProcedure(request.StoreProcName, filters); ;

              return items;

        }

        public async Task ExportLargeDataToExcelAsync(DataTableRequest request, Stream outputStream , string licensePath)
        {

			currentRequest = request;
			var workbook = new Workbook();

            if(!workbook.IsLicensed)
             new License().SetLicense(licensePath);


            var resQuery = _dataTableQuery.BuildSqlServerQuery(request);
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
                var header = request.columns[i].title;
                worksheet.Cells[0, i].PutValue(header);
            }

            int row = 1; // Start adding data from the second row

            // Add the first chunk data to the worksheet
            await AddChunkDataToWorksheet(worksheet, firstChunk, row , request);
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

        public List<ReportBuilderReport> GetListReports()
        {
            return unitOfWork.Repository<ReportBuilderReport>()
                .TableNoTracking
                .Where(c => c.IsActive == IsActiveEnum.Active)
                .Select(c=>new ReportBuilderReport()
                {
                    Title = c.Title,
                    Id = c.Id,
                    IsActive = c.IsActive
                })
                .ToList();
        }


        private Task AddChunkDataToWorksheet(Worksheet worksheet, IEnumerable<Dictionary<string, object>> chunkData,
            int startRow, DataTableRequest request)
        {
            int row = startRow;
            foreach (var record in chunkData)
            {
                int col = 0;
                foreach (var value in record.Values)
                {
                    var colInfo = request.columns[col];
                    var putValue = value;
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



        public async Task<List<DbObjects>> ListDbObjects()
	    {
		  var data= await unitOfWork.Repository<ReportBuilderTable>()
			    .ExecuteQueryAsync(@"
				SELECT TABLE_SCHEMA ,TABLE_NAME, TABLE_TYPE 
					FROM INFORMATION_SCHEMA.TABLES
					WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
					ORDER BY TABLE_TYPE, TABLE_NAME;
									");

         var newObj=  data.Select(c => new DbObjects()
            {
                Name = c["TABLE_NAME"].ToString(),
                Schema = c["TABLE_SCHEMA"].ToString(),
                Type = c["TABLE_TYPE"].ToString()
            }).ToList();


              data = await unitOfWork.Repository<ReportBuilderTable>()
               .ExecuteQueryAsync(@"
				SELECT 
    SCHEMA_NAME(schema_id)  'TABLE_SCHEMA',
	name AS TABLE_NAME,
    'Store_Procedures' 'TABLE_TYPE'

                FROM 
                    sys.procedures
                ORDER BY 
                    name;
									");

            newObj.AddRange(
                data.Select(c => new DbObjects()
                {
                    Name = c["TABLE_NAME"].ToString(),
                    Schema = c["TABLE_SCHEMA"].ToString(),
                    Type = c["TABLE_TYPE"].ToString()
                }).ToList()
                );


            return newObj;


	    }

	    public async Task<DbObjects> DbObjectWithItems(string tableName , string type)
	    {
            IEnumerable<Dictionary<string, object>> data = new List<Dictionary<string, object>>();

            var returnDataObject = new DbObjects()
            {
                TableType = type,
                
            };

            var procData = new Dictionary<string, object>();

            if (type == "BASE TABLE" || type == "VIEW")
            {
                returnDataObject.Selects.Add(new ()
                {
                    Items = new()
                });
                data = await unitOfWork.Repository<ReportBuilderTable>()
                          .ExecuteQueryAsync(@"
			        SELECT 
                        c.name AS ColumnName,
                        t.name AS DataType,
                        c.max_length AS MaxLength,
                        c.is_nullable AS IsNullable,
                        c.is_identity AS IsIdentity
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID(@tableName)
                    ORDER BY c.column_id;", new { tableName });

                foreach (var c in data)
                {
                    returnDataObject.Selects[0].Items.Add(new DbObjectItems()
                    {
                        Show = true,
                        ColumnName = c["ColumnName"].ToString(),
                        DataType = c["DataType"].ToString(),
                        Filter = false,
                        TableType = type
                    });
                 
                }

            }
            else
            {
                data = await unitOfWork.Repository<ReportBuilderTable>()
                    .ExecuteQueryAsync(@"
			 	SELECT 
                        p.name AS ColumnName,
                        p.is_output AS IsOutput,
                        t.name AS DataType,
                        p.max_length AS MaxLength
                    FROM 
                        sys.parameters AS p
                    JOIN 
                        sys.types AS t ON p.user_type_id = t.user_type_id
                    WHERE 
                        object_id = OBJECT_ID(@tableName);", new { tableName });

                var queryGetCol = @$"EXEC {tableName} ";

                foreach (var c in data)
                {
                    var tempVal = "";
                    var colType = c["DataType"].ToString();


                    if (colType == "datetime" || colType == "datetime2")
                    {
                        var format = "yyyy-MM-dd HH:mm:ss:fff";
                        tempVal = DateTime.Now.ToString(format);

                    }
                    else if (colType == "nvarchar")
                    {
                        tempVal = "";
                    }
                    else if (colType == "bigint" || colType == "int" || colType == "integer")
                    {
                        tempVal = "1";
                    }
                    else if (colType == "bit" || colType == "boolean" || colType == "bool")
                    {
                        tempVal = "0";
                    }
                    else if (colType == "uniqueidentifier")
                    {
                        tempVal = Guid.NewGuid().ToString();
                    }

                    returnDataObject.Filters.Add(new DbObjectItems()
                    {
                        Show = false,
                        ColumnName = c["ColumnName"].ToString().Replace("@",""),
                        DataType = c["DataType"].ToString(),
                        Filter = true,
                        TableType = type
                    });

                   procData.Add(c["ColumnName"].ToString().Replace("@", ""), tempVal);

  
                }


                var data2 = await ExecuteQueryGetHeadersAsync(tableName, procData);
               foreach (var selects in data2)
               {

                   var selectObj = new DbObjectSelectItems()
                   {
                       Items = new()
                   };
                   
                   foreach (var c in selects)
                   {
                       selectObj.Items.Add(new DbObjectItems()
                       {
                           Show = true,
                           ColumnName = c["ColumnName"].ToString(),
                           DataType = c["DataType"].ToString(),
                           Filter = false,
                           TableType = type
                       });
                   }

                   returnDataObject.Selects.Add(selectObj);
                }

            }

            var dts = tableName.Split(".");

            var existTable = await unitOfWork.Repository<ReportBuilderTable>()
                .TableNoTracking
                 
                .FirstOrDefaultAsync(c => 
                    c.ObjectSchema == dts[0] &&
                    c.ObjectName == dts[1]);

            if (existTable != null)
            {
                existTable.ReportBuilderTableFilters =
                    existTable.FiltersContent.JsonDeserialize<List<ReportBuilderTableItem>>() ?? new List<ReportBuilderTableItem>();

                existTable.ReportBuilderTableSelects =
                    existTable.SelectsContent.JsonDeserialize<List<ReportBuilderTableSelect>>() ?? new List<ReportBuilderTableSelect>();

                returnDataObject.TableItemId = existTable.Id;
                returnDataObject.TableTitle = existTable.Title;
                foreach (var filter in returnDataObject.Filters)
                {
                    var f = existTable.ReportBuilderTableFilters
                        .FirstOrDefault(f => f.ColumnName == filter.ColumnName);
                    filter.Title = f?.Title;
                    filter.Show = f?.Show ?? false;
                    
                }

                for (int i = 0; i < returnDataObject.Selects.Count; i++)
                {
                    if (existTable.ReportBuilderTableSelects.Count > i)
                    {
                        var existSelect = existTable.ReportBuilderTableSelects[i];

                        foreach (var select in returnDataObject.Selects[i].Items)
                        {
                            var s = existSelect?.ReportBuilderReportItems
                                .FirstOrDefault(f => f.ColumnName == select.ColumnName);
                            select.Title = s?.Title;
                            select.Show = s?.Show ?? false;

                        }

                        returnDataObject.Selects[i].Title = existSelect?.Title;
                    }
                
                     
                }
                
            }
            
            return returnDataObject;

         
        }

    
        private string ConvertType(string type)
        {
            switch(type)
            {
                case"datetime2":
                case "datetime":
                    return "datetime";
                case "date":
                    return "date";

                case "nvarchar":
                case "char":
                    return "string";
                case "bigint":
                case "int":
                case "integer":
                    return "long";
                case "bool":
                case "boolean":
                case "bit":
                    return "bool";

                case "uniqueidentifier":
            
                    return "guid";

                default:
                    return "string";
            }
        }

        public async Task<DataTableResponse> FetchData(DataTableRequest request, CancellationToken cn)
        {
            currentRequest = request;
			var resQuery = _dataTableQuery.BuildSqlServerQuery(request);

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

        public List<Dictionary<string, object>> FetchSampleData(string objectName )
        {
            var items = ExecuteQuery($@"SELECT Top (100) * FROM {objectName} AS r (NoLock)");

            return items.ToList();
        }



    }
}

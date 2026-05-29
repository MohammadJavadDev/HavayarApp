 
using System.Text;
 
using Common.System;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.Base.DataTable;
 

namespace Data.Repositories
{
    public class DataTableQueryBuilder(ISdk sdk) : IDataTableQueryBuilder
    {
        private List<TablesCounter> TablesCounters { get; set; } = new();
        private int Counter { get; set; } = 0;
        public class TablesCounter
        {
            public string TableName { get; set; }
            public string Alias { get; set; }
            public string? RelatedTablePropertyName { get; set; }
            public List<TablesProperty> Items { get; set; } = new();
        }
        public class TablesProperty
        {
            public string Alias { get; set; }
            public string Data { get; set; }
            public string Name { get; set; }
        }

        public class DataTableQueryBuilderResult
        {
            public string MainQuery { get; set; }
            public string CountFiltterdQuery { get; set; }
            public string CountTotalQuery { get; set; }
            public string WithoutPagnationQuery { get; set; }

    }
    public DataTableQueryBuilderResult BuildSqlServerQuery(DataTableRequest request)
    {
        var res = new DataTableQueryBuilderResult();
            StringBuilder query = new StringBuilder("SELECT ");
            StringBuilder selectSection = new StringBuilder();
            StringBuilder searchSection = new StringBuilder();

            TablesCounters.Add(new TablesCounter
            {
                TableName = request.TableName,
                Alias = $"t{Counter}"
            });


            // Add columns
            if (request.columns is { Count: > 0 })
            {
                foreach (var cl in request.columns)
                {
                    var selectname = cl.data;
                    var tablename = request.TableName;

                    if (cl.tableName.HasValue(true))
                    {
                        tablename = cl.tableName;
                    }

                    if (cl.name.HasValue(true))
                    {
                        selectname = cl.name;
                    }

                    var table = TablesCounters.FirstOrDefault(c => c.TableName == tablename);

                    if (cl.type == "entity")
                    {
                        var parts = cl.name.Split('.');
                        var relateTable = TablesCounters.FirstOrDefault(c => c.RelatedTablePropertyName == parts[0]);
                        if (relateTable == null)
                        {
                            Counter++;
                            TablesCounters.Add(new()
                            {
                                TableName = tablename,
                                Alias = $"t{Counter}",
                                RelatedTablePropertyName = parts[0]


                            });
                            table = TablesCounters.FirstOrDefault(t => t.RelatedTablePropertyName == parts[0]);
                        }
                        else
                        {
                            table = relateTable;
                        }
                    }
                    else
                    {
                        if (table == null)
                        {
                            Counter++;
                            TablesCounters.Add(new()
                            {
                                TableName = tablename,
                                Alias = $"t{Counter}",


                            });
                            table = TablesCounters.FirstOrDefault(t => t.TableName == tablename);

                        }
                    }



                    table.Items.Add(new TablesProperty
                    {
                        Alias = table.Alias,
                        Data = cl.data,
                        Name = cl.name
                    });


                    if (cl.type == "entity")
                    {
                        var parts = cl.name.Split('.');
                        var tableAlias = cl.tableName;
                        var columnName = parts[1];


                        selectSection.Append($"[{table.Alias}].[{columnName}] {cl.data} ,");
                        if (cl.Search.value is { Length: > 0 } && cl.Search.value[0].HasValue())
                        {
                            if (searchSection.Length > 0)
                            {
                                searchSection.Append(" And ");
                            }

                            if (cl.Search.value.Length == 1)
                            {
                                searchSection.Append($"[{table.Alias}].[{columnName}] Like N'%{cl.Search.value[0]}%'");
                            }
                            else
                            {
                                searchSection.Append($"[{table.Alias}].[{columnName}]  BETWEEN '{cl.Search.value[0]}' AND '{cl.Search.value[1]}'");
                            }
                        }

                    }
                    else
                    {

                        if (cl.Search.value is { Length: > 0 } && cl.Search.value[0].HasValue())
                        {
                            if (searchSection.Length > 0)
                            {
                                searchSection.Append(" And ");
                            }

                            if (cl.Search.value.Length == 1)
                            {
                                searchSection.Append($"[{table.Alias}].[{selectname}] Like N'%{cl.Search.value[0]}%'");
                            }
                            else
                            {
                                searchSection.Append($"[{table.Alias}].[{selectname}]  BETWEEN '{cl.Search.value[0]}' AND '{cl.Search.value[1]}'");
                            }
                        }

                        selectSection.Append($"[{table.Alias}].[{cl.data}] {cl.data} ,");
                    }


                }

            }
            else
            {
                selectSection.Append("* ");
            }
            selectSection.Remove(selectSection.Length - 1, 1);


            query.Append(selectSection.ToString());

            var sp = request.TableName.Split(".");

            if (sp.Length > 1)
            {

                query.Append(" FROM ").Append($"[{sp[0]}].[{sp[1]}] {TablesCounters[0].Alias}");
            }
            else
            {
                query.Append(" FROM ").Append($"[{sp[0]}] {TablesCounters[0].Alias}");
            }

            // Track tables to avoid duplicate joins
            var joinedTables = new HashSet<string>();

            // Add JOINs if columns contain '.'
            if (request.columns != null)
            {
                foreach (var column in request.columns)
                {
                    var tablename = request.TableName;

                    if (column.tableName.HasValue(true))
                    {
                        tablename = column.tableName;
                    }

                    var table = TablesCounters.FirstOrDefault(c => c.TableName == tablename);

                    if (column.type == "entity")
                    {
                        var parts = column.name.Split('.');

                        table = TablesCounters.FirstOrDefault(t => t.RelatedTablePropertyName == parts[0]);

                        var tableAlias = table.TableName;
                        var columnName = parts[1];
                        var coladdress = column.data.Replace(columnName, "");

                        if (!joinedTables.Contains(tableAlias))
                        {


                            query.Append($" LEFT JOIN [{tableAlias}] {table.Alias} ON [{TablesCounters[0].Alias}].[{coladdress}Id] = [{table.Alias}].[Id]");
                            joinedTables.Add(tableAlias);
                        }
                    }
                }
            }

            res.CountTotalQuery = query.Replace(selectSection.ToString(), "Count(0) TotalCount").ToString();


            // Add search searchBuilder conditions
            if (request.searchBuilder is { criteria.Count: > 0 })
            {

                var criteriaQuery = BuildCriteriaQuery(request.searchBuilder.criteria, request.searchBuilder.logic, request);
                if (searchSection.Length > 0)
                {
                    searchSection.Append($"And {criteriaQuery}");
                }
                else
                {
                    searchSection.Append($" {criteriaQuery} ");
                }

            }

            // Add search conditions
            if (searchSection.Length > 0)
            {
                query.Append(" WHERE ");
                query.Append(searchSection.ToString());

            }


            res.CountFiltterdQuery = query.ToString();

            // Add order conditions
            if (request.order is { Length: > 0 })
            {
                query.Append(" ORDER BY ");
                var orderConditions = request.order.Select(o => $"{o.column} {o.dir}");
                query.Append(string.Join(", ", orderConditions));
            }
            else
            {
                query.Append(" ORDER BY ");

                query.Append($"[{TablesCounters[0].Alias}].[{request.columns[0].data}] DESC");
            }

            query = query.Replace("Count(0) TotalCount", selectSection.ToString());

            res.WithoutPagnationQuery = query.ToString();


            query.Append($" OFFSET {request.start} ROWS FETCH NEXT {request.length} ROWS ONLY");

            res.MainQuery = query.ToString();

            return res;

    }
           

    public DataTableQueryBuilderResult BuildSqlServerQueryProfile(DataTableRequest request, SystemDataTableProfile systemDataTableProfile)
    {
        var res = new DataTableQueryBuilderResult();
            StringBuilder query = new StringBuilder("Select " + systemDataTableProfile.SelectQuery);

            if (systemDataTableProfile.FilterQuery != null && systemDataTableProfile.FilterQuery.IndexOf("{currentUser.Id}", StringComparison.Ordinal) > -1)
            {
                systemDataTableProfile.FilterQuery = systemDataTableProfile.FilterQuery.Replace("{currentUser.Id}", sdk?.CurrentUser?.Id.ToString());
            }
             

            query.AppendLine("\n"+systemDataTableProfile.FromQuery);
        StringBuilder searchSection = new StringBuilder(systemDataTableProfile.FilterQuery);

        systemDataTableProfile.SystemDataTableProfileSelectViewModels = 
            systemDataTableProfile.Columns
            .JsonDeserialize<List<SystemDataTableProfileSelectViewModel>>();

            if (request.columns is { Count: > 0 })
            {
                foreach (var cl in request.columns)
                {
                    var pc = 
                        systemDataTableProfile
                        .SystemDataTableProfileSelectViewModels
                        .FirstOrDefault(c => c.Alliance == cl.data);

                    if(cl.Search.value.Any())
                         {
                              if(searchSection.Length > 0)
                              {
                                   searchSection.Append(" and ");

						}

						if (cl.type == "shamsidatetime" || cl.type == "datetimeshamsi" || cl.type == "shamsidate" || cl.type == "dateshamsi")
						{

							if (cl.Search.value.Length == 1 && cl.Search.value[0].Contains("from"))
							{
								var dateFilter = cl.Search.value[0].Replace("from ", "");
								searchSection.Append($"[{pc.Label}].[{pc.PropName.Replace("Shamsi", "Miladi")}] > '{dateFilter}'");
							}
							else if (cl.Search.value.Length == 1 && cl.Search.value[0].Contains("to"))
							{
								var dateFilter = cl.Search.value[0].Replace("to ", "");
								searchSection.Append($"[{pc.Label}].[{pc.PropName.Replace("Shamsi", "Miladi")}] < '{dateFilter}'");
							}
							else if (cl.Search.value.Length == 2)
							{
								var dateFilterFrom = cl.Search.value.First(c => c.Contains("from")).Replace("from ", "");
								var dateFilterTo = cl.Search.value.First(c => c.Contains("to")).Replace("to ", "");
								searchSection.Append($"[{pc.Label}].[{pc.PropName.Replace("Shamsi", "Miladi")}] BETWEEN '{dateFilterFrom}' AND '{dateFilterTo}'");
							}

						}
						else if (cl.Search.value.Length == 1)
						{
							searchSection.Append($"[{pc.Label}].[{pc.PropName}] Like N'%{cl.Search.value[0]}%'");
						}
						else if (cl.Search.value.Length == 2)
						{
							searchSection.Append($"[{pc.Label}].[{pc.PropName}]  BETWEEN '{cl.Search.value[0]}' AND '{cl.Search.value[1]}'");
						}
					}
                      
                }

            }
           res.CountTotalQuery = query.Replace(systemDataTableProfile.SelectQuery, "Count(0) TotalCount").ToString();

            // Add search searchBuilder conditions
            if (request.searchBuilder is { criteria.Count: > 0 })
            {

                var criteriaQuery = BuildCriteriaQueryProfile(request.searchBuilder.criteria, request.searchBuilder.logic, request ,systemDataTableProfile);
                if (searchSection.Length > 0)
                {
                    searchSection.Append($"And {criteriaQuery}");
                }
                else
                {
                    searchSection.Append($" {criteriaQuery} ");
                }

            }

            // Add search conditions
            if (searchSection.Length > 0)
            {
                query.Append(" WHERE ");
                query.Append(searchSection.ToString());

            }
             
            res.CountFiltterdQuery = query.ToString();

            // Add order conditions
            if (request.order is { Length: > 0 })
            {
                query.Append(" ORDER BY ");
                var orderConditions = request.order.Select(o => $"{o.column} {o.dir}");
                query.Append(string.Join(", ", orderConditions));
            }
            else
            {
                query.Append(" ORDER BY ");

                query.Append($"[{request.columns[0].data}] DESC");
            }

            query = query.Replace("Count(0) TotalCount", systemDataTableProfile.SelectQuery);

            res.WithoutPagnationQuery = query.ToString();


            query.Append($" OFFSET {request.start} ROWS FETCH NEXT {request.length} ROWS ONLY");

            res.MainQuery = query.ToString();

            return res;
    }

    private string BuildCriteriaQueryProfile(List<DataTableCriterion> criteria, string logic, DataTableRequest request , SystemDataTableProfile systemDataTableProfile)
        {
            var queryParts = new List<string>();
            foreach (var criterion in criteria)
            {
                if (criterion.criteria != null && criterion.criteria.Count > 0)
                {
                    queryParts.Add($"({BuildCriteriaQueryProfile(criterion.criteria, criterion.logic, request , systemDataTableProfile)})");
                }
                else if((criterion.value != null && criterion.value.Any()) || (criterion.condition == "null" || criterion.condition == "!null"))
                {
                    string condition = criterion.condition;

                    string part;
                    var valuePlaceholders = string.Join(",", criterion.value.Select(v => $"{v}"));

                    var colName = criterion.name;

                    var pc =
                        systemDataTableProfile
                            .SystemDataTableProfileSelectViewModels
                            .FirstOrDefault(c => c.Alliance == criterion.name);

                    var propAddress = $"[{pc.Label}].[{pc.PropName}]";

                    if (pc.Type == "shamsidatetime" || pc.Type == "shamsidate")
                    {
                        propAddress = $" dbo.fn_shmasiToMiladi([{pc.Label}].[{pc.PropName}]) ";
                    }

                    switch (condition)
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
                            part = $"{propAddress} BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
                            break;
                        case "!between":
                            part = $"{propAddress} NOT BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
                            break;
                        default:
                            throw new ArgumentException($"Unsupported operator: {criterion.condition}");
                    }
                    queryParts.Add($"({part})");
                }

            }

            if(queryParts.Count() >0)
            return string.Join($" {logic} ", queryParts);
            else
            {
				return "";

			}
        }
    private string BuildCriteriaQuery(List<DataTableCriterion> criteria, string logic, DataTableRequest request)
        {
            var queryParts = new List<string>();
            foreach (var criterion in criteria)
            {
                if (criterion.criteria != null && criterion.criteria.Count > 0)
                {
                    queryParts.Add($"({BuildCriteriaQuery(criterion.criteria, criterion.logic, request)})");
                }
                else if((criterion.value != null && criterion.value.Any()) || (criterion.condition == "null" || criterion.condition == "!null"))
                {
                    string condition = criterion.condition;

                    string part;
                    var valuePlaceholders = string.Join(",", criterion.value.Select(v => $"{v}"));
                    var colName = criterion.name;
                    var column = request.columns.FirstOrDefault(c => c.name == criterion.name);
                    var alince = "";
                    var tableName = request.TableName;

                    if (column.tableName.HasValue(true))
                    {
                        tableName = column.tableName;
                    }
                    if (column.type == "entity")
                    {
                        var parts = column.name.Split('.');
                        alince = TablesCounters.FirstOrDefault(c => c.RelatedTablePropertyName == parts[0]).Alias;
                        colName = parts[1];


                    }
                    else
                    {
                        alince = TablesCounters.FirstOrDefault(c => c.TableName == tableName).Alias;
                    }

                    var propAddress = $"[{alince}].[{colName}]";

                    if (column.type == "shamsidatetime" || column.type == "shamsidate")
                    {
                        propAddress = $" dbo.fn_shmasiToMiladi([{alince}].[{colName}]) ";
                    }


                    switch (condition)
                    {
                        case "=":
                            part = $"{propAddress} = N'{valuePlaceholders}'";
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
                            part = $"{propAddress} BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
                            break;
                        case "!between":
                            part = $"{propAddress} NOT BETWEEN N'{criterion.value[0]}' And N'{criterion.value[1]}'";
                            break;
                        default:
                            throw new ArgumentException($"Unsupported operator: {criterion.condition}");
                    }
                    queryParts.Add($"({part})");
                }

            }

            if(queryParts.Count() >0)
            return string.Join($" {logic} ", queryParts);
            else
            {
				return "";

			}
        }

    }
}

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
using System.Text;
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
			UseWesternDigitStyle(workbook);

			if (request.displayExport != null)
			{
				WriteDisplayExport(workbook.Worksheets[0], request.displayExport);
				workbook.Save(outputStream, SaveFormat.Xlsx);
				return;
			}

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
				worksheet.Cells[0, i].PutValue(ToWesternExcelText(col.title), false);
			}

			int row = 1;
	
			await AddChunkDataToWorksheet(worksheet, firstChunk.Rows, row, request, orderedHeaders);
			row += firstChunk.Rows.Count();

			workbook.Save(outputStream, SaveFormat.Xlsx);
			await Task.CompletedTask;
		}


		private static void WriteDisplayExport(Worksheet worksheet, DataTableDisplayExport export)
		{
			if (export.Headers == null || export.Rows == null || export.Headers.Count == 0
				|| export.Headers.Count > 16384 || export.Rows.Count > 1048575)
				throw new ArgumentException("ابعاد خروجی اکسل معتبر نیست.");

			worksheet.Name = "LargeData";
			worksheet.DisplayRightToLeft = true;
			for (var column = 0; column < export.Headers.Count; column++)
				worksheet.Cells[0, column].PutValue(ToWesternExcelText(export.Headers[column]), false);

			// فقط ستونی بولین می‌شود که همه مقادیر غیرخالی آن دقیقاً TRUE یا FALSE باشند.
			var booleanColumns = new bool[export.Headers.Count];
			var booleanColumnHasValue = new bool[export.Headers.Count];
			for (var column = 0; column < booleanColumns.Length; column++)
				booleanColumns[column] = true;

			foreach (var values in export.Rows)
			{
				if (values == null || values.Count != export.Headers.Count)
					throw new ArgumentException("تعداد ستون‌های خروجی اکسل یکسان نیست.");

				for (var column = 0; column < values.Count; column++)
				{
					if (!booleanColumns[column])
						continue;

					var value = values[column];
					if (value?.Number != null)
					{
						booleanColumns[column] = false;
						continue;
					}

					var text = value?.Text?.Trim();
					if (string.IsNullOrEmpty(text))
						continue;

					if (text == "TRUE" || text == "FALSE")
						booleanColumnHasValue[column] = true;
					else
						booleanColumns[column] = false;
				}
			}

			for (var row = 0; row < export.Rows.Count; row++)
			{
				var values = export.Rows[row];
				if (values == null || values.Count != export.Headers.Count)
					throw new ArgumentException("تعداد ستون‌های خروجی اکسل یکسان نیست.");

				for (var column = 0; column < values.Count; column++)
				{
					var value = values[column];

					if (booleanColumns[column] && booleanColumnHasValue[column]
						&& value?.Text?.Trim() is { Length: > 0 } boolText)
					{
						worksheet.Cells[row + 1, column].PutValue(boolText == "TRUE");
						continue;
					}

					if (value?.Number is double number)
					{
						if (!double.IsFinite(number))
							throw new ArgumentException("مقدار سلول خروجی اکسل معتبر نیست.");
						PutWesternNumber(worksheet.Cells[row + 1, column], number);
						continue;
					}

					var text = ToWesternExcelText(value?.Text);
					// رشته خالی سلول متنی می‌سازد؛ اکسل آن را Blank نمی‌داند و توابع #VALUE! می‌دهند.
					if (string.IsNullOrWhiteSpace(text))
						continue;
					if (TryPutExcelDate(worksheet.Cells[row + 1, column], text))
						continue;
					if (text.Length > 32767)
						throw new ArgumentException("متن یک سلول از ظرفیت اکسل بیشتر است.");
					// متن معمولی، صفر ابتدایی و عبارت شبیه فرمول را عیناً متن نگه دار.
					worksheet.Cells[row + 1, column].PutValue(text, false);
				}
			}
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

					var colType = (colInfo.type ?? string.Empty).Trim().ToLowerInvariant();

					// ستون اعشاری: اگر بخش اعشار صفر باشد به‌صورت عدد صحیح، وگرنه با اعشار معنادار
					if (colType == "decimal"
					    && putValue != null
					    && putValue != DBNull.Value
					    && TryPutDecimalExcelValue(worksheet.Cells[row, colIndex], putValue))
					{
						// مقدار عددی در سلول قرار گرفت
					}
					else if (putValue is DateTime dateTime)
					{
						PutExcelDate(worksheet.Cells[row, colIndex], dateTime, dateTime.TimeOfDay != TimeSpan.Zero);
					}
					else if (putValue != null && putValue != DBNull.Value)
					{
						var text = ToWesternExcelText(putValue.ToString());
						if (!string.IsNullOrWhiteSpace(text)
						    && !TryPutExcelDate(worksheet.Cells[row, colIndex], text))
						{
							var cell = worksheet.Cells[row, colIndex];
							cell.PutValue(text);
							if (cell.Type == CellValueType.IsNumeric)
								ApplyWesternNumberFormat(cell);
						}
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
					PutWesternNumber(cell, i);
					return true;
				case long l when l >= int.MinValue && l <= int.MaxValue:
					PutWesternNumber(cell, (int)l);
					return true;
				case long l:
					PutWesternNumber(cell, Convert.ToDouble(l));
					return true;
				case short s16:
					PutWesternNumber(cell, (int)s16);
					return true;
				case byte b:
					PutWesternNumber(cell, (int)b);
					return true;
				default:
					var s = NormalizeWesternDigits(value.ToString())?.Trim();
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
				PutWesternNumber(cell, decimal.ToInt32(d));
			else
				PutWesternNumber(cell, Convert.ToDouble(d));

			return true;
		}

		private static readonly string[] IsoDateFormats =
		[
			"yyyy-MM-dd",
			"yyyy-MM-dd HH:mm:ss",
			"yyyy-MM-ddTHH:mm:ss",
			"yyyy-MM-dd HH:mm:ss.FFFFFFF",
			"yyyy-MM-ddTHH:mm:ss.FFFFFFF"
		];

		/// <summary>
		/// تاریخ میلادی ISO را به‌صورت عدد تاریخ اکسل می‌نویسد تا تفریق و توابع تاریخ کار کنند.
		/// تاریخ شمسی (با /) متن می‌ماند.
		/// </summary>
		private static bool TryPutExcelDate(Cell cell, string text)
		{
			var value = text.Trim();
			if (value.Length < 10 || value.Length > 30 || value[4] != '-')
				return false;

			if (!DateTime.TryParseExact(value, IsoDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
				return false;
			if (date.Year < 1900 || date.Year > 9999)
				return false;

			var hasTime = value.Length > 10 && value.IndexOf(':', 10) >= 0;
			PutExcelDate(cell, date, hasTime);
			return true;
		}

		private const string WesternNumberFormat = "[$-010409]General";
		private const string WesternDateFormat = "[$-010409]yyyy/mm/dd";
		private const string WesternDateTimeFormat = "[$-010409]yyyy/mm/dd hh:mm:ss";

		private static void UseWesternDigitStyle(Workbook workbook)
		{
			var style = workbook.DefaultStyle;
			style.Custom = WesternNumberFormat;
			workbook.DefaultStyle = style;
		}

		private static void PutWesternNumber(Cell cell, double number)
		{
			cell.PutValue(number);
			ApplyWesternNumberFormat(cell);
		}

		private static void ApplyWesternNumberFormat(Cell cell)
		{
			var style = cell.GetStyle();
			style.Custom = WesternNumberFormat;
			cell.SetStyle(style);
		}

		private static string? NormalizeWesternDigits(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			var chars = text.ToCharArray();
			var changed = false;
			for (var i = 0; i < chars.Length; i++)
			{
				var c = chars[i];
				if (c is >= '\u06F0' and <= '\u06F9')
				{
					chars[i] = (char)('0' + (c - '\u06F0'));
					changed = true;
				}
				else if (c is >= '\u0660' and <= '\u0669')
				{
					chars[i] = (char)('0' + (c - '\u0660'));
					changed = true;
				}
				else if (c == '\u066C')
				{
					chars[i] = ',';
					changed = true;
				}
				else if (c == '\u066B')
				{
					chars[i] = '.';
					changed = true;
				}
			}

			return changed ? new string(chars) : text;
		}

		/// <summary>
		/// رقم‌های فارسی و عربی را انگلیسی می‌کند. اگر عدد بعد از متن راست‌به‌چپ باشد
		/// یک علامت چپ‌به‌راست قبلش می‌گذارد تا اکسل دوباره آن را فارسی نشان ندهد.
		/// </summary>
		private static string ToWesternExcelText(string? text)
		{
			text = NormalizeWesternDigits(text);
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			var rtlContext = false;
			var sb = new StringBuilder(text.Length);
			foreach (var c in text)
			{
				if (c is >= '0' and <= '9')
				{
					if (rtlContext)
					{
						sb.Append('\u200E');
						rtlContext = false;
					}

					sb.Append(c);
					continue;
				}

				sb.Append(c);
				if (IsRtlLetter(c))
					rtlContext = true;
				else if (!IsDigitContextNeutral(c))
					rtlContext = false;
			}

			return sb.ToString();
		}

		private static bool IsRtlLetter(char c) =>
			c is >= '\u0600' and <= '\u06FF'
				or >= '\u0750' and <= '\u077F'
				or >= '\u08A0' and <= '\u08FF'
				or >= '\uFB50' and <= '\uFDFF'
				or >= '\uFE70' and <= '\uFEFF';

		private static bool IsDigitContextNeutral(char c) =>
			char.IsWhiteSpace(c)
			|| c is '/' or '-' or ':' or '.' or ',' or '،' or '\u200C' or '\u200D' or '\u200E' or '\u200F';

		private static void PutExcelDate(Cell cell, DateTime date, bool hasTime)
		{
			cell.PutValue(date);
			var style = cell.GetStyle();
			style.Custom = hasTime ? WesternDateTimeFormat : WesternDateFormat;
			cell.SetStyle(style);
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

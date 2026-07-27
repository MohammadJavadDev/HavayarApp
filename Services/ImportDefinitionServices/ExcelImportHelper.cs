using Aspose.Cells;
using Common.Attributes;
using Common.Utilities;
using Data.SystemAuth;
using Entities.Base;
using Entities.Base.ImportDefinitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Xml;
using ValidationType = Aspose.Cells.ValidationType;

namespace Services.ImportDefinitionServices
{
	public class ExcelImportHelper
	{


		// -------------------------------------------------------
		// تولید نمونه اکسل برای دانلود
		// -------------------------------------------------------
		public static byte[] GenerateSampleExcel(ImportDefinition definition)
		{
			var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Aspose.Total.NET.lic");

			var wb = new Workbook();

			if (!wb.IsLicensed)
				new License().SetLicense(licensePath);

			var ws = wb.Worksheets[0];
			ws.Name = "نمونه";

			var columns = definition.Columns.JsonDeserialize<ImportDefinitionColumn[]>();

			// راست به چپ برای فارسی
			ws.DisplayRightToLeft = true;

			// فقط ستون‌های غیر سیستمی در اکسل نمایش می‌یابند
			var excelColumns = columns
			    .Where(c => !c.IsSystemVariable)
			    .OrderBy(c => c.SortOrder)
			    .ToList();

			// -------------------------------------------------------
			// استایل هدر: آبی با فونت سفید و Bold
			// -------------------------------------------------------
			var headerStyle = wb.CreateStyle();
			headerStyle.Font.IsBold = true;
			headerStyle.Font.Color = Color.White;
			headerStyle.ForegroundColor = Color.FromArgb(0xFF, 0x7B, 0x8C, 0xA0);
			headerStyle.Pattern = BackgroundType.Solid;
			headerStyle.HorizontalAlignment = TextAlignmentType.Center;
			headerStyle.VerticalAlignment = TextAlignmentType.Center;
			headerStyle.IsTextWrapped = false;
			headerStyle.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Medium;
			headerStyle.Borders[BorderType.BottomBorder].Color = Color.FromArgb(0x1D, 0x4E, 0xD8);

			var headerFlag = new StyleFlag
			{
				Font = true,
				HorizontalAlignment = true,
				VerticalAlignment = true,
				Borders = true,
			};

			// -------------------------------------------------------
			// استایل سطر نمونه: پس‌زمینه خاکستری روشن
			// -------------------------------------------------------
			var sampleStyle = wb.CreateStyle();
			sampleStyle.ForegroundColor = Color.FromArgb(0xF1, 0xF5, 0xF9);
			sampleStyle.Pattern = BackgroundType.Solid;
			sampleStyle.Font.Color = Color.FromArgb(0x64, 0x74, 0x8B);
			sampleStyle.Font.IsItalic = true;

			var sampleFlag = new StyleFlag { Font = true };

			// -------------------------------------------------------
			// نوشتن هدرها
			// -------------------------------------------------------
			for (int i = 0; i < excelColumns.Count; i++)
			{
				var col = excelColumns[i];
				var cell = ws.Cells[0, i];

				// عنوان: الزامی‌ها با ستاره
				cell.PutValue(col.IsRequired
				    ? col.DisplayName + " *"
				    : col.DisplayName);

				cell.SetStyle(headerStyle);

				// ارتفاع ردیف هدر
				ws.Cells.SetRowHeightPixel(0, 36);

				// اعتبارسنجی نوع داده در اکسل
				ApplyCellValidation(wb, ws, i, col.DataType);
			}

			// -------------------------------------------------------
			// ردیف نمونه (ردیف ۲)
			// -------------------------------------------------------
			for (int i = 0; i < excelColumns.Count; i++)
			{
				var cell = ws.Cells[1, i];
				cell.PutValue(GetSampleValue(excelColumns[i].DataType));
				cell.SetStyle(sampleStyle);
			}

			// -------------------------------------------------------
			// تنظیم عرض ستون‌ها (AutoFit)
			// -------------------------------------------------------
			for (int i = 0; i < excelColumns.Count; i++)
			{
				if (ws.Cells.GetColumnWidthPixel(i) < 120)
					ws.Cells.SetColumnWidthPixel(i, 150);
			}

			// =======================================================
			// بخش جدید: ایجاد شیت راهنما برای گزینه‌های هر ستون
			// =======================================================
			var options = columns.Where(c => c.OptionSettings != null &&
									   (c.OptionSettings.TypeOption == TypeOptionEnum.Defination ||
									    c.OptionSettings.TypeOption == TypeOptionEnum.System))
							 .ToArray();

			if (options.Any())
			{
				CreateOptionsSheet(wb, options, excelColumns);
			}

			// Freeze ردیف هدر
			ws.FreezePanes(1, 0, 1, excelColumns.Count);

			// ذخیره در MemoryStream
			using var ms = new MemoryStream();
			wb.Save(ms, SaveFormat.Xlsx);
			return ms.ToArray();
		}

		/// <summary>
		/// ایجاد شیت راهنما برای گزینه‌های هر ستون
		/// </summary>
		private static void CreateOptionsSheet(Workbook wb, ImportDefinitionColumn[] options, List<ImportDefinitionColumn> excelColumns)
		{
			 

			var optionsSheet = wb.Worksheets.Add("راهنمای مقادیر مجاز");
			optionsSheet.DisplayRightToLeft = true;

			// استایل هدر شیت راهنما
			var guideHeaderStyle = wb.CreateStyle();
			guideHeaderStyle.Font.IsBold = true;
			guideHeaderStyle.Font.Color = Color.White;
			guideHeaderStyle.ForegroundColor = Color.FromArgb(0x2C, 0x3E, 0x50);
			guideHeaderStyle.Pattern = BackgroundType.Solid;
			guideHeaderStyle.HorizontalAlignment = TextAlignmentType.Center;
			guideHeaderStyle.Font.Size = 12;

			var guideHeaderFlag = new StyleFlag
			{
				Font = true,
				HorizontalAlignment = true,
			 
			};

			// استایل برای ردیف‌های داده
			var dataStyle = wb.CreateStyle();
			dataStyle.VerticalAlignment = TextAlignmentType.Center;

			var dataStyleFlag = new StyleFlag
			{
				VerticalAlignment = true
			};

			// نوشتن هدرهای شیت راهنما
			optionsSheet.Cells[0, 0].PutValue("نام ستون");
			optionsSheet.Cells[0, 1].PutValue("مقدار");
			optionsSheet.Cells[0, 2].PutValue("عنوان نمایشی");
			optionsSheet.Cells[0, 3].PutValue("توضیحات");

			// تنظیم استایل برای ردیف اول
			for (int i = 0; i < 4; i++)
			{
				optionsSheet.Cells[0, i].SetStyle(guideHeaderStyle);
			}

			// تنظیم عرض ستون‌ها
			optionsSheet.Cells.SetColumnWidthPixel(0, 150); // نام ستون
 
			optionsSheet.Cells.SetColumnWidthPixel(1, 100); // مقدار
			optionsSheet.Cells.SetColumnWidthPixel(2, 200); // عنوان نمایشی
			optionsSheet.Cells.SetColumnWidthPixel(3, 300); // توضیحات

			int currentRow = 1;

			foreach (var column in options)
			{
				var columnName = excelColumns.FirstOrDefault(c => c.ColumnName == column.ColumnName)?.DisplayName ?? column.ColumnName;
				List<SelectOptions> optionList = new List<SelectOptions>();

				// دریافت لیست گزینه‌ها بر اساس نوع
				if (column.OptionSettings.TypeOption == TypeOptionEnum.Defination)
				{
					// حالت Definition: از ListOptions استفاده می‌شود
					optionList = column.OptionSettings.ListOptions ?? new List<SelectOptions>();

					// اضافه کردن ردیف عنوان ستون
					var titleRow = currentRow;
					optionsSheet.Cells[titleRow, 0].PutValue(columnName);
 
					optionsSheet.Cells[titleRow, 1].PutValue("");
					optionsSheet.Cells[titleRow, 2].PutValue($"گزینه‌های مجاز برای ستون {columnName}");
					optionsSheet.Cells[titleRow, 3].PutValue("از لیست زیر می‌توانید مقادیر مجاز را انتخاب کنید");

					// استایل توضیحات
					var descStyle = wb.CreateStyle();
					descStyle.Font.IsItalic = true;
					descStyle.Font.Color = Color.FromArgb(0x7F, 0x8C, 0x8D);
					optionsSheet.Cells[titleRow, 2].SetStyle(descStyle);
					optionsSheet.Cells[titleRow, 3].SetStyle(descStyle);

					currentRow++;

					// نوشتن گزینه‌ها
					foreach (var opt in optionList)
					{
						optionsSheet.Cells[currentRow, 0].PutValue("");
 
						optionsSheet.Cells[currentRow, 1].PutValue(opt.Value);
						optionsSheet.Cells[currentRow, 2].PutValue(opt.Name);
						optionsSheet.Cells[currentRow, 3].PutValue("");

						for (int i = 0; i < 4; i++)
						{
							optionsSheet.Cells[currentRow, i].SetStyle(dataStyle);
						}

						currentRow++;
					}
				}
				else if (column.OptionSettings.TypeOption == TypeOptionEnum.System)
				{
					// حالت System: مقادیر از Enum سیستم دریافت می‌شود
					var enumType = EnumExtensions.GetEnumValuesWithDisplayNamesByTypeName(column.OptionSettings.SystemTypeName);
					if (enumType != null)
					{
						var enumValues = enumType;

						// اضافه کردن ردیف عنوان ستون
						optionsSheet.Cells[currentRow, 0].PutValue("---");
						optionsSheet.Cells[currentRow, 1].PutValue("");
						optionsSheet.Cells[currentRow, 2].PutValue($"مقادیر مجاز");
						optionsSheet.Cells[currentRow, 3].PutValue("مقادیر از پیش تعریف شده سیستم");

						// استایل توضیحات
						var descStyle = wb.CreateStyle();
						descStyle.Font.IsItalic = true;
						descStyle.Font.Color = Color.FromArgb(0x7F, 0x8C, 0x8D);
						descStyle.Font.IsBold = true;
 
						descStyle.ForegroundColor = Color.FromArgb(169, 208, 142);
						descStyle.Pattern = BackgroundType.Solid;

						optionsSheet.Cells[currentRow, 0].SetStyle(descStyle);
						optionsSheet.Cells[currentRow, 1].SetStyle(descStyle);
						optionsSheet.Cells[currentRow, 2].SetStyle(descStyle);
						optionsSheet.Cells[currentRow, 3].SetStyle(descStyle);

						currentRow++;

						// نوشتن گزینه‌های Enum
						foreach (var enumValue in enumValues)
						{
							var intValue = enumValue.Value;
							var displayName = enumValue.Text;

							optionsSheet.Cells[currentRow, 0].PutValue(columnName);
							optionsSheet.Cells[currentRow, 1].PutValue(intValue);
							optionsSheet.Cells[currentRow, 2].PutValue(displayName);
							optionsSheet.Cells[currentRow, 3].PutValue($"مقدار مجاز سیستم (شناسه: {intValue})");

							for (int i = 0; i < 4; i++)
							{
								optionsSheet.Cells[currentRow, i].SetStyle(dataStyle);
							}

							currentRow++;
						}
					}
					else
					{
						// اگر Enum پیدا نشد
						optionsSheet.Cells[currentRow, 0].PutValue(columnName);
						optionsSheet.Cells[currentRow, 1].PutValue("خطا");
						optionsSheet.Cells[currentRow, 2].PutValue($"Enum یافت نشد: {column.OptionSettings.SystemTypeName}");
						optionsSheet.Cells[currentRow, 3].PutValue("لطفاً با مدیر سیستم تماس بگیرید");
						currentRow++;
					}
				}

				// اضافه کردن یک ردیف خالی بین ستون‌ها
				currentRow++;
			}

			// تنظیم ارتفاع ردیف‌ها
			optionsSheet.Cells.SetRowHeightPixel(0, 30);

			// Freeze سطر اول شیت راهنما
			optionsSheet.FreezePanes(1, 0, 1, 5);

			 
		}

		// -------------------------------------------------------
		// اعتبارسنجی سلول‌ها بر اساس نوع داده (Data Validation)
		// -------------------------------------------------------
		private static void ApplyCellValidation(Workbook wb, Worksheet ws, int colIndex, SystemType dataType)
		{
			if (dataType is SystemType.String or SystemType.Boolean) return;   // متن و بله/خیر نیاز به validation ندارند

			var ca = new CellArea
			{
				StartRow = 1,
				StartColumn = colIndex,
				EndRow = 10000,
				EndColumn = colIndex
			};

			int valIdx = ws.Validations.Add(ca);
			var val = ws.Validations[valIdx];

			switch (dataType)
			{
				case SystemType.Int:
					val.Type = ValidationType.WholeNumber;
					val.Operator = OperatorType.Between;
					val.Formula1 = "-2147483648";
					val.Formula2 = "2147483647";
					val.ErrorMessage = "لطفاً یک عدد صحیح وارد کنید.";
					val.ErrorTitle = "خطای ورودی";
					val.ShowError = true;
					break;

				case SystemType.Decimal:
					val.Type = ValidationType.Decimal;
					val.Operator = OperatorType.Between;
					val.Formula1 = "-999999999";
					val.Formula2 = "999999999";
					val.ErrorMessage = "لطفاً یک عدد اعشاری وارد کنید.";
					val.ErrorTitle = "خطای ورودی";
					val.ShowError = true;
					break;

				case SystemType.Date:
				case SystemType.DateTime:
					val.Type = ValidationType.Date;
					val.ErrorMessage = "لطفاً یک تاریخ معتبر وارد کنید (مثال: 2024-01-15)";
					val.ErrorTitle = "خطای ورودی";
					val.ShowError = true;
					break;
				case SystemType.DateTimeShamsi:
				case SystemType.DateShamsi:
					val.Type = ValidationType.Date;
					val.ErrorMessage = "لطفاً یک تاریخ معتبر وارد کنید (مثال: 1405/02/31 23:32:07)";
					val.ErrorTitle = "خطای ورودی";
					val.ShowError = true;
					break;
			}
		}

		// -------------------------------------------------------
		// مقدار نمونه بر اساس نوع داده
		// -------------------------------------------------------
		private static string GetSampleValue(SystemType dataType) => dataType switch
		{
			SystemType.Int => "123",
			SystemType.Long => "123",
			SystemType.Select => "0",
			SystemType.Decimal => "12.50",
			SystemType.Date => DateTime.Today.ToString("yyyy-MM-dd"),
			SystemType.DateTime => DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
			SystemType.DateShamsi => DateTime.Today.ToShamsiDate(),
			SystemType.DateTimeShamsi => DateTime.Now.ToShamsiDateTime(),
			SystemType.Boolean=> "true",
			_ => "نمونه متن"
		};

		// -------------------------------------------------------
		// خواندن هدرهای اکسل (برای AJAX GetExcelHeaders)
		// -------------------------------------------------------
		public static List<string> ReadExcelHeaders(Stream stream )
		{
			var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Aspose.Total.NET.lic");
			var wb = new Workbook(stream);
			 

			if (!wb.IsLicensed)
				new License().SetLicense(licensePath);
			var ws = wb.Worksheets[0];
			var maxCol = ws.Cells.MaxDataColumn;   // 0-based
			var headers = new List<string>();

			for (int c = 0; c <= maxCol; c++)
			{
				var val = ws.Cells[0, c].StringValue?.Trim();
				if (!string.IsNullOrEmpty(val))
					headers.Add(val);
			}
			return headers;
		}

		// -------------------------------------------------------
		// خواندن کامل اکسل → List<Dictionary<header, value>>
		// -------------------------------------------------------
		public static List<Dictionary<string, string>> ReadExcel(Stream stream)
		{
			var wb = new Workbook(stream);
			var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Aspose.Total.NET.lic");

			if (!wb.IsLicensed)
				new License().SetLicense(licensePath);
			var ws = wb.Worksheets[0];
			var maxRow = ws.Cells.MaxDataRow;    // 0-based (آخرین ردیف با داده)
			var maxCol = ws.Cells.MaxDataColumn; // 0-based

			// ردیف صفر = هدر
			var headers = new Dictionary<int, string>();
			for (int c = 0; c <= maxCol; c++)
			{
				var h = ws.Cells[0, c].StringValue?.Trim();
				if (!string.IsNullOrEmpty(h))
					headers[c] = h;
			}

			var rows = new List<Dictionary<string, string>>();

			// از ردیف ۱ به بعد = داده
			for (int r = 1; r <= maxRow; r++)
			{
				var row = new Dictionary<string, string>();
				bool hasData = false;

				foreach (var kv in headers)
				{
					// StringValue مقدار سلول را همیشه به صورت string برمی‌گرداند
					// حتی برای سلول‌های عددی یا تاریخ
					var cell = ws.Cells[r, kv.Key];
					string val;

					// تاریخ را به فرمت استاندارد تبدیل می‌کنیم
					if (cell.Type == CellValueType.IsDateTime)
						val = cell.DateTimeValue.ToString("yyyy-MM-dd HH:mm");
					else
						val = cell.StringValue?.Trim() ?? "";

					row[kv.Value] = val;
					if (!string.IsNullOrEmpty(val)) hasData = true;
				}

				// ردیف‌های کاملاً خالی را نادیده می‌گیریم
				if (hasData) rows.Add(row);
			}

			return rows;
		}
	}

	// اجراکننده کوئری برای هر ردیف اکسل
	public class ImportExecutor
	{
		private readonly string _connectionString;
		private readonly ISdk _sdk;

		private  readonly Dictionary<string, object> _systemVariables ;

		public ImportExecutor(string connectionString , ISdk sdk)
		{
			_connectionString = connectionString;
			_sdk = sdk;

			_systemVariables = new()
			{
			    { "CurrentUserId", sdk.CurrentUser.Id },
			    { "CurrentDateMiladi", DateTime.Today },
			    { "CurrentDateTimeMiladi", DateTime.Now },
			    { "CurrentDateTimeShamsi", DateTime.Now.ToShamsiDateTime() },
			    { "CurrentDateShamsi", DateTime.Now.ToShamsiDate() },
			    { "CurrentUserName", sdk.CurrentUser.FullName },
			};
		}

		public ImportLog ExecuteImport(
		    ImportDefinition definition,
		    List<Dictionary<string, string>> excelRows,
		    Dictionary<string, string> columnMapping,
		    bool rollbackOnError = false)
		{
			var log = new ImportLog
			{
				ImportDefinitionId = definition.Id.Value,
				TotalRows = excelRows.Count,
				CreatedById = _sdk.CurrentUser.Id
			};

			var columns = definition.Columns.JsonDeserialize<ImportDefinitionColumn[]>();

			foreach (var column in columns.Where(c=>c.DataType == SystemType.Select && c.OptionSettings.TypeOption == TypeOptionEnum.System)) {
				column.OptionSettings.ListOptions = EnumExtensions
					.GetEnumValuesWithDisplayNamesByTypeName(column.OptionSettings.SystemTypeName)
					.Select(c=> new SelectOptions() {Name = c.Text, Value = c.Value }).ToList();
			}

			var details = new List<ImportLogDetail>();

			using var conn = new SqlConnection(_connectionString);
			conn.Open();

			SqlTransaction transaction = null;
			if (rollbackOnError)
				transaction = conn.BeginTransaction();

			try
			{
				for (int rowIdx = 0; rowIdx < excelRows.Count; rowIdx++)
				{
					var excelRow = excelRows[rowIdx];
					var detail = new ImportLogDetail { RowNumber = rowIdx + 2 };

					try
					{
						using var cmd = new SqlCommand(definition.SqlQuery, conn);
						if (transaction != null)
							cmd.Transaction = transaction;

						cmd.Parameters.Clear();

						foreach (var p in _systemVariables)
						{
							cmd.Parameters.AddWithValue("@" + p.Key, p.Value);
						}

						foreach (var col in columns)
						{
							object paramValue = DBNull.Value;

							// پیدا کردن ستون در اکسل از طریق mapping
							var excelHeader = columnMapping.ContainsKey(col.ColumnName)
							    ? columnMapping[col.ColumnName]
							    : col.DisplayName;

							if (excelRow.ContainsKey(excelHeader) && !string.IsNullOrWhiteSpace(excelRow[excelHeader]))
							{
								paramValue = ConvertValue(excelRow[excelHeader], col.DataType, col);
							}
							else if (col.IsRequired)
							{
								throw new Exception($"ستون «{col.DisplayName}» اجباری است و مقدار ندارد.");
							}

							cmd.Parameters.AddWithValue("@" + col.ColumnName, paramValue);
						}

						cmd.ExecuteNonQuery();
						detail.Success = true;
						log.SuccessRows++;
						details.Add(detail);
					}
					catch (Exception ex)
					{
						detail.Success = false;
						detail.ErrorMessage = ex.Message;
						details.Add(detail);

						if (rollbackOnError)
						{
							transaction?.Rollback();
							transaction = null;
							log.RolledBack = true;

							foreach (var prev in details.Where(d => d.Success))
							{
								prev.Success = false;
								prev.ErrorMessage = $"به دلیل خطا در ردیف {detail.RowNumber}، تغییرات این ردیف برگشت داده شد.";
							}

							log.SuccessRows = 0;

							for (int skipIdx = rowIdx + 1; skipIdx < excelRows.Count; skipIdx++)
							{
								details.Add(new ImportLogDetail
								{
									RowNumber = skipIdx + 2,
									Success = false,
									ErrorMessage = "به دلیل خطای قبلی پردازش نشد."
								});
							}

							log.FailedRows = details.Count(d => !d.Success);
							break;
						}

						log.FailedRows++;
					}
				}

				transaction?.Commit();
			}
			catch
			{
				transaction?.Rollback();
				throw;
			}
			finally
			{
				transaction?.Dispose();
			}

			log.Details = details;
			return log;
		}
 

		private static object ConvertValue(string value, SystemType dataType , ImportDefinitionColumn col)
		{

			 
			if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
			return dataType switch
			{
				SystemType.Int=> int.TryParse(value, out int i) ? (object)i
						   : throw new Exception($"مقدار «{value}» عدد صحیح نیست."),
				SystemType.Long => long.TryParse(value, out long i) ? (object)i
				   : throw new Exception($"مقدار «{value}» عدد صحیح نیست."),
				SystemType.Decimal => decimal.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any,
						   System.Globalization.CultureInfo.InvariantCulture, out decimal d) ? (object)d
						   : throw new Exception($"مقدار «{value}» عدد اعشاری نیست."),
				SystemType.Date => DateTime.TryParse(value, out DateTime dt) ? (object)dt
						   : throw new Exception($"مقدار «{value}» تاریخ معتبر نیست."),
				SystemType.DateTime => DateTime.TryParse(value, out DateTime dtt) ? (object)dtt
						   : throw new Exception($"مقدار «{value}» تاریخ/ساعت معتبر نیست."),
				SystemType.Boolean => value.ToLower() is "true" or "yes" or "1" or "بله" ? (object)true
						   : value.ToLower() is "false" or "no" or "0" or "خیر" ? false
						   : throw new Exception($"مقدار «{value}» برای بله/خیر معتبر نیست."),
				SystemType.Select => ConvertSelectValue(value, col),
				_ => value
			};
		}
		private static object ConvertSelectValue(string value, ImportDefinitionColumn col)
		{
			// بررسی اینکه OptionSetting وجود داشته باشد
			if (col.OptionSettings == null)
				throw new Exception($"ستون {col.DisplayName} از نوع Select است اما OptionSetting ندارد.");

			// تلاش برای تبدیل به int
			if (int.TryParse(value, out int intValue))
			{
				// اگر مقدار عددی بود، بررسی کنیم که در لیست گزینه‌ها وجود داشته باشد
				var exists = col.OptionSettings.ListOptions.Any(x => x.Value == intValue);
				if (!exists)
					throw new Exception($"مقدار عددی {value} در لیست گزینه‌های مجاز برای ستون {col.DisplayName} وجود ندارد.");

				return intValue;
			}

			// اگر مقدار string بود، مقدار Value را از Name پیدا کن
			var option = col.OptionSettings.ListOptions
			    .FirstOrDefault(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

			if (option == null)
				throw new Exception($"مقدار «{value}» در لیست گزینه‌های مجاز برای ستون {col.DisplayName} یافت نشد.");

			return option.Value;
		}
	}
}

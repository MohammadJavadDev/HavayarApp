using Microsoft.Extensions.Logging;
using ReportBuilder.Services;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Dictionary;

namespace ReportBuilder.WebApp
{
	/// <summary>
	/// ثبت فونت‌های IRANSansWeb(FaNum) در StiFontCollection.
	/// 
	/// دلیل وجود این کلاس: خروجی PDF / چاپ استیمول‌سافت (PdfFonts.GetFontMetrics) بایت فونت را فقط از
	/// StiFontCollection.GetCustomFontData می‌گیرد. اگر فونت در پروسهٔ سرور ثبت نشده باشد، آرایهٔ خالی برمی‌گردد و
	/// خطای «CMAP table not found» (فونت IRANSansWeb(FaNum) Medium) رخ می‌دهد.
	/// 
	/// قبلاً فونت‌ها از wwwroot/panelLib/fonts روی دیسک خوانده می‌شدند و هر خطا یا نبودن فایل بی‌صدا نادیده گرفته
	/// می‌شد (و حتی فلگ «ثبت شد» true می‌شد). اکنون فایل‌های TTF داخل خود اسمبلی (EmbeddedResource) هستند، پس به مسیر
	/// انتشار / IIS / Docker / دسترسی دیسک وابسته نیست، و ثبت تا موفق‌شدن تکرار می‌شود و لاگ می‌گیرد.
	/// </summary>
	public static class StimulsoftFontRegistry
	{
		/// <summary>نام فایل → نام خانوادهٔ واقعی داخل TTF (برای بررسی بعد از ثبت).</summary>
		private static readonly (string File, string Family)[] Catalog =
		[
			("IRANSansWeb.ttf", "IRANSansWeb(FaNum)"),
			("IRANSansWeb_Bold.ttf", "IRANSansWeb(FaNum)"),
			("IRANSansWeb_Medium.ttf", "IRANSansWeb(FaNum) Medium"),
			("IRANSansWeb_Light.ttf", "IRANSansWeb(FaNum) Light"),
			("IRANSansWeb_UltraLight.ttf", "IRANSansWeb(FaNum) UltraLight"),
		];

		private const string ResourcePrefix = "ReportBuilder.WebApp.EmbeddedFonts.";

		private static readonly object Sync = new();
		private static readonly Dictionary<string, byte[]> BytesCache = new(StringComparer.OrdinalIgnoreCase);
		private static bool _allRegistered;

		/// <summary>لیست فونت‌های Resource که به گزارش اضافه می‌شود (بایت‌ها از Embedded خوانده می‌شود).</summary>
		public static IReadOnlyList<ReportBuilderReportFonts> Fonts { get; } =
			Catalog.Select(c => new ReportBuilderReportFonts { Name = c.File, Type = StiResourceType.FontTtf }).ToList();

		/// <summary>بایت TTF از Embedded Resource؛ در صورت نبودن، از wwwroot/panelLib/fonts (سازگاری با قبل).</summary>
		public static byte[]? GetFontBytes(string fileName)
		{
			lock (Sync)
			{
				if (BytesCache.TryGetValue(fileName, out var cached))
					return cached;

				var bytes = ReadEmbedded(fileName) ?? ReadFromDisk(fileName);
				if (bytes is { Length: > 0 })
					BytesCache[fileName] = bytes;
				return bytes is { Length: > 0 } ? bytes : null;
			}
		}

		/// <summary>
		/// مطمئن می‌شود همهٔ فونت‌ها در StiFontCollection هستند. اگر نبودند (یا قبلاً ثبت ناموفق بوده) دوباره ثبت می‌کند.
		/// ارزان است و می‌تواند قبل از هر رندر / چاپ / خروجی صدا زده شود.
		/// </summary>
		public static void EnsureRegistered(ILogger? logger = null)
		{
			// PdfFonts بافر اولین چاپ را تا پایان پروسه نگه می‌دارد. اگر آن بافر TTF نبود
			// (فونت هنوز ثبت نشده و GDI دادهٔ جایگزین داده)، step #70 همان CMAP را تکرار می‌کند
			// حتی بعد از AddFontBytes. کلاس PdfFonts داخلی است؛ این سوییچ عمومی کش را خاموش می‌کند.
			StiOptions.Export.Pdf.AllowFontsCache = false;

			if (_allRegistered && AllPresent())
				return;

			lock (Sync)
			{
				if (_allRegistered && AllPresent())
					return;

				foreach (var (file, _) in Catalog)
				{
					// ثبت مجدد بی‌خطر است؛ StiFontCollection بر اساس hash محتوا تکراری اضافه نمی‌کند.
					var bytes = GetFontBytes(file);
					if (bytes == null)
					{
						logger?.LogError("Stimulsoft font {File} not found (embedded or disk).", file);
						continue;
					}

					try
					{
						// alias کوتاه برای گزارش‌هایی که Font=IRANSansWeb دارند
						var alias = file.Equals("IRANSansWeb.ttf", StringComparison.OrdinalIgnoreCase) ? "IRANSansWeb" : null;
						StiFontCollection.AddFontBytes(bytes, alias);
					}
					catch (Exception ex)
					{
						logger?.LogError(ex, "Stimulsoft font {File} registration failed.", file);
					}
				}

				var missing = Catalog.Where(c => !IsRegistered(c.Family)).Select(c => c.Family).Distinct().ToList();
				_allRegistered = missing.Count == 0;

				if (_allRegistered)
					logger?.LogInformation("Stimulsoft fonts registered ({Count} files).", Catalog.Length);
				else
					logger?.LogError("Stimulsoft fonts still missing after registration: {Missing}", string.Join(", ", missing));
			}
		}

		private static bool AllPresent() => Catalog.All(c => IsRegistered(c.Family));

		private static bool IsRegistered(string family)
		{
			try
			{
				var data = StiFontCollection.GetCustomFontData(family);
				// طول غیرصفر کافی نیست: بایت بسته‌شدهٔ Resource یا خروجی GDI هم Length دارند
				// و همان‌ها در GetFontMetrics با «CMAP table not found» می‌ترکند.
				return HasUnicodeCmap(data);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>TTF/OTF با جدول cmap. بافر خالی، woff و بستهٔ Stimulsoft رد می‌شوند.</summary>
		private static bool HasUnicodeCmap(byte[]? b)
		{
			if (b == null || b.Length < 12)
				return false;

			var isTtf = b[0] == 0x00 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00;
			var isTrue = b[0] == (byte)'t' && b[1] == (byte)'r' && b[2] == (byte)'u' && b[3] == (byte)'e';
			var isOtto = b[0] == (byte)'O' && b[1] == (byte)'T' && b[2] == (byte)'T' && b[3] == (byte)'O';
			if (!isTtf && !isTrue && !isOtto)
				return false;

			var tables = (b[4] << 8) | b[5];
			for (var i = 0; i < tables && 12 + 16 * i + 4 <= b.Length; i++)
			{
				var o = 12 + 16 * i;
				if (b[o] == (byte)'c' && b[o + 1] == (byte)'m' && b[o + 2] == (byte)'a' && b[o + 3] == (byte)'p')
					return true;
			}

			return false;
		}

		private static byte[]? ReadEmbedded(string fileName)
		{
			var asm = typeof(StimulsoftFontRegistry).Assembly;
			using var stream = asm.GetManifestResourceStream(ResourcePrefix + fileName);
			if (stream == null)
				return null;

			using var ms = new MemoryStream();
			stream.CopyTo(ms);
			return ms.ToArray();
		}

		private static byte[]? ReadFromDisk(string fileName)
		{
			var candidates = new[]
			{
				Path.Combine(AppContext.BaseDirectory, "wwwroot", "panelLib", "fonts", fileName),
				Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "panelLib", "fonts", fileName),
			};

			foreach (var path in candidates)
			{
				try
				{
					if (File.Exists(path))
						return File.ReadAllBytes(path);
				}
				catch
				{
					// مسیر بعدی
				}
			}

			return null;
		}
	}
}

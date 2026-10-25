using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Acc;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace App.BackgroundJob.Jobs.Acc;

/// <summary>
/// معادل HTS UpdateExchangeRateArchive / FetchExchangeRateArchiveFromTGJU.
/// هر روز نرخ آزاد دلار (PriceUnitId = 2) را از TGJU می‌خواند و روزهای بدون قیمت را با میانگین پر می‌کند.
/// </summary>
public sealed class DollarExchangeRateJob(
	IUnitOfWork unitOfWork,
	ApplicationDbContext db,
	IHttpClientFactory httpClientFactory)
{
	public const int DollarPriceUnitId = 2;
	public const string ErrorRecipient = "mirlohi.m@havayar.com";
	public const string GapComment = "بعلت عدم قیمت دهی توسط دولت، میانگین روزهای قبل و بعد گرفته شده است";

	private const int PageSize = 30;
	private const int MaxPages = 40;
	private const string IndicatorUrl = "https://api.tgju.org/v1/market/indicator/summary-table-data/price_dollar_rl";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	[JobHandler(
		"آپدیت نرخ ارز دلار",
		"نرخ آزاد دلار را از TGJU می‌خواند و در بایگانی نرخ ارز درج می‌کند. روزهای بدون قیمت با میانگین روز قبل و بعد پر می‌شوند.")]
	public async Task UpdateDollarExchangeRate(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
	{
		try
		{
			await UpdateDollarRatesAsync(jobLogger, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			await LogExceptionAsync(jobLogger, ex, cancellationToken);
			await TryQueueErrorEmailAsync(ex, jobLogger, cancellationToken);
			throw;
		}
	}

	private static Task LogInfoAsync(IJobLogger? jobLogger, string message, CancellationToken cancellationToken)
	{
		return jobLogger == null ? Task.CompletedTask : jobLogger.LogInfoAsync(message, cancellationToken);
	}

	private static Task LogWarningAsync(IJobLogger? jobLogger, string message, CancellationToken cancellationToken)
	{
		return jobLogger == null ? Task.CompletedTask : jobLogger.LogWarningAsync(message, 0, cancellationToken);
	}

	private static Task LogExceptionAsync(IJobLogger? jobLogger, Exception exception, CancellationToken cancellationToken)
	{
		return jobLogger == null ? Task.CompletedTask : jobLogger.LogExceptionAsync(exception, cancellationToken);
	}

	private async Task UpdateDollarRatesAsync(IJobLogger? jobLogger, CancellationToken cancellationToken)
	{
		var today = DateTime.Now.Date;
		await LogInfoAsync(jobLogger, $"شروع آپدیت نرخ آزاد دلار تا {today.ToShamsiDate()}", cancellationToken);

		var existing = await unitOfWork.Repository<ExchangeRateArchive>()
			.TableNoTracking
			.Where(x => x.PriceUnitId == DollarPriceUnitId && x.CreatedOnMiladiDateTime != null)
			.Select(x => new RatePoint(
				x.Id,
				x.CreatedOnMiladiDateTime!.Value.Date,
				x.FinalPrice))
			.ToListAsync(cancellationToken);

		var rates = new Dictionary<DateTime, int?>();
		foreach (var row in existing.OrderBy(x => x.Id))
			rates[row.Date] = row.FinalPrice;

		if (rates.Count == 0)
			throw new InvalidOperationException("ردیفی برای نرخ دلار در بایگانی نرخ ارز پیدا نشد.");

		var fromDate = rates.Keys.Max().AddDays(1);
		var inserted = new List<ExchangeRateArchive>();

		if (fromDate > today)
		{
			await LogInfoAsync(jobLogger, "نرخ دلار تا امروز در بایگانی موجود است.", cancellationToken);
		}
		else
		{
			for (var page = 0; page < MaxPages && fromDate <= today; page++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var batch = await FetchPageAsync(fromDate, today, cancellationToken);
				if (batch.Count == 0)
				{
					await LogInfoAsync(jobLogger, 
						$"از {fromDate.ToShamsiDate()} داده‌ای از TGJU برنگشت.",
						cancellationToken);
					break;
				}

				DateTime? maxInBatch = null;
				var addedInPage = 0;
				foreach (var element in batch)
				{
					var row = ParseQuote(element);
					if (row.CreatedOnMiladiDateTime is null)
						continue;

					var rateDate = row.CreatedOnMiladiDateTime.Value.Date;
					if (rateDate >= fromDate && (maxInBatch is null || rateDate > maxInBatch))
						maxInBatch = rateDate;
					if (rateDate < fromDate || rateDate > today || rates.ContainsKey(rateDate))
						continue;

					rates[rateDate] = row.FinalPrice;
					inserted.Add(row);
					addedInPage++;
				}

				await LogInfoAsync(jobLogger, 
					$"بازه {fromDate.ToShamsiDate()} تا {today.ToShamsiDate()}: {batch.Count} ردیف دریافت شد و {addedInPage} ردیف جدید است.",
					cancellationToken);

				if (maxInBatch is null || maxInBatch.Value.Date < fromDate)
					break;

				var next = maxInBatch.Value.Date.AddDays(1);
				if (next <= fromDate)
					break;

				fromDate = next;
				if (batch.Count < PageSize)
					break;
			}
		}

		var gapRows = BuildGapRows(rates);
		inserted.AddRange(gapRows);

		if (inserted.Count == 0)
		{
			await LogInfoAsync(jobLogger, "ردیفی برای درج نبود.", cancellationToken);
			return;
		}

		await unitOfWork.Repository<ExchangeRateArchive>().AddRangeAsync(inserted, cancellationToken);
		await LogInfoAsync(jobLogger, 
			$"{inserted.Count - gapRows.Count} نرخ دلار و {gapRows.Count} روز میانگین درج شد.",
			cancellationToken);
	}

	private async Task<List<string[]>> FetchPageAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
	{
		var url = IndicatorUrl
			+ "?lang=fa&order_dir=asc&start=0&length=" + PageSize
			+ "&search=&from=" + from.ToShamsiDate()
			+ "&to=" + to.ToShamsiDate()
			+ "&convert_to_ad=1";

		var client = httpClientFactory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		using var response = await client.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		var payload = await JsonSerializer.DeserializeAsync<TgjuTable>(stream, JsonOptions, cancellationToken);
		if (payload?.Data == null)
			return [];

		return payload.Data.Where(row => row is { Length: >= 7 }).ToList();
	}

	private static ExchangeRateArchive ParseQuote(string[] element)
	{
		var rateDate = DateTime.ParseExact(element[6], "yyyy/MM/dd", CultureInfo.InvariantCulture).Date;
		var rateOfChange = ParseChangeRate(element[4]);
		var percentageChange = ParsePercentageChange(element[5]);

		return new ExchangeRateArchive
		{
			PriceUnitId = DollarPriceUnitId,
			Price = ParseIntPrice(element[0]),
			MinPrice = ParseIntPrice(element[1]),
			MaxPrice = ParseIntPrice(element[2]),
			FinalPrice = ParseIntPrice(element[3]),
			RateofChange = ParseIntPrice(rateOfChange),
			PercentageChange = ParseDecimalPrice(percentageChange),
			CreatedOnMiladiDateTime = rateDate,
			CreatedOnShamsiDateTime = rateDate.ToShamsiDate(),
			IsComputeAndAddManually = false,
			SanaInitialPrice = 0,
			SanaMinPrice = 0,
			SanaMaxPrice = 0,
			SanaFinalPrice = 0,
			SanaRateofChange = 0,
			SanaPercentageChange = 0
		};
	}

	/// <summary>
	/// روزهای قبل از آخرین تاریخ موجود که ردیف ندارند، با میانگین صحیح نزدیک‌ترین FinalPrice قبل و بعد پر می‌شوند.
	/// ردیف‌های میانگین خودشان همسایه حساب نمی‌شوند.
	/// </summary>
	private static List<ExchangeRateArchive> BuildGapRows(Dictionary<DateTime, int?> rates)
	{
		if (rates.Count == 0)
			return [];

		var dates = rates.Keys.OrderBy(x => x).ToList();
		var first = dates[0];
		var last = dates[^1];
		var gaps = new List<ExchangeRateArchive>();

		for (var day = first.AddDays(1); day < last; day = day.AddDays(1))
		{
			if (rates.ContainsKey(day))
				continue;

			var prevIndex = dates.BinarySearch(day);
			prevIndex = prevIndex < 0 ? ~prevIndex - 1 : prevIndex - 1;
			var nextIndex = dates.BinarySearch(day);
			nextIndex = nextIndex < 0 ? ~nextIndex : nextIndex + 1;
			if (prevIndex < 0 || nextIndex >= dates.Count)
				continue;

			var prevPrice = rates[dates[prevIndex]];
			var nextPrice = rates[dates[nextIndex]];
			if (prevPrice is null || nextPrice is null)
				continue;

			var average = (int)(((long)prevPrice.Value + nextPrice.Value) / 2);
			gaps.Add(new ExchangeRateArchive
			{
				PriceUnitId = DollarPriceUnitId,
				Price = 0,
				MinPrice = 0,
				MaxPrice = 0,
				FinalPrice = average,
				RateofChange = null,
				PercentageChange = null,
				CreatedOnMiladiDateTime = day,
				CreatedOnShamsiDateTime = day.ToShamsiDate(),
				IsComputeAndAddManually = true,
				Comment = GapComment,
				SanaInitialPrice = 0,
				SanaMinPrice = 0,
				SanaMaxPrice = 0,
				SanaFinalPrice = 0,
				SanaRateofChange = 0,
				SanaPercentageChange = 0
			});
		}

		return gaps;
	}

	private static string ParseChangeRate(string rateText)
	{
		if (rateText != "-" && (rateText.Contains("high") || rateText.Contains("low")))
		{
			var isPositive = rateText.Contains("high");
			var matchedValue = Regex.Match(rateText, "dir=\"ltr\">(.*)</span>").Groups[1].Value;
			return (isPositive ? "" : "-") + matchedValue;
		}

		return "0";
	}

	private static string ParsePercentageChange(string percentageText)
	{
		if (percentageText != "-" && (percentageText.Contains("high") || percentageText.Contains("low")))
		{
			var isPositive = percentageText.Contains("high");
			var matchedValue = Regex.Match(percentageText, "dir=\"ltr\">(.*)%</span>").Groups[1].Value;
			return (isPositive ? "" : "-") + matchedValue;
		}

		return "0";
	}

	private static int ParseIntPrice(string value)
	{
		var cleaned = value.Replace(",", "", StringComparison.Ordinal).Trim();
		if (!int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			throw new FormatException($"عدد نامعتبر در نرخ دلار: {value}");

		return result;
	}

	private static decimal ParseDecimalPrice(string value)
	{
		var cleaned = value.Replace(",", "", StringComparison.Ordinal).Trim();
		if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
			throw new FormatException($"درصد نامعتبر در نرخ دلار: {value}");

		return result;
	}

	private async Task TryQueueErrorEmailAsync(Exception ex, IJobLogger? jobLogger, CancellationToken cancellationToken)
	{
		try
		{
			var ownerId = await db.Users.AsNoTracking()
				.Where(u => u.Email != null && u.Email.ToLower() == ErrorRecipient)
				.Select(u => u.Id)
				.FirstOrDefaultAsync(cancellationToken);

			if (ownerId is null or 0)
			{
			await LogWarningAsync(
				jobLogger,
				$"کاربر {ErrorRecipient} برای ایمیل خطای نرخ دلار پیدا نشد.",
				cancellationToken);
				return;
			}

			var message = WebUtility.HtmlEncode(ex.Message);
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "خطا در آپدیت نرخ ارز دلار",
				Body = "در آپدیت نرخ ارز (دلار) مشکلی پیش آمده لطفا بررسی نمایید.<br/>خطا در آپدیت نرخ ارز آزاد است.<br/>" + message,
				OwnerId = ownerId.Value,
				ToEmails = [ErrorRecipient],
				IsRead = false,
				IsSend = false
			}, cancellationToken);
		}
		catch (Exception mailEx)
		{
			await LogExceptionAsync(jobLogger, mailEx, cancellationToken);
		}
	}

	private readonly record struct RatePoint(long? Id, DateTime Date, int? FinalPrice);

	private sealed class TgjuTable
	{
		[JsonPropertyName("data")]
		public string[][]? Data { get; set; }
	}
}

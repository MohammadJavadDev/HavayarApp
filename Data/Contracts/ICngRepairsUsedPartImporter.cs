namespace Data.Contracts
{
	/// <summary>
	/// ورود قطعات مصرفی تعمیرات CNG از راهکاران — معادل HTS WriteData.Import_Cng_Repairing_UsedPart (شاخه Rahkaran).
	/// کنترلر می‌تواند پس از ذخیره تعمیر فراخوانی کند؛ جاب پس‌زمینه هم همین قرارداد را اجرا می‌کند.
	/// </summary>
	public interface ICngRepairsUsedPartImporter
	{
		Task<int> ImportAsync(CancellationToken cancellationToken = default);
	}
}

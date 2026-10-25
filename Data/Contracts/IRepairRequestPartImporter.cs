namespace Data.Contracts
{
	/// <summary>
	/// ورود اقلام مصرفی درخواست تعمیر از درخواست کالای راهکاران.
	/// معادل HTS Import_Rpr_RepairRequest_Part و Update_Rpr_RepairRequest_Part (شاخه Rahkaran).
	/// </summary>
	public interface IRepairRequestPartImporter
	{
		/// <param name="dlId">تفصیل یک شناسنامه. خالی یعنی همه شناسنامه‌ها.</param>
		Task<int> ImportAsync(long? dlId = null, CancellationToken cancellationToken = default);
	}
}

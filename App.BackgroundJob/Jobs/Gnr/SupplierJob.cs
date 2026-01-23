using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Gnr
{
	public class SupplierJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{
		[JobHandler("افزودن تامیین کنندگان از راهکاران")]
		public async Task AddSupplierFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی تامین‌کنندگان از راهکاران", cn);

				var sqlRow = @"WITH FirstAddress AS
					(
						SELECT MIN(AddressRef) as PartyAddressID,PartyRef 
						FROM gnr3.PartyAddress 
						GROUP by PartyRef
					)
					SELECT
						p.PartyID,
						L.[Value] as PreTitle,
						a.ZipCode AS PostBox,
						a.Phone as Phone,
						a.Fax as Fax, 
						a.Email as Email,
						Dl.Code AS DlCode
					FROM GNR3.Party P 
					LEFT JOIN FirstAddress on p.PartyID = FirstAddress.PartyRef
					LEFT JOIN GNR3.Address A on a.AddressID = FirstAddress.PartyAddressID
					LEFT JOIN SYS3.[Lookup] L on l.Code = p.Title AND L.System  ='GNR3' AND l.Type = 'PartyTitle'
					LEFT JOIN FIN3.DL AS Dl ON Dl.ReferenceID = P.PartyID AND Dl.EntityCode = 242 AND  (Dl.DLTypeRef IN (1,2))
					";

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری برای دریافت داده‌های تامین‌کنندگان از راهکاران...", cn);
				var rahakarnData = await Rdb.Database.SqlQueryRaw<AddSupplierDto>(sqlRow).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahakarnData.Count} رکورد تامین‌کننده از راهکاران دریافت شد", cn);

				// بارگذاری Party ها برای نگاشت PartyID به PartyId
				await jobLogger?.LogInfoAsync("در حال بارگذاری Party ها از پایگاه داده برنامه...", cn);
				var appParties = await unitOfWork.Repository<Party>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);

				var partyMap = appParties
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				await jobLogger?.LogInfoAsync($"تعداد {partyMap.Count} Party با HamkaranId در پایگاه داده برنامه موجود است", cn);

				// بارگذاری Supplier های موجود
				var appSuppliers = await unitOfWork.Repository<Supplier>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appSuppliers.Count} تامین‌کننده در پایگاه داده برنامه موجود است", cn);

				var appSuppliersDict = appSuppliers
					.ToDictionary(x => x.PartyId);

				var newSuppliers = new List<Supplier>();
				int updatedSuppliersCount = 0;
				int skippedCount = 0;

				foreach (var rahkaranData in rahakarnData)
				{
					// پیدا کردن PartyId بر اساس HamkaranId (PartyID)
					if (!partyMap.TryGetValue(rahkaranData.PartyID, out var partyId))
					{
						await jobLogger?.LogWarningAsync($"Party با HamkaranId {rahkaranData.PartyID} برای تامین‌کننده یافت نشد", 0, cn);
						skippedCount++;
						continue;
					}

					if (!appSuppliersDict.TryGetValue((long)partyId, out var existSupplier))
					{
						// ایجاد رکورد جدید
						newSuppliers.Add(new Supplier
						{
							PartyId = (long)partyId,
							Prefix = rahkaranData.PreTitle,
							PostalCode = rahkaranData.PostBox,
							PhoneNumber = rahkaranData.Phone,
							Fax = rahkaranData.Fax,
							Email = rahkaranData.Email,
							DlCode = rahkaranData.DlCode
						});

					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existSupplier.Prefix != rahkaranData.PreTitle)
						{
							existSupplier.Prefix = rahkaranData.PreTitle;
							isModified = true;
						}

						if (existSupplier.PostalCode != rahkaranData.PostBox)
						{
							existSupplier.PostalCode = rahkaranData.PostBox;
							isModified = true;
						}

						if (existSupplier.PhoneNumber != rahkaranData.Phone)
						{
							existSupplier.PhoneNumber = rahkaranData.Phone;
							isModified = true;
						}

						if (existSupplier.Fax != rahkaranData.Fax)
						{
							existSupplier.Fax = rahkaranData.Fax;
							isModified = true;
						}

						if (existSupplier.Email != rahkaranData.Email)
						{
							existSupplier.Email = rahkaranData.Email;
							isModified = true;
						}

						if (existSupplier.DlCode != rahkaranData.DlCode)
						{
							existSupplier.DlCode = rahkaranData.DlCode;
							isModified = true;
						}

						if (isModified)
						{
							updatedSuppliersCount++;
						}
					}
				}

				if (skippedCount > 0)
				{
					await jobLogger?.LogWarningAsync($"تعداد {skippedCount} رکورد به دلیل عدم وجود Party مربوطه رد شد", 0, cn);
				}

				if (newSuppliers.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newSuppliers.Count} تامین‌کننده جدید به پایگاه داده", cn);
					await unitOfWork.Repository<Supplier>().AddRangeAsync(newSuppliers, cn, false);
				}

				if (updatedSuppliersCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedSuppliersCount} تامین‌کننده موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی تامین‌کنندگان با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private class AddSupplierDto
		{
			public long PartyID { get; set; }
			public string? PreTitle { get; set; }
			public string? PostBox { get; set; }
			public string? Phone { get; set; }
			public string? Fax { get; set; }
			public string? Email { get; set; }
			public string? DlCode { get; set; }
		}
	}
}

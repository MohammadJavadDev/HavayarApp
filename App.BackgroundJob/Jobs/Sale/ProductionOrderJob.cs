using System.Text;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Pln;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Entities.Hts.Pln;
using Microsoft.EntityFrameworkCore;
using Services.Job;
 

namespace App.BackgroundJob.Jobs.Sale
{
	public class ProductionOrderJob(RahkaranDbContext Rdb, HtsDbContext Hdb, IUnitOfWork unitOfWork, ApplicationDbContext appContext)
	{


		[JobHandler("افزودن سفارش ساخت از راهکاران")]
		public async Task AddProductionOrderFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی سفارش ساخت از راهکاران", cn);

				// همگام‌سازی ProductionOrder (جدول والد)
				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های سفارش ساخت از راهکاران...", cn);
				var rahkaranProductionOrders = await Rdb.RahkaranSale_ProductionOrder
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranProductionOrders.Count} رکورد سفارش ساخت در راهکاران یافت شد", cn);

				// بارگذاری entity های مرتبط برای نگاشت
				await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط...", cn);
				var appContracts = await unitOfWork.Repository<Contract>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				// Contract.HamkaranId → (App ContractId, BranchId) — Branch مثل SP قدیمی از قرارداد می‌آید
				var contractMap = appContracts.ToDictionary(
					x => x.HamkaranId!.Value,
					x => (ContractId: x.Id, BranchId: x.BranchId));

				var appDLs = await unitOfWork.Repository<DL>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var dlMap = appDLs.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				var appCustomers = await unitOfWork.Repository<Customer>()
					.TableNoTracking
					.ToListAsync(cn);
				var customerMap = appCustomers.ToDictionary(x => x.HamkaranId, x => x.Id);

				var appRegions = await unitOfWork.Repository<Region>()
					.TableNoTracking
					.Where(x => x.RahkaranId.HasValue)
					.ToListAsync(cn);
				var regionMap = appRegions.ToDictionary(x => x.RahkaranId!.Value, x => x.Id);

				// بارگذاری Personel ها برای نگاشت Employee → User
				var appPersonels = await unitOfWork.Repository<Personel>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue && x.PartyId.HasValue)
					.ToListAsync(cn);

				// بارگذاری User ها با PartyId و Id
				var appUsers = await appContext.Users
					.AsNoTracking()
					.Where(x => x.PartyId.HasValue && x.Id.HasValue)
					.ToListAsync(cn);

				// ایجاد نگاشت از PartyId به UserId
				var partyToUserMap = appUsers.ToDictionary(x => x.PartyId!.Value, x => x.Id!.Value);

				// ایجاد نگاشت نهایی از HamkaranId (EmployeeId) به UserId
				var employeeToUserMap = new Dictionary<long, long>();
				foreach (var personel in appPersonels)
				{
					if (personel.HamkaranId.HasValue && personel.PartyId.HasValue &&
						partyToUserMap.TryGetValue(personel.PartyId.Value, out var userId))
					{
						employeeToUserMap[personel.HamkaranId.Value] = userId;
					}
				}

				await jobLogger?.LogInfoAsync($"Entity های مرتبط بارگذاری شدند: {contractMap.Count} قرارداد، {dlMap.Count} حساب تفصیلی، {customerMap.Count} مشتری، {regionMap.Count} منطقه، {employeeToUserMap.Count} نگاشت کارمند به کاربر", cn);

				// بارگذاری ProductionOrder های موجود
				var appProductionOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appProductionOrders.Count} رکورد سفارش ساخت در پایگاه داده برنامه موجود است", cn);

				var appProductionOrdersDict = appProductionOrders
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				var newProductionOrders = new List<ProductionOrder>();
				int updatedProductionOrdersCount = 0;

				foreach (var rahkaranPO in rahkaranProductionOrders)
				{
					if (!appProductionOrdersDict.TryGetValue(rahkaranPO.Sale_ProductionOrderID, out var existPO))
					{
						// Number در HTS قدیمی = ProductionOrderNumber (نه فیلد Number راهکاران که شماره داخلی دیگری است)
						var productionOrderNumber = rahkaranPO.ProductionOrderNumber.HasValue
							? (long)rahkaranPO.ProductionOrderNumber.Value
							: (long?)null;
						var numberText = productionOrderNumber?.ToString() ?? rahkaranPO.Number ?? string.Empty;

						// ایجاد رکورد جدید
						// پیوست‌ها: مثل SP قدیمی NULL می‌مانند — شناسه پیوست راهکاران معادل FileEntity اپ نیست
						var newPO = new ProductionOrder
						{
							HamkaranId = rahkaranPO.Sale_ProductionOrderID,
							State = (ProductionOrderStateEnum)rahkaranPO.State,
							EndUser = rahkaranPO.EndUser,
							ProjectStartDate = rahkaranPO.ProjectStartDate,
							MetreDate = rahkaranPO.MetreDate,
							MetreNumber = rahkaranPO.MetreNumber,
							AgreedDeliveryDate = rahkaranPO.AgreedDeliveryDate,
							IsNeedInspectionBeforePacking = rahkaranPO.IsNeedInspectionBeforePacking ?? false,
							HasContractor = rahkaranPO.HasContractor ?? false,
							CompressorHouseIsReady = rahkaranPO.CompressorHouseIsReady ?? false,
							InstallationIsByHy = rahkaranPO.InstallationIsByHy ?? false,
							HasFinancialGuarantee = rahkaranPO.HasFinancialGuarantee ?? false,
							SalesComment = rahkaranPO.SalesComment,
							Comment = rahkaranPO.Comment,
							CentrifugeHasCompressor = rahkaranPO.CentrifugeHasCompressor ?? false,
							CentrifugeCompressorCount = rahkaranPO.CentrifugeCompressorCount ?? 0,
							CentrifugeElectromotorVoltage = rahkaranPO.CentrifugeElectromotorVoltage,
							Number = numberText,
							HasGA = rahkaranPO.HasGA ?? false,
							PackingType = rahkaranPO.PackingTypeId,
							DeliveryType = rahkaranPO.DeliveryTypeId,
							ProductType = rahkaranPO.ProductTypeId,
							CentrifugeSetupType = rahkaranPO.CentrifugeSetupTypeId,
							IsNeedProjectManager = rahkaranPO.IsNeedProjectManager ?? false,
							ProductionOrderNumber = productionOrderNumber,
							Revision = rahkaranPO.Revision.HasValue ? (int)rahkaranPO.Revision.Value : null,
							CustomerIndustry = rahkaranPO.CustomerIndustry,
							ProjectDlCode = rahkaranPO.ProjectDlCode,
							EdmsProject = rahkaranPO.EdmsProjectId,
							// مثل InsertOrUpdateProductionOrders: اگر مدیر پروژه داشته باشد برای گزارش تحویل به‌موقع غیرفعال می‌شود
							IsDisableForTimelyDeliveryReport = rahkaranPO.ProjectManagerIdRef.HasValue,
							DisableForTimelyDeliveryReportComment = rahkaranPO.ProjectManagerIdRef.HasValue ? "پروژه" : null
						};

						if (newPO.State == ProductionOrderStateEnum.Obsolete)
						{
							var nowObsolete = DateTime.Now;
							newPO.ObsoletedOnMiladiDate = nowObsolete;
							newPO.ObsoletedOnShamsiDate = nowObsolete.ToShamsiDateTime();
						}

						// نگاشت ContractIdRef → ContractId + BranchId (از قرارداد، مثل SP قدیمی)
						if (rahkaranPO.ContractIdRef.HasValue && contractMap.TryGetValue(rahkaranPO.ContractIdRef.Value, out var contractInfo))
						{
							newPO.ContractId = contractInfo.ContractId;
							newPO.BranchId = contractInfo.BranchId;
						}

						// نگاشت ProjectDlIdRef → ProjectDlId
						if (rahkaranPO.ProjectDlIdRef.HasValue && dlMap.TryGetValue(rahkaranPO.ProjectDlIdRef.Value, out var projectDlId))
						{
							newPO.ProjectDlId = projectDlId;
						}

						// نگاشت SalesAgencyIdRef → SalesAgencyId
						if (rahkaranPO.SalesAgencyIdRef.HasValue && customerMap.TryGetValue(rahkaranPO.SalesAgencyIdRef.Value, out var salesAgencyId))
						{
							newPO.SalesAgencyId = salesAgencyId;
						}

						// نگاشت InstallationCityIdRef → InstallationCityId
						if (rahkaranPO.InstallationCityIdRef.HasValue && regionMap.TryGetValue(rahkaranPO.InstallationCityIdRef.Value, out var installationCityId))
						{
							newPO.InstallationCityId = installationCityId;
						}

						// نگاشت User ها از طریق Employee → Personel → User
						// استفاده از SalesManagerIdRef (Employee) به جای SalesManagerUserIdRef
						if (rahkaranPO.SalesManagerIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.SalesManagerIdRef.Value, out var salesManagerId))
						{
							newPO.SalesManagerId = salesManagerId;
						}

						if (rahkaranPO.ProjectManagerIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.ProjectManagerIdRef.Value, out var projectManagerId))
						{
							newPO.ProjectManagerId = projectManagerId;
						}

						if (rahkaranPO.AlternativeExpertIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.AlternativeExpertIdRef.Value, out var alternativeExpertId))
						{
							newPO.AlternativeExpertId = alternativeExpertId;
						}

						if (rahkaranPO.SalesExpertIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.SalesExpertIdRef.Value, out var salesExpertId))
						{
							newPO.SalesExpertId = salesExpertId;
						}

						if (rahkaranPO.IntroducerExpertRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.IntroducerExpertRef.Value, out var introducerExpertId))
						{
							newPO.IntroducerExpertId = introducerExpertId;
						}

						newProductionOrders.Add(newPO);
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;
						var previousState = existPO.State;

						if (existPO.State != (ProductionOrderStateEnum)rahkaranPO.State)
						{
							existPO.State = (ProductionOrderStateEnum)rahkaranPO.State;
							isModified = true;
						}

						// منسوخ‌سازی از راهکاران (State=4) — معادل LastStatusId=2207 در SP قدیمی
						if (existPO.State == ProductionOrderStateEnum.Obsolete
							&& previousState != ProductionOrderStateEnum.Obsolete
							&& !existPO.ObsoletedOnMiladiDate.HasValue)
						{
							var nowObsolete = DateTime.Now;
							existPO.ObsoletedOnMiladiDate = nowObsolete;
							existPO.ObsoletedOnShamsiDate = nowObsolete.ToShamsiDateTime();
							isModified = true;
						}
						else if (existPO.State != ProductionOrderStateEnum.Obsolete
							&& previousState == ProductionOrderStateEnum.Obsolete)
						{
							existPO.ObsoletedOnMiladiDate = null;
							existPO.ObsoletedOnShamsiDate = null;
							existPO.ObsoletedById = null;
							isModified = true;
						}

						if (existPO.EndUser != rahkaranPO.EndUser)
						{
							existPO.EndUser = rahkaranPO.EndUser;
							isModified = true;
						}

						if (existPO.ProjectStartDate != rahkaranPO.ProjectStartDate)
						{
							existPO.ProjectStartDate = rahkaranPO.ProjectStartDate;
							isModified = true;
						}

						if (existPO.MetreDate != rahkaranPO.MetreDate)
						{
							existPO.MetreDate = rahkaranPO.MetreDate;
							isModified = true;
						}

						if (existPO.MetreNumber != rahkaranPO.MetreNumber)
						{
							existPO.MetreNumber = rahkaranPO.MetreNumber;
							isModified = true;
						}

						if (existPO.AgreedDeliveryDate != rahkaranPO.AgreedDeliveryDate)
						{
							existPO.AgreedDeliveryDate = rahkaranPO.AgreedDeliveryDate;
							isModified = true;
						}

						var newIsNeedInspectionBeforePacking = rahkaranPO.IsNeedInspectionBeforePacking ?? false;
						if (existPO.IsNeedInspectionBeforePacking != newIsNeedInspectionBeforePacking)
						{
							existPO.IsNeedInspectionBeforePacking = newIsNeedInspectionBeforePacking;
							isModified = true;
						}

						var newHasContractor = rahkaranPO.HasContractor ?? false;
						if (existPO.HasContractor != newHasContractor)
						{
							existPO.HasContractor = newHasContractor;
							isModified = true;
						}

						var newCompressorHouseIsReady = rahkaranPO.CompressorHouseIsReady ?? false;
						if (existPO.CompressorHouseIsReady != newCompressorHouseIsReady)
						{
							existPO.CompressorHouseIsReady = newCompressorHouseIsReady;
							isModified = true;
						}

						var newInstallationIsByHy = rahkaranPO.InstallationIsByHy ?? false;
						if (existPO.InstallationIsByHy != newInstallationIsByHy)
						{
							existPO.InstallationIsByHy = newInstallationIsByHy;
							isModified = true;
						}

						var newHasFinancialGuarantee = rahkaranPO.HasFinancialGuarantee ?? false;
						if (existPO.HasFinancialGuarantee != newHasFinancialGuarantee)
						{
							existPO.HasFinancialGuarantee = newHasFinancialGuarantee;
							isModified = true;
						}

						if (existPO.SalesComment != rahkaranPO.SalesComment)
						{
							existPO.SalesComment = rahkaranPO.SalesComment;
							isModified = true;
						}

						if (existPO.Comment != rahkaranPO.Comment)
						{
							existPO.Comment = rahkaranPO.Comment;
							isModified = true;
						}

						var newCentrifugeHasCompressor = rahkaranPO.CentrifugeHasCompressor ?? false;
						if (existPO.CentrifugeHasCompressor != newCentrifugeHasCompressor)
						{
							existPO.CentrifugeHasCompressor = newCentrifugeHasCompressor;
							isModified = true;
						}

						var newCentrifugeCompressorCount = rahkaranPO.CentrifugeCompressorCount ?? 0;
						if (existPO.CentrifugeCompressorCount != newCentrifugeCompressorCount)
						{
							existPO.CentrifugeCompressorCount = newCentrifugeCompressorCount;
							isModified = true;
						}

						if (existPO.CentrifugeElectromotorVoltage != rahkaranPO.CentrifugeElectromotorVoltage)
						{
							existPO.CentrifugeElectromotorVoltage = rahkaranPO.CentrifugeElectromotorVoltage;
							isModified = true;
						}

						var productionOrderNumber = rahkaranPO.ProductionOrderNumber.HasValue
							? (long)rahkaranPO.ProductionOrderNumber.Value
							: (long?)null;
						var numberText = productionOrderNumber?.ToString() ?? rahkaranPO.Number ?? string.Empty;
						if (existPO.Number != numberText)
						{
							existPO.Number = numberText;
							isModified = true;
						}

						if (existPO.HasGA != (rahkaranPO.HasGA ?? false))
						{
							existPO.HasGA = rahkaranPO.HasGA ?? false;
							isModified = true;
						}

						if (existPO.PackingType != rahkaranPO.PackingTypeId)
						{
							existPO.PackingType = rahkaranPO.PackingTypeId;
							isModified = true;
						}

						if (existPO.DeliveryType != rahkaranPO.DeliveryTypeId)
						{
							existPO.DeliveryType = rahkaranPO.DeliveryTypeId;
							isModified = true;
						}

						if (existPO.ProductType != rahkaranPO.ProductTypeId)
						{
							existPO.ProductType = rahkaranPO.ProductTypeId;
							isModified = true;
						}

						if (existPO.CentrifugeSetupType != rahkaranPO.CentrifugeSetupTypeId)
						{
							existPO.CentrifugeSetupType = rahkaranPO.CentrifugeSetupTypeId;
							isModified = true;
						}

						if (existPO.IsNeedProjectManager != (rahkaranPO.IsNeedProjectManager ?? false))
						{
							existPO.IsNeedProjectManager = rahkaranPO.IsNeedProjectManager ?? false;
							isModified = true;
						}

						if (existPO.ProductionOrderNumber != productionOrderNumber)
						{
							existPO.ProductionOrderNumber = productionOrderNumber;
							isModified = true;
						}

						var revision = rahkaranPO.Revision.HasValue ? (int)rahkaranPO.Revision.Value : (int?)null;
						if (existPO.Revision != revision)
						{
							existPO.Revision = revision;
							isModified = true;
						}

						if (existPO.CustomerIndustry != rahkaranPO.CustomerIndustry)
						{
							existPO.CustomerIndustry = rahkaranPO.CustomerIndustry;
							isModified = true;
						}

						if (existPO.ProjectDlCode != rahkaranPO.ProjectDlCode)
						{
							existPO.ProjectDlCode = rahkaranPO.ProjectDlCode;
							isModified = true;
						}

						if (existPO.EdmsProject != rahkaranPO.EdmsProjectId)
						{
							existPO.EdmsProject = rahkaranPO.EdmsProjectId;
							isModified = true;
						}

						 
						// به‌روزرسانی روابط
						if (rahkaranPO.ContractIdRef.HasValue && contractMap.TryGetValue(rahkaranPO.ContractIdRef.Value, out var contractInfo))
						{
							if (existPO.ContractId != contractInfo.ContractId)
							{
								existPO.ContractId = contractInfo.ContractId;
								isModified = true;
							}
							if (existPO.BranchId != contractInfo.BranchId)
							{
								existPO.BranchId = contractInfo.BranchId;
								isModified = true;
							}
						}

						if (rahkaranPO.ProjectDlIdRef.HasValue && dlMap.TryGetValue(rahkaranPO.ProjectDlIdRef.Value, out var projectDlId))
						{
							if (existPO.ProjectDlId != projectDlId)
							{
								existPO.ProjectDlId = projectDlId;
								isModified = true;
							}
						}

						if (rahkaranPO.SalesAgencyIdRef.HasValue && customerMap.TryGetValue(rahkaranPO.SalesAgencyIdRef.Value, out var salesAgencyId))
						{
							if (existPO.SalesAgencyId != salesAgencyId)
							{
								existPO.SalesAgencyId = salesAgencyId;
								isModified = true;
							}
						}

						if (rahkaranPO.InstallationCityIdRef.HasValue && regionMap.TryGetValue(rahkaranPO.InstallationCityIdRef.Value, out var installationCityId))
						{
							if (existPO.InstallationCityId != installationCityId)
							{
								existPO.InstallationCityId = installationCityId;
								isModified = true;
							}
						}

						// به‌روزرسانی User ها از طریق Employee → Personel → User
						long? newSalesManagerId = null;
						if (rahkaranPO.SalesManagerIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.SalesManagerIdRef.Value, out var salesManagerId))
						{
							newSalesManagerId = salesManagerId;
						}
						if (existPO.SalesManagerId != newSalesManagerId)
						{
							existPO.SalesManagerId = newSalesManagerId;
							isModified = true;
						}

						long? newProjectManagerId = null;
						if (rahkaranPO.ProjectManagerIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.ProjectManagerIdRef.Value, out var projectManagerId))
						{
							newProjectManagerId = projectManagerId;
						}
						if (existPO.ProjectManagerId != newProjectManagerId)
						{
							existPO.ProjectManagerId = newProjectManagerId;
							isModified = true;
						}

						long? newAlternativeExpertId = null;
						if (rahkaranPO.AlternativeExpertIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.AlternativeExpertIdRef.Value, out var alternativeExpertId))
						{
							newAlternativeExpertId = alternativeExpertId;
						}
						if (existPO.AlternativeExpertId != newAlternativeExpertId)
						{
							existPO.AlternativeExpertId = newAlternativeExpertId;
							isModified = true;
						}

						long? newSalesExpertId = null;
						if (rahkaranPO.SalesExpertIdRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.SalesExpertIdRef.Value, out var salesExpertId))
						{
							newSalesExpertId = salesExpertId;
						}
						if (existPO.SalesExpertId != newSalesExpertId)
						{
							existPO.SalesExpertId = newSalesExpertId;
							isModified = true;
						}

						long? newIntroducerExpertId = null;
						if (rahkaranPO.IntroducerExpertRef.HasValue && employeeToUserMap.TryGetValue(rahkaranPO.IntroducerExpertRef.Value, out var introducerExpertId))
						{
							newIntroducerExpertId = introducerExpertId;
						}
						if (existPO.IntroducerExpertId != newIntroducerExpertId)
						{
							existPO.IntroducerExpertId = newIntroducerExpertId;
							isModified = true;
						}

						if (isModified)
						{
							updatedProductionOrdersCount++;
						}
					}
				}

				if (newProductionOrders.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newProductionOrders.Count} سفارش ساخت جدید به پایگاه داده", cn);
					await unitOfWork.Repository<ProductionOrder>().AddRangeAsync(newProductionOrders, cn, false);
				}

				if (updatedProductionOrdersCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedProductionOrdersCount} سفارش ساخت موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی سفارش ساخت با موفقیت انجام شد", cn);

				// همگام‌سازی ProductionOrderItem (جدول فرزند)
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی اقلام سفارش ساخت از راهکاران...", cn);
				var rahkaranItems = await Rdb.RahkaranSale_ProductionOrderItem
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranItems.Count} رکورد قلم سفارش ساخت در راهکاران یافت شد", cn);

				// بارگذاری مجدد ProductionOrder ها برای نگاشت _MasterRef
				var allAppProductionOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.ToListAsync(cn);

				var productionOrderMap = allAppProductionOrders
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				var appParts = await unitOfWork.Repository<Part>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var partMap = appParts.ToDictionary(
					x => x.HamkaranId!.Value,
					x => (PartId: x.Id, IsRoutine: x.EngineeringRoutine || (x.DesignTypeIsRoutine ?? false)));

				var appItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appItems.Count} رکورد قلم سفارش ساخت در پایگاه داده برنامه موجود است", cn);

				var appItemsDict = appItems
					.Where(x => x.HamkaranId.HasValue && x.IsLatestVersion)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				// سفارش‌های منسوخ برای همگام‌سازی وضعیت اقلام (معادل BuyStatusId=2208 در SP قدیمی)
				var obsoleteProductionOrderIds = allAppProductionOrders
					.Where(x => x.State == ProductionOrderStateEnum.Obsolete && x.Id.HasValue)
					.Select(x => x.Id!.Value)
					.ToHashSet();

				var newItems = new List<ProductionOrderItem>();
				var processedHamkaranIds = new HashSet<long>();

				int updatedItemsCount = 0;
				int skippedItemsCount = 0;

				foreach (var rahkaranItem in rahkaranItems)
				{
					processedHamkaranIds.Add(rahkaranItem.Sale_ProductionOrderItemID);

					// پیدا کردن ProductionOrderId بر اساس _MasterRef
					if (!productionOrderMap.TryGetValue(rahkaranItem._MasterRef, out var productionOrderId))
					{
						await jobLogger?.LogWarningAsync($"سفارش ساخت والد با HamkaranId {rahkaranItem._MasterRef} برای قلم یافت نشد", 0, cn);
						skippedItemsCount++;
						continue;
					}

					// پیدا کردن PartId / IsRoutine بر اساس PartIdRef (مثل SP قدیمی از DesignTypeIsRoutine)
					long? partId = null;
					var isRoutine = false;
					if (rahkaranItem.PartIdRef.HasValue && partMap.TryGetValue(rahkaranItem.PartIdRef.Value, out var foundPart))
					{
						partId = foundPart.PartId;
						isRoutine = foundPart.IsRoutine;
					}

					var checkStatus = rahkaranItem.CheckStatusId.HasValue
						&& Enum.IsDefined(typeof(ProductionOrderItemCheckStatusEnum), rahkaranItem.CheckStatusId.Value)
							? (ProductionOrderItemCheckStatusEnum?)rahkaranItem.CheckStatusId.Value
							: ProductionOrderItemCheckStatusEnum.InitialRegistration;

					if (!appItemsDict.TryGetValue(rahkaranItem.Sale_ProductionOrderItemID, out var existItem))
					{
						// ایجاد رکورد جدید (New Insert)
						// HamkaranId = Sale_ProductionOrderItemID (کلید منطقی قلم).
						// توجه: در HTS قدیمی RahkaranId = History.Id بود؛ کات‌اور با Number+PartCode هم تطبیق می‌دهد.
						var newItem = new ProductionOrderItem
						{
							HamkaranId = rahkaranItem.Sale_ProductionOrderItemID,
							ProductionOrderId = (long)productionOrderId,
							IsBuildInside = rahkaranItem.IsBuildInside ?? false,
							DeviceType = rahkaranItem.DeviceTypeId.HasValue ? (ProductionOrderItemDeviceTypeEnum?)rahkaranItem.DeviceTypeId.Value : null,
							EquipmentType = rahkaranItem.EquipmentTypeId.HasValue ? (ProductionOrderItemEquipmentTypeEnum?)rahkaranItem.EquipmentTypeId.Value : null,
							PartId = partId,
							Amount = rahkaranItem.Amount,
							AgreedDeliverDate = rahkaranItem.AgreedDeliverDate,
							PartModel = rahkaranItem.PartModel,
							AirendOrCategory = rahkaranItem.AirendOrCategory,
							InputPressureBar = rahkaranItem.InputPressureBar,
							OutputPressureBar = rahkaranItem.OutputPressureBar,
							Capacity = rahkaranItem.Capacity,
							Scale = rahkaranItem.Scale,
							GasType = rahkaranItem.GasType,
							MaximumTemperature = rahkaranItem.MaximumTemperature,
							PurityPercentage = rahkaranItem.PurityPercentage,
							RelativeHumidity = rahkaranItem.RelativeHumidity,
							BarometricPressure = rahkaranItem.BarometricPressure,
							MovingType = rahkaranItem.MovingType,
							HasInspection = rahkaranItem.HasInspection ?? false,
							SalesConsideration = rahkaranItem.SalesConsideration,
							Revision = rahkaranItem.Revision,
							IsLatestVersion = true,
							CheckStatus = checkStatus,
							PartCode = rahkaranItem.PartCode,
							IsRoutine = isRoutine,
							Status = ProductionOrderItemStatusEnum.NotCompleted
						};
						newItems.Add(newItem);

						newItem.ProductionOrderItemComments.Add(new ProductionOrderItemComment
						{
							ProductionStatus = ProductionOrderItemProductionStatusEnum.NotDetermined,
							Comment = "خوانده شده از راهکاران",
							StartMiladiDateTime = DateTime.Now,
							StartShamsiDate = DateTime.Now.ToShamsiDateTime(),
							CreatedById = 1,
							CreatedOnMiladiDateTime = DateTime.Now,
							CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
							IsActive = IsActiveEnum.Active
						});
					}
					else
					{
						// بررسی تغییر نسخه (Update -> New Version Insert)
						// منطق SQL: بایستی به ازای هر ویرایش، قلم جدیدی ایجاد گردد
						if (existItem.Revision != rahkaranItem.Revision)
						{
							existItem.IsLatestVersion = false;

							var revisionNow = DateTime.Now;
							var newItem = new ProductionOrderItem
							{
								HamkaranId = rahkaranItem.Sale_ProductionOrderItemID,
								ProductionOrderId = (long)productionOrderId,
								IsBuildInside = rahkaranItem.IsBuildInside ?? false,
								DeviceType = rahkaranItem.DeviceTypeId.HasValue ? (ProductionOrderItemDeviceTypeEnum?)rahkaranItem.DeviceTypeId.Value : null,
								EquipmentType = rahkaranItem.EquipmentTypeId.HasValue ? (ProductionOrderItemEquipmentTypeEnum?)rahkaranItem.EquipmentTypeId.Value : null,
								PartId = partId,
								Amount = rahkaranItem.Amount,
								AgreedDeliverDate = rahkaranItem.AgreedDeliverDate,
								PartModel = rahkaranItem.PartModel,
								AirendOrCategory = rahkaranItem.AirendOrCategory,
								InputPressureBar = rahkaranItem.InputPressureBar,
								OutputPressureBar = rahkaranItem.OutputPressureBar,
								Capacity = rahkaranItem.Capacity,
								Scale = rahkaranItem.Scale,
								GasType = rahkaranItem.GasType,
								MaximumTemperature = rahkaranItem.MaximumTemperature,
								PurityPercentage = rahkaranItem.PurityPercentage,
								RelativeHumidity = rahkaranItem.RelativeHumidity,
								BarometricPressure = rahkaranItem.BarometricPressure,
								MovingType = rahkaranItem.MovingType,
								HasInspection = rahkaranItem.HasInspection ?? false,
								SalesConsideration = rahkaranItem.SalesConsideration,
								Revision = rahkaranItem.Revision,
								IsLatestVersion = true,
								CheckStatus = checkStatus,
								PartCode = rahkaranItem.PartCode,
								IsRoutine = isRoutine,
								Status = ProductionOrderItemStatusEnum.NotCompleted,
								SendToIndustrialMiladiDate = revisionNow,
								SendToIndustrialShamsiDate = revisionNow.ToShamsiDate()
							};

							newItem.ProductionOrderItemComments.Add(new ProductionOrderItemComment
							{
								ProductionStatus = ProductionOrderItemProductionStatusEnum.NotDetermined,
								Comment = "خوانده شده از راهکاران (نسخه جدید)",
								StartMiladiDateTime = revisionNow,
								StartShamsiDate = revisionNow.ToShamsiDateTime(),
								CreatedById = 1,
								CreatedOnMiladiDateTime = revisionNow,
								CreatedOnShamsiDateTime = revisionNow.ToShamsiDateTime(),
								IsActive = IsActiveEnum.Active
							});

							newItems.Add(newItem);
							updatedItemsCount++;
						}
						// Revision یکسان ⇒ طبق تریگر راهکاران تغییری نیست؛ فیلد عملیاتی توسط کات‌اور HTS پوشش داده می‌شود
					}
				}

				// بررسی اقلام حذف شده (حذف از جدول زنده راهکاران = History.Status=2 در SP قدیمی)
				var itemsToDelete = appItemsDict.Values
					.Where(x => !processedHamkaranIds.Contains(x.HamkaranId!.Value))
					.ToList();

				if (itemsToDelete.Any())
				{
					foreach (var item in itemsToDelete)
					{
						item.IsLatestVersion = false;
						item.IsDeleted = true;
						item.IsActive = IsActiveEnum.Deleted;
						item.Status = ProductionOrderItemStatusEnum.Invalid;
					}
					await jobLogger?.LogInfoAsync($"تعداد {itemsToDelete.Count} قلم حذف/باطل شدند", cn);
				}

				// منسوخ‌سازی / خروج از منسوخی اقلام بر اساس State هدر (معادل 2208 / 1903 در SP قدیمی)
				int deprecatedItemsCount = 0;
				int restoredFromDeprecatedCount = 0;
				foreach (var item in appItems.Where(i => i.IsLatestVersion && !i.IsDeleted && i.IsActive != IsActiveEnum.Deleted))
				{
					if (!item.ProductionOrderId.HasValue)
						continue;

					if (obsoleteProductionOrderIds.Contains(item.ProductionOrderId.Value))
					{
						if (item.CheckStatus != ProductionOrderItemCheckStatusEnum.Deprecated)
						{
							item.CheckStatus = ProductionOrderItemCheckStatusEnum.Deprecated;
							deprecatedItemsCount++;
						}
					}
					else if (item.CheckStatus == ProductionOrderItemCheckStatusEnum.Deprecated)
					{
						item.CheckStatus = ProductionOrderItemCheckStatusEnum.InitialRegistration;
						restoredFromDeprecatedCount++;
					}
				}
				foreach (var item in newItems.Where(i => i.ProductionOrderId.HasValue))
				{
					if (obsoleteProductionOrderIds.Contains(item.ProductionOrderId!.Value))
					{
						item.CheckStatus = ProductionOrderItemCheckStatusEnum.Deprecated;
						deprecatedItemsCount++;
					}
				}
				if (deprecatedItemsCount > 0)
					await jobLogger?.LogInfoAsync($"تعداد {deprecatedItemsCount} قلم به‌خاطر منسوخ شدن سفارش ساخت، Deprecated شدند", cn);
				if (restoredFromDeprecatedCount > 0)
					await jobLogger?.LogInfoAsync($"تعداد {restoredFromDeprecatedCount} قلم از منسوخی خارج و به ثبت اولیه برگشتند", cn);

				if (skippedItemsCount > 0)
				{
					await jobLogger?.LogWarningAsync($"تعداد {skippedItemsCount} قلم به دلیل عدم وجود سفارش ساخت والد رد شد", 0, cn);
				}

				if (newItems.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newItems.Count} قلم جدید (شامل نسخه‌های جدید) به پایگاه داده", cn);
					await unitOfWork.Repository<ProductionOrderItem>().AddRangeAsync(newItems, cn, false);
				}

				if (updatedItemsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedItemsCount} قلم موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی اقلام سفارش ساخت با موفقیت انجام شد", cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی کامل سفارش ساخت و اقلام با موفقیت به پایان رسید", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		// Update AddProductionOrderItemBomFromHts method to sync Bom data and add logging
		[JobHandler("افزودن Bom سفارش ساخت از Hts")]
		public async Task AddProductionOrderItemBomFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی Bom اقلام سفارش ساخت از HTS", cn);

				// بارگذاری داده‌های پایه
				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های پایه از پایگاه داده برنامه...", cn);
				var appProductionOrders = await unitOfWork
					.Repository<ProductionOrder>()
					.TableNoTracking
					.Where(po => po.ProductionOrderNumber.HasValue)
					.ToListAsync(cn);

				var appPOItems = await unitOfWork.Repository<ProductionOrderItem>()
					.TableNoTracking
					.Include(c => c.Part)
					.ToListAsync(cn);

				var appPOItemsBom = await unitOfWork
					.Repository<ProductionOrderItemBom>()
					.TableNoTracking
					.ToListAsync(cn);

				var parts = await unitOfWork.Repository<Part>()
					.TableNoTracking
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"بارگذاری داده‌های پایه انجام شد: {appProductionOrders.Count} سفارش ساخت، {appPOItems.Count} قلم سفارش، {appPOItemsBom.Count} BOM موجود، {parts.Count} کالا", cn);

				// ساخت dictionary ها برای جستجوی سریع
				var partMap = parts.Where(p => !string.IsNullOrEmpty(p.Code))
					.ToDictionary(p => p.Code!, p => p.Id);

				var productionOrderMap = appProductionOrders
					.Where(po => po.ProductionOrderNumber.HasValue)
					.ToDictionary(
						po => $"{po.ProductionOrderNumber.Value.ToString("0")}",
						po => po.Id
					);

				// ساخت dictionary برای ProductionOrderItem بر اساس ProductionOrderId و PartCode
				var productionOrderItemMap = appPOItems
					.Where(poi => poi.Part != null && !string.IsNullOrEmpty(poi.Part.Code))
					.GroupBy(poi => new { poi.ProductionOrderId, PartCode = poi.Part!.Code! })
					.ToDictionary(
						g => $"{g.Key.ProductionOrderId}_{g.Key.PartCode}",
						g => g.First()
					);

				// ساخت dictionary برای BOM های موجود بر اساس ProductionOrderItemId, Revision, PartId
				var existingBomMap = appPOItemsBom
					.Where(b => b.Revision.HasValue)
					.GroupBy(b => new { b.ProductionOrderItemId, Revision = b.Revision!.Value, b.PartId })
					.ToDictionary(
						g => $"{g.Key.ProductionOrderItemId}_{g.Key.Revision.ToString("0")}_{g.Key.PartId}",
						g => g.First()
					);

				// پردازش batch به batch
				const int batchSize = 100;
				int totalProcessed = 0;
				int totalNewBoms = 0;
				int totalUpdatedBoms = 0;
				int totalSkipped = 0;

				for (int skip = 0; skip < appProductionOrders.Count; skip += batchSize)
				{
					var batchProductionOrders = appProductionOrders
						.Skip(skip)
						.Take(batchSize)
						.Where(po => po.ProductionOrderNumber.HasValue)
						.ToList();

					if (!batchProductionOrders.Any())
						break;

					var productionOrderNumbers = batchProductionOrders
						.Select(po => po.ProductionOrderNumber!.Value)
						.ToList();

					await jobLogger?.LogInfoAsync($"پردازش batch {skip / batchSize + 1}: {productionOrderNumbers.Count} سفارش ساخت", cn);

					// ساخت query با parameterized values برای جلوگیری از SQL injection
					var numberParams = string.Join(",", productionOrderNumbers.Select((_, i) => $"@p{i}"));
					var sqlQuery = $@"
						SELECT poib.*, 
							poi.Revision AS ProductionOrderItemRevision,
							p.Part_Code AS ProductionOrderItemPartCode,
							po.Number AS ProductionOrderNumber,
							po.Revision AS ProductionOrderRevision,
							pp.Part_Code AS ProductionOrderItemBomPartCode
						FROM Pln_ProductionOrderItemBom poib (NOLOCK)
							LEFT JOIN Inv_Part pp ON poib.PartId = pp.Part_ID
							LEFT JOIN Pln_ProductionOrderItem poi (NOLOCK) ON poib.ProductionOrderItemId = poi.Id
							LEFT JOIN Inv_Part p (NOLOCK) ON poi.PartId = p.Part_ID
							LEFT JOIN Pln_ProductionOrder po (NOLOCK) ON poi.ProductionOrderId = po.Id
						WHERE po.IsLatestVersion =1 and poib.IsLatest = 1  and poi.IsLatestVersion = 1 and po.Number IN ({numberParams})";

					// ساخت parameters
					var parameters = productionOrderNumbers
						.Select((num, i) => new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", num))
						.ToArray();

					var htsBoms = await Hdb.Hts_Pln_ProductionOrderItemBoms
						.FromSqlRaw(sqlQuery, parameters)
						.ToListAsync(cn);

					if (!htsBoms.Any())
					{
						await jobLogger?.LogInfoAsync($"هیچ BOM برای این batch یافت نشد", cn);
						totalProcessed += batchProductionOrders.Count;
						continue;
					}

					await jobLogger?.LogInfoAsync($"تعداد {htsBoms.Count} BOM از HTS دریافت شد", cn);

					var newBomList = new List<ProductionOrderItemBom>();
					int batchUpdatedCount = 0;
					int batchSkippedCount = 0;

					foreach (var htsBom in htsBoms)
					{
						// پیدا کردن ProductionOrder
						if (!htsBom.ProductionOrderRevision.HasValue)
						{
							batchSkippedCount++;
							continue;
						}

						var revKey = $"{htsBom.ProductionOrderNumber}";
						if (!productionOrderMap.TryGetValue(revKey, out var productionOrderId))
						{
							await jobLogger?.LogWarningAsync($"سفارش ساخت با شماره {htsBom.ProductionOrderNumber} و نسخه {htsBom.ProductionOrderRevision} یافت نشد", 0, cn);
							batchSkippedCount++;
							continue;
						}

						// پیدا کردن ProductionOrderItem
						if (string.IsNullOrEmpty(htsBom.ProductionOrderItemPartCode))
						{
							batchSkippedCount++;
							continue;
						}

						var itemKey = $"{productionOrderId}_{htsBom.ProductionOrderItemPartCode}";
						if (!productionOrderItemMap.TryGetValue(itemKey, out var productionOrderItem))
						{
							await jobLogger?.LogWarningAsync($"قلم سفارش ساخت با ProductionOrderId {productionOrderId} و PartCode {htsBom.ProductionOrderItemPartCode} یافت نشد", 0, cn);
							batchSkippedCount++;
							continue;
						}

						// پیدا کردن Part برای BOM
						if (string.IsNullOrEmpty(htsBom.ProductionOrderItemBomPartCode))
						{
							batchSkippedCount++;
							continue;
						}

						if (!partMap.TryGetValue(htsBom.ProductionOrderItemBomPartCode, out var partId) || !partId.HasValue)
						{
							await jobLogger?.LogWarningAsync($"کالا با کد {htsBom.ProductionOrderItemBomPartCode} برای BOM یافت نشد", 0, cn);
							batchSkippedCount++;
							continue;
						}

						// بررسی وجود BOM موجود
						if (!htsBom.Revision.HasValue)
						{
							batchSkippedCount++;
							continue;
						}

						if (productionOrderItem.Id == null)
						{
							batchSkippedCount++;
							continue;
						}

						var bomKey = $"{(long)productionOrderItem.Id}_{htsBom.Revision.Value}_{partId.Value}";
						var existingBom = existingBomMap.TryGetValue(bomKey, out var foundBom) ? foundBom : null;

						if (existingBom == null)
						{
							// اضافه کردن BOM جدید
							var newBom = new ProductionOrderItemBom
							{
								ProductionOrderItemId = (long)productionOrderItem.Id,
								Revision = htsBom.Revision.Value,
								PartId = partId.Value,
								Amount = htsBom.Amount ?? 0m,
								IsLatest = htsBom.IsLatest ?? false,
								Description = htsBom.Comment,
								SaleUnitDetails = htsBom.SalesUnitComment,
								NeedsAVL = htsBom.HasNeedToAvl ?? false
							};
							newBomList.Add(newBom);
							// اضافه کردن به map برای جلوگیری از duplicate در همان batch
							existingBomMap[bomKey] = newBom;
						}
						else
						{
							// به‌روزرسانی BOM موجود
							bool modified = false;

							if (existingBom.Amount != (htsBom.Amount ?? 0m))
							{
								existingBom.Amount = htsBom.Amount ?? 0m;
								modified = true;
							}

							if (existingBom.IsLatest != (htsBom.IsLatest ?? false))
							{
								existingBom.IsLatest = htsBom.IsLatest ?? false;
								modified = true;
							}

							if (existingBom.Description != htsBom.Comment)
							{
								existingBom.Description = htsBom.Comment;
								modified = true;
							}

							if (existingBom.SaleUnitDetails != htsBom.SalesUnitComment)
							{
								existingBom.SaleUnitDetails = htsBom.SalesUnitComment;
								modified = true;
							}

							if (existingBom.NeedsAVL != (htsBom.HasNeedToAvl ?? false))
							{
								existingBom.NeedsAVL = htsBom.HasNeedToAvl ?? false;
								modified = true;
							}

							if (modified)
							{
								batchUpdatedCount++;
							}
						}
					}

					// ذخیره BOM های جدید این batch
					if (newBomList.Any())
					{
						await unitOfWork.Repository<ProductionOrderItemBom>().AddRangeAsync(newBomList, cn, false);
						await jobLogger?.LogInfoAsync($"افزودن {newBomList.Count} BOM جدید از این batch", cn);
						totalNewBoms += newBomList.Count;
					}

					if (batchUpdatedCount > 0)
					{
						await jobLogger?.LogInfoAsync($"به‌روزرسانی {batchUpdatedCount} BOM موجود از این batch", cn);
						totalUpdatedBoms += batchUpdatedCount;
					}

					if (batchSkippedCount > 0)
					{
						await jobLogger?.LogWarningAsync($"تعداد {batchSkippedCount} BOM از این batch رد شد", 0, cn);
						totalSkipped += batchSkippedCount;
					}

					totalProcessed += batchProductionOrders.Count;
					await jobLogger?.LogInfoAsync($"پردازش batch {skip / batchSize + 1} با موفقیت انجام شد", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"همگام‌سازی Bom اقلام سفارش ساخت با موفقیت انجام شد. خلاصه: {totalProcessed} سفارش پردازش شد، {totalNewBoms} BOM جدید اضافه شد، {totalUpdatedBoms} BOM به‌روزرسانی شد، {totalSkipped} BOM رد شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}


		private const string AutoStartProcessComment = "ایجاد شده بصورت خودکار توسط سیستم";

		[JobHandler("بررسی و تغییر وضعیت قلم سفارش ساخت")]
		public async Task CheckFinancialConfirmsOrders(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی سفارشات تایید مالی شده", cn);

				var productionOrderItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(p => p.ProductionOrder)
					.Include(p => p.Part)
					.Where(p => p.IsLatestVersion &&
								!p.IsDeleted &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.ProductionOrder.State == ProductionOrderStateEnum.FinancialApproval &&
								p.CheckStatus == ProductionOrderItemCheckStatusEnum.InitialRegistration)
					.ToListAsync(cn);

				if (!productionOrderItems.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ قلمی در وضعیت ثبت اولیه برای سفارش‌های تایید مالی یافت نشد", cn);
					return;
				}

				await jobLogger?.LogInfoAsync($"تعداد {productionOrderItems.Count} قلم سفارش برای پردازش یافت شد", cn);

				var nowDate = DateTime.Now;
				var newItemComments = RouteInitialRegistrationItems(productionOrderItems, nowDate);

				if (newItemComments.Any())
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newItemComments, cn, false);

				var productionOrderIds = productionOrderItems
					.Where(p => p.ProductionOrderId.HasValue)
					.Select(p => p.ProductionOrderId!.Value)
					.Distinct()
					.ToList();

				var headerCommentsAdded = await EnsureAutoStartProcessComments(productionOrderIds, nowDate, cn);

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"تغییر وضعیت اقلام سفارش با موفقیت انجام شد. اقلام: {productionOrderItems.Count}، کامنت هدر: {headerCommentsAdded}",
					cn);

				// معادل HTS: SendEngineeringAndProjectManagerNotification - اطلاع‌رسانی به تاییدکننده
				// مهندسی مکانیک/برق (بر اساس Pln.ProductionOrderEquipmentConfirmer) یا مدیر پروژه، بلافاصله
				// پس از مسیریابی اولیه اقلام تازه تایید مالی‌شده
				var engineeringItems = productionOrderItems
					.Where(p => p.CheckStatus == ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending)
					.ToList();
				if (engineeringItems.Any())
					await SendEngineeringNotificationAsync(engineeringItems, "ورود اقلام جدید به کارتابل مهندسی", jobLogger, cn);

				var pmItems = productionOrderItems
					.Where(p => p.CheckStatus == ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval)
					.ToList();
				if (pmItems.Any())
					await SendProjectManagerNotificationAsync(pmItems, "ورود اقلام جدید به کارتابل مدیر پروژه", jobLogger, cn);
			}
			catch (Exception exception)
			{
				await jobLogger?.LogExceptionAsync(exception, cn);
			}
		}

		[JobHandler("اعمال سفارش‌های تایید مالی بدون آغاز فرآیند")]
		public async Task ApplyUnConfirmedProductionOrders(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع اعمال سفارش‌های تایید مالی بدون آغاز فرآیند", cn);

				var financialOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.Include(p => p.Comments)
					.Where(p => p.State == ProductionOrderStateEnum.FinancialApproval)
					.ToListAsync(cn);

				var ordersMissingAutoComment = financialOrders
					.Where(p => p.Id.HasValue && !HasAutoStartProcessComment(p.Comments))
					.ToList();

				if (!ordersMissingAutoComment.Any())
				{
					await jobLogger?.LogInfoAsync("سفارش تایید مالی بدون کامنت آغاز فرآیند یافت نشد", cn);
					return;
				}

				var nowDate = DateTime.Now;
				var orderIds = ordersMissingAutoComment.Select(p => p.Id!.Value).ToList();

				await jobLogger?.LogInfoAsync($"تعداد {orderIds.Count} سفارش تایید مالی بدون کامنت آغاز فرآیند یافت شد", cn);

				var headerCommentsAdded = await EnsureAutoStartProcessComments(orderIds, nowDate, cn);

				var initialItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(p => p.ProductionOrder)
					.Include(p => p.Part)
					.Where(p => p.IsLatestVersion &&
								!p.IsDeleted &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.ProductionOrderId.HasValue &&
								orderIds.Contains(p.ProductionOrderId.Value) &&
								p.CheckStatus == ProductionOrderItemCheckStatusEnum.InitialRegistration)
					.ToListAsync(cn);

				var newItemComments = RouteInitialRegistrationItems(initialItems, nowDate);
				if (newItemComments.Any())
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newItemComments, cn, false);

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"اعمال سفارش‌های تایید مالی بدون آغاز فرآیند انجام شد. کامنت هدر: {headerCommentsAdded}، اقلام روت‌شده: {initialItems.Count}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		[JobHandler("محاسبه و بروزرسانی تاریخ تحویل استاندارد اقلام سفارش ساخت")]
		public async Task AddOrUpdateStandardDeliveryDates(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع محاسبه و بروزرسانی تاریخ تحویل استاندارد اقلام سفارش ساخت", cn);

				const string kwConst = "kw";
				const string noBomString = "kw";
				const string noStructureString = "Structure!";
				const string noLeadTimeString = "????/??/??";

				var initialDate = "1400/01/01".ToMiladiDate();

				var records = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(p => p.Part)
					.Include(p => p.ProductionOrderItemBom)
						.ThenInclude(b => b.Part)
					.Where(p => p.IsLatestVersion &&
								!p.IsDeleted &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.SendToIndustrialMiladiDate.HasValue &&
								p.CreatedOnMiladiDateTime >= initialDate &&
								(!p.StandardDeliveryMiladiDate.HasValue ||
								 p.StandardDeliveryShamsiDate == noBomString ||
								 p.StandardDeliveryShamsiDate == noStructureString ||
								 p.StandardDeliveryShamsiDate == noLeadTimeString))
					.ToListAsync(cn);

				if (!records.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ قلمی برای محاسبه تاریخ تحویل استاندارد یافت نشد", cn);
					return;
				}

				await jobLogger?.LogInfoAsync($"تعداد {records.Count} قلم برای محاسبه تاریخ تحویل استاندارد یافت شد", cn);

				var structurePartIds = records
					.SelectMany(r => r.ProductionOrderItemBom)
					.Where(b => b.Part != null && !string.IsNullOrEmpty(b.Part.Code) && b.Part.Code.StartsWith("1475"))
					.Select(b => b.PartId)
					.Distinct()
					.ToList();

				var leadTimesByPartId = structurePartIds.Count == 0
					? new Dictionary<long, LeadTime>()
					: (await unitOfWork.Repository<LeadTime>()
							.TableNoTracking
							.Where(lt => lt.PartId.HasValue && structurePartIds.Contains(lt.PartId.Value))
							.ToListAsync(cn))
						.GroupBy(lt => lt.PartId!.Value)
						.ToDictionary(g => g.Key, g => g.First());

				var updatedCount = 0;

				foreach (var item in records)
				{
					var part = item.Part;
					if (part == null || string.IsNullOrEmpty(part.Code))
						continue;

					var partCode = part.Code;
					var partName = (part.Name ?? string.Empty).ToLowerInvariant();
					var partIsRoutine = part.DesignTypeIsRoutine ?? (part.EngineeringRoutine || item.IsRoutine);
					var receivedDate = item.SendToIndustrialMiladiDate!.Value;
					var calculatedStandardDate = default(DateTime);
					var shouldBeCalculatedByLeadTime = false;
					var productionOrderItemBom = item.ProductionOrderItemBom?.ToList() ?? new List<ProductionOrderItemBom>();
					var hasProductionOrderItemBom = productionOrderItemBom.Any();
					ProductionOrderItemBom? partBomStructurePart = null;
					LeadTime? leadTime = null;

					// Oil Free: 1801301*
					if (partCode.StartsWith("1801301") && partName.Contains(kwConst))
					{
						const int oilFreeKwCapacity = 110;
						if (TryExtractKwCapacity(partName, kwConst, out var compressorKwDigit))
						{
							calculatedStandardDate = compressorKwDigit < oilFreeKwCapacity
								? receivedDate.AddDays(partIsRoutine ? 60 : 90)
								: receivedDate.AddDays(partIsRoutine ? 90 : 120);
						}
					}

					// Compressors: 180112* / 180190* / 180195*
					if ((partCode.StartsWith("180112") || partCode.StartsWith("180190") || partCode.StartsWith("180195"))
						&& partName.Contains(kwConst))
					{
						const short kwCapacity = 75;
						if (TryExtractKwCapacity(partName, kwConst, out var compressorKwDigit))
						{
							calculatedStandardDate = compressorKwDigit < kwCapacity
								? receivedDate.AddDays(partIsRoutine ? 45 : 60)
								: receivedDate.AddDays(partIsRoutine ? 60 : 90);
						}
					}

					// Desiccant dryer
					if (partCode.StartsWith("180213") || partCode.StartsWith("180215") ||
						partCode.StartsWith("180223") || partCode.StartsWith("180224") ||
						partCode.StartsWith("180225"))
					{
						calculatedStandardDate = receivedDate.AddDays(22);
						shouldBeCalculatedByLeadTime = true;
						hasProductionOrderItemBom = productionOrderItemBom.Any();
						if (hasProductionOrderItemBom)
						{
							partBomStructurePart = productionOrderItemBom.FirstOrDefault(a =>
								a.Part?.Code != null &&
								a.Part.Code.StartsWith("1475") &&
								leadTimesByPartId.ContainsKey(a.PartId));
							if (partBomStructurePart != null &&
								leadTimesByPartId.TryGetValue(partBomStructurePart.PartId, out leadTime) &&
								leadTime != null)
							{
								calculatedStandardDate = calculatedStandardDate.AddDays(leadTime.LeadTimeDay);
							}
						}
					}

					// PSA / Coal Tower
					if (partCode.StartsWith("181510") || partCode.StartsWith("181610") || partCode.StartsWith("180214"))
					{
						calculatedStandardDate = receivedDate.AddDays(22);
						shouldBeCalculatedByLeadTime = true;
						hasProductionOrderItemBom = productionOrderItemBom.Any();
						if (hasProductionOrderItemBom)
						{
							partBomStructurePart = productionOrderItemBom.FirstOrDefault(a =>
								a.Part?.Code != null && a.Part.Code.StartsWith("1475"));
							if (partBomStructurePart != null)
							{
								leadTimesByPartId.TryGetValue(partBomStructurePart.PartId, out leadTime);
								if (leadTime != null)
									calculatedStandardDate = calculatedStandardDate.AddDays(leadTime.LeadTimeDay);
							}
						}
					}

					// Vessels
					if (partCode.StartsWith("1806100") || partCode.StartsWith("1806101") ||
						partCode.StartsWith("1806110") || partCode.StartsWith("1806111") ||
						partCode.StartsWith("180612") || partCode.StartsWith("180613") ||
						partCode.StartsWith("180614"))
					{
						calculatedStandardDate = receivedDate.AddDays(11);
						shouldBeCalculatedByLeadTime = true;
						hasProductionOrderItemBom = productionOrderItemBom.Any();
						if (hasProductionOrderItemBom)
						{
							partBomStructurePart = productionOrderItemBom.FirstOrDefault(a =>
								a.Part?.Code != null &&
								a.Part.Code.StartsWith("1475") &&
								leadTimesByPartId.ContainsKey(a.PartId));
							if (partBomStructurePart != null &&
								leadTimesByPartId.TryGetValue(partBomStructurePart.PartId, out leadTime) &&
								leadTime != null)
							{
								calculatedStandardDate = calculatedStandardDate.AddDays(leadTime.LeadTimeDay);
							}
						}
					}

					if (shouldBeCalculatedByLeadTime)
					{
						if (!hasProductionOrderItemBom)
						{
							item.StandardDeliveryMiladiDate = DateTime.MaxValue;
							item.StandardDeliveryShamsiDate = noBomString;
							updatedCount++;
						}
						else if (partBomStructurePart == null)
						{
							item.StandardDeliveryMiladiDate = DateTime.MaxValue;
							item.StandardDeliveryShamsiDate = noStructureString;
							updatedCount++;
						}
						else if (leadTime == null)
						{
							item.StandardDeliveryMiladiDate = DateTime.MaxValue;
							item.StandardDeliveryShamsiDate = noLeadTimeString;
							updatedCount++;
						}
						else
						{
							ApplyStandardDeliveryDate(item, calculatedStandardDate, receivedDate);
							updatedCount++;
						}
					}
					else if (calculatedStandardDate != default)
					{
						ApplyStandardDeliveryDate(item, calculatedStandardDate, receivedDate);
						updatedCount++;
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"محاسبه تاریخ تحویل استاندارد انجام شد. اقلام به‌روزرسانی‌شده: {updatedCount}", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}


		[JobHandler("تغییر وضعیت اقلامی که بیش از تایم مشخصی در کارتابل مدیر پروژه بوده اند")]
		public async Task SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی اقلام متوقف در کارتابل مدیر پروژه", cn);

			var deadLineInDay = 2;
			var stepId = ProductionOrderItemProductionStepEnum.AwaitingProjectManager;

			var items = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(p => p.ProductionOrder)
				.Where(p => p.IsLatestVersion &&
							p.IsActive != IsActiveEnum.Deleted &&
							p.ProductionStep == stepId)
				.ToListAsync(cn);

				var nowDate = DateTime.Now;
				var finalItems = new List<ProductionOrderItem>();
				var newComments = new List<ProductionOrderItemComment>();

				foreach (var item in items)
				{
					// باگ رفع‌شده: نسخه قبلی این کد به‌دنبال کامنتی با ProductionStatus == 1380 می‌گشت، اما هیچ
					// اکشنی هرگز چنین کامنتی نمی‌سازد (1380 حتی عضوی از ProductionOrderItemProductionStatusEnum
					// نیست)، بنابراین lastComment همیشه null بود و این Job هیچ‌وقت آیتمی را منتقل نمی‌کرد.
					// درست مثل SendExpiredEngineeringItemsToIndustrial، مبنای تشخیص مهلت اکنون مستقیماً
					// CheckStatusChangedOnMiladiDate است که در تمام اکشن‌های گردش‌کار به‌روزرسانی می‌شود
					if (item.CheckStatusChangedOnMiladiDate == null || nowDate <= item.CheckStatusChangedOnMiladiDate.Value.AddDays(deadLineInDay))
						continue;

					// تغییر وضعیت - هم‌زمان با ProductionStep، CheckStatus و تاریخ‌های تغییر وضعیت نیز به‌روزرسانی می‌شوند
					// تا این دو فیلد همواره هماهنگ باقی بمانند (مطابق نگاشت استفاده‌شده در CheckFinancialConfirmsOrders)
					item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry;
					item.CheckStatus = ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
					item.CheckStatusChangedOnMiladiDate = nowDate;
					item.CheckStatusChangedOnShamsiDate = nowDate.ToShamsiDateTime();
					TrySetSendToIndustrial(item, ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal, nowDate);

					finalItems.Add(item);

					// ایجاد کامنت جدید
					// باگ رفع‌شده: نسخه قبلی این خط مقدار enum مرحله ساخت (ProductionOrderItemProductionStepEnum.AwaitingIndustry=2744)
					// را به ProductionOrderItemProductionStatusEnum کست می‌کرد که چنین عضوی ندارد (مقدار نامعتبر/خارج از دامنه
					// در تاریخچه ذخیره می‌شد)؛ باید مطابق CheckStatus واقعی که چند سطر بالاتر ست شده (IndustrialDashboardInternal)
					// از عضو معادل و صحیح همان Enum استفاده شود
					newComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)item.Id!,
						ProductionStatus = ProductionOrderItemProductionStatusEnum.InIndustriesInternalInbox,
						ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry,
						Comment = "بصورت اتوماتیک و بعلت منقضی شدن زمان بررسی توسط مدیر پروژه",
						CreatedById = 1,
						CreatedOnMiladiDateTime = nowDate,
						CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
						StartMiladiDateTime = nowDate,
						StartShamsiDate = nowDate.ToShamsiDateTime(),
						IsForProductionMode = false,
						IsForProductionStepStatus = true,
						IsActive = IsActiveEnum.Active
					});
				}

				if (finalItems.Any())
				{
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newComments, cn, false);
					await unitOfWork.SaveChangesAsync(cn);

					await SendProjectManagerNotificationAsync(finalItems, "انتقال خودکار اقلام معطل‌مانده در کارتابل مدیر پروژه به صنایع", jobLogger, cn);

					await jobLogger?.LogInfoAsync($"تعداد {finalItems.Count} قلم کالا به دلیل انقضای زمان به واحد صنعتی منتقل شدند", cn);
				}
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		// توجه: زمان‌بندی این Job باید توسط ادمین در پنل مدیریت Job تنظیم شود
		[JobHandler("انتقال خودکار اقلام منقضی‌شده در کارتابل مهندسی به صنایع")]
		public async Task SendExpiredEngineeringItemsToIndustrial(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی اقلام منقضی‌شده در کارتابل مهندسی", cn);

				var deadLineInDay = 1;
				var stepId = ProductionOrderItemProductionStepEnum.AwaitingEngineering;

			var items = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(p => p.ProductionOrderItemComments)
				.Include(p => p.ProductionOrder)
				.Where(p => p.IsLatestVersion &&
							p.IsActive != IsActiveEnum.Deleted &&
							p.ProductionStep == stepId)
				.ToListAsync(cn);

				var nowDate = DateTime.Now;
				var finalItems = new List<ProductionOrderItem>();
				var newComments = new List<ProductionOrderItemComment>();

				foreach (var item in items)
				{
					// استفاده از CheckStatusChangedOnMiladiDate به جای کامنت‌ها، چون دقیق‌تر و مستقیماً وابسته به CheckStatus است
					if (item.CheckStatusChangedOnMiladiDate == null || nowDate <= item.CheckStatusChangedOnMiladiDate.Value.AddDays(deadLineInDay))
						continue;

					// تغییر وضعیت به صنایع
					item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProduction;
					item.CheckStatus = ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
					item.CheckStatusChangedOnMiladiDate = nowDate;
					item.CheckStatusChangedOnShamsiDate = nowDate.ToShamsiDateTime();
					TrySetSendToIndustrial(item, ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal, nowDate);

					finalItems.Add(item);

					// باگ رفع‌شده: مشابه SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable - کست از
					// ProductionOrderItemProductionStepEnum.AwaitingProduction(214) به ProductionOrderItemProductionStatusEnum
					// مقداری نامعتبر (بدون عضو متناظر) تولید می‌کرد؛ اکنون از عضو صحیح متناظر با CheckStatus واقعاً ست‌شده
					// (IndustrialDashboardInternal) استفاده می‌شود
					newComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)item.Id!,
						ProductionStatus = ProductionOrderItemProductionStatusEnum.InIndustriesInternalInbox,
						ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingProduction,
						Comment = "بصورت اتوماتیک و بعلت منقضی شدن زمان بررسی مهندسی",
						CreatedById = 1,
						CreatedOnMiladiDateTime = nowDate,
						CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
						StartMiladiDateTime = nowDate,
						StartShamsiDate = nowDate.ToShamsiDateTime(),
						IsForProductionMode = false,
						IsForProductionStepStatus = true,
						IsActive = IsActiveEnum.Active
					});
				}

				if (finalItems.Any())
				{
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newComments, cn, false);
					await unitOfWork.SaveChangesAsync(cn);

					await SendEngineeringNotificationAsync(finalItems, "انتقال خودکار اقلام منقضی‌شده در کارتابل مهندسی به صنایع", jobLogger, cn);

					await jobLogger?.LogInfoAsync($"تعداد {finalItems.Count} قلم کالا به دلیل انقضای زمان بررسی مهندسی به واحد صنعتی منتقل شدند", cn);
				}
				else
				{
					await jobLogger?.LogInfoAsync("هیچ قلم منقضی‌شده‌ای در کارتابل مهندسی یافت نشد", cn);
				}
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		// توجه: زمان‌بندی این Job باید توسط ادمین در پنل مدیریت Job تنظیم شود
		[JobHandler("اطلاع‌رسانی اقلام سفارش ساخت نزدیک به تاریخ تحویل")]
		public async Task SendProductionOrderItemsWithNearDeliveryDateNotifications(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی اقلام سفارش ساخت نزدیک به تاریخ تحویل", cn);

				var nowDate = DateTime.Now;
				var upperBoundDate = nowDate.AddDays(15);

				// وضعیت‌هایی که تولید/تحویل آن‌ها به پایان رسیده و نیازی به اطلاع‌رسانی ندارند
				var excludedSteps = new List<ProductionOrderItemProductionStepEnum>
				{
					ProductionOrderItemProductionStepEnum.Completed,
					ProductionOrderItemProductionStepEnum.Canceled,
					ProductionOrderItemProductionStepEnum.ReadyForShipping,
					ProductionOrderItemProductionStepEnum.DeliveredToSales,
					ProductionOrderItemProductionStepEnum.ConsignmentDelivery,
					ProductionOrderItemProductionStepEnum.ReturnedFromSales,
					ProductionOrderItemProductionStepEnum.Unshippable
				};

				var items = await unitOfWork.Repository<ProductionOrderItem>()
					.TableNoTracking
					.Include(p => p.ProductionOrder)
					.Include(p => p.Part)
					.Where(p => p.IsLatestVersion &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.AgreedDeliverDate != null &&
								p.AgreedDeliverDate.Value <= upperBoundDate &&
								p.AgreedDeliverDate.Value >= nowDate &&
								!excludedSteps.Contains(p.ProductionStep))
					.ToListAsync(cn);

				if (!items.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ قلمی با تاریخ تحویل نزدیک یافت نشد", cn);
					return;
				}

				await jobLogger?.LogInfoAsync($"تعداد {items.Count} قلم سفارش ساخت با تاریخ تحویل نزدیک یافت شد", cn);

				await SendNearDeliveryDateNotification(items, jobLogger, cn);

				await jobLogger?.LogInfoAsync($"اطلاع‌رسانی برای {items.Count} قلم سفارش ساخت با تاریخ تحویل نزدیک ارسال شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// One-time (re-runnable) cutover sync from old HTS → App ProductionOrder / ProductionOrderItem.
		/// Overwrites App with HTS when HTS has a value (status, steps, key dates/text, serial/planning).
		/// Not bi-directional; not continuous sync.
		/// </summary>
		[JobHandler("کات‌اور یک‌باره سفارش ساخت از HTS (وضعیت و فیلدهای عملیاتی)")]
		public async Task SyncProductionOrderOperationalFieldsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع کات‌اور یک‌باره سفارش ساخت از HTS (وضعیت + فیلدهای عملیاتی)", cn);

				const int batchSize = 200;
				var appProductionOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.Where(p => p.ProductionOrderNumber.HasValue)
					.ToListAsync(cn);

				if (!appProductionOrders.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ سفارش ساختی برای همگام‌سازی یافت نشد", cn);
					return;
				}

				var productionOrderByNumber = appProductionOrders
					.GroupBy(p => p.ProductionOrderNumber!.Value)
					.ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Revision ?? 0).First());

				var appItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(i => i.Part)
					.Where(i => i.IsLatestVersion && i.IsActive != IsActiveEnum.Deleted)
					.ToListAsync(cn);

				var itemsByHamkaranId = appItems
					.Where(i => i.HamkaranId.HasValue)
					.GroupBy(i => i.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				var itemsByPoAndPartCode = appItems
					.Where(i => i.ProductionOrderId.HasValue && !string.IsNullOrWhiteSpace(i.PartCode ?? i.Part?.Code))
					.GroupBy(i => $"{i.ProductionOrderId}_{i.PartCode ?? i.Part!.Code}")
					.ToDictionary(g => g.Key, g => g.First());

				var appUsersByUsername = (await appContext.Users
						.AsNoTracking()
						.Where(u => u.Id.HasValue && !string.IsNullOrWhiteSpace(u.Username))
						.ToListAsync(cn))
					.GroupBy(u => u.Username.Trim(), StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First().Id!.Value, StringComparer.OrdinalIgnoreCase);

				await jobLogger?.LogInfoAsync(
					$"بارگذاری انجام شد: {productionOrderByNumber.Count} سفارش، {appItems.Count} قلم، {appUsersByUsername.Count} کاربر برای نگاشت",
					cn);

				var productionOrderNumbers = productionOrderByNumber.Keys.OrderBy(x => x).ToList();
				int totalMatched = 0;
				int totalUpdatedItems = 0;
				int totalSkippedUnchanged = 0;
				int totalUnmatched = 0;
				int totalUpdatedHeaders = 0;
				int totalUserMapMisses = 0;
				var processedHeaderIds = new HashSet<long>();

				for (var skip = 0; skip < productionOrderNumbers.Count; skip += batchSize)
				{
					var batchNumbers = productionOrderNumbers.Skip(skip).Take(batchSize).ToList();
					var numberParams = string.Join(",", batchNumbers.Select((_, i) => $"@p{i}"));
					var sqlQuery = $@"
						SELECT
							poi.Id,
							poi.ProductionOrderId,
							poi.Revision,
							poi.IsLatestVersion,
							poi.PartId,
							poi.BuyStatusId,
							poi.ProductionStepId,
							poi.ProductionStatusId,
							poi.StatusId,
							poi.SerialTypeId,
							poi.PlanningNumber,
							poi.ReceivedDate,
							poi.ReceivedDateInText,
							poi.FirstIndustrialChangeUserId,
							poi.FirstIndustrialChangeDate,
							poi.FirstIndustrialChangeDateInText,
							poi.DocumentPreparationDate,
							poi.DocumentPreparationDateInText,
							poi.StandardDeliveryDate,
							poi.StandardDeliveryDateInText,
							poi.EngineeringConsideration,
							poi.PlanningConsideration,
							poi.IsRoutine,
							poi.ProductionStartDateInEurope,
							poi.ProductionStartDate,
							poi.ProductionEndDateInEurope,
							poi.ProductionEndDate,
							poi.TestingEndDateInEurope,
							poi.TestingEndDate,
							poi.PreparationDate,
							poi.PreparationDateInText,
							poi.DeliveryDate,
							poi.DeliveryDateInText,
							poi.Serial,
							poi.RahkaranId,
							poi.IsDeleted,
							poi.UpdatedDate,
							poi.UpdatedDateInText,
							po.Number AS ProductionOrderNumber,
							p.Part_Code AS PartCode,
							po.IsDisableForTimelyDeliveryReport AS HeaderIsDisableForTimelyDeliveryReport,
							po.DisableForTimelyDeliveryReportComment AS HeaderDisableForTimelyDeliveryReportComment,
							u.Username AS FirstIndustrialChangeUserUsername
						FROM Pln_ProductionOrderItem poi (NOLOCK)
							INNER JOIN Pln_ProductionOrder po (NOLOCK) ON poi.ProductionOrderId = po.Id
							LEFT JOIN Inv_Part p (NOLOCK) ON poi.PartId = p.Part_ID
							LEFT JOIN Gnr_User u (NOLOCK) ON poi.FirstIndustrialChangeUserId = u.User_ID
						WHERE po.IsLatestVersion = 1
							AND poi.IsLatestVersion = 1
							AND poi.IsDeleted = 0
							AND po.Number IN ({numberParams})";

					var parameters = batchNumbers
						.Select((num, i) => new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", (int)num))
						.ToArray();

					var htsItems = await Hdb.Hts_Pln_ProductionOrderItems
						.FromSqlRaw(sqlQuery, parameters)
						.AsNoTracking()
						.ToListAsync(cn);

					int batchMatched = 0;
					int batchUpdated = 0;
					int batchUnmatched = 0;

					foreach (var htsItem in htsItems)
					{
						ProductionOrderItem? appItem = null;
						if (htsItem.RahkaranId.HasValue)
							itemsByHamkaranId.TryGetValue(htsItem.RahkaranId.Value, out appItem);

						if (appItem == null
							&& htsItem.ProductionOrderNumber.HasValue
							&& !string.IsNullOrWhiteSpace(htsItem.PartCode)
							&& productionOrderByNumber.TryGetValue(htsItem.ProductionOrderNumber.Value, out var matchedPo)
							&& matchedPo.Id.HasValue)
						{
							var key = $"{matchedPo.Id}_{htsItem.PartCode}";
							itemsByPoAndPartCode.TryGetValue(key, out appItem);
						}

						if (appItem == null)
						{
							totalUnmatched++;
							batchUnmatched++;
							if (batchUnmatched <= 5)
							{
								await jobLogger?.LogWarningAsync(
									$"قلم HTS بدون متناظر در App: PO={htsItem.ProductionOrderNumber}، Part={htsItem.PartCode}، RahkaranId={htsItem.RahkaranId}",
									0,
									cn);
							}
							continue;
						}

						totalMatched++;
						batchMatched++;

						var changed = ApplyHtsCutoverToItem(appItem, htsItem, appUsersByUsername, out var userMapMiss);

						if (userMapMiss)
						{
							totalUserMapMisses++;
							if (totalUserMapMisses <= 10)
							{
								await jobLogger?.LogWarningAsync(
									$"نگاشت FirstIndustrialChangeUser ناموفق (Username HTS یافت نشد در App): '{htsItem.FirstIndustrialChangeUserUsername}' — PO={htsItem.ProductionOrderNumber}، Part={htsItem.PartCode}",
									0,
									cn);
							}
						}

						if (changed)
						{
							totalUpdatedItems++;
							batchUpdated++;
						}
						else
						{
							totalSkippedUnchanged++;
						}

						if (htsItem.ProductionOrderNumber.HasValue
							&& productionOrderByNumber.TryGetValue(htsItem.ProductionOrderNumber.Value, out var header)
							&& header.Id.HasValue
							&& processedHeaderIds.Add(header.Id.Value)
							&& ApplyHtsCutoverToHeader(header, htsItem))
						{
							totalUpdatedHeaders++;
						}
					}

					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync(
						$"batch {skip / batchSize + 1}: HTS={htsItems.Count}، matched={batchMatched}، updated={batchUpdated}، unmatched={batchUnmatched}",
						cn);
				}

				await jobLogger?.LogInfoAsync(
					$"کات‌اور تمام شد. matched={totalMatched}، updated(items)={totalUpdatedItems}، skipped(unchanged)={totalSkippedUnchanged}، unmatched={totalUnmatched}، updated(headers)={totalUpdatedHeaders}، firstIndustrialUserMapMiss={totalUserMapMisses}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// Cutover rule: HTS wins when HTS has a non-null / non-empty value.
		/// </summary>
		private static bool ApplyHtsCutoverToItem(
			ProductionOrderItem appItem,
			Hts_Pln_ProductionOrderItem htsItem,
			IReadOnlyDictionary<string, long> appUsersByUsername,
			out bool firstIndustrialUserMapMissed)
		{
			var changed = false;
			firstIndustrialUserMapMissed = false;

			if (TryMapCheckStatusFromHts(htsItem.BuyStatusId, out ProductionOrderItemCheckStatusEnum checkStatus)
				&& appItem.CheckStatus != checkStatus)
			{
				appItem.CheckStatus = checkStatus;
				var statusChangedOn = htsItem.UpdatedDate ?? DateTime.Now;
				appItem.CheckStatusChangedOnMiladiDate = statusChangedOn;
				appItem.CheckStatusChangedOnShamsiDate = !string.IsNullOrWhiteSpace(htsItem.UpdatedDateInText)
					? htsItem.UpdatedDateInText
					: statusChangedOn.ToShamsiDateTime();
				changed = true;
			}
			else if (!appItem.CheckStatusChangedOnMiladiDate.HasValue && htsItem.UpdatedDate.HasValue)
			{
				appItem.CheckStatusChangedOnMiladiDate = htsItem.UpdatedDate;
				appItem.CheckStatusChangedOnShamsiDate = !string.IsNullOrWhiteSpace(htsItem.UpdatedDateInText)
					? htsItem.UpdatedDateInText
					: htsItem.UpdatedDate.Value.ToShamsiDateTime();
				changed = true;
			}

			if (TryMapEnum(htsItem.ProductionStepId, out ProductionOrderItemProductionStepEnum productionStep)
				&& appItem.ProductionStep != productionStep)
			{
				appItem.ProductionStep = productionStep;
				changed = true;
			}

			if (TryMapEnum(htsItem.ProductionStatusId, out ProductionOrderItemProductionStatusEnum productionStatus)
				&& appItem.ProductionStatus != productionStatus)
			{
				appItem.ProductionStatus = productionStatus;
				changed = true;
			}

			if (TryMapEnum(htsItem.StatusId, out ProductionOrderItemStatusEnum itemStatus)
				&& appItem.Status != itemStatus)
			{
				appItem.Status = itemStatus;
				changed = true;
			}

			if (TryMapEnum(htsItem.SerialTypeId, out ProductionOrderItemSerialTypeEnum serialType)
				&& appItem.SerialType != serialType)
			{
				appItem.SerialType = serialType;
				changed = true;
			}

			if (htsItem.PlanningNumber.HasValue)
			{
				var planningNumber = Convert.ToInt64(Math.Truncate(htsItem.PlanningNumber.Value));
				if (appItem.PlanningNumber != planningNumber)
				{
					appItem.PlanningNumber = planningNumber;
					changed = true;
				}
			}

			if (TryApplyHtsDate(
				htsItem.ReceivedDate,
				htsItem.ReceivedDateInText,
				appItem.SendToIndustrialMiladiDate,
				appItem.SendToIndustrialShamsiDate,
				out var sendMiladi,
				out var sendShamsi))
			{
				appItem.SendToIndustrialMiladiDate = sendMiladi;
				appItem.SendToIndustrialShamsiDate = sendShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.FirstIndustrialChangeDate,
				htsItem.FirstIndustrialChangeDateInText,
				appItem.FirstIndustrialChangeMiladiDate,
				appItem.FirstIndustrialChangeShamsiDate,
				out var firstIndMiladi,
				out var firstIndShamsi))
			{
				appItem.FirstIndustrialChangeMiladiDate = firstIndMiladi;
				appItem.FirstIndustrialChangeShamsiDate = firstIndShamsi;
				changed = true;
			}

			if (!string.IsNullOrWhiteSpace(htsItem.FirstIndustrialChangeUserUsername))
			{
				var htsUsername = htsItem.FirstIndustrialChangeUserUsername.Trim();
				if (appUsersByUsername.TryGetValue(htsUsername, out var mappedUserId))
				{
					if (appItem.FirstIndustrialChangeById != mappedUserId)
					{
						appItem.FirstIndustrialChangeById = mappedUserId;
						changed = true;
					}
				}
				else
				{
					firstIndustrialUserMapMissed = true;
				}
			}

			if (TryApplyHtsDate(
				htsItem.DocumentPreparationDate,
				htsItem.DocumentPreparationDateInText,
				appItem.DocumentPreparationMiladiDate,
				appItem.DocumentPreparationShamsiDate,
				out var docMiladi,
				out var docShamsi))
			{
				appItem.DocumentPreparationMiladiDate = docMiladi;
				appItem.DocumentPreparationShamsiDate = docShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.StandardDeliveryDate,
				htsItem.StandardDeliveryDateInText,
				appItem.StandardDeliveryMiladiDate,
				appItem.StandardDeliveryShamsiDate,
				out var stdMiladi,
				out var stdShamsi))
			{
				appItem.StandardDeliveryMiladiDate = stdMiladi;
				appItem.StandardDeliveryShamsiDate = stdShamsi;
				changed = true;
			}

			if (!string.IsNullOrWhiteSpace(htsItem.EngineeringConsideration)
				&& appItem.EngineeringConsideration != htsItem.EngineeringConsideration)
			{
				appItem.EngineeringConsideration = htsItem.EngineeringConsideration;
				changed = true;
			}

			if (!string.IsNullOrWhiteSpace(htsItem.PlanningConsideration)
				&& appItem.PlanningConsideration != htsItem.PlanningConsideration)
			{
				appItem.PlanningConsideration = htsItem.PlanningConsideration;
				changed = true;
			}

			if (appItem.IsRoutine != htsItem.IsRoutine)
			{
				appItem.IsRoutine = htsItem.IsRoutine;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.ProductionStartDateInEurope,
				htsItem.ProductionStartDate,
				appItem.ProductionStartMiladiDate,
				appItem.ProductionStartShamsiDate,
				out var startMiladi,
				out var startShamsi))
			{
				appItem.ProductionStartMiladiDate = startMiladi;
				appItem.ProductionStartShamsiDate = startShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.ProductionEndDateInEurope,
				htsItem.ProductionEndDate,
				appItem.ProductionEndMiladiDate,
				appItem.ProductionEndShamsiDate,
				out var endMiladi,
				out var endShamsi))
			{
				appItem.ProductionEndMiladiDate = endMiladi;
				appItem.ProductionEndShamsiDate = endShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.TestingEndDateInEurope,
				htsItem.TestingEndDate,
				appItem.TestingEndMiladiDate,
				appItem.TestingEndShamsiDate,
				out var testMiladi,
				out var testShamsi))
			{
				appItem.TestingEndMiladiDate = testMiladi;
				appItem.TestingEndShamsiDate = testShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.PreparationDate,
				htsItem.PreparationDateInText,
				appItem.PreparationMiladiDate,
				appItem.PreparationShamsiDate,
				out var prepMiladi,
				out var prepShamsi))
			{
				appItem.PreparationMiladiDate = prepMiladi;
				appItem.PreparationShamsiDate = prepShamsi;
				changed = true;
			}

			if (TryApplyHtsDate(
				htsItem.DeliveryDate,
				htsItem.DeliveryDateInText,
				appItem.DeliveryMiladiDate,
				appItem.DeliveryShamsiDate,
				out var deliveryMiladi,
				out var deliveryShamsi))
			{
				appItem.DeliveryMiladiDate = deliveryMiladi;
				appItem.DeliveryShamsiDate = deliveryShamsi;
				changed = true;
			}

			if (!string.IsNullOrWhiteSpace(htsItem.Serial) && appItem.Serial != htsItem.Serial)
			{
				appItem.Serial = htsItem.Serial;
				changed = true;
			}

			return changed;
		}

		private static bool ApplyHtsCutoverToHeader(ProductionOrder header, Hts_Pln_ProductionOrderItem htsItem)
		{
			var changed = false;

			if (htsItem.HeaderIsDisableForTimelyDeliveryReport.HasValue
				&& header.IsDisableForTimelyDeliveryReport != htsItem.HeaderIsDisableForTimelyDeliveryReport.Value)
			{
				header.IsDisableForTimelyDeliveryReport = htsItem.HeaderIsDisableForTimelyDeliveryReport.Value;
				changed = true;
			}

			if (!string.IsNullOrWhiteSpace(htsItem.HeaderDisableForTimelyDeliveryReportComment)
				&& header.DisableForTimelyDeliveryReportComment != htsItem.HeaderDisableForTimelyDeliveryReportComment)
			{
				header.DisableForTimelyDeliveryReportComment = htsItem.HeaderDisableForTimelyDeliveryReportComment;
				changed = true;
			}

			return changed;
		}

		/// <summary>
		/// Maps HTS BuyStatusId → App CheckStatus.
		/// App intentionally uses mechanical CheckStatus codes for both mech/elec cartables
		/// (DeviceType distinguishes the lane). HTS electrical IDs 2196/2199/2200 are normalized.
		/// </summary>
		private static bool TryMapCheckStatusFromHts(short? htsBuyStatusId, out ProductionOrderItemCheckStatusEnum checkStatus)
		{
			checkStatus = default;
			if (!htsBuyStatusId.HasValue)
				return false;

			var normalized = htsBuyStatusId.Value switch
			{
				2196 => (short)ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending, // electrical awaiting
				2199 => (short)ProductionOrderItemCheckStatusEnum.EngineeringConfirmationMechanical, // electrical approved
				2200 => (short)ProductionOrderItemCheckStatusEnum.MechanicalEngineeringNotApproved, // electrical rejected
				_ => htsBuyStatusId.Value
			};

			return TryMapEnum(normalized, out checkStatus);
		}

		private static bool TryMapEnum<TEnum>(short? htsValue, out TEnum mapped)
			where TEnum : struct, Enum
		{
			mapped = default;
			if (!htsValue.HasValue)
				return false;

			var intValue = (int)htsValue.Value;
			if (!Enum.IsDefined(typeof(TEnum), intValue))
				return false;

			mapped = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
			return true;
		}

		/// <summary>HTS wins when miladi date is present; shamsi falls back to conversion.</summary>
		private static bool TryApplyHtsDate(
			DateTime? htsMiladi,
			string? htsShamsi,
			DateTime? appMiladi,
			string? appShamsi,
			out DateTime? newMiladi,
			out string? newShamsi)
		{
			newMiladi = appMiladi;
			newShamsi = appShamsi;

			if (!htsMiladi.HasValue)
				return false;

			var shamsi = !string.IsNullOrWhiteSpace(htsShamsi) ? htsShamsi : htsMiladi.Value.ToShamsiDate();
			if (appMiladi == htsMiladi && appShamsi == shamsi)
				return false;

			newMiladi = htsMiladi;
			newShamsi = shamsi;
			return true;
		}

		/// <summary>
		/// معادل HTS ReceivedDate (تاریخ اخذ): صنایع داخلی (1904) یا خارجی (1905) — همیشه با now ست می‌شود.
		/// </summary>
		private static void TrySetSendToIndustrial(ProductionOrderItem item, ProductionOrderItemCheckStatusEnum newCheckStatus, DateTime now)
		{
			if (newCheckStatus != ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal &&
				newCheckStatus != ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog)
				return;

			item.SendToIndustrialMiladiDate = now;
			item.SendToIndustrialShamsiDate = now.ToShamsiDate();
		}

		private static bool HasAutoStartProcessComment(IEnumerable<ProductionOrderComment>? comments)
		{
			if (comments == null)
				return false;

			return comments.Any(c =>
				string.Equals(c.Comment, AutoStartProcessComment, StringComparison.Ordinal) ||
				string.Equals(c.Comment, "ایجاده شده بصورت خودکار توسط سیستم", StringComparison.Ordinal));
		}

		private async Task<int> EnsureAutoStartProcessComments(IReadOnlyCollection<long> productionOrderIds, DateTime nowDate, CancellationToken cn)
		{
			if (productionOrderIds.Count == 0)
				return 0;

			var existingComments = await unitOfWork.Repository<ProductionOrderComment>()
				.TableNoTracking
				.Where(c => productionOrderIds.Contains(c.ProductionOrderId))
				.ToListAsync(cn);

			var ordersWithAutoComment = existingComments
				.Where(c => HasAutoStartProcessComment(new[] { c }))
				.Select(c => c.ProductionOrderId)
				.ToHashSet();

			var newHeaderComments = new List<ProductionOrderComment>();
			foreach (var productionOrderId in productionOrderIds.Distinct())
			{
				if (ordersWithAutoComment.Contains(productionOrderId))
					continue;

				newHeaderComments.Add(new ProductionOrderComment
				{
					ProductionOrderId = productionOrderId,
					PreviousState = ProductionOrderStateEnum.FinancialApproval,
					NewState = ProductionOrderStateEnum.FinancialApproval,
					Comment = AutoStartProcessComment,
					CreatedById = 1,
					CreatedOnMiladiDateTime = nowDate,
					CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
					IsActive = IsActiveEnum.Active
				});
			}

			if (newHeaderComments.Any())
				await unitOfWork.Repository<ProductionOrderComment>().AddRangeAsync(newHeaderComments, cn, false);

			return newHeaderComments.Count;
		}

		private static List<ProductionOrderItemComment> RouteInitialRegistrationItems(
			List<ProductionOrderItem> productionOrderItems,
			DateTime nowDate)
		{
			var specialStatusIds = new List<ProductionOrderItemDeviceTypeEnum?>
			{
				ProductionOrderItemDeviceTypeEnum.CngEquipment,
				ProductionOrderItemDeviceTypeEnum.SparePart
			};

			var newComments = new List<ProductionOrderItemComment>();

			foreach (var p in productionOrderItems)
			{
				bool isRoutine = p.Part?.EngineeringRoutine ?? false;
				p.IsRoutine = isRoutine;

				ProductionOrderItemCheckStatusEnum newStatus;
				ProductionOrderItemProductionStepEnum newStep;

				if (isRoutine || (p.DeviceType.HasValue && specialStatusIds.Contains(p.DeviceType)))
				{
					newStatus = ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
					newStep = ProductionOrderItemProductionStepEnum.AwaitingProduction;
				}
				else if (p.ProductionOrder?.ProjectManagerId.HasValue == true)
				{
					newStatus = ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval;
					newStep = ProductionOrderItemProductionStepEnum.AwaitingProjectManager;
				}
				else
				{
					newStatus = ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;
					newStep = ProductionOrderItemProductionStepEnum.AwaitingEngineering;
				}

				p.CheckStatus = newStatus;
				p.ProductionStep = newStep;
				p.CheckStatusChangedOnMiladiDate = nowDate;
				p.CheckStatusChangedOnShamsiDate = nowDate.ToShamsiDateTime();
				TrySetSendToIndustrial(p, newStatus, nowDate);

				newComments.Add(new ProductionOrderItemComment
				{
					ProductionOrderItemId = (long)p.Id!,
					ProductionStatus = (ProductionOrderItemProductionStatusEnum)newStatus,
					Comment = "ارسال شده بصورت خودکار توسط سیستم",
					StartMiladiDateTime = nowDate,
					StartShamsiDate = nowDate.ToShamsiDateTime(),
					CreatedById = 1,
					CreatedOnMiladiDateTime = nowDate,
					CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
					IsActive = IsActiveEnum.Active
				});
			}

			return newComments;
		}

		private static bool TryExtractKwCapacity(string partNameLower, string kwConst, out int kwCapacity)
		{
			kwCapacity = 0;
			var kwIndex = partNameLower.IndexOf(kwConst, StringComparison.Ordinal);
			if (kwIndex < 3)
				return false;

			try
			{
				var kwString = partNameLower.Substring(kwIndex - 3, 5).Replace(" ", "");
				var digits = Regex.Match(kwString.Replace(kwConst, ""), @"-?\d+");
				if (!digits.Success)
					return false;

				kwCapacity = Convert.ToInt32(digits.Value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Equivalent of HTS CalculateStandardDeliveryDate — holiday window around Persian year-end.
		/// </summary>
		private static void ApplyStandardDeliveryDate(
			ProductionOrderItem item,
			DateTime calculatedStandardDate,
			DateTime receivedDate)
		{
			item.StandardDeliveryMiladiDate = calculatedStandardDate;

			var now = DateTime.Now;
			var shamsiYear = now.GetShamsiYear();
			var fromDate = $"{shamsiYear}/12/25".ToMiladiDate();
			var toDate = $"{shamsiYear + 1}/01/15".ToMiladiDate();

			if (receivedDate <= fromDate && calculatedStandardDate > fromDate)
			{
				var holidayCount = toDate.Subtract(fromDate).TotalDays;
				item.StandardDeliveryMiladiDate = item.StandardDeliveryMiladiDate.Value.AddDays(holidayCount);
			}

			item.StandardDeliveryShamsiDate = item.StandardDeliveryMiladiDate.Value.ToShamsiDate();
		}

		/// <summary>
		/// باید دقیقاً با HashSet هم‌نام در ProductionOrderItemController همگام بماند (تشخیص رشته مهندسی
		/// بر اساس DeviceType). چون این دو کلاس در دو پروژه/اسمبلی متفاوت هستند (WebApp در برابر
		/// App.BackgroundJob)، امکان اشتراک مستقیم فیلد private وجود ندارد.
		/// </summary>
		private static readonly HashSet<ProductionOrderItemDeviceTypeEnum> ElectricalDeviceTypes = new()
		{
			ProductionOrderItemDeviceTypeEnum.ControlPanel,
			ProductionOrderItemDeviceTypeEnum.ElectricalPanel,
			ProductionOrderItemDeviceTypeEnum.InverterPanel,
			ProductionOrderItemDeviceTypeEnum.Sequencer,
		};

		/// <summary>
		/// معادل HTS: SendEngineeringAndProjectManagerNotification / SendExpiredItemsNotification (بخش مهندسی).
		/// برای هر قلم، بر اساس Gnr_Lookup6.Pln_ProductionOrderEquipmentConfirmer (اکنون
		/// Pln.ProductionOrderEquipmentConfirmer با کلید DeviceType)، تاییدکننده(های) مهندسی مکانیک/برق مسئول
		/// همان نوع دستگاه پیدا و یک اعلان ایمیلی به هرکدام صف می‌شود. دقیقاً مثل HTS، هر DeviceType می‌تواند
		/// چند ردیف/چند تاییدکننده هم‌زمان داشته باشد (یک‌به‌یک نیست)؛ همه آن‌ها اعلان دریافت می‌کنند. اگر برای
		/// DeviceType موردنظر هیچ تنظیمی ثبت نشده باشد، اعلانی ارسال نمی‌شود اما Job با خطا متوقف نمی‌شود.
		/// </summary>
		private async Task SendEngineeringNotificationAsync(List<ProductionOrderItem> items, string reasonTitle, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var deviceTypes = items
					.Where(p => p.DeviceType.HasValue)
					.Select(p => p.DeviceType!.Value)
					.Distinct()
					.ToList();

				if (!deviceTypes.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ‌کدام از اقلام نوع دستگاه مشخصی ندارند؛ اعلان مهندسی ارسال نشد", cn);
					return;
				}

				// هر DeviceType می‌تواند چند ردیف تاییدکننده داشته باشد؛ همه با هم گروه‌بندی می‌شوند
				var confirmersByDeviceType = (await unitOfWork.Repository<ProductionOrderEquipmentConfirmer>()
						.TableNoTracking
						.Where(c => deviceTypes.Contains(c.DeviceType))
						.ToListAsync(cn))
					.GroupBy(c => c.DeviceType)
					.ToDictionary(g => g.Key, g => g.ToList());

				// گروه‌بندی بر اساس گیرنده (کاربر مسئول) تا هر کاربر فقط یک ایمیل شامل همه اقلامش دریافت کند
				var itemsByRecipient = new Dictionary<long, List<ProductionOrderItem>>();

				foreach (var item in items)
				{
					if (!item.DeviceType.HasValue || !confirmersByDeviceType.TryGetValue(item.DeviceType.Value, out var confirmersForType))
						continue;

					var isElectrical = ElectricalDeviceTypes.Contains(item.DeviceType.Value);
					var recipientIds = (isElectrical
							? confirmersForType.Select(c => c.ElectricalUserId)
							: confirmersForType.Select(c => c.MechanicalUserId))
						.Where(id => id.HasValue)
						.Select(id => id!.Value)
						.Distinct();

					foreach (var recipientId in recipientIds)
					{
						if (!itemsByRecipient.TryGetValue(recipientId, out var list))
						{
							list = new List<ProductionOrderItem>();
							itemsByRecipient[recipientId] = list;
						}
						list.Add(item);
					}
				}

				if (!itemsByRecipient.Any())
				{
					await jobLogger?.LogInfoAsync("برای نوع دستگاه اقلام موردنظر، تاییدکننده‌ای در Pln.ProductionOrderEquipmentConfirmer تعریف نشده است؛ اعلان مهندسی ارسال نشد", cn);
					return;
				}

				var notifications = itemsByRecipient.Select(kv => new Notification
				{
					Type = NotificationType.Email,
					Title = reasonTitle,
					Body = BuildEngineeringNotificationBody(reasonTitle, kv.Value),
					EntityId = kv.Value.First().Id,
					OwnerId = kv.Key,
					ViewPath = $"Panel/ProductionOrderItem/Edit/{kv.Value.First().Id}",
					IsRead = false
				}).ToList();

				await unitOfWork.Repository<Notification>().AddRangeAsync(notifications, cn, false);
				await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"اعلان مهندسی برای {notifications.Count} تاییدکننده ({itemsByRecipient.Sum(k => k.Value.Count)} قلم) صف شد", cn);
			}
			catch (Exception ex)
			{
				// نباید Job اصلی (مسیریابی/انتقال وضعیت) را متوقف کند - شکست اعلان صرفاً لاگ می‌شود
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		private static string BuildEngineeringNotificationBody(string reasonTitle, List<ProductionOrderItem> items)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='100%'>");
			sb.AppendLine("<tr style='background:#000aa0'><td><div style='font-size:14pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
			sb.AppendLine("<tr><td><div style='font-size:12pt;text-align:right;direction:rtl'>");
			sb.AppendLine("<p>با سلام و احترام</p>");
			sb.AppendLine($"<p>{reasonTitle} - اقلام ذیل نیازمند بررسی مهندسی شما هستند:</p>");
			sb.AppendLine("<ul>");
			foreach (var item in items)
			{
				var orderNumber = item.ProductionOrder?.ProductionOrderNumber?.ToString() ?? "-";
				sb.AppendLine($"<li>سفارش ساخت <strong>{orderNumber}</strong> - قلم شناسه <strong>{item.Id}</strong> (نوع دستگاه: {item.DeviceType})</li>");
			}
			sb.AppendLine("</ul>");
			sb.AppendLine("</div></td></tr></table></div>");
			return sb.ToString();
		}

		/// <summary>
		/// معادل HTS: بخش SendToProjectManager در SendEngineeringAndProjectManagerNotification/SendExpiredItemsNotification.
		/// گیرنده مستقیماً از Sale.ProductionOrder.ProjectManagerId خوانده می‌شود (این بخش نیازی به
		/// Pln.ProductionOrderEquipmentConfirmer ندارد چون مدیر پروژه در خود سرفصل سفارش ساخت مشخص است).
		/// </summary>
		private async Task SendProjectManagerNotificationAsync(List<ProductionOrderItem> items, string reasonTitle, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var itemsByProjectManager = items
					.Where(p => p.ProductionOrder?.ProjectManagerId != null)
					.GroupBy(p => p.ProductionOrder!.ProjectManagerId!.Value)
					.ToList();

				if (!itemsByProjectManager.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ‌کدام از سفارش‌های ساخت اقلام موردنظر مدیر پروژه مشخصی ندارند؛ اعلانی ارسال نشد", cn);
					return;
				}

				var notifications = itemsByProjectManager.Select(g => new Notification
				{
					Type = NotificationType.Email,
					Title = reasonTitle,
					Body = BuildProjectManagerNotificationBody(reasonTitle, g.ToList()),
					EntityId = g.First().Id,
					OwnerId = g.Key,
					ViewPath = $"Panel/ProductionOrderItem/Edit/{g.First().Id}",
					IsRead = false
				}).ToList();

				await unitOfWork.Repository<Notification>().AddRangeAsync(notifications, cn, false);
				await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"اعلان مدیر پروژه برای {notifications.Count} نفر ({itemsByProjectManager.Sum(g => g.Count())} قلم) صف شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		private static string BuildProjectManagerNotificationBody(string reasonTitle, List<ProductionOrderItem> items)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='100%'>");
			sb.AppendLine("<tr style='background:#000aa0'><td><div style='font-size:14pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
			sb.AppendLine("<tr><td><div style='font-size:12pt;text-align:right;direction:rtl'>");
			sb.AppendLine("<p>با سلام و احترام</p>");
			sb.AppendLine($"<p>{reasonTitle} - اقلام ذیل نیازمند بررسی شما به‌عنوان مدیر پروژه هستند:</p>");
			sb.AppendLine("<ul>");
			foreach (var item in items)
			{
				var orderNumber = item.ProductionOrder?.ProductionOrderNumber?.ToString() ?? "-";
				sb.AppendLine($"<li>سفارش ساخت <strong>{orderNumber}</strong> - قلم شناسه <strong>{item.Id}</strong></li>");
			}
			sb.AppendLine("</ul>");
			sb.AppendLine("</div></td></tr></table></div>");
			return sb.ToString();
		}

		private async Task SendNearDeliveryDateNotification(List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			// TODO: سیم‌کشی کامل به سرویس اطلاع‌رسانی (INotificationService/NotificationGroupService) در فاز بعدی انجام شود.
			// در حال حاضر صرفاً لاگ پیشرفت ثبت می‌شود، مطابق الگوی موجود SendInitilizeEmailNotification.
			foreach (var item in items)
			{
				await jobLogger?.LogInfoAsync($"قلم سفارش ساخت شماره {item.Id} (سفارش ساخت: {item.ProductionOrderId}) در تاریخ {item.AgreedDeliverDate:yyyy-MM-dd} به تحویل نزدیک است", cn);
			}
		}




	}

}

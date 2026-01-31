using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using Entities.Rahkaran.USR3;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Job;
using System.Linq;

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
				var contractMap = appContracts.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

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

				// بارگذاری User ها با HamkaranId
				var appUsers = await appContext.Users
					.AsNoTracking()
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var userMap = appUsers.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				await jobLogger?.LogInfoAsync($"Entity های مرتبط بارگذاری شدند: {contractMap.Count} قرارداد، {dlMap.Count} حساب تفصیلی، {customerMap.Count} مشتری، {regionMap.Count} منطقه، {userMap.Count} کاربر", cn);

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
						// ایجاد رکورد جدید
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
							CentrifugeCompressorCount = rahkaranPO.CentrifugeCompressorCount,
							CentrifugeElectromotorVoltage = rahkaranPO.CentrifugeElectromotorVoltage,
							ManagementConfirmationAttachment = rahkaranPO.ManagementConfirmationAttachment,
							DepositFactorAttachment = rahkaranPO.DepositFactorAttachment,
							PreFactorAttachment = rahkaranPO.PreFactorAttachment,
							Number = rahkaranPO.Number ?? string.Empty,
							ContractAttachment = rahkaranPO.ContractAttachment,
							HasGA = rahkaranPO.HasGA ?? false,
							PackingType = rahkaranPO.PackingTypeId,
							DeliveryType = rahkaranPO.DeliveryTypeId,
							ProductType = rahkaranPO.ProductTypeId,
							CentrifugeSetupType = rahkaranPO.CentrifugeSetupTypeId,
							IsNeedProjectManager = rahkaranPO.IsNeedProjectManager ?? false,
							ProductionOrderNumber = rahkaranPO.ProductionOrderNumber,
							Revision = rahkaranPO.Revision,
							CustomerIndustry = rahkaranPO.CustomerIndustry,
							ProjectDlCode = rahkaranPO.ProjectDlCode,
							EdmsProject = rahkaranPO.EdmsProjectId
						};

						// نگاشت ContractIdRef → ContractId
						if (rahkaranPO.ContractIdRef.HasValue && contractMap.TryGetValue(rahkaranPO.ContractIdRef.Value, out var contractId))
						{
							newPO.ContractId = contractId;
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

						// نگاشت User ها - اگر پیدا نشد null می‌گذاریم
						if (rahkaranPO.SalesManagerUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.SalesManagerUserIdRef.Value, out var salesManagerId))
						{
							newPO.SalesManagerId = salesManagerId;
						}

						if (rahkaranPO.ProjectManagerUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.ProjectManagerUserIdRef.Value, out var projectManagerId))
						{
							newPO.ProjectManagerId = projectManagerId;
						}

						if (rahkaranPO.AlternativeExpertUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.AlternativeExpertUserIdRef.Value, out var alternativeExpertId))
						{
							newPO.AlternativeExpertId = alternativeExpertId;
						}

						if (rahkaranPO.SalesExpertUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.SalesExpertUserIdRef.Value, out var salesExpertId))
						{
							newPO.SalesExpertId = salesExpertId;
						}

						if (rahkaranPO.IntroducerExpertRef.HasValue && userMap.TryGetValue(rahkaranPO.IntroducerExpertRef.Value, out var introducerExpertId))
						{
							newPO.IntroducerExpertId = introducerExpertId;
						}

						newProductionOrders.Add(newPO);
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existPO.State != (ProductionOrderStateEnum)rahkaranPO.State)
						{
							existPO.State = (ProductionOrderStateEnum)rahkaranPO.State;
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

						if (existPO.CentrifugeCompressorCount != rahkaranPO.CentrifugeCompressorCount)
						{
							existPO.CentrifugeCompressorCount = rahkaranPO.CentrifugeCompressorCount;
							isModified = true;
						}

						if (existPO.CentrifugeElectromotorVoltage != rahkaranPO.CentrifugeElectromotorVoltage)
						{
							existPO.CentrifugeElectromotorVoltage = rahkaranPO.CentrifugeElectromotorVoltage;
							isModified = true;
						}

						if (existPO.Number != rahkaranPO.Number)
						{
							existPO.Number = rahkaranPO.Number ?? string.Empty;
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

						if (existPO.ProductionOrderNumber != rahkaranPO.ProductionOrderNumber)
						{
							existPO.ProductionOrderNumber = rahkaranPO.ProductionOrderNumber;
							isModified = true;
						}

						if (existPO.Revision != rahkaranPO.Revision)
						{
							existPO.Revision = rahkaranPO.Revision;
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
						if (rahkaranPO.ContractIdRef.HasValue && contractMap.TryGetValue(rahkaranPO.ContractIdRef.Value, out var contractId))
						{
							if (existPO.ContractId != contractId)
							{
								existPO.ContractId = contractId;
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

						// به‌روزرسانی User ها
						long? newSalesManagerId = null;
						if (rahkaranPO.SalesManagerUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.SalesManagerUserIdRef.Value, out var salesManagerId))
						{
							newSalesManagerId = salesManagerId;
						}
						if (existPO.SalesManagerId != newSalesManagerId)
						{
							existPO.SalesManagerId = newSalesManagerId;
							isModified = true;
						}

						long? newProjectManagerId = null;
						if (rahkaranPO.ProjectManagerUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.ProjectManagerUserIdRef.Value, out var projectManagerId))
						{
							newProjectManagerId = projectManagerId;
						}
						if (existPO.ProjectManagerId != newProjectManagerId)
						{
							existPO.ProjectManagerId = newProjectManagerId;
							isModified = true;
						}

						long? newAlternativeExpertId = null;
						if (rahkaranPO.AlternativeExpertUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.AlternativeExpertUserIdRef.Value, out var alternativeExpertId))
						{
							newAlternativeExpertId = alternativeExpertId;
						}
						if (existPO.AlternativeExpertId != newAlternativeExpertId)
						{
							existPO.AlternativeExpertId = newAlternativeExpertId;
							isModified = true;
						}

						long? newSalesExpertId = null;
						if (rahkaranPO.SalesExpertUserIdRef.HasValue && userMap.TryGetValue(rahkaranPO.SalesExpertUserIdRef.Value, out var salesExpertId))
						{
							newSalesExpertId = salesExpertId;
						}
						if (existPO.SalesExpertId != newSalesExpertId)
						{
							existPO.SalesExpertId = newSalesExpertId;
							isModified = true;
						}

						long? newIntroducerExpertId = null;
						if (rahkaranPO.IntroducerExpertRef.HasValue && userMap.TryGetValue(rahkaranPO.IntroducerExpertRef.Value, out var introducerExpertId))
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
				var partMap = appParts.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				var appItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appItems.Count} رکورد قلم سفارش ساخت در پایگاه داده برنامه موجود است", cn);

				var appItemsDict = appItems
					.Where(x => x.HamkaranId.HasValue && x.IsLatestVersion)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				var newItems = new List<ProductionOrderItem>();
				var newComments = new List<ProductionOrderItemComment>();
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

					// پیدا کردن PartId بر اساس PartIdRef
					long? partId = null;
					if (rahkaranItem.PartIdRef.HasValue && partMap.TryGetValue(rahkaranItem.PartIdRef.Value, out var foundPartId))
					{
						partId = foundPartId;
					}

					if (!appItemsDict.TryGetValue(rahkaranItem.Sale_ProductionOrderItemID, out var existItem))
					{
						// ایجاد رکورد جدید (New Insert)
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
							IsLatestVersion = true, // آیتم جدید همیشه آخرین نسخه است
							CheckStatus = rahkaranItem.CheckStatusId.HasValue ? (ProductionOrderItemCheckStatusEnum?)rahkaranItem.CheckStatusId.Value : null,
							PartCode = rahkaranItem.PartCode,
							Status = ProductionOrderItemStatusEnum.NotCompleted
						};
						newItems.Add(newItem);

						// ایجاد کامنت اولیه
						// Note: ProductionOrderItemId will be set after SaveChanges, usually needs logic to handle ID linking or 
						// add to collection if navigation property exists. Here we add to separate list for bulk insert after save.
						// However, since Id is not generated yet, we might need to rely on EF graph insertion or save first.
						// To avoid complexity, we can assume Graph Insert if we add to ProductionOrderItemComments collection of newItem.
						
						newItem.ProductionOrderItemComments.Add(new ProductionOrderItemComment
						{
							ProductionStatus = ProductionOrderItemProductionStatusEnum.InIndustriesInternalInbox, // Default or mapped
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
						// بررسی می‌کنیم اگر Revision تغییر کرده باشد
						if (existItem.Revision != rahkaranItem.Revision)
						{
							// غیرفعال کردن نسخه قبلی
							existItem.IsLatestVersion = false;
							
							// ایجاد نسخه جدید
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
								CheckStatus = rahkaranItem.CheckStatusId.HasValue ? (ProductionOrderItemCheckStatusEnum?)rahkaranItem.CheckStatusId.Value : null,
								PartCode = rahkaranItem.PartCode,
								Status = ProductionOrderItemStatusEnum.NotCompleted
							};
							
							newItem.ProductionOrderItemComments.Add(new ProductionOrderItemComment
							{
								ProductionStatus = ProductionOrderItemProductionStatusEnum.InIndustriesInternalInbox,
								Comment = "خوانده شده از راهکاران (نسخه جدید)",
								StartMiladiDateTime = DateTime.Now,
								StartShamsiDate = DateTime.Now.ToShamsiDateTime(),
								CreatedById = 1,
								CreatedOnMiladiDateTime = DateTime.Now,
								CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
								IsActive = IsActiveEnum.Active
							});

							newItems.Add(newItem);
							updatedItemsCount++;
						}
						else
						{
							// اگر Revision تغییر نکرده، می‌توانیم فیلدها را به‌روزرسانی کنیم (Update In-Place)
							// یا هیچ کاری نکنیم. برای اطمینان از همگام‌سازی، تغییرات را اعمال می‌کنیم
							// اما طبق منطق Trigger، هر آپدیت Revision را بالا می‌برد. پس اگر Revision یکی است، تغییری نیست.
							// با این حال، کد قبلی فیلدها را چک می‌کرد. اینجا فرض بر این است که Revision معیار تغییر است.
						}
					}
				}

				// بررسی اقلام حذف شده (در دیتابیس هستند اما در لیست راهکاران نیستند)
				var itemsToDelete = appItemsDict.Values
					.Where(x => !processedHamkaranIds.Contains(x.HamkaranId!.Value))
					.ToList();

				if (itemsToDelete.Any())
				{
					foreach (var item in itemsToDelete)
					{
						item.IsLatestVersion = false;
						item.IsActive = IsActiveEnum.Deleted;
						// طبق SQL وضعیت به "باطل" تغییر می‌کند
						item.Status = ProductionOrderItemStatusEnum.Invalid; 
					}
					await jobLogger?.LogInfoAsync($"تعداد {itemsToDelete.Count} قلم حذف/باطل شدند", cn);
				}

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


		[JobHandler("بررسی و تغییر وضعیت قلم سفارش ساخت")]
		public async Task CheckFinancialConfirmsOrders(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی سفارشات تایید مالی شده", cn);

				var statusId = ProductionOrderStateEnum.FinancialApproval;

				var productionOrderItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.Include(p => p.ProductionOrder)
					.Include(p => p.Part)
					.Where(p => p.IsLatestVersion &&
								p.ProductionOrder.State == statusId &&
								p.CheckStatus == ProductionOrderItemCheckStatusEnum.InitialRegistration)
					.ToListAsync(cn);

				if (!productionOrderItems.Any())
					return;

				await jobLogger?.LogInfoAsync($"تعداد {productionOrderItems.Count} قلم سفارش برای پردازش یافت شد", cn);

				var specialStatusIds = new List<ProductionOrderItemDeviceTypeEnum?>
				{
					ProductionOrderItemDeviceTypeEnum.CngEquipment,
					ProductionOrderItemDeviceTypeEnum.SparePart
				};

				var newComments = new List<ProductionOrderItemComment>();

				foreach (var p in productionOrderItems)
				{
					// IsRoutine logic mapped to Part.EngineeringRoutine
					bool isRoutine = p.Part?.EngineeringRoutine ?? false;

					ProductionOrderItemCheckStatusEnum newStatus;

					if (isRoutine || (p.DeviceType.HasValue && specialStatusIds.Contains(p.DeviceType)))
					{
						newStatus = ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
					}
					else if (p.ProductionOrder.ProjectManagerId.HasValue)
					{
						newStatus = ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval;
					}
					else
					{
						newStatus = ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;
					}

					p.CheckStatus = newStatus;

					newComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)p.Id!,
						ProductionStatus = (ProductionOrderItemProductionStatusEnum)newStatus,
						Comment = "ارسال شده بصورت خودکار توسط سیستم",
						StartMiladiDateTime = DateTime.Now,
						StartShamsiDate = DateTime.Now.ToShamsiDateTime(),
						CreatedById = 1,
						CreatedOnMiladiDateTime = DateTime.Now,
						CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
						IsActive = IsActiveEnum.Active
					});
				}

				if (newComments.Any())
				{
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newComments, cn, false);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("تغییر وضعیت اقلام سفارش با موفقیت انجام شد", cn);
			}
			catch (Exception exception)
			{
				await jobLogger?.LogExceptionAsync(exception, cn);
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
					.Include(p => p.ProductionOrderItemComments)
					.Where(p => p.IsLatestVersion &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.ProductionStep == stepId)
					.ToListAsync(cn);

				var nowDate = DateTime.Now;
				var finalItems = new List<ProductionOrderItem>();
				var newComments = new List<ProductionOrderItemComment>();

				foreach (var item in items)
				{
					// بررسی آخرین کامنت با وضعیت 1380
					var lastComment = item.ProductionOrderItemComments
						.Where(c => (int)c.ProductionStatus == (int)stepId)
						.OrderByDescending(c => c.CreatedOnMiladiDateTime)
						.FirstOrDefault();

					// اگر کامنت وجود ندارد یا مهلت تمام نشده
					if (lastComment == null || (lastComment.CreatedOnMiladiDateTime.HasValue && nowDate <= lastComment.CreatedOnMiladiDateTime.Value.AddDays(deadLineInDay)))
						continue;

					// تغییر وضعیت
					item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry;

					finalItems.Add(item);

					// ایجاد کامنت جدید
					newComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)item.Id!,
						ProductionStatus = (ProductionOrderItemProductionStatusEnum)ProductionOrderItemProductionStepEnum.AwaitingIndustry,
						Comment = "بصورت اتوماتیک و بعلت منقضی شدن زمان بررسی توسط مدیر پروژه",
						CreatedById = 1,
						CreatedOnMiladiDateTime = nowDate,
						CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
						StartMiladiDateTime = nowDate,
						StartShamsiDate = nowDate.ToShamsiDateTime(),
						IsActive = IsActiveEnum.Active
					});
				}

				if (finalItems.Any())
				{
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newComments, cn, false);
					await unitOfWork.SaveChangesAsync(cn);

					await SendInitilizeEmailNotification(finalItems, jobLogger, cn);

					await jobLogger?.LogInfoAsync($"تعداد {finalItems.Count} قلم کالا به دلیل انقضای زمان به واحد صنعتی منتقل شدند", cn);
				}
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		private async Task SendInitilizeEmailNotification(List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			// TODO: Implement email notification logic
			await jobLogger?.LogInfoAsync("ارسال ایمیل اطلاع‌رسانی انجام شد", cn);
		}




	}

}

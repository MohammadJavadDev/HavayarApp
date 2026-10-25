using System.Text;
using System.Text.RegularExpressions;
using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm;
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
using Entities.Rahkaran.USR3;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using App.BackgroundJob.Jobs;
using ReportBuilder.Services;
using Services.FileServices;
using Services.Job;
using Services.ProductionOrderServices;


namespace App.BackgroundJob.Jobs.Sale
{
	public class ProductionOrderJob(
		RahkaranDbContext Rdb,
		HtsDbContext Hdb,
		IUnitOfWork unitOfWork,
		ApplicationDbContext appContext,
		IFileService fileService,
		IProductionOrderFormPdfExporter productionOrderFormPdf,
		IProductionOrderItemBomService bomService)
	{
		private const string ReadFromRahkaranComment = "خوانده شده از راهکاران";
		private const string ObsoleteFromRahkaranComment = "منسوخ شده در راهکاران";
		/// <summary>معادل پیش‌فرض SP: IIF(HtsPerson.Personel_ID IS NULL, 253, HtsPerson.OrgUnit_FK)</summary>
		private const long DefaultCreatedOrganizationUnitId = 253;

		[JobHandler("افزودن سفارش ساخت از راهکاران")]
		public async Task AddProductionOrderFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			const int maxQueuePasses = 20;
			for (var pass = 1; pass <= maxQueuePasses; pass++)
			{
				var runAgain = await SyncProductionOrdersFromRahkaranPass(jobLogger, cn);
				if (!runAgain)
					return;

				if (pass < maxQueuePasses)
				{
					await jobLogger?.LogInfoAsync(
						$"در صف همگام سفارش ساخت شناسه جدید مانده است؛ دور {pass + 1} از {maxQueuePasses}",
						cn);
				}
			}

			await jobLogger?.LogWarningAsync(
				"صف همگام سفارش ساخت بعد از ۲۰ دور هنوز خالی نشد؛ دور بعدی را زمان‌بندی جاب برمی‌دارد",
				0,
				cn);
		}

		/// <summary>
		/// یک دور همگام. شناسه‌های صف را قبل از خواندن راهکاران برمی‌دارد و فقط بعد از موفقیت همان‌ها را پاک می‌کند.
		/// اگر وسط کار شناسه جدید در dbo.Sale_ProductionOrderSyncQueue آمده باشد true برمی‌گرداند.
		/// </summary>
		private async Task<bool> SyncProductionOrdersFromRahkaranPass(IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var capturedQueueIds = await LoadSyncQueueIdsAsync(cn);
				if (capturedQueueIds.Count > 0)
				{
					await jobLogger?.LogInfoAsync(
						$"صف همگام سفارش ساخت: {capturedQueueIds.Count} شناسه قبل از این دور",
						cn);
				}

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
				var contractMap = await JobLookup.ToUniqueValueMapAsync(
					appContracts,
					x => x.HamkaranId!.Value,
					x => (ContractId: x.Id, BranchId: x.BranchId),
					jobLogger,
					"Sls.Contract.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appDLs = await unitOfWork.Repository<DL>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var dlMap = await JobLookup.ToUniqueValueMapAsync(
					appDLs,
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Fin.DL.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appCustomers = await unitOfWork.Repository<Customer>()
					.TableNoTracking
					.ToListAsync(cn);
				var customerMap = await JobLookup.ToUniqueValueMapAsync(
					appCustomers,
					x => x.HamkaranId,
					x => x.Id,
					jobLogger,
					"Sls.Customer.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appRegions = await unitOfWork.Repository<Region>()
					.TableNoTracking
					.Where(x => x.RahkaranId.HasValue)
					.ToListAsync(cn);
				var regionMap = await JobLookup.ToUniqueValueMapAsync(
					appRegions,
					x => x.RahkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Gnr.Region.RahkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				// بارگذاری Personel ها برای نگاشت Employee → User و OrgUnit ایجادکننده
				var appPersonels = await unitOfWork.Repository<Personel>()
					.TableNoTracking
					.Where(x => x.PartyId.HasValue)
					.ToListAsync(cn);

				// بارگذاری User ها با PartyId و Id
				var appUsers = await appContext.Users
					.AsNoTracking()
					.Where(x => x.Id.HasValue)
					.ToListAsync(cn);

				// ایجاد نگاشت از PartyId به UserId (کارشناسان فروش / مدیران)
				var partyToUserMap = await JobLookup.ToUniqueValueMapAsync(
					appUsers.Where(x => x.PartyId.HasValue).ToList(),
					x => x.PartyId!.Value,
					x => x.Id!.Value,
					jobLogger,
					"system.User.PartyId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				// ایجاد نگاشت نهایی از HamkaranId (EmployeeId) به UserId
				var employeeToUserMap = new Dictionary<long, long>();
				foreach (var personel in appPersonels.Where(x => x.HamkaranId.HasValue))
				{
					if (personel.HamkaranId.HasValue && personel.PartyId.HasValue &&
						partyToUserMap.TryGetValue(personel.PartyId.Value, out var userId))
					{
						employeeToUserMap[personel.HamkaranId.Value] = userId;
					}
				}

				// Creator راهکاران (SYS3.User.UserID) → User.HamkaranId — مثل ContractJob
				var creatorUserMap = await JobLookup.ToUniqueValueMapAsync(
					appUsers.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					x => x.Id!.Value,
					jobLogger,
					"system.User.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				// نام ایجادکننده مثل ContractJob: NameFa و در نبود آن Name. کلید تکراری همان انتخاب creatorUserMap است.
				var creatorNameMap = await JobLookup.ToUniqueValueMapAsync(
					appUsers.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					x =>
					{
						var name = !string.IsNullOrWhiteSpace(x.NameFa) ? x.NameFa!.Trim() : x.Name?.Trim();
						if (string.IsNullOrWhiteSpace(name))
							return string.Empty;
						return name.Length <= 150 ? name : name[..150];
					},
					null,
					"system.User.HamkaranId (name)",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var creatorPartyByHamkaranId = appUsers
					.Where(x => x.HamkaranId.HasValue && x.PartyId.HasValue)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First().PartyId!.Value);

				var personelOrgUnitByPartyId = appPersonels
					.Where(x => x.PartyId.HasValue && x.OrgUnitId.HasValue)
					.GroupBy(x => x.PartyId!.Value)
					.ToDictionary(g => g.Key, g => g.First().OrgUnitId!.Value);

				// معادل پیش‌فرض SP = 253؛ فقط اگر ردیف Hrm.OrgUnit واقعاً وجود داشته باشد ست می‌شود
				var defaultCreatedOrgUnitExists = await unitOfWork.Repository<OrgUnit>()
					.TableNoTracking
					.AnyAsync(x => x.Id == DefaultCreatedOrganizationUnitId, cn);

				// فیلتر درج مثل SP: INNER JOIN Sale_RequirmentAdvertise ON Sale_RequirmentAdvertiseID = RequirementAdvertiseIdRef
				var requirementAdvertiseIds = (await Rdb.Sale_RequirmentAdvertise
					.AsNoTracking()
					.Select(x => x.Sale_RequirmentAdvertiseID)
					.ToListAsync(cn))
					.ToHashSet();

				await jobLogger?.LogInfoAsync(
					$"Entity های مرتبط بارگذاری شدند: {contractMap.Count} قرارداد، {dlMap.Count} حساب تفصیلی، {customerMap.Count} مشتری، {regionMap.Count} منطقه، {employeeToUserMap.Count} نگاشت کارمند به کاربر، {creatorUserMap.Count} کاربر با HamkaranId، {requirementAdvertiseIds.Count} اعلام نیازمندی",
					cn);

				// اعلام نیازمندی راهکاران (HamkaranId) → شناسه اپ؛ برای ستون ProductionOrder.RequirmentAdvertiseId (شماره نماینده در پیامک‌ها)
				var requirmentAdvertiseMap = (await unitOfWork.Repository<RequirmentAdvertise>()
					.TableNoTracking
					.Where(x => x.HamkaranId != null && x.Id != null)
					.Select(x => new { HamkaranId = x.HamkaranId!.Value, Id = x.Id!.Value })
					.ToListAsync(cn))
					.GroupBy(x => x.HamkaranId)
					.ToDictionary(g => g.Key, g => g.First().Id);

				// بارگذاری ProductionOrder های موجود
				var appProductionOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appProductionOrders.Count} رکورد سفارش ساخت در پایگاه داده برنامه موجود است", cn);

				var appProductionOrdersDict = await JobLookup.ToUniqueMapAsync(
					appProductionOrders.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					jobLogger,
					"Sale.ProductionOrder.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var newProductionOrders = new List<ProductionOrder>();
				var obsoleteHeaderComments = new List<ProductionOrderComment>();
				var intendedStatesForNewOrders = new List<(ProductionOrder Order, ProductionOrderStateEnum IntendedState)>();
				int updatedProductionOrdersCount = 0;
				int skippedNoAdvertiseCount = 0;
				int skippedSameVersionCount = 0;
				int creationAuditFixedCount = 0;
				var commentNow = DateTime.Now;

				// تاریخ ایجاد را از CreationDate راهکاران می‌نویسد. اگر خالی بماند Repository ساعت اجرای جاب را می‌گذارد.
				bool ApplyRahkaranCreationAudit(ProductionOrder po, RahkaranSale_ProductionOrder rahkaran)
				{
					if (rahkaran.CreationDate.Year < 2000)
						return false;

					var changed = false;
					var creation = rahkaran.CreationDate;
					var shamsi = creation.ToShamsiDateTime();
					if (!po.CreatedOnMiladiDateTime.HasValue
						|| Math.Abs((po.CreatedOnMiladiDateTime.Value - creation).TotalSeconds) > 1
						|| !string.Equals(po.CreatedOnShamsiDateTime, shamsi, StringComparison.Ordinal))
					{
						po.CreatedOnMiladiDateTime = creation;
						po.CreatedOnShamsiDateTime = shamsi;
						changed = true;
					}

					if (creatorUserMap.TryGetValue(rahkaran.Creator, out var auditCreatorId)
						&& po.CreatedById != auditCreatorId)
					{
						po.CreatedById = auditCreatorId;
						changed = true;
					}

					if (creatorNameMap.TryGetValue(rahkaran.Creator, out var auditCreatorName)
						&& !string.IsNullOrWhiteSpace(auditCreatorName)
						&& !string.Equals(po.CreatedByName, auditCreatorName, StringComparison.Ordinal))
					{
						po.CreatedByName = auditCreatorName;
						changed = true;
					}

					return changed;
				}

				foreach (var rahkaranPO in rahkaranProductionOrders)
				{
					var rahkaranVersion = ToRahkaranVersionLong(rahkaranPO.Version);
					long createdById = 1;
					if (creatorUserMap.TryGetValue(rahkaranPO.Creator, out var mappedCreatorId))
						createdById = mappedCreatorId;

					long? createdOrganizationUnitId = null;
					if (creatorPartyByHamkaranId.TryGetValue(rahkaranPO.Creator, out var creatorPartyId)
						&& personelOrgUnitByPartyId.TryGetValue(creatorPartyId, out var creatorOrgUnitId))
					{
						createdOrganizationUnitId = creatorOrgUnitId;
					}
					else if (defaultCreatedOrgUnitExists)
					{
						// IIF(HtsPerson.Personel_ID IS NULL, 253, ...)
						createdOrganizationUnitId = DefaultCreatedOrganizationUnitId;
					}

					if (!appProductionOrdersDict.TryGetValue(rahkaranPO.Sale_ProductionOrderID, out var existPO))
					{
						// فیلتر درج SP: JOIN Sale_RequirmentAdvertise (بدون شرط نگاشت کارشناس فروش)
						if (!rahkaranPO.RequirementAdvertiseIdRef.HasValue
							|| !requirementAdvertiseIds.Contains(rahkaranPO.RequirementAdvertiseIdRef.Value))
						{
							skippedNoAdvertiseCount++;
							continue;
						}

						// Number در HTS قدیمی = ProductionOrderNumber (نه فیلد Number راهکاران که شماره داخلی دیگری است)
						var productionOrderNumber = rahkaranPO.ProductionOrderNumber.HasValue
							? (long)rahkaranPO.ProductionOrderNumber.Value
							: (long?)null;
						var numberText = productionOrderNumber?.ToString() ?? rahkaranPO.Number ?? string.Empty;

						// ایجاد رکورد جدید
						// پیوست‌ها: مثل SP قدیمی NULL می‌مانند — شناسه پیوست راهکاران معادل FileEntity اپ نیست
						// وضعیت ۲ و ۳ را جاب SQL راهکاران (کامنت + ایمیل) روی هدر می‌نویسد؛ اینجا Submit می‌ماند تا آن جاب اختلاف State را ببیند.
						var rahkaranState = (ProductionOrderStateEnum)rahkaranPO.State;
						var headerState = IsFinancialStateOwnedByRahkaranEmailJob(rahkaranState)
							? ProductionOrderStateEnum.Submit
							: rahkaranState;
						var newPO = new ProductionOrder
						{
							HamkaranId = rahkaranPO.Sale_ProductionOrderID,
							State = headerState,
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
							RequirmentAdvertiseId = requirmentAdvertiseMap.TryGetValue(rahkaranPO.RequirementAdvertiseIdRef!.Value, out var newRaId) ? newRaId : null,
							Revision = rahkaranPO.Revision.HasValue ? (int)rahkaranPO.Revision.Value : null,
							CustomerIndustry = rahkaranPO.CustomerIndustry,
							ProjectDlCode = rahkaranPO.ProjectDlCode,
							EdmsProject = rahkaranPO.EdmsProjectId,
							RahkaranVersion = rahkaranVersion,
							CreatedById = createdById,
							CreatedOrganizationUnitId = createdOrganizationUnitId,
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

						// کامنت StatusId=2436 → Submit. وضعیت ۲/۳ عمداً Submit می‌ماند تا جاب ایمیل راهکاران آن را عوض کند.
						newPO.Comments.Add(new ProductionOrderComment
						{
							NewState = ProductionOrderStateEnum.Submit,
							PreviousState = null,
							Comment = ReadFromRahkaranComment,
							CreatedById = 1,
							CreatedOnMiladiDateTime = commentNow,
							CreatedOnShamsiDateTime = commentNow.ToShamsiDateTime(),
							IsActive = IsActiveEnum.Active
						});

						if (headerState == ProductionOrderStateEnum.Obsolete)
						{
							newPO.Comments.Add(new ProductionOrderComment
							{
								NewState = ProductionOrderStateEnum.Obsolete,
								PreviousState = ProductionOrderStateEnum.Submit,
								Comment = ObsoleteFromRahkaranComment,
								CreatedById = 1,
								CreatedOnMiladiDateTime = commentNow,
								CreatedOnShamsiDateTime = commentNow.ToShamsiDateTime(),
								IsActive = IsActiveEnum.Active
							});
						}

						ApplyRahkaranCreationAudit(newPO, rahkaranPO);
						newProductionOrders.Add(newPO);
						intendedStatesForNewOrders.Add((newPO, newPO.State));
					}
					else
					{
						if (ApplyRahkaranCreationAudit(existPO, rahkaranPO))
							creationAuditFixedCount++;

						// گیت آپدیت مثل SP: WHERE RahkaranVersion <> CAST(Version AS BIGINT)
						if (existPO.RahkaranVersion.HasValue && existPO.RahkaranVersion == rahkaranVersion)
						{
							skippedSameVersionCount++;
							continue;
						}

						// به‌روزرسانی رکورد موجود
						bool isModified = false;
						var previousState = existPO.State;
						var rahkaranState = (ProductionOrderStateEnum)rahkaranPO.State;
						// وضعیت ۲ و ۳ را این جاب روی هدر نمی‌نویسد؛ جاب SQL با شرط نابرابری State کامنت و ایمیل را یک‌بار می‌فرستد.
						if (!IsFinancialStateOwnedByRahkaranEmailJob(rahkaranState) && existPO.State != rahkaranState)
						{
							existPO.State = rahkaranState;
							isModified = true;
						}

						// منسوخ‌سازی از راهکاران (State=4) — معادل LastStatusId=2207 در SP قدیمی
						if (rahkaranState == ProductionOrderStateEnum.Obsolete
							&& previousState != ProductionOrderStateEnum.Obsolete
							&& !existPO.ObsoletedOnMiladiDate.HasValue)
						{
							var nowObsolete = DateTime.Now;
							existPO.ObsoletedOnMiladiDate = nowObsolete;
							existPO.ObsoletedOnShamsiDate = nowObsolete.ToShamsiDateTime();
							isModified = true;

							if (existPO.Id.HasValue)
							{
								obsoleteHeaderComments.Add(new ProductionOrderComment
								{
									ProductionOrderId = existPO.Id.Value,
									NewState = ProductionOrderStateEnum.Obsolete,
									PreviousState = previousState,
									Comment = ObsoleteFromRahkaranComment,
									CreatedById = 1,
									CreatedOnMiladiDateTime = commentNow,
									CreatedOnShamsiDateTime = commentNow.ToShamsiDateTime(),
									IsActive = IsActiveEnum.Active
								});
							}
						}
						else if (rahkaranState != ProductionOrderStateEnum.Obsolete
							&& previousState == ProductionOrderStateEnum.Obsolete)
						{
							existPO.ObsoletedOnMiladiDate = null;
							existPO.ObsoletedOnShamsiDate = null;
							existPO.ObsoletedById = null;
							isModified = true;
						}

						long? mappedRequirmentAdvertiseId = rahkaranPO.RequirementAdvertiseIdRef.HasValue
							&& requirmentAdvertiseMap.TryGetValue(rahkaranPO.RequirementAdvertiseIdRef.Value, out var existRaId)
							? existRaId
							: null;
						if (existPO.RequirmentAdvertiseId != mappedRequirmentAdvertiseId)
						{
							existPO.RequirmentAdvertiseId = mappedRequirmentAdvertiseId;
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

						// مثل SP: Revision = Revision + 1 (نه کپی از راهکاران)
						existPO.Revision = (existPO.Revision ?? 0) + 1;
						isModified = true;

						// صنعت مشتری روی Havayar می‌ماند. راهکاران این متن را خالی دارد و HTS دیگر منبع به‌روزرسانی نیست.
						if (!string.IsNullOrWhiteSpace(rahkaranPO.CustomerIndustry)
							&& existPO.CustomerIndustry != rahkaranPO.CustomerIndustry)
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

						if (existPO.CreatedById != createdById)
						{
							existPO.CreatedById = createdById;
							isModified = true;
						}

						if (existPO.CreatedOrganizationUnitId != createdOrganizationUnitId)
						{
							existPO.CreatedOrganizationUnitId = createdOrganizationUnitId;
							isModified = true;
						}

						if (existPO.RahkaranVersion != rahkaranVersion)
						{
							existPO.RahkaranVersion = rahkaranVersion;
							isModified = true;
						}

						if (isModified)
						{
							updatedProductionOrdersCount++;
						}
					}
				}

				if (skippedNoAdvertiseCount > 0)
				{
					await jobLogger?.LogInfoAsync(
						$"رد {skippedNoAdvertiseCount} سفارش ساخت جدید بدون اعلام نیازمندی (فیلتر JOIN Sale_RequirmentAdvertise)",
						cn);
				}

				if (skippedSameVersionCount > 0)
				{
					await jobLogger?.LogInfoAsync(
						$"رد به‌روزرسانی {skippedSameVersionCount} سفارش ساخت با RahkaranVersion یکسان",
						cn);
				}

				if (creationAuditFixedCount > 0)
				{
					await jobLogger?.LogInfoAsync(
						$"اصلاح تاریخ ایجاد یا ایجادکننده {creationAuditFixedCount} سفارش ساخت از راهکاران",
						cn);
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

				if (obsoleteHeaderComments.Any())
				{
					await unitOfWork.Repository<ProductionOrderComment>().AddRangeAsync(obsoleteHeaderComments, cn, false);
				}

				await unitOfWork.SaveChangesAsync(cn);

				// ProductionOrderCommentAction ممکن است State را از کامنت 2436/Submit بازنویسی کند — State intentional از راهکاران را برگردان
				foreach (var (po, intendedState) in intendedStatesForNewOrders)
				{
					if (!po.Id.HasValue)
						continue;
					if (intendedState == ProductionOrderStateEnum.Submit
						|| intendedState == ProductionOrderStateEnum.Obsolete)
						continue;

					await unitOfWork.Repository<ProductionOrder>().UpdateFieldsAsync(
						po.Id.Value,
						c => c.State,
						intendedState,
						cn);
					po.State = intendedState;
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی سفارش ساخت با موفقیت انجام شد", cn);

				// همگام‌سازی ProductionOrderItem — معادل InsertOrUpdateProductionOrderItems
				// منبع: Sale_ProductionOrderItemHistory (Status: 0=جدید، 1=ویرایش/نسخه جدید، 2=حذف)
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی اقلام سفارش ساخت از تاریخچه راهکاران...", cn);
				var historyItems = await Rdb.RahkaranSale_ProductionOrderItemHistory
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {historyItems.Count} رکورد تاریخچه قلم سفارش ساخت در راهکاران یافت شد", cn);

				var liveItemsById = (await Rdb.RahkaranSale_ProductionOrderItem
					.AsNoTracking()
					.ToListAsync(cn))
					.ToDictionary(x => x.Sale_ProductionOrderItemID, x => x);

				var allAppProductionOrders = await unitOfWork.Repository<ProductionOrder>()
					.Table
					.ToListAsync(cn);

				var productionOrderMap = allAppProductionOrders
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);
				var rahkaranHeaderIds = rahkaranProductionOrders
					.Select(x => x.Sale_ProductionOrderID)
					.ToHashSet();

				// Part: Status=0 در SP با Hamkaran_Part_FK؛ Status=1 با Part_Code
				var appParts = await unitOfWork.Repository<Part>()
					.Table
					.ToListAsync(cn);
				var partMapByHamkaranId = appParts
					.Where(x => x.HamkaranId.HasValue)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(
						g => g.Key,
						g => g.First());
				var partMapByCode = appParts
					.Where(x => !string.IsNullOrWhiteSpace(x.Code))
					.GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

				var appItems = await unitOfWork.Repository<ProductionOrderItem>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appItems.Count} رکورد قلم سفارش ساخت در پایگاه داده برنامه موجود است", cn);

				// کلید نسخه در HTS قدیمی: RahkaranId = History.Id
				var appItemsByHistoryId = appItems
					.Where(x => x.RahkaranHistoryId.HasValue)
					.GroupBy(x => x.RahkaranHistoryId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				var appLatestByHamkaranId = appItems
					.Where(x => x.HamkaranId.HasValue && x.IsLatestVersion && !x.IsDeleted)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				// برای bridge امن: HamkaranId + Revision بدون RahkaranHistoryId
				var appItemsByHamkaranRevision = appItems
					.Where(x => x.HamkaranId.HasValue && !x.RahkaranHistoryId.HasValue)
					.GroupBy(x => $"{x.HamkaranId!.Value}_{x.Revision?.ToString() ?? "null"}")
					.ToDictionary(g => g.Key, g => g.First());

				var obsoleteProductionOrderIds = allAppProductionOrders
					.Where(x => x.State == ProductionOrderStateEnum.Obsolete && x.Id.HasValue)
					.Select(x => x.Id!.Value)
					.ToHashSet();

				var newItems = new List<ProductionOrderItem>();
				var historyById = historyItems.ToDictionary(x => x.Id);

				int insertedNewCount = 0;
				int insertedRevisionCount = 0;
				int bridgedCount = 0;
				int skippedItemsCount = 0;
				int skippedMissingSourceParentNewCount = 0;
				int skippedMissingSourceParentEditCount = 0;
				int skippedNoPartCount = 0;
				int deletedItemsCount = 0;
				int syncedLatestCount = 0;

				ProductionOrderItemCheckStatusEnum ResolveCheckStatus(int? checkStatusId) =>
					checkStatusId.HasValue
					&& Enum.IsDefined(typeof(ProductionOrderItemCheckStatusEnum), checkStatusId.Value)
						? (ProductionOrderItemCheckStatusEnum)checkStatusId.Value
						: ProductionOrderItemCheckStatusEnum.InitialRegistration;

				ProductionOrderItemDeviceTypeEnum? ResolveDeviceType(int? deviceTypeId) =>
					deviceTypeId.HasValue
					&& Enum.IsDefined(typeof(ProductionOrderItemDeviceTypeEnum), deviceTypeId.Value)
						? (ProductionOrderItemDeviceTypeEnum?)deviceTypeId.Value
						: null;

				ProductionOrderItemEquipmentTypeEnum? ResolveEquipmentType(int? equipmentTypeId) =>
					equipmentTypeId.HasValue
					&& Enum.IsDefined(typeof(ProductionOrderItemEquipmentTypeEnum), equipmentTypeId.Value)
						? (ProductionOrderItemEquipmentTypeEnum?)equipmentTypeId.Value
						: null;

				bool TryResolvePart(
					RahkaranSale_ProductionOrderItemHistory history,
					bool preferCodeFallback,
					out Part? part)
				{
					part = null;
					if (history.PartIdRef.HasValue
						&& partMapByHamkaranId.TryGetValue(history.PartIdRef.Value, out part))
						return true;

					// معادل JOIN روی Part_Code در شاخه Status=1 اسپ
					if (preferCodeFallback)
					{
						string? code = null;
						if (history.Sale_ProductionOrderItemID.HasValue
							&& liveItemsById.TryGetValue(history.Sale_ProductionOrderItemID.Value, out var live)
							&& !string.IsNullOrWhiteSpace(live.PartCode))
							code = live.PartCode.Trim();

						if (!string.IsNullOrWhiteSpace(code)
							&& partMapByCode.TryGetValue(code, out part))
							return true;
					}

					return false;
				}

				// فقط شناسه History — بدون بازنویسی فیلدهای عملیاتی/فروش (برخلاف UPDATE واقعی SP که INSERT است)
				void StampRahkaranHistoryId(ProductionOrderItem existItem, RahkaranSale_ProductionOrderItemHistory history)
				{
					existItem.RahkaranHistoryId = history.Id;
					if (!existItem.HamkaranId.HasValue && history.Sale_ProductionOrderItemID.HasValue)
						existItem.HamkaranId = history.Sale_ProductionOrderItemID.Value;
					if (string.IsNullOrEmpty(existItem.Version) && history.Version.HasValue)
						existItem.Version = history.Version.Value.ToString();
				}

				ProductionOrderItem? TryBridgeExisting(RahkaranSale_ProductionOrderItemHistory history)
				{
					if (!history.Sale_ProductionOrderItemID.HasValue)
						return null;

					var key = $"{history.Sale_ProductionOrderItemID.Value}_{history.Revision?.ToString() ?? "null"}";
					if (!appItemsByHamkaranRevision.TryGetValue(key, out var bridged))
						return null;

					StampRahkaranHistoryId(bridged, history);
					appItemsByHistoryId[history.Id] = bridged;
					appItemsByHamkaranRevision.Remove(key);
					return bridged;
				}

				ProductionOrderItem BuildItemFromHistory(
					RahkaranSale_ProductionOrderItemHistory history,
					long productionOrderId,
					long saleProductionOrderItemId,
					Part part,
					DateTime now)
				{
					var isRoutine = part.DesignTypeIsRoutine ?? false;
					string? partCode = part.Code;
					if (string.IsNullOrWhiteSpace(partCode)
						&& liveItemsById.TryGetValue(saleProductionOrderItemId, out var live)
						&& !string.IsNullOrWhiteSpace(live.PartCode))
					{
						partCode = live.PartCode;
					}

					var item = new ProductionOrderItem
					{
						HamkaranId = saleProductionOrderItemId,
						RahkaranHistoryId = history.Id,
						ProductionOrderId = productionOrderId,
						IsBuildInside = history.IsBuildInside ?? false,
						DeviceType = ResolveDeviceType(history.DeviceTypeId),
						EquipmentType = ResolveEquipmentType(history.EquipmentTypeId),
						PartId = part.Id,
						Amount = history.Amount,
						AgreedDeliverDate = history.AgreedDeliverDate,
						AgreedDeliverShamsiDate = history.AgreedDeliverDate?.ToShamsiDate(),
						PartModel = history.PartModel,
						AirendOrCategory = history.AirendOrCategory,
						InputPressureBar = history.InputPressureBar,
						OutputPressureBar = history.OutputPressureBar,
						Capacity = history.Capacity,
						Scale = history.Scale,
						GasType = history.GasType,
						MaximumTemperature = history.MaximumTemperature,
						PurityPercentage = history.PurityPercentage,
						RelativeHumidity = history.RelativeHumidity,
						BarometricPressure = history.BarometricPressure,
						MovingType = history.MovingType,
						HasInspection = history.HasInspection ?? false,
						SalesConsideration = history.SalesConsideration,
						Revision = history.Revision,
						IsLatestVersion = history.IsLatestVersion ?? true,
						CheckStatus = ResolveCheckStatus(history.CheckStatusId),
						PartCode = partCode,
						IsRoutine = isRoutine,
						Version = history.Version?.ToString(),
						Status = ProductionOrderItemStatusEnum.NotCompleted,
						IsDeleted = false
					};

					item.ProductionOrderItemComments.Add(new ProductionOrderItemComment
					{
						// معادل StatusId=1903 در SP قدیمی
						ProductionStatus = ProductionOrderItemProductionStatusEnum.InitialRegistration,
						Comment = "خوانده شده از راهکاران",
						StartMiladiDateTime = now,
						StartShamsiDate = now.ToShamsiDateTime(),
						CreatedById = 1,
						CreatedOnMiladiDateTime = now,
						CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
						IsActive = IsActiveEnum.Active
					});

					return item;
				}

				var nowDate = DateTime.Now;

				//------------------------------------------------------------------
				// Status = 0 → درج قلم جدید (معادل #NewRahkaranProductionOrderItem)
				//------------------------------------------------------------------
				foreach (var history in historyItems.Where(h => h.Status == 0))
				{
					if (appItemsByHistoryId.ContainsKey(history.Id))
						continue;

					long? productionOrderId = null;
					var parentResolved = history.ProductionOrderId.HasValue
						&& productionOrderMap.TryGetValue(history.ProductionOrderId.Value, out productionOrderId)
						&& productionOrderId.HasValue;
					if (!parentResolved || !history.Sale_ProductionOrderItemID.HasValue)
					{
						// والد در جدول سربرگ راهکاران نیست؛ شکاف منبع است و هر دور خط جدا نمی‌خواهد.
						if (!parentResolved
							&& (!history.ProductionOrderId.HasValue
								|| !rahkaranHeaderIds.Contains(history.ProductionOrderId.Value)))
						{
							skippedMissingSourceParentNewCount++;
							continue;
						}

						await jobLogger?.LogWarningAsync(
							$"سفارش ساخت والد با HamkaranId {history.ProductionOrderId} برای History.Id={history.Id} یافت نشد", 0, cn);
						skippedItemsCount++;
						continue;
					}

					var saleItemId = history.Sale_ProductionOrderItemID.Value;

					// پل امن: فقط اگر همان Revision بدون HistoryId باشد — بدون overwrite فیلدها
					if (TryBridgeExisting(history) != null)
					{
						bridgedCount++;
						continue;
					}

					// SP: INNER JOIN Part روی Hamkaran_Part_FK
					if (!TryResolvePart(history, preferCodeFallback: false, out var part) || part?.Id == null)
					{
						await jobLogger?.LogWarningAsync(
							$"کالای PartIdRef={history.PartIdRef} برای History.Id={history.Id} یافت نشد (معادل INNER JOIN اسپ)", 0, cn);
						skippedNoPartCount++;
						continue;
					}

					// SP: JOIN Gnr_AllDate روی AgreedDeliverDate — بدون تاریخ وارد نمی‌شود
					if (!history.AgreedDeliverDate.HasValue)
					{
						await jobLogger?.LogWarningAsync(
							$"AgreedDeliverDate خالی برای History.Id={history.Id} — رد شد (معادل JOIN تاریخ اسپ)", 0, cn);
						skippedItemsCount++;
						continue;
					}

					var newItem = BuildItemFromHistory(history, productionOrderId.Value, saleItemId, part, nowDate);
					newItems.Add(newItem);
					appItemsByHistoryId[history.Id] = newItem;
					if (newItem.IsLatestVersion)
						appLatestByHamkaranId[saleItemId] = newItem;
					insertedNewCount++;
				}

				//------------------------------------------------------------------
				// Status = 1 → به‌ازای هر ویرایش قلم جدید (معادل #Rahkaran_ProductinoOrderItem_UpdatedItems)
				//------------------------------------------------------------------
				foreach (var history in historyItems.Where(h => h.Status == 1))
				{
					if (appItemsByHistoryId.ContainsKey(history.Id))
						continue;

					long? productionOrderId = null;
					var parentResolved = history.ProductionOrderId.HasValue
						&& productionOrderMap.TryGetValue(history.ProductionOrderId.Value, out productionOrderId)
						&& productionOrderId.HasValue;
					if (!parentResolved || !history.Sale_ProductionOrderItemID.HasValue)
					{
						if (!parentResolved
							&& (!history.ProductionOrderId.HasValue
								|| !rahkaranHeaderIds.Contains(history.ProductionOrderId.Value)))
						{
							skippedMissingSourceParentEditCount++;
							continue;
						}

						await jobLogger?.LogWarningAsync(
							$"سفارش ساخت والد با HamkaranId {history.ProductionOrderId} برای ویرایش History.Id={history.Id} یافت نشد", 0, cn);
						skippedItemsCount++;
						continue;
					}

					var saleItemId = history.Sale_ProductionOrderItemID.Value;

					if (TryBridgeExisting(history) != null)
					{
						bridgedCount++;
						continue;
					}

					if (!history.AgreedDeliverDate.HasValue)
					{
						await jobLogger?.LogWarningAsync(
							$"AgreedDeliverDate خالی برای ویرایش History.Id={history.Id} — رد شد", 0, cn);
						skippedItemsCount++;
						continue;
					}

					// SP: INNER JOIN Part روی Code
					if (!TryResolvePart(history, preferCodeFallback: true, out var part) || part?.Id == null)
					{
						await jobLogger?.LogWarningAsync(
							$"کالا برای ویرایش History.Id={history.Id} (PartIdRef={history.PartIdRef}) یافت نشد", 0, cn);
						skippedNoPartCount++;
						continue;
					}

					// نسخه قبلی همان قلم دیگر آخرین نسخه نیست (تا قبل از sync سراسری IsLatestVersion)
					if (appLatestByHamkaranId.TryGetValue(saleItemId, out var previousLatest))
					{
						previousLatest.IsLatestVersion = false;
					}

					var newItem = BuildItemFromHistory(history, productionOrderId.Value, saleItemId, part, nowDate);
					newItems.Add(newItem);
					appItemsByHistoryId[history.Id] = newItem;
					if (newItem.IsLatestVersion)
						appLatestByHamkaranId[saleItemId] = newItem;
					insertedRevisionCount++;
				}

				//------------------------------------------------------------------
				// همگام‌سازی IsLatestVersion از History (معادل UPDATE انتهای SP)
				//------------------------------------------------------------------
				foreach (var item in appItems.Concat(newItems).Where(i => i.RahkaranHistoryId.HasValue))
				{
					if (!historyById.TryGetValue(item.RahkaranHistoryId!.Value, out var history))
						continue;

					var historyLatest = history.IsLatestVersion ?? false;
					if (item.IsLatestVersion != historyLatest)
					{
						item.IsLatestVersion = historyLatest;
						syncedLatestCount++;
					}
				}

				//------------------------------------------------------------------
				// Status = 2 → باطل (معادل UPDATE IsDeleted/ProductionStepId=227/StatusId=581)
				//------------------------------------------------------------------
				var deletedHistoryIds = historyItems
					.Where(h => h.Status == 2)
					.Select(h => h.Id)
					.ToHashSet();

				foreach (var item in appItems.Where(i =>
					i.RahkaranHistoryId.HasValue
					&& deletedHistoryIds.Contains(i.RahkaranHistoryId.Value)
					&& !i.IsDeleted))
				{
					item.IsDeleted = true;
					item.IsLatestVersion = false;
					item.IsActive = IsActiveEnum.Deleted;
					item.ProductionStep = ProductionOrderItemProductionStepEnum.Canceled;
					item.Status = ProductionOrderItemStatusEnum.Invalid;
					deletedItemsCount++;
				}

				// منسوخ‌سازی / خروج از منسوخی اقلام بر اساس State هدر (معادل BuyStatusId=2208 در SP هدر)
				int deprecatedItemsCount = 0;
				int restoredFromDeprecatedCount = 0;
				foreach (var item in appItems.Concat(newItems)
					.Where(i => i.IsLatestVersion && !i.IsDeleted && i.IsActive != IsActiveEnum.Deleted))
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
					else if (item.CheckStatus == ProductionOrderItemCheckStatusEnum.Deprecated
						&& !newItems.Contains(item))
					{
						item.CheckStatus = ProductionOrderItemCheckStatusEnum.InitialRegistration;
						restoredFromDeprecatedCount++;
					}
				}

				if (insertedNewCount > 0)
					await jobLogger?.LogInfoAsync($"درج {insertedNewCount} قلم جدید از History.Status=0", cn);
				if (insertedRevisionCount > 0)
					await jobLogger?.LogInfoAsync($"درج {insertedRevisionCount} نسخه جدید از History.Status=1", cn);
				if (bridgedCount > 0)
					await jobLogger?.LogInfoAsync($"اتصال {bridgedCount} قلم موجود به RahkaranHistoryId", cn);
				if (syncedLatestCount > 0)
					await jobLogger?.LogInfoAsync($"همگام‌سازی IsLatestVersion برای {syncedLatestCount} قلم", cn);
				if (deletedItemsCount > 0)
					await jobLogger?.LogInfoAsync($"باطل شدن {deletedItemsCount} قلم بر اساس History.Status=2", cn);
				if (deprecatedItemsCount > 0)
					await jobLogger?.LogInfoAsync($"تعداد {deprecatedItemsCount} قلم به‌خاطر منسوخ شدن سفارش ساخت، Deprecated شدند", cn);
				if (restoredFromDeprecatedCount > 0)
					await jobLogger?.LogInfoAsync($"تعداد {restoredFromDeprecatedCount} قلم از منسوخی خارج و به ثبت اولیه برگشتند", cn);
				if (skippedMissingSourceParentNewCount > 0 || skippedMissingSourceParentEditCount > 0)
					await jobLogger?.LogInfoAsync(
						$"رد {skippedMissingSourceParentNewCount} قلم جدید و {skippedMissingSourceParentEditCount} ویرایش؛ سفارش والد در USR3.Sale_ProductionOrder نیست",
						cn);
				if (skippedItemsCount > 0)
					await jobLogger?.LogWarningAsync($"تعداد {skippedItemsCount} قلم به دلیل والد/تاریخ نامعتبر رد شد", 0, cn);
				if (skippedNoPartCount > 0)
					await jobLogger?.LogWarningAsync($"تعداد {skippedNoPartCount} قلم به دلیل عدم یافتن کالا (INNER JOIN اسپ) رد شد", 0, cn);

				if (newItems.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newItems.Count} قلم جدید (شامل نسخه‌های جدید) به پایگاه داده", cn);
					await unitOfWork.Repository<ProductionOrderItem>().AddRangeAsync(newItems, cn, false);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی اقلام سفارش ساخت با موفقیت انجام شد", cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی کامل سفارش ساخت و اقلام با موفقیت به پایان رسید", cn);

				if (capturedQueueIds.Count > 0)
				{
					await DeleteSyncQueueIdsAsync(capturedQueueIds, cn);
					await jobLogger?.LogInfoAsync(
						$"از صف همگام سفارش ساخت {capturedQueueIds.Count} شناسه پاک شد",
						cn);
				}

				var pendingQueueCount = await CountSyncQueueAsync(cn);
				return pendingQueueCount > 0;
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private async Task<List<int>> LoadSyncQueueIdsAsync(CancellationToken cn)
		{
			return await Rdb.Database
				.SqlQueryRaw<int>("SELECT Id AS Value FROM dbo.Sale_ProductionOrderSyncQueue")
				.ToListAsync(cn);
		}

		private async Task<int> CountSyncQueueAsync(CancellationToken cn)
		{
			var counts = await Rdb.Database
				.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.Sale_ProductionOrderSyncQueue")
				.ToListAsync(cn);
			return counts.Count == 0 ? 0 : counts[0];
		}

		private async Task DeleteSyncQueueIdsAsync(IReadOnlyList<int> ids, CancellationToken cn)
		{
			const int batchSize = 400;
			for (var offset = 0; offset < ids.Count; offset += batchSize)
			{
				var batch = ids.Skip(offset).Take(batchSize).ToArray();
				var parameters = new object[batch.Length];
				var names = new string[batch.Length];
				for (var i = 0; i < batch.Length; i++)
				{
					names[i] = $"@p{i}";
					parameters[i] = new SqlParameter(names[i], batch[i]);
				}

				var sql = "DELETE FROM dbo.Sale_ProductionOrderSyncQueue WHERE Id IN (" + string.Join(", ", names) + ")";
				await Rdb.Database.ExecuteSqlRawAsync(sql, parameters, cn);
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

				var industrialItems = productionOrderItems
					.Where(p => p.CheckStatus == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal)
					.ToList();
				if (industrialItems.Any())
					await SendIndustrialCartableNotificationAsync(industrialItems, jobLogger, cn);
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
					var partIsRoutine = part.DesignTypeIsRoutine ?? false;
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
			var awaitingPmStep = ProductionOrderItemProductionStepEnum.AwaitingProjectManager;

			// معادل HTS SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable:
			// فقط مرحله ساخت ۱۳۸۰. وضعیت بررسی ۲۲۵۸ در این جاب نیست (در HTS هم از مهلت یک‌روزه کامنت شده).
			var items = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(p => p.ProductionOrder)
				.Include(p => p.Part)
				.Where(p => p.IsLatestVersion &&
							!p.IsDeleted &&
							p.IsActive != IsActiveEnum.Deleted &&
							p.ProductionStep == awaitingPmStep)
				.ToListAsync(cn);

			var nowDate = DateTime.Now;
			var finalItems = new List<ProductionOrderItem>();
			var newComments = new List<ProductionOrderItemComment>();

			// معادل HTS: مبنای مهلت، تاریخ آخرین کامنت StatusId=1380 است. بدون این کامنت قلم رد می‌شود.
			var itemIds = items.Select(i => i.Id!.Value).ToList();
			var lastStepCommentDates = itemIds.Count == 0
				? new Dictionary<long, DateTime?>()
				: (await unitOfWork.Repository<ProductionOrderItemComment>()
					.TableNoTracking
					.Where(c => itemIds.Contains(c.ProductionOrderItemId)
						&& c.IsForProductionStepStatus
						&& c.ProductionStep == awaitingPmStep
						&& c.IsActive == IsActiveEnum.Active)
					.GroupBy(c => c.ProductionOrderItemId)
					.Select(g => new { g.Key, Date = g.Max(c => c.CreatedOnMiladiDateTime) })
					.ToListAsync(cn))
					.ToDictionary(x => x.Key, x => x.Date);

			foreach (var item in items)
			{
				if (!lastStepCommentDates.TryGetValue(item.Id!.Value, out var stepDate) || stepDate == null)
					continue;
				if (nowDate <= stepDate.Value.AddDays(deadLineInDay))
					continue;

				// معادل HTS: فقط ProductionStepId → 2744. BuyStatus/CheckStatus عوض نمی‌شود.
				item.ProductionStep = ProductionOrderItemProductionStepEnum.AwaitingIndustry;
				finalItems.Add(item);

				var statusForHistory = item.CheckStatus.HasValue
					? (ProductionOrderItemProductionStatusEnum)item.CheckStatus.Value
					: ProductionOrderItemProductionStatusEnum.NotDetermined;

				newComments.Add(new ProductionOrderItemComment
				{
					ProductionOrderItemId = (long)item.Id!,
					ProductionStatus = statusForHistory,
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

					await SendHoldedItemsIndustrialNotificationAsync(finalItems, jobLogger, cn);

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

				// معادل HTS SendExpiredOrdersNotification: مبنا BuyStatusId ∈ {۲۱۹۵ مکانیک، ۲۱۹۶ برق} است (در سیستم جدید هر دو
				// رشته با ۲۱۹۵ + DeviceType نمایش داده می‌شوند)، نه مرحله ساخت — قلمی که پس از تایید مهندسی به کمیته (۲۲۰۲)
				// رفته هنوز مرحله «در انتظار مهندسی» دارد و نباید دوباره توسط این Job ارجاع شود.
			var items = await unitOfWork.Repository<ProductionOrderItem>()
				.Table
				.Include(p => p.Part)
				.Include(p => p.ProductionOrder)
				.Where(p => p.IsLatestVersion &&
							p.IsActive != IsActiveEnum.Deleted &&
							p.CheckStatus == ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending)
				.ToListAsync(cn);

				var nowDate = DateTime.Now;
				var finalItems = new List<ProductionOrderItem>();
				var industrialItems = new List<ProductionOrderItem>();
				var committeeItems = new List<ProductionOrderItem>();
				var newComments = new List<ProductionOrderItemComment>();

				foreach (var item in items)
				{
					// استفاده از CheckStatusChangedOnMiladiDate به جای کامنت‌ها، چون دقیق‌تر و مستقیماً وابسته به CheckStatus است
					if (item.CheckStatusChangedOnMiladiDate == null || nowDate <= item.CheckStatusChangedOnMiladiDate.Value.AddDays(deadLineInDay))
						continue;

					// معادل HTS: (غیرروتین و ساخت داخل) یا پیشوند کالای ساخت داخل → صنایع داخلی ۱۹۰۴؛ وگرنه → کمیته تامین ۲۲۰۲
					var next = ProductionOrderItemWorkflowRules.ResolveAfterEngineeringApproval(item.IsRoutine, item.IsBuildInside, item.Part?.Code);
					var goesToIndustrial = next == ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;

					item.CheckStatus = next;
					item.CheckStatusChangedOnMiladiDate = nowDate;
					item.CheckStatusChangedOnShamsiDate = nowDate.ToShamsiDateTime();
					if (goesToIndustrial)
					{
						// معادل HTS: فقط BuyStatus عوض می‌شود. مرحله ساخت را صنایع خودش ثبت می‌کند.
						TrySetSendToIndustrial(item, next, nowDate);
						industrialItems.Add(item);
					}
					else
					{
						committeeItems.Add(item);
					}

					finalItems.Add(item);

					// تب «وضعیت سفارش ساخت» (معادل کامنت BuyStatus در HTS با متن «تایید خودکار بعلت منقضی شدن زمان انتظار»)
					newComments.Add(new ProductionOrderItemComment
					{
						ProductionOrderItemId = (long)item.Id!,
						ProductionStatus = (ProductionOrderItemProductionStatusEnum)next,
						Comment = "تایید خودکار بعلت منقضی شدن زمان انتظار مهندسی",
						CreatedById = 1,
						CreatedOnMiladiDateTime = nowDate,
						CreatedOnShamsiDateTime = nowDate.ToShamsiDateTime(),
						StartMiladiDateTime = nowDate,
						StartShamsiDate = nowDate.ToShamsiDateTime(),
						IsForProductionMode = false,
						IsForProductionStepStatus = false,
						IsActive = IsActiveEnum.Active
					});

				}

				if (finalItems.Any())
				{
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newComments, cn, false);
					await unitOfWork.SaveChangesAsync(cn);

					// اطلاع به تاییدکنندگان مهندسی که مهلت‌شان گذشت (معادل SendExpiredItemsNotification در HTS)
					await SendEngineeringNotificationAsync(finalItems, "انتقال خودکار اقلام منقضی‌شده در کارتابل مهندسی", jobLogger, cn);

					if (industrialItems.Any())
						await SendGroupNotificationAsync(
							ProductionOrderItemNotificationHelper.GroupIndustrial,
							"ارجاع خودکار اقلام سفارش ساخت به کارتابل صنایع (انقضای مهلت مهندسی)",
							"اقلام ذیل به‌علت انقضای مهلت بررسی مهندسی به‌صورت خودکار به کارتابل صنایع ارسال شدند",
							industrialItems, jobLogger, cn);

					if (committeeItems.Any())
						await SendSupplyCommitteeNotificationAsync(
							"ارجاع خودکار اقلام سفارش ساخت به رئیس کمیته تامین (انقضای مهلت مهندسی)",
							"اقلام ذیل به‌علت انقضای مهلت بررسی مهندسی به‌صورت خودکار جهت بررسی پروسه تامین به کارتابل شما ارسال شدند",
							committeeItems, jobLogger, cn);

					await jobLogger?.LogInfoAsync($"تعداد {finalItems.Count} قلم کالا به دلیل انقضای زمان بررسی مهندسی ارجاع شدند ({industrialItems.Count} به صنایع، {committeeItems.Count} به کمیته تامین)", cn);
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

				// معادل HTS ProductionOrderItemService.GetNearDeliveryItems(15): فقط اقلامی که «تاریخ تحویل مؤثر» آن‌ها دقیقاً
				// ۱۵ روز دیگر است (نه پنجره ۱۵ روزه — تا هر قلم فقط یک‌بار هشدار بگیرد). تاریخ مؤثر = دیرترینِ تاریخ تحویل توافقی
				// و تاریخ تحویل استاندارد؛ اقلام ثبت اولیه (۱۹۰۳)، باطل (۵۸۱)، آماده ارسال (۲۲۱) و تحویل فروش (۲۲۳) مستثنا هستند.
				const int interval = 15;
				var targetDate = DateTime.Now.Date.AddDays(interval);
				var nextDay = targetDate.AddDays(1);

				var excludedSteps = new List<ProductionOrderItemProductionStepEnum>
				{
					ProductionOrderItemProductionStepEnum.ReadyForShipping,
					ProductionOrderItemProductionStepEnum.DeliveredToSales
				};

				var items = await unitOfWork.Repository<ProductionOrderItem>()
					.TableNoTracking
					.Include(p => p.ProductionOrder)
					.Include(p => p.Part)
					.Where(p => p.IsLatestVersion &&
								!p.IsDeleted &&
								p.IsActive != IsActiveEnum.Deleted &&
								p.CheckStatus != ProductionOrderItemCheckStatusEnum.InitialRegistration &&
								p.Status != ProductionOrderItemStatusEnum.Invalid &&
								!excludedSteps.Contains(p.ProductionStep) &&
								(
									(p.StandardDeliveryMiladiDate != null && p.AgreedDeliverDate == null
										&& p.StandardDeliveryMiladiDate >= targetDate && p.StandardDeliveryMiladiDate < nextDay) ||
									(p.AgreedDeliverDate != null && p.StandardDeliveryMiladiDate == null
										&& p.AgreedDeliverDate >= targetDate && p.AgreedDeliverDate < nextDay) ||
									(p.AgreedDeliverDate != null && p.StandardDeliveryMiladiDate != null && p.AgreedDeliverDate > p.StandardDeliveryMiladiDate
										&& p.AgreedDeliverDate >= targetDate && p.AgreedDeliverDate < nextDay) ||
									(p.AgreedDeliverDate != null && p.StandardDeliveryMiladiDate != null && p.StandardDeliveryMiladiDate > p.AgreedDeliverDate
										&& p.StandardDeliveryMiladiDate >= targetDate && p.StandardDeliveryMiladiDate < nextDay)
								))
					.ToListAsync(cn);

				if (!items.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ قلمی با تاریخ تحویل نزدیک یافت نشد", cn);
					return;
				}

				await jobLogger?.LogInfoAsync($"تعداد {items.Count} قلم سفارش ساخت با تاریخ تحویل نزدیک یافت شد", cn);

				var sentCount = await SendNearDeliveryDateNotification(items, interval, jobLogger, cn);

				await jobLogger?.LogInfoAsync($"اطلاع‌رسانی برای {sentCount} قلم سفارش ساخت با تاریخ تحویل نزدیک صف شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// معادل HTS ProductionOrderItemInquiryService.SendExpiredItemsNotification (زمان‌بندی ۰۹:۰۱ و ۱۵:۰۱ در HTS):
		/// آخرین استعلام هر قلم اگر در وضعیت «نیاز به استعلام» (۱۹۰۶) یا «نیاز به بررسی وزارت صنایع» (۱۹۰۸) بوده و LeadTime آن
		/// گذشته باشد، به مسئول استعلام و کارشناسان/رئیس کمیته تامین اعلان انقضا ارسال می‌شود.
		/// </summary>
		[JobHandler("اعلان انقضای زمان استعلام اقلام سفارش ساخت")]
		public async Task SendExpiredInquiryItemsNotification(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع بررسی استعلام‌های منقضی‌شده اقلام سفارش ساخت", cn);

				var now = DateTime.Now;
				var inquiryStatuses = ProductionOrderItemWorkflowRules.InquiryStatuses;

				// آخرین استعلام هر قلم (معادل GroupBy + OrderByDescending(Id).First در HTS)
				var latestInquiryIds = await unitOfWork.Repository<ProductionOrderItemInquiry>()
					.TableNoTracking
					.Where(i => i.IsActive == IsActiveEnum.Active)
					.GroupBy(i => i.ProductionOrderItemId)
					.Select(g => g.Max(i => i.Id))
					.ToListAsync(cn);

				if (!latestInquiryIds.Any())
				{
					await jobLogger?.LogInfoAsync("هیچ استعلامی ثبت نشده است", cn);
					return;
				}

				var expired = await unitOfWork.Repository<ProductionOrderItemInquiry>()
					.TableNoTracking
					.Include(i => i.ProductionOrderItem).ThenInclude(p => p.Part)
					.Include(i => i.ProductionOrderItem).ThenInclude(p => p.ProductionOrder)
					.Where(i => latestInquiryIds.Contains(i.Id)
						&& inquiryStatuses.Contains(i.Status)
						&& i.LeadTimeMiladiDate != null
						&& i.LeadTimeMiladiDate < now)
					.ToListAsync(cn);

				if (!expired.Any())
				{
					await jobLogger?.LogInfoAsync("استعلام منقضی‌شده‌ای یافت نشد", cn);
					return;
				}

				var committeeEmails = await ProductionOrderItemNotificationHelper.GetRoleEmailsAsync(appContext, "Sale.ProductionOrderItem.SupplyCommitteeBoss", cn);
				committeeEmails.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSupplyCommitteeExperts, cn));

				var queued = 0;
				foreach (var inquiry in expired)
				{
					var item = inquiry.ProductionOrderItem;
					var to = new List<string?>(committeeEmails);
					if (inquiry.ResponsibleId is > 0)
						to.AddRange(await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(appContext, new[] { inquiry.ResponsibleId.Value }, cn));

					var creatorEmails = inquiry.CreatedById is > 0
						? await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(appContext, new[] { inquiry.CreatedById.Value }, cn)
						: new List<string>();

					var body = ProductionOrderItemNotificationHelper.BuildEmailShell(
						"بدینوسیله به استحضار می رساند مدت زمان اخذ استعلام تامین اقلام سفارشات ذیل منقضی گشته است.<br /><br /><ul>"
						+ ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", item?.ProductionOrder?.ProductionOrderNumber ?? (object?)item?.ProductionOrder?.Number)
						+ ProductionOrderItemNotificationHelper.Li("مسئول استعلام", inquiry.ResponsibleName)
						+ ProductionOrderItemNotificationHelper.Li("مدت زمان اخذ استعلام", inquiry.LeadTimeShamsiDate)
						+ ProductionOrderItemNotificationHelper.Li("وضعیت", inquiry.Status.ToDisplay())
						+ ProductionOrderItemNotificationHelper.Li("کامنت آخر", inquiry.Comment)
						+ ProductionOrderItemNotificationHelper.Li("کد کالا", item?.Part?.Code)
						+ ProductionOrderItemNotificationHelper.Li("عنوان کالا", item?.Part?.Name)
						+ ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item?.Amount)
						+ "</ul>");

					if (await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork,
							"اعلان انقضای زمان استعلام تامین اقلام سفارش ساخت", body, to, creatorEmails,
							item?.Id, inquiry.ResponsibleId ?? 1, $"Panel/ProductionOrderItem/Edit/{item?.Id}", cn, saveNow: false))
						queued++;
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"اعلان انقضای استعلام برای {queued} قلم صف شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// معادل HtsTaskService.AddRoutineBomListTask: قلم آخرین نسخه در کارتابل صنایع (۱۹۰۴/۱۹۰۵)
		/// با وضعیت ۵۷۹، کالای روتین (Part.DesignTypeIsRoutine مثل HTS) و بدون هیچ ردیف BOM.
		/// درج از طریق ProductionOrderItemBomService: ردیف اصلی (نسخهٔ ۰) + رخداد «ایجاد» در تاریخچه.
		/// </summary>
		[JobHandler("درج خودکار BOM روتین برای اقلام سفارش ساخت بدون BOM")]
		public async Task AddRoutineBomListTask(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع درج BOM روتین برای اقلام بدون BOM", cn);

				var industrialStatuses = ProductionOrderItemWorkflowRules.IndustrialDashboardStatuses;

				var candidates = await unitOfWork.Repository<ProductionOrderItem>()
					.TableNoTracking
					.Where(p => p.IsLatestVersion
						&& !p.IsDeleted
						&& p.IsActive != IsActiveEnum.Deleted
						&& p.PartId != null
						&& p.CheckStatus != null
						&& industrialStatuses.Contains(p.CheckStatus.Value)
						&& p.Status == ProductionOrderItemStatusEnum.NotCompleted
						&& p.Part != null
						&& p.Part.DesignTypeIsRoutine == true
						&& !p.ProductionOrderItemBom.Any())
					.Select(p => new { Id = p.Id!.Value, PartId = p.PartId!.Value })
					.ToListAsync(cn);

				if (!candidates.Any())
				{
					await jobLogger?.LogInfoAsync("قلم روتین بدون BOM یافت نشد", cn);
					return;
				}

				var partIds = candidates.Select(c => c.PartId).Distinct().ToList();
				var templates = (await appContext.vw_ProductItems
						.AsNoTracking()
						.Where(v => partIds.Contains(v.PartId) && v.PartItemId > 0)
						.Select(v => new { v.PartId, v.PartItemId, v.UsingRate })
						.ToListAsync(cn))
					.GroupBy(v => v.PartId)
					.ToDictionary(g => g.Key, g => g
						.GroupBy(x => x.PartItemId)
						.Select(x => new { PartItemId = x.Key, Amount = x.Sum(y => y.UsingRate) })
						.ToList());

				var newBoms = new List<NewBomLine>();
				var itemsWithBom = 0;
				foreach (var candidate in candidates)
				{
					if (!templates.TryGetValue(candidate.PartId, out var rows) || rows.Count == 0)
						continue;

					itemsWithBom++;
					newBoms.AddRange(rows.Select(r => new NewBomLine
					{
						ProductionOrderItemId = candidate.Id,
						PartId = r.PartItemId,
						Amount = r.Amount,
						Description = "افزوده شده از BOM روتین محصول (خودکار)",
						CreatedById = 1,
						CreatedByName = "سیستم (درج خودکار BOM روتین)"
					}));
				}

				if (newBoms.Any())
				{
					try
					{
						await bomService.CreateLinesAsync(newBoms, cn);
					}
					catch (Exception ex) when (ProductionOrderItemBomService.IsDuplicateKeyException(ex))
					{
						// کاربر هم‌زمان BOM قلم را ساخته است؛ اجرای بعدی جاب این قلم را دیگر کاندید نمی‌کند
						await jobLogger?.LogInfoAsync("درج BOM روتین به‌خاطر ردیف تکراری (ثبت هم‌زمان کاربر) رد شد", cn);
						return;
					}
				}

				await jobLogger?.LogInfoAsync($"برای {itemsWithBom} قلم روتین، {newBoms.Count} ردیف BOM از الگوی محصول درج شد ({candidates.Count - itemsWithBom} قلم الگو نداشتند)", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// One-time (re-runnable) cutover sync from old HTS → App ProductionOrder / ProductionOrderItem.
		/// Overwrites App with HTS when HTS has a value (status, steps, key dates/text, serial/planning),
		/// then upserts header/item comments by HtsId.
		/// Not bi-directional; not continuous sync.
		/// </summary>
		[JobHandler("کات‌اور یک‌باره سفارش ساخت از HTS (وضعیت، فیلدهای عملیاتی و کامنت‌ها)")]
		public async Task SyncProductionOrderOperationalFieldsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع کات‌اور یک‌باره سفارش ساخت از HTS (وضعیت + فیلدهای عملیاتی + کامنت‌ها)", cn);

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

				// کلید اصلی تطبیق با HTS: App.RahkaranHistoryId == HTS.RahkaranId (== History.Id)
				var itemsByRahkaranHistoryId = appItems
					.Where(i => i.RahkaranHistoryId.HasValue)
					.GroupBy(i => i.RahkaranHistoryId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				// fallback فقط برای اقلامی که هنوز RahkaranHistoryId ندارند
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
					$"بارگذاری انجام شد: {productionOrderByNumber.Count} سفارش، {appItems.Count} قلم ({itemsByRahkaranHistoryId.Count} با RahkaranHistoryId)، {appUsersByUsername.Count} کاربر برای نگاشت",
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

						// اولویت ۱: HTS.RahkaranId == App.RahkaranHistoryId
						if (htsItem.RahkaranId.HasValue)
							itemsByRahkaranHistoryId.TryGetValue(htsItem.RahkaranId.Value, out appItem);

						// اولویت ۲: Number + PartCode — فقط وقتی هنوز HistoryId ست نشده
						if (appItem == null
							&& htsItem.ProductionOrderNumber.HasValue
							&& !string.IsNullOrWhiteSpace(htsItem.PartCode)
							&& productionOrderByNumber.TryGetValue(htsItem.ProductionOrderNumber.Value, out var matchedPo)
							&& matchedPo.Id.HasValue)
						{
							var key = $"{matchedPo.Id}_{htsItem.PartCode}";
							if (itemsByPoAndPartCode.TryGetValue(key, out var byPart)
								&& !byPart.RahkaranHistoryId.HasValue)
							{
								appItem = byPart;
							}
						}

						if (appItem == null)
						{
							totalUnmatched++;
							batchUnmatched++;
							if (batchUnmatched <= 5)
							{
								await jobLogger?.LogWarningAsync(
									$"قلم HTS بدون متناظر در App: PO={htsItem.ProductionOrderNumber}، Part={htsItem.PartCode}، HTS.RahkaranId={htsItem.RahkaranId}",
									0,
									cn);
							}
							continue;
						}

						totalMatched++;
						batchMatched++;

						var changed = ApplyHtsCutoverToItem(appItem, htsItem, appUsersByUsername, out var userMapMiss);
						// بعد از کات‌اور، دیکشنری HistoryId را برای همگام‌سازی کامنت‌ها به‌روز نگه دار
						if (appItem.RahkaranHistoryId.HasValue)
							itemsByRahkaranHistoryId[appItem.RahkaranHistoryId.Value] = appItem;

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

				await SyncProductionOrderCommentsFromHts(
					productionOrderByNumber,
					itemsByRahkaranHistoryId,
					itemsByPoAndPartCode,
					appUsersByUsername,
					batchSize,
					jobLogger,
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// همگام‌سازی کامنت‌های هدر و قلم سفارش ساخت از HTS.
		/// با HtsId یکتا است؛ رکوردهای موجود بروزرسانی و جدیدها درج می‌شوند.
		/// از AddRangeAsync استفاده می‌شود تا EntityAction وضعیت والد را بازنویسی نکند.
		/// </summary>
		private async Task SyncProductionOrderCommentsFromHts(
			Dictionary<long, ProductionOrder> productionOrderByNumber,
			Dictionary<long, ProductionOrderItem> itemsByRahkaranHistoryId,
			Dictionary<string, ProductionOrderItem> itemsByPoAndPartCode,
			IReadOnlyDictionary<string, long> appUsersByUsername,
			int batchSize,
			IJobLogger? jobLogger,
			CancellationToken cn)
		{
			await jobLogger?.LogInfoAsync("شروع همگام‌سازی کامنت‌های سفارش ساخت از HTS", cn);

			var existingHeaderComments = await unitOfWork.Repository<ProductionOrderComment>()
				.Table
				.Where(c => c.HtsId != 0)
				.ToListAsync(cn);
			var headerByHtsId = existingHeaderComments
				.GroupBy(c => c.HtsId)
				.ToDictionary(g => g.Key, g => g.First());

			var existingItemComments = await unitOfWork.Repository<ProductionOrderItemComment>()
				.Table
				.Where(c => c.HtsId != 0)
				.ToListAsync(cn);
			var itemCommentByHtsId = existingItemComments
				.GroupBy(c => c.HtsId)
				.ToDictionary(g => g.Key, g => g.First());

			var productionOrderNumbers = productionOrderByNumber.Keys.OrderBy(x => x).ToList();
			int headerInserted = 0, headerUpdated = 0, headerUnmatched = 0;
			int itemInserted = 0, itemUpdated = 0, itemUnmatched = 0, itemUserMapMiss = 0;

			for (var skip = 0; skip < productionOrderNumbers.Count; skip += batchSize)
			{
				var batchNumbers = productionOrderNumbers.Skip(skip).Take(batchSize).ToList();
				var numberParams = string.Join(",", batchNumbers.Select((_, i) => $"@p{i}"));
				var parameters = batchNumbers
					.Select((num, i) => new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", (int)num))
					.ToArray();

				var headerSql = $@"
					SELECT
						c.Id,
						c.ProductionOrderId,
						c.StatusId,
						c.CreatedUserId,
						c.CreatedDate,
						c.CreatedDateInText,
						c.Comment,
						po.Number AS ProductionOrderNumber,
						u.Username AS CreatedUserUsername
					FROM Pln_ProductionOrderComment c (NOLOCK)
						INNER JOIN Pln_ProductionOrder po (NOLOCK) ON c.ProductionOrderId = po.Id
						LEFT JOIN Gnr_User u (NOLOCK) ON c.CreatedUserId = u.User_ID
					WHERE po.IsLatestVersion = 1
						AND po.Number IN ({numberParams})";

				var htsHeaderComments = await Hdb.Hts_Pln_ProductionOrderComments
					.FromSqlRaw(headerSql, parameters)
					.AsNoTracking()
					.ToListAsync(cn);

				var newHeaderComments = new List<ProductionOrderComment>();
				var lastStateByAppPoId = new Dictionary<long, ProductionOrderStateEnum?>();
				foreach (var htsComment in htsHeaderComments
					.OrderBy(c => c.CreatedDate)
					.ThenBy(c => c.Id))
				{
					if (!htsComment.ProductionOrderNumber.HasValue
						|| !productionOrderByNumber.TryGetValue(htsComment.ProductionOrderNumber.Value, out var header)
						|| !header.Id.HasValue)
					{
						headerUnmatched++;
						continue;
					}

					var appPoId = header.Id.Value;
					var createdById = ResolveAppUserId(htsComment.CreatedUserUsername, appUsersByUsername);
					var newState = MapProductionOrderStateFromHts(htsComment.StatusId);
					lastStateByAppPoId.TryGetValue(appPoId, out var previousState);
					var createdOn = htsComment.CreatedDate;
					var createdOnShamsi = !string.IsNullOrWhiteSpace(htsComment.CreatedDateInText)
						? htsComment.CreatedDateInText
						: createdOn.ToShamsiDateTime();

					if (headerByHtsId.TryGetValue(htsComment.Id, out var existing))
					{
						var changed = false;
						if (existing.ProductionOrderId != appPoId)
						{
							existing.ProductionOrderId = appPoId;
							changed = true;
						}
						if (existing.PreviousState != previousState)
						{
							existing.PreviousState = previousState;
							changed = true;
						}
						if (existing.NewState != newState)
						{
							existing.NewState = newState;
							changed = true;
						}
						if (!string.Equals(existing.Comment, htsComment.Comment, StringComparison.Ordinal))
						{
							existing.Comment = htsComment.Comment;
							changed = true;
						}
						if (existing.CreatedById != createdById)
						{
							existing.CreatedById = createdById;
							changed = true;
						}
						if (existing.CreatedOnMiladiDateTime != createdOn)
						{
							existing.CreatedOnMiladiDateTime = createdOn;
							existing.CreatedOnShamsiDateTime = createdOnShamsi;
							changed = true;
						}
						if (changed)
							headerUpdated++;
					}
					else
					{
						var entity = new ProductionOrderComment
						{
							ProductionOrderId = appPoId,
							PreviousState = previousState,
							NewState = newState,
							Comment = htsComment.Comment,
							CreatedById = createdById,
							CreatedOnMiladiDateTime = createdOn,
							CreatedOnShamsiDateTime = createdOnShamsi,
							IsActive = IsActiveEnum.Active,
							HtsId = htsComment.Id
						};
						newHeaderComments.Add(entity);
						headerByHtsId[htsComment.Id] = entity;
						headerInserted++;
					}

					if (newState.HasValue)
						lastStateByAppPoId[appPoId] = newState;
				}

				if (newHeaderComments.Count > 0)
					await unitOfWork.Repository<ProductionOrderComment>().AddRangeAsync(newHeaderComments, cn, false);

				var itemSql = $@"
					SELECT
						c.Id,
						c.ProductionOrderItemId,
						c.StatusId,
						c.CommentDate,
						c.CommentDateInText,
						c.CommentEndDate,
						c.CommentEndDateInText,
						c.CommentStartTime,
						c.CommentEndTime,
						c.FailureTypeIds,
						c.FailureTypeInText,
						c.IsForProductionMode,
						c.CreatedUserId,
						c.CreatedDate,
						c.CreatedDateInText,
						c.Comment,
						c.CcReciversIds,
						c.CcReciversInText,
						c.StopRequestId,
						c.IsForProductionStepStatus,
						po.Number AS ProductionOrderNumber,
						p.Part_Code AS PartCode,
						poi.RahkaranId AS ItemRahkaranId,
						u.Username AS CreatedUserUsername
					FROM Pln_ProductionOrder_Itm_New_Comment c (NOLOCK)
						INNER JOIN Pln_ProductionOrderItem poi (NOLOCK) ON c.ProductionOrderItemId = poi.Id
						INNER JOIN Pln_ProductionOrder po (NOLOCK) ON poi.ProductionOrderId = po.Id
						LEFT JOIN Inv_Part p (NOLOCK) ON poi.PartId = p.Part_ID
						LEFT JOIN Gnr_User u (NOLOCK) ON c.CreatedUserId = u.User_ID
					WHERE po.IsLatestVersion = 1
						AND poi.IsLatestVersion = 1
						AND poi.IsDeleted = 0
						AND c.ProductionOrderItemId IS NOT NULL
						AND po.Number IN ({numberParams})";

				var itemParameters = batchNumbers
					.Select((num, i) => new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", (int)num))
					.ToArray();

				var htsItemComments = await Hdb.Hts_Pln_ProductionOrderItemComments
					.FromSqlRaw(itemSql, itemParameters)
					.AsNoTracking()
					.ToListAsync(cn);

				var newItemComments = new List<ProductionOrderItemComment>();
				foreach (var htsComment in htsItemComments)
				{
					ProductionOrderItem? appItem = null;

					// HTS.ItemRahkaranId == App.RahkaranHistoryId
					if (htsComment.ItemRahkaranId.HasValue)
						itemsByRahkaranHistoryId.TryGetValue(htsComment.ItemRahkaranId.Value, out appItem);

					// fallback: Number + PartCode فقط وقتی HistoryId هنوز خالی است
					if (appItem == null
						&& htsComment.ProductionOrderNumber.HasValue
						&& !string.IsNullOrWhiteSpace(htsComment.PartCode)
						&& productionOrderByNumber.TryGetValue(htsComment.ProductionOrderNumber.Value, out var matchedPo)
						&& matchedPo.Id.HasValue)
					{
						var key = $"{matchedPo.Id}_{htsComment.PartCode}";
						if (itemsByPoAndPartCode.TryGetValue(key, out var byPart)
							&& !byPart.RahkaranHistoryId.HasValue)
						{
							appItem = byPart;
						}
					}

					if (appItem?.Id == null)
					{
						itemUnmatched++;
						continue;
					}

					var createdById = ResolveAppUserId(htsComment.CreatedUserUsername, appUsersByUsername);
					if (!string.IsNullOrWhiteSpace(htsComment.CreatedUserUsername)
						&& createdById == 1
						&& !string.Equals(htsComment.CreatedUserUsername.Trim(), "a", StringComparison.OrdinalIgnoreCase))
					{
						itemUserMapMiss++;
					}

					MapItemCommentStatuses(
						htsComment,
						out var productionStatus,
						out var productionStep);

					var startMiladi = CombineHtsDateAndTime(htsComment.CommentDate, htsComment.CommentStartTime)
						?? htsComment.CreatedDate;
					var startShamsi = !string.IsNullOrWhiteSpace(htsComment.CommentDateInText)
						? AppendTimeToShamsi(htsComment.CommentDateInText, htsComment.CommentStartTime)
						: startMiladi.ToShamsiDateTime();
					var endMiladi = CombineHtsDateAndTime(htsComment.CommentEndDate, htsComment.CommentEndTime);
					var endShamsi = endMiladi.HasValue
						? (!string.IsNullOrWhiteSpace(htsComment.CommentEndDateInText)
							? AppendTimeToShamsi(htsComment.CommentEndDateInText, htsComment.CommentEndTime)
							: endMiladi.Value.ToShamsiDateTime())
						: null;
					var createdOnShamsi = !string.IsNullOrWhiteSpace(htsComment.CreatedDateInText)
						? htsComment.CreatedDateInText
						: htsComment.CreatedDate.ToShamsiDateTime();

					if (itemCommentByHtsId.TryGetValue(htsComment.Id, out var existing))
					{
						var changed = ApplyHtsItemCommentFields(
							existing,
							appItem.Id.Value,
							htsComment,
							productionStatus,
							productionStep,
							createdById,
							startMiladi,
							startShamsi,
							endMiladi,
							endShamsi,
							createdOnShamsi);
						if (changed)
							itemUpdated++;
					}
					else
					{
						var entity = new ProductionOrderItemComment
						{
							ProductionOrderItemId = appItem.Id.Value,
							ProductionStatus = productionStatus,
							ProductionStep = productionStep,
							StartMiladiDateTime = startMiladi,
							StartShamsiDate = startShamsi,
							EndMiladiDateTime = endMiladi,
							EndShamsiDate = endShamsi,
							FailureTypeIds = htsComment.FailureTypeIds,
							FailureTypeNames = htsComment.FailureTypeInText,
							Comment = htsComment.Comment,
							CcReciversIds = htsComment.CcReciversIds,
							CcReciversNames = htsComment.CcReciversInText,
							IsForProductionMode = htsComment.IsForProductionMode,
							IsForProductionStepStatus = htsComment.IsForProductionStepStatus,
							CreatedById = createdById,
							CreatedOnMiladiDateTime = htsComment.CreatedDate,
							CreatedOnShamsiDateTime = createdOnShamsi,
							IsActive = IsActiveEnum.Active,
							HtsId = htsComment.Id
						};
						newItemComments.Add(entity);
						itemCommentByHtsId[htsComment.Id] = entity;
						itemInserted++;
					}
				}

				if (newItemComments.Count > 0)
					await unitOfWork.Repository<ProductionOrderItemComment>().AddRangeAsync(newItemComments, cn, false);

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"comment batch {skip / batchSize + 1}: header(hts={htsHeaderComments.Count}, +{newHeaderComments.Count}) item(hts={htsItemComments.Count}, +{newItemComments.Count})",
					cn);
			}

			await jobLogger?.LogInfoAsync(
				$"همگام‌سازی کامنت‌ها تمام شد. header(inserted={headerInserted}, updated={headerUpdated}, unmatched={headerUnmatched}) item(inserted={itemInserted}, updated={itemUpdated}, unmatched={itemUnmatched}, userMapMiss={itemUserMapMiss})",
				cn);
		}

		private static long ResolveAppUserId(string? htsUsername, IReadOnlyDictionary<string, long> appUsersByUsername)
		{
			if (!string.IsNullOrWhiteSpace(htsUsername)
				&& appUsersByUsername.TryGetValue(htsUsername.Trim(), out var userId))
				return userId;

			return 1;
		}

		/// <summary>
		/// وضعیت بررسی مالی (۲) و تایید مالی (۳) را جاب SQL روی Erps با کامنت و ایمیل روی هدر می‌نویسد.
		/// اگر این جاب همان مقدار را زودتر بنویسد، شرط نابرابری State ایمیل را حذف می‌کند.
		/// </summary>
		private static bool IsFinancialStateOwnedByRahkaranEmailJob(ProductionOrderStateEnum state)
			=> state is ProductionOrderStateEnum.FinancialUnitReview or ProductionOrderStateEnum.FinancialApproval;

		/// <summary>معادل SQL: CAST(Version AS BIGINT) برای timestamp/rowversion راهکاران.</summary>
		private static long? ToRahkaranVersionLong(byte[]? version)
		{
			if (version == null || version.Length == 0)
				return null;

			ulong value = 0;
			foreach (var b in version)
				value = (value << 8) | b;

			return unchecked((long)value);
		}

		private static ProductionOrderStateEnum? MapProductionOrderStateFromHts(short statusId)
		{
			return statusId switch
			{
				2055 => ProductionOrderStateEnum.Submit,                 // ایجاد شده
				2056 => ProductionOrderStateEnum.Submit,                 // ویرایش شده
				2436 => ProductionOrderStateEnum.Submit,                 // خوانده شده از راهکاران
				2084 => ProductionOrderStateEnum.FinancialUnitReview,    // در انتظار تایید مالی / صدور سند همکاران
				2057 => ProductionOrderStateEnum.FinancialApproval,      // تایید مالی
				2071 => ProductionOrderStateEnum.FinancialApproval,      // تایید مالی و آغاز فرآیند ساخت
				2207 => ProductionOrderStateEnum.Obsolete,               // منسوخ شده
				_ => null
			};
		}

		private static void MapItemCommentStatuses(
			Hts_Pln_ProductionOrderItemComment htsComment,
			out ProductionOrderItemProductionStatusEnum productionStatus,
			out ProductionOrderItemProductionStepEnum? productionStep)
		{
			productionStep = null;
			productionStatus = ProductionOrderItemProductionStatusEnum.NotDetermined;

			if (htsComment.IsForProductionStepStatus
				&& TryMapEnum(htsComment.StatusId, out ProductionOrderItemProductionStepEnum step))
			{
				productionStep = step;
				return;
			}

			if (TryMapCheckStatusFromHts(htsComment.StatusId, out var checkStatus))
			{
				productionStatus = (ProductionOrderItemProductionStatusEnum)(int)checkStatus;
				return;
			}

			if (TryMapEnum(htsComment.StatusId, out ProductionOrderItemProductionStatusEnum status))
			{
				productionStatus = status;
				return;
			}

			// مقدار ناشناخته را به‌صورت خام نگه می‌داریم تا تاریخچه از بین نرود
			productionStatus = (ProductionOrderItemProductionStatusEnum)(int)htsComment.StatusId;
		}

		private static bool ApplyHtsItemCommentFields(
			ProductionOrderItemComment existing,
			long productionOrderItemId,
			Hts_Pln_ProductionOrderItemComment htsComment,
			ProductionOrderItemProductionStatusEnum productionStatus,
			ProductionOrderItemProductionStepEnum? productionStep,
			long createdById,
			DateTime startMiladi,
			string? startShamsi,
			DateTime? endMiladi,
			string? endShamsi,
			string? createdOnShamsi)
		{
			var changed = false;

			if (existing.ProductionOrderItemId != productionOrderItemId)
			{
				existing.ProductionOrderItemId = productionOrderItemId;
				changed = true;
			}
			if (existing.ProductionStatus != productionStatus)
			{
				existing.ProductionStatus = productionStatus;
				changed = true;
			}
			if (existing.ProductionStep != productionStep)
			{
				existing.ProductionStep = productionStep;
				changed = true;
			}
			if (existing.StartMiladiDateTime != startMiladi)
			{
				existing.StartMiladiDateTime = startMiladi;
				existing.StartShamsiDate = startShamsi;
				changed = true;
			}
			if (existing.EndMiladiDateTime != endMiladi)
			{
				existing.EndMiladiDateTime = endMiladi;
				existing.EndShamsiDate = endShamsi;
				changed = true;
			}
			if (!string.Equals(existing.FailureTypeIds, htsComment.FailureTypeIds, StringComparison.Ordinal))
			{
				existing.FailureTypeIds = htsComment.FailureTypeIds;
				changed = true;
			}
			if (!string.Equals(existing.FailureTypeNames, htsComment.FailureTypeInText, StringComparison.Ordinal))
			{
				existing.FailureTypeNames = htsComment.FailureTypeInText;
				changed = true;
			}
			if (!string.Equals(existing.Comment, htsComment.Comment, StringComparison.Ordinal))
			{
				existing.Comment = htsComment.Comment;
				changed = true;
			}
			if (!string.Equals(existing.CcReciversIds, htsComment.CcReciversIds, StringComparison.Ordinal))
			{
				existing.CcReciversIds = htsComment.CcReciversIds;
				changed = true;
			}
			if (!string.Equals(existing.CcReciversNames, htsComment.CcReciversInText, StringComparison.Ordinal))
			{
				existing.CcReciversNames = htsComment.CcReciversInText;
				changed = true;
			}
			if (existing.IsForProductionMode != htsComment.IsForProductionMode)
			{
				existing.IsForProductionMode = htsComment.IsForProductionMode;
				changed = true;
			}
			if (existing.IsForProductionStepStatus != htsComment.IsForProductionStepStatus)
			{
				existing.IsForProductionStepStatus = htsComment.IsForProductionStepStatus;
				changed = true;
			}
			if (existing.CreatedById != createdById)
			{
				existing.CreatedById = createdById;
				changed = true;
			}
			if (existing.CreatedOnMiladiDateTime != htsComment.CreatedDate)
			{
				existing.CreatedOnMiladiDateTime = htsComment.CreatedDate;
				existing.CreatedOnShamsiDateTime = createdOnShamsi;
				changed = true;
			}

			return changed;
		}

		private static DateTime? CombineHtsDateAndTime(DateTime? date, string? time)
		{
			if (!date.HasValue)
				return null;

			if (string.IsNullOrWhiteSpace(time) || !TimeSpan.TryParse(time.Trim(), out var ts))
				return date.Value.Date;

			return date.Value.Date.Add(ts);
		}

		private static string AppendTimeToShamsi(string shamsiDate, string? time)
		{
			if (string.IsNullOrWhiteSpace(time))
				return shamsiDate;

			return $"{shamsiDate.Trim()} {time.Trim()}";
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

			if (htsItem.RahkaranId.HasValue && appItem.RahkaranHistoryId != htsItem.RahkaranId)
			{
				appItem.RahkaranHistoryId = htsItem.RahkaranId;
				changed = true;
			}

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
				// مثل HTS: IsRoutine همان فلگ قلم است که موقع درج از DesignTypeIsRoutine کالا آمده.
				var isRoutine = p.IsRoutine;

				ProductionOrderItemCheckStatusEnum newStatus;

				if (isRoutine || (p.DeviceType.HasValue && specialStatusIds.Contains(p.DeviceType)))
				{
					// معادل HTS: فقط BuyStatus = صنایع داخلی. مرحله ساخت ست نمی‌شود.
					newStatus = ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal;
				}
				else if (p.ProductionOrder?.ProjectManagerId.HasValue == true)
					newStatus = ProductionOrderItemCheckStatusEnum.AwaitingProjectManagerApproval;
				else
					newStatus = ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;

				p.CheckStatus = newStatus;
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
		/// تشخیص رشته مهندسی (برق/مکانیک) از منبع مشترک با ProductionOrderItemController:
		/// Entities.App.Sale.ProductionOrderItemWorkflowRules.ElectricalDeviceTypes
		/// </summary>
		private static HashSet<ProductionOrderItemDeviceTypeEnum> ElectricalDeviceTypes => ProductionOrderItemWorkflowRules.ElectricalDeviceTypes;

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

		/// <summary>
		/// معادل HTS SendInitilizeEmailNotification بعد از انتقال قلم معطل مدیر پروژه.
		/// گیرنده گروه صنایع است؛ مدیر پروژه، فروش و مهندسی در رونوشت‌اند.
		/// </summary>
		private async Task SendHoldedItemsIndustrialNotificationAsync(List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var industrialEmails = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork,
					ProductionOrderItemNotificationHelper.GroupIndustrial,
					cn);
				var engineeringEmails = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork,
					ProductionOrderItemNotificationHelper.GroupBomEngineering,
					cn);

				var sent = 0;
				foreach (var item in items)
				{
					var order = item.ProductionOrder;
					var ccIds = new List<long>();
					if (item.CreatedById is > 0)
						ccIds.Add(item.CreatedById.Value);
					if (order?.SalesExpertId is > 0)
						ccIds.Add(order.SalesExpertId.Value);
					if (order?.AlternativeExpertId is > 0)
						ccIds.Add(order.AlternativeExpertId.Value);
					if (order?.SalesManagerId is > 0)
						ccIds.Add(order.SalesManagerId.Value);
					if (order?.ProjectManagerId is > 0)
						ccIds.Add(order.ProjectManagerId.Value);

					var cc = await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(appContext, ccIds, cn);
					cc.AddRange(engineeringEmails);

					var orderNumber = order?.ProductionOrderNumber?.ToString() ?? order?.Number ?? "-";
					var body = new StringBuilder();
					body.Append("بدینوسیله اعلام میگردد قلم ");
					body.Append($"سفارش ساخت شماره {orderNumber} و کد محصول {item.Part?.Code} بعلت عدم رسیدگی در زمان مقرر (توسط مدیر پروژه) ، بصورت خودکار <span style='color:red'>تعیین تکلیف</span> گردید.");
					body.Append("<br />لذا خواهشمند است نسبت به بررسی و انجام اقدامات لازم مساعدت فرمایید.<br /><br /><ul>");
					body.Append(ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", orderNumber));
					body.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", item.Part?.Code));
					body.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name));
					body.Append(ProductionOrderItemNotificationHelper.Li("سریال", item.Serial));
					body.Append(ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item.Amount));
					body.Append("</ul>");

					var queued = await ProductionOrderItemNotificationHelper.QueueEmailAsync(
						unitOfWork,
						$"اعلان [تغییر وضعیت اتوماتیک] در قلم سفارش ساخت {orderNumber}",
						ProductionOrderItemNotificationHelper.BuildEmailShell(body.ToString()),
						industrialEmails,
						cc,
						item.Id,
						1,
						$"Panel/ProductionOrderItem/Edit/{item.Id}",
						cn,
						saveNow: false);
					if (queued)
						sent++;
				}

				if (sent > 0)
					await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"اعلان صنایع برای {sent} قلم معطل‌مانده در کارتابل مدیر پروژه صف شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// معادل HTS EditItemStatusAndSendNotification برای مسیر صنایع داخلی (۱۹۰۴).
		/// گیرنده اصلی گروه صنایع است؛ گروه افزودن قلم، ایجادکننده، فروش و مدیر پروژه در رونوشت‌اند.
		/// پیوست، PDF همان قالب چاپی ProductionOrderForm است.
		/// </summary>
		private async Task SendIndustrialCartableNotificationAsync(List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var industrialEmails = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork,
					ProductionOrderItemNotificationHelper.GroupIndustrial,
					cn);
				var itemAddedEmails = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(
					unitOfWork,
					ProductionOrderItemNotificationHelper.GroupItemAdded,
					cn);

				var sent = 0;
				foreach (var group in items.Where(p => p.ProductionOrderId.HasValue).GroupBy(p => p.ProductionOrderId!.Value))
				{
					var order = group.First().ProductionOrder;
					var ccIds = new List<long>();
					if (order?.CreatedById is > 0)
						ccIds.Add(order.CreatedById.Value);
					if (order?.SalesExpertId is > 0)
						ccIds.Add(order.SalesExpertId.Value);
					if (order?.AlternativeExpertId is > 0)
						ccIds.Add(order.AlternativeExpertId.Value);
					if (order?.SalesManagerId is > 0)
						ccIds.Add(order.SalesManagerId.Value);
					if (order?.ProjectManagerId is > 0)
						ccIds.Add(order.ProjectManagerId.Value);

					var cc = await ProductionOrderItemNotificationHelper.GetUserEmailsAsync(appContext, ccIds, cn);
					cc.AddRange(itemAddedEmails);
					var body = new StringBuilder();
					body.Append("بدینوسیله اعلام میگردد سفارش ساختی با مشخصات و اقلام ذیل جهت بهره برداری در سامانه بروزرسانی گردید<br /><br /><ul>");
					body.Append(ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", order?.ProductionOrderNumber ?? (object?)order?.Number));
					body.Append(ProductionOrderItemNotificationHelper.Li("نسخه", order?.Revision));
					body.Append("<li style='margin-top:15px'>اقلام</li>");
					foreach (var item in group)
					{
						body.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", item.Part?.Code));
						body.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", item.Part?.Name));
						body.Append(ProductionOrderItemNotificationHelper.Li("تعداد/مقدار", item.Amount));
					}
					body.Append("</ul>");

					var attachmentIds = await TryCreateProductionOrderFormAttachmentAsync(order, jobLogger, cn);
					var queued = await ProductionOrderItemNotificationHelper.QueueEmailAsync(
						unitOfWork,
						"اعلان افزودن قلم/اقلام سفارش ساخت",
						ProductionOrderItemNotificationHelper.BuildEmailShell(body.ToString()),
						industrialEmails,
						cc,
						group.First().Id,
						1,
						$"Panel/ProductionOrderItem/Edit/{group.First().Id}",
						cn,
						saveNow: false,
						attachmentIds);
					if (queued)
						sent++;
				}

				if (sent > 0)
					await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"اعلان صنایع داخلی برای {sent} سفارش ({items.Count} قلم) صف شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// PDF فرم چاپی سفارش ساخت را مثل پیوست HTS می‌سازد. شکست ساخت فایل، ارسال ایمیل را متوقف نمی‌کند.
		/// </summary>
		private async Task<List<long>?> TryCreateProductionOrderFormAttachmentAsync(ProductionOrder? order, IJobLogger? jobLogger, CancellationToken cn)
		{
			if (order?.Id is not > 0)
				return null;

			try
			{
				var pdf = await productionOrderFormPdf.TryExportAsync(order.Id.Value, cn);
				if (pdf is not { Length: > 0 })
				{
					await jobLogger?.LogWarningAsync($"قالب چاپی سفارش ساخت {order.Id} برای پیوست ایمیل در دسترس نبود", 0, cn);
					return null;
				}

				var orderNumber = order.ProductionOrderNumber?.ToString();
				if (string.IsNullOrWhiteSpace(orderNumber))
					orderNumber = string.IsNullOrWhiteSpace(order.Number) ? order.Id.ToString() : order.Number;

				var file = await fileService.UploadAsync(
					pdf,
					$"سفارش ساخت_{orderNumber}.pdf",
					"application/pdf",
					typeof(ProductionOrder).FullName,
					"ProductionOrderForm",
					order.Id,
					cn);

				return file.Id is > 0 ? [file.Id.Value] : null;
			}
			catch (Exception ex)
			{
				await jobLogger?.LogWarningAsync($"پیوست چاپ سفارش ساخت {order.Id} ساخته نشد: {ex.Message}", 0, cn);
				return null;
			}
		}

		/// <summary>
		/// معادل HTS HtsTaskService.SendProductionOrderItemsWithNearDeliveryDateNotifications: یک ایمیل به‌ازای هر قلم برای
		/// گروه اعلان «نزدیک به موعد تحویل» (معادل گروه کاربری 567 در HTS).
		/// </summary>
		private async Task<int> SendNearDeliveryDateNotification(List<ProductionOrderItem> items, int interval, IJobLogger? jobLogger, CancellationToken cn)
		{
			var receivers = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupNearDeliveryDate, cn);
			if (!receivers.Any())
			{
				await jobLogger?.LogInfoAsync($"گروه اعلان {ProductionOrderItemNotificationHelper.GroupNearDeliveryDate} عضوی ندارد؛ هشدار نزدیک‌تحویل ارسال نشد", cn);
				return 0;
			}

			var sent = 0;
			foreach (var item in items)
			{
				var effectiveDate = item.AgreedDeliverDate == null ? item.StandardDeliveryMiladiDate
					: item.StandardDeliveryMiladiDate == null ? item.AgreedDeliverDate
					: item.AgreedDeliverDate > item.StandardDeliveryMiladiDate ? item.AgreedDeliverDate : item.StandardDeliveryMiladiDate;

				var body = ProductionOrderItemNotificationHelper.BuildEmailShell(
					$"احتراماَ کالای: <strong>{item.Part?.Name} | {item.Part?.Code}</strong> مندرج در سفارش ساخت شماره : <strong>{item.ProductionOrder?.ProductionOrderNumber ?? (object?)item.ProductionOrder?.Number}</strong>"
					+ $" با موعد تحویل : <strong>{interval}</strong> روز بعد ({effectiveDate?.ToShamsiDate()}) در وضعیت : <strong>{item.ProductionStep.ToDisplay()}</strong> قرار دارد."
					+ "<br />لطفا بررسی های لازم را انجام دهید.");

				if (await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, "هشدار نزدیک بودن زمان تحویل کالا", body,
						receivers, null, item.Id, 1, $"Panel/ProductionOrderItem/Edit/{item.Id}", cn, saveNow: false))
					sent++;
			}

			await unitOfWork.SaveChangesAsync(cn);
			return sent;
		}

		/// <summary>
		/// اعلان گروهی (گروه اعلان پنل) با فهرست اقلام — برای ارجاع‌های خودکار Job به صنایع.
		/// </summary>
		private async Task SendGroupNotificationAsync(string groupCode, string title, string intro, List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var receivers = await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, groupCode, cn);
				if (!receivers.Any())
				{
					await jobLogger?.LogInfoAsync($"گروه اعلان {groupCode} عضوی ندارد؛ اعلان «{title}» ارسال نشد", cn);
					return;
				}

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, title,
					ProductionOrderItemNotificationHelper.BuildEmailShell(intro + BuildItemsListHtml(items)),
					receivers, null, items.First().Id, 1, $"Panel/ProductionOrderItem/Edit/{items.First().Id}", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		/// <summary>
		/// اعلان به رئیس کمیته تامین (نقش) + کارشناسان کمیته (گروه اعلان) — معادل بخش WaitingToCheckingSupplyCommitteeBoss در HTS.
		/// </summary>
		private async Task SendSupplyCommitteeNotificationAsync(string title, string intro, List<ProductionOrderItem> items, IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var receivers = await ProductionOrderItemNotificationHelper.GetRoleEmailsAsync(appContext, "Sale.ProductionOrderItem.SupplyCommitteeBoss", cn);
				receivers.AddRange(await ProductionOrderItemNotificationHelper.GetGroupEmailsAsync(unitOfWork, ProductionOrderItemNotificationHelper.GroupSupplyCommitteeExperts, cn));
				if (!receivers.Any())
				{
					await jobLogger?.LogInfoAsync("کاربری با نقش رئیس کمیته تامین یافت نشد؛ اعلان کمیته ارسال نشد", cn);
					return;
				}

				await ProductionOrderItemNotificationHelper.QueueEmailAsync(unitOfWork, title,
					ProductionOrderItemNotificationHelper.BuildEmailShell(intro + BuildItemsListHtml(items)),
					receivers, null, items.First().Id, 1, $"Panel/ProductionOrderItem/Edit/{items.First().Id}", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
			}
		}

		private static string BuildItemsListHtml(List<ProductionOrderItem> items)
		{
			var sb = new StringBuilder();
			sb.Append("<br /><br /><ul>");
			foreach (var item in items)
			{
				sb.Append($"<li>سفارش ساخت <strong>{item.ProductionOrder?.ProductionOrderNumber ?? (object?)item.ProductionOrder?.Number ?? "-"}</strong>");
				sb.Append($" - کالا <strong>{item.Part?.Code} | {item.Part?.Name}</strong> - تعداد/مقدار <strong>{item.Amount}</strong> - نسخه <strong>{item.Revision}</strong></li>");
			}
			sb.Append("</ul>");
			return sb.ToString();
		}




	}

}

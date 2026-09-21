using App.BackgroundJob.Jobs;
using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Base;
using Entities.Rahkaran.USR3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Sls
{
	public class RequirmentAdvertiseJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن اعلام نیاز مندی ها از راهکاران")]
		public async Task AddRequirmentAdvertiseFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی اعلام نیازمندی‌ها از راهکاران", cn);

				// همگام‌سازی RequirmentAdvertise (جدول والد)
				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های اعلام نیازمندی از راهکاران...", cn);
				var rahkaranRequirmentAdvertises = await Rdb.Sale_RequirmentAdvertise
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranRequirmentAdvertises.Count} رکورد اعلام نیازمندی در راهکاران یافت شد", cn);

				// بارگذاری entity های مرتبط برای نگاشت
				await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط...", cn);
				var appParties = await unitOfWork.Repository<Party>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var partyMap = await JobLookup.ToUniqueValueMapAsync(
					appParties,
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Gnr.Party.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appParts = await unitOfWork.Repository<Part>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var partMap = await JobLookup.ToUniqueValueMapAsync(
					appParts,
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Inv.Part.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appPartyIndustryItems = await unitOfWork.Repository<PartyIndustryItem>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var partyIndustryItemMap = await JobLookup.ToUniqueValueMapAsync(
					appPartyIndustryItems,
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Gnr.PartyIndustryItem.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appOrgUnits = await unitOfWork.Repository<OrgUnit>()
					.Table
					.ToListAsync(cn);
				var orgUnitMap = await JobLookup.ToUniqueValueMapAsync(
					appOrgUnits,
					x => x.HamkaranUnitId,
					x => x.Id,
					jobLogger,
					"Hrm.OrgUnit.HamkaranUnitId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appRegions = await unitOfWork.Repository<Region>()
					.Table
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

				var appCustomers = await unitOfWork.Repository<Customer>()
					.Table
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

				await jobLogger?.LogInfoAsync("Entity های مرتبط بارگذاری شدند", cn);

				// بارگذاری RequirmentAdvertise های موجود
				var appRequirmentAdvertises = await unitOfWork.Repository<RequirmentAdvertise>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appRequirmentAdvertises.Count} رکورد اعلام نیازمندی در پایگاه داده برنامه موجود است", cn);

				var appRequirmentAdvertisesDict = await JobLookup.ToUniqueMapAsync(
					appRequirmentAdvertises.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					jobLogger,
					"Sls.RequirmentAdvertise.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var newRequirmentAdvertises = new List<RequirmentAdvertise>();
				int updatedRequirmentAdvertisesCount = 0;

				foreach (var rahkaranRA in rahkaranRequirmentAdvertises)
				{
					if (!appRequirmentAdvertisesDict.TryGetValue(rahkaranRA.Sale_RequirmentAdvertiseID, out var existRA))
					{
						// ایجاد رکورد جدید
						var newRA = new RequirmentAdvertise
						{
							HamkaranId = rahkaranRA.Sale_RequirmentAdvertiseID,
							RequestNumber = rahkaranRA.RequestNumber.HasValue ? (long?)rahkaranRA.RequestNumber.Value : null,
							Budget = rahkaranRA?.IsBudget ?? false,
							RepresentativeDetails = rahkaranRA.CustomerAgentInfo,
							RepresentativeContactNumber = rahkaranRA.CustomerAgentPhone,
							Description = rahkaranRA.Comments,
							PreInvoiceAmountRial = rahkaranRA.PreInvoiceAmountInRial.HasValue ? (long?)rahkaranRA.PreInvoiceAmountInRial.Value : null,
							PreInvoiceAmount = rahkaranRA.PreInvoiceAmountInForeignCurrency,
							PreInvoiceMiladiDate = rahkaranRA.PreInvoiceDate,
							Priority = rahkaranRA.Priority.HasValue ? (RequirementAdvertisePriorityEnum?)rahkaranRA.Priority.Value : null,
							ProjectFinancialValue = rahkaranRA.ProjectValueIdRef.HasValue ? (RequirmentAdvertiseProjectFinancialValueEnum?)rahkaranRA.ProjectValueIdRef.Value : null,
							EndUser = rahkaranRA.EndUser,
							Metric = rahkaranRA?.IsMetric ?? false,
							InquiryType = rahkaranRA.InquiryTypeIdRef.HasValue ? (RequirementAdvertiseInquiryTypeEnum?)rahkaranRA.InquiryTypeIdRef.Value : null
						};

						// نگاشت PartyIdRef → PersonOrCompanyId
						if (rahkaranRA.PartyIdRef.HasValue && partyMap.TryGetValue(rahkaranRA.PartyIdRef.Value, out var personOrCompanyId))
						{
							newRA.PersonOrCompanyId = personOrCompanyId;
						}

						// نگاشت SalesExpertIdRef → SalesExpertId
						if (rahkaranRA.SalesExpertIdRef.HasValue && partyMap.TryGetValue(rahkaranRA.SalesExpertIdRef.Value, out var salesExpertId))
						{
							newRA.SalesExpertId = salesExpertId;
						}

						// نگاشت SalesAdministratorRef → SalesSupervisorId
						if (rahkaranRA.SalesAdministratorRef.HasValue && partyMap.TryGetValue(rahkaranRA.SalesAdministratorRef.Value, out var salesSupervisorId))
						{
							newRA.SalesSupervisorId = salesSupervisorId;
						}

						// نگاشت PartyIndustryItemIdRef → SubIndustryId
						if (rahkaranRA.PartyIndustryItemIdRef.HasValue && partyIndustryItemMap.TryGetValue(rahkaranRA.PartyIndustryItemIdRef.Value, out var subIndustryId))
						{
							newRA.SubIndustryId = subIndustryId;
						}

						// نگاشت SalesAgencyIdRef → SalesRepresentativeId
						if (rahkaranRA.SalesAgencyIdRef.HasValue && customerMap.TryGetValue(rahkaranRA.SalesAgencyIdRef.Value, out var salesRepresentativeId))
						{
							newRA.SalesRepresentativeId = salesRepresentativeId;
						}

						// نگاشت IntroducerExpertRef → ReferrerExpertId
						if (rahkaranRA.IntroducerExpertRef.HasValue && partyMap.TryGetValue(rahkaranRA.IntroducerExpertRef.Value, out var referrerExpertId))
						{
							newRA.ReferrerExpertId = referrerExpertId;
						}

						// نگاشت InstallationCountryRef → CountryOfInstallationId
						if (rahkaranRA.InstallationCountryRef.HasValue && regionMap.TryGetValue(rahkaranRA.InstallationCountryRef.Value, out var countryId))
						{
							newRA.CountryOfInstallationId = countryId;
						}

						// نگاشت RegionalDivisionIdRef → InstallationProvinceId
						if (rahkaranRA.RegionalDivisionIdRef.HasValue && regionMap.TryGetValue(rahkaranRA.RegionalDivisionIdRef.Value, out var provinceId))
						{
							newRA.InstallationProvinceId = provinceId;
						}

						// نگاشت InstallationCityRef → InstallationCityId
						if (rahkaranRA.InstallationCityRef.HasValue && regionMap.TryGetValue(rahkaranRA.InstallationCityRef.Value, out var cityId))
						{
							newRA.InstallationCityId = cityId;
						}

						// نگاشت SaleDepartmentIdRef → OrganizationUnitId
						if (rahkaranRA.SaleDepartmentIdRef.HasValue && orgUnitMap.TryGetValue(rahkaranRA.SaleDepartmentIdRef.Value, out var orgUnitId))
						{
							newRA.OrganizationUnitId = orgUnitId;
						}

						newRequirmentAdvertises.Add(newRA);
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						var newRequestNumber = rahkaranRA.RequestNumber.HasValue ? (long?)rahkaranRA.RequestNumber.Value : null;
						if (existRA.RequestNumber != newRequestNumber)
						{
							existRA.RequestNumber = newRequestNumber;
							isModified = true;
						}

						if (existRA.Budget != rahkaranRA.IsBudget)
						{
							existRA.Budget = rahkaranRA?.IsBudget ?? false;
							isModified = true;
						}

						if (existRA.RepresentativeDetails != rahkaranRA.CustomerAgentInfo)
						{
							existRA.RepresentativeDetails = rahkaranRA.CustomerAgentInfo;
							isModified = true;
						}

						if (existRA.RepresentativeContactNumber != rahkaranRA.CustomerAgentPhone)
						{
							existRA.RepresentativeContactNumber = rahkaranRA.CustomerAgentPhone;
							isModified = true;
						}

						if (existRA.Description != rahkaranRA.Comments)
						{
							existRA.Description = rahkaranRA.Comments;
							isModified = true;
						}

						var newPreInvoiceAmountRial = rahkaranRA.PreInvoiceAmountInRial.HasValue ? (long?)rahkaranRA.PreInvoiceAmountInRial.Value : null;
						if (existRA.PreInvoiceAmountRial != newPreInvoiceAmountRial)
						{
							existRA.PreInvoiceAmountRial = newPreInvoiceAmountRial;
							isModified = true;
						}

						if (existRA.PreInvoiceAmount != rahkaranRA.PreInvoiceAmountInForeignCurrency)
						{
							existRA.PreInvoiceAmount = rahkaranRA.PreInvoiceAmountInForeignCurrency;
							isModified = true;
						}

						if (existRA.PreInvoiceMiladiDate != rahkaranRA.PreInvoiceDate)
						{
							existRA.PreInvoiceMiladiDate = rahkaranRA.PreInvoiceDate;
							isModified = true;
						}

						var newPriority = rahkaranRA.Priority.HasValue ? (RequirementAdvertisePriorityEnum?)rahkaranRA.Priority.Value : null;
						if (existRA.Priority != newPriority)
						{
							existRA.Priority = newPriority;
							isModified = true;
						}

						var newProjectFinancialValue = rahkaranRA.ProjectValueIdRef.HasValue ? (RequirmentAdvertiseProjectFinancialValueEnum?)rahkaranRA.ProjectValueIdRef.Value : null;
						if (existRA.ProjectFinancialValue != newProjectFinancialValue)
						{
							existRA.ProjectFinancialValue = newProjectFinancialValue;
							isModified = true;
						}

						if (existRA.EndUser != rahkaranRA.EndUser)
						{
							existRA.EndUser = rahkaranRA.EndUser;
							isModified = true;
						}

						if (existRA.Metric != rahkaranRA.IsMetric)
						{
							existRA.Metric = rahkaranRA?.IsMetric ?? false;
							isModified = true;
						}

						var newInquiryType = rahkaranRA.InquiryTypeIdRef.HasValue ? (RequirementAdvertiseInquiryTypeEnum?)rahkaranRA.InquiryTypeIdRef.Value : null;
						if (existRA.InquiryType != newInquiryType)
						{
							existRA.InquiryType = newInquiryType;
							isModified = true;
						}

						// به‌روزرسانی روابط
						if (rahkaranRA.PartyIdRef.HasValue && partyMap.TryGetValue(rahkaranRA.PartyIdRef.Value, out var personOrCompanyId))
						{
							if (existRA.PersonOrCompanyId != personOrCompanyId)
							{
								existRA.PersonOrCompanyId = personOrCompanyId;
								isModified = true;
							}
						}

						if (rahkaranRA.SalesExpertIdRef.HasValue && partyMap.TryGetValue(rahkaranRA.SalesExpertIdRef.Value, out var salesExpertId))
						{
							if (existRA.SalesExpertId != salesExpertId)
							{
								existRA.SalesExpertId = salesExpertId;
								isModified = true;
							}
						}

						if (rahkaranRA.SalesAdministratorRef.HasValue && partyMap.TryGetValue(rahkaranRA.SalesAdministratorRef.Value, out var salesSupervisorId))
						{
							if (existRA.SalesSupervisorId != salesSupervisorId)
							{
								existRA.SalesSupervisorId = salesSupervisorId;
								isModified = true;
							}
						}

						if (rahkaranRA.PartyIndustryItemIdRef.HasValue && partyIndustryItemMap.TryGetValue(rahkaranRA.PartyIndustryItemIdRef.Value, out var subIndustryId))
						{
							if (existRA.SubIndustryId != subIndustryId)
							{
								existRA.SubIndustryId = subIndustryId;
								isModified = true;
							}
						}

						if (rahkaranRA.SalesAgencyIdRef.HasValue && customerMap.TryGetValue(rahkaranRA.SalesAgencyIdRef.Value, out var salesRepresentativeId))
						{
							if (existRA.SalesRepresentativeId != salesRepresentativeId)
							{
								existRA.SalesRepresentativeId = salesRepresentativeId;
								isModified = true;
							}
						}

						if (rahkaranRA.IntroducerExpertRef.HasValue && partyMap.TryGetValue(rahkaranRA.IntroducerExpertRef.Value, out var referrerExpertId))
						{
							if (existRA.ReferrerExpertId != referrerExpertId)
							{
								existRA.ReferrerExpertId = referrerExpertId;
								isModified = true;
							}
						}

						if (rahkaranRA.InstallationCountryRef.HasValue && regionMap.TryGetValue(rahkaranRA.InstallationCountryRef.Value, out var countryId))
						{
							if (existRA.CountryOfInstallationId != countryId)
							{
								existRA.CountryOfInstallationId = countryId;
								isModified = true;
							}
						}

						if (rahkaranRA.RegionalDivisionIdRef.HasValue && regionMap.TryGetValue(rahkaranRA.RegionalDivisionIdRef.Value, out var provinceId))
						{
							if (existRA.InstallationProvinceId != provinceId)
							{
								existRA.InstallationProvinceId = provinceId;
								isModified = true;
							}
						}

						if (rahkaranRA.InstallationCityRef.HasValue && regionMap.TryGetValue(rahkaranRA.InstallationCityRef.Value, out var cityId))
						{
							if (existRA.InstallationCityId != cityId)
							{
								existRA.InstallationCityId = cityId;
								isModified = true;
							}
						}

						if (rahkaranRA.SaleDepartmentIdRef.HasValue && orgUnitMap.TryGetValue(rahkaranRA.SaleDepartmentIdRef.Value, out var orgUnitId))
						{
							if (existRA.OrganizationUnitId != orgUnitId)
							{
								existRA.OrganizationUnitId = orgUnitId;
								isModified = true;
							}
						}

						if (isModified)
						{
							updatedRequirmentAdvertisesCount++;
						}
					}
				}

				if (newRequirmentAdvertises.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newRequirmentAdvertises.Count} اعلام نیازمندی جدید به پایگاه داده", cn);
					await unitOfWork.Repository<RequirmentAdvertise>().AddRangeAsync(newRequirmentAdvertises, cn, false);
				}

				if (updatedRequirmentAdvertisesCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedRequirmentAdvertisesCount} اعلام نیازمندی موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی اعلام نیازمندی با موفقیت انجام شد", cn);

				// همگام‌سازی RequirmentAdvertiseItem (جدول فرزند)
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی اقلام اعلام نیازمندی از راهکاران...", cn);
				var rahkaranItems = await Rdb.Sale_RequirmentAdvertiseItem
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranItems.Count} رکورد قلم اعلام نیازمندی در راهکاران یافت شد", cn);

				// بارگذاری مجدد RequirmentAdvertise ها برای نگاشت _MasterRef
				var allAppRequirmentAdvertises = await unitOfWork.Repository<RequirmentAdvertise>()
					.Table
					.ToListAsync(cn);

				var requirmentAdvertiseMap = await JobLookup.ToUniqueValueMapAsync(
					allAppRequirmentAdvertises.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Sls.RequirmentAdvertise.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var appItems = await unitOfWork.Repository<RequirmentAdvertiseItem>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appItems.Count} رکورد قلم اعلام نیازمندی در پایگاه داده برنامه موجود است", cn);

				var appItemsDict = await JobLookup.ToUniqueMapAsync(
					appItems.Where(x => x.HamkaranId.HasValue).ToList(),
					x => x.HamkaranId!.Value,
					jobLogger,
					"Sls.RequirmentAdvertiseItem.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var newItems = new List<RequirmentAdvertiseItem>();
				int updatedItemsCount = 0;
				int skippedItemsCount = 0;

				foreach (var rahkaranItem in rahkaranItems)
				{
					// پیدا کردن RequirmentAdvertiseId بر اساس _MasterRef
					if (!requirmentAdvertiseMap.TryGetValue(rahkaranItem._MasterRef, out var requirmentAdvertiseId))
					{
						await jobLogger?.LogWarningAsync($"اعلام نیازمندی والد با HamkaranId {rahkaranItem._MasterRef} برای قلم یافت نشد", 0, cn);
						skippedItemsCount++;
						continue;
					}

					// پیدا کردن ProductId بر اساس ProductIdRef
					long? productId = null;
					if (rahkaranItem.ProductIdRef.HasValue && partMap.TryGetValue(rahkaranItem.ProductIdRef.Value, out var foundProductId))
					{
						productId = foundProductId;
					}
					else if (rahkaranItem.ProductIdRef.HasValue)
					{
						await jobLogger?.LogWarningAsync($"کالا با HamkaranId {rahkaranItem.ProductIdRef} برای قلم یافت نشد", 0, cn);
					}

					if (!appItemsDict.TryGetValue(rahkaranItem.Sale_RequirmentAdvertiseItemID, out var existItem))
					{
						// ایجاد رکورد جدید
						newItems.Add(new RequirmentAdvertiseItem
						{
							HamkaranId = rahkaranItem.Sale_RequirmentAdvertiseItemID,
							RequirmentAdvertiseId = (long)requirmentAdvertiseId,
							ProductId = productId,
							Quantity = rahkaranItem.Quantity.HasValue ? (int?)rahkaranItem.Quantity.Value : null
						});
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existItem.RequirmentAdvertiseId != requirmentAdvertiseId)
						{
							existItem.RequirmentAdvertiseId = (long)requirmentAdvertiseId;
							isModified = true;
						}

						if (existItem.ProductId != productId)
						{
							existItem.ProductId = productId;
							isModified = true;
						}

						var newQuantity = rahkaranItem.Quantity.HasValue ? (int?)rahkaranItem.Quantity.Value : null;
						if (existItem.Quantity != newQuantity)
						{
							existItem.Quantity = newQuantity;
							isModified = true;
						}

						if (isModified)
						{
							updatedItemsCount++;
						}
					}
				}

				if (skippedItemsCount > 0)
				{
					await jobLogger?.LogWarningAsync($"تعداد {skippedItemsCount} قلم به دلیل عدم وجود اعلام نیازمندی والد رد شد", 0, cn);
				}

				if (newItems.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newItems.Count} قلم جدید به پایگاه داده", cn);
					await unitOfWork.Repository<RequirmentAdvertiseItem>().AddRangeAsync(newItems, cn, false);
				}

				if (updatedItemsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedItemsCount} قلم موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی اقلام اعلام نیازمندی با موفقیت انجام شد", cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی کامل اعلام نیازمندی و اقلام با موفقیت به پایان رسید", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}

using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Sec;
using Entities.Base;
using Entities.Rahkaran.HCM3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Reflection.Emit;

namespace App.BackgroundJob.Jobs.Hcm
{
	public class PersonelJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork , ApplicationDbContext appDb)
	{
		[JobHandler("هماهنگ کردن اطلاعات پرسنل از راهکاران")]
		public async Task SyncPersonelFromRahkaran(IJobLogger jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				// دریافت اطلاعات از راهکاران
				var rahkaranEmployees = await GetRahkaranEmployee(cn);

				await jobLogger?.LogInfoAsync($"تعداد پرسنل در راهکاران: {rahkaranEmployees.Count}", cn);

				// دریافت اطلاعات موجود در سیستم
				var appPersonels = await unitOfWork.Repository<Personel>().Table
					.Include(p => p.OrgUnit)
					.Include(p => p.Party)
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد پرسنل موجود در سیستم: {appPersonels.Count}", cn);

				// دریافت واحدهای سازمانی برای mapping
				var orgUnits = await unitOfWork.Repository<OrgUnit>().Table.ToListAsync(cn);
				var orgUnitsDict = orgUnits
					.Where(x => x.HamkaranUnitId > 0)
					.ToDictionary(x => x.HamkaranUnitId);

				// دریافت اشخاص برای mapping
				var parties = await unitOfWork.Repository<Party>().Table.ToListAsync(cn);
				var partiesDict = parties
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				// ایجاد دیکشنری پرسنل موجود بر اساس HamkaranId
				var appPersonelsDict = appPersonels
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);


				var rahKaranPartyIds = rahkaranEmployees.Select(c => c.PartyID).ToList();

				var appUser = appDb.Users
					.Where(c=> c.Party.HamkaranId.HasValue && rahKaranPartyIds.Contains((long)c.Party.HamkaranId))
					.ToList();

				var newPersonels = new List<Personel>();
				int updatedCount = 0;
				var rahkaranEmployeesGrouped = rahkaranEmployees.GroupBy(c => c.EmployeeID);

				foreach (var rahkaranEmpGp in rahkaranEmployeesGrouped)
				{
					var rahkaranEmp = rahkaranEmpGp.FirstOrDefault();
					// پیدا کردن واحد سازمانی
					OrgUnit? orgUnit = null;
					if (rahkaranEmp.DepartmentID.HasValue && orgUnitsDict.TryGetValue(rahkaranEmp.DepartmentID.Value, out var ou))
					{
						orgUnit = ou;
					}

					// پیدا کردن شخص
					Party? party = null;
					if (partiesDict.TryGetValue(rahkaranEmp.PartyID, out var p))
					{
						party = p;
						if (orgUnit != null) {
						 appUser.FirstOrDefault(c => c.PartyId == party.Id)
								?.OrgUnitId = orgUnit.Id;
						}
					}

					// بررسی وضعیت فعال/غیرفعال
					IsActiveEnum isActive = IsActiveEnum.Active;
					if (rahkaranEmp.StatusCode == 3 || rahkaranEmp.StatusCode == 121000006)
					{
						isActive = IsActiveEnum.DeActive;
					}

					if (!appPersonelsDict.TryGetValue(rahkaranEmp.EmployeeID, out var existPersonel))
					{
						// اضافه کردن پرسنل جدید
						newPersonels.Add(new Personel
						{
							HamkaranId = rahkaranEmp.EmployeeID,
							Code = rahkaranEmp.EmpCode,
							Name = rahkaranEmp.FirstName,
							Family = rahkaranEmp.LastName,
							FamilyDisplay = rahkaranEmp.LastName,
							FatherName = rahkaranEmp.FatherName,
							Email = rahkaranEmp.Email,
							Mobile = rahkaranEmp.MobileNumber,
							BranchCode = rahkaranEmp.BranchCode,
							BirthDate = rahkaranEmp.BirthDate,
							RealyBirthDate = rahkaranEmp.BirthDate,
							EmploymentDate = rahkaranEmp.EmploymentDate,
							GenderCode = rahkaranEmp.GenderCode,
							NationalID = rahkaranEmp.NationalID,
							InsuranceNo = rahkaranEmp.InsuranceNo,
							IDNumber = rahkaranEmp.IDNumber,
							FieldOfStudy = rahkaranEmp.FieldOfStudy,
							DegreeOfEducation = rahkaranEmp.DegreeOfEducation,
							OrgUnitId = orgUnit?.Id,
							PartyId = party?.Id,
							IsActive = isActive
						});
					}
					else
					{
						// بروزرسانی پرسنل موجود
						bool updated = false;

						if (existPersonel.Code != rahkaranEmp.EmpCode)
						{
							existPersonel.Code = rahkaranEmp.EmpCode;
							updated = true;
						}

						if (existPersonel.Name != rahkaranEmp.FirstName)
						{
							existPersonel.Name = rahkaranEmp.FirstName;
							updated = true;
						}

						if (existPersonel.Family != rahkaranEmp.LastName)
						{
							existPersonel.Family = rahkaranEmp.LastName;
							existPersonel.FamilyDisplay = rahkaranEmp.LastName;
							updated = true;
						}

						if (existPersonel.FatherName != rahkaranEmp.FatherName)
						{
							existPersonel.FatherName = rahkaranEmp.FatherName;
							updated = true;
						}

						if (existPersonel.Email != rahkaranEmp.Email)
						{
							existPersonel.Email = rahkaranEmp.Email;
							updated = true;
						}

						if (existPersonel.Mobile != rahkaranEmp.MobileNumber)
						{
							existPersonel.Mobile = rahkaranEmp.MobileNumber;
							updated = true;
						}

						if (existPersonel.BranchCode != rahkaranEmp.BranchCode)
						{
							existPersonel.BranchCode = rahkaranEmp.BranchCode;
							updated = true;
						}

						if (existPersonel.GenderCode != rahkaranEmp.GenderCode)
						{
							existPersonel.GenderCode = rahkaranEmp.GenderCode;
							updated = true;
						}

						if (rahkaranEmp.BirthDate.HasValue && existPersonel.BirthDate != rahkaranEmp.BirthDate)
						{
							existPersonel.BirthDate = rahkaranEmp.BirthDate;
							existPersonel.RealyBirthDate = rahkaranEmp.BirthDate;
							updated = true;
						}

						if (rahkaranEmp.EmploymentDate.HasValue && existPersonel.EmploymentDate != rahkaranEmp.EmploymentDate)
						{
							existPersonel.EmploymentDate = rahkaranEmp.EmploymentDate;
							updated = true;
						}

						if (!string.IsNullOrEmpty(rahkaranEmp.NationalID) && existPersonel.NationalID != rahkaranEmp.NationalID)
						{
							existPersonel.NationalID = rahkaranEmp.NationalID;
							updated = true;
						}

						if (!string.IsNullOrEmpty(rahkaranEmp.InsuranceNo) && existPersonel.InsuranceNo != rahkaranEmp.InsuranceNo)
						{
							existPersonel.InsuranceNo = rahkaranEmp.InsuranceNo;
							updated = true;
						}

						if (!string.IsNullOrEmpty(rahkaranEmp.IDNumber) && existPersonel.IDNumber != rahkaranEmp.IDNumber)
						{
							existPersonel.IDNumber = rahkaranEmp.IDNumber;
							updated = true;
						}

						if (!string.IsNullOrEmpty(rahkaranEmp.FieldOfStudy) && existPersonel.FieldOfStudy != rahkaranEmp.FieldOfStudy)
						{
							existPersonel.FieldOfStudy = rahkaranEmp.FieldOfStudy;
							updated = true;
						}

						if (!string.IsNullOrEmpty(rahkaranEmp.DegreeOfEducation) && existPersonel.DegreeOfEducation != rahkaranEmp.DegreeOfEducation)
						{
							existPersonel.DegreeOfEducation = rahkaranEmp.DegreeOfEducation;
							updated = true;
						}

						// بروزرسانی واحد سازمانی
						if (orgUnit != null && existPersonel.OrgUnitId != orgUnit.Id)
						{
							existPersonel.OrgUnitId = orgUnit.Id;
							updated = true;
						}

						// بروزرسانی شخص
						if (party != null && existPersonel.PartyId != party.Id)
						{
							existPersonel.PartyId = party.Id;
							updated = true;
						}

						// بروزرسانی وضعیت فعال/غیرفعال
						if (existPersonel.IsActive != isActive)
						{
							existPersonel.IsActive = isActive;
							updated = true;

							if (isActive == IsActiveEnum.DeActive)
							{
								await jobLogger?.LogInfoAsync($"پرسنل غیرفعال شد: {existPersonel.Code} - {existPersonel.Name} {existPersonel.Family}", cn);
							}
						}

						if (updated)
						{
							updatedCount++;
						}
					}

			
				}

				// ذخیره پرسنل های جدید
				if (newPersonels.Any())
				{
					await unitOfWork.Repository<Personel>().AddRangeAsync(newPersonels, cn, false);
					await jobLogger?.LogInfoAsync($"تعداد پرسنل جدید اضافه شده: {newPersonels.Count}", cn);
				}

		 

				// ذخیره تغییرات
				await unitOfWork.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد پرسنل بروزرسانی شده: {updatedCount}", cn);

				// معادل HTS Import_AllPersonelDabiKhane + UpdateActivated_AllPersonelDabirKhane
				await SyncSecPersonnelFromPersonelAsync(jobLogger, cn);

				await jobLogger?.LogInfoAsync("عملیات همگام‌سازی پرسنل با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// پس از همگام‌سازی پرسنل: ردیف‌های گم‌شده Sec.Personnel را می‌سازد
		/// و IsActive را به‌روز می‌کند مگر ForceToSend = true.
		/// </summary>
		private async Task SyncSecPersonnelFromPersonelAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var activePersonels = await unitOfWork.Repository<Personel>().Table
				.Where(p => p.IsActive == IsActiveEnum.Active)
				.ToListAsync(cn);

			var secRows = await unitOfWork.Repository<Personnel>().Table.ToListAsync(cn);
			var secByPersonelId = secRows
				.Where(s => s.PersonelId.HasValue)
				.GroupBy(s => s.PersonelId!.Value)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

			var newSec = new List<Personnel>();
			foreach (var p in activePersonels)
			{
				if (p.Id == null) continue;
				if (secByPersonelId.ContainsKey(p.Id.Value))
					continue;

				newSec.Add(new Personnel
				{
					PersonelId = p.Id,
					NameDisplay = p.Name,
					FamilyDisplay = p.Family ?? p.FamilyDisplay,
					BirthDate = p.BirthDate.HasValue ? p.BirthDate.Value.ToShamsiDate() : null,
					EmploymentDate = p.EmploymentDate.HasValue ? p.EmploymentDate.Value.ToShamsiDate() : null,
					IsActive = IsActiveEnum.Active,
					ForceToSend = false
				});
			}

			if (newSec.Count > 0)
			{
				await unitOfWork.Repository<Personnel>().AddRangeAsync(newSec, cn, false);
				await jobLogger?.LogInfoAsync($"دبیرخانه: {newSec.Count} ردیف پرسنل جدید اضافه شد", cn);
			}

			// همه پرسنل (فعال/غیرفعال) برای به‌روزرسانی IsActive
			var allPersonels = await unitOfWork.Repository<Personel>().Table.ToListAsync(cn);
			var personelById = allPersonels.Where(p => p.Id.HasValue).ToDictionary(p => p.Id!.Value);
			int updatedSec = 0;
			foreach (var sec in secRows)
			{
				if (sec.ForceToSend == true) continue;
				if (!sec.PersonelId.HasValue || !personelById.TryGetValue(sec.PersonelId.Value, out var personel))
					continue;

				var desired = personel.IsActive == IsActiveEnum.Active
					? IsActiveEnum.Active
					: IsActiveEnum.DeActive;
				if (sec.IsActive != desired)
				{
					sec.IsActive = desired;
					updatedSec++;
				}
			}

			await unitOfWork.SaveChangesAsync(cn);
			if (updatedSec > 0)
				await jobLogger?.LogInfoAsync($"دبیرخانه: وضعیت فعال {updatedSec} ردیف به‌روز شد (بدون ForceToSend)", cn);
		}


		public async Task<List<RahkaranEmployee>> GetRahkaranEmployee(CancellationToken cn)
		{
			var sql = @"
WITH LastEmployeeStatute AS 
 (
	SELECT m1.* FROM 
	HCM3.EmployeeStatute m1 
	LEFT OUTER JOIN HCM3.EmployeeStatute m2 ON m1.CreationDate<m2.CreationDate AND m1.EmployeeRef=m2.EmployeeRef
	WHERE m2.EmployeeStatuteID IS NULL
)

SELECT  
			p.PartyID,
            e.EmployeeID,
            e.Code AS EmpCode,
	        CASE WHEN p.Gender = 1 THEN N'آقا' ELSE N'خانم' END AS PersonTitle,
	        p.FirstName,
		    p.LastName,
	        p.FatherName,
	        j.JobID AS JobID ,
	        ps.PostID AS PostID,
	        d.DepartmentID AS DepartmentID,
	        st.StatuteTypeID AS StatusCode,
	        p.Mobile AS MobileNumber,
	        p.Email,
	        p.IDNumber,
	        CASE WHEN EmployeeStatute.WorkLocationCode = 1 THEN 2 WHEN EmployeeStatute.WorkLocationCode = 10 THEN 1 ELSE EmployeeStatute.WorkLocationCode  END AS BranchCode,
	        p.BirthDate AS BirthDate,
	        pst.EmploymentDate AS EmploymentDate,
	        p.Gender AS GenderCode,
	        p.NationalID,
	        EmployeeRelatedOrganization.Code AS InsuranceNo,
			ed.Value AS  FieldOfStudy,
            EducationDiscipline.Value AS  DegreeOfEducation,
            EmployeeStatute.ApplyDate AS LastEmployeeStatuteDate

FROM
	HCM3.Employee e 
	
	INNER JOIN GNR3.Party p ON p.PartyID=e.PartyRef 
	
	INNER JOIN HCM3.EmployeeStatute AS EmployeeStatute ON e.EmployeeID=EmployeeStatute.EmployeeRef 
	
	INNER JOIN LastEmployeeStatute pst ON e.EmployeeID=pst.EmployeeRef 
	INNER JOIN HCM3.StatuteType st ON st.StatuteTypeID=EmployeeStatute.StatuteTypeRef 
	INNER JOIN HCM3.Department d ON d.DepartmentID=pst.DepartmentRef 
	INNER JOIN HCM3.Job j ON j.JobID=pst.JobRef 
	INNER JOIN HCM3.Post ps ON ps.PostID=pst.PostRef 
	INNER JOIN SYS3.Lookup lu ON lu.Type='WorkLocation' AND lu.Code=EmployeeStatute.WorkLocationCode 
	INNER JOIN SYS3.Lookup g ON g.Type='PersonGender' AND g.Code=p.Gender LEFT JOIN 
	HCM3.EmployeeRelatedOrganization AS EmployeeRelatedOrganization ON EmployeeRelatedOrganization.EmployeeRelatedOrganizationID = (SELECT MIN(ERO.EmployeeRelatedOrganizationID) FROM HCM3.EmployeeRelatedOrganization AS ERO WHERE ERO.EmployeeRef = e.EmployeeID AND LEN(ERO.Code) > 4 AND ERO.IsInsurance = 1) LEFT JOIN 
	HCM3.EmployeeEducation ee ON ee.EmployeeEducationID = (SELECT TOP (1) EmployeeEducationID FROM HCM3.EmployeeEducation AS EmployeeEducation WHERE EmployeeEducation.EmployeeRef = e.EmployeeID ORDER BY EmployeeEducation.EffectiveDate DESC) LEFT JOIN 
	SYS3.Lookup ed ON ed.Type = 'EducationDegree' AND ed.Code = ee.DegreeCode LEFT JOIN 
	SYS3.Lookup AS EducationDiscipline ON EducationDiscipline.Type = 'EducationDiscipline' AND EducationDiscipline.Code = ee.DisciplineCode

WHERE st.StatuteTypeID IN (1,121000005,121000014,300000001,300000002,3,121000006)
AND EmployeeStatute.EmployeeStatuteID = (SELECT TOP(1) EmployeeStatuteTemp.EmployeeStatuteID from HCM3.EmployeeStatute AS EmployeeStatuteTemp WHERE EmployeeStatuteTemp.EmployeeRef = e.EmployeeID  ORDER BY EmployeeStatuteTemp.ApplyDate DESC)
";
			return await Rdb.Database.SqlQueryRaw<RahkaranEmployee>(sql).ToListAsync(cn);
		 
		}
	}
}

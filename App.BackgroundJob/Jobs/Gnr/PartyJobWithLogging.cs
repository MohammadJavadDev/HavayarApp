
using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Gnr.Enums;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Gnr
{
    public class PartyJobWithLogging(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
    {
        

        [JobHandler("افزودن اشخاص و شرکت از راهکاران (با لاگ)")]
        public async Task AddPartsFromRahkaran(IJobLogger jobLogger = null, CancellationToken cn = default)
        {
               var logger = jobLogger;


		  try
            {
                // Note: Passing 0 as historyId is fine - the logger will automatically use the current history ID
                logger?.LogInfoAsync("Starting PartyJob - Adding parties from Rahkaran", cn);
                
                var rahakarnParties = await Rdb.RahkaranParties.AsNoTracking()
                    .ToListAsync(cn);

                logger?.LogInfoAsync($"Found {rahakarnParties.Count} parties in Rahkaran", cn);

                var appParties = await unitOfWork.Repository<Party>().Table.ToListAsync(cn);

                logger?.LogInfoAsync($"Found {appParties.Count} parties in application database", cn);

                var appPartiesDict = appParties
                    .Where(x => x.HamkaranId.HasValue)
                    .ToDictionary(x => x.HamkaranId!.Value);

                var newParties = new List<Party>();

                foreach (var rahkaranParty in rahakarnParties)
                {
                    if (!appPartiesDict.TryGetValue(rahkaranParty.PartyID, out var existParty))
                    {
                        newParties.Add(new Party
                        {
                            HamkaranId = rahkaranParty.PartyID,
                            FirstName = rahkaranParty.FirstName,
                            LastName = rahkaranParty.LastName,
                            FullName = rahkaranParty.FullName,
                            Alias = rahkaranParty.Alias,
                            NationalID = rahkaranParty.NationalID,
                            Gender = rahkaranParty.Gender.HasValue ? (PartyGenderEnum?)rahkaranParty.Gender.Value : null,
                            Nationality = rahkaranParty.Nationality,
                            Mobile = rahkaranParty.Mobile,
                            Email = rahkaranParty.Email,
                            IDNumber = rahkaranParty.IDNumber,
                            IDSerial = rahkaranParty.IDSerial,
                            FatherName = rahkaranParty.FatherName,
                            BirthDate = rahkaranParty.BirthDate,
                            EconomicCode = rahkaranParty.EconomicCode,
                            CompanyName = rahkaranParty.CompanyName,
                            Number = rahkaranParty.Number,
                            Type = (PartyTypeEnum)rahkaranParty.Type,
                            FirstNameInEnglish = rahkaranParty.FirstName_EN,
                            LastNameInEnglish = rahkaranParty.LastName_EN,
                            CompanyNameInEnglish = rahkaranParty.CompanyName_EN,
                            Phone = rahkaranParty.Tel,
                            FullNameInEnglish = rahkaranParty.FullName_EN
                        });
                        
                        logger?.LogDebugAsync($"Adding new party: {rahkaranParty.FullName} (ID: {rahkaranParty.PartyID})", 0, cn);
                    }
                    else
                    {
                        bool isModified = false;

                        if (existParty.FirstName != rahkaranParty.FirstName)
                        {
                            existParty.FirstName = rahkaranParty.FirstName;
                            isModified = true;
                        }

                        if (existParty.LastName != rahkaranParty.LastName)
                        {
                            existParty.LastName = rahkaranParty.LastName;
                            isModified = true;
                        }

                        if (existParty.FullName != rahkaranParty.FullName)
                        {
                            existParty.FullName = rahkaranParty.FullName;
                            isModified = true;
                        }

                        if (existParty.Alias != rahkaranParty.Alias)
                        {
                            existParty.Alias = rahkaranParty.Alias;
                            isModified = true;
                        }

                        if (existParty.NationalID != rahkaranParty.NationalID)
                        {
                            existParty.NationalID = rahkaranParty.NationalID;
                            isModified = true;
                        }

                        var newGender = rahkaranParty.Gender.HasValue ? (PartyGenderEnum?)rahkaranParty.Gender.Value : null;
                        if (existParty.Gender != newGender)
                        {
                            existParty.Gender = newGender;
                            isModified = true;
                        }

                        if (existParty.Nationality != rahkaranParty.Nationality)
                        {
                            existParty.Nationality = rahkaranParty.Nationality;
                            isModified = true;
                        }

                        if (existParty.Mobile != rahkaranParty.Mobile)
                        {
                            existParty.Mobile = rahkaranParty.Mobile;
                            isModified = true;
                        }

                        if (existParty.Email != rahkaranParty.Email)
                        {
                            existParty.Email = rahkaranParty.Email;
                            isModified = true;
                        }

                        if (existParty.IDNumber != rahkaranParty.IDNumber)
                        {
                            existParty.IDNumber = rahkaranParty.IDNumber;
                            isModified = true;
                        }

                        if (existParty.IDSerial != rahkaranParty.IDSerial)
                        {
                            existParty.IDSerial = rahkaranParty.IDSerial;
                            isModified = true;
                        }

                        if (existParty.FatherName != rahkaranParty.FatherName)
                        {
                            existParty.FatherName = rahkaranParty.FatherName;
                            isModified = true;
                        }

                        if (existParty.BirthDate != rahkaranParty.BirthDate)
                        {
                            existParty.BirthDate = rahkaranParty.BirthDate;
                            isModified = true;
                        }

                        if (existParty.EconomicCode != rahkaranParty.EconomicCode)
                        {
                            existParty.EconomicCode = rahkaranParty.EconomicCode;
                            isModified = true;
                        }

                        if (existParty.CompanyName != rahkaranParty.CompanyName)
                        {
                            existParty.CompanyName = rahkaranParty.CompanyName;
                            isModified = true;
                        }

                        if (existParty.Number != rahkaranParty.Number)
                        {
                            existParty.Number = rahkaranParty.Number;
                            isModified = true;
                        }

                        var newType = (PartyTypeEnum)rahkaranParty.Type;
                        if (existParty.Type != newType)
                        {
                            existParty.Type = newType;
                            isModified = true;
                        }

                        if (existParty.FirstNameInEnglish != rahkaranParty.FirstName_EN)
                        {
                            existParty.FirstNameInEnglish = rahkaranParty.FirstName_EN;
                            isModified = true;
                        }

                        if (existParty.LastNameInEnglish != rahkaranParty.LastName_EN)
                        {
                            existParty.LastNameInEnglish = rahkaranParty.LastName_EN;
                            isModified = true;
                        }

                        if (existParty.CompanyNameInEnglish != rahkaranParty.CompanyName_EN)
                        {
                            existParty.CompanyNameInEnglish = rahkaranParty.CompanyName_EN;
                            isModified = true;
                        }

                        if (existParty.Phone != rahkaranParty.Tel)
                        {
                            existParty.Phone = rahkaranParty.Tel;
                            isModified = true;
                        }

                        if (existParty.FullNameInEnglish != rahkaranParty.FullName_EN)
                        {
                            existParty.FullNameInEnglish = rahkaranParty.FullName_EN;
                            isModified = true;
                        }

                        if (isModified)
                        {
                            logger?.LogDebugAsync($"Updated existing party: {rahkaranParty.FullName} (ID: {rahkaranParty.PartyID})", 0, cn);
                        }
                    }
                }

                if (newParties.Any())
                {
                    logger?.LogInfoAsync($"Adding {newParties.Count} new parties to database", cn);
                    await unitOfWork.Repository<Party>().AddRangeAsync(newParties, cn, false);
                }

                await unitOfWork.SaveChangesAsync(cn);
                
                logger?.LogInfoAsync("PartyJob completed successfully", cn);
            }
            catch (Exception ex)
            {
                logger?.LogExceptionAsync(ex, cn);
                throw; // Re-throw to let the job worker handle it
            }
        }
    }
}
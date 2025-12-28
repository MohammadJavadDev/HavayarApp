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
    public class PartyJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
    {

        [JobHandler("افزودن اشخاص و شرکت از راهکاران")]
        public async Task AddPartsFromRahkaran(IJobLogger jobLogger = null, CancellationToken cn = default)
        {
            var rahakarnParties = await Rdb.RahkaranParties.AsNoTracking()
                .ToListAsync(cn);

            var appParties = await unitOfWork.Repository<Party>().Table.ToListAsync(cn);

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
                }
                else
                {
                    if (existParty.FirstName != rahkaranParty.FirstName)
                    {
                        existParty.FirstName = rahkaranParty.FirstName;
                    }

                    if (existParty.LastName != rahkaranParty.LastName)
                    {
                        existParty.LastName = rahkaranParty.LastName;
                    }

                    if (existParty.FullName != rahkaranParty.FullName)
                    {
                        existParty.FullName = rahkaranParty.FullName;
                    }

                    if (existParty.Alias != rahkaranParty.Alias)
                    {
                        existParty.Alias = rahkaranParty.Alias;
                    }

                    if (existParty.NationalID != rahkaranParty.NationalID)
                    {
                        existParty.NationalID = rahkaranParty.NationalID;
                    }

                    var newGender = rahkaranParty.Gender.HasValue ? (PartyGenderEnum?)rahkaranParty.Gender.Value : null;
                    if (existParty.Gender != newGender)
                    {
                        existParty.Gender = newGender;
                    }

                    if (existParty.Nationality != rahkaranParty.Nationality)
                    {
                        existParty.Nationality = rahkaranParty.Nationality;
                    }

                    if (existParty.Mobile != rahkaranParty.Mobile)
                    {
                        existParty.Mobile = rahkaranParty.Mobile;
                    }

                    if (existParty.Email != rahkaranParty.Email)
                    {
                        existParty.Email = rahkaranParty.Email;
                    }

                    if (existParty.IDNumber != rahkaranParty.IDNumber)
                    {
                        existParty.IDNumber = rahkaranParty.IDNumber;
                    }

                    if (existParty.IDSerial != rahkaranParty.IDSerial)
                    {
                        existParty.IDSerial = rahkaranParty.IDSerial;
                    }

                    if (existParty.FatherName != rahkaranParty.FatherName)
                    {
                        existParty.FatherName = rahkaranParty.FatherName;
                    }

                    if (existParty.BirthDate != rahkaranParty.BirthDate)
                    {
                        existParty.BirthDate = rahkaranParty.BirthDate;
                    }

                    if (existParty.EconomicCode != rahkaranParty.EconomicCode)
                    {
                        existParty.EconomicCode = rahkaranParty.EconomicCode;
                    }

                    if (existParty.CompanyName != rahkaranParty.CompanyName)
                    {
                        existParty.CompanyName = rahkaranParty.CompanyName;
                    }

                    if (existParty.Number != rahkaranParty.Number)
                    {
                        existParty.Number = rahkaranParty.Number;
                    }

                    var newType = (PartyTypeEnum)rahkaranParty.Type;
                    if (existParty.Type != newType)
                    {
                        existParty.Type = newType;
                    }

                    if (existParty.FirstNameInEnglish != rahkaranParty.FirstName_EN)
                    {
                        existParty.FirstNameInEnglish = rahkaranParty.FirstName_EN;
                    }

                    if (existParty.LastNameInEnglish != rahkaranParty.LastName_EN)
                    {
                        existParty.LastNameInEnglish = rahkaranParty.LastName_EN;
                    }

                    if (existParty.CompanyNameInEnglish != rahkaranParty.CompanyName_EN)
                    {
                        existParty.CompanyNameInEnglish = rahkaranParty.CompanyName_EN;
                    }

                    if (existParty.Phone != rahkaranParty.Tel)
                    {
                        existParty.Phone = rahkaranParty.Tel;
                    }

                    if (existParty.FullNameInEnglish != rahkaranParty.FullName_EN)
                    {
                        existParty.FullNameInEnglish = rahkaranParty.FullName_EN;
                    }
                }
            }

            if (newParties.Any())
            {
                await unitOfWork.Repository<Party>().AddRangeAsync(newParties, cn, false);
            }

            await unitOfWork.SaveChangesAsync(cn);
        }
    }
}


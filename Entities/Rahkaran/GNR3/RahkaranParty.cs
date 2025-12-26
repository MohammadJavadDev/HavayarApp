using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.GNR3
{
	[Table(name: "Party", Schema = "GNR3")]
	public class RahkaranParty
	{
		[Key]
		public long PartyID { get; set; }

		public int? Title { get; set; }

		[MaxLength(50)]
		public string? FirstName { get; set; }

		[MaxLength(50)]
		public string? LastName { get; set; }

		[MaxLength(101)]
		public string? FullName { get; set; }

		[MaxLength(50)]
		public string? Alias { get; set; }

		[MaxLength(20)]
		public string? NationalID { get; set; }

		public int? Gender { get; set; }

		[MaxLength(20)]
		public string? Nationality { get; set; }

		[MaxLength(20)]
		public string? Allegiance { get; set; }

		public int? Religion { get; set; }

		public int? Subreligion { get; set; }

		[MaxLength(20)]
		public string? Mobile { get; set; }

		[MaxLength(50)]
		public string? Email { get; set; }

		[MaxLength(20)]
		public string? IDNumber { get; set; }

		[MaxLength(20)]
		public string? IDSerial { get; set; }

		[MaxLength(50)]
		public string? FatherName { get; set; }

		public long? BirthPlaceRef { get; set; }

		public DateTime? BirthDate { get; set; }

		public DateTime? IssuanceDate { get; set; }

		public long? IssuancePlaceRef { get; set; }

		[MaxLength(20)]
		public string? CompanyCode { get; set; }

		[MaxLength(10)]
		public string? Abbreviation { get; set; }

		[MaxLength(20)]
		public string? EconomicCode { get; set; }

		[MaxLength(20)]
		public string? RegistrationCode { get; set; }

		public int? CompanyType { get; set; }

		[MaxLength(50)]
		public string? ActivityType { get; set; }

		[MaxLength(30)]
		public string? Website { get; set; }

		[MaxLength(50)]
		public string? ReferenceCompany { get; set; }

		[MaxLength(50)]
		public string? Group { get; set; }

		[MaxLength(100)]
		public string? CompanyName { get; set; }

		[MaxLength(50)]
		public string? Number { get; set; }

		public int Type { get; set; }

		public byte[]? Version { get; set; }

		[MaxLength(50)]
		public string? FirstName_EN { get; set; }

		[MaxLength(50)]
		public string? LastName_EN { get; set; }

		[MaxLength(100)]
		public string? CompanyName_EN { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		[MaxLength(20)]
		public string? Tel { get; set; }

		public int? MaritalStatus { get; set; }

		public int? EducationDegree { get; set; }

		public DateTime? MarriageDate { get; set; }

		public DateTime? VATCertificateValidityDate { get; set; }

		[MaxLength(20)]
		public string? FidaRegistration { get; set; }

		public bool? IsPharmaceuticalSupplier { get; set; }

		[MaxLength(101)]
		public string? FullName_EN { get; set; }

	}
}

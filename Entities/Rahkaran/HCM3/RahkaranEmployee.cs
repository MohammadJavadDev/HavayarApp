using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Rahkaran.HCM3
{
	[Keyless]
	public class RahkaranEmployee
	{
		public long PartyID { get; set; }
		public long EmployeeID { get; set; }
		public string? EmpCode { get; set; }
		public string? PersonTitle { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? FatherName { get; set; }
		public long? JobID { get; set; }
		public long? PostID { get; set; }
		public long? DepartmentID { get; set; }
		public long? StatusCode { get; set; }
		public string? MobileNumber { get; set; }
		public string? Email { get; set; }
		public string? IDNumber { get; set; }
		public int? BranchCode { get; set; }
		public DateTime? BirthDate { get; set; }
		public DateTime? EmploymentDate { get; set; }
		public int? GenderCode { get; set; }
		public string? NationalID { get; set; }
		public string? InsuranceNo { get; set; }
		public string? FieldOfStudy { get; set; }
		public string? DegreeOfEducation { get; set; }
		public DateTime? LastEmployeeStatuteDate { get; set; }
	}
}

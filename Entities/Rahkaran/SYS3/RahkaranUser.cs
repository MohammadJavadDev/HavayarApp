using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.SYS3
{ 

	[Table(name: "User", Schema = "SYS3")]
	public class RahkaranUser
	{
		[Key]
		public long UserID { get; set; }

		[Required]
		[MaxLength(50)]
		public string Name { get; set; }

		public int Status { get; set; }

		public long? PartyRef { get; set; }

		public int Type { get; set; }

		public bool? IsAdministrator { get; set; }

		public bool IsLocked { get; set; }

		public DateTime? LockExpiration { get; set; }
		 
		[MaxLength(50)]
		public string? DomainUserName { get; set; }

		[MaxLength(500)]
		public string? ValidWorkstations { get; set; }

		[MaxLength(50)]
		public string? Context { get; set; }

		[MaxLength(500)]
		public string? ContextData { get; set; }

		public bool? RemoteServiceEnabled { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		public byte[]? Version { get; set; }

	}
}

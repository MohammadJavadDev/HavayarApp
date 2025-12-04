using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base
{
	[Table(name: "AuditLog", Schema = "system")]
	public class AuditLog
    {
        public long Id { get; set; }
        public string TableName { get; set; }
        public string? KeyValues { get; set; }
        public long EntityId { get; set; }
        public DateTime CreatedOnMiladiDateTime { get; set; }
        public string CreatedOnShamsiDateTime { get; set; }
        public long? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public AuditLogType Type {get; set; }
        public ICollection<AuditLogDetail> AuditLogDetails { get; set; } = new List<AuditLogDetail>();
    }

    public enum AuditLogType
    {
        Add,
        Update,
        Delete,
    }

	[Table(name: "AuditLogDetail", Schema = "system")]
	public class AuditLogDetail
    {
        public long Id { get; set; }
        public long AuditLogId { get; set; }
        public string PropertyName { get; set; }
        public string? PropertyTitle { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
 
    }
}

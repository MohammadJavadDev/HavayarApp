using Common.Attributes;
using System.ComponentModel;

namespace Entities.Base
{
    public class PropertyIdentity :BaseEntity
    {
        [DisplayName("نام ستون")]
        [DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
        public string PropertyName { get; set; }
        [DisplayName("نام جدول")]
        [DisplayInfo(null, true, type: SystemType.String, required: true)]
        public string TypeName { get; set; }
        [DisplayName("شماره")]
        [DisplayInfo(null, true, type: SystemType.Long, required: true)]
        public long Number { get; set; }
        [DisplayName("پرش")]
        [DisplayInfo(null, true, type: SystemType.Long, required: true)]
        public long Step { get; set; }
        [DisplayName("پیش شماره")]
        [DisplayInfo(null, true, type: SystemType.Long, required: true)]
        public long Prefix { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stimulsoft.Report.Dictionary;

namespace ReportBuilder.Services
{
    public class ReportBuilderReportFonts
    {
        public string Name { get; set; }
        public StiResourceType Type  { get; set; }
        public Byte[]? Bytes { get; set; }
        public bool Loaded { get; set; } = false;
    }
}

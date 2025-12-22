using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.Job
{
	// Models/JobViewModel.cs
	// Models/JobViewModel.cs
	public class JobViewModel
	{
		public int ScheduleId { get; set; }
		public string JobId { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public bool IsActive { get; set; }
		public int IntervalSeconds { get; set; }
		public string LastStatus { get; set; } // Running, Idle, Failed
		public DateTime? LastRunTime { get; set; }
		public DateTime? NextRunTime { get; set; }
		public bool HasSchedule { get; set; }
	}

	public class JobHistoryViewModel
	{
		public DateTime StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public bool IsSuccess { get; set; }
		public string Message { get; set; } // LogOutput or ErrorMessage
	}
}

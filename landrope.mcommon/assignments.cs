using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mcommon
{

	public class AssignmentViewM
	{
		public string key { get; set; }
		public string identifier { get; set; }
		public DocProcessStep? step { get; set; }
		public AssignmentCat? category { get; set; }
		public DateTime? issued { get; set; }
		public DateTime? accepted { get; set; }
		public DateTime? closed { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string company { get; set; }
		public uint? duration { get; set; }
		public DateTime? duedate { get; set; }
		public int? overdue { get; set; }
	}
}

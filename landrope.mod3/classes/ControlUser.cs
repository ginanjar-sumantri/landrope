using landrope.common;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3.classes
{
	[Entity("cuser", "assignment_IC")]
	public class ControlUser : entity3
	{
		public AssignmentTeam team { get; set; }
		public ControlLevel level { get; set; }
	}
}

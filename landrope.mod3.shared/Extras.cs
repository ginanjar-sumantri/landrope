using DynForm.shared;
using landrope.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3.shared
{
	public class Extras3
	{
		public option[] optProjects { get; set; }
		public option[] optDesas { get; set; }
		public optbase<string, AssignmentCat>[] lstCategories { get; set; }
		public optbase<AssignmentCat, DocProcessStep>[] lstSteps { get; set; }
		public optbase<string, SifatBerkas>[] lstSifats { get; set; }
		public option[] optPenampungs { get; set; }
		public option[] optSKs { get; set; }
		public option[] optNotarists { get; set; }
		public option[] optCompanies { get; set; }
		public option[] optUsers { get; set; }
		public option[] optPICs { get; set; }
	}
}

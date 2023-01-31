using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using landrope.common;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace landrope.mod3
{
	public class procCost
	{
		public DateTime time { get; set; }
		public Cost[] costs { get; set; }
	}

	public class Cost
	{
		public AssignmentCat[] cats { get; set; }
		public CostDetail[] details { get; set; }
	}

	public class CostCond
	{
		public string prop { get; set; }
		public string oper { get; set; }
		public string valstr { get; set; }
		public double? valnum { get; set; }
	}

	public class CostDetail
	{
		public class SubDtl
		{
			public string desc { get; set; }
			public string subdesc { get; set; }
			public CostCond cond { get; set; }
			public double? variable { get; set; }
			public double? @fixed { get; set; }
		}

		public DocProcessStep step { get; set; }
		public SubDtl[] subdetails { get; set; }
	}


	public class FlatCost
	{
		public DateTime time { get; set; }
		public AssignmentCat categ { get; set; }
		public DocProcessStep step { get; set; }
		public string desc { get; set; }
		public string subdesc { get; set; }
		public CostCond cond { get; set; }
		public double? variable { get; set; }
		public double? @fixed { get; set; }

		public static List<FlatCost> Lists = null;
		public static void LoadFlatCosts(LandropePlusContext context)
		{
			Lists = context.GetCollections(new FlatCost(), "process_costs", "{}", "{_id:0}").ToList();
		}
	}

}



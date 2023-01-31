using System;
using System.Collections.Generic;
using System.Text;
using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;

namespace landrope.mod3
{
	[Entity("budget","budgets")]
	[BsonKnownTypes(typeof(BudgetHibah))]
	public class Budget : namedentity3
	{
		public DateTime? since { get; set; }
		public double amout { get; set; }
	}

	[Entity("budget_hibah","budgets")]
	public class BudgetHibah : Budget
	{
		public class Part
		{
			public BudgetPost post { get; set; }
			public int? term { get; set; }
			public double[] amounts { get; set; }
		}

		public Part[] parts { get; set; }
	}
}

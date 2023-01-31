using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using mongospace;

namespace landrope.mod3
{
	public partial class docnodtl
	{
		[BsonDateTimeOptions(Kind=DateTimeKind.Local)]
		public DateTime moyear { get; set; }
		public int lastno { get; set; } = 0;

		public int NextNumber()
		{
			int num = this.lastno + 1;
			this.lastno = num;
			return num;
		}

	}
}

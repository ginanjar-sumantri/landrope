using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Linq;

namespace landrope.docdb
{
	[BsonDiscriminator("doctemplate")]
	public class doctemplate
	{
		public string key { get; set; }
		public string Html { get; set; }
		public DateTime? lastUpd { get; set; }
		public DateTime? lastReq { get; set; }
	}

	[BsonDiscriminator("doclayout")]
	public class doclayout
	{
		public string key { get; set; }
		public string keyTemplate { get; set; }
		public string[] infos { get; set; } = new string[0];
		public string[] signers { get; set; } = new string[0];
		public string[] notes { get; set; } = new string[0];
        public DateTime? lastUpd { get; set; }
		public DateTime? lastReq { get; set; }
	}

	[BsonDiscriminator("docView")]
	public class docView
	{
	}
}

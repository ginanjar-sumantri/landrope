using auth.mod;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace landrope.mod2
{
	public class Pay : DetailBase
	{
		[BsonRequired]
		public string nomor { get; set; }
		[BsonRequired]
		public DateTime? tanggal { get; set; }
#if (_INIT_MONGO_)
			= DateTime.Today;
#endif
		[BsonRequired]
		public double? jumlah { get; set; }
		public virtual object extras { get; set; }
	}

	public class Cash : Pay
	{

	}

	public class Rfp : Pay
	{
		public string giro { get; set; } = null;
	}

	public class CashRinci : Cash
	{
		public double? hargaSatuan { get; set; }
	}

	public class RfpRinci : Rfp
	{
		public double? hargaSatuan { get; set; }
	}

	public class Payment : DetailBase
	{
		public Rfp rfp { get; set; }
#if (_INIT_MONGO_)
			= new Rfp();
#endif
		public Cash kas { get; set; }
#if (_INIT_MONGO_)
			= new Cash();
#endif
	}

	public class PaymentRinci : DetailBase
	{
		public RfpRinci rfp { get; set; }
#if (_INIT_MONGO_)
			= new RfpRinci();
#endif
		public CashRinci kas { get; set; }
#if (_INIT_MONGO_)
			= new CashRinci();
#endif
	}

	public class Pajak
	{
		public double? jumlah { get; set; }
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? validasi { get; set; }
	}

	public class SPS
	{
		public string nomor { get; set; }
		public DateTime tanggal { get; set; }
		public double jumlah { get; set; }
	}

}

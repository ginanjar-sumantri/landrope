using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class Pay 
	{
		public string nomor { get; set; }
		public DateTime? tanggal { get; set; }
		public double? jumlah { get; set; }
	}

	public class Cash : Pay
	{

	}

	public interface IMultiply
	{
		bool CheckDummy();
	}

	public class Rfp : Pay, IMultiply
	{
		public string giro { get; set; }
		public bool CheckDummy() => string.IsNullOrEmpty(this.giro) && this.jumlah == null && string.IsNullOrEmpty(this.nomor) && this.tanggal == null;
	}

	public class CashRinci : Cash
	{
		public double? hargaSatuan { get; set; }
	}

	public class RfpRinci : Rfp
	{
		public double? hargaSatuan { get; set; }
	}

	public class Payment
	{
		public Rfp rfp { get; set; }
		public Cash kas { get; set; }
	}

	public class PaymentRinci
	{
		public RfpRinci rfp { get; set; }
		public CashRinci kas { get; set; }
	}
}

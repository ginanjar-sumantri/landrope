using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class Order
	{
		public string nomor { get; set; }
		public DateTime? tanggal { get; set; }
	}

	public class OrderItem : Order
	{
		public string keyNotaris { get; set; }
		public string keyPT { get; set; }
		public Payment payment { get; set; }
	}

	public class OrderNotaris : OrderItem
	{
		public class OrderNotarisInfo
		{
			public string keyNotaris { get; set; }
			public string keyPT { get; set; }
			public Payment payment { get; set; }
		}
		public OrderNotarisInfo[] histories { get; set; } //*
	}

	public class OrderBPN : Order
	{
		public RfpRinci taktis { get; set; }
	}
}

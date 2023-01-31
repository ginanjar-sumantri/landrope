using flow.common;
//using landrope.mcommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public enum TrxType
	{
		Masuk = 1,
		Peminjaman = 2,
		Pengembalian = 3
	}

	public class DocPart
	{
		public string key { get; set; }
		public string keyDocType { get; set; }
		public string chainkey { get; set; }

		public DocPart(string key, string keyDocType, string chainkey)
		{
			(this.key, this.keyDocType, this.chainkey) = (key, keyDocType, chainkey);
		}
	}

	public class DocPartTrx : DocPart
	{
		public int[] exis { get; set; }

		public Existency[] exis2
		{
			get => exis.ConvertBack2();
			set { exis = value.Convert(); }
		}

		public DocPartTrx(string key, string keyDocType, string chainkey, int[] exis)
			: base(key, keyDocType, chainkey)
		{
			this.exis = exis;
		}

		public DocPartTrx(string key, string keyDocType, string chainkey, Existency[] exis)
			: base(key, keyDocType, chainkey)
		{
			this.exis = exis.Convert();
		}
	}

	public class trxView
	{
		public string key { get; set; }
		public string number { get; set; }
		public string creator { get; set; }
		public DateTime created { get; set; }
		public string trxtipe { get; set; }
		public int? durasi { get; set; }
		public string trxref { get; set; }

		public ToDoState state { get; set; }
		public string status { get; set; }
		public DateTime? statustime { get; set; }
		public ToDoVerb verb { get; set; }
		public string ToDo { get; set; }
		public string keyRoute { get; set; }
		public ToDoControl[] cmds { get; set; }
	}

	public class trxCore
	{
		public TrxType tipe { get; set; }
		public int? duration { get; set; }
		public string keyReff { get; set; }

	}
}

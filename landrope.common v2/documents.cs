using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class cmnDoc : cmnBase
	{
		public string key { get; set; }
		public bool? invalid { get; set; }

		public int en_jenis { get; set; }
		public string jenis { get; set; }
		public int? tahun { get; set; }
		public string note { get; set; }
		public string title { get; set; }
	}

	public class cmnDocAlasHak : cmnDoc
	{
		public int en_jnsalas { get; set; }
		public string jnsalas { get; set; }
		public string nomor { get; set; }
		public string nama { get; set; }
		public double? luas { get; set; }
		public DateTime? expired { get; set; }
	}

	public class cmnDocID : cmnDoc
	{
		public string nomor { get; set; }
		public int en_jenisid { get; set; }
		public string jenisid { get; set; }
	}

	public class cmnDocPBB : cmnDoc
	{
		public string NOP { get; set; }
		public int nilai { get; set; }
		public bool lunas { get; set; }
	}
}

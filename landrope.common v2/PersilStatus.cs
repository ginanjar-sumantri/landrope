using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace landrope.common
{
	public class PersilStatus
	{
		[CsvLabel("Id Bidang")]
		public string key { get; set; }
		[CsvLabel(true)]
		public string keyProject { get; set; }
		[CsvLabel(true)]
		public string keyDesa { get; set; }
		[CsvLabel("Luas Produk")]
		public double? luasSurat { get; set; }
		public bool? overlap { get; set; }
		public bool? bebas { get; set; }
		public bool? deal { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string proses { get; set; }
		[CsvLabel("Dok Proses")]
		public string g_type { get; set; }
		[CsvLabel("Nomor Dok")]
		public string g_nomor { get; set; }
		[CsvLabel("Luas Dok")]
		public double? g_luas { get; set; }

		public bool _Bebas => (proses == "s") && (bebas == true);
		public bool BlmBebas => (proses == "s") && (bebas != true);
		public bool Damai => (proses == "b") && (bebas == true);
		public bool BlmDamai => (proses == "b") && (bebas != true);

		public PersilStatus SetLocation(string project, string desa)
		{
			if (project != null)
				this.project = project;
			if (desa != null)
				this.desa = desa;
			return this;
		}
	}

	public class PersilStatusExt : PersilStatus
	{
		public string dummy => "Luas";

		public string Desc => (proses, bebas) switch
		{
			("s", true) => "ANormatif - Bebas",
			("s", false) => "BNormatif - Belum Bebas",
			("b", true) => "CBintang - Damai",
			("b", false) => "DBintang - Belum Damai",
			_ => ""
		};

		public decimal LuasHa => ((decimal)(luasSurat ?? 0)) / 10000m;
		public decimal lsBebas => _Bebas ? ((decimal)(luasSurat ?? 0) / 10000m) : 0m;
		//public decimal lsBebasBintang => proses == "hibah" && _Bebas ? ((decimal)(luasSurat ?? 0) / 10000m) : 0m;
		public decimal lsBelom => BlmBebas ? ((decimal)(luasSurat ?? 0) / 10000m) : 0;
		//public decimal lsOverlap => overlap == true ? ((decimal)(luasSurat??0) / 10000m) : 0m;
		public decimal lsDamai => Damai ? ((decimal)(luasSurat ?? 0) / 10000m) : 0m;
		public decimal lsBlmDamai => BlmDamai ? ((decimal)(luasSurat ?? 0) / 10000m) : 0m;
		public decimal lsTotal => (decimal)(luasSurat ?? 0) / 10000m;
	}

	public class PersilStatus2
	{
		//[CsvLabel("Id Bidang")]
		public string idBidang { get; set; }
		[CsvLabel(false)]
		public string keyProject { get; set; }
		[CsvLabel(false)]
		public string keyDesa { get; set; }
		[CsvLabel("Luas")]
		public double? luas { get; set; }
		[CsvLabel("Luas Surat")]
		public double? luasSurat { get; set; }
		[CsvLabel("Luas Dibayar")]
		public double? luasDibayar { get; set; }
		[CsvLabel("Luas Overlap")]
		public double? luasOverlap { get; set; }
		public double? sisaOverlap { get; set; }
		public string status { get; set; }
		//public bool? bebas { get; set; }
		//public bool? deal { get; set; }
		[CsvLabel("Project")]
		public string project { get; set; }
		[CsvLabel("Desa")]
		public string desa { get; set; }
		public string proses { get; set; }
		public string kategori { get; set; }
		//[CsvLabel("sub kategori")]
		//public string subkategori { get; set; }

		//[CsvLabel("Dok Proses")]
		//public string g_type { get; set; }
		//[CsvLabel("Nomor Dok")]
		//public string g_nomor { get; set; }
		//[CsvLabel("Luas Dok")]
		//public double? g_luas { get; set; }

		//public bool _Bebas => Regex.Match(status,"[a-z]Bebas$");
		//public bool BlmBebas => (proses == "s") && (bebas != true);
		//public bool Damai => (proses == "b") && (bebas == true);
		//public bool BlmDamai => (proses == "b") && (bebas != true);

		//public double Luas => status=="aSudah Bebas" ? (luasDibayar??0) : (luasSurat??0);
		public decimal luasD => (decimal)luas;
		public decimal luasOverlapD => (decimal)(luasOverlap ?? 0);

		public PersilStatus2 SetLocation(string project, string desa)
		{
			if (project != null)
				this.project = project;
			if (desa != null)
				this.desa = desa;
			return this;
		}
	}

	public class PersilStatusExt2 : PersilStatus2
	{
		//public string Desc => status switch
		//	{
		//		"aDamai" or "cBebas" => "aBebas",
		//		_ => "bBelum Bebas"
		//	};
		public string desc { get; set; }

		[JsonIgnore]
		public decimal luasHa => luasD / 10000m;

		public PersilStatusExt2 Clone(double newLuas)
		{
			var js = JsonConvert.SerializeObject(this);
			var ps = JsonConvert.DeserializeObject<PersilStatusExt2>(js);
			ps.luasDibayar = /*ps.luasSurat = */newLuas;
			//ps.bebas = newbebas;
			return ps;
		}
		//public decimal lsBebas => _Bebas ? LuasHa : 0m;
		////public decimal lsBebasBintang => proses == "hibah" && _Bebas ? ((decimal)(luasSurat ?? 0) / 10000m) : 0m;
		//public decimal lsBelom => BlmBebas ? LuasHa : 0;
		////public decimal lsOverlap => overlap == true ? ((decimal)(luasSurat??0) / 10000m) : 0m;
		//public decimal lsDamai => Damai ? LuasOverlapD / 10000m : 0m;
		//public decimal lsBlmDamai => Damai ? (Luas-LuasOverlapD) / 10000m : Luas;
	}

	[AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = false)]
	public class CsvLabelAttribute : Attribute
	{
		public string Text { get; set; }
		public bool Avoid { get; set; }

		public CsvLabelAttribute(string Text)
		{
			this.Text = Text;
		}
		public CsvLabelAttribute(bool Avoid)
		{
			this.Avoid = Avoid;
		}
	}
}

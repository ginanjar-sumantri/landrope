using landrope.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace landrope.mod.shared
{

	public class PersilSteps
	{
		public string _t { get; set; }
		public string key { get; set; }
		public Step[] steps { get; set; }

		public PersilStatus Convert() =>
			_t switch
			{
				"persilGirik" => new Status_Girik(this),
				"persilHGB" => new Status_HGB(this),
				"persilSHM" => new Status_SHM(this),
				"persilSHP" => new Status_SHP(this),
				"PersilHibah" => new Status_Hibah(this),
				_ => null
			};
	}

	public class Step
	{
		public string k { get; set; }
		public int v { get; set; }
	}

	public abstract class PersilStatus
	{
		public string key { get; set; }
		public LandState status { get; set; }

		public PersilStatus(string key)
		{
			this.key = key;
		}

		protected void FindStatus(PersilSteps pss, string[] steporders)
		{
			if (pss.steps.Any(s => s.k == "kampung" && s.v == 1))
			{
				this.status = LandState.Kampung__Bengkok_Desa;
				return;
			}
			else if (pss.steps.Any(s => s.k == "belum" && s.v == 1))
			{
				this.status = LandState.Belum_Bebas;
				return;
			}
			var status = "";
			for (int i = steporders.Length - 1; i >= 0; i--)
			{
				var xstatus = steporders[i];
				if (pss.steps.Any(s => s.k == xstatus && s.v == 1))
				{
					status = xstatus;
					break;
				}
			}
			if (status == "")
				status = "basic";
			this.status = FindStatus(status);
		}

		protected abstract LandState FindStatus(string status);
	}

	public class Status_Girik : PersilStatus
	{
		string[] steporders = new string[] {
						"kampung",
						"belum",
						"basic",
						"akta",
						"nibo",
						"sph",
						"nibp",
						"skkt",
						"skkw",
						"buku"
			};

		public Status_Girik(PersilSteps pss)
			: base(pss.key)
		{
			FindStatus(pss, steporders);
		}
		protected override LandState FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandState.Kampung__Bengkok_Desa,
				"belum" => LandState.Belum_Bebas,
				"akta" => LandState.PPJB|LandState.Selesai_,
				"nibo" => LandState.PBT_Perorangan | LandState.Selesai_,
				"sph" => LandState.SPH | LandState.Selesai_,
				"nibp" => LandState.PBT_PT | LandState.Selesai_,
				"sk" => LandState.SK_BPN | LandState.Selesai_,
				"buku" => LandState.Cetak_Buku|LandState.Selesai_,
				_ => LandState.Kumpulkan_Berkas|LandState.Sedang_
			};
	}

	public class Status_HGB : PersilStatus
	{
		string[] steporders = new string[] {
				"kampung",
				"belum",
				"basic",
				"akta",
				"prosajb",
				"balik"
		};

		public Status_HGB(PersilSteps pss)
			: base(pss.key)
		{
			FindStatus(pss, steporders);
		}
		protected override LandState FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandState.Kampung__Bengkok_Desa,
				"belum" => LandState.Belum_Bebas,
				"akta" => LandState.PPJB | LandState.Selesai_,
				"prosajb" => LandState.AJB|LandState.Selesai_,
				"balik" => LandState.Balik_Nama|LandState.Selesai_,
				_ => LandState.Kumpulkan_Berkas|LandState.Sedang_
			};
	}

	public class Status_SHM : PersilStatus
	{
		string[] steporders = new string[] {
				"kampung",
				"belum",
				"basic",
				"akta",
				"turun",
				"prosajb",
				"balik"
		};

		public Status_SHM(PersilSteps pss)
			: base(pss.key)
		{
			FindStatus(pss, steporders);
		}
		protected override LandState FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandState.Kampung__Bengkok_Desa,
				"belum" => LandState.Belum_Bebas,
				"akta" => LandState.PPJB | LandState.Selesai_,
				"turun" => LandState.Penurunan__Peningkatan_Hak | LandState.Selesai_,
				"prosajb" => LandState.AJB | LandState.Selesai_,
				"balik" => LandState.Balik_Nama|LandState.Selesai_,
				_ => LandState.Kumpulkan_Berkas | LandState.Sedang_
			};
	}

	public class Status_SHP : PersilStatus
	{
		string[] steporders = new string[] {
				"kampung",
				"belum",
				"basic",
				"akta",
				"naik",
				"prosajb",
				"balik"
		};

		public Status_SHP(PersilSteps pss)
			: base(pss.key)
		{
			FindStatus(pss, steporders);
		}
		protected override LandState FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandState.Kampung__Bengkok_Desa,
				"belum" => LandState.Belum_Bebas,
				"akta" => LandState.PPJB | LandState.Selesai_,
				"naik" => LandState.Penurunan__Peningkatan_Hak | LandState.Selesai_,
				"prosajb" => LandState.AJB | LandState.Selesai_,
				"balik" => LandState.Balik_Nama | LandState.Selesai_,
				_ => LandState.Kumpulkan_Berkas | LandState.Sedang_
			};
	}

	public class Status_Hibah : PersilStatus
	{
		string[] steporders = new string[] {
			"kampung",
			"belum",
			"basic",
			"nibo",
			"shm",
			"turun",
			"prosajb",
			"balik"
		};

		public Status_Hibah(PersilSteps pss)
			: base(pss.key)
		{
			FindStatus(pss, steporders);
		}
		protected override LandState FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandState.Kampung__Bengkok_Desa,
				"belum" => LandState.Belum_Bebas,
				"nibo" => LandState.PBT_Perorangan | LandState.Selesai_,
				"turun" => LandState.Penurunan__Peningkatan_Hak | LandState.Selesai_,
				"prosajb" => LandState.AJB | LandState.Selesai_,
				"balik" => LandState.Cetak_Buku | LandState.Selesai_,
				_ => LandState.Kumpulkan_Berkas | LandState.Sedang_
			};
	}

}


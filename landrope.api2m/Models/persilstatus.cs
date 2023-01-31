using landrope.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.api2.Models
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
		public LandStatus2 status { get; set; }

		public PersilStatus(string key)
		{
			this.key = key;
		}

		protected void FindStatus(PersilSteps pss, string[] steporders)
		{
			if (pss.steps.Any(s => s.k == "belum" && s.v == 1))
			{
				this.status = LandStatus2.Belum_Bebas;
				return;
			}
			var status = "";
			for (int i = steporders.Length - 1; i > 0; i--)
			{
				status = steporders[i];
				if (pss.steps.Any(s => s.k == status && s.v == 1))
					break;
			}

			this.status = FindStatus(status);
		}

		protected abstract LandStatus2 FindStatus(string status);
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
		protected override LandStatus2 FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandStatus2.Kampung__Bengkok_Desa,
				"belum" => LandStatus2.Belum_Bebas,
				"akta" => LandStatus2.PJB,
				"nibo" => LandStatus2.PBT,
				"sph" => LandStatus2.SPH,
				"nibp" => LandStatus2.PBT_PT,
				"sk" => LandStatus2.SK,
				"buku" => LandStatus2.HGB_PT,
				_ => LandStatus2.Baru_Bebas
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
		protected override LandStatus2 FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandStatus2.Kampung__Bengkok_Desa,
				"belum" => LandStatus2.Belum_Bebas,
				"akta" => LandStatus2.PJB,
				"prosajb" => LandStatus2.AJB,
				"balik" => LandStatus2.HGB_PT,
				_ => LandStatus2.Baru_Bebas
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
		protected override LandStatus2 FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandStatus2.Kampung__Bengkok_Desa,
				"belum" => LandStatus2.Belum_Bebas,
				"akta" => LandStatus2.PJB,
				"turun" => LandStatus2.Penurunan__Peningkatan_Hak,
				"prosajb" => LandStatus2.AJB,
				"balik" => LandStatus2.HGB_PT,
				_ => LandStatus2.Baru_Bebas
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
		protected override LandStatus2 FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandStatus2.Kampung__Bengkok_Desa,
				"belum" => LandStatus2.Belum_Bebas,
				"akta" => LandStatus2.PJB,
				"naik" => LandStatus2.Penurunan__Peningkatan_Hak,
				"prosajb" => LandStatus2.AJB,
				"balik" => LandStatus2.HGB_PT,
				_ => LandStatus2.Baru_Bebas
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
		protected override LandStatus2 FindStatus(string status)
			=> status switch
			{
				//"basic" => LandStatus2.Baru_Bebas,
				"kampung" => LandStatus2.Kampung__Bengkok_Desa,
				"belum" => LandStatus2.Belum_Bebas,
				"nibo" => LandStatus2.PBT,
				"shm" => LandStatus2.SHM,
				"turun" => LandStatus2.Penurunan_Hak,
				"prosajb" => LandStatus2.AJB,
				"balik" => LandStatus2.HGB_PT,
				_ => LandStatus2.Baru_Bebas
			};
	}

}


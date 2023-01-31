#define VER_15
//using landrope.layout;
//using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace landrope.common
{
	public class cmnPersilBasic : cmnBase
	{
		public int en_proses { get; set; }
		public string proses { get; set; }
		public int en_lahan { get; set; }
		public string lahan { get; set; }
		public int en_jenis { get; set; }
		public string jenis { get; set; }
		public int en_status { get; set; }
		public string status { get; set; }

		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public string noPeta { get; set; }
		public string keyPenampung { get; set; }
		public string keyPTSK { get; set; }
		public OrderNotaris order { get; set; }

		public DateTime? terimaBerkas { get; set; }
		public DateTime? inputBerkas { get; set; }
		public string pemilik { get; set; }
		public string telpPemilik { get; set; }
		public string group { get; set; }
		public string mediator { get; set; }
		public string telpMediator { get; set; }
		public AlasHak surat { get; set; }
		public AlasHak[] riwayat { get; set; }
		public string st_riwayat { get; set; }

		public double? luasSurat { get; set; }
		public double? luasInternal { get; set; }
		public double? luasGU { get; set; }
		public double? luasPBT { get; set; }
		public double? luasDibayar { get; set; }
		public string NOP_PBB { get; set; }
		public string kekurangan { get; set; }
		public string alias { get; set; }
		public double? satuan { get; set; }
		public double? total { get; set; }
		public int? tahap { get; set; }

		public string note { get; set; }
		public RiwayatArsipBasic arsip { get; set; }
	}

#if VER_15
	public class cmnProsesPerjanjian : cmnBase
	{
		public OrderNotaris order { get; set; }
		public Akta PJB { get; set; }
		public Akta kuasa { get; set; }
		public Akta kesepakatan { get; set; }
		public AktaLain lainnya { get; set; }
	}
#else

	public class ProsesPerjanjian<T, TH, TO> : ValidatableItem, IConvertible<cmnProsesPerjanjian<TH>>
				where T : Perjanjian where TH : cmnPerjanjian where TO : cmnProsesPerjanjian<TH>
	{
		public OrderNotaris order { get; set; }
		public T hasil { get; set; }

		public virtual void PostBackward(cmnProsesPerjanjian<TH> obj)
		{
		}

		public virtual void PostForward(cmnProsesPerjanjian<TH> toObj)
		{
		}

		public virtual void PreBackward(cmnProsesPerjanjian<TH> obj)
		{
		}

		public virtual void PreForward()
		{
		}
	}

	public class ProsesPerjanjianSertifikat : ProsesPerjanjian<PerjanjianSertifikat, cmnPerjanjianSertifikat, cmnProsesPerjanjianSertifikat>
	{
	}
	public class ProsesPerjanjianGirik : ProsesPerjanjian<PerjanjianGirik, cmnPerjanjianGirik, cmnProsesPerjanjianGirik>
	{
	}
#endif

	public class cmnProsesPBT : cmnBase
	{
		public OrderBPN order { get; set; }
		public NIB hasil { get; set; }
		public Rfp byrSPS { get; set; }
		public DateTime? tglTerimaPPh { get; set; }
	}

	public class cmnProsesSPH : cmnBase
	{
		public OrderNotaris order { get; set; }
		public SPH hasil { get; set; }
		//public Payment bayarPPh { get; set; }
		public DateTime? kirimKePusat { get; set; } = null;

		public DateTime? terimaDrPusat { get; set; } = null;
		//		public Rfp rfpValidasi { get; set; }
		//#if (_INIT_MONGO_)
		//			= new Rfp();
		//#endif
		public DateTime? kirimKePemilik { get; set; } = null;
		public OrderNotaris orderNotaris { get; set; }
		public DateTime? minuta { get; set; }
	}

	public class cmnProsesMohonSK : cmnBase
	{
		public OrderBPN order { get; set; }
		public SKBPN hasil { get; set; }
	}

	public class cmnProsesMohonSKKanwil : cmnProsesMohonSK
	{
		public Rfp rfpSPS { get; set; }
		public Rfp rfpBPHTB { get; set; }
		public Rfp rfpValBPHTB { get; set; }
	}

	public class cmnProsesMohonSKKantah : cmnProsesMohonSKKanwil
	{
		public Rfp biayaSaksi { get; set; }
		public Rfp biayaLurah { get; set; }
		public Rfp rfpBiayaSurvei { get; set; }
	}

	public class cmnProsesSertifikat : cmnBase
	{
		public class Pengumuman
		{
			public double? luas { get; set; }
			public DateTime? tanggal { get; set; }
		}

		public Pengumuman pengumuman { get; set; } = new Pengumuman();
	}

	public class cmnProsesCetakBuku : cmnProsesSertifikat
	{
		public OrderBPN order { get; set; }

		public HGB hasil { get; set; }
		public Rfp bayarSPS { get; set; }
	}

	public class cmnProsesCetakBukuSHM : cmnProsesSertifikat
	{
		public OrderBPN order { get; set; }

		public Sertifikat hasil { get; set; }
		public Rfp bayarSPS { get; set; }
	}

	public class cmnProsesSHM : cmnProsesSertifikat
	{
		public Sertifikat hasil { get; set; }
	}

	//public class cmnProsesCetakBuku : cmnBase
	//{
	//	public class Pengumuman
	//	{
	//		public double? luas { get; set; }
	//		public DateTime? tanggal { get; set; }
	//	}

	//	public OrderBPN order { get; set; }
	//	public Pengumuman pengumuman { get; set; } = new Pengumuman();

	//	public HGB hasil { get; set; }
	//	public Rfp bayarSPS { get; set; }
	//}

	public class cmnProsesTurunHak : cmnBase
	{
		public OrderNotaris orderNot { get; set; }
		public OrderBPN orderBPN { get; set; }
		public HGB hasil { get; set; }
		public Rfp bayarSPS { get; set; }

		public DateTime? tglPPh { get; set; }
		public double? nilaiBPHTB { get; set; }
		public DateTime? tglBPHTB { get; set; }
	}

	public class cmnProsesNaikHak : cmnProsesTurunHak
	{
	}

	public class cmnProsesAJB : cmnBase
	{
		public OrderNotaris order { get; set; }
		public AJB hasil { get; set; }
	}

	public class cmnProsesBalikNama : cmnBase
	{
		public OrderNotaris orderNot { get; set; }
		public OrderBPN orderBPN { get; set; }
		public HGB hasil { get; set; }
		public string keyPTAsal { get; set; }
		public string keyPTTujuan { get; set; }

		public Rfp bayarPPh { get; set; }
		public DateTime? valPPh { get; set; }
		public Rfp bayarBPHTB { get; set; }
		public DateTime? valBPHTB { get; set; }
		public Rfp daftarPNBP { get; set; }
	}

	public class cmnMasukAJB : cmnBase
	{
		public AJB hasil { get; set; }
	}

	public class cmnPembayaranPajak : cmnBase
	{
		public class Pajak
		{
			public double? jumlah { get; set; }
			public DateTime? validasi { get; set; }
		}

		public Pajak PPh { get; set; } = new Pajak();
		public Pajak BPHTB { get; set; } = new Pajak();
	}
}
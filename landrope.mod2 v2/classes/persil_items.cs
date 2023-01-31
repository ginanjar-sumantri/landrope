#define VER_15
using auth.mod;
using DynForm.shared;
using landrope.common;
//using landrope.mcommon;
//using landrope.layout;
using landrope.mod.shared;
//using Microsoft.CodeAnalysis.CSharp;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace landrope.mod2
{
    public class PersilBasic : ValidatableItem
    {
        public JenisProses en_proses { get; set; }
        [BsonIgnore]
        public string proses
        {
            get => en_proses.ToString("g");
            set { if (Enum.TryParse<JenisProses>(value, out JenisProses jp)) en_proses = jp; }
        }
        public JenisLahan en_lahan { get; set; }
        [BsonIgnore]
        public string lahan
        {
            get => en_lahan.ToString("g");
            set { if (Enum.TryParse<JenisLahan>(value, out JenisLahan jl)) en_lahan = jl; }
        }
        public JenisAlasHak en_jenis { get; set; } = JenisAlasHak.unknown;
        [BsonIgnore]
        public string jenis
        {
            get => en_jenis.ToString("g");
            set { if (Enum.TryParse<JenisAlasHak>(value, out JenisAlasHak jb)) en_jenis = jb; }
        }
        public SifatBerkas en_status { get; set; }
        [BsonIgnore]
        public string status
        {
            get => en_status.ToString("g");
            set { if (Enum.TryParse<SifatBerkas>(value, out SifatBerkas sb)) en_status = sb; }
        }
        public string keyParent { get; set; }
        public string IdBidangParent { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string noPeta { get; set; }
        public string keyPenampung { get; set; }
        public string keyPTSK { get; set; }
        public OrderNotaris order { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? terimaBerkas { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false)]
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
        public double? luasNIBTemp { get; set; }
        public double? luasDibayar { get; set; }
        public DateTime? deal { get; set; }
        public string dealer { get; set; }
        public DateTime? dealSystem { get; set; }
        public string reason { get; set; }

        //private double? GetLuasNIB()
        //      {
        //	var bhost = ContextService.services.GetService<>
        //      }
        public string NOP_PBB { get; set; }
        public string kekurangan { get; set; }
        public string alias { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkte { get; set; }
        public double? total { get; set; }
        public int? tahap { get; set; }

        [BsonIgnore]
        [JsonIgnore]
        public double? dtahap
        {
            get => (double?)tahap;
            set { tahap = value == null ? (int?)null : (int)Math.Truncate((decimal)(value.Value)); }
        }
        public string note { get; set; }
        public RiwayatArsipBasic arsip { get; set; }

    }

#if VER_15
    public class ProsesPerjanjian : ValidatableItem
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

    public class ProsesPBT<T> : ValidatableItem where T : NIB
    {
        public OrderBPN order { get; set; }
        public T hasil { get; set; }
        public Rfp byrSPS { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? tglTerimaPPh { get; set; }
    }

    public class ProsesSPH : ValidatableItem
    {
        public OrderNotaris order { get; set; }
        public SPH hasil { get; set; }
        //public Payment bayarPPh { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false  /*DateOnly = false /*true */)]
        public DateTime? kirimKePusat { get; set; } = null;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false  /*DateOnly = false /*true */)]
        public DateTime? terimaDrPusat { get; set; } = null;
        //		public Rfp rfpValidasi { get; set; }
        //#if (_INIT_MONGO_)
        //			= new Rfp();
        //#endif
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? kirimKePemilik { get; set; } = null;
        //public OrderNotaris orderNotaris { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? minuta { get; set; }
    }

    public class ProsesMohonSK : ValidatableItem
    {
        public OrderBPN order { get; set; }
        public SKBPN hasil { get; set; }
    }

    public class ProsesMohonSKKanwil : ProsesMohonSK
    {
        public Rfp rfpSPS { get; set; }
        public Rfp rfpBPHTB { get; set; }
        public Rfp rfpValBPHTB { get; set; }
    }

    public class ProsesMohonSKKantah : ProsesMohonSKKanwil
    {
        public Rfp biayaSaksi { get; set; }
        public Rfp biayaLurah { get; set; }
        public Rfp rfpBiayaSurvei { get; set; }
    }

    public class ProsesSertifikat : ValidatableItem
    {
        public class Pengumuman
        {
            [BundlePropMap("JDOK034", MetadataKey.Luas, Dynamic.ValueType.Number)]
            public double? luas { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
            [BundlePropMap("JDOK034", MetadataKey.Tanggal, Dynamic.ValueType.Date)]
            public DateTime? tanggal { get; set; }
        }

        public Pengumuman pengumuman { get; set; } = new Pengumuman();
    }

    public class ProsesCetakBuku<T> : ProsesSertifikat where T : Sertifikat
    {
        public OrderBPN order { get; set; }

        public T hasil { get; set; }
        public Rfp bayarSPS { get; set; }
    }

    public class ProsesCetakBukuHGB : ProsesCetakBuku<HGB_Final> { }
    public class ProsesCetakBukuSHM : ProsesCetakBuku<SHM> { }

    public class ProsesSHM : ProsesSertifikat
    {
        public SHM hasil { get; set; }
    }

    public class ProsesTurunHak : ValidatableItem
    {
        public OrderNotaris orderNot { get; set; }
        public OrderBPN orderBPN { get; set; }
        public HGB hasil { get; set; }
        public Rfp bayarSPS { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? tglPPh { get; set; }
        public double? nilaiBPHTB { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? tglBPHTB { get; set; }
    }

    public class ProsesNaikHak : ProsesTurunHak
    {
    }

    public class ProsesBalikNama : ValidatableItem
    {
        public OrderNotaris orderNot { get; set; }
        public OrderBPN orderBPN { get; set; }
        public HGB_Final hasil { get; set; }
        public string keyPTAsal { get; set; }
        public string keyPTTujuan { get; set; }

        public Rfp bayarPPh { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? valPPh { get; set; }
        public Rfp bayarBPHTB { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
        public DateTime? valBPHTB { get; set; }
        public Rfp daftarPNBP { get; set; }
    }

    public class MasukAJB : ValidatableItem
    {
        public AJB hasil { get; set; }
        //public Pajak PPh { get; set; } = new Pajak();
        //public Pajak BPHTB { get; set; } = new Pajak();
    }

    public class ProsesAJB : MasukAJB
    {
        public OrderNotaris order { get; set; }
    }

    public class PembayaranPajak : ValidatableItem
    {

        public Pajak PPh { get; set; } = new Pajak();
        public Pajak BPHTB { get; set; } = new Pajak();
    }
}
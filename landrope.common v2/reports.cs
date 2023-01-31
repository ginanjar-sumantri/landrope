//using landrope.mcommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.common
{
    public class RptBase
    {
        public string project { get; set; }
        public string desa { get; set; }
        public string PT { get; set; }
        public double dibayar { get; set; }
        public double hgbpt { get; set; }
    }

    public class RptSerti : RptBase
    {
        public double surat { get; set; }
        public double shm { get; set; }
        public double turun { get; set; }
        public double hgb { get; set; }
        public double ajb { get; set; }
        public double balik { get; set; }
    }


    public class RptGirik : RptBase
    {
        public double nonib { get; set; }
        public double nibor { get; set; }
        public double nibpt { get; set; }
        public double kanta { get; set; }
        public double kanwil { get; set; }
        public double buku { get; set; }
    }


    public class RptHibah : RptBase
    {
        public double surat { get; set; }
        public double nonib { get; set; }
        public double nibor { get; set; }
        public double notif { get; set; }
        public double shm { get; set; }
        public double turun { get; set; }
        public double hgb { get; set; }
        public double ajb { get; set; }
        public double balik { get; set; }
    }

    public class RptBudgetBase
    {
        public bool masukBpn { get; set; }
        public bool girik { get; set; }
    }

    public class RptBudgetCommon : RptBudgetBase
    {
        public string project { get; set; }
        public DocProcessStep head { get; set; }
        public double price { get; set; }
        public int bidang { get; set; }
        public double luas { get; set; }
        public double amount { get; set; }
    }

    public class RptBudgetSummary : RptBudgetBase
    {
        public int bidang { get; set; }
        public double luas { get; set; }
        public double amount { get; set; }
    }
    public class RptBudget1 : RptBudgetSummary
    {
        public string project { get; set; }

        public static RptBudget1[] FromCommon(RptBudgetCommon[] data) =>
            data.GroupBy(d => (d.masukBpn, d.girik, d.project)).
            Select(g => new RptBudget1
            {
                project = g.Key.project,
                masukBpn = g.Key.masukBpn,
                girik = g.Key.girik,
                bidang = g.Sum(d => d.bidang),
                luas = g.Sum(d => d.luas),
                amount = g.Sum(d => d.amount)
            }).ToArray();
    }

    public class RptBudget2 : RptBudgetSummary
    {
        public DocProcessStep head { get; set; }
        public double price { get; set; }

        public static RptBudget2[] FromCommon(RptBudgetCommon[] data) =>
            data.GroupBy(d => (d.masukBpn, d.girik, d.head))
                .Select(g => new RptBudget2
                {
                    masukBpn = g.Key.masukBpn,
                    girik = g.Key.girik,
                    head = g.Key.head,
                    price = g.First().price,
                    bidang = g.Sum(d => d.bidang),
                    luas = g.Sum(d => d.luas),
                    amount = g.Sum(d => d.amount)
                }).ToArray();
    }

    public class RptOrderNotary
    {
        public string pengirim { get; set; }
        public string nomorPenugasan { get; set; }
        public DateTime? tanggalPenugasan { get; set; }
        public string perihal { get; set; }
        public string notaris { get; set; }
        public string desa { get; set; }
        public lampiranSurat[] lampiran { get; set; } = new lampiranSurat[0];
        public signSurat[] signs { get; set; } = new signSurat[0];
    }
    public class lampiranSurat
    {
        public string namaPemilik { get; set; }
        public string exSHM { get; set; }
        public string noShgb { get; set; }
        public double? luas { get; set; }
        public double? hargaJual { get; set; }
        public double? nilaiTransaksi { get; set; }
    }

    public class RptSuratTugas
    {
        public DateTime? tanggalPenugasan { get; set; }
        public string nomorPenugasan { get; set; }
        public string penerima { get; set; }
        public string[] tembusan { get; set; }
        public string jenisPenugasan { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public int jumlahBidang { get; set; }
        public double luasSurat { get; set; }
        public signSurat[] signs { get; set; }
    }

    public class signSurat
    {
        public int signOrder { get; set; }
        public string signDescription { get; set; }
        public string signName { get; set; }

        public void FillSigns(int order, string desc, string name)
        {
            (signOrder, signDescription, signName)
                =
            (order, desc, name);
        }
    }

    public class StaticInfo
    {
        public string penerima { get; set; }
        public string tembusan { get; set; }
        public string hormatKami { get; set; }
        public string approvedBy { get; set; }
    }
    public class RptStatusPerBayar
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public int NomorTahap { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasInternal { get; set; }
        public double? Jumlah { get; set; }
        public JenisBayar JenisPembayaran { get; set; }
        public string Step { get; set; }
        public DateTime? tanggalPembayaran { get; set; }
    }

    public class RptStatusPerBayarCSV
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public int NomorTahap { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasInternal { get; set; }
        public double? Jumlah { get; set; }
        public JenisBayar JenisPembayaran { get; set; }
        public string Step { get; set; }
        public string tanggalPembayaran { get; set; }
    }

    public class RptStatusPerDeal
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string AlasHak { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public int Tahap { get; set; }
        public string StatusBidang { get; set; }
        public string StatusDeal { get; set; }
    }
    public class RptSisaLuasOverlap
    {
        public string IdBidang { get; set; }
        public string Bebas { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NoPeta { get; set; }
        public string NoSurat { get; set; }
        public double? LuasSurat { get; set; }
        public double? SisaLuas { get; set; }
        public string Group { get; set; }
        public int? Tahap { get; set; }
        public string NamaDiSurat { get; set; }
    }

    public class RptLogBundle
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NoPeta { get; set; }
        public string AlasHak { get; set; }
        public string JenisDokumen { get; set; }
        public DateTime? created { get; set; }
        public string activityType { get; set; }
        public string activityModul { get; set; }
        public string user { get; set; }
    }

    public class RptLogBundleCsv
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NoPeta { get; set; }
        public string AlasHak { get; set; }
        public string JenisDokumen { get; set; }
        public string created { get; set; }
        public string activityType { get; set; }
        public string activityModul { get; set; }
        public string user { get; set; }
    }

    public class RptDetailLunasBidangView
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public int Tahap { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasDibayar { get; set; }
        public double? HargaSatuan { get; set; }
        public double? HargaAkta { get; set; }
        public double? HargaTotal { get; set; }
        public double? SisaPelunasan { get; set; }
        public string StatusPembayaran { get; set; }
        public DateTime? TglBayar { get; set; }
    }

    public class RptKategoriBidang
    {
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Pemilik { get; set; }
        public string Group { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasInternal { get; set; }
        public string statusBebas { get; set; }
        public string Kategori { get; set; }
    }

    public class RptTandaTerimaNotarisBase
    {
        public string Identity { get; set; }
        public int orderHeader { get; set; }
    }

    public class RptTandaTerimaNotaris : RptTandaTerimaNotarisBase
    {
        public ReportDetail[] Details { get; set; }
    }

    public class ReportDetail : RptTandaTerimaNotarisBase
    {
        public ReportDetail[] Details { get; set; }
        public string value { get; set; }

        public ReportDetail(string identity, string value)
        {
            this.Identity = identity;
            this.value = value;
        }
        public ReportDetail()
        {

        }
    }

    public class RptPivotPembayaranView
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public bool? isLunas { get; set; }
        public string Status { get; set; }
        public double? LuasBayar { get; set; }
    }

    public class BelumBebasDeal
    {
        public string DealStatus { get; set; }
        public string GroupType { get; set; }
        public string Group { get; set; }
        public string KeyProject { get; set; }
        public string Project { get; set; }
        public double? Value { get; set; }
    }

    public class BelumBebasNormatif
    {
        public string KeyCategory { get; set; }
        public string Category { get; set; }
        public string KeySubCategory { get; set; }
        public string SubCategory { get; set; }
        public string KeyGroup { get; set; }
        public string Group { get; set; }
        public string KeyPemilik { get; set; }
        public string Pemilik { get; set; }
        public double? Luas { get; set; }
        public double? LuasM2 { get; set; }
        public string Description { get; set; }
        public string KeyPIC { get; set; }
        public string PIC { get; set; }
        public string KeySales { get; set; }
        public string Sales { get; set; }
        public string KeyPersil { get; set; }
        public string IdBidang { get; set; }
        public string KeyProject { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NoPeta { get; set; }
    }

    public class BelumBebasNormatifFishBone : BelumBebasNormatif
    {
        public string KeyCategory { get; set; }
    }

    public class BelumBebasNormatifReport : BelumBebasNormatif
    {
        public int Index { get; set; }
    }

    public class BelumBebasNormatifSubCategory : BelumBebasNormatifFishBone
    {
        public string KeySubCategory { get; set; }
    }

    public class BelumBebasOverlap
    {
        public int index { get; set; }
        public string keyCategory { get; set; }
        public string category { get; set; }
        public string keySubCategory { get; set; }
        public string subCategory { get; set; }
        public string keyPersil { get; set; }
        public string idBidang { get; set; }
        public string keyGroup { get; set; }
        public string group { get; set; }
        public double? luas { get; set; }
        public string pic { get; set; }
        public string keterangan { get; set; }
        public double? alasHak { get; set; }
        public double? nib { get; set; }
        public double? pengumuman { get; set; }
        public double? shm { get; set; }

        public BelumBebasOverlap SetIndex(int i)
        {
            this.index = i;
            return this;
        }
    }

    public class BelumBebasGroupKecilMSSummary
    {
        public string BaseUrl { get; set; }
        public string KeySubCategory { get; set; }
        public string SubCategory { get; set; }
        public string BukaHarga { get; set; }
        public string KeyPIC { get; set; }
        public string PIC { get; set; }
        public int JumlahOrang { get; set; }
        public int JumlahBidang { get; set; }
        public double? JumlahLuas { get; set; }
        public bool IsBukaHarga { get; set; }
    }

    public class BelumBebasGroupKecilMSPerBidang
    {
        public string KeySubCategory { get; set; }
        public string SubCategory { get; set; }
        public string keyPIC { get; set; }
        public string PIC { get; set; }
        public string _t { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NomorPeta { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasSuratHektar { get; set; }
        public string NamaGroup { get; set; }
        public string NamaPemilik { get; set; }
        public string Kategory { get; set; }
        public string Sales { get; set; }
        public string HasilFollowUp { get; set; }
    }

    public class BelumBebasGroupKecilSudahKetemu
    {
        public string Segment1 { get; set; }
        public string Segment6 { get; set; }
        public string keyPIC { get; set; }
        public string Kategory { get; set; }
        public double? Luas { get; set; }
        public double? LuasPencapaian { get; set; }
        public double? Persentase { get; set; }
        public double? SisaTarget { get; set; }
    }

    public class BelumBebasNormatifGroupKecilNoNameSimple
    {
        public string KeyPIC { set; get; }
        public string PIC { set; get; }
        public double? Luas { get; set; }
        public double? Pencapaian { get; set; }
        public double? PencapaianPersentase { get; set; }
        public double? SisaTarget { get; set; }
    }


    public class SuratTugas1Dim
    {
        public bool IsPreview { get; set; } = false;
        public string JenisSurat { get; set; }
        public string TitleSurat { get; set; }
        public DateTime? TanggalSurat { get; set; }
        public string PIC { get; set; }
        public string NomorSurat { get; set; }
        public string Project { get; set; }
        public string PTSK { get; set; }
        public string Desa { get; set; }
        public int Total { get; set; }
        public double TotalLuas { get; set; }
        public string Signers1 { get; set; }
        public string Signers2 { get; set; }
        public string Signers3 { get; set; }
        public string Base64QR { get; set; }
    }

    public class SuratTugas2Dim
    {
        public string text { get; set; }
    }

    public class SuratTugasBidangSummary
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Seller { get; set; }
        public string PPJBTo { get; set; }
        public int Tahap { get; set; }
        public string AlasHak { get; set; }
        public double NilaiBPHTB { get; set; }
        public string NomorPBTPT { get; set; }
        public string NomorPengantarSK { get; set; }
        public string NomorSK { get; set; }
        public string NomorSPH { get; set; }
        public string Shm { get; set; }
        public string Shgb { get; set; }
        public string NomorSertifikat { get; set; }
        public double LuasPBTPT { get; set; }
        public double LuasPBTPerorang { get; set; }
        public double LuasSurat { get; set; }
        public double Satuan { get; set; }
        public double NilaiTransaksi { get; set; }
        public int JumlahBidang { get; set; }
        public string PTSK { get; set; }
    }

    public class SuratTugas
    {
        public SuratTugas2Dim[] Notes { get; set; }
        public SuratTugas2Dim[] Perihal  { get; set; }
        public SuratTugas1Dim[] Data { get; set; }
        public SuratTugasBidangSummary[] SummaryBidang { get; set; }
    }

    public class SuratTugasStatic
    {
        public string[] tembusan { get; set; } = new string[0];
        public string[] perihal { get; set; } = new string[0];
        public string[] signs { get; set; } = new string[0];
        public string[] notes { get; set; } = new string[0];
    }

    public class ViewSpkReport
    {
        public string IdBidang { get; set; }
        public string Group { get; set; }
        public string Nama { get; set; }
        public string AlasHak { get; set; }
        public string Desa { get; set; }
        public string LuasSurat { get; set; }
        public string LuasUkur { get; set; }
        public string NoPeta { get; set; }
        public string Notaris { get; set; }
        public string Project { get; set; }
        public string Ptsk { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
    }
}
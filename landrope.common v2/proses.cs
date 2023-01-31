using flow.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.common
{

    public class SertifikasiView
    {
        public string nomorRFD { get; set; }

        public string type { get; set; }

        public string subtype { get; set; }

        public DateTime? tanggaldiBuat { get; set; }

        public string keterangan { get; set; }

        public string creator { get; set; }

    }

    public class PajakView
    {
        public string nomorRFD { get; set; }

        public string type { get; set; }

        public string subtype { get; set; }

        public DateTime? tanggaldiBuat { get; set; }

        public string keterangan { get; set; }

        public string creator { get; set; }

    }

    public class ProsesView
    {
        public string key { get; set; }

        public string instkey { get; set; }

        public string keyType { get; set; }

        public string keyProject { get; set; }

        public string keyDesa { get; set; }

        public string keyPTSK { get; set; }

        public string keyAktor { get; set; }

        public string project { get; set; }

        public string desa { get; set; }

        public string ptsk { get; set; }

        public string aktor { get; set; }

        public string nomorRFP { get; set; }

        public double? TotalNominal { get; set; }

        public string proses { get; set; }

        public string type { get; set; }

        public DateTime tanggaldiBuat { get; set; }

        public string keterangan { get; set; }

        public string creator { get; set; }

        public string status { get; set; }

        public ToDoState state { get; set; }

        public bool? isAttachment { get; set; }

        public DateTime? ExpiredDate { get; set; }

        public ReasonCore[] reasons { get; set; } = new ReasonCore[0];

    }

    public class ProsesViewExt : ProsesView
    {
        public DateTime? created { get; set; }

        public DateTime? issued { get; set; }

        public string issuer { get; set; }

        public DateTime? verify1 { get; set; }

        public DateTime? verify2 { get; set; }

        public DateTime? verify3 { get; set; }

        public DateTime? verifLegal { get; set; }

        public DateTime? review { get; set; }

        public DateTime? accounting { get; set; }

        public DateTime? cashier { get; set; }

        public DateTime? final { get; set; }
    }

    public class ProsesViewExtGraph : ProsesViewExt
    {
        public ToDoState state { get; set; }

        public string status { get; set; }

        public DateTime? statustm { get; set; }

        public bool isCreator { get; set; }
        public routes[] rou { get; set; } = new routes[0];
        public ProsesViewExtGraph SetState(ToDoState state) { this.state = state; return this; }
        public ProsesViewExtGraph SetStatus(string status, DateTime? time) { (this.status, this.statustm) = (status, time); return this; }
        public ProsesViewExtGraph SetCreator(bool IsCreator) { this.isCreator = IsCreator; return this; }
        public ProsesViewExtGraph SetMilestones(DateTime? cre, DateTime? iss, DateTime? ver1, DateTime? ver2, DateTime? ver3, DateTime? lel, DateTime? accGm, DateTime? accAcc, DateTime? cash, DateTime? clo)
        {
            (this.created, this.issued, this.verify1, this.verify2, this.verify3, this.verifLegal, this.review, this.accounting, this.cashier, this.final) =
                (cre, iss, ver1, ver2, ver3, lel, accGm, accAcc, cash, clo); ; return this;
        }

        public static ProsesViewExtGraph Upgrade(ProsesViewExt old)
            => System.Text.Json.JsonSerializer.Deserialize<ProsesViewExtGraph>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );

        public ProsesViewExtGraph SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
        {
            var lst = new List<routes>();
            foreach (var item in routes)
            {
                var r = new routes();
                r.routekey = item.key;
                r.todo = item.todo;
                r.verb = item.verb;
                r.cmds = item.cmds;

                lst.Add(r);
            }

            rou = lst.ToArray();

            return this;
        }
    }

    public class SertifikasiDtlView
    {
        public string key { get; set; }
        public string keyProses { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string alashak { get; set; }
        public string pemilik { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public double? luasSurat { get; set; }
        public double? luasInternal { get; set; }
        public double? luasDiBayar { get; set; }
        public double? luasPBT { get; set; }
        public double? luasNIBBidang { get; set; }
        public string nomorSK { get; set; }
        public bool? isAttachment { get; set; }
        public SertifikasiDtlView setBidang(string IdBidang, string pemilik, double? luasSurat, double? luasInternal, double? luasDiBayar,
                                            string alashak, double? satuan, double? satuanAkta, double? luasNIB, List<(string keypersil, double? luas, string sk)> bundles)
        {
            if (IdBidang != null)
                this.IdBidang = IdBidang;
            if (pemilik != null)
                this.pemilik = pemilik;
            if (luasSurat != null)
                this.luasSurat = luasSurat;
            if (luasInternal != null)
                this.luasInternal = luasInternal;
            if (luasDiBayar != null)
                this.luasDiBayar = luasDiBayar;
            if (alashak != null)
                this.alashak = alashak;
            if (satuan != null)
                this.satuan = satuan;
            if (satuanAkta != null)
                this.satuanAkta = satuanAkta;
            if (luasNIB != null)
                this.luasNIBBidang = luasNIB;

            this.luasPBT = bundles.FirstOrDefault(x => x.keypersil == keyPersil).luas ?? 0;
            this.nomorSK = bundles.FirstOrDefault(x => x.keypersil == keyPersil).sk ?? "";

            return this;
        }
        public SertifikasiDtlView setBundle(double? luasPBT, string nomorSK)
        {
            if (luasPBT != null)
                this.luasPBT = luasPBT;
            if (nomorSK != null)
                this.nomorSK = nomorSK;
            return this;
        }
    }

    public class SertifikasiDtlSubTypeView
    {
        public string keySubType { get; set; }
        public string subType { get; set; }
        public string nomorAkta { get; set; }
        public DateTime? tanggalAkta { get; set; }
        public double? biayaPerBuku { get; set; }
        public double? totalHarga { get; set; }
        public string aktor { get; set; }
        public string nama { get; set; }
        public double? patok { get; set; }
        public double? satuan { get; set; }
        public double? budget { get; set; }
        public double? nominal { get; set; }
        public string namaInternal { get; set; }
        public string keterangan { get; set; }
        public bool? isAttachment { get; set; }
        public string jenisDokumen { get; set; }
        public int? jumlahAkte { get; set; }
        public SertifikasiDtlSubTypeView setType(string subtype)
        {
            if (subtype != null)
                this.subType = subtype;


            return this;
        }
    }

    public class PajakDtlView
    {
        public string key { get; set; }
        public string keyProses { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string pemilik { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public double? luasSurat { get; set; }
        public double? luasInternal { get; set; }
        public double? luasDiBayar { get; set; }
        public string alashak { get; set; }
        public double? luasPBT { get; set; }
        public double? LuasNIBBidang { get; set; }
        public string nomorSK { get; set; }
        public bool? isAttachment { get; set; }
        public bool? PaidByOwner { get; set; }
        public string reasonPBO { get; set; }
        public PajakDtlView setBidang(string IdBidang, string pemilik, double? luasSurat, double? luasInternal, double? luasDiBayar,
                                            string alashak, double? satuan, double? satuanAkta, double? luasNIB, List<(string keypersil, double? luas, string sk)> bundles)
        {
            if (IdBidang != null)
                this.IdBidang = IdBidang;
            if (pemilik != null)
                this.pemilik = pemilik;
            if (luasSurat != null)
                this.luasSurat = luasSurat;
            if (luasInternal != null)
                this.luasInternal = luasInternal;
            if (luasDiBayar != null)
                this.luasDiBayar = luasDiBayar;
            if (alashak != null)
                this.alashak = alashak;
            if (satuan != null)
                this.satuan = satuan;
            if (satuanAkta != null)
                this.satuanAkta = satuanAkta;
            if (luasNIB != null)
                this.LuasNIBBidang = luasNIB;

            this.luasPBT = bundles.FirstOrDefault(x => x.keypersil == keyPersil).luas ?? 0;
            this.nomorSK = bundles.FirstOrDefault(x => x.keypersil == keyPersil).sk ?? "";

            return this;
        }

        public PajakDtlView setBundle(double? luasPBT, string nomorSK)
        {
            if (luasPBT != null)
                this.luasPBT = luasPBT;
            if (nomorSK != null)
                this.nomorSK = nomorSK;
            return this;
        }
    }

    public class PajakDtlViewExt : PajakDtlView
    {
        public string routekey { get; set; }
        public ToDoState state { get; set; }
        public string status { get; set; }
        public DateTime? statustm { get; set; }
        public ToDoVerb verb { get; set; }
        public string todo { get; set; }
        public ToDoControl[] cmds { get; set; } = new ToDoControl[0];

        public PajakDtlViewExt SetRoute(string key) { this.routekey = key; return this; }
        public PajakDtlViewExt SetState(ToDoState state) { this.state = state; return this; }
        public PajakDtlViewExt SetStatus(string status, DateTime? time) { (this.status, this.statustm) = (status, time); return this; }
        public PajakDtlViewExt SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
        public PajakDtlViewExt SetTodo(string todo) { this.todo = todo; return this; }
        public PajakDtlViewExt SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
        public PajakDtlViewExt SetAttributes(string routekey, ToDoState state, DateTime? time,
                                                                    ToDoVerb verb, string todo, ToDoControl[] cmds)
        {
            (this.routekey, this.state, this.status, this.statustm,
                this.verb, this.todo, this.cmds) =
            (routekey, state, state.AsStatus(), time, verb, todo, cmds);
            return this;
        }

        public static PajakDtlViewExt Upgrade(PajakDtlView old)
            => System.Text.Json.JsonSerializer.Deserialize<PajakDtlViewExt>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );

        public PajakDtlViewExt setBidang(string IdBidang, string pemilik, double? luasSurat, double? luasInternal, double? luasDiBayar,
                                            string alashak, double? satuan, double? satuanAkta)
        {
            if (IdBidang != null)
                this.IdBidang = IdBidang;
            if (pemilik != null)
                this.pemilik = pemilik;
            if (luasSurat != null)
                this.luasSurat = luasSurat;
            if (luasInternal != null)
                this.luasInternal = luasInternal;
            if (luasDiBayar != null)
                this.luasDiBayar = luasDiBayar;
            if (alashak != null)
                this.alashak = alashak;
            if (satuan != null)
                this.satuan = satuan;
            if (satuanAkta != null)
                this.satuanAkta = satuanAkta;

            return this;
        }

        public PajakDtlViewExt setBundle(double? luasPBT, string nomorSK)
        {
            if (luasPBT != null)
                this.luasPBT = luasPBT;
            if (nomorSK != null)
                this.nomorSK = nomorSK;
            return this;
        }
    }

    public class PajakDtlSubTypeView
    {
        public string keySubType { get; set; }
        public string subType { get; set; }
        public double? NJOP { get; set; }
        public double? nominal { get; set; }
        public string keterangan { get; set; }
        public double? NPOPTKP { get; set; }
        public string tahunPengaktifan { get; set; }
        public bool? isAttachment { get; set; }
        public double? nominalPBO { get; set; }
        public string reasonPBO { get; set; }
        public ValidasiView validasi { get; set; }
        public PinaltiView[] pinaltis { get; set; } = new PinaltiView[0];
        public PajakDtlSubTypeView setType(string subtype)
        {
            if (subtype != null)
                this.subType = subtype;


            return this;
        }
    }

    public class ValidasiView
    {
        public DateTime? tanggaldiBuat { get; set; }
        public DateTime? tanggaldiKirim { get; set; }
        public DateTime? tanggalSelesai { get; set; }
        public string BPN_Notaris { get; set; }
        public string keterangan { get; set; }
    }

    public class PinaltiView
    {
        public string tahun { get; set; }
        public double? denda { get; set; }
        public double? nominal { get; set; }
        public double? total { get; set; }
    }

    public class ProsesCore
    {
        public string key { get; set; }
        public string proses { get; set; }
        public string nomorRFP { get; set; }
        public string type { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string keyAktor { get; set; }
        public string keterangan { get; set; }
        public DateTime? ExpiredDate { get; set; }

        public bool? isDetail { get; set; }
    }

    public class ProsesDetailCore
    {
        public PersilProsesCore[] persil { get; set; }

        public SertifikasiDetailCore sertifikasi { get; set; }

        public PajakDetailCore pajak { get; set; }
    }

    public class PersilProsesCore
    {
        public string keypersil { get; set; }
        public double? luasNIBBidang { get; set; }
    }

    public class AktaCore
    {
        public string nomorAkta { get; set; }
        public DateTime? tanggalAkta { get; set; }
        public double? biayaPerBuku { get; set; }
        public double? totalHarga { get; set; }

    }

    public class SertifikasiDetailCore
    {
        public string subType { get; set; }
        public AktaCore akta { get; set; } = new AktaCore();
        public double? luas_Patok { get; set; }
        public double? satuan { get; set; }
        public double? budget { get; set; }
        public double? nominal { get; set; }
        public string jenisDokumen { get; set; }
        public int? jumlahAkte { get; set; }
        public string keterangan { get; set; }
    }

    public class PajakDetailCore
    {
        public string subType { get; set; }
        public double? NJOP { get; set; }
        public double? NPOPTKP { get; set; }
        public string tahunPengaktifan { get; set; }
        public double? nominal { get; set; }
        public string keterangan { get; set; }

        public ValidasiCore validasi { get; set; } = new ValidasiCore();

        public PinaltiCore[] pinaltis { get; set; } = new PinaltiCore[0];
    }

    public class ValidasiCore
    {
        public DateTime? tanggaldiBuat { get; set; }
        public DateTime? tanggaldiKirim { get; set; }
        public DateTime? tanggalSelesai { get; set; }
        public string BPN_Notaris { get; set; }
        public string keterangan { get; set; }
    }

    public class PinaltiCore
    {
        public string tahun { get; set; }
        public double? denda { get; set; }
        public double? nominal { get; set; }
        public double? total { get; set; }
    }

    public class PersilProses
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public int? en_state { get; set; }
        public string alasHak { get; set; }
        public string noPeta { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string states { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public double? luasSurat { get; set; }
        public double? luasInternal { get; set; }
        public double? luasDiBayar { get; set; }
        public double? luasPBT { get; set; }
        public string SK { get; set; }
        public string group { get; set; }
        public string pemilik { get; set; }

        public PersilProses setBundle(double? luasPBT, string SK)
        {
            if (luasPBT != null)
                this.luasPBT = luasPBT;
            if (SK != null)
                this.SK = SK;

            return this;
        }
    }

    public class ProsesType
    {
        public string key { get; set; }
        public string _t { get; set; }
        public string desc { get; set; }
        public bool repeatReq { get; set; }
        public ProsesSubType[] subType { get; set; } = new ProsesSubType[0];
    }

    public class ProsesSubType
    {
        public string key { get; set; }
        public string desc { get; set; }
        public bool repeatReq { get; set; }
    }

    public class ProsesCommand
    {
        public string key { get; set; }
        public string instkey { get; set; }
        public string rkey { get; set; }
        public string type { get; set; }
        public string proses { get; set; }
        public string reason { get; set; }
        public ToDoControl control { get; set; }
    }

    public class ProsesDtlCommand : ProsesCommand
    {
        public string dkey { get; set; }

        public ValidasiCore validasi { get; set; } = new ValidasiCore();
    }

    public class ProsesDtlPaidByOwner
    {
        public string keyDetail { get; set; }

        public string subtype { get; set; }

        public string reasonPBO { get; set; }

        public double nominalPBO { get; set; }
    }

    public interface ISertifikasi { }
    public interface IPajak { }
}

using flow.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace landrope.common
{
    public class BayarCore : CoreBase
    {
        public string key { get; set; }
        public JenisPersil jenisPersil { get; set; }
        public int nomorTahap { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string group { get; set; }
        public string keyPTSK { get; set; }
        public string keyPenampung { get; set; }
        public string keyCreator { get; set; }

        public BayarDtlBidangCore[] bidangs { get; set; }
    }
    public class BayarDtlCore : CoreBase
    {
        public string Tkey { get; set; }
        public string key { get; set; }
        public string keyPersil { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public string noMemo { get; set; }
        public double Jumlah { get; set; }
        public string NamaContactPerson { get; set; }
        public string TelpContactPerson { get; set; }
        public DateTime TanggalPenyerahan { get; set; }
        public double? Persentase { get; set; }
        public string Note { get; set; }
        public string Tembusan { get; set; }
        public string MemoSigns { get; set; }
        public ProposionalBayar? fgProposional { get; set; } //true : berdasarkan luas, false : Bagi rata
        public GiroCore[] pemecahanGiro { get; set; }
    }
    public class BayarDtlCoreExt : BayarDtlCore
    {
        public string keyProject { get; set; }
        public int noTahap { get; set; }
        public DateTime tglBayar { get; set; }

    }
    public class BayarDtlBidangCore : CoreBase
    {
        public string keyPersil { get; set; }
    }
    public class BayarDtlDepositCore : CoreBase
    {
        public string Tkey { get; set; }
        public string key { get; set; }
        public string? keyPersil { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public string noMemo { get; set; }
        public double Jumlah { get; set; }
        public string NamaContactPerson { get; set; }
        public string TelpContactPerson { get; set; }
        public DateTime TanggalPenyerahan { get; set; }
        public string Note { get; set; }
        public string Tembusan { get; set; }
        public string MemoSigns { get; set; }
        public BayarSubDtlDepositCore[] subdetails { get; set; } = new BayarSubDtlDepositCore[0];
        public GiroCore[] pemecahanGiro { get; set; } = new GiroCore[0];
    }
    public class BayarSubDtlDepositCore
    {
        public string keyPersil { get; set; }
        public double Jumlah { get; set; }
        public bool? pph21 { get; set; }
        public double? vpph21 { get; set; }
        public bool? ValidasiPPH { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? vValidasiPPH { get; set; }
        public bool? mandor { get; set; }
        public double? vMandor { get; set; }
        public bool? pembatalanNIB { get; set; }
        public double? vPembatalanNIB { get; set; }
        public bool? BiayaBN { get; set; }
        public double? vBiayaBN { get; set; }
        public bool? gantiBlanko { get; set; }
        public double? vGantiBlanko { get; set; }
        public bool? kompensasi { get; set; }
        public double? vKompensasi { get; set; }
        public bool? pajakLama { get; set; }
        public double? vPajakLama { get; set; }
        public bool? pajakWaris { get; set; }
        public double? vPajakWaris { get; set; }
        public bool? tunggakanPBB { get; set; }
        public double? vTunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayaLainnya { get; set; } = new BiayalainnyaCore[0];
    }
    public class BayarView
    {
        public string key { get; set; }
        public string instkey { get; set; }
        public int nomorTahap { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string keyPenampung { get; set; }
        public string group { get; set; }
        public int? jmlBidang { get; set; }
        public DateTime? created { get; set; }
        public string creator { get; set; }
        public BayarDtlXView[] details { get; set; }
        public BayarView SetLocation(string project, string desa)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            return this;
        }
    }
    public class BayarDtlXView
    {
        public string key { get; set; }
        public JenisBayar jenisBayar { get; set; }
    }
    public class BayarDtlXMemo : BayarDtlXView
    {
        public string Tkey { get; set; } //Tahap Key
        public string noMemo { get; set; }
        public string Instkey { get; set; }
    }
    public class DtltoBundle
    {
        public string key { get; set; }
        public ToDoState? state { get; set; }
    }
    public class BiayaCore : IValidatableObject
    {
        public string Tkey { get; set; } // BayarKey / Tahapkey
        public string Pkey { get; set; } //PersilKey
        public bool luasFix { get; set; }
        public string AlasHak { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? luasUkur { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public bool? pph21 { get; set; } // flag pph21 ditanggung penjual
        public bool? ValidasiPPH { get; set; } // flag validasi pph ditanggung penjual
        public double? ValidasiPPHValue { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? Mandor { get; set; }
        public double? PembatalanNIB { get; set; }
        public double? BalikNama { get; set; }
        public double? GantiBlanko { get; set; }
        public double? Kompensasi { get; set; }
        public double? PajakLama { get; set; }
        public double? PajakWaris { get; set; }
        public double? TunggakanPBB { get; set; }
        public bool? fgmandor { get; set; }
        public bool? fgpembatalanNIB { get; set; }
        public bool? fgbaliknama { get; set; }
        public bool? fggantiblanko { get; set; }
        public bool? fgkompensasi { get; set; }
        public bool? fgpajaklama { get; set; }
        public bool? fgpajakwaris { get; set; }
        public bool? fgtunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayalainnyaCore { get; set; } = new BiayalainnyaCore[0];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (satuan.GetValueOrDefault() < 1)
                yield return new ValidationResult("Harga satuan tidak dapat kosong.", new List<string> { "satuan" });
            if (satuanAkta.GetValueOrDefault() < 1)
                yield return new ValidationResult("Harga akta tidak dapat kosong.", new List<string> { "satuanAkta" });
            if (ValidasiPPH.GetValueOrDefault() && ValidasiPPHValue.GetValueOrDefault() < 1)
                yield return new ValidationResult("Pph validasi tidak dapat kosong.", new List<string> { "ValidasiPPHValue" });
            if (fgmandor.GetValueOrDefault() && Mandor.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya mandor tidak dapat kosong.", new List<string> { "Mandor" });
            if (fgpembatalanNIB.GetValueOrDefault() && PembatalanNIB.GetValueOrDefault() < 1)
                yield return new ValidationResult("Pembatalan NIB tidak dapat kosong.", new List<string> { "PembatalanNIB" });
            if (fgbaliknama.GetValueOrDefault() && BalikNama.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya balik nama tidak dapat kosong.", new List<string> { "BalikNama" });
            if (fggantiblanko.GetValueOrDefault() && GantiBlanko.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya ganti blanko tidak dapat kosong.", new List<string> { "GantiBlanko" });
            if (fgkompensasi.GetValueOrDefault() && Kompensasi.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya kompensasi tidak dapat kosong.", new List<string> { "Kompensasi" });
            if (fgpajaklama.GetValueOrDefault() && PajakLama.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya pajak lama tidak dapat kosong.", new List<string> { "PajakLama" });
            if (fgpajakwaris.GetValueOrDefault() && PajakWaris.GetValueOrDefault() < 1)
                yield return new ValidationResult("Biaya pajak waris tidak dapat kosong.", new List<string> { "PajakWaris" });
            if (fgtunggakanPBB.GetValueOrDefault() && TunggakanPBB.GetValueOrDefault() < 1)
                yield return new ValidationResult("Tunggakan PBB tidak dapat kosong.", new List<string> { "TunggakanPBB" });
            if (biayalainnyaCore.Any(x => x.nilai.GetValueOrDefault() < 1))
                yield return new ValidationResult("Biaya lainnya tidak dapat kosong.", new List<string> { "biayalainnyaCore" });
        }
    }
    public class GiroCore
    {
        public string key { get; set; }
        public JenisGiro? Jenis { get; set; }
        public double? Nominal { get; set; }
        public string NamaPenerima { get; set; }
        public string BankPenerima { get; set; }
        public string AccountPenerima { get; set; }

    }
    public class ReasonCore
    {
        public DateTime tanggal { get; set; }
        public ToDoState state { get; set; }
        public string state_ { get; set; }
        public string privs { get; set; }
        public string keyCreator { get; set; }
        public string creator { get; set; }
        public bool flag { get; set; } //True dari pembatalan, false dari reject
        public string description { get; set; }
    }
    public class BiayalainnyaCore
    {
        public string identity { get; set; }
        public double? nilai { get; set; }
        public bool? fglainnya { get; set; }

        public BiayalainnyaView ToView()
        {
            return new BiayalainnyaView
            {
                identity = this.identity,
                nilai = this.nilai,
                fglainnya = this.fglainnya
            };
        }
    }

    //View

    public class BayarDtlView
    {
        public string Tkey { get; set; }
        public int nomorTahap { get; set; }
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string group { get; set; }
        public string instkey { get; set; }
        public string idBidang { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public string noMemo { get; set; }
        public double Jml { get; set; }
        public DateTime? tgl { get; set; }
        public DateTime? created { get; set; }
        public string creator { get; set; }
        public DateTime? issued { get; set; }
        public string issuer { get; set; }
        public DateTime? submited { get; set; }
        public DateTime? review { get; set; }
        public DateTime? accounting { get; set; }
        public DateTime? final { get; set; }
        public string NoPBT { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasDibayar { get; set; }
        public string laststatus { get; set; }
        public string status { get; set; }
        public DateTime? statustm { get; set; }
        public BayarDtlView SetLocation(string project, string desa)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            return this;
        }
        public DateTime? tglPenyerahan { get; set; }
        public string ContactPerson { get; set; }
        public string noTlpCP { get; set; }
        public string tembusan { get; set; }
        public string note { get; set; }
        public string memoSigns { get; set; }
        public string memoTo { get; set; }
        public GiroCore[] giro { get; set; }
        public ReasonCore[] reasons { get; set; }
    }
    public class BayarDtlDepositView
    {
        public string Tkey { get; set; }
        public int nomorTahap { get; set; }
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string keyDesa { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public string instkey { get; set; }
        public string idBidang { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public string noMemo { get; set; }
        public double Jml { get; set; }
        public DateTime? tgl { get; set; }
        public DateTime? created { get; set; }
        public string creator { get; set; }
        public DateTime? issued { get; set; }
        public string issuer { get; set; }
        public DateTime? submited { get; set; }
        public DateTime? review { get; set; }
        public DateTime? accounting { get; set; }
        public DateTime? final { get; set; }
        public string NoPBT { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasDibayar { get; set; }
        public string laststatus { get; set; }
        public BayarDtlDepositView SetLocation(string project, string desa)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            return this;
        }
        public DateTime? tglPenyerahan { get; set; }
        public string ContactPerson { get; set; }
        public string noTlpCP { get; set; }
        public string tembusan { get; set; }
        public string note { get; set; }
        public string memoSigns { get; set; }
        public string memoTo { get; set; }
        public GiroCore[] giro { get; set; }
        public ReasonCore[] reasons { get; set; }
        public BayarSubDtlDepositView[] subdetails { get; set; } = new BayarSubDtlDepositView[0];
    }
    public class BayarSubDtlDepositView
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public double Jumlah { get; set; }
        public bool? pph21 { get; set; }
        public bool? ValidasiPPH { get; set; } //nilai validasi pph yg ditanggung penjual
        public bool? mandor { get; set; }
        public bool? pembatalanNIB { get; set; }
        public bool? BiayaBN { get; set; }
        public bool? gantiBlanko { get; set; }
        public bool? kompensasi { get; set; }
        public bool? pajakLama { get; set; }
        public bool? pajakWaris { get; set; }
        public bool? tunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayaLainnya { get; set; } = new BiayalainnyaCore[0];
    }
    public class BayarSubDtlDepositSS
    {
        public double? vpph21 { get; set; }
        public double? vValidasiPPH { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? vMandor { get; set; }
        public double? vPembatalanNIB { get; set; }
        public double? vBiayaBN { get; set; }
        public double? vGantiBlanko { get; set; }
        public double? vKompensasi { get; set; }
        public double? vPajakLama { get; set; }
        public double? vPajakWaris { get; set; }
        public double? vTunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayaLainnya { get; set; } = new BiayalainnyaCore[0];
    }
    public class BayarDtlDepositSelected  //For Edit
    {
        public string Tkey { get; set; }
        public string key { get; set; }
        public bool? invalid { get; set; }
        public string instkey { get; set; }
        public string? keyPersil { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public DateTime? tglBayar { get; set; }
        public double Jumlah { get; set; }
        public string? noMemo { get; set; }
        public DateTime? tglPenyerahan { get; set; }
        public string contactPerson { get; set; }
        public string noTlpCP { get; set; }
        public string tembusan { get; set; }
        public string note { get; set; }
        public string memoSigns { get; set; }
        public string memoTo { get; set; }
        public GiroCore[] giro { get; set; }
        public ReasonCore[] reasons { get; set; }
        public BayarSubDtlDepositSelected[] subdetails { get; set; } = new BayarSubDtlDepositSelected[0];
    }
    public class BayarSubDtlDepositSelected
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public double Jumlah { get; set; }
        public string IdBidang { get; set; }
        public string pemilik { get; set; }
        public string alashak { get; set; }
        public double? luasDiBayar { get; set; }
        public bool? pph21 { get; set; }
        public bool? ValidasiPPH { get; set; } //nilai validasi pph yg ditanggung penjual
        public bool? mandor { get; set; }
        public bool? pembatalanNIB { get; set; }
        public bool? BiayaBN { get; set; }
        public bool? gantiBlanko { get; set; }
        public bool? kompensasi { get; set; }
        public bool? pajakLama { get; set; }
        public bool? pajakWaris { get; set; }
        public bool? tunggakanPBB { get; set; }
        public double? vpph21 { get; set; }
        public double? vValidasiPPH { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? vMandor { get; set; }
        public double? vPembatalanNIB { get; set; }
        public double? vBiayaBN { get; set; }
        public double? vGantiBlanko { get; set; }
        public double? vKompensasi { get; set; }
        public double? vPajakLama { get; set; }
        public double? vPajakWaris { get; set; }
        public double? vTunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayaLainnya { get; set; } = new BiayalainnyaCore[0];
    }
    public class BayarDtlViewExt2 : BayarDtlView
    {
        public BayarDtlBidangViewSelected[] bidangs { get; set; } = new BayarDtlBidangViewSelected[0];
    }

    public class BayarDtlViewExt : BayarDtlView
    {
        //public string routekey { get; set; }
        public ToDoState state { get; set; }

        //public ToDoVerb verb { get; set; }
        //public string todo { get; set; }
        //public ToDoControl[] cmds { get; set; } = new ToDoControl[0];
        public bool isCreator { get; set; }
        public routes[] rou { get; set; } = new routes[0];
        //public BayarDtlViewExt SetRoute(string key) { this.routekey = key; return this; }
        public BayarDtlViewExt SetState(ToDoState state) { this.state = state; return this; }
        public BayarDtlViewExt SetStatus(string status, DateTime? time) { (this.status, this.statustm) = (status, time); return this; }
        //public BayarDtlViewExt SetVerb(ToDoVerb verb) { this.verb = verb; return this; }
        //public BayarDtlViewExt SetTodo(string todo) { this.todo = todo; return this; }
        //public BayarDtlViewExt SetCmds(ToDoControl[] cmds) { this.cmds = cmds; return this; }
        public BayarDtlViewExt SetCreator(bool IsCreator) { this.isCreator = IsCreator; return this; }
        public BayarDtlViewExt SetMilestones(DateTime? cre, DateTime? iss, DateTime? sub, DateTime? accGm, DateTime? accAcc, DateTime? clo)
        { (this.created, this.issued, this.submited, this.review, this.accounting, this.final) = (cre, iss, sub, accGm, accAcc, clo); ; return this; }

        public static BayarDtlViewExt Upgrade(BayarDtlView old)
            => System.Text.Json.JsonSerializer.Deserialize<BayarDtlViewExt>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );

        //public BayarDtlViewExt SetRoutes(GraphRoute[] route)
        //{
        //    var lst = new List<routes>();
        //    foreach (var item in route)
        //    {
        //        var r = new routes();
        //        r.routekey = item.key;
        //        r.todo = item._verb.Title();
        //        r.verb = item._verb == null ? ToDoVerb.unknown_ : item._verb;
        //        r.cmds = item.branches.Select(b => b._control).ToArray();

        //        lst.Add(r);
        //    }

        //    rou = lst.ToArray();

        //    return this;
        //}

        public BayarDtlViewExt SetRoutes((string key, string todo, ToDoVerb verb, ToDoControl[] cmds)[] routes)
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
    public class routes
    {
        public string routekey { get; set; }
        public ToDoVerb verb { get; set; }
        public string todo { get; set; }
        public ToDoControl[] cmds { get; set; } = new ToDoControl[0];
    }

    public class BayarDtlBidangView
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string IdBidang { get; set; }
        public double? Jumlah { get; set; }
        public StatusBidang en_state { get; set; }
        public JenisProses en_proses { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasInternal { get; set; }
        public double? luasNIB { get; set; }
        public double? luasNIBTemp { get; set; }
        public bool? luasFix { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public string NamaSurat { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public double? total { get; set; }
        public double? totalAkta { get; set; }
        public double? SisaLunas { get; set; }
        public double? TotalPembayaran { get; set; }
        public string noPeta { get; set; }
        public bool? fgPPH21 { get; set; }
        public double? TotalPPH21 { get; set; } // nilai Total Bayar * 2.5%
        public double? TotalPPH21Akta { get; set; } // nilai Total Bayar * 2.5%
        public bool? validasiPPH { get; set; }
        public double? TotalValidasiPPH21 { get; set; } // nilai total validasi
        public bool? isEdited { get; set; }
        public double? Mandor { get; set; }
        public double? PembatalanNIB { get; set; }
        public double? BalikNama { get; set; }
        public double? GantiBlanko { get; set; }
        public double? Kompensasi { get; set; }
        public double? PajakLama { get; set; }
        public double? PajakWaris { get; set; }
        public double? TunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayalainnyas { get; set; }
        public BayarSubDtlDepositSS deposit { get; set; }
        public BayarDtlBidangView SetLocation(string project)
        {
            if (project != null)
                this.project = project;
            return this;
        }
    }

    public class BayarDtlBidangViewSelected : BayarDtlBidangView
    {
        public bool IsBidangSelected { get; set; }
        public double? hargaPelunasan { get; set; }
    }

    public class BayarDtlBidangViewDeposit
    {
        public string keyPersil { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string IdBidang { get; set; }
        public double? luasDibayar { get; set; }
        public double? TotalPembayaran { get; set; }
        public string noPeta { get; set; }
        public double? satuan { get; set; }
        public string Pemilik { get; set; }
        public string alasHak { get; set; }
        public double? jumlah { get; set; }
        public string keterangan { get; set; }
        public BayarSubDtlDepositSS deposit { get; set; }
    }

    public class BiayalainnyaView : BiayalainnyaCore
    {
        public string strNilai
        {
            set { this.nilai = double.TryParse(value, out double res) ? res : 0; }
            get { return string.Format("{0:n0}", this.nilai); }
        }

        public BiayalainnyaCore ToCore()
        {
            return new BiayalainnyaCore
            {
                nilai = this.nilai,
                identity = this.identity,
                fglainnya = this.fglainnya
            };
        }
    }

    public class GroupBidangView
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang en_state { get; set; }
        public DateTime? deal { get; set; }
        public string keyProject { get; set; }
        public string project { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? luasUkur { get; set; }
        public double? satuan { get; set; }
        public double? total { get; set; }
        public string noPeta { get; set; }
        public string keyPenampung { get; set; }
        public string keyPTSK { get; set; }
        public string ptsk { get; set; }


    }

    public class BayarDtlCommand
    {
        public string tkey { get; set; }
        public string pkey { get; set; }
        public string rkey { get; set; }
        public string noMemo { get; set; }
        public ToDoControl control { get; set; }
        public string reason { get; set; }
    }

    public class BidangCommand
    {
        public string Tkey { get; set; }
        public string keyPersil { get; set; }
        public double? luasInternal { get; set; }
        public double? luasDibayar { get; set; }
        public double? satuan { get; set; }
        public string reason { get; set; }
    }

    public class BayarViewRpt
    {
        public int NoTahap { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public int? JumlahBidang { get; set; }
        public double? TotalLuas { get; set; }
        public string Dibuat { get; set; }
        public DateTime Tanggal { get; set; }
        public string PTSK { get; set; }
    }
    public class BayarRpt
    {
        public int NoTahap { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PTSK { get; set; }
        public string Group { get; set; }
        public int? JumlahBidang { get; set; }
        public double? TotalLuas { get; set; }
        public string Dibuat { get; set; }
        public string Tanggal { get; set; }

    }

    public class BayarRptExt
    {

        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public int? JumlahBidang { get; set; }
        public double? TotalLuas { get; set; }
    }

    public class BayarPivot
    {
        public string keyProject { get; set; }
        public string project { get; set; }
        public string keyTahap { get; set; }
        public int tahap { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Kode { get; set; }
        public double? value { get; set; }

        public BayarPivot setIdBidang(string idBidang)
        {
            if (idBidang != null)
                this.IdBidang = idBidang;

            return this;
        }
        public BayarPivot setProject(string project)
        {
            if (project != null)
                this.project = project;

            return this;
        }
    }

    public class BiayaView
    {
        public string Tkey { get; set; } //TahapKey
        public string Pkey { get; set; } //PersilKey
        public bool? luasFix { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? luasUkur { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public bool? pph21 { get; set; } // flag pph21 ditanggung penjual
        public bool? ValidasiPPH { get; set; } // flag validasi pph ditanggung penjual
        public double? ValidasiPPHValue { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? Mandor { get; set; }
        public double? PembatalanNIB { get; set; }
        public double? BalikNama { get; set; }
        public double? GantiBlanko { get; set; }
        public double? Kompensasi { get; set; }
        public double? PajakLama { get; set; }
        public double? PajakWaris { get; set; }
        public double? TunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayalainnya { get; set; } = new BiayalainnyaCore[0];
        public BiayaCore ToCoreView()
        {
            return new BiayaCore
            {
                Tkey = this.Tkey,
                Pkey = this.Pkey,
                satuan = this.satuan,
                satuanAkta = this.satuanAkta,
                pph21 = this.pph21,
                ValidasiPPH = this.ValidasiPPH,
                ValidasiPPHValue = this.ValidasiPPHValue,
                Mandor = this.Mandor > -1 ? this.Mandor : this.Mandor * -1,
                PembatalanNIB = this.PembatalanNIB > -1 ? this.PembatalanNIB : this.PembatalanNIB * -1,
                BalikNama = this.BalikNama > -1 ? this.BalikNama : this.BalikNama * -1,
                GantiBlanko = this.GantiBlanko > -1 ? this.GantiBlanko : this.GantiBlanko * -1,
                Kompensasi = this.Kompensasi > -1 ? this.Kompensasi : this.Kompensasi * -1,
                PajakLama = this.PajakLama > -1 ? this.PajakLama : this.PajakLama * -1,
                PajakWaris = this.PajakWaris > -1 ? this.PajakWaris : this.PajakWaris * -1,
                TunggakanPBB = this.TunggakanPBB > -1 ? this.TunggakanPBB : this.TunggakanPBB * -1,
                fgmandor = this.Mandor > 0,
                fgpembatalanNIB = this.PembatalanNIB > 0,
                fgbaliknama = this.BalikNama > 0,
                fggantiblanko = this.GantiBlanko > 0,
                fgkompensasi = this.Kompensasi > 0,
                fgpajaklama = this.PajakLama > 0,
                fgpajakwaris = this.PajakWaris > 0,
                fgtunggakanPBB = this.TunggakanPBB > 0,
                biayalainnyaCore = this.biayalainnya
            };
        }
    }

    public interface IBayar { }

    public class DetailBidangs
    {
        public string pKey { get; set; }
        public bool IsBayar { get; set; }
        public string Alias { get; set; }
        public string Desa { get; set; }
        public string Project { get; set; }
        public string Perusahaan { get; set; }
        public string Pemilik { get; set; }
        public string NomorPeta { get; set; }
        public string SuratAsal { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasUkurInternal { get; set; }
        public double? LuasBayar { get; set; }
        public string id_bidang { get; set; }
        public double Harga { get; set; }
        public double Jumlah { get; set; }
        public string NamaSurat { get; set; }
        public string AlasHak { get; set; }
        public string Tahap { get; set; }
        public double LuasPBT { get; set; }
        public string NIB { get; set; }
        public double? Satuan { get; set; }
        public double? SatuanAkte { get; set; }
        public string Notaris { get; set; }
        public double? luasBintang { get; set; }
        public double? luasOverlap { get; set; }

        public void fillDetailBidang(DetailBidangs dtlBid)
        {
            (pKey, IsBayar, Alias, Pemilik, Desa,
             Project, id_bidang, Perusahaan, NomorPeta,
             SuratAsal, LuasSurat, LuasUkurInternal, LuasBayar,
             id_bidang, Harga, Jumlah, SatuanAkte, Notaris
            ) =
            (dtlBid.pKey, dtlBid.IsBayar, dtlBid.Alias, dtlBid.Pemilik, dtlBid.Desa,
             dtlBid.Project, dtlBid.id_bidang, dtlBid.Perusahaan, dtlBid.NomorPeta,
             dtlBid.SuratAsal, dtlBid.LuasSurat, dtlBid.LuasUkurInternal, dtlBid.LuasBayar,
             dtlBid.id_bidang, dtlBid.Harga, dtlBid.Jumlah, dtlBid.SatuanAkte, dtlBid.Notaris
            );
        }


    }

    public class DetailPembayaran : BiayalainnyaCore
    {
        public int keyOrder { get; set; }
        public string key { get; set; }
        public DateTime? Tanggal { get; set; }
    }

    public class MemoPembayaranView
    {
        public string NoMemo { get; set; }
        public string Kepada { get; set; }
        public string TahapProject { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DetailBidangs[] detailBidangs { get; set; } = new DetailBidangs[0];
        public double? TotalTunggakanPBB { get; set; }
        public double? TotalJumlah { get; set; }
        public double? TotalLuasSurat { get; set; }
        public double? TotalLuasInternal { get; set; }
        public double? TotalPBT { get; set; }
        public double? Pelunasan { get; set; }
        public string Note { get; set; }
        public Double? NilaiAkte { get; set; } // Ini Satuan di bayar
        public DateTime? TanggalPenyerahan { get; set; }
        public DateTime? TanggalPelunasan { get; set; }
        public GiroCore[] Giro { get; set; } = new GiroCore[0];
        public string Notaris { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPersonPhone { get; set; }
        public string MemoSigns { get; set; }
        public string[] Tembusan { get; set; } = new string[0];
        public string Mng { get; set; }
        public string Sales { get; set; }
        public string Mediator { get; set; }
        public DetailPembayaran[] detailPembayaran { get; set; } = new DetailPembayaran[0];
    }

    public class MemoTanahView
    {
        public string Title { get; set; }
        public string NoMemo { get; set; }
        public string TanggalMemo { get; set; }
        public string TahapProject { get; set; }

        public string Note { get; set; }
        public Double? NilaiAkte { get; set; } // Ini Satuan di bayar
        public DateTime? TanggalPenyerahan { get; set; }

        public string Mng { get; set; }
        public string Sales { get; set; }
        public string Mediator { get; set; }
        public string MemoSigns { get; set; }
        public string Notaris { get; set; }
        public string BiayaLain { get; set; }
        public BidangDetailsMemoBayar[] BidangDetails { get; set; } = new BidangDetailsMemoBayar[0];
        public BayarBidangDetails[] BayarBidangDetails { get; set; } = new BayarBidangDetails[0];
        public GiroCore[] Giro { get; set; } = new GiroCore[0];

        public MemoTanahView AddBidangDetail(BayarBidangDetails bidangDetail)
        {
            var listBidangDetail = this.BayarBidangDetails.ToList();
            listBidangDetail.Add(bidangDetail);
            this.BayarBidangDetails = listBidangDetail.ToArray();
            return this;
        }
    }

    public class BidangDetailsMemoBayar
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Alias { get; set; }
        public string Desa { get; set; }
        public string Project { get; set; }
        public string Ptsk { get; set; }
        public string Pemilik { get; set; }
        public string SuratAsal { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasInternal { get; set; }
        public double? LuasPBT { get; set; }
        public double? LuasBayar { get; set; }
        public string NoPeta { get; set; }
        public double? Harga { get; set; }
        public double? Totalbeli { get; set; }
        public string Nama { get; set; }
        public string Keterangan { get; set; }
        public double? LuasOverlap { get; set; }
        public string Biayalain { get; set; }
        public bool IsSelected { get; set; }
    }

    public class BayarBidangDetails
    {
        public int order { get; set; }
        public string keyPersil { get; set; }
        public string ColumnName { get; set; }
        public dynamic Value { get; set; }
    }

    public class BayarPendingSumView
    {
        public int nomorTahap { get; set; }
        public string namaProject { get; set; }
        public string namaDesa { get; set; }
        public string PTSK { get; set; }
        public string jenisBayar { get; set; }
        public int totalBidang { get; set; }
        public double totalBayar { get; set; }
        public BayarPendingSumPersilView[] details { get; set; } = new BayarPendingSumPersilView[0];
        public ToDoState state { get; set; }
        public string to { get; set; }
    }

    public class BayarPendingSumPersilView
    {
        public double Jumlah { get; set; }
        public string key { get; set; }
        public string keyPersil { get; set; }
    }

    public class BayarPendingDtView
    {
        public string idBidang { get; set; }
        public string alasHak { get; set; }
        public string nomorPeta { get; set; }
        public int nomorTahap { get; set; }
        public string namaProject { get; set; }
        public string namaDesa { get; set; }
        public string PTSK { get; set; }
        public string jenisBayar { get; set; }
        public double totalBayarPersil { get; set; }
        public ToDoState state { get; set; }
    }
}

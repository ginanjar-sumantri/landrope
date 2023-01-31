using auth.mod;
using landrope.mod;
using landrope.mod.shared;
//using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using landrope.common;
using landrope.mod2;
//using landrope.mcommon;

namespace landrope.mod2
{
    [BsonKnownTypes(typeof(PersilGirik), typeof(PersilHGB), typeof(PersilSHM))]
    [Entity("persil", "persils_v2")]
    [BsonIgnoreExtraElements(true, Inherited = true)]
    public class Persil : entity
    {
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public StateHistories[] statehistories { get; set; } = new StateHistories[0];

        [BsonIgnore]
        public string state
        {
            get => (en_state ?? StatusBidang.bebas).ToString("g");
            set
            {
                if (state == null)
                    en_state = null;
                else if (Enum.TryParse<StatusBidang>(value, out StatusBidang sta))
                    en_state = sta;
            }
        }

        public DateTime? statechanged { get; set; }
        public string statechanger { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? deal { get; set; }
        public string dealer { get; set; }
        public DateTime? dealSystem { get; set; }
        public deals[] dealStatus { get; set; } = new deals[0];
        public LatLon dealGPS { get; set; }

        [BsonExtraElements]
        Dictionary<string, object> extraelem { get; set; }

        public string notebatal { get; set; }
        public Persil()
        {
            created = DateTime.Now;
            key = MongoEntity.MakeKey;
        }
        public static string MakeID(authEntities context, string keyProject, string keyDesa)
        {
            string collname = "IDcodes";
            int? cnt = null;
            var codes = context.GetCollections(new { key = "", code = "", last = cnt }, collname, $"{{key:{{$in:['{keyProject}','{keyDesa}']}}}}", "{_id:0}")
                .ToList();
            var cdproj = codes.FirstOrDefault(c => c.key == keyProject);
            var cddesa = codes.FirstOrDefault(c => c.key == keyDesa);
            if (cdproj == null || cddesa == null)
                return null;

            int last = (cddesa.last ?? 0) + 1;
            context.db.GetCollection<BsonDocument>(collname).FindOneAndUpdate($"{{key:'{cddesa.key}'}}",
                                                                                                                $"{{$set:{{last:{last}}}}}");
            return $"{cdproj.code}{cddesa.code}{last:0000}";
        }

        public string Discriminator => GetType().GetCustomAttribute<EntityAttribute>()?.Discriminator;

        public virtual void MakeID()
        {
            if (!string.IsNullOrWhiteSpace(IdBidang))
                return;

            var keyDesa = basic?.current?.keyDesa ?? basic?.entries?.LastOrDefault()?.item?.keyDesa;
            if (string.IsNullOrEmpty(keyDesa))
                return;
            var project = MyContext().GetCollections(new { project = new { key = "", identity = "" } }, "villages",
                                                        $"{{'village.key':'{keyDesa}'}}", "{_id:0,project:1}").FirstOrDefault();
            if (project == null)
                return;
            var keyProject = project.project.key;

            var idb = MakeID(MyContext(), keyProject, keyDesa);
            if (idb != null)
                IdBidang = idb;
        }

        public DateTime? created { get; set; }
        public bool? regular { get; set; }
        #region FOR BAYARS
        public bool? luasFix { get; set; }
        public bool? pph21 { get; set; } // flag untuk pph ditanggung penjual
        public bool? earlyPay { get; set; } // flag untuk Langsung lunas
        public bool? ValidasiPPH { get; set; } // flag validasi pph ditanggung penjual
        public double? ValidasiPPHValue { get; set; } //nilai validasi pph yg ditanggung penjual
        public double? mandor { get; set; }
        public double? pembatalanNIB { get; set; }
        public double? BiayaBN { get; set; }
        public double? gantiBlanko { get; set; }
        public double? kompensasi { get; set; }
        public double? pajakLama { get; set; }
        public double? pajakWaris { get; set; }
        public double? tunggakanPBB { get; set; }
        public biayalainnya[] biayalainnya { get; set; } = new biayalainnya[0];
        public double? luasPelunasan { get; set; } // nilai luas 
        #endregion
        public string PraNotaris { get; set; }
        public bool? isEdited { get; set; }// apakah sudah diedit luasbayar, internal dan satuan
        public PostNotaris[] postNotaris { get; set; } = new PostNotaris[0];
        public skhistory[] histories { get; set; } = new skhistory[0];
        public category[] categories { get; set; } = new category[0];
        public pelunasan[] paidHistories { get; set; } = new pelunasan[0];
        public PajakSertifikasi PajakSertifikasi { get; set; }
        public bool IsValidated()
        {
            var shells = GetAllShell(false);
            return shells.Any(v => v.isValidated()) && !shells.Any(v => v.isValidating());
        }
        public bool IsValidating()
        {
            var shells = GetAllShell(false);
            return shells.Any(v => v.isValidating());
        }

        public bool isRejected()
        {
            var shells = GetAllShell(false);
            return shells.Any(v => v.isRejected());
        }

        public bool IsNew() => basic != null && basic.current == null && (basic.entries?.Count ?? 0) == 1 && regular == true;

        public bool IsImported() => _isImported(GetAllShell());

        IEnumerable<ValidatableShell> GetAllShell(bool currexists = true)
        {
            var all = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => typeof(ValidatableShell).IsAssignableFrom(p.PropertyType))
                    .Select(p => p.GetValue(this)).Where(v => v != null)
                    .Cast<ValidatableShell>();
            //if (currexists)
            //	all = all.Where(v => v.isExists());
            return all.ToArray();
        }

        bool _isImported(IEnumerable<ValidatableShell> shells) => shells.Any() && shells.All(s => s.isImported());

        //basic != null && basic.current != null && (basic.entries?.Count ?? 0) == 1 
        //														&& basic.entries[0].keyCreator== "BCAB674C-45E4-492B-8EDE-791C872DCC15";

        [Step(order = 9, en_step = ProcessStep.basic,
                    privs = "DATA_VIEW,DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PRA_VIEW,PRA_FULL,PASCA_VIEW,PASCA_FULL,MAP_FULL")]
        public ValidatableShell<PersilBasic> basic { get; set; }
            = new ValidatableShell<PersilBasic>();

        //[Step(order = 10, en_step = ProcessStep.utj, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PRA_FULL,PASCA_FULL")]
        //public ValidatableShell<GroupUTJ> gUTJ { get; set; }
        //    = new ValidatableShell<GroupUTJ>();
        ////public ValidatableShell<PersilUTJ> UTJ { get; set; }
        ////	= new ValidatableShell<PersilUTJ>();

        //[Step(order = 20, en_step = ProcessStep.dp, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PRA_FULL,PASCA_FULL")]
        //public ValidatableShell<GroupDP> gDP { get; set; }
        //    = new ValidatableShell<GroupDP>();

        public string GetStatus()
        {
            var shells = GetAllShell();
            return _isImported() ? "EXCEL" : _isNew() ? "BARU" : _isValidated() ? (_isRejected() ? "KOREKSI" : "VALID") :
            _isValidating() ? "KOTOR" : "UNKNOWN";

            bool _isValidated() => shells.Any(v => v.isValidated()) && !shells.All(s => s.isImported()) && !shells.Any(v => v.isValidating());
            bool _isRejected() => shells.Any(v => v.isRejected());
            bool _isValidating() => shells.Any(v => v.isValidating());
            bool _isImported() => shells.Any() && shells.All(s => s.isImported());
            bool _isNew() => basic != null && basic.current == null && (basic.entries?.Count ?? 0) == 1 && regular == true;
        }
        //public ValidatableShell<PersilDP> DP { get; set; }
        //	= new ValidatableShell<PersilDP>();

        //[Step(order = 40, en_step = ProcessStep.pelunasan, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PRA_FULL,PASCA_FULL")]
        //public ValidatableShell<GroupPelunasan> gpelunasan { get; set; }
        //    = new ValidatableShell<GroupPelunasan>();
        ////public ValidatableShell<PersilPelunasan> pelunasan { get; set; }
        ////	= new ValidatableShell<PersilPelunasan>();
        //[Step(order = 30, en_step = ProcessStep.perjanjian, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesPerjanjian> perjanjian { get; set; }
        //= new ValidatableShell<ProsesPerjanjian>();
        //[Step(order = 45, en_step = ProcessStep.bayarpajak, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<PembayaranPajak> bayarpajak { get; set; }
        //    = new ValidatableShell<PembayaranPajak>();

        public static void FillBlankIDs(ExtLandropeContext context)
        {
            var persils = context.persils.Query(p => p.en_state != StatusBidang.batal && p.IdBidang == null &&
                                                p.basic.current != null && p.basic.current.keyDesa != null && p.basic.current.keyDesa != null).ToList();
            persils.ForEach(p =>
            {
                p.IdBidang = MakeID(context, p.basic.current.keyProject, p.basic.current.keyDesa);
                context.persils.Update(p);
            });
            context.SaveChanges();
        }

        public class LatLon
        {
            public double lat { get; set; }
            public double lon { get; set; }
        }

        public PersilView ToView(ExtLandropeContext context)
        {
            var basic = this.basic.current;
            var view = new PersilView();
            (view.key, view.IdBidang, view.noPeta, view.noSurat, view.group,
                view.pemilik, view.alias, view.nama, view.luasDibayar, view.luasSurat) =
            (key, IdBidang, basic.noPeta,
            basic.surat.nomor, basic.group, basic.pemilik, basic.alias, basic.surat.nama, basic.luasDibayar, basic.luasSurat);
            return view;
        }
        public PersilView ToView()
        {
            var basic = this.basic.current;
            var view = new PersilView();
            (view.key, view.IdBidang, view.noPeta, view.noSurat, view.group,
                view.pemilik, view.alias, view.nama, view.luasDibayar, view.luasSurat) =
            (key, IdBidang, basic.noPeta,
            basic.surat.nomor, basic.group, basic.pemilik, basic.alias, basic.surat.nama, basic.luasDibayar, basic.luasSurat);
            return view;
        }

        public PersilViewTemp toView(string project, string desa)
        {
            var basic = this.basic.current;
            var view = new PersilViewTemp();
            var surat = string.Empty;

            if (basic != null)
            {
                if (basic.surat != null)
                    surat = basic.surat.nomor == null ? "" : basic.surat.nomor.Replace(",", " ");
            }

            (view.idBidang, view.project, view.desa, view.alashak, view.luasSurat, view.luasInternal, view.luasdiBayar, view.NamaPemilik, view.noPeta)
                = (IdBidang, project, desa, surat, basic == null ? 0 : basic.luasSurat, basic == null ? 0 : basic.luasInternal, basic == null ? 0 : basic.luasDibayar,
                basic == null ? "" : basic.pemilik, basic == null ? "" : basic.noPeta);

            return view;
        }

        public void FromCore(StatusBidang? es, DateTime? deals, string dealers, string statechangers, DateTime? tgl,
            double? _luas, double? _satuan, double? _total)
        {
            (en_state, deal, dealer, statechanger, statechanged, basic.current.luasDibayar, basic.current.satuan, basic.current.total) =
                (Enum.TryParse<StatusBidang>(es.ToString(), out StatusBidang stp) ? stp : default,
                deals,
                dealers,
                statechangers,
                tgl, _luas, _satuan, _total);
        }

        public void FromCore2(StatusBidang? es, DateTime? deals, string dealers, string statechangers, DateTime? tgl,
            double? _luas, double? _satuan, bool? fix, bool? _pph21, bool? _validasiPPH, double? _validasiPPHValue, bool? _earlypay)
        {
            (en_state, deal, dealer, statechanger, statechanged, basic.current.satuan, basic.current.luasSurat,
                luasFix, pph21, ValidasiPPH, ValidasiPPHValue, earlyPay) =
                (Enum.TryParse<StatusBidang>(es.ToString(), out StatusBidang stp) ? stp : default,
                deals,
                dealers,
                statechangers,
                tgl, _satuan, _luas, fix, _pph21, _validasiPPH, _validasiPPHValue, _earlypay);
        }

        public void FromCore3(StatusBidang? es, DateTime? deals, string dealers, string statechangers, DateTime? tgl,
            bool? fix, bool? _pph21, bool? _validasiPPH, double? _validasiPPHValue, bool? _earlypay)
        {
            (en_state, deal, dealer, statechanger, statechanged,
                luasFix, pph21, ValidasiPPH, ValidasiPPHValue, earlyPay) =
                (Enum.TryParse<StatusBidang>(es.ToString(), out StatusBidang stp) ? stp : default,
                deals,
                dealers,
                statechangers,
                tgl, fix, _pph21, _validasiPPH, _validasiPPHValue, _earlypay);
        }

        public void FromCore4(bool? fix, bool? _pph21, bool? _validasiPPH, double? _validasiPPHValue, bool? _earlypay)
        {
            (luasFix, pph21, ValidasiPPH, ValidasiPPHValue, earlyPay, isEdited) =
                (fix, _pph21, _validasiPPH, _validasiPPHValue, _earlypay, true);
        }

        public void FromCore(bool? fix, bool? _pph21, bool? _validasiPPH, double? _validasiPPHValue, bool? _earlypay, double? _mandor, double? _pembatalanNIB, double? _balikNama,
            double? _gantiBlanko, double? _kompensasi, double? _pajaklama, double? _pajakwaris, double? _tunggakanPBB, BiayalainnyaCore[] lainnyaCore)
        {
            var list = new List<biayalainnya>();
            foreach (var item in lainnyaCore)
            {
                var lainnya = new biayalainnya();
                lainnya.identity = item.identity;
                lainnya.nilai = item.nilai;
                lainnya.fgLainnya = item.fglainnya;

                list.Add(lainnya);
            }

            (luasFix, pph21, ValidasiPPH, ValidasiPPHValue, earlyPay, isEdited, mandor, pembatalanNIB, BiayaBN, gantiBlanko, kompensasi, pajakLama, pajakWaris, tunggakanPBB, biayalainnya) =
                (fix, _pph21, _validasiPPH, _validasiPPHValue, _earlypay, true, _mandor, _pembatalanNIB, _balikNama, _gantiBlanko, _kompensasi, _pajaklama, _pajakwaris, _tunggakanPBB, list.ToArray());
        }

        public void FromCore(StatusBidang? es, DateTime? deals, string dealers, string statechangers, DateTime? tgl)
        {
            (en_state, deal, dealer, statechanger, statechanged) =
                (Enum.TryParse<StatusBidang>(es.ToString(), out StatusBidang stp) ? stp : default,
                deals,
                dealers,
                statechangers,
                tgl);
        }

        public void FromCore(StatusBidang? es, DateTime? deals, DateTime? dealsSystem, string dealers, string statechangers, DateTime? tgl, string notaris)
        {
            (en_state, deal, dealSystem, dealer, statechanger, statechanged, PraNotaris) =
                (Enum.TryParse<StatusBidang>(es.ToString(), out StatusBidang stp) ? stp : default,
                deals,
                dealsSystem,
                dealers,
                statechangers,
                tgl, notaris);
        }
        public GroupBidangView ToGroupView()
        {
            var basic = this.basic.current;
            var view = new GroupBidangView();

            (view.key, view.IdBidang, view.en_state, view.deal, view.keyProject,
                view.keyDesa, view.group, view.luasDibayar, view.satuan, view.total) =
                (key, IdBidang, (Enum.TryParse<StatusBidang>(en_state.ToString(), out StatusBidang stp) ? stp : default),
                deal, basic.keyProject, basic.keyDesa, basic.group, basic.luasDibayar, basic.satuan, basic.total);

            return view;
        }
    }

    [Entity("persilGirik", "persils_v2")]
    public class PersilGirik : Persil
    {
        //[Step(order = 30, name = "perjanjian", descr = "PPJB dan Akta", privs="DATA_FULL,PAYMENT_FULL,ARSIP_FULL")]
        //public ValidatableShell<ProsesPerjanjianGirik> perjanjian { get; set; }
        //= new ValidatableShell<ProsesPerjanjianGirik>();
        //[Step(order = 47, en_step = ProcessStep.pbtpersonal, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesPBT<NIB_Perorangan>> PBTPerorangan { get; set; }
        //    = new ValidatableShell<ProsesPBT<NIB_Perorangan>>();

        //[Step(order = 50, en_step = ProcessStep.sph, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesSPH> SPH { get; set; }
        //    = new ValidatableShell<ProsesSPH>();

        //[Step(order = 60, en_step = ProcessStep.pbtpt, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesPBT<NIB_PT>> PBTPT { get; set; }
        //    = new ValidatableShell<ProsesPBT<NIB_PT>>();

        //[Step(order = 70, en_step = ProcessStep.skkantah, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesMohonSKKantah> SKKantah { get; set; }
        //    = new ValidatableShell<ProsesMohonSKKantah>();

        //[Step(order = 80, en_step = ProcessStep.skkanwil, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesMohonSKKanwil> SKKanwil { get; set; }
        //    = new ValidatableShell<ProsesMohonSKKanwil>();

        //[Step(order = 90, en_step = ProcessStep.cetakbuku, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesCetakBukuHGB> cetakBuku { get; set; }
        //    = new ValidatableShell<ProsesCetakBukuHGB>();
    }

    [BsonKnownTypes(typeof(PersilSHM), typeof(PersilSHP))]
    [Entity("persilHGB", "persils_v2")]
    public class PersilHGB : Persil
    {
        //[Step(order = 30, name = "perjanjian", descr = "PPJB dan Akta", privs="DATA_FULL,PAYMENT_FULL,ARSIP_FULL")]
        //public ValidatableShell<ProsesPerjanjianSertifikat> perjanjian { get; set; }
        //	= new ValidatableShell<ProsesPerjanjianSertifikat>();
        //[Step(order = 70, en_step = ProcessStep.ajb, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesAJB> prosesAJB { get; set; }
        //    = new ValidatableShell<ProsesAJB>();
        //[Step(order = 100, en_step = ProcessStep.baliknama, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesBalikNama> balikNama { get; set; }
        //    = new ValidatableShell<ProsesBalikNama>();
    }

    [BsonKnownTypes(typeof(PersilHibah))]
    [Entity("persilSHM", "persils_v2")]
    public class PersilSHM : PersilHGB
    {
        //[Step(order = 120, en_step = ProcessStep.turunhak, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesTurunHak> turunHak { get; set; }
        //    = new ValidatableShell<ProsesTurunHak>();
    }

    [Entity("persilSHP", "persils_v2")]
    public class PersilSHP : PersilHGB
    {
        //[Step(order = 120, en_step = ProcessStep.naikhak, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesNaikHak> naikHak { get; set; }
        //    = new ValidatableShell<ProsesNaikHak>();
    }

    [Entity("PersilHibah", "persils_v2")]
    public class PersilHibah : PersilSHM
    {
        //[Step(order = 40, en_step = ProcessStep.pbtpersonal, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesPBT<NIB_Perorangan>> PBTPerorangan { get; set; }
        //    = new ValidatableShell<ProsesPBT<NIB_Perorangan>>();
        //[Step(order = 90, en_step = ProcessStep.cetakbuku, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesCetakBukuSHM> cetakBuku { get; set; }
        //    = new ValidatableShell<ProsesCetakBukuSHM>();
        //[Step(order = 100, en_step = ProcessStep.masukajb, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<MasukAJB> masukAJB { get; set; }
        //    = new ValidatableShell<MasukAJB>();

        //// Hibah untuk saluran -- added 2020-04-30 - JJ
        //[Step(order = 50, en_step = ProcessStep.sph, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesSPH> SPH { get; set; }
        //    = new ValidatableShell<ProsesSPH>();
        //[Step(order = 60, en_step = ProcessStep.pbtpt, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesPBT<NIB_PT>> PBTPT { get; set; }
        //    = new ValidatableShell<ProsesPBT<NIB_PT>>();
        //[Step(order = 70, en_step = ProcessStep.skkantah, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesMohonSKKantah> SKKantah { get; set; }
        //    = new ValidatableShell<ProsesMohonSKKantah>();
        //[Step(order = 80, en_step = ProcessStep.skkanwil, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesMohonSKKanwil> SKKanwil { get; set; }
        //    = new ValidatableShell<ProsesMohonSKKanwil>();

        //[Step(order = 110, en_step = ProcessStep.shm, privs = "DATA_FULL,PAYMENT_FULL,ARSIP_FULL,PASCA_FULL")]
        //public ValidatableShell<ProsesSHM> SHM { get; set; }
        //    = new ValidatableShell<ProsesSHM>();
    }

    public class PersilBase
    {
        public string _t { get; set; }
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public DateTime? deal { get; set; }
        public AssignmentCat cat => _t switch
        {
            "persilGirik" => AssignmentCat.Girik,
            "persilSHM" => AssignmentCat.SHM,
            "persilSHP" => AssignmentCat.SHP,
            "persilHGB" => AssignmentCat.HGB,
            _ => AssignmentCat.Hibah,
        };
        public DateTime? created { get; set; }

        public PersilBasic basic { get; set; }

        public PersilForMap ForMap(PersilPositionWithNext origin) =>
            new PersilForMap
            {
                key = key,
                IdBidang = IdBidang,
                category = cat,
                keyDesa = origin?.keyDesa ?? basic?.keyDesa,
                keyProject = origin?.keyProject ?? basic?.keyProject,
                keyPenampung = origin?.keyPenampung ?? basic?.keyPenampung,
                keyPTSK = origin?.keyPTSK ?? basic?.keyPTSK,
                luasDibayar = origin?.luasDibayar ?? basic?.luasDibayar,
                luasSurat = origin?.luasSurat ?? basic?.luasSurat,
                luasHGB = origin?.luasHGB,
                luasPBT = origin?.luasPBT,
                luasPropsHGB = origin?.luasPropsHGB,
                luasPropsPBT = origin?.luasPropsPBT,
                ongoing = origin?.next != null,
                step = (origin == null, origin?.next == null, en_state) switch
                {
                    (true, _, null or StatusBidang.bebas) => DocProcessStep.Baru_Bebas,
                    (true, _, StatusBidang.kampung or StatusBidang.belumbebas) => DocProcessStep.Belum_Bebas,
                    (false, false, _) => origin.next.Value,
                    _ => origin.step
                },
                nomor = basic?.surat?.nomor,
                noPeta = basic?.noPeta,
                group = basic?.group,
                nama = basic?.surat?.nama,
                kampung = en_state == StatusBidang.kampung,
                deal = en_state == StatusBidang.belumbebas && deal != null
            };
    }

}

using auth.mod;
using GraphConsumer;
using landrope.common;
using landrope.consumers;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using flow.common;
using System.Linq;
using GenWorkflow;
using MongoDB.Bson.Serialization.Attributes;

namespace landrope.mod4
{
    [Entity("sertifikasi", "proses")]
    public class Sertifikasi : namedentity4, ISertifikasi
    {
        public Sertifikasi(user user)
        {
            key = MakeKey;
            rfp.tglBuat = DateTime.Now;
            keyCreator = user.key;
        }

        public Sertifikasi()
        {

        }

        public Sertifikasi(user user, string proses, string type)
        {
            key = MakeKey;
            rfp.tglBuat = DateTime.Now;
            keyCreator = user.key;

            CreateGraphInstance(user, proses, type);
        }

        public string instkey { get; set; }

        public string type { get; set; }

        public RFP rfp { get; set; } = new RFP();

        public string keyProject { get; set; }

        public string keyDesa { get; set; }

        public string keyPTSK { get; set; }

        public string keyAktor { get; set; }

        public string keyCreator { get; set; }

        public bool? isAttachment { get; set; }

        public SertifikasiDetail[] details { get; set; } = new SertifikasiDetail[0];

        GraphHostConsumer graph => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

        public void CreateGraphInstance(user user, string proses, string types)
        {

            if (instkey != null)
                return;

            var type = ToDoType.Payment_Process;

            instkey = graph.Create(user, type).GetAwaiter().GetResult()?.key;
        }

        public void AddDetail(SertifikasiDetail detail)
        {
            var lst = new List<SertifikasiDetail>();
            if (details != null)
                lst = details.ToList();

            lst.Add(detail);
            details = lst.ToArray();
        }

        public SertifikasiView toView()
        {
            SertifikasiView view = new SertifikasiView();

            (view.nomorRFD, view.type, view.tanggaldiBuat, view.keterangan) = (rfp.nomor, type, rfp.tglBuat, rfp.keterangan);

            return view;
        }

        public void FromCore(ProsesCore core)
        {
            (
                type,
                rfp.nomor,
                rfp.keterangan,
                keyProject,
                keyDesa,
                keyPTSK,
                keyAktor
            ) =
            (
                core.type,
                core.nomorRFP,
                core.keterangan,
                core.keyProject,
                core.keyDesa,
                core.keyPTSK,
                core.keyAktor
             );
        }

        public void Abort()
        {
            invalid = true;
        }
    }

    public class SertifikasiDetail
    {
        public string key { get; set; }

        public string keyPersil { get; set; }

        public bool? isAttachment { get; set; }

        public SertifikasiSubTypeDetail[] subTypes { get; set; } = new SertifikasiSubTypeDetail[0];

        public void AddSubTypes(SertifikasiSubTypeDetail detail)
        {
            var lst = new List<SertifikasiSubTypeDetail>();
            if (subTypes != null)
                lst = subTypes.ToList();

            lst.Add(detail);
            subTypes = lst.ToArray();
        }

        public SertifikasiDtlView ToView(string keyproses)
        {
            var view = new SertifikasiDtlView();

            (
                view.key,
                view.keyPersil,
                view.keyProses,
                view.isAttachment
            ) =
            (
                key,
                keyPersil,
                keyproses,
                isAttachment ?? false
            );

            return view;
        }


    }

    public class SertifikasiSubTypeDetail
    {
        public string subType { get; set; }

        public Akta akta { get; set; } = new Akta();

        public double? patok { get; set; }

        public double? satuan { get; set; }

        public double? budget { get; set; }

        public double? nominal { get; set; }

        public string? jenisDokumen { get; set; }
        public int? jumlahAkta { get; set; }

        public string keterangan { get; set; }
        public bool? isAttachment { get; set; }
        public void FromCore(SertifikasiDetailCore core, bool isAttach)
        {
            if (!string.IsNullOrEmpty(core.akta.nomorAkta))
            {
                (
                    akta.nomorAkta, akta.tanggalAkta, akta.biayaPerBuku, akta.totalHarga
                ) =
                (
                    core.akta.nomorAkta, core.akta.tanggalAkta, core.akta.biayaPerBuku, core.akta.totalHarga
                );
            }

            (
                subType, patok, satuan, budget, nominal, keterangan, isAttachment, jenisDokumen, jumlahAkta
            ) =
            (
                core.subType, core.luas_Patok, core.satuan, core.budget, core.nominal, core.keterangan, isAttach, core.jenisDokumen, core.jumlahAkte
            );
        }

        public SertifikasiDtlSubTypeView ToView(LandropePayContext.entitas[] subtypes, CollDocs[] docs)
        {
            var view = new SertifikasiDtlSubTypeView();

            if (!string.IsNullOrEmpty(akta.nomorAkta))
            {
                (
                    view.nomorAkta,
                    view.tanggalAkta,
                    view.totalHarga,
                    view.biayaPerBuku

                ) =
                (
                    akta.nomorAkta,
                    akta.tanggalAkta,
                    akta.totalHarga,
                    akta.biayaPerBuku
                );
            }

            (
                view.keySubType,
                view.patok,
                view.satuan,
                view.budget,
                view.nominal,
                view.keterangan,
                view.subType,
                view.isAttachment,
                view.jenisDokumen,
                view.jumlahAkte
            ) =
            (
                subType,
                patok,
                satuan,
                budget,
                nominal,
                keterangan,
                subtypes.FirstOrDefault(x => x.key == subType)?.identity,
                isAttachment,
                docs.FirstOrDefault(x => x.JenisId == jenisDokumen)?.JenisDokumen,
                jumlahAkta

            );

            return view;
        }
    }

    public class Akta
    {
        public string nomorAkta { get; set; }
        public DateTime? tanggalAkta { get; set; }
        public double? biayaPerBuku { get; set; }
        public double? totalHarga { get; set; }
    }

    public class StaticCollSertifikasi
    {
        public CollDocs[] Docs { get; set; } = new CollDocs[0];
    }

    public class CollDocs
    {
        public string JenisId { get; set; }
        public string JenisDokumen { get; set; }
    }

    [Entity("pajak", "proses")]
    public class Pajak : namedentity4, IPajak
    {
        public Pajak(user user)
        {
            key = MakeKey;
            rfp.tglBuat = DateTime.Now;
            keyCreator = user.key;
        }

        public Pajak(user user, string proses, string type)
        {
            key = MakeKey;
            rfp.tglBuat = DateTime.Now;
            keyCreator = user.key;

            CreateGraphInstance(user, proses, type);
        }

        public Pajak()
        {

        }

        public string instkey { get; set; }

        public string type { get; set; }

        public string keyProject { get; set; }

        public string keyDesa { get; set; }

        public string keyPTSK { get; set; }

        public RFP rfp { get; set; } = new RFP();

        //public DateTime? tglterimabukti { get; set; }

        public string keyCreator { get; set; }

        public bool? isAttachment { get; set; }

        public DateTime? ExpiredDate { get; set; }

        public PajakDetail[] details { get; set; } = new PajakDetail[0];

        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        public GraphMainInstance instance => graph?.Get(instkey).GetAwaiter().GetResult();
        GraphHostConsumer graph => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

        public void CreateGraphInstance(user user, string proses, string types)
        {
            if (instkey != null)
                return;

            var type = proses == "sertifikasi" ? ToDoType.Payment_Process :
                (withChild().Contains(types) && proses == "pajak") ? ToDoType.Payment_Tax_NonPBB_NonVal :
                (withoutChild().Contains(types) && proses == "pajak") ? ToDoType.Payment_Tax_PBB :
                ToDoType.Payment_Tax_NonPBB_Val;

            instkey = graph.Create(user, type).GetAwaiter().GetResult()?.key;

        }

        public void AddDetail(PajakDetail detail)
        {
            var lst = new List<PajakDetail>();
            if (details != null)
                lst = details.ToList();

            lst.Add(detail);
            details = lst.ToArray();
        }

        public PajakView ToView()
        {
            PajakView view = new PajakView();

            (view.nomorRFD, view.type, view.tanggaldiBuat, view.keterangan) = (rfp.nomor, type, rfp.tglBuat, rfp.keterangan);

            return view;
        }

        public void FromCore(ProsesCore core)
        {
            (
                type,
                rfp.nomor,
                rfp.keterangan,
                keyProject,
                keyDesa,
                keyPTSK,
                ExpiredDate
            ) =
            (
                core.type,
                core.nomorRFP,
                core.keterangan,
                core.keyProject,
                core.keyDesa,
                core.keyPTSK,
                core.ExpiredDate
             );
        }

        public string[] withChild()
        {
            string[] wVal = { "PROSES004", "PROSES006", "PROSES008", "PROSES010", "PROSES012", "PROSES014" };
            return wVal;
        }

        public string[] withoutChild()
        {
            string[] nVal = { "PROSES001", "PROSES002", "PROSES003" };
            return nVal;
        }

        public void Abort()
        {
            invalid = true;
        }
    }

    public class PajakDetail
    {
        public string key { get; set; }

        public string keyPersil { get; set; }

        public bool? isAttachment { get; set; }

        public PajakSubTypeDetail[] subTypes { get; set; } = new PajakSubTypeDetail[0];

        public void AddSubTypes(PajakSubTypeDetail detail)
        {
            var lst = new List<PajakSubTypeDetail>();
            if (subTypes != null)
                lst = subTypes.ToList();

            lst.Add(detail);
            subTypes = lst.ToArray();
        }

        public PajakDtlView ToView(string keyproses)
        {
            var view = new PajakDtlView();

            (
                view.key,
                view.keyPersil,
                view.keyProses,
                view.isAttachment
            ) =
            (
                key,
                keyPersil,
                keyproses,
                isAttachment ?? false
            );

            return view;
        }

        public void FromCore(PajakDetailCore core, string keypersil)
        {

        }

        
    }

    public class PajakSubTypeDetail
    {
        public string subType { get; set; }

        public double? NJOP { get; set; }

        public double? NPOPTKP { get; set; }

        public string tahunPengaktifan { get; set; }

        public double? nominal { get; set; }

        public string keterangan { get; set; }

        public bool? isAttachment { get; set; }

        public bool? PaidByOwner { get; set; }

        public double? nominalPBO { get; set; }

        public string reasonPBO { get; set; }

        public Validasi validasi { get; set; } = new Validasi();

        public Pinalti[] pinaltis { get; set; } = new Pinalti[0];

        public PajakDtlSubTypeView ToView(LandropePayContext.entitas[] subtypes)
        {
            var view = new PajakDtlSubTypeView();
            var vv = new ValidasiView();
            var pv = new List<PinaltiView>();

            if (!string.IsNullOrEmpty(validasi.BPN_Notaris))
            {
                vv = new ValidasiView
                {
                    BPN_Notaris = validasi.BPN_Notaris,
                    tanggaldiBuat = validasi.tanggaldiBuat,
                    tanggaldiKirim = validasi.tanggaldiKirim,
                    tanggalSelesai = validasi.tanggalSelesai,
                    keterangan = validasi.keterangan
                };
            }

            if (pinaltis.Count() > 0)
            {
                var lst = pinaltis.ToList();
                foreach (var item in lst)
                {
                    var pi = new PinaltiView
                    {
                        denda = item.denda,
                        nominal = item.nominal,
                        tahun = item.tahun,
                        total = item.total
                    };

                    pv.Add(pi);

                }
            }

            (
                view.keySubType,
                view.NJOP,
                view.nominal,
                view.keterangan,
                view.validasi,
                view.pinaltis,
                view.subType,
                view.NPOPTKP,
                view.tahunPengaktifan,
                view.isAttachment,
                view.nominalPBO,
                view.reasonPBO
            ) =
            (
                subType,
                NJOP,
                (nominal - nominalPBO ?? 0),
                keterangan,
                vv,
                pv.ToArray(),
                subtypes.FirstOrDefault(x => x.key == subType)?.identity,
                NPOPTKP,
                tahunPengaktifan,
                isAttachment,
                nominalPBO,
                reasonPBO
            );

            return view;
        }

        public void FromCore(PajakDetailCore core, bool isAttach)
        {
            if (core.validasi != null)
            {
                (
                    validasi.BPN_Notaris, validasi.tanggaldiBuat, validasi.tanggaldiKirim, validasi.tanggalSelesai, validasi.keterangan
                ) =
                (
                    core.validasi.BPN_Notaris, core.validasi.tanggaldiBuat, core.validasi.tanggaldiKirim, core.validasi.tanggalSelesai, core.validasi.keterangan
                );
            }

            var listPinalti = new List<Pinalti>();
            if (core.pinaltis.Count() > 0)
            {
                foreach (var pin in core.pinaltis)
                {
                    var pinalti = new Pinalti
                    {
                        denda = pin.denda,
                        nominal = pin.nominal,
                        tahun = pin.tahun,
                        total = pin.total
                    };

                    listPinalti.Add(pinalti);
                }
            }

            (
                subType, NJOP, NPOPTKP, tahunPengaktifan, nominal, keterangan, isAttachment, pinaltis
            ) =
            (
                core.subType, core.NJOP, core.NPOPTKP, core.tahunPengaktifan, core.nominal, core.keterangan, isAttach, listPinalti.ToArray()
            );
        }

        public void FromCore(ValidasiCore core)
        {
            (validasi.BPN_Notaris,
               validasi.tanggaldiBuat,
               validasi.tanggaldiKirim,
               validasi.tanggalSelesai,
               validasi.keterangan
            ) =
            (
               core.BPN_Notaris,
               core.tanggaldiBuat,
               core.tanggaldiKirim,
               core.tanggalSelesai,
               core.keterangan
            );
        }

        public void FromCore(string reason, double nominal)
        {
            nominalPBO = nominal;
            PaidByOwner = nominal == 0 ? false : true;
            reasonPBO = reason;
        }
    }

    public class Pinalti
    {
        public string tahun { get; set; }
        public double? denda { get; set; }
        public double? nominal { get; set; }
        public double? total { get; set; }
    }

    public class Validasi
    {
        public DateTime? tanggaldiBuat { get; set; }
        public DateTime? tanggaldiKirim { get; set; }
        public DateTime? tanggalSelesai { get; set; }
        public string BPN_Notaris { get; set; }
        public string keterangan { get; set; }
    }

    public class Proses
    {
        public string key { get; set; }

        public string instkey { get; set; }

        public string proses { get; set; }

        public string type { get; set; }

        public string nomorRFP { get; set; }

        public double? TotalNominal { get; set; }

        public string keyType { get; set; }

        public string keyProject { get; set; }

        public string project { get; set; }

        public string keyDesa { get; set; }

        public string desa { get; set; }

        public string keyPTSK { get; set; }

        public string keyAktor { get; set; }

        public DateTime tanggaldiBuat { get; set; }

        public string keterangan { get; set; }
        public DateTime? expiredDate { get; set; }

        public string creator { get; set; }

        public bool? isAttachment { get; set; }

        public Reason[] reasons { get; set; } = new Reason[0];

        public Pajak pajak(LandropePayContext context, string keyProses) => context.pajaks.FirstOrDefault(p => p.key == keyProses);

        public Sertifikasi sertifikasi(LandropePayContext context, string keyProses) => context.sertifikasis.FirstOrDefault(p => p.key == keyProses);

        public ProsesView toView(LandropePayContext context, (LandropePayContext.entitas project, LandropePayContext.entitas desa)[] villages,
                                                LandropePayContext.entitas[] companies, LandropePayContext.entitas[] aktors,
                                                (string key, ToDoState state, string status)[] Laststates)
        {
            var view = new ProsesView();
            var rview = new List<ReasonCore>();

            if (reasons.Count() > 0 && reasons != null)
            {
                foreach (var item in reasons)
                {
                    var r = new ReasonCore
                    {
                        tanggal = item.tanggal,
                        description = item.description,
                        flag = item.flag,
                        keyCreator = item.keyCreator,
                        privs = item.privs,
                        state = item.state,
                        state_ = item.state.AsStatus(),
                        creator = context.users.FirstOrDefault(y => y.key == item.keyCreator)?.FullName
                    };

                    rview.Add(r);
                }
            }

            (var project, var desa) = villages.FirstOrDefault(v => v.desa.key == keyDesa);

            (
                view.key,
                view.instkey,
                view.proses,
                view.type,
                view.nomorRFP,
                view.keyType,
                view.keyProject,
                view.keyDesa,
                view.keyPTSK,
                view.keyAktor,
                view.project,
                view.desa,
                view.ptsk,
                view.aktor,
                view.tanggaldiBuat,
                view.keterangan,
                view.creator,
                view.status,
                view.state,
                view.isAttachment,
                view.reasons,
                view.TotalNominal,
                view.ExpiredDate
            ) =
            (
                key,
                instkey,
                proses,
                type,
                nomorRFP,
                keyType,
                keyProject,
                keyDesa,
                keyPTSK,
                keyAktor,
                project?.identity,
                desa?.identity,
                companies.FirstOrDefault(x => x.key == keyPTSK)?.identity,
                aktors.FirstOrDefault(x => x.key == keyAktor)?.identity,
                tanggaldiBuat,
                keterangan,
                creator,
                Laststates.FirstOrDefault(x => x.key == instkey).status,
                Laststates.FirstOrDefault(x => x.key == instkey).state,
                isAttachment ?? false,
                rview.ToArray(),
                TotalNominal,
                expiredDate
            );

            return view;
        }

        public ProsesViewExt toViewExt(LandropePayContext context, (LandropePayContext.entitas project, LandropePayContext.entitas desa)[] villages,
                                                LandropePayContext.entitas[] companies)
        {
            var view = new ProsesViewExt();
            var rview = new List<ReasonCore>();

            if (reasons.Count() > 0 && reasons != null)
            {
                foreach (var item in reasons)
                {
                    var r = new ReasonCore
                    {
                        tanggal = item.tanggal,
                        description = item.description,
                        flag = item.flag,
                        keyCreator = item.keyCreator,
                        privs = item.privs,
                        state = item.state,
                        state_ = item.state.AsStatus(),
                        creator = context.users.FirstOrDefault(y => y.key == item.keyCreator)?.FullName
                    };

                    rview.Add(r);
                }
            }

            (var project, var desa) = villages.FirstOrDefault(v => v.desa.key == keyDesa);

            (
                view.key,
                view.keyProject,
                view.keyDesa,
                view.keyPTSK,
                view.keyType,
                view.instkey,
                view.nomorRFP,
                view.project,
                view.desa,
                view.ptsk,
                view.proses,
                view.type,
                view.aktor,
                view.keterangan,
                view.creator,
                view.tanggaldiBuat,
                view.isAttachment,
                view.reasons,
                view.TotalNominal
            ) =
            (
                key,
                keyProject,
                keyDesa,
                keyPTSK,
                keyType,
                instkey,
                nomorRFP,
                project?.identity,
                desa?.identity,
                companies.FirstOrDefault(x => x.key == keyPTSK)?.identity,
                proses,
                type,
                keyAktor,
                keterangan,
                creator,
                tanggaldiBuat,
                isAttachment ?? false,
                rview.ToArray(),
                TotalNominal
            );

            return view;
        }

        public Proses setTotalNominal(LandropePayContext context, string proses)
        {
            if (proses == "sertifikasi")
            {
                var totalNominal = this.sertifikasi(context, key).details.SelectMany(x => x.subTypes).Select(x => x.nominal).Sum();
                this.TotalNominal = totalNominal ?? 0;
            }
            else
            {
                var totalNominal = this.pajak(context, key).details.SelectMany(x => x.subTypes).Select(x => x.nominal).Sum();
                var totalPaid = this.pajak(context, key).details.SelectMany(x => x.subTypes).Select(x => (x.nominalPBO ?? 0)).Sum();
                this.TotalNominal = (totalNominal ?? 0) - totalPaid;
            }

            return this;
        }
    }

    public class RFP
    {
        public string nomor { get; set; }

        public DateTime? tglBuat { get; set; }

        public DateTime? tglVerifikasi { get; set; } //Tanggal sewaktu bu betty/welly meneruskan

        public DateTime? tglLegal { get; set; }

        public DateTime? tglKasir { get; set; }

        public DateTime? tglAccounting { get; set; }

        public DateTime? tglTerimaBukti { get; set; } //Tanggal terima bukti bayar dari kasir ke welly/betty


        //public DateTime? tanggalDiserahkanLegal { get; set; } //Tanggal sewaktu diserahkan ke bu lili

        //public DateTime? tanggalDiserahkanPIC { get; set; } //Tanggal setelah selesai flow

        //public DateTime? tanggalInvoice { get; set; }

        public string keterangan { get; set; }

        public Reason[] reasons { get; set; } = new Reason[0];

        public void AddReason(Reason reason)
        {
            var lst = new List<Reason>();
            if (reasons != null)
                lst = reasons.ToList();

            lst.Add(reason);
            reasons = lst.ToArray();
        }
    }

    [BsonKnownTypes(typeof(pajakdate), typeof(sertifikasidate))]
    public class prosesdate : entity
    {
        public string rfp { get; set; }
        public string keypersil { get; set; }
    }

    [Entity("pajakdate", "prosesdate")]
    public class pajakdate : prosesdate
    {
        public DateTime? tglDiserahkan { get; set; }
    }

    [Entity("sertifikasidate", "prosesdate")]
    public class sertifikasidate : prosesdate
    {
        public DateTime? tglDiserahkanPIC { get; set; }
        public DateTime? tglTerimaPIC { get; set; }
        public DateTime? tglTerimaInvoice { get; set; }
    }

}

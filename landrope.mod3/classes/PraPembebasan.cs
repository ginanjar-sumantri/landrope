# define test
using auth.mod;
using landrope.documents;
using landrope.common;
using mongospace;
using flow.common;
using System;
using Tracer;
using GraphConsumer;
using landrope.consumers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using DynForm.shared;
using System.Linq;
using GenWorkflow;
using landrope.mod2;
using GraphHost;

namespace landrope.mod3
{
    [Entity("PraPembebasan", "praDeals")]
    public class PraPembebasan : namedentity3, IPraBebas
    {
#if test
        IGraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>();
#else
        GraphHostSvc graphhost => ContextService.services.GetService<IGraphHostSvc>() as GraphHostSvc;
#endif

#if test
        public GraphMainInstance instance => ghost?.Get(instancesKey).GetAwaiter().GetResult();
#else
        public GraphMainInstance instance => graphhost?.Get(instancesKey);
#endif
        public DateTime? created { get; set; }
        public string keyCreator { get; set; }
        public string keyReff { get; set; }
        public string instancesKey { get; set; }
        public string manager { get; set; }
        public string sales { get; set; }
        public string mediator { get; set; }
        public double? luasDeal { get; set; }
        public DateTime? tanggalProses { get; set; }
        public string notaris { get; set; }
        public DetailsPraBebas[] details { get; set; } = new DetailsPraBebas[0];

        public PraPembebasan()
        {

        }

        public PraPembebasan(user user, PraPembebasanCore core, List<PraPembebasan> prevPraBebas, string mgInitial = null, string identifier = null)
        {
            this.key = MakeKey;
            this.created = DateTime.Now;
            this.keyCreator = user.key;
            this.identifier = GenerateReqIdentifier(core, prevPraBebas, mgInitial, identifier);
            this.instancesKey = CreateGraphInstance(user, ToDoType.Deal_Payment, -1);
        }

        public PraPembebasan FromCore(user user, PraPembebasanCore core, List<PraPembebasan> prevPraDeal, string mgInitial, string identifier = null)
        {
            var mod = new PraPembebasan(user, core, prevPraDeal, mgInitial);
            (mod.sales, mod.manager, mod.mediator, mod.tanggalProses, mod.luasDeal)
            =
            (core.sales, core.manager, core.mediator, core?.tanggalProses?.AddHours(7), core?.luasDeal);
            return mod;
        }

        public PraPembebasan FromCore(user user, PraPembebasanCore core, List<PraPembebasan> prePraBebas, string identifier = null)
        {
            var mod = new PraPembebasan(user, core, prePraBebas, null, identifier);
            double? luasDeal = prePraBebas.FirstOrDefault(p => p.keyReff == null)?.luasDeal;
            (
                this.key, this.keyCreator, this.identifier, this.instancesKey,
                this.created, this.keyReff, this.tanggalProses, this._id, this.details,
                this.luasDeal
            )
                =
            (
                mod.key, mod.keyCreator, mod.identifier, mod.instancesKey,
                mod.created, core.keyReff, core?.tanggalProses?.AddHours(7), mod._id, new DetailsPraBebas[0],
                luasDeal
            );
            return this;
        }

        public PraPembebasan FromCore2(PraPembebasanCore core, PraPembebasan oldPraBebas, string identifier)
        {
            string manager = core.manager != oldPraBebas.manager ? core.manager : oldPraBebas.manager;
            string sales = core.sales != oldPraBebas.sales ? core.sales : oldPraBebas.sales;
            string mediator = core.mediator != oldPraBebas.mediator ? core.mediator : oldPraBebas.mediator;
            DateTime? tanggalProses = core.tanggalProses != oldPraBebas.tanggalProses ? core.tanggalProses : oldPraBebas.tanggalProses;


            (this.identifier, this.manager, this.sales, this.mediator, this.tanggalProses, this.luasDeal)
                =
            (identifier, core.manager, sales, mediator, tanggalProses, core?.luasDeal);

            return this;
        }

        public PraPembebasan FromCore3(string newIdentifier, PraPembebasanCore core)
        {
            (
                this.identifier, this.manager, this.mediator, this.sales,
                this.keyReff, this.luasDeal, this.tanggalProses
            )
                =
            (
                newIdentifier, core.manager, core.mediator, core.sales,
                core.keyReff, core.luasDeal, core.tanggalProses
            );
            return this;
        }

        public PraPembebasanCore ToCore()
        {
            PraPembebasanCore core = new PraPembebasanCore();
            (core.keyReff, core.manager, core.sales, core.mediator)
            =
            (this.identifier, this.manager, this.sales, this.mediator);
            return core;
        }

        public string CreateGraphInstance(user user, ToDoType todo, int version = -1)
        {
            MyTracer.TraceInfo(MethodBase.GetCurrentMethod(), "Create Graphs line 74");
            if (instancesKey != null)
                return instancesKey;
#if test
            this.instancesKey = ghost.Create(user, todo).GetAwaiter().GetResult()?.key;
#else
            this.instancesKey = graphhost.Create(user, todo)?.key;
#endif
            return this.instancesKey;
        }

        public string GenerateReqIdentifier(PraPembebasanCore core, List<PraPembebasan> prevPraBebas, string mgInitial = null, string identifier = null)
        {
            if (!string.IsNullOrEmpty(core.keyReff))
            {
                string lastAlphabet = "A";
                var lastPraBebas = prevPraBebas.Where(x => x.keyReff == core.keyReff)
                                               .Where(x => x.identifier.Split("/")
                                               .Any(y => y.Split("-").Count() == 2)).ToList();
                if (prevPraBebas.Count() != 0 && lastPraBebas.Count() != 0)
                {
                    char lastId = lastPraBebas.OrderByDescending(x => x.created)
                                                  .FirstOrDefault().identifier.Split("/")[2].Split("-")[1].ToUpper().ToCharArray()[0];
                    lastId++;
                    lastAlphabet = lastId.ToString();
                }
                return $"{identifier}-{lastAlphabet}";
            }
            else
            {
                string lastId = "001";
                string yearNow = DateTime.Today.Year.ToString();
                if (prevPraBebas.Count() != 0)
                {
                    var lasPraBebas = prevPraBebas.OrderByDescending(x => x.created)
                                                  .FirstOrDefault(x => x.identifier.Contains($"{mgInitial}/{yearNow}"));
                    if (lasPraBebas != null)
                    {
                        int lastIdInc = Convert.ToInt32(lasPraBebas?.identifier.Split("/")[2]) + 1;
                        lastId = SetNumberDigit(lastIdInc);
                    }
                }
                return $"{mgInitial}/{yearNow}/{lastId}";
            }
        }

        public string SetNumberDigit(int number)
        {
            string result = "";
            int digit = (int)Math.Floor(Math.Log10(number) + 1);
            if (digit == 3)
            {
                result = number.ToString();
            }
            else if (digit == 2)
            {
                result = $"0{number.ToString()}";
            }
            else
            {
                result = $"00{number.ToString()}";
            }
            return result;
        }

        public PraBebasView toView(List<route> routes, LatestState mstate,
                                   GraphState sstate, ToDoVerb verb,
                                   ToDoControl[] cmds, bool IsCreator,
                                   (DateTime? iss, DateTime? acc, DateTime? clo) tmf
                                   )
        {
            PraBebasView view = new PraBebasView();
            var partial = mstate != null ? mstate.partial : false;
            var xstate = mstate != null ? (mstate.state, mstate.time) : (sstate?.state, sstate?.time);
            (
                view.NoRequest, view.Manager, view.Sales, view.Mediator,
                view.State, view.Status, view.statustm, view.verb,
                view.todo, view.routes, view.cmds, view.isCreator,
                view.issued, view.accepted, view.closed
            )
                =
            (
                this.identifier, this.manager, this.sales, this.mediator,
                xstate.state ?? ToDoState.unknown_,
                $"{xstate.state?.AsStatus()} {(partial == true ? "(partial)" : "")}",
                xstate.time,
                verb,
                verb.Title(),
                routes.ToArray(),
                cmds,
                IsCreator,
                tmf.iss,
                tmf.acc,
                tmf.clo
            );
            return view;
        }

        public static PraPembebasan Upgrade(BayarDtlView old)
            => System.Text.Json.JsonSerializer.Deserialize<PraPembebasan>(
                    System.Text.Json.JsonSerializer.Serialize(old)
                );

        public DateTime? issued => instance?.states.LastOrDefault(s => s.state == ToDoState.issued_)?.time;
        public DateTime? accepted => instance?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time;
        public DateTime? closed => (instance?.closed ?? false) ? instance?.states.LastOrDefault()?.time : null;
    }
    public class DetailsPraBebas : IPraBebasDtl
    {
        public string key { get; set; }
        public string keyFollowUp { get; set; }
        public JenisAlasHak? jenisAlasHak { get; set; }
        public LandOwnershipType? kepemilikan { get; set; }
        public string alasHak { get; set; }
        public string pemilik { get; set; }
        public string alias { get; set; }
        public string group { get; set; }
        public string keyPersil { get; set; }
        public string keyBundle { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string notaris { get; set; }
        public DateTime? created { get; set; }
        public double? luasSurat { get; set; }
        public double? hargaSatuan { get; set; }
        public DateTime? dealUTJ { get; set; }
        public DateTime? dealDP { get; set; }
        public DateTime? dealLunas { get; set; }
        public ReasonPraDeal[] reasons { get; set; } = new ReasonPraDeal[0];
        public string keyForm { get; set; }
        public ResultDoc[] infoes { get; set; }
        public DateTime? FileToNotary { get; set; }
        public DateTime? FileToPra { get; set; }
        public string keyPersilCat { get; set; }
        public string[] Categories { get; set; }
        //public category[] CategoriesPersil { get; set; } = new category[0];
        public string NotesBelumLengkap { get; set; }
        public DateTime? AdminToPraDP { get; set; }
        public DateTime? AdminToPraLunas { get; set; }
        public Base PPJB { get; set; }
        public Base NIBPerorangan { get; set; }
        public Child CekSertifikat { get; set; }
        public Child BukaBlokir { get; set; }
        public Child RevisiNop { get; set; }
        public Child RevisiGeoKPP { get; set; }
        public Child BalikNama { get; set; }
        public Base Shm { get; set; }

        public DetailsPraBebas()
        {

        }

        public DetailsPraBebas(user user)
        {
            this.key = mongospace.MongoEntity.MakeKey;
            this.created = DateTime.Now;
            //this.instKey = CreateGraphInstance(user, ToDoType.Sub_Deal_Payment, 3);
        }

        public DetailsPraBebas FromCore(user user, DetailsPraBebasCore core, ResultDoc[] infoes, category[] categories, string keyPersilCat)
        {
            var ppjb = new Base();
            (ppjb.NomorSurat, ppjb.TanggalSurat, ppjb.Notaris) = (core.Ppjb?.NomorSurat, core.Ppjb?.TanggalSurat, core?.Ppjb.Notaris);

            var nib = new Base();
            (nib.NomorSurat, nib.TanggalSurat, nib.Notaris) = (core.NibPerorangan?.NomorSurat, core.NibPerorangan?.TanggalSurat, core.NibPerorangan?.Notaris);

            var cekCert = new Child();
            (cekCert.NomorSurat, cekCert.TanggalSurat, cekCert.Notaris, cekCert.TanggalSelesai)
                =
            (core.CekSertifikat?.NomorSurat, core.CekSertifikat?.TanggalSurat, core.CekSertifikat?.Notaris, core.EndDates?.CertEndDate);

            var bukaBlokir = new Child();
            (bukaBlokir.NomorSurat, bukaBlokir.TanggalSurat, bukaBlokir.Notaris, bukaBlokir.TanggalSelesai)
                =
            (core.BukaBlokir?.NomorSurat, core.BukaBlokir?.TanggalSurat, core.BukaBlokir?.Notaris, core.EndDates?.OpenBlockEndDate);

            var revisiNop = new Child();
            (revisiNop.NomorSurat, revisiNop.TanggalSurat, revisiNop.Notaris, revisiNop.TanggalSelesai)
                =
            (core.RevisiNop?.NomorSurat, core.RevisiNop?.TanggalSurat, core.RevisiNop?.Notaris, core.EndDates?.NOPReviseEndDate);

            var revisiGeo = new Child();
            (revisiGeo.NomorSurat, revisiGeo.TanggalSurat, revisiGeo.Notaris, revisiGeo.TanggalSelesai)
                =
            (core.RevisiGeoKpp?.NomorSurat, core.RevisiGeoKpp?.TanggalSurat, core.RevisiGeoKpp?.Notaris, core.EndDates?.GeoKppEndDate);

            var balikNama = new Child();
            (balikNama.NomorSurat, balikNama.TanggalSurat, balikNama.Notaris, balikNama.TanggalSelesai)
                =
            (core.BalikNama?.NomorSurat, core.BalikNama?.TanggalSurat, core.BalikNama?.Notaris, core.EndDates?.BalikNamaEndDate);

            var shm = new Base();
            (shm.NomorSurat, shm.TanggalSurat, shm.Notaris)
                =
            (core.Shm?.NomorSurat, core.Shm?.TanggalSurat, core.Shm?.Notaris);

            var detail = new DetailsPraBebas(user);
            (
                detail.alasHak, detail.pemilik, detail.group, detail.notaris,
                //detail.infoes, detail.luasSurat, detail.hargaSatuan,
                detail.luasSurat, detail.hargaSatuan, detail.keyProject, detail.keyDesa,
                detail.FileToNotary, detail.FileToPra, detail.Categories, detail.AdminToPraDP,
                detail.AdminToPraLunas, detail.NotesBelumLengkap, detail.PPJB, detail.NIBPerorangan,
                detail.CekSertifikat, detail.BukaBlokir, detail.RevisiNop, detail.RevisiGeoKPP,
                detail.BalikNama, detail.Shm, created, detail.alias, detail.keyPersilCat,
                detail.desa, detail.keyFollowUp, detail.jenisAlasHak, detail.kepemilikan
             )
                =
            (
                core.alasHak, core.pemilik, core.group, core.notaris,
                //infoes, core.luasSurat, core.hargaSatuan,
                core.luasSurat, core.hargaSatuan, core.keyProject, core.keyDesa,
                core?.FileToNotary, core?.FileToPra, core?.Categories, core.AdminToPraDP,
                core.AdminToPraLunas, core.NotesBelumLengkap, ppjb, nib, cekCert, bukaBlokir,
                revisiNop, revisiGeo, balikNama, shm, DateTime.Now, core.alias, keyPersilCat,
                core.desa, core.followUpKey ?? "", core.jenisAlasHak, core.kepemilikan
            );

            return detail;
        }

        public DetailsPraBebas FromCore(DetailsPraBebasCore core, category[] cat, string keyPersilCat)
        {
            var ppjb = new Base();
            (ppjb.NomorSurat, ppjb.TanggalSurat, ppjb.Notaris) = (core.Ppjb?.NomorSurat, core.Ppjb?.TanggalSurat, core.Ppjb?.Notaris);

            var nib = new Base();
            (nib.NomorSurat, nib.TanggalSurat, nib.Notaris) = (core.NibPerorangan?.NomorSurat, core.NibPerorangan?.TanggalSurat, core.NibPerorangan?.Notaris);

            var cekCert = new Child();
            (cekCert.NomorSurat, cekCert.TanggalSurat, cekCert.Notaris, cekCert.TanggalSelesai)
                =
            (core.CekSertifikat?.NomorSurat, core.CekSertifikat?.TanggalSurat, core.CekSertifikat?.Notaris, core.EndDates?.CertEndDate);

            var bukaBlokir = new Child();
            (bukaBlokir.NomorSurat, bukaBlokir.TanggalSurat, bukaBlokir.Notaris, bukaBlokir.TanggalSelesai)
                =
            (core.BukaBlokir?.NomorSurat, core.BukaBlokir?.TanggalSurat, core.BukaBlokir?.Notaris, core.EndDates?.OpenBlockEndDate);

            var revisiNop = new Child();
            (revisiNop.NomorSurat, revisiNop.TanggalSurat, revisiNop.Notaris, revisiNop.TanggalSelesai)
                =
            (core.RevisiNop?.NomorSurat, core.RevisiNop?.TanggalSurat, core.RevisiNop?.Notaris, core.EndDates?.NOPReviseEndDate);

            var revisiGeo = new Child();
            (revisiGeo.NomorSurat, revisiGeo.TanggalSurat, revisiGeo.Notaris, revisiGeo.TanggalSelesai)
                =
            (core.RevisiGeoKpp?.NomorSurat, core.RevisiGeoKpp?.TanggalSurat, core.RevisiGeoKpp?.Notaris, core.EndDates?.GeoKppEndDate);

            var balikNama = new Child();
            (balikNama.NomorSurat, balikNama.TanggalSurat, balikNama.Notaris, balikNama.TanggalSelesai)
                =
            (core.BalikNama?.NomorSurat, core.BalikNama?.TanggalSurat, core.BalikNama?.Notaris, core.EndDates?.BalikNamaEndDate);

            var shm = new Base();
            (shm.NomorSurat, shm.TanggalSurat, shm.Notaris)
                =
            (core.Shm?.NomorSurat, core.Shm?.TanggalSurat, core.Shm?.Notaris);
            (
                this.alasHak, this.pemilik, this.group, this.notaris, this.luasSurat, this.hargaSatuan,
                this.FileToNotary, this.FileToPra, this.Categories, this.NotesBelumLengkap, this.AdminToPraDP,
                this.AdminToPraLunas, this.PPJB, this.NIBPerorangan, this.CekSertifikat, this.BukaBlokir,
                this.RevisiNop, this.RevisiGeoKPP, this.BalikNama, this.Shm, this.alias,
                this.desa, this.keyFollowUp, this.keyPersilCat, this.jenisAlasHak, this.kepemilikan,
                this.keyProject, this.keyDesa
            )
                =
            (
                core.alasHak, core.pemilik, core.group, core.notaris, core.luasSurat, core.hargaSatuan,
                core.FileToNotary, core.FileToPra, core.Categories, core.NotesBelumLengkap, core.AdminToPraDP,
                core.AdminToPraLunas, ppjb, nib, cekCert, bukaBlokir, revisiNop, revisiGeo, balikNama, shm, core.alias,
                core.desa, core.followUpKey, keyPersilCat, core.jenisAlasHak, core.kepemilikan,
                core.keyProject, core.keyDesa
            );

            return this;
        }

        public PraDealsDetailView ToView(string reqKey, Notaris notaris_, List<user> securities, string idBidang, string nomorFU, List<ExtLandropeContext.entitas> villages)
        {
            string namaNotaris = (this.notaris == null || notaris_ == null) ? "" : notaris_.identifier;
            string desaView = this.keyDesa != null ? villages.FirstOrDefault(v => v.key == this.keyDesa)?.identity ?? "" : this.desa;

            var reasonView = this.reasons.Select(x => new ReasonView
            {
                reason = x.reason,
                jenisBayar = Enum.GetName(typeof(JenisBayar), x.jenisBayar),
                reasonDate = x.reasonDate,
                user = securities.Count() != 0 ? securities.FirstOrDefault(y => y.key == x.user)?.FullName : ""
            });

            var view = new PraDealsDetailView();
            (
                view.reqKey, view.key, view.AlasHak, view.Pemilik,
                view.Group, view.keyNotaris, view.notaris, view.luasSurat,
                view.keyBundle, view.idBidang, view.dealDP, view.statusDeal,
                view.hargaSatuan, view.created, view.dealLunas, view.reasons, view.Alias,
                view.followUpKey, view.nomorFollowUp, view.desa
            )
                =
            (
                reqKey, key, alasHak, pemilik,
                group, notaris, namaNotaris, luasSurat,
                keyBundle, idBidang, dealDP, (dealDP != null || dealLunas != null ? "Deal" : ""),
                hargaSatuan, created, dealLunas, reasonView.ToArray(), this.alias,
                this.keyFollowUp ?? "", nomorFU ?? "", desaView
            );

            return view;
        }

        public PraDealsDetailViewExt ToViewExt(string reqKey, List<Notaris> listNotaris,
                                               string idBidang, List<Category> categories,
                                               List<user> securities,
                                               List<(ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)> villages,
                                               string nomorFu,
                                               List<PersilCategories> listPersilCat
                                              )
        {
            var notary = listNotaris.FirstOrDefault(n => n.key == this.notaris);
            string namaNotaris = notary == null ? "" : notary.identifier;
            FormEndDates endDates = new FormEndDates();
            string desa = string.IsNullOrEmpty(this.keyDesa) ? this.desa : villages.FirstOrDefault(v => v.desa.key == this.keyDesa).desa?.identity;
            string project = villages.FirstOrDefault(v => v.project.key == this.keyProject).project?.identity;

            var persilCat = listPersilCat.FirstOrDefault(x => x.key == this.keyPersilCat);
            string[] categories1 = persilCat != null ? persilCat?.categories1?.LastOrDefault()?.keyCategory.ToArray() : new string[0];

            (endDates.BalikNamaEndDate, endDates.CertEndDate, endDates.GeoKppEndDate, endDates.NOPReviseEndDate, endDates.OpenBlockEndDate)
                =
            (
                BalikNama == null ? null : BalikNama.TanggalSelesai?.AddHours(7),
                CekSertifikat == null ? null : CekSertifikat?.TanggalSelesai?.AddHours(7),
                RevisiGeoKPP == null ? null : RevisiGeoKPP.TanggalSelesai?.AddHours(7),
                RevisiNop == null ? null : RevisiNop.TanggalSelesai?.AddHours(7),
                BukaBlokir == null ? null : BukaBlokir.TanggalSelesai?.AddHours(7)
            );

            var reasonView = this.reasons.Select(x => new ReasonView
            {
                reason = x.reason,
                jenisBayar = Enum.GetName(typeof(JenisBayar), x.jenisBayar),
                reasonDate = Convert.ToDateTime(x.reasonDate).ToLocalTime(),
                user = securities.Count() != 0 ? securities.FirstOrDefault(y => y.key == x.user)?.FullName : ""
            });

            var catView = this.Categories.Select(x => new
            {
                key = x,
                desc = categories.FirstOrDefault(c => c.key == x)?.desc
            });


            var view = new PraDealsDetailViewExt();
            (
                view.reqKey, view.key, view.AlasHak, view.Pemilik,
                view.Group, view.keyNotaris, view.notaris, view.luasSurat,
                view.keyBundle, view.idBidang, view.dealDP, view.statusDeal,
                view.hargaSatuan, view.FileToNotary, view.FileToPra,
                view.NotesBelumLengkap, view.AdminToPraDP, view.AdminToPraLunas, view.Ppjb,
                view.NibPerorangan, view.CekSertifikat, view.BukaBlokir, view.RevisiNop,
                view.RevisiGeoKpp, view.BalikNama, view.Shm, view.EndDates, view.reasons,
                view.Alias, view.desa, view.followUpKey, view.nomorFollowUp, view.jenisAlasHak,
                view.kepemilikan, view.Categories, view.keyProject, view.keyDesa, view.project
            )
                =
            (
                reqKey, key, alasHak, pemilik,
                group, notaris, namaNotaris, luasSurat,
                keyBundle, idBidang, dealDP, (dealDP != null ? "Deal" : ""),
                hargaSatuan, FileToNotary?.AddHours(7), FileToPra?.AddHours(7),
                NotesBelumLengkap, AdminToPraDP?.AddHours(7), AdminToPraLunas?.AddHours(7), PPJB.ToView(),
                NIBPerorangan.ToView(), CekSertifikat.ToView(), BukaBlokir.ToView(), RevisiNop.ToView(),
                RevisiGeoKPP.ToView(), BalikNama.ToView(), Shm.ToView(), endDates, reasonView.ToArray(),
                this.alias, desa, this.keyFollowUp ?? "", nomorFu ?? "",
                this.jenisAlasHak != null ? this.jenisAlasHak.ToString().ToUpper() : "", 
                this.kepemilikan != null ? this.kepemilikan.ToString().ToUpper() : "",
                catView.Select(x => new CategoryL { keyCategory = x.key, desc = x.desc }).ToArray(), keyProject, keyDesa, project
            );
            return view;
        }
    }

    [Entity("FormPraBebas", "formPraBebas")]
    public class FormPraBebas : namedentity3
    {
        public string keyPraDeal { get; set; }
        public string keyDetailPraDeal { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? FileToNotary { get; set; }
        public DateTime? FileToPra { get; set; }
        public string[] Categories { get; set; }
        public string NotesBelumLengkap { get; set; }
        public DateTime? AdminToPraDP { get; set; }
        public DateTime? AdminToPraLunas { get; set; }
        public Base PPJB { get; set; }
        public Base NIBPerorangan { get; set; }
        public Child CekSertifikat { get; set; }
        public Child BukaBlokir { get; set; }
        public Child RevisiNop { get; set; }
        public Child RevisiGeoKPP { get; set; }
        public Child BalikNama { get; set; }
        public Base Shm { get; set; }

        public FormPraBebas FromCore(DetailsPraBebasCore core)
        {
            var ppjb = new Base();
            (ppjb.NomorSurat, ppjb.TanggalSurat, ppjb.Notaris) = (core.Ppjb?.NomorSurat, core.Ppjb?.TanggalSurat, core.Ppjb?.Notaris);

            var nib = new Base();
            (nib.NomorSurat, nib.TanggalSurat, nib.Notaris) = (core.NibPerorangan?.NomorSurat, core.NibPerorangan?.TanggalSurat, core.NibPerorangan?.Notaris);

            var cekCert = new Child();
            (cekCert.NomorSurat, cekCert.TanggalSurat, cekCert.Notaris, cekCert.TanggalSelesai)
                =
            (core.CekSertifikat?.NomorSurat, core.CekSertifikat?.TanggalSurat, core.CekSertifikat?.Notaris, core.EndDates?.CertEndDate);

            var bukaBlokir = new Child();
            (bukaBlokir.NomorSurat, bukaBlokir.TanggalSurat, bukaBlokir.Notaris, bukaBlokir.TanggalSelesai)
                =
            (core.BukaBlokir?.NomorSurat, core.BukaBlokir?.TanggalSurat, core.BukaBlokir?.Notaris, core.EndDates?.OpenBlockEndDate);

            var revisiNop = new Child();
            (revisiNop.NomorSurat, revisiNop.TanggalSurat, revisiNop.Notaris, revisiNop.TanggalSelesai)
                =
            (core.RevisiNop?.NomorSurat, core.RevisiNop?.TanggalSurat, core.RevisiNop?.Notaris, core.EndDates?.NOPReviseEndDate);

            var revisiGeo = new Child();
            (revisiGeo.NomorSurat, revisiGeo.TanggalSurat, revisiGeo.Notaris, revisiGeo.TanggalSelesai)
                =
            (core.RevisiGeoKpp?.NomorSurat, core.RevisiGeoKpp?.TanggalSurat, core.RevisiGeoKpp?.Notaris, core.EndDates?.GeoKppEndDate);

            var balikNama = new Child();
            (balikNama.NomorSurat, balikNama.TanggalSurat, balikNama.Notaris, balikNama.TanggalSelesai)
                =
            (core.BalikNama?.NomorSurat, core.BalikNama?.TanggalSurat, core.BalikNama?.Notaris, core.EndDates?.BalikNamaEndDate);

            var shm = new Base();
            (shm.NomorSurat, shm.TanggalSurat, shm.Notaris)
                =
            (core.Shm?.NomorSurat, core.Shm?.TanggalSurat, core.Shm?.Notaris);

            var form = new FormPraBebas();
            (
                form.FileToNotary, form.key, form.keyPraDeal, form.keyDetailPraDeal,
                form.FileToPra, form.Categories, form.AdminToPraDP, form.AdminToPraLunas,
                form.NotesBelumLengkap, form.PPJB, form.NIBPerorangan, form.CekSertifikat, form.BukaBlokir,
                form.RevisiNop, form.RevisiGeoKPP, form.BalikNama, form.Shm, Created
            )
                =
            (
                core?.FileToNotary, MakeKey, core?.reqKey, core?.detKey,
                core?.FileToPra, core?.Categories, core?.AdminToPraDP, core?.AdminToPraLunas,
                core?.NotesBelumLengkap, ppjb, nib, cekCert, bukaBlokir,
                revisiNop, revisiGeo, balikNama, shm, DateTime.Now
            );

            return form;
        }

        public FormPraBebas UpdateFromCore(DetailsPraBebasCore core)
        {
            var ppjb = new Base();
            (ppjb.NomorSurat, ppjb.TanggalSurat, ppjb.Notaris) = (core.Ppjb?.NomorSurat, core.Ppjb?.TanggalSurat, core.Ppjb?.Notaris);

            var nib = new Base();
            (nib.NomorSurat, nib.TanggalSurat, nib.Notaris) = (core.NibPerorangan?.NomorSurat, core.NibPerorangan?.TanggalSurat, core.NibPerorangan?.Notaris);

            var cekCert = new Child();
            (cekCert.NomorSurat, cekCert.TanggalSurat, cekCert.Notaris, cekCert.TanggalSelesai)
                =
            (core.CekSertifikat?.NomorSurat, core.CekSertifikat?.TanggalSurat, core.CekSertifikat?.Notaris, core.EndDates?.CertEndDate);

            var bukaBlokir = new Child();
            (bukaBlokir.NomorSurat, bukaBlokir.TanggalSurat, bukaBlokir.Notaris, bukaBlokir.TanggalSelesai)
                =
            (core.BukaBlokir?.NomorSurat, core.BukaBlokir?.TanggalSurat, core.BukaBlokir?.Notaris, core.EndDates?.OpenBlockEndDate);

            var revisiNop = new Child();
            (revisiNop.NomorSurat, revisiNop.TanggalSurat, revisiNop.Notaris, revisiNop.TanggalSelesai)
                =
            (core.RevisiNop?.NomorSurat, core.RevisiNop?.TanggalSurat, core.RevisiNop?.Notaris, core.EndDates?.NOPReviseEndDate);

            var revisiGeo = new Child();
            (revisiGeo.NomorSurat, revisiGeo.TanggalSurat, revisiGeo.Notaris, revisiGeo.TanggalSelesai)
                =
            (core.RevisiGeoKpp?.NomorSurat, core.RevisiGeoKpp?.TanggalSurat, core.RevisiGeoKpp?.Notaris, core.EndDates?.GeoKppEndDate);

            var balikNama = new Child();
            (balikNama.NomorSurat, balikNama.TanggalSurat, balikNama.Notaris, balikNama.TanggalSelesai)
                =
            (core.BalikNama?.NomorSurat, core.BalikNama?.TanggalSurat, core.BalikNama?.Notaris, core.EndDates?.BalikNamaEndDate);

            var shm = new Base();
            (shm.NomorSurat, shm.TanggalSurat, shm.Notaris)
                =
            (core.Shm?.NomorSurat, core.Shm?.TanggalSurat, core.Shm?.Notaris);

            (
                this.FileToNotary, this.FileToPra, this.Categories, this.AdminToPraDP,
                this.AdminToPraLunas, this.NotesBelumLengkap, this.PPJB, this.NIBPerorangan,
                this.CekSertifikat, this.BukaBlokir, this.RevisiNop, this.RevisiGeoKPP,
                this.BalikNama, this.Shm
            )
                =
            (
                core?.FileToNotary, core?.FileToPra, core?.Categories, core?.AdminToPraDP,
                core?.AdminToPraLunas, core?.NotesBelumLengkap, ppjb, nib,
                cekCert, bukaBlokir, revisiNop, revisiGeo,
                balikNama, shm
            );

            return this;
        }
    }

    public class Base
    {
        public string NomorSurat { get; set; }
        public DateTime? TanggalSurat { get; set; }
        public string Notaris { get; set; }

        public FormPraBebasBase ToView()
        {
            var view = new FormPraBebasBase();
            (view.NomorSurat, view.TanggalSurat, view.Notaris)
                =
            (this.NomorSurat, Convert.ToDateTime(this.TanggalSurat).ToLocalTime(), this.Notaris);
            return view;
        }
    }

    public class Child : Base
    {
        public DateTime? TanggalSelesai { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PraPembabasanResult
    {
        public PreResultDoc info { get; set; }

        [BsonExtraElements]
        public Dictionary<string, object> extraelems { get; set; }
    }

    public class ReasonPraDeal
    {
        public string reason { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public DateTime? reasonDate { get; set; }
        public string user { get; set; }

        public ReasonPraDeal(string reason, JenisBayar jenisBayar, DateTime? currentTime, string user)
        {
            (this.reason, this.jenisBayar, this.reasonDate, this.user)
                =
            (reason, jenisBayar, currentTime, user);
        }
    }

    public class DocsAccess
    {
        public string[] privs { get; set; }
        public string[] jnsDoks { get; set; }
    }

    [Entity("MeasurementRequest", "measurementRequest")]
    public class MeasurementRequest : namedentity
    {
        public string reason { get; set; }
        public string requestor { get; set; }
        public DateTime requestDate { get; set; }
        public HistoryMeasurementRequest[] historyRequest { get; set; } = new HistoryMeasurementRequest[0];

        public MeasurementRequest FromCore(string keyDetail, string reason, DateTime currentTime, string userKey)
        {

            (this.key, this.requestDate, requestor, this.reason)
                =
            (keyDetail, currentTime, userKey, reason);

            return this;
        }

        public MeasurementRequest AddHistory(HistoryMeasurementRequest history)
        {
            var lst = this.historyRequest.ToList();
            lst.Add(history);
            this.historyRequest = lst.ToArray();

            return this;
        }
    }

    public class HistoryMeasurementRequest
    {
        public string key { get; set; }
        public string reason { get; set; }
        public string requestor { get; set; }
        public DateTime requestDate { get; set; }
    }

    public class DetailPraDeal
    {
        public string reqKey { get; set; }
        public string key { get; set; }
        public string followUpKey { get; set; }
        public string nomorFollowUp { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public DateTime? created { get; set; }
        public string AlasHak { get; set; }
        public string Pemilik { get; set; }
        public string Alias { get; set; }
        public string Group { get; set; }
        public string Status { get; set; }
        public string keyBundle { get; set; }
        public string keyNotaris { get; set; }
        public string notaris { get; set; }
        public double? luasSurat { get; set; }
        public string idBidang { get; set; }
        public DateTime? dealUTJ { get; set; }
        public DateTime? dealDP { get; set; }
        public DateTime? dealLunas { get; set; }
        public ReasonPraDeal[] reasons { get; set; } = new ReasonPraDeal[0];
        public string statusDeal { get; set; }
        public double? hargaSatuan { get; set; }
        public string jenisAlasHak { get; set; }
        public string kepemilikan { get; set; }

        public PraDealsDetailView ToView(List<user> securities,
                                        List<(ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)> villages)
        {
            var reasonView = this.reasons.Select(x => new ReasonView
            {
                reason = x.reason,
                jenisBayar = Enum.GetName(typeof(JenisBayar), x.jenisBayar),
                reasonDate = x.reasonDate,
                user = securities.Count() != 0 ? securities.FirstOrDefault(y => y.key == x.user)?.FullName : ""
            }).ToArray();

            string xdesa = string.IsNullOrEmpty(this.keyDesa) ? this.desa : villages.FirstOrDefault(v => v.desa.key == this.keyDesa).desa?.identity;
            string project = villages.FirstOrDefault(v => v.project.key == this.keyProject).project?.identity;

            var view = new PraDealsDetailView();
            (
                view.reqKey, view.key, view.AlasHak, view.Pemilik,
                view.Group, view.keyNotaris, view.notaris, view.luasSurat,
                view.keyBundle, view.idBidang, view.dealDP, view.statusDeal,
                view.hargaSatuan, view.created, view.dealLunas, view.reasons, view.Alias, view.dealUTJ,
                view.followUpKey, view.nomorFollowUp, view.desa, view.jenisAlasHak, view.kepemilikan,
                view.keyDesa, view.keyProject, view.project
            )
            =
            (
                this.reqKey, this.key, this.AlasHak, this.Pemilik,
                this.Group, this.keyNotaris, this.notaris, this.luasSurat,
                this.keyBundle, this.idBidang, this.dealDP, this.statusDeal,
                this.hargaSatuan, this.created, this.dealLunas, reasonView, this.Alias, this.dealUTJ,
                this.followUpKey, this.nomorFollowUp, xdesa, this.jenisAlasHak, this.kepemilikan,
                this.keyDesa, this.keyProject, project
            );
            return view;
        }
    }


    public class MailReceivers
    {
        public string name { get; set; }
        public string email { get; set; }
    }

    public class AlertLuasPraDeal
    {
        public MailReceivers[] receivers { get; set; } = new MailReceivers[0];
        public MailReceivers[] receiversCC { get; set; } = new MailReceivers[0];
        public MailReceivers[] receiversBCC { get; set; } = new MailReceivers[0];
        public double tolerance { get; set; }
    }

    public class ReminderSiapPembayaran
    {
        public MailReceivers[] receivers { get; set; } = new MailReceivers[0];
        public MailReceivers[] receiversCC { get; set; } = new MailReceivers[0];
        public MailReceivers[] receiversBCC { get; set; } = new MailReceivers[0];
    }
}
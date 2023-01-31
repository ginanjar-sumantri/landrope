using GraphConsumer;
using landrope.consumers;
using Microsoft.AspNetCore.Mvc;
using mongospace;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using landrope.mod4;
using Tracer;
using landrope.mod3;
using landrope.api3.Models;
using landrope.common;
using landrope.mod2;
using MongoDB.Driver;

namespace landrope.api3.Controllers
{
    [Route("api/fish")]
    public class FishBoneController : Controller
    {
        IServiceProvider services;
        GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        LandropePayContext context = Contextual.GetContextPay();
        LandropePlusContext contextplus = Contextual.GetContextPlus();

        public FishBoneController(IServiceProvider services)
        {
            this.services = services;
            context = services.GetService<LandropePayContext>();
        }

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("bone")]
        public IActionResult GetFishBone(string token, string kp)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var projects = new List<string>();

                if (!string.IsNullOrEmpty(kp))
                    projects.Add(kp);
                else
                    projects = context.GetDocuments(new { key = "", identity = "" }, "maps",
                                "{$lookup : {from : 'projects_include', localField: 'key', foreignField : 'key', as : 'map'}}",
                                "{$match : {'map' : {$ne : []}}}",
                                @"{$project : { _id: 0, key: {$arrayElemAt: ['$map.key', -1]}}}")
                                .Select(x => x.key).ToList();

                var categories = context.GetCollections(new Category(), "categories_new", "{}", "{_id:0}").ToList().AsParallel();

                var allPersils = context.persils.Query(x => x.en_state != StatusBidang.batal && x.basic != null && x.basic.current != null)
                    .Where(x => x.IdBidang != null && x.basic.current.en_proses != null && projects.Contains(x.basic.current.keyProject))
                    .Select(x => new PersilFishBone
                    {
                        key = x.key,
                        IdBidang = x.IdBidang,
                        deal = x.deal,
                        keyProject = x.basic.current.keyProject,
                        state = x.en_state.GetValueOrDefault(),
                        proses = x.basic.current.en_proses,
                        luasSurat = x.basic.current.luasSurat ?? 0,
                        luasdiBayar = x.basic.current.luasDibayar ?? 0,
                        group = x.basic.current.group ?? "",
                        categories = x.categories.LastOrDefault(),
                        keySegment1 = x.categories.LastOrDefault()?.keyCategory.Skip(0).Take(1).FirstOrDefault(),
                        keySegment2 = x.categories.LastOrDefault()?.keyCategory.Skip(1).Take(1).FirstOrDefault(),
                        keySegment4 = x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault(),
                        keySegment6 = x.categories.LastOrDefault()?.keyCategory.Skip(5).Take(1).FirstOrDefault(),
                        Segment1 = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(0).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                        Segment2 = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(1).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                        Segment4 = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                        Segment6 = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(5).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                    }).ToArray().AsParallel();

                var hibahOverlapLuas = context.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().AsParallel();


                var standardKmp = allPersils.Where(x => x.state == StatusBidang.kampung && x.proses == JenisProses.standar).ToArray();
                var standardBB = allPersils.Where(x => x.state == StatusBidang.belumbebas && x.proses == JenisProses.standar).ToArray();
                var hibah = allPersils.Where(x => x.proses == JenisProses.hibah).ToArray();
                var hibahTrans = hibah.Join(hibahOverlapLuas, p => p.IdBidang, h => h.IdBidang,
                    (p, h) => p.hitungLuas(h.overlap.Count() == 0 ? 0 : h.overlap.Select(x => x.luas).Sum()
                )).ToArray();

                (var keyCatKeluar,
                    var keyCatDeal,
                    var keyCatLain,
                    var keyCatGroupKecil,
                    var keyCatBlmKetemu,
                    var keyCatSulit,
                    var keyCatMudah,
                    var keyCatSdhKetemu,
                    var keyCatGroupBesar,
                    var keyCatSengketa,
                    var keyCatDKI,
                    var keyCatPidana,
                    var keyCatUrusanPidana,
                    var keyCatAsset,
                    var keyCatSaluran,
                    var keyCatRelokasi,
                    var keyCatPemda,
                    var keyCatMakam,
                    var keyCatKebonKelapa,
                    var keyCatBengkok
                )
                    =
                (
                    categories.FirstOrDefault(x => x.desc == "DIKELUARKAN DARI PLANNING" && x.segment == 1)?.key,
                    categories.FirstOrDefault(x => x.desc == "DEAL" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "LAIN - LAIN" && x.segment == 1)?.key,
                    categories.FirstOrDefault(x => x.desc == "GROUP KECIL")?.key,
                    categories.FirstOrDefault(x => x.desc == "BELUM KETEMU")?.key,
                    categories.FirstOrDefault(x => x.desc == "SULIT" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "MUDAH" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "SUDAH KETEMU")?.key,
                    categories.FirstOrDefault(x => x.desc == "GROUP BESAR")?.key,
                    categories.FirstOrDefault(x => x.desc == "SENGKETA")?.key,
                    categories.FirstOrDefault(x => x.desc == "DKI")?.key,
                    categories.FirstOrDefault(x => x.desc == "PIDANA")?.key,
                    categories.FirstOrDefault(x => x.desc == "URUSAN PIDANA")?.key,
                    categories.FirstOrDefault(x => x.desc == "ASSET" && x.segment == 1)?.key,
                    categories.FirstOrDefault(x => x.desc == "SALURAN" && x.segment == 2)?.key,
                    categories.FirstOrDefault(x => x.desc == "RELOKASI" && x.segment == 2)?.key,
                    categories.FirstOrDefault(x => x.desc == "PEMDA" && x.segment == 2)?.key,
                    categories.FirstOrDefault(x => x.desc == "MAKAM" && x.segment == 2)?.key,
                    categories.FirstOrDefault(x => x.desc == "KEBON KELAPA" && x.segment == 2)?.key,
                    categories.FirstOrDefault(x => x.desc == "BENGKOK DESA" && x.segment == 2)?.key
                );

                var dtlsBelumBebasNormatif = new List<FishBone>();
                var dtlsLainLain = new List<FishBone>();
                var dtlsBelumDeal = new List<FishBone>();
                var dtlsTarget = new List<FishBone>();
                var dtlsGroupKecil = new List<FishBone>();
                var dtlsGroupBesar = new List<FishBone>();
                var dtlsOverlap = new List<FishBone>();
                var dtlsOVGroupBesar = new List<FishBone>();

                #region Belum Bebas Normatif - Lain lain - saluran/bolong

                var saluran = hibah.Where(x =>  x.keySegment1 == keyCatAsset
                                                &&  x.deal == null
                                                && x.keySegment2 == keyCatSaluran).Sum(x => x.luasSurat);

                var dtSaluran = new FishBone
                {
                    cat = "SALURAN",
                    value = saluran / 10000
                };

                dtlsLainLain.Add(dtSaluran);

                #endregion

                #region Belum Bebas Normatif - Lain lain - relokasi

                var relokasi = standardBB.Where(x => x.keySegment1 == keyCatAsset
                                                    &&  x.deal == null
                                                    && x.keySegment2 == keyCatRelokasi).Sum(x => x.luasSurat);
                var dtRelokasi = new FishBone
                {
                    cat = "RELOKASI",
                    value = relokasi / 10000
                };

                dtlsLainLain.Add(dtRelokasi);

                #endregion

                #region Belum Bebas Normatif - Lain lain - pemda

                var pemda = standardBB.Where(x => x.keySegment1 == keyCatAsset
                                                    && x.deal == null
                                                    && x.keySegment2 == keyCatPemda).Sum(x => x.luasSurat);

                var dtPemda = new FishBone
                {
                    cat = "PEMDA",
                    value = pemda / 10000
                };

                dtlsLainLain.Add(dtPemda);

                #endregion

                #region Belum Bebas Normatif - Lain lain - makam

                var makam = standardBB.Where(x => x.keySegment1 == keyCatAsset
                                                    && x.deal == null
                                                    && x.keySegment2 == keyCatMakam).Sum(x => x.luasSurat);

                var dtMakam = new FishBone
                {
                    cat = "MAKAM",
                    value = makam / 10000
                };

                dtlsLainLain.Add(dtMakam);

                #endregion

                #region Belum Bebas Normatif - Lain lain - kebon kelapa

                var kebonKelapa = standardBB.Where(x => x.keySegment1 == keyCatAsset
                                                    && x.deal == null
                                                    && x.keySegment2 == keyCatKebonKelapa).Sum(x => x.luasSurat);

                var dtKebonKelapa = new FishBone
                {
                    cat = "KEBUN KELAPA",
                    value = kebonKelapa / 10000
                };

                dtlsLainLain.Add(dtKebonKelapa);

                #endregion

                #region Belum Bebas Normatif - Lain lain - kampung

                var kampung = standardKmp.Where(x => x.keySegment1 != keyCatKeluar
                                                    && (x.keySegment6 != keyCatDeal
                                                    && x.deal == null)).Sum(x => x.luasSurat);

                var dtKampung = new FishBone
                {
                    cat = "KAMPUNG",
                    value = kampung / 10000
                };

                dtlsLainLain.Add(dtKampung);

                #endregion

                #region Belum Bebas Normatif - Lain lain - bengkok desa

                var bengkokDesa = standardBB.Where(x => x.keySegment1 == keyCatAsset
                                                    && x.deal == null
                                                    && x.keySegment2 == keyCatBengkok).Sum(x => x.luasSurat);

                var dtBengkokDesa = new FishBone
                {
                    cat = "BENGKOK DESA",
                    value = bengkokDesa / 10000
                };

                dtlsLainLain.Add(dtBengkokDesa);

                #endregion

                #region Belum Bebas Normatif - Lain lain

                var lainLain = (saluran + relokasi + pemda + makam + kebonKelapa + kampung + bengkokDesa) / 10000;

                var dtLainLain = new FishBone
                {
                    cat = "LAIN - LAIN",
                    value = lainLain,
                    detail = dtlsLainLain.ToArray(),
                    keySegment1 = keyCatLain
                };

                dtlsBelumBebasNormatif.Add(dtLainLain);
                #endregion

                #region Belum Bebas Normatif - Group Kecil

                var GroupKecil = standardBB.Where(x => x.keySegment1 == keyCatGroupKecil
                                                && x.deal == null && x.keySegment6 != keyCatDeal)
                                    .GroupBy(x => (x.keySegment1, x.Segment1), (y, z) =>
                                    new FishBone
                                    {
                                        cat = y.Segment1,
                                        keySegment1 = y.keySegment1,
                                        Segment1 = y.Segment1,
                                        value = z.Sum(x => x.luasSurat) / 10000,
                                        detail = z.Where(x => x.keySegment1 == keyCatGroupKecil
                                                && x.deal == null && x.keySegment6 != keyCatDeal).GroupBy(x => (x.keySegment1, x.keySegment6, x.Segment6), (y, z) =>
                                        new FishBone
                                        {
                                            cat = y.Segment6,
                                            keySegment1 = y.keySegment1,
                                            keySegment6 = y.keySegment6,
                                            value = z.Sum(x => x.luasSurat) / 10000,
                                            detail = z.GroupBy(x => (x.keySegment4, x.Segment4), (y, z) =>
                                            new FishBone
                                            {
                                                cat = y.Segment4,
                                                value = z.Sum(x => x.luasSurat) / 10000
                                            }).ToArray()
                                        }).ToArray()
                                    }).FirstOrDefault();

                if (GroupKecil != null)
                    dtlsBelumBebasNormatif.Add(GroupKecil);

                #endregion

                #region Belum Bebas Normatif - Group Besar

                var GroupBesarBB = standardBB.Where(x => x.keySegment1 == keyCatGroupBesar
                                                && x.deal == null && x.keySegment6 != keyCatDeal)
                                    .GroupBy(x => (x.keySegment1, x.Segment1), (y, z) =>
                                    new FishBone
                                    {
                                        cat = y.Segment1,
                                        keySegment1 = y.keySegment1,
                                        Segment1 = y.Segment1,
                                        value = z.Sum(x => x.luasSurat) / 10000,
                                        detail = z.GroupBy(x => (x.keySegment1, x.keySegment6, x.Segment6), (y, z) =>
                                        new FishBone
                                        {
                                            cat = (string.IsNullOrEmpty(y.Segment6) ? "LAIN - LAIN" : y.Segment6),
                                            keySegment1 = y.keySegment1,
                                            keySegment6 = y.keySegment6,
                                            value = z.Sum(x => x.luasSurat) / 10000
                                        }).ToArray()
                                    }).FirstOrDefault();

                if (GroupBesarBB != null)
                    dtlsBelumBebasNormatif.Add(GroupBesarBB);

                #endregion

                #region BELUM BEBAS NORMATIF

                var belumBebasNormatif = (GroupBesarBB == null ? 0 : GroupBesarBB.value) + (GroupKecil == null ? 0 : GroupKecil.value) + lainLain;

                var dtBelumBebasNormatif = new FishBone
                {
                    cat = "BELUM BEBAS NORMATIF",
                    value = belumBebasNormatif,
                    detail = dtlsBelumBebasNormatif.ToArray()
                };

                dtlsBelumDeal.Add(dtBelumBebasNormatif);

                #endregion

                #region Overlap - Belum Claim

                var OVBlmClaim = hibahTrans.Where(x => x.categories == null).Sum(x => x.luasdiBayar);

                var dtOVBlmClaim = new FishBone
                {
                    cat = "BELUM CLAIM",
                    value = OVBlmClaim / 10000
                };

                dtlsOverlap.Add(dtOVBlmClaim);

                #endregion

                #region Overlap - Group Besar

                var OVGroupBesar = hibahTrans.Where(x => x.keySegment1 == keyCatGroupBesar && x.categories != null)
                                    .GroupBy(x => (x.keySegment1, x.Segment1), (y, z) =>
                                    new FishBone
                                    {
                                        cat = y.Segment1,
                                        keySegment1 = y.keySegment1,
                                        Segment1 = y.Segment1,
                                        value = z.Sum(x => x.luasdiBayar) / 10000,
                                        detail = z.GroupBy(x => (x.keySegment1, x.keySegment6, x.Segment6), (y, z) =>
                                        new FishBone
                                        {
                                            cat = y.Segment6,
                                            keySegment1 = y.keySegment1,
                                            keySegment6 = y.keySegment6,
                                            value = z.Sum(x => x.luasdiBayar) / 10000
                                        }).ToArray()
                                    }).FirstOrDefault();

                if (OVGroupBesar != null)
                    dtlsOverlap.Add(OVGroupBesar);

                #endregion

                #region Overlap

                var Overlap = (OVGroupBesar == null ? 0 : OVGroupBesar.value) + (OVBlmClaim / 10000);

                var dtOverlap = new FishBone
                {
                    cat = "OVERLAP",
                    value = Overlap,
                    detail = dtlsOverlap.ToArray()
                };

                dtlsBelumDeal.Add(dtOverlap);

                #endregion

                #region Belum Deal

                var BelumDeal = Overlap + belumBebasNormatif;

                var dtBelumDeal = new FishBone
                {
                    cat = "BELUM DEAL",
                    value = BelumDeal,
                    detail = dtlsBelumDeal.ToArray()
                };

                dtlsTarget.Add(dtBelumDeal);

                #endregion

                #region Deal

                var DealUnion = standardBB.Where(x => x.deal != null || x.keySegment6 == keyCatDeal).ToList();

                var Deal = DealUnion.Sum(x => x.luasSurat);

                var dtDeal = new FishBone
                {
                    cat = "DEAL",
                    value = Deal / 10000
                };

                dtlsTarget.Add(dtDeal);

                #endregion

                #region Target

                var Target = (Deal / 10000) + BelumDeal;

                var dtTarget = new FishBone
                {
                    cat = "TARGET",
                    value = Target,
                    detail = dtlsTarget.ToArray()
                };

                #endregion

                return new JsonResult(dtTarget);

            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("bone/v2")]
        public IActionResult GetFishBoneNew(string token)
        {
            try
            {
                var user = contextplus.FindUser(token);
                //var projects = new List<string>();

                //if (!string.IsNullOrEmpty(kp))
                //    projects.Add(kp);
                //else
                //    projects = context.GetCollections(new { key = "" }, "projects_include", "{}", "{_id:0, key:1}").ToList().Select(x => x.key).ToList();

                //var data = context.GetCollections(new FishBone_v2(), "pbi_FB_ALL", "{}", "{_id:0}").ToList().Where(x => projects.Contains(x.project)).ToArray();

                var data = context.GetCollections(new FishBone_v2(), "pbi_FB_ALL", "{}", "{_id:0}").ToList();

                return new JsonResult(data);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }

        }

        [HttpGet("projects")]
        public IActionResult GetProjectList()
        {
            try
            {
                var data = context.GetDocuments(new { key = "", identity = "" }, "maps",
                                 "{$lookup : {from : 'projects_include', localField: 'key', foreignField : 'key', as : 'map'}}",
                                 "{$match : {'map' : {$ne : []}}}",
                                 @"{$project : {
                                        _id: 0,
                                        key: {$arrayElemAt: ['$map.key', -1]},
                                        identity: {$arrayElemAt: ['$map.identity', -1]}
                                   }}").ToList()
                                  .Select(p => new cmnItem { key = p.key, name = p.identity })
                                  .OrderBy(p => p.name).ToList();
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        public class PersilFishBone
        {
            public string key { get; set; }
            public string IdBidang { get; set; }
            public DateTime? deal { get; set; }
            public string keyProject { get; set; }
            public StatusBidang state { get; set; }
            public JenisProses proses { get; set; }
            public double luasSurat { get; set; }
            public double luasdiBayar { get; set; }
            public string group { get; set; }
            public string keySegment1 { get; set; }
            public string Segment1 { get; set; }
            public string keySegment2 { get; set; }
            public string Segment2 { get; set; }
            public string keySegment4 { get; set; }
            public string Segment4 { get; set; }
            public string keySegment6 { get; set; }
            public string Segment6 { get; set; }
            public category categories { get; set; }

            public PersilFishBone hitungLuas(double? overlap)
            {
                if (overlap != null)
                    luasdiBayar = (luasdiBayar - (double)overlap) < 0 ? 0 : luasdiBayar - (double)overlap;

                return this;
            }
        }
    }
}
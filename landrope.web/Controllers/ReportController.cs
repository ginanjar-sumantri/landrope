using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using landrope.mod;
using landrope.mod.shared;
using MongoDB.Driver;
using Newtonsoft.Json;
using landrope.web.Models;
using landrope.mod2;
using APIGrid;
using MongoDB.Driver.Encryption;
using mongospace;
using Microsoft.AspNetCore.Authentication;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using SharpCompress.Common;
using auth.mod;
using System.Threading;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using landrope.common;
using netDxf;
using Microsoft.AspNetCore.Cors;
using System.Threading.Tasks;
using landrope.mod4;
using landrope.mod3;
using System.Text;
using Tracer;
//using GridMvc.Server;

namespace landrope.web.Controllers
{
    [ApiController]
    [Route("api/report/")]
    public class ReportController : ControllerBase

    {
        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextex = Contextual.GetContextExt();
        LandropePlusContext contextplus = Contextual.GetContextPlus();


        public class pivot
        {
            public class step
            {
                public string k { get; set; }
                public int v { get; set; }
            }
            public string key { get; set; }
            public double? luasSurat { get; set; }
            public double luasDibayar { get; set; }
            public double luasPBT { get; set; }
            public string project { get; set; }
            public string desa { get; set; }
            public string PT { get; set; }
            public step[] steps { get; set; }
            public string nohgb { get; set; }


            public Dictionary<string, int> dsteps = new Dictionary<string, int>();
            public pivot ToDict()
            {
                dsteps = steps.ToDictionary(s => s.k, s => s.v);
                return this;
            }
        }

        public class pivot_girik : pivot
        {
            public string nopbt { get; set; }
        }


        public class pbtgroup
        {
            public string key { get; set; }
            public double luaspart { get; set; }
            public double luaspbt { get; set; }
            public int count { get; set; }
            public double ratio { get; set; }
        }


        public class hgbgroup
        {
            public string key { get; set; }
            public double luaspart { get; set; }
            public double luashgb { get; set; }
            public int count { get; set; }
            public double ratio { get; set; }
        }

        public class byr
        {
            public int nomorTahap { get; set; }
            public bidang[] bidangs { get; set; }

        }

        public class bidang
        {
            public string key { get; set; }
            public string keyPersil { get; set; }
        }


        //[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
        [EnableCors(nameof(landrope))]
        [HttpGet("progress/serti")]
        public IActionResult GetProgressSerti()
        {
            var steporders = new[] {
                ( id:0, step:"shm"),
                ( id:1, step:"orturun"),
                ( id:2, step:"ornaik"),
                ( id:3, step:"turun"),
                ( id:4, step:"naik"),
                ( id:5, step:"prosajb"),
                ( id:6, step:"orbalik"),
                ( id:7, step:"balik"),
            };
            try
            {
                List<pivot> pivot_shm = new List<pivot>();
                List<pivot> pivot_shp = new List<pivot>();
                List<pivot> pivot_hgb = new List<pivot>();
                var hgbs = new List<hgbgroup>();

                var task1 = Task.Run(() =>
                {
                    pivot_shm = context.GetCollections(new pivot(), "material_pivot_shm", "{}", "{_id:0}").ToList().Select(s => s.ToDict()).ToList();
                    pivot_shm.ForEach(p =>
                    {
                        p.dsteps.Add("shm", 1);
                        steporders.OrderByDescending(so => so.id).ToList().ForEach(so =>
                        {
                            var step = so.step;
                            if (p.dsteps.TryGetValue(step, out int val) && val == 1)
                                for (int x = 0; x < so.id; x++)
                                {
                                    var rstep = steporders[x].step;
                                    p.dsteps[rstep] = 0;
                                }
                        });
                    });
                });

                var task2 = Task.Run(() =>
                {
                    pivot_shp = context.GetCollections(new pivot(), "material_pivot_shp", "{}", "{_id:0}").ToList().Select(s => s.ToDict()).ToList();
                    pivot_shp.ForEach(p =>
                    {
                        steporders.OrderByDescending(so => so.id).ToList().ForEach(so =>
                        {
                            var step = so.step;
                            if (p.dsteps.TryGetValue(step, out int val) && val == 1)
                                for (int x = 0; x < so.id; x++)
                                {
                                    var rstep = steporders[x].step;
                                    p.dsteps[rstep] = 0;
                                }
                        });
                    });
                });

                var task3 = Task.Run(() =>
                {
                    pivot_hgb = context.GetCollections(new pivot(), "material_pivot_hgb", "{}", "{_id:0}").ToList().Select(s => s.ToDict()).ToList();
                    pivot_hgb.ForEach(p =>
                    {
                        p.dsteps.Add("turun", 1);
                        steporders.OrderByDescending(so => so.id).ToList().ForEach(so =>
                        {
                            var step = so.step;
                            if (p.dsteps.TryGetValue(step, out int val) && val == 1)
                                for (int x = 0; x < so.id; x++)
                                {
                                    var rstep = steporders[x].step;
                                    p.dsteps[rstep] = 0;
                                }
                        });
                    });
                });

                var task4 = Task.Run(() => hgbs = context.GetCollections(new hgbgroup(), "material_group_hgb", "{}", "{_id:0}").ToList());
                Task.WaitAll(task1, task2, task3, task4);

                //var nohgbs = pivot_shm.Select(p => p.nohgb).Union(pivot_hgb.Select(p => p.nohgb)).Union(pivot_shp.Select(p => p.nohgb));
                //hgbs = hgbs.Union(nohgbs.Except(hgbs.Select(h => h.key)).Select(n => new hgbgroup { key = n, ratio=1})).ToList();
                //hgbs = hgbs.Select(h =>
                //{
                //	if (h.luashgb == 0 || h.luaspart == 0)
                //		(h.luashgb, h.luaspart) = (1, 1);
                //	return h;
                //}).ToList();
                var res_shm = pivot_shm//.Join(hgbs,p=>p.nohgb, h=>h.key,(p,h)=>(p,h.ratio))
                .Select(p => new
                {
                    /*x.*/
                    p.key,
                    /*x.*/
                    p.project,
                    /*x.*/
                    p.desa,
                    /*x.*/
                    p.PT,
                    /*x.*/
                    p.luasDibayar,
                    p.nohgb,
                    luasSurat = /*x.*/p.luasSurat ?? /*x.*/p.luasDibayar,
                    shm = /*x.*/p.dsteps.TryGetValue("shm", out int oshm) ? oshm * /*x.*/p.luasDibayar : 0d,
                    orturun = /*x.*/p.dsteps.TryGetValue("orturun", out int oorturun) ? oorturun * /*x.*/p.luasDibayar : 0d,
                    hgb = /*x.*/p.dsteps.TryGetValue("turun", out int oturun) ? oturun * /*x.*/p.luasDibayar : 0d,
                    ajb = /*x.*/p.dsteps.TryGetValue("prosajb", out int oprosajb) ? oprosajb * /*x.*/p.luasDibayar : 0d,
                    orbalik = /*x.*/p.dsteps.TryGetValue("orbalik", out int oorbalik) ? oorbalik * /*x.*/p.luasDibayar : 0d,
                    balik = /*x.*/p.dsteps.TryGetValue("balik", out int obalik) ? obalik * /*x.*/p.luasDibayar : 0d// * /*x.*/ratio: 0d
                });

                var res_shp = pivot_shp//.Join(hgbs, p => p.nohgb, h => h.key, (p, h) => (p, h.ratio))
                    .Select(p => new
                    {
                        /*x.*/
                        p.key,
                        /*x.*/
                        p.project,
                        /*x.*/
                        p.desa,
                        /*x.*/
                        p.PT,
                        /*x.*/
                        p.luasDibayar,
                        p.nohgb,
                        luasSurat = /*x.*/p.luasSurat ?? /*x.*/p.luasDibayar,
                        shm = 0d,
                        orturun = 0d,
                        hgb = /*x.*/p.dsteps.TryGetValue("naik", out int onaik) ? onaik * /*x.*/p.luasDibayar : 0d,
                        ajb = /*x.*/p.dsteps.TryGetValue("prosajb", out int oprosajb) ? oprosajb * /*x.*/p.luasDibayar : 0d,
                        orbalik = /*x.*/p.dsteps.TryGetValue("orbalik", out int oorbalik) ? oorbalik * /*x.*/p.luasDibayar : 0d,
                        balik = /*x.*/p.dsteps.TryGetValue("balik", out int obalik) ? obalik * /*x.*/p.luasDibayar : 0//d * /*x.*/ratio: 0d
                    });

                var res_hgb = pivot_hgb//.Join(hgbs, p => p.nohgb, h => h.key, (p, h) => (p, h.ratio))
                    .Select(p => new
                    {
                        /*x.*/
                        p.key,
                        /*x.*/
                        p.project,
                        /*x.*/
                        p.desa,
                        /*x.*/
                        p.PT,
                        /*x.*/
                        p.luasDibayar,
                        p.nohgb,
                        luasSurat = /*x.*/p.luasSurat ?? /*x.*/p.luasDibayar,
                        shm = 0d,
                        orturun = 0d,
                        hgb = /*x.*/p.dsteps.TryGetValue("turun", out int onaik) ? onaik * /*x.*/p.luasDibayar : 0d,
                        ajb = /*x.*/p.dsteps.TryGetValue("prosajb", out int oprosajb) ? oprosajb * /*x.*/p.luasDibayar : 0d,
                        orbalik = /*x.*/p.dsteps.TryGetValue("orbalik", out int oorbalik) ? oorbalik * /*x.*/p.luasDibayar : 0d,
                        balik = /*x.*/p.dsteps.TryGetValue("balik", out int obalik) ? obalik * /*x.*/p.luasDibayar : 0d//* /*x.*/ratio: 0d
                    });

                var res_all = res_shm.Union(res_shp).Union(res_hgb)
                        .GroupJoin(hgbs, p => p.nohgb, h => h.key, (p, sh) => (p, sh.DefaultIfEmpty(new hgbgroup { ratio = 1 }).First().ratio))
                        .GroupBy(x => (x.p.project, x.p.desa, x.p.PT))
                        .Select(g => new
                        {
                            g.Key.project,
                            g.Key.desa,
                            g.Key.PT,
                            dibayar = g.Sum(x => x.p.luasDibayar),
                            surat = g.Sum(x => x.p.luasSurat),
                            shm = g.Sum(x => x.p.shm),
                            turun = g.Sum(x => x.p.orturun),
                            hgb = g.Sum(x => x.p.hgb),
                            ajb = g.Sum(x => x.p.ajb),
                            balik = g.Sum(x => x.p.orbalik),
                            hgbpt = g.Sum(x => x.p.balik * x.ratio)
                        }).ToArray();

                return Ok(res_all);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpGet("progress/girik")]
        public IActionResult GetProgressGririk()
        {
            var steporders = new[] {
                ( id:0, step:"nonibo"),
                ( id:1, step:"nibo"),
                ( id:2, step:"nibp"),
                ( id:3, step:"skkt"),
                ( id:4, step:"skkw"),
                ( id:5, step:"ctbuku"),
                ( id:6, step:"buku")
            };
            try
            {
                var pivot_gir = new List<pivot_girik>();
                var pbts = new List<pbtgroup>();
                var hgbs = new List<hgbgroup>();

                var task1 = Task.Run(() =>
                {
                    pivot_gir = context.GetCollections(new pivot_girik(), "material_pivot_girik", "{}", "{_id:0}").ToList().Select(s => s.ToDict()).Cast<pivot_girik>().ToList();
                    pivot_gir.ForEach(p =>
                    {
                        int nibo = 0;
                        p.dsteps.TryGetValue("nibo", out nibo);
                        p.dsteps.Add("nonibo", 1 - nibo);

                        steporders.OrderByDescending(so => so.id).ToList().ForEach(so =>
                        {
                            var step = so.step;
                            if (p.dsteps.TryGetValue(step, out int val) && val == 1)
                                for (int x = 0; x < so.id; x++)
                                {
                                    var rstep = steporders[x].step;
                                    p.dsteps[rstep] = 0;
                                }
                        });
                    });
                });

                var task2 = Task.Run(() => pbts = context.GetCollections(new pbtgroup(), "material_group_pbt", "{}", "{_id:0}").ToList());
                var task3 = Task.Run(() => hgbs = context.GetCollections(new hgbgroup(), "material_group_hgb", "{}", "{_id:0}").ToList());

                Task.WaitAll(task1, task2, task3);

                var combo = pivot_gir.GroupJoin(pbts, p => p.nopbt ?? "1", s => s.key,
                    (p, ss) => (p, pb: ss.DefaultIfEmpty(new pbtgroup { luaspart = p.luasDibayar, count = 1, luaspbt = p.luasDibayar }).First()))
                    .GroupJoin(hgbs, p => p.p.nohgb ?? "1", s => s.key,
                    (p, ss) => (p.p, p.pb, ph: ss.DefaultIfEmpty(new hgbgroup { luaspart = p.p.luasDibayar, count = 1, luashgb = p.p.luasDibayar }).First()))
                                            .ToArray();
                var res_gir = combo.Select(x => (x.p, x.pb, x.ph, lr: x.p.luasDibayar))
                //x.pb.luaspbt * x.p.luasDibayar / x.pb.luaspart))
                .Select(x => new
                {
                    x.p.key,
                    x.p.project,
                    x.p.desa,
                    x.p.PT,
                    //x.p.luasDibayar,
                    //nonibo = x.p.dsteps.TryGetValue("nonibo", out int ononibo) ? ononibo * x.p.luasDibayar : 0d,
                    //nibo = x.p.dsteps.TryGetValue("nibo", out int onibo) ? onibo * x.p.luasDibayar : 0d,
                    luasDibayar = x.lr,
                    nonibo = x.p.dsteps.TryGetValue("nonibo", out int ononibo) ? ononibo * x.lr : 0d,
                    nibo = x.p.dsteps.TryGetValue("nibo", out int onibo) ? onibo * x.lr : 0d,
                    nibp = x.p.dsteps.TryGetValue("nibp", out int onibp) ? onibp * x.lr * x.pb.ratio : 0d,
                    skkt = x.p.dsteps.TryGetValue("skkt", out int oskkt) ? oskkt * x.lr * x.pb.ratio : 0d,
                    skkw = x.p.dsteps.TryGetValue("skkw", out int oskkw) ? oskkw * x.lr * x.pb.ratio : 0d,
                    ctbuku = x.p.dsteps.TryGetValue("ctbuku", out int octbuku) ? octbuku * x.lr * x.pb.ratio : 0d,
                    buku = x.p.dsteps.TryGetValue("buku", out int obuku) ? obuku * x.lr * x.ph.ratio : 0d
                });

                var res_all = res_gir
                        .GroupBy(p => (p.project, p.desa, p.PT))
                        .Select(g => new
                        {
                            g.Key.project,
                            g.Key.desa,
                            g.Key.PT,
                            dibayar = g.Sum(p => p.luasDibayar),
                            nonib = g.Sum(p => p.nonibo),
                            nibor = g.Sum(p => p.nibo),
                            nibpt = Math.Round(g.Sum(p => p.nibp)),
                            kanta = Math.Round(g.Sum(p => p.skkt)),
                            kanwil = Math.Round(g.Sum(p => p.skkw)),
                            buku = Math.Round(g.Sum(p => p.ctbuku)),
                            hgbpt = Math.Round(g.Sum(p => p.buku))
                        }).ToArray();

                return Ok(res_all);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpGet("progress/hibah")]
        public IActionResult GetProgressHibah()
        {
            var steporders = new[] {
                ( id:0, step:"nonibo"),
                ( id:1, step:"nibo"),
                ( id:2, step:"notifshm"),
                ( id:3, step:"shm"),
                ( id:4, step:"orturun"),
                ( id:5, step:"turun"),
                ( id:6, step:"prosajb"),
                ( id:7, step:"orbalik"),
                ( id:8, step:"balik")
            };
            try
            {
                List<pivot> pivot_hib = new List<pivot>();
                //var pbts = new List<pbtgroup>();
                var hgbs = new List<hgbgroup>();

                var task1 = Task.Run(() =>
                {
                    pivot_hib = context.GetCollections(new pivot(), "material_pivot_hibah", "{}", "{_id:0}").ToList().Select(s => s.ToDict()).ToList();
                    pivot_hib.ForEach(p =>
                    {
                        int nibo = 0;
                        p.dsteps.TryGetValue("nibo", out nibo);
                        p.dsteps.Add("nonibo", 1 - nibo);

                        steporders.OrderByDescending(so => so.id).ToList().ForEach(so =>
                        {
                            var step = so.step;
                            if (p.dsteps.TryGetValue(step, out int val) && val == 1)
                                for (int x = 0; x < so.id; x++)
                                {
                                    var rstep = steporders[x].step;
                                    p.dsteps[rstep] = 0;
                                }
                        });
                    });
                });

                //var task2 = Task.Run(() => pbts = context.GetCollections(new pbtgroup(), "material_group_pbt", "{}", "{_id:0}").ToList());
                var task2 = Task.Run(() => hgbs = context.GetCollections(new hgbgroup(), "material_group_hgb", "{}", "{_id:0}").ToList());

                Task.WaitAll(task1, task2);//, task3);

                //var combo = pivot_gir.GroupJoin(pbts, p => p.key, s => s.key,
                //	(p, ss) => (p, pb: ss.DefaultIfEmpty(new pbtgroup { luaspart = p.luasDibayar, count = 1, luaspbt = p.luasDibayar }).First())).ToArray();
                //var res_gir = combo.Select(x => new
                //{
                //	x.p.project,
                //	x.p.desa,
                //	x.p.PT,
                //	x.p.luasDibayar,
                //	nonibo = x.p.dsteps.TryGetValue("nonibo", out int ononibo) ? ononibo * x.p.luasDibayar : 0d,
                //	nibo = x.p.dsteps.TryGetValue("nibo", out int onibo) ? onibo * x.p.luasDibayar : 0d,
                //	nibp = x.p.dsteps.TryGetValue("nibp", out int onibp) ? onibp * x.pb.luaspart : 0d,
                //	skkt = x.p.dsteps.TryGetValue("skkt", out int oskkt) ? oskkt * x.pb.luaspart : 0d,
                //	skkw = x.p.dsteps.TryGetValue("skkw", out int oskkw) ? oskkw * x.pb.luaspart : 0d,
                //	ctbuku = x.p.dsteps.TryGetValue("ctbuku", out int octbuku) ? octbuku * x.pb.luaspart : 0d,
                //	buku = x.p.dsteps.TryGetValue("buku", out int obuku) ? obuku * x.pb.luaspart : 0d
                //});

                var combo = pivot_hib
                    //.GroupJoin(pbts, p => p.nopbt ?? "1", s => s.key,
                    //(p, ss) => (p, pb: ss.DefaultIfEmpty(new pbtgroup { luaspart = p.luasDibayar, count = 1, luaspbt = p.luasDibayar }).First()))
                    .GroupJoin(hgbs, p => p.nohgb ?? "1", s => s.key,
                    (p, ss) => (p, ph: ss.DefaultIfEmpty(new hgbgroup { luaspart = p.luasDibayar, count = 1, luashgb = p.luasDibayar }).First()))
                                            .ToArray();
                var res_hib = combo.Select(x => (x.p, x.ph, lr: x.p.luasDibayar))
                .Select(x => new
                {
                    x.p.key,
                    x.p.project,
                    x.p.desa,
                    x.p.PT,
                    x.p.luasDibayar,
                    luasSurat = x.p.luasSurat ?? x.lr,
                    nonibo = x.p.dsteps.TryGetValue("nonibo", out int ononibo) ? ononibo * x.lr : 0d,
                    nibo = x.p.dsteps.TryGetValue("nibo", out int onibo) ? onibo * x.lr : 0d,
                    notifshm = x.p.dsteps.TryGetValue("notifshm", out int onotifshm) ? onotifshm * x.lr : 0d,
                    shm = x.p.dsteps.TryGetValue("shm", out int oshm) ? oshm * x.lr : 0d,
                    orturun = x.p.dsteps.TryGetValue("orturun", out int oorturun) ? oorturun * x.lr : 0d,
                    turun = x.p.dsteps.TryGetValue("turun", out int orturun) ? orturun * x.lr : 0d,
                    prosajb = x.p.dsteps.TryGetValue("prosajb", out int oprosajb) ? oprosajb * x.lr : 0d,
                    orbalik = x.p.dsteps.TryGetValue("orbalik", out int oorbalik) ? oorbalik * x.lr : 0d,
                    balik = x.p.dsteps.TryGetValue("balik", out int obalik) ? obalik * x.lr * x.ph.ratio : 0d,
                });

                var res_all = res_hib
                        .GroupBy(p => (p.project, p.desa, p.PT))
                        .Select(g => new
                        {
                            g.Key.project,
                            g.Key.desa,
                            g.Key.PT,
                            dibayar = g.Sum(p => p.luasDibayar),
                            surat = g.Sum(p => p.luasSurat),
                            nonib = g.Sum(p => p.nonibo),
                            nibor = g.Sum(p => p.nibo),
                            notif = g.Sum(p => p.notifshm),
                            shm = g.Sum(p => p.shm),
                            turun = g.Sum(p => p.orturun),
                            hgb = g.Sum(p => p.turun),
                            ajb = g.Sum(p => p.prosajb),
                            balik = g.Sum(p => p.orbalik),
                            hgbpt = g.Sum(p => p.balik)
                        }).ToArray();

                return Ok(res_all);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [NeedToken("DATA_VIEW,DATA_FULL,DATA_REVIEW,MAP_VIEW,MAP_FULL,MAP_REVIEW,PASCA_VIEW,PASCA_FULL,PASCA_REVIEW,PRA_VIEW,VIEW_MAP,REP_VIEW")]
        [EnableCors(nameof(landrope))]
        [HttpGet("persil")]
        public IActionResult GetReportAllPersil([FromQuery] string token, bool report, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var ureg = contextex.FindUserReg(token);
                var coll = contextex.GetDocuments(new PersilCoreRpt(), "material_persil_core", "{$project: {_id:0}}" );

                var persilByProject = contextex.GetDocuments(new { keyPersil = "" }, "persils_v2",
                    "{$match: {'basic.current' : {$ne : null}}}",
                    "{$lookup : {from : 'maps', localField: 'basic.current.keyProject', foreignField: 'key', as: 'project'}}",
                    "{$match : {'project': {$ne : []}}}",
                   @"{$project : {
                                    _id: 0,
                                    keyPersil: '$key'
                                }}"
                    ).Select(p => p.keyPersil).ToList();

                coll = coll.Where(c => persilByProject.Contains(c.key)).ToList();

                var persilkeys = string.Join(',', coll.Select(c => $"'{c.key}'"));

                var entries = contextex.GetDocuments(new EntriesPersil(), "persils_v2", "{$match: {key: {$in : [" + persilkeys + "]}}}",
                    "{$project : {_id : 0, keyPersil:'$key', entries: '$basic.entries'}}"
                    ).ToList();

                List<user> listUser = contextex.GetDocuments(new user(), "securities",
                      "{$match: {$and: [{'_t': 'user'}, {'invalid' : {$ne: true}}]}}").ToList();

                coll = coll.GroupJoin(entries, c => c.key, e => e.keyPersil, (c, e) => c.SetKeyCreator(e.FirstOrDefault().entries.LastOrDefault(en => en.approved == true)?.keyCreator,
                    e.FirstOrDefault().entries.LastOrDefault(en => en.approved == true)?.keyPreReviewer,
                    e.FirstOrDefault().entries.LastOrDefault(en => en.approved == true)?.keyReviewer,
                    listUser
                    )).ToList();


                var bayar = contextex.GetCollections(new byr(), "bayars", "{}", "{_id:0, nomorTahap:1, bidangs:1}").ToList()
                    .SelectMany(x => x.bidangs, (y, z) => new { noTahap = y.nomorTahap, keyPersil = z.keyPersil }).ToList();

                var bundles = contextex.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
                            "{$match: {'_t' : 'mainBundle'}}",
                            "{$unwind: '$doclist'}",
                            "{$match : {$and : [{'doclist.keyDocType':'JDOK032'}, {'doclist.entries' : {$ne : []}}]}}",
                            @"{$project : {
                                                              _id: 0
                                                            , key: 1
                                                            , doclist: 1
                    }}").ToList();
                
                var type = MetadataKey.Nomor_PBT.ToString("g");

                var cleanbundle = bundles.Select(x => new { key = x.key, entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value })
                                .Select(x => new { key = x.key, dyn = (x.entries == null) ? null : x.entries.props.TryGetValue(type, out Dynamic val) ? val : null })
                                .Select(x => new { key = x.key, nomorPBT = Convert.ToString(x.dyn?.Value ?? 0) }).ToList();
                
                coll = coll.GroupJoin(bayar, c => c.key, b => b.keyPersil, (c, b) => c.SetTahapNoNIB(b.FirstOrDefault(x => x.keyPersil == c.key)?.noTahap, GetNomorPBT2(cleanbundle.Select(x => (x.key, x.nomorPBT)).ToList(), c.key))).AsParallel().ToList();

                if (report)
                {
                    var sb = MakeCsv(coll.Select(c => c.ToCSV()).ToArray());
                    return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
                }
                else
                    return Ok(coll.GridFeed(gs));
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

        static StringBuilder MakeCsv<T>(T[] reportData)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
            var header = string.Join(",", props.ToList().Select(x => x.Name));
            lines.Add(header);

            var valueLines = reportData.Select(row => string.Join(",", header.Split(',')
                                       .Select(a => CsvFormatCorrection(row.GetType().GetProperty(a).GetValue(row, null)))
                                        ));
            lines.AddRange(valueLines);

            foreach (string item in lines)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        private Persil GetPersil(string key)
        {
            return contextex.persils.FirstOrDefault(p => p.key == key);
        }

        private string GetNomorPBT(string key)
        {
            var bundle = contextplus.mainBundles.FirstOrDefault(b => b.key == key);

            if (bundle == null)
                return string.Empty;

            var doclist = bundle.doclist.FirstOrDefault(b => b.keyDocType == "JDOK032");
            if (doclist == null)
                return string.Empty;

            var entry = doclist.entries.LastOrDefault();
            if (entry == null)
                return string.Empty;

            var part = entry.Item.FirstOrDefault().Value;
            if (part == null)
                return string.Empty;

            var typename = MetadataKey.Nomor_PBT.ToString("g");
            var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
            var castvalue = dyn?.Value;

            string result = string.Empty;
            if (castvalue != null)
                result = Convert.ToString(castvalue);

            return result;
        }


        private string GetNomorPBT2(List<(string key, string nomorPBT)> pbtDocs, string keyPersil)
        {
            string nomorPbt = "";
            nomorPbt = pbtDocs.Where(x => x.key == keyPersil) == null ? "" : pbtDocs.Where(x => x.key == keyPersil).FirstOrDefault().nomorPBT;

            return nomorPbt;
        }

        private static string CsvFormatCorrection(object objVal)
        {
            string value = objVal != null ? objVal.ToString() : "";
            string correctedFormat = "";
            correctedFormat = value.Contains(",") ? "\"" + value + "\"" : value;
            return correctedFormat;
        }

        private class EntriesPersil
        {
            public string keyPersil { get; set; }
            public List<ValidatableEntry<PersilBasic>> entries { get; set; } = new List<ValidatableEntry<PersilBasic>>();
        }
    }
}

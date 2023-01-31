using landrope.mod4;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using landrope.api3.Models;
using APIGrid;
using landrope.hosts;
using Tracer;
using landrope.common;
using MongoDB.Driver;
using GraphConsumer;
using landrope.consumers;
using mongospace;
using GenWorkflow;
using flow.common;
using auth.mod;
using FileRepos;
using landrope.mod3;
using Microsoft.AspNetCore.Http;
using System.IO;
using ExcelDataReader;
using System.Data;
using HttpAccessor;
using core.helpers;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace landrope.api3.Controllers
{
    [Route("api/proses")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class ProsesController : ControllerBase
    {
        IServiceProvider services;
        LandropePayContext context = Contextual.GetContextPay();
        LandropePlusContext contextplus = Contextual.GetContextPlus();
        FileGrid fgrid;
        graph.mod.GraphContext contextGraph;
        GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        public ProsesController(IServiceProvider services)
        {
            this.services = services;
            fgrid = services.GetService<FileGrid>();
            context = services.GetService<LandropePayContext>();
        }

        [HttpGet("list")]
        public IActionResult ListProses([FromQuery] string token, string find, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var jenisproses = priv.Contains("PAY_PROCESS_FULL") ? "'sertifikasi'" : priv.Contains("PAY_TAX_FULL") ? "'pajak'" : "";

                var proses = context.GetDocuments(new Proses(), "proses",
                    $@"<$match: <invalid:<$ne:true>,_t:{jenisproses}>>".Replace("<", "{").Replace(">", "}"),
                    @"{$lookup:{from:'securities', localField:'keyCreator',foreignField:'key',as:'user'}}",
                    @"{$lookup:{from:'prosestype', localField:'type',foreignField:'key',as:'tipe'}}",
                    $@"<$lookup:<from:'prosestype',let:<key: '$subType'>,pipeline:[<$unwind: '$subType'>,<$match: <$expr: <$eq:['$subType.key','$$key']>>>,
                    <$project: <key:'$subType.key', desc: '$subType.desc'>>], as:'subtype'>>".Replace("<", "{").Replace(">", "}"),
                    @"{$project:{ 
                                    key:'$key', 
                                    instkey:'$instkey',
                                    proses:'$_t',
                                    type:{$arrayElemAt:['$tipe.desc',-1]},
                                    nomorRFP:'$rfp.nomor',
                                    keyType:'$type',
                                    keyProject:'$keyProject', 
                                    keyDesa:'$keyDesa', 
                                    keyPTSK:'$keyPTSK',
                                    keyAktor:'$keyAktor',
                                    expiredDate : '$ExpiredDate',
                                    tanggaldiBuat:'$rfp.tglBuat', 
                                    keterangan:'$rfp.keterangan',
                                    creator:{$arrayElemAt:['$user.FullName',-1]},
                                    isAttachment:'$isAttachment',
                                    reasons:'$rfp.reasons',
                                    _id:0}}").Select(x => x.setTotalNominal(context, x.proses)).ToArray().AsParallel();

                //When using searching
                if (!string.IsNullOrEmpty(find))
                {
                    var bidangs = context.GetDocuments(new { key = "" }, "proses",
                        @"{$unwind:{ path: '$details'}}",
                        @"{$lookup: {from: 'persils_v2',localField: 'details.keyPersil',foreignField: 'key',as: 'persil'}}",
                        $@"<$match: <'persil.IdBidang' : /{find}/i>>".Replace("<", "{").Replace(">", "}"),
                        @"{$project: {
                                        key : '$key',
                                        keyPersil: '$details.keyPersil',
                                        IdBidang: {$arrayElemAt:['$persil.IdBidang',0]},
                                        _id: 0
                                     }}").ToArray();

                    proses = proses.Where(x => bidangs.Select(x => x.key).Contains(x.key));
                }

                var statekeys = string.Join(',', proses.Select(k => $"{k.instkey}"));

                var instaces = ghost.GetMany(statekeys, "").GetAwaiter().GetResult().Cast<GraphMainInstance>().ToArray().AsParallel();

                var lastStates = instaces.Select(x => new { key = x.key, state = x.lastState?.state, status = x.lastState?.state.AsStatus() }).ToArray();

                var locations = context.GetVillages().ToArray();

                var companies = context.ptsk.Query(x => x.invalid != true).ToArray()
                                            .Where(p => p.status == StatusPT.pembeli)
                                            .Select(p => new LandropePayContext.entitas { key = p.key, identity = p.identifier }).ToArray();

                var aktors = context.notarists.Query(x => x.invalid != true).ToArray()
                                            .Select(p => new LandropePayContext.entitas { key = p.key, identity = p.identifier }).ToArray();

                var view = proses.Select(x => x.toView(context, locations, companies, aktors, lastStates.Select(x => (x.key, x.state.GetValueOrDefault(), x.status)).ToArray())).ToArray().AsParallel();

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<ProsesView>), typeof(ProsesView), gs);
                var data = xlst.result.Cast<ProsesView>().ToList();

                return Ok(data.GridFeed(gs));
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

        [HttpGet("list-app")]
        public IActionResult ListProsesApp([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);
                var Gs1 = ghost.List(user, 1).GetAwaiter().GetResult() ?? new GraphTree[0];
                var Gs = Gs1.Where(x => x.subs.Any()).Select(x => (x.main,
                                                                    inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes,
                                                                    insubs: (from xs in x.subs
                                                                             from s in xs.nodes
                                                                             where xs.instance.key != x.main.key
                                                                             select xs))).ToArray().AsParallel();

                var instkeys = Gs.Select(g => g.main.key).ToArray();
                var keys = string.Join(',', instkeys.Select(k => $"'{k}'"));

                var proses = context.GetDocuments(new Proses(), "proses",
                    $@"<$match: <instkey:<$in:[{keys}]>, invalid:<$ne:true>>>".Replace("<", "{").Replace(">", "}"),
                    @"{$lookup:{from:'securities', localField:'keyCreator',foreignField:'key',as:'user'}}",
                    @"{$lookup:{from:'prosestype', localField:'type',foreignField:'key',as:'tipe'}}",
                    $@"<$lookup:<from:'prosestype',let:<key: '$subType'>,pipeline:[<$unwind: '$subType'>,<$match: <$expr: <$eq:['$subType.key','$$key']>>>,
                    <$project: <key:'$subType.key', desc: '$subType.desc'>>], as:'subtype'>>".Replace("<", "{").Replace(">", "}"),
                    @"{$project:{ 
                                    key:'$key', 
                                    instkey:'$instkey',
                                    proses:'$_t',
                                    type:{$arrayElemAt:['$tipe.desc',-1]},
                                    nomorRFP:'$rfp.nomor',
                                    keyType:'$type',
                                    keyProject:'$keyProject', 
                                    keyDesa:'$keyDesa', 
                                    keyPTSK:'$keyPTSK',
                                    keyAktor:'$keyAktor',
                                    tanggaldiBuat:'$rfp.tglBuat', 
                                    keterangan:'$rfp.keterangan',
                                    creator:{$arrayElemAt:['$user.FullName',-1]},
                                    isAttachment:'$isAttachment',
                                    reasons:'$rfp.reasons',
                                    _id:0}}").Select(x => x.setTotalNominal(context, x.proses)).ToArray().AsParallel();

                var locations = context.GetVillages().ToArray();
                var companies = context.ptsk.Query(x => x.invalid != true).ToArray()
                                            .Where(p => p.status == StatusPT.pembeli)
                                            .Select(p => new LandropePayContext.entitas { key = p.key, identity = p.identifier }).ToArray();

                var viewExt = proses.Select(x => x.toViewExt(context, locations, companies)).ToArray().AsParallel();

                var nxdata = viewExt.Join(Gs, a => a.instkey, g => g.main?.key,
                                       (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault())).ToArray().AsParallel();

                var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.Distinct() ?? new GraphRoute[0])).ToArray().AsParallel();
                var ndatax = ndata.Select(x => (x, y: ProsesViewExtGraph.Upgrade(x.a))).ToArray().AsParallel();

                var viewExtGraph = ndatax.Where(X => X.y != null).Select(X => X.y?
                 .SetRoutes(X.x.routes?.Select(x => (x.key, x._verb.Title(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                 .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)
                 .SetStatus(X.x.i?.lastState?.state.AsStatus(), X.x.i?.lastState?.time)
                 .SetCreator(user.FullName == X.x.a.creator)
                 .SetMilestones(X.x.i?.states.LastOrDefault(s => s.state == ToDoState.created_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.issued_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.verification1_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.verification2_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.verification3_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.verifLegal_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.reviewApproved_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.accountingApproved_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.cashierApproved_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.finalApproved_)?.time)
                ).ToArray();

                var xlst = ExpressionFilter.Evaluate(viewExtGraph, typeof(List<ProsesViewExtGraph>), typeof(ProsesViewExtGraph), gs);
                var data = xlst.result.Cast<ProsesViewExtGraph>().ToArray();

                return Ok(data.GridFeed(gs));
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

        [HttpGet("list-dtl")]
        public IActionResult ListProsesDetail([FromQuery] string token, string keyProses, string proses, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);

                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var keys = sertifikasi.details.Select(x => x.keyPersil);
                    var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));

                    var persils = context.GetDocuments(new { key = "", IdBidang = "", luasNIBBidang = 0, basic = new landrope.mod2.PersilBasic() }, "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>,key:<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
                   "{$project:{_id:0,key:1,IdBidang: 1,luasNIBBidang:'$PajakSertifikasi.LuasNIBBidang',basic: '$basic.current'}}").ToArray().AsParallel();

                    var details = sertifikasi.details.Select(x => x.ToView(keyProses)).ToArray();
                    var xlst = ExpressionFilter.Evaluate(details, typeof(List<SertifikasiDtlView>), typeof(SertifikasiDtlView), gs);
                    var datax = xlst.result.Cast<SertifikasiDtlView>().ToArray();

                    var bundles = GetSmallBundle(prokeys);

                    datax = datax.Join(persils, d => d.keyPersil, p => p.key, (d, p) => d.setBidang(p.IdBidang, p.basic.pemilik, p.basic.luasSurat,
                                                                                                    p.basic.luasInternal, p.basic.luasDibayar, p.basic.surat?.nomor,
                                                                                                    p.basic.satuan, p.basic.satuanAkte, p.luasNIBBidang,
                                                                                                    bundles.Select(x => (x.keyPersil, x.Luas, x.SK)).ToList())).ToArray();

                    var data = datax.Join(bundles, d => d.keyPersil, b => b.keyPersil, (d, b) => d.setBundle(b.Luas, b.SK)).ToArray();

                    return Ok(data.GridFeed(gs));
                }
                else
                {
                    var pajak = context.pajaks.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var keys = pajak.details.Select(x => x.keyPersil);
                    var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));

                    var persils = context.GetDocuments(new { key = "", IdBidang = "", luasNIBBidang = 0, basic = new landrope.mod2.PersilBasic() }, "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>,key:<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
                   "{$project:{_id:0,key:1,IdBidang: 1,luasNIBBidang:'$PajakSertifikasi.LuasNIBBidang',basic: '$basic.current'}}").ToArray().AsParallel();

                    var details = pajak.details.Select(x => x.ToView(keyProses)).ToList();
                    var xlst = ExpressionFilter.Evaluate(details, typeof(List<PajakDtlView>), typeof(PajakDtlView), gs);
                    var datax = xlst.result.Cast<PajakDtlView>().ToArray();

                    var bundles = GetSmallBundle(prokeys);

                    datax = datax.Join(persils, d => d.keyPersil, p => p.key, (d, p) => d.setBidang(p.IdBidang, p.basic.pemilik, p.basic.luasSurat,
                                                                                                    p.basic.luasInternal, p.basic.luasDibayar, p.basic.surat?.nomor,
                                                                                                    p.basic.satuan, p.basic.satuanAkte, p.luasNIBBidang,
                                                                                                    bundles.Select(x => (x.keyPersil, x.Luas, x.SK)).ToList())).ToArray();

                    var data = datax.Join(bundles, d => d.keyPersil, b => b.keyPersil, (d, b) => d.setBundle(b.Luas, b.SK)).ToArray();

                    return Ok(data.GridFeed(gs));
                }

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

        [HttpGet("list-dtl-app")]
        public IActionResult ListProsesDetailApp([FromQuery] string token, string keyProses, string proses, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);

                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var keys = sertifikasi.details.Select(x => x.keyPersil);
                    var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));

                    var persils = context.GetDocuments(new { key = "", IdBidang = "", luasNIBBidang = 0, basic = new landrope.mod2.PersilBasic() }, "persils_v2",
                   $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>,key:<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
                  "{$project:{_id:0,key:1,IdBidang: 1,luasNIBBidang:'$PajakSertifikasi.LuasNIBBidang',basic: '$basic.current'}}").ToArray().AsParallel();

                    var details = sertifikasi.details.Select(x => x.ToView(keyProses)).ToArray().AsParallel();
                    var xlst = ExpressionFilter.Evaluate(details, typeof(List<SertifikasiDtlView>), typeof(SertifikasiDtlView), gs);
                    var datax = xlst.result.Cast<SertifikasiDtlView>().ToArray();

                    var bundles = GetSmallBundle(prokeys);

                    var data = datax.Join(persils, d => d.keyPersil, p => p.key, (d, p) => d.setBidang(p.IdBidang, p.basic.pemilik, p.basic.luasSurat,
                                                                                                   p.basic.luasInternal, p.basic.luasDibayar, p.basic.surat?.nomor,
                                                                                                   p.basic.satuan, p.basic.satuanAkte, p.luasNIBBidang,
                                                                                                   bundles.Select(x => (x.keyPersil, x.Luas, x.SK)).ToList())).ToArray();

                    //var data = datax.Join(bundles, d => d.keyPersil, b => b.keyPersil, (d, b) => d.setBundle(b.Luas, b.SK)).ToArray();

                    return Ok(data.GridFeed(gs));
                }
                else
                {
                    var pajak = context.pajaks.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var keys = pajak.details.Select(x => x.keyPersil);
                    var prokeys = string.Join(',', keys.Select(k => $"'{k}'"));

                    var persils = context.GetDocuments(new { key = "", IdBidang = "", luasNIBBidang = 0, basic = new landrope.mod2.PersilBasic() }, "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>,key:<$in:[{prokeys}]>>>".Replace("<", "{").Replace(">", "}"),
                   "{$project:{_id:0,key:1,IdBidang: 1,luasNIBBidang:'$PajakSertifikasi.LuasNIBBidang',basic: '$basic.current'}}").ToArray().AsParallel();

                    var details = pajak.details.Select(d => d.ToView(keyProses)).ToArray().AsParallel();

                    var ndata = details.Select(x => (x, y: PajakDtlViewExt.Upgrade(x))).ToArray().AsParallel();
                    var nxdata = ndata.Select(x => x.y).ToArray();

                    var xlst = ExpressionFilter.Evaluate(nxdata, typeof(List<PajakDtlView>), typeof(PajakDtlView), gs);
                    var datax = xlst.result.Cast<PajakDtlView>().ToArray();

                    var bundles = GetSmallBundle(prokeys);

                    var data = datax.Join(persils, d => d.keyPersil, p => p.key, (d, p) => d.setBidang(p.IdBidang, p.basic.pemilik, p.basic.luasSurat,
                                                                                                    p.basic.luasInternal, p.basic.luasDibayar, p.basic.surat?.nomor,
                                                                                                    p.basic.satuan, p.basic.satuanAkte, p.luasNIBBidang,
                                                                                                    bundles.Select(x => (x.keyPersil, x.Luas, x.SK)).ToList())).ToArray();

                    //var data = datax.Join(bundles, d => d.keyPersil, b => b.keyPersil, (d, b) => d.setBundle(b.Luas, b.SK)).ToArray();



                    return Ok(data.GridFeed(gs));
                }
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }

            ToDoState GetActualState(GraphMainInstance inst, string routekey)
            {
                if (routekey == null)
                    return inst.lastState?.state ?? ToDoState.unknown_;

                var sub = inst.children.Select(c => c.Value).FirstOrDefault(c => c.Core.nodes.OfType<GraphNode>().Any(n => n.routes.Any(r => r.key == routekey)));
                return sub?.lastState?.state ?? inst.lastState?.state ?? ToDoState.unknown_;
            }
        }


        [HttpGet("list-dtl-subtype")]
        public IActionResult ListProsesDetailSubType([FromQuery] string token, string keyProses, string proses, string keyProsesDtl, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);

                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var docs = context.GetCollections(new StaticCollSertifikasi(), "static_collections", "{_t:'sertifikasi'}", "{_id:0, Docs:1}").FirstOrDefault();

                    var subtypes = context.GetCollections(new ProsesType(), "prosestype",
                        $"<invalid:<$ne:true>, key:'{sertifikasi.type}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                        .SelectMany(x => x.subType)
                        .Select(p => new LandropePayContext.entitas { key = p.key, identity = p.desc }).ToArray().AsParallel();

                    var details = sertifikasi.details.Where(x => x.key == keyProsesDtl).FirstOrDefault();
                    var detailSubTypes = details.subTypes.Select(x => x.ToView(subtypes.ToArray(), docs.Docs)).ToArray();


                    var xlst = ExpressionFilter.Evaluate(detailSubTypes, typeof(List<SertifikasiDtlSubTypeView>), typeof(SertifikasiDtlSubTypeView), gs);
                    var data = xlst.result.Cast<SertifikasiDtlSubTypeView>().ToArray();

                    return Ok(data.GridFeed(gs));
                }
                else
                {
                    var pajak = context.pajaks.Query(x => x.invalid != true && x.key == keyProses).FirstOrDefault();

                    var subtypes = context.GetCollections(new ProsesType(), "prosestype",
                        $"<invalid:<$ne:true>, key:'{pajak.type}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                        .SelectMany(x => x.subType)
                        .Select(p => new LandropePayContext.entitas { key = p.key, identity = p.desc }).ToArray().AsParallel();

                    var details = pajak.details.Where(x => x.key == keyProsesDtl).FirstOrDefault();
                    var detailSubTypes = details.subTypes.Select(x => x.ToView(subtypes.ToArray())).ToArray();

                    var xlst = ExpressionFilter.Evaluate(detailSubTypes, typeof(List<PajakDtlSubTypeView>), typeof(PajakDtlSubTypeView), gs);
                    var data = xlst.result.Cast<PajakDtlSubTypeView>().ToArray();

                    return Ok(data.GridFeed(gs));
                }

                return Ok();
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

        [HttpGet("get")]
        public IActionResult GetHeaderToEdit([FromQuery] string token, string proses, string keyProses)
        {
            try
            {
                var user = context.FindUser(token);
                var data = new ProsesCore();

                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.FirstOrDefault(x => x.key == keyProses);
                    var gMain = ghost.Get(sertifikasi.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    var isDetail = sertifikasi.details.Count() > 0 ? true : false;

                    if (gMain.lastState?.state != ToDoState.created_)
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa diedit");

                    data = new ProsesCore
                    {
                        key = sertifikasi.key,
                        nomorRFP = sertifikasi.rfp.nomor,
                        keyProject = sertifikasi.keyProject,
                        keyDesa = sertifikasi.keyDesa,
                        keyPTSK = sertifikasi.keyPTSK,
                        keyAktor = sertifikasi.keyAktor,
                        type = sertifikasi.type,
                        proses = "sertifikasi",
                        keterangan = sertifikasi.rfp.keterangan,
                        isDetail = isDetail
                    };

                }
                else
                {
                    var pajak = context.pajaks.FirstOrDefault(x => x.key == keyProses);
                    var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    if (gMain.lastState?.state != ToDoState.created_)
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa diedit");

                    var isDetail = pajak.details.Count() > 0 ? true : false;

                    data = new ProsesCore
                    {
                        key = pajak.key,
                        nomorRFP = pajak.rfp.nomor,
                        keyProject = pajak.keyProject,
                        keyDesa = pajak.keyDesa,
                        keyPTSK = pajak.keyPTSK,
                        type = pajak.type,
                        proses = "pajak",
                        keterangan = pajak.rfp.keterangan,
                        isDetail = isDetail,
                        ExpiredDate = pajak.ExpiredDate
                    };
                }

                return new JsonResult(data);

            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("add")]
        public IActionResult AddHeader([FromQuery] string token, [FromBody] ProsesCore proses)
        {
            try
            {
                var user = context.FindUser(token);
                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var creatorSertifikasi = priv.Contains("PAY_PROCESS_FULL");
                var creatorPajak = priv.Contains("PAY_TAX_FULL");

                if (creatorSertifikasi && proses.proses == "sertifikasi")
                {
                    var noRFP = context.sertifikasis.FirstOrDefault(x => x.rfp.nomor == proses.nomorRFP);
                    if (noRFP != null)
                        return new UnprocessableEntityObjectResult($"Nomor RFP sudah ada");

                    var host = HostServicesHelper.GetProsesHost(services);
                    var ent = new Sertifikasi(user, proses.proses, proses.type);

                    ent.FromCore(proses);
                    proses.key = ent.key;

                    host.AddSertifikasi(ent);
                }

                else if (creatorPajak && proses.proses == "pajak")
                {
                    var noRFP = context.sertifikasis.FirstOrDefault(x => x.rfp.nomor == proses.nomorRFP);
                    if (noRFP != null)
                        return new UnprocessableEntityObjectResult($"Nomor RFP sudah ada");

                    var host = HostServicesHelper.GetProsesHost(services);
                    var ent = new Pajak(user, proses.proses, proses.type);

                    ent.FromCore(proses);
                    proses.key = ent.key;

                    host.AddPajak(ent);
                }
                else
                {
                    return new UnprocessableEntityObjectResult($"Anda tidak memiliki hak untuk menambahkan data pembayaran {proses.proses}");
                }

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("edit")]
        public IActionResult EditHeader([FromQuery] string token, [FromBody] ProsesCore proses)
        {
            try
            {
                var user = context.FindUser(token);

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var creatorSertifikasi = priv.Contains("PAY_PROCESS_FULL");
                var creatorPajak = priv.Contains("PAY_TAX_FULL");

                if (proses.proses == "sertifikasi" && creatorSertifikasi)
                {
                    var old = context.sertifikasis.FirstOrDefault(x => x.key == proses.key);
                    var gMain = ghost.Get(old.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa diedit");

                    old.FromCore(proses);

                    context.sertifikasis.Update(old);
                    context.SaveChanges();
                }

                else if (proses.proses == "pajak" && creatorPajak)
                {
                    var old = context.pajaks.FirstOrDefault(x => x.key == proses.key);
                    var gMain = ghost.Get(old.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa diedit");

                    old.FromCore(proses);

                    context.pajaks.Update(old);
                    context.SaveChanges();

                }
                else
                {
                    return new UnprocessableEntityObjectResult($"Anda tidak memiliki hak untuk merubah data pembayaran {proses.proses}");
                }

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("add-dtl")]
        public IActionResult AddDetail([FromQuery] string token, string keyProses, string proses, [FromBody] string[] pkeys)
        {
            try
            {
                var user = context.FindUser(token);
                var host = HostServicesHelper.GetProsesHost(services);

                var result = proses switch
                {
                    "sertifikasi" => SertifikasiProses(),
                    _ => PajakProses(),
                };

                IActionResult SertifikasiProses()
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.key == keyProses && x.invalid != true).FirstOrDefault();

                    if (sertifikasi == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(sertifikasi.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, tidak bisa menambahkan bidang");

                    foreach (var k in pkeys)
                    {
                        if (sertifikasi.details != null)
                        {
                            if (sertifikasi.details.Any(d => d.keyPersil == k))
                                continue;
                        }

                        var sertifikatdtl = new SertifikasiDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };

                        sertifikasi.AddDetail(sertifikatdtl);
                    }

                    context.sertifikasis.Update(sertifikasi);
                    context.SaveChanges();
                    return Ok();
                }

                IActionResult PajakProses()
                {
                    var pajak = context.pajaks.Query(x => x.key == keyProses && x.invalid != true).FirstOrDefault();

                    if (pajak == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, tidak bisa menambahkan bidang");


                    foreach (var k in pkeys)
                    {
                        if (pajak.details != null)
                        {
                            if (pajak.details.Any(d => d.keyPersil == k))
                                continue;
                        }

                        var pajakdtl = new PajakDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };

                        pajak.AddDetail(pajakdtl);
                    }
                    context.pajaks.Update(pajak);
                    context.SaveChanges();
                    return Ok();
                }

                return result;
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("add-dtl-subtype")]
        public IActionResult AddDetailSubType([FromQuery] string token, string keyProses, string proses, [FromBody] ProsesDetailCore prosesDetail)
        {
            try
            {
                var user = context.FindUser(token);
                var host = HostServicesHelper.GetProsesHost(services);

                var prosesTypes = context.GetCollections(new ProsesType(), "prosestype", "{}", "{_id:0}").ToList()
                                   .SelectMany(x => x.subType, (y, z) => new { keyHead = y.key, keySub = z.key, nameSub = z.desc }).ToArray();

                var result = proses switch
                {
                    "sertifikasi" => SertifikasiProses(),
                    _ => PajakProses(),
                };

                IActionResult SertifikasiProses()
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.key == keyProses && x.invalid != true).FirstOrDefault();
                    if (sertifikasi == null)
                        return new UnprocessableEntityObjectResult("Request Pembayaran tidak ada");

                    if (string.IsNullOrEmpty(prosesDetail.sertifikasi.subType))
                        return new UnprocessableEntityObjectResult("Subtype tidak boleh kosong");

                    var gMain = ghost.Get(sertifikasi.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, tidak bisa menambahkan subtype");

                    var forLuasPBT = new List<(string key, double? luas)>();

                    foreach (var persil in prosesDetail.persil)
                    {
                        var detail = sertifikasi.details.Where(x => x.keyPersil == persil.keypersil)?.FirstOrDefault();

                        if (detail.subTypes.Any(x => x.subType == prosesDetail.sertifikasi.subType && x.subType != "PROSES05101"))
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Type yang dimaksud sudah ada di dalam detail bidang {id}");
                        }

                        if (!IsRepeat(sertifikasi.key, detail.key, sertifikasi.type, prosesDetail.sertifikasi.subType, persil.keypersil, proses))
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Bidang {id} sudah pernah melakukan pembayaran proses ini, silahkan upload IOM");
                        }

                        if (!sertifikasi.details.Any(d => d.keyPersil == persil.keypersil))
                        {
                            var sertifikatdtl = new SertifikasiDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.keypersil };
                            sertifikasi.AddDetail(sertifikatdtl);
                            context.sertifikasis.Update(sertifikasi);
                            context.SaveChanges();
                        }

                        if (detail == null)
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Bidang {id} tidak ada dalam request pembayaran");
                        }

                        var iomExists = IOMUploaded(keyProses, prosesDetail.sertifikasi.subType, detail.key, proses);
                        var subTypeDetail = new SertifikasiSubTypeDetail();

                        var subsDesc = prosesTypes.FirstOrDefault(x => x.keySub == prosesDetail.sertifikasi.subType);
                        var aktadesc = "akta";

                        if (subsDesc.nameSub.ToLower().Contains(aktadesc))
                        {
                            if (prosesDetail.sertifikasi.akta == null)
                                return new UnprocessableEntityObjectResult($"Data akta harus diisi");

                            var akta = prosesDetail.sertifikasi.akta;

                            if (string.IsNullOrEmpty(akta.nomorAkta) || akta.tanggalAkta == null || akta.totalHarga <= 0 || akta.biayaPerBuku <= 0)
                                return new UnprocessableEntityObjectResult($"Data akta tidak lengkap");

                            if (akta.totalHarga != prosesDetail.sertifikasi.nominal)
                                return new UnprocessableEntityObjectResult($"Nilai total akta tidak sama dengan nominal");
                        }

                        subTypeDetail.FromCore(prosesDetail.sertifikasi, iomExists);

                        detail.AddSubTypes(subTypeDetail);

                        if (persil.luasNIBBidang != null && persil.luasNIBBidang != 0) // luas pbt pt perbidang di input lewat menu pembayaran sertifikasi dan pajak
                            forLuasPBT.Add((persil.keypersil, persil.luasNIBBidang));
                    }

                    context.sertifikasis.Update(sertifikasi);
                    context.SaveChanges();

                    if (forLuasPBT.Count() > 0)
                        UpdateLuasPBTPT(forLuasPBT.ToArray(), user); // luas pbt pt perbidang di input lewat menu pembayaran sertifikasi dan pajak

                    return Ok();
                }

                IActionResult PajakProses()
                {
                    var pajak = context.pajaks.Query(x => x.key == keyProses && x.invalid != true).FirstOrDefault();

                    if (pajak == null)
                        return new UnprocessableEntityObjectResult("Request Pembayaran tidak ada");

                    if (string.IsNullOrEmpty(prosesDetail.pajak.subType))
                        return new UnprocessableEntityObjectResult("Subtype tidak boleh kosong");

                    var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, tidak bisa menambahkan subtype");

                    var forLuasPBT = new List<(string key, double? luas)>();

                    foreach (var persil in prosesDetail.persil)
                    {
                        var detail = pajak.details.Where(x => x.keyPersil == persil.keypersil)?.FirstOrDefault();

                        if (detail.subTypes.Any(x => x.subType == prosesDetail.pajak.subType))
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Type yang dimaksud sudah ada di dalam detail bidang {id}");
                        }

                        if (!IsRepeat(pajak.key, detail.key, pajak.type, prosesDetail.pajak.subType, persil.keypersil, proses))
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Bidang {id} sudah pernah melakukan pembayaran proses ini, silahkan upload IOM");
                        }

                        if (!pajak.details.Any(d => d.keyPersil == persil.keypersil))
                        {
                            var pajakdtl = new PajakDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.keypersil };

                            pajak.AddDetail(pajakdtl);
                            context.pajaks.Update(pajak);
                            context.SaveChanges();
                        }

                        if (detail == null)
                        {
                            var id = GetPersil(persil.keypersil).IdBidang;
                            return new UnprocessableEntityObjectResult($"Bidang {id} tidak ada dalam request pembayaran");
                        }

                        var iomExists = IOMUploaded(keyProses, prosesDetail.pajak.subType, detail.key, proses);
                        var subTypeDetail = new PajakSubTypeDetail();
                        subTypeDetail.FromCore(prosesDetail.pajak, iomExists);

                        var subsDesc = prosesTypes.FirstOrDefault(x => x.keySub == prosesDetail.pajak.subType);
                        var nunggak = "tunggakan";

                        if (subsDesc.nameSub.ToLower().Contains(nunggak))
                        {
                            if (prosesDetail.pajak.pinaltis.Count() <= 0)
                                return new UnprocessableEntityObjectResult($"Data tunggakan harus diisi");

                            var zero = prosesDetail.pajak.pinaltis.Any(x => x.total == 0);
                            if (zero)
                                return new UnprocessableEntityObjectResult($"Nilai total tunggakan tidak boleh 0");

                            var sumPinalti = prosesDetail.pajak.pinaltis.Select(x => x.total).Sum();

                            if (sumPinalti != prosesDetail.pajak.nominal)
                                return new UnprocessableEntityObjectResult($"Nilai total tunggakan tidak sama dengan nominal");
                        }

                        detail.AddSubTypes(subTypeDetail);

                        if (persil.luasNIBBidang != null && persil.luasNIBBidang != 0) // luas pbt pt perbidang di input lewat menu pembayaran sertifikasi dan pajak
                            forLuasPBT.Add((persil.keypersil, persil.luasNIBBidang));
                    }

                    context.pajaks.Update(pajak);
                    context.SaveChanges();

                    if (forLuasPBT.Count() > 0)
                        UpdateLuasPBTPT(forLuasPBT.ToArray(), user); // luas pbt pt perbidang di input lewat menu pembayaran sertifikasi dan pajak

                    return Ok();
                }

                return result;
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpGet("del-dtl")]
        public IActionResult DeleteDetail([FromQuery] string token, string keyProses, string keyDetail, string proses)
        {
            try
            {
                var user = context.FindUser(token);

                var result = proses switch
                {
                    "sertifikasi" => SertifikasiProses(),
                    _ => PajakProses(),
                };

                IActionResult SertifikasiProses()
                {
                    var sertifikasi = context.sertifikasis.FirstOrDefault(x => x.key == keyProses);

                    if (sertifikasi == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(sertifikasi.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa dihapus");

                    var details = sertifikasi.details.ToList();
                    var detail = sertifikasi.details.FirstOrDefault(x => x.key == keyDetail);
                    details.Remove(detail);
                    sertifikasi.details = details.ToArray();

                    context.sertifikasis.Update(sertifikasi);
                    context.SaveChanges();

                    return Ok();
                }

                IActionResult PajakProses()
                {
                    var pajak = context.pajaks.FirstOrDefault(x => x.key == keyProses);

                    if (pajak == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, bidang tidak bisa dihapus");

                    var details = pajak.details.ToList();
                    var detail = pajak.details.FirstOrDefault(x => x.key == keyDetail);
                    details.Remove(detail);
                    pajak.details = details.ToArray();

                    context.pajaks.Update(pajak);
                    context.SaveChanges();

                    return Ok();
                }

                return result;
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpGet("del-dtl-subtype")]
        public IActionResult DeleteDetailSubtype([FromQuery] string token, string keyProses, string keyDetail, string keySubType, string proses)
        {
            try
            {
                var user = context.FindUser(token);

                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.FirstOrDefault(x => x.key == keyProses);

                    if (sertifikasi == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(sertifikasi.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, subtype tidak bisa dihapus");

                    var details = sertifikasi.details.ToList();
                    var detail = sertifikasi.details.FirstOrDefault(x => x.key == keyDetail);

                    var subtypes = detail.subTypes.ToList();
                    var subtype = detail.subTypes.FirstOrDefault(x => x.subType == keySubType);

                    subtypes.Remove(subtype);
                    detail.subTypes = subtypes.ToArray();

                    context.sertifikasis.Update(sertifikasi);
                    context.SaveChanges();

                    return Ok();
                }
                else
                {
                    var pajak = context.pajaks.FirstOrDefault(x => x.key == keyProses);

                    if (pajak == null)
                        return new UnprocessableEntityObjectResult("RFP tidak ada");

                    var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                    int[] sta = { (int)ToDoState.created_, (int)ToDoState.rejected_ };
                    var state = (int)gMain.lastState?.state;

                    if (!sta.Contains(state))
                        return new UnprocessableEntityObjectResult($"RFP sudah berjalan, subtype tidak bisa dihapus");

                    var details = pajak.details.ToList();
                    var detail = pajak.details.FirstOrDefault(x => x.key == keyDetail);

                    var subtypes = detail.subTypes.ToList();
                    var subtype = detail.subTypes.FirstOrDefault(x => x.subType == keySubType);

                    subtypes.Remove(subtype);
                    detail.subTypes = subtypes.ToArray();

                    context.pajaks.Update(pajak);
                    context.SaveChanges();

                    return Ok();
                }
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpGet("persils/list")]
        public IActionResult GetListPersil([FromQuery] string token, string key, string keyProject, string keyDesa, string keyPTSK, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);

                var states = new object[] { 0, 1, null };
                var statekeys = string.Join(',', states.Select(k => $"{k}"));
                var sample = new { key = "", identity = "" };
                var desas = context.GetCollections(sample, "villages", "{invalid:{$ne:true}}",
                                                  "{_id:0,key:'$village.key', identity:'$village.identity'}").ToList().AsParallel();

                var projects = context.GetCollections(sample, "maps", "{}", "{_id:0,key:1,identity:1}").ToList().AsParallel();

                var fltr = $"<key:'{key}'>".Replace("<", "{").Replace(">", "}");

                var persilsExist = context.GetDocuments(new { key = "" }, "proses",
                    $@"<'$match':<key:'{key}'>>".Replace("<", "{").Replace(">", "}"),
                    "{$unwind:{path:'$details'}}",
                    "{'$project':{key : '$details.keyPersil', _id:0}}").Select(x => x.key).ToArray();

                var builder = Builders<mod2.Persil>.Filter;
                var filter = builder.Ne(e => e.invalid, true) & builder.Ne(e => e.basic.current, null) & builder.In(e => e.en_state, states);
                if (!string.IsNullOrEmpty(keyProject))
                    filter &= builder.Eq(e => e.basic.current.keyProject, keyProject);
                if (!string.IsNullOrEmpty(keyDesa))
                    filter &= builder.Eq(e => e.basic.current.keyDesa, keyDesa);
                if (!string.IsNullOrEmpty(keyPTSK))
                    filter &= builder.Eq(e => e.basic.current.keyPTSK, keyPTSK);


                var persils = context.GetCollections(new mod2.Persil(), "persils_v2", filter, "{_id:0}").ToList()
                                                    .Where(x => !persilsExist.Contains(x.key))
                                                    .Select(x => new PersilProses
                                                    {
                                                        key = x.key,
                                                        IdBidang = x.IdBidang,
                                                        en_state = (int?)x.en_state,
                                                        alasHak = x.basic.current?.surat?.nomor,
                                                        noPeta = x.basic.current?.noPeta,
                                                        keyProject = x.basic.current?.keyProject,
                                                        keyDesa = x.basic.current?.keyDesa,
                                                        keyPTSK = x.basic.current?.keyPTSK,
                                                        states = (x.en_state == StatusBidang.bebas ? "BEBAS" :
                                                         x.en_state == StatusBidang.belumbebas ? "BELUM BEBAS" :
                                                         x.en_state == StatusBidang.kampung ? "KAMPUNG" :
                                                         x.en_state == StatusBidang.batal ? "BATAL" :
                                                         x.en_state == StatusBidang.overlap ? "OVERLAP" :
                                                         x.en_state == StatusBidang.keluar ? "KELUAR" : "BEBAS"),
                                                        desa = desas.FirstOrDefault(d => d.key == x.basic.current.keyDesa)?.identity,
                                                        project = projects.FirstOrDefault(p => p.key == x.basic.current.keyProject)?.identity,
                                                        luasSurat = x.basic.current.luasSurat,
                                                        luasDiBayar = x.basic.current.luasDibayar,
                                                        luasInternal = x.basic.current.luasInternal,
                                                        satuan = x.basic.current.satuan,
                                                        satuanAkta = x.basic.current.satuanAkte,
                                                        group = x.basic.current.group,
                                                        pemilik = x.basic.current.pemilik
                                                    }).ToArray();

                var prokeys = string.Join(',', persils.Select(k => $"'{k.key}'"));

                var bundles = GetSmallBundle(prokeys);

                persils = persils.Join(bundles, p => p.key, b => b.keyPersil, (p, b) => p.setBundle(b.Luas, b.SK)).ToArray();

                var xlst = ExpressionFilter.Evaluate(persils, typeof(List<PersilProses>), typeof(PersilProses), gs);
                var data = xlst.result.Cast<PersilProses>().ToArray();

                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpGet("type/list")]
        public IActionResult GetListType([FromQuery] string token)
        {
            try
            {
                var user = context.FindUser(token);

                var types = context.GetCollections(new ProsesType(), "prosestype", "{}", "{_id:0}").ToList();

                return Ok(types.ToArray());
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpGet("subtype/list")]
        public IActionResult GetListType([FromQuery] string token, string type)
        {
            try
            {
                var user = context.FindUser(token);

                var subTypes = context.GetCollections(new ProsesType(), "prosestype", $"<key:'{type}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList().SelectMany(x => x.subType);

                return Ok(subTypes.ToArray());
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpPost("step")]
        [Consumes("application/json")]
        public IActionResult Step([FromQuery] string token, [FromBody] ProsesCommand cmd)
        {
            try
            {
                var user = context.FindUser(token);
                var proses = context.GetCollections(new { key = "", instkey = "", invalid = false },
                    "proses",
                    $"<key:'{cmd.key}'>".Replace("<", "{").Replace(">", "}"),
                    "{key:1, instkey:1, invalid:1, _id:0}").FirstOrDefault();

                if (proses == null)
                    return new UnprocessableEntityObjectResult("Request Pembayaran tidak ada");
                if (proses.invalid == true)
                    return new UnprocessableEntityObjectResult("Request Pembayaran tidak aktif");

                var instance = ghost.Get(proses.instkey).GetAwaiter().GetResult();
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Request Pembayaran belum lengkap");
                if (instance.closed)
                    return new UnprocessableEntityObjectResult("Request Pembayaran telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == cmd.rkey) && n.Active == true);
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow Request Pembayaran tidak jelas");

                var route = node.routes.First(r => r.key == cmd.rkey);

                var issuing = node._state == ToDoState.created_ && route._verb == ToDoVerb.issue_;

                if (issuing && cmd.proses == "pajak")
                {
                    var pajak = context.pajaks.Query(x => x.key == cmd.key && x.invalid != true).FirstOrDefault();

                    if (!pajak.details.Any())
                        return new UnprocessableEntityObjectResult("Request Pembayaran hanya bisa diterbitkan jika sudah ditambahkan detail bidang yang akan diproses");

                    if (pajak.details.Any(x => x.subTypes.Count() == 0))
                        return new UnprocessableEntityObjectResult("Request Pembayaran hanya bisa diterbitkan jika sudah ditambahkan detail pajak yang akan diproses");

                    if (pajak.withChild().Contains(cmd.type))
                    {
                        ghost.RegisterDocs(instance.key, pajak.details.Select(d => d.key).ToArray(), true).Wait();
                    }
                }
                else if (issuing && cmd.proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.key == cmd.key && x.invalid != true).FirstOrDefault();

                    if (!sertifikasi.details.Any())
                        return new UnprocessableEntityObjectResult("Request Pembayaran hanya bisa diterbitkan jika sudah ditambahkan detail bidang yang akan diproses");

                    if (sertifikasi.details.Any(x => x.subTypes.Count() == 0))
                        return new UnprocessableEntityObjectResult("Request Pembayaran hanya bisa diterbitkan jika sudah ditambahkan detail sertifikasi yang akan diproses");
                }

                (var ok, var reason) = ghost.Take(user, proses.instkey, cmd.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, proses.instkey, cmd.rkey, cmd.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);

                var priv = route.privs.FirstOrDefault();
                string error = null;
                if (cmd.control == ToDoControl.yes_)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.approve_ => (UpdateDateRFP(cmd.key, cmd.proses, node._state)),
                        _ => (true, null)
                    };
                else if (cmd.control == ToDoControl._)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.abort_ => (Abort(cmd.key, cmd.proses, node._state, cmd.reason, user)),
                        ToDoVerb.confirmAbort_ => (FinalAbort(cmd.key, cmd.proses)),
                        _ => (true, null)
                    };
                else if (cmd.control == ToDoControl.no_)
                {
                    (ok, error) = AddReason(cmd.key, cmd.proses, node._state, cmd.reason, route?._verb, user);
                }

                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status Request Pembayaran" : error);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("step-dtl")]
        public IActionResult StepDtl([FromQuery] string token, [FromBody] ProsesDtlCommand cmd)
        {
            try
            {
                var user = context.FindUser(token);

                var pajak = context.pajaks.Query(x => x.key == cmd.key && x.invalid != true).FirstOrDefault();
                if (pajak == null)
                    return new UnprocessableEntityObjectResult("Request Pembayaran tidak ada");

                if (pajak.invalid == true)
                    return new UnprocessableEntityObjectResult("Request Pembayaran tidak aktif");

                var detail = pajak.details.FirstOrDefault(d => d.key == cmd.dkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Request Pembayaran tidak valid");

                var instance = pajak.instance;
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Request Pembayaran belum lengkap");

                if (instance.closed)
                    return new UnprocessableEntityObjectResult("Request Pembayaran telah selesai diproses");

                var subinst = ghost.GetSub(instance.key, cmd.dkey).GetAwaiter().GetResult();
                if (subinst == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Detail Request Pembayaran belum lengkap");
                if (subinst.closed)
                    return new UnprocessableEntityObjectResult("Detail Request Pembayaran telah selesai diproses");

                (var ok, var reason) = ghost.Take(user, pajak.instkey, cmd.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, pajak.instkey, cmd.rkey, cmd.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);

                string error = null;
                var route = subinst.Core.nodes.OfType<GraphNode>().SelectMany(n => n.routes).FirstOrDefault(r => r.key == cmd.rkey);
                if (cmd.control == ToDoControl._)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.continue_ => (UpdateValidasi(cmd)),
                        _ => (true, null)
                    };
                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status Request Pembayaran bidang" : error);
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("get-notaris")]
        public IActionResult GetNotaris(string keyPersil)
        {
            try
            {
                var notaris = context.persils.FirstOrDefault(x => x.key == keyPersil)?.PraNotaris;
                return new JsonResult(notaris);

            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("history")]
        public IActionResult HistoryPersil([FromQuery] string token, string type, string subType)
        {

            try
            {
                var proses = context.GetCollections(new Pajak(), "proses",
                                                            $"<invalid:<$ne:true>, type:'{type}'", "{_id:0}").ToList();
                var prosesExistsPersil = proses.SelectMany(x => x.details, (y, z) => new { pajak = y, keyPersil = z.keyPersil, subType = z.subTypes.SelectMany(x => x.subType).ToList() }).ToList();
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

            return Ok();
        }

        [HttpGet("isrepeat")]
        public IActionResult Repeat([FromQuery] string keyType, string keySubtype, string keyPersil, string proses)
        {
            var subTypes = context.GetCollections(new ProsesType(), "prosestype", $"<key:'{keyType}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                .SelectMany(x => x.subType).FirstOrDefault(x => x.key == keySubtype);

            if (proses == "sertifikasi")
            {
                if (subTypes.repeatReq == false)
                {
                    var sertifikasis = context.sertifikasis.Query(x => x.type == keyType).SelectMany(x => x.details)
                        .SelectMany(x => x.subTypes, (y, z) => new { keyPersil = y.keyPersil, sub = z.subType }).ToList();

                    if (sertifikasis.Any(x => x.keyPersil == keyPersil && x.sub == keySubtype))
                        return Ok(false);
                }
            }

            if (proses == "pajak")
            {
                if (subTypes.repeatReq == false)
                {
                    var pajaks = context.pajaks.Query(x => x.type == keyType).SelectMany(x => x.details)
                       .SelectMany(x => x.subTypes, (y, z) => new { keyPersil = y.keyPersil, sub = z.subType }).ToList();

                    if (pajaks.Any(x => x.keyPersil == keyPersil && x.sub == keySubtype))
                        return Ok(false);
                }
            }

            return Ok(true);
        }

        [NeedToken("PAY_CASHIER_LEADER,PAY_TAX_FULL,PAY_TAX_CLOSE")]
        [HttpPost("dtl-paidbyowner")]
        public IActionResult PaidByOwners([FromQuery] string token, string keyProses, [FromBody] ProsesDtlPaidByOwner core)
        {
            try
            {
                var user = context.FindUser(token);
                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();
                var allowed = priv.Intersect(new[] { "PAY_CASHIER_LEADER", "PAY_TAX_FULL", "PAY_TAX_CLOSE" }).Any();

                var pajak = context.pajaks.FirstOrDefault(x => x.key == keyProses);

                if (pajak == null)
                    return new UnprocessableEntityObjectResult("RFP tidak ada");

                //var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
                //var lastState = gMain.lastState?.state;

                if (!allowed)
                    return new UnprocessableEntityObjectResult($"Anda tidak memiliki hak merubah data");

                var dtl = pajak.details.FirstOrDefault(x => x.key == core.keyDetail);
                var sub = dtl.subTypes.FirstOrDefault(x => x.subType == core.subtype);

                if (sub.nominal < core.nominalPBO)
                    return new UnprocessableEntityObjectResult($"nominal yang sudah dibayar owner tidak boleh lebih besar dari pembayaran");

                if (core.nominalPBO < 0)
                    return new UnprocessableEntityObjectResult($"nominal yang sudah dibayar owner tidak boleh lebih minus");

                sub.FromCore(core.reasonPBO, core.nominalPBO);

                context.pajaks.Update(pajak);
                context.SaveChanges();

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpGet("get-docs")]
        public IActionResult GetDocs()
        {
            try
            {
                var docs = context.GetCollections(new StaticCollSertifikasi(), "static_collections", "{_t:'sertifikasi'}", "{_id:0, Docs:1}").FirstOrDefault();

                var documents = docs.Docs.Select(d => new LandropePayContext.entitas { key = d.JenisId, identity = d.JenisDokumen }).ToArray();

                return new JsonResult(documents);

            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        (bool OK, string error) UpdateDateRFP(string keyProses, string proses, ToDoState state)
        {
            try
            {
                if (proses == "sertifikasi")
                {
                    var sertifikasi = context.sertifikasis.Query(x => x.key == keyProses).FirstOrDefault();

                    if (state == ToDoState.verification2_)
                        sertifikasi.rfp.tglVerifikasi = DateTime.Now;
                    else if (state == ToDoState.verifLegal_)
                        sertifikasi.rfp.tglLegal = DateTime.Now;
                    else if (state == ToDoState.accountingApproval_)
                        sertifikasi.rfp.tglAccounting = DateTime.Now;
                    else if (state == ToDoState.reviewAndAcctgApproved_)
                        sertifikasi.rfp.tglKasir = DateTime.Now;
                    else if (state == ToDoState.processClose_)
                        sertifikasi.rfp.tglTerimaBukti = DateTime.Now;

                    context.sertifikasis.Update(sertifikasi);
                    context.SaveChanges();
                }
                else
                {
                    var pajak = context.pajaks.Query(x => x.key == keyProses).FirstOrDefault();

                    if (state == ToDoState.verification3_)
                        pajak.rfp.tglVerifikasi = DateTime.Now;
                    else if (state == ToDoState.verifLegal_)
                        pajak.rfp.tglLegal = DateTime.Now;
                    else if (state == ToDoState.accountingApproval_)
                        pajak.rfp.tglAccounting = DateTime.Now;
                    else if (state == ToDoState.reviewAndAcctgApproved_)
                        pajak.rfp.tglKasir = DateTime.Now;
                    else if (state == ToDoState.taxClose_)
                        pajak.rfp.tglTerimaBukti = DateTime.Now;

                    context.pajaks.Update(pajak);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return (true, null);
        }

        (bool OK, string error) UpdateValidasi(ProsesDtlCommand cmd)
        {
            try
            {
                var pajak = context.pajaks.FirstOrDefault(x => x.key == cmd.key);
                var detail = pajak.details.FirstOrDefault(x => x.key == cmd.dkey);
                var subDetail = detail.subTypes.FirstOrDefault();

                var gMain = ghost.Get(pajak.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

                cmd.validasi.tanggaldiBuat = gMain.states.LastOrDefault(s => s.state == ToDoState.created_)?.time;
                cmd.validasi.tanggaldiKirim = gMain.states.LastOrDefault(s => s.state == ToDoState.reviewerApproval_)?.time;
                cmd.validasi.tanggalSelesai = gMain.states.LastOrDefault(s => s.state == ToDoState.taxClose_)?.time;

                subDetail.FromCore(cmd.validasi);

                context.pajaks.Update(pajak);
                context.SaveChanges();
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return (true, null);
        }

        (bool OK, string error) Abort(string keyProses, string proses, ToDoState state, string desc, user user)
        {
            var reason = new Reason
            {
                tanggal = DateTime.Now,
                flag = true,
                keyCreator = user.key,
                privs = "",
                state = state,
                description = desc
            };

            if (proses == "sertifikasi")
            {
                var old = context.sertifikasis.FirstOrDefault(a => a.key == keyProses);
                old.rfp.AddReason(reason);
                if (state == ToDoState.created_)
                    old.Abort();

                context.sertifikasis.Update(old);
                context.SaveChanges();
            }
            else
            {
                var old = context.pajaks.FirstOrDefault(a => a.key == keyProses);
                old.rfp.AddReason(reason);
                if (state == ToDoState.created_)
                    old.Abort();

                context.pajaks.Update(old);
                context.SaveChanges();
            }

            return (true, null);
        }

        (bool OK, string error) FinalAbort(string keyProses, string proses)
        {
            if (proses == "sertifikasi")
            {
                var old = context.sertifikasis.FirstOrDefault(a => a.key == keyProses);

                old.Abort();

                context.sertifikasis.Update(old);
                context.SaveChanges();
            }
            else
            {
                var old = context.pajaks.FirstOrDefault(a => a.key == keyProses);

                old.Abort();

                context.pajaks.Update(old);
                context.SaveChanges();
            }

            return (true, null);
        }

        (bool OK, string error) AddReason(string keyProses, string proses, ToDoState state, string desc, ToDoVerb? verb, user user)
        {
            try
            {
                var reason = new Reason
                {
                    tanggal = DateTime.Now,
                    flag = verb == ToDoVerb.abort_ ? true : false,
                    keyCreator = user.key,
                    privs = "",
                    state = state,
                    description = desc
                };

                if (proses == "sertifikasi")
                {
                    var old = context.sertifikasis.FirstOrDefault(a => a.key == keyProses);
                    old.rfp.AddReason(reason);

                    context.sertifikasis.Update(old);
                    context.SaveChanges();
                }
                else
                {
                    var old = context.pajaks.FirstOrDefault(a => a.key == keyProses);
                    old.rfp.AddReason(reason);

                    context.pajaks.Update(old);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return (true, null);
        }

        bool IsRepeat(string keyProses, string keyDetail, string keyType, string keySubtype, string keyPersil, string proses)
        {
            var subTypes = context.GetCollections(new ProsesType(), "prosestype", $"<key:'{keyType}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                .SelectMany(x => x.subType).FirstOrDefault(x => x.key == keySubtype);

            if (proses == "sertifikasi")
            {
                if (subTypes.repeatReq == false)
                {
                    var sertifikasis = context.sertifikasis.Query(x => x.type == keyType).SelectMany(x => x.details)
                        .SelectMany(x => x.subTypes, (y, z) => new { keyPersil = y.keyPersil, sub = z.subType }).ToList();

                    if (sertifikasis.Any(x => x.keyPersil == keyPersil && x.sub == keySubtype))
                    {
                        var iomExists = IOMUploaded(keyProses, keySubtype, keyDetail, proses);
                        if (!iomExists)
                            return false;
                    }
                }
            }

            if (proses == "pajak")
            {
                if (subTypes.repeatReq == false)
                {
                    var pajaks = context.pajaks.Query(x => x.type == keyType).SelectMany(x => x.details)
                       .SelectMany(x => x.subTypes, (y, z) => new { keyPersil = y.keyPersil, sub = z.subType }).ToList();

                    if (pajaks.Any(x => x.keyPersil == keyPersil && x.sub == keySubtype))
                    {
                        var iomExists = IOMUploaded(keyProses, keySubtype, keyDetail, proses);
                        if (!iomExists)
                            return false;
                    }
                }
            }

            return true;
        }

        bool IOMUploaded(string keyProses, string keySubtype, string keyDetail, string proses)
        {
            try
            {
                var fname = $"{proses}//{keyProses}-{keyDetail}{keySubtype}";

                var lastfiles = fgrid.GetBucket().Find($"{{filename: '{fname}'}}");
                if (!lastfiles.Any())
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private landrope.mod2.Persil GetPersil(string key)
        {
            return context.persils.FirstOrDefault(p => p.key == key);
        }

        private void UpdateLuasPBTPT((string key, double? luas)[] datas, user user)
        {
            foreach (var data in datas)
            {
                var persil = GetPersil(data.key);

                if (persil == null)
                    continue;

                if ((persil.PajakSertifikasi?.LuasNIBBidang ?? 0) == data.luas)
                    continue;

                var hist = new landrope.mod2.PajakSertifikasiHistories(user)
                {
                    LuasNIBBidang = data.luas,
                    date = DateTime.Now
                };

                var lst = new List<landrope.mod2.PajakSertifikasiHistories>();
                if (persil.PajakSertifikasi?.histories != null)
                    lst = persil.PajakSertifikasi.histories.ToList();

                lst.Add(hist);

                var pajakSertifikasi = new landrope.mod2.PajakSertifikasi
                {
                    LuasNIBBidang = data.luas,
                    histories = lst.ToArray()
                };

                persil.PajakSertifikasi = pajakSertifikasi;


                context.persils.Update(persil);
                context.SaveChanges();
            }
        }

        private smallBundle[] GetSmallBundle(string keys)
        {
            var typename = MetadataKey.Luas.ToString("g");
            var typename2 = MetadataKey.Nomor.ToString("g");

            var bundles = contextplus.GetCollections(new MainBundle(), "bundles", $"<key:<$in:[{keys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                .Select(x => new
                {
                    keyPersil = x.key,
                    PartLuas = x.doclist.FirstOrDefault(b => b.keyDocType == "JDOK050")?
                    .entries.LastOrDefault()?
                    .Item.FirstOrDefault().Value,
                    PartSK = x.doclist.FirstOrDefault(b => b.keyDocType == "JDOK047")?
                    .entries.LastOrDefault()?
                    .Item.FirstOrDefault().Value
                }).Select(x => new
                {
                    keyPersil = x.keyPersil,
                    DynLuas = x.PartLuas == null ? null : x.PartLuas.props.TryGetValue(typename, out Dynamic dynamic) ? dynamic : null,
                    DynSK = x.PartSK == null ? null : x.PartSK.props.TryGetValue(typename2, out Dynamic dynamic2) ? dynamic2 : null
                }).Select(x => new smallBundle
                {
                    keyPersil = x.keyPersil,
                    Luas = x.DynLuas?.Value != null ? Convert.ToDouble(x.DynLuas?.Value) : 0,
                    SK = x.DynSK?.Value != null ? Convert.ToString(x.DynLuas?.Value) : string.Empty
                }).ToArray();

            return bundles;
        }

        private class smallBundle
        {
            public string keyPersil { get; set; }
            public double? Luas { get; set; }
            public string SK { get; set; }
        }

        [HttpPost("import-proses-pajak")]
        public IActionResult ImportProsesPajak(IFormFile file)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var ColumnInfoes = ColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

                var name = file.FileName;
                var strm = file.OpenReadStream();
                var data = new byte[strm.Length];
                strm.Read(data, 0, data.Length);

                Stream stream = new MemoryStream(data);

                var reader = ExcelReaderFactory.CreateReader(stream).AsDataSet();

                var failures = new List<Failures>();
                var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
                if (table == null)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dapat diproses"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };

                }

                var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
                        .Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
                        .Where(x => !String.IsNullOrEmpty(x.s)).ToList();

                foreach (var (s, i) in firstrow)
                {
                    var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s.Trim().ToLower()));

                    if (col != null)
                        switch (col)
                        {
                            case ColInfoS cs: cs.number = i; break;
                            case ColInfoM cm: cm.numbers.Add(i); break;
                        }
                }

                var colRFP = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.RFP);
                var noRFP = ((ColInfoS)colRFP).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 2 || noRFP == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var colId = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.IdBidang);
                var noIdBidang = ((ColInfoS)colId).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 2 || noIdBidang == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);

                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var rows = table.Rows.Cast<DataRow>().Skip(1).Select((r, i) => (r, i))
                .Where(x => x.r[noIdBidang] != DBNull.Value).ToArray();

                var locations = context.GetVillages().ToArray().AsParallel();
                var ptsks = context.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray().AsParallel();

                var prosesTypes = context.GetCollections(new ProsesType(), "prosestype", "{}", "{_id:0}").ToList()
                                   .SelectMany(x => x.subType, (y, z) => new { keyHead = y.key, nameHead = y.desc, keySub = z.key, nameSub = z.desc }).ToArray();

                foreach (var (r, i) in rows)
                {
                    var objidbidang = r[noIdBidang];
                    var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
                    if (string.IsNullOrWhiteSpace(idbidang))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            Keterangan = "Bidang kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var objRFP = r[noRFP];
                    var RFP = objRFP == DBNull.Value ? null : objRFP.ToString();
                    if (string.IsNullOrWhiteSpace(RFP))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            Keterangan = "RFP kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var persil = context.persils.FirstOrDefault(p => p.IdBidang == idbidang);
                    if (persil == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            RFP = RFP,
                            Keterangan = "Bidang tidak ditemukan, cek kembali ID BIDANG"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var _project = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Project).Get<string>(r);
                    var _desa = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Desa).Get<string>(r);
                    var _ptsk = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PTSK).Get<string>(r);
                    var _nominal = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Nominal).Get<double>(r);
                    var _type = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Type).Get<string>(r);
                    var _subtype = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Subtype).Get<string>(r);

                    var _tglDibuat = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDibuat).Get<DateTime>(r);
                    var _tglDiVerifikasi = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiVerifikasi).Get<DateTime>(r);
                    var _tglDiLegal = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiLegal).Get<DateTime>(r);
                    var _tglDiKasir = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiKasir).Get<DateTime>(r);
                    var _tglDiAccounting = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiAccounting).Get<DateTime>(r);
                    var _tglTerimaBuktiBayar = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglTerimaBuktiBayar).Get<DateTime>(r);
                    var _tglDiSerahkan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiserahkan).Get<DateTime>(r);
                    var _tglExpired = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglExpired).Get<DateTime>(r);

                    var _njop = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.NJOP).Get<double>(r);
                    var _npoptkp = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.NPOPTKP).Get<double>(r);
                    var _tahunpengaktifan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TahunPengaktifan).Get<double>(r);
                    var _keterangan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Keterangan).Get<string>(r);
                    var _tahun1 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun1).Get<double>(r);
                    var _denda1 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda1).Get<double>(r);
                    var _nominal1 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal1).Get<double>(r);
                    var _tahun2 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun2).Get<double>(r);
                    var _denda2 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda2).Get<double>(r);
                    var _nominal2 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal2).Get<double>(r);
                    var _tahun3 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun3).Get<double>(r);
                    var _denda3 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda3).Get<double>(r);
                    var _nominal3 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal3).Get<double>(r);
                    var _tahun4 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun4).Get<double>(r);
                    var _denda4 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda4).Get<double>(r);
                    var _nominal4 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal4).Get<double>(r);
                    var _tahun5 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun5).Get<double>(r);
                    var _denda5 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda5).Get<double>(r);
                    var _nominal5 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal5).Get<double>(r);
                    var _tahun6 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun6).Get<double>(r);
                    var _denda6 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda6).Get<double>(r);
                    var _nominal6 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal6).Get<double>(r);
                    var _tahun7 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun7).Get<double>(r);
                    var _denda7 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda7).Get<double>(r);
                    var _nominal7 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal7).Get<double>(r);
                    var _tahun8 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun8).Get<double>(r);
                    var _denda8 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda8).Get<double>(r);
                    var _nominal8 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal8).Get<double>(r);
                    var _tahun9 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TTahun9).Get<double>(r);
                    var _denda9 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Denda9).Get<double>(r);
                    var _nominal9 = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.TNominal9).Get<double>(r);

                    var _vbpnnotaris = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.vBpnNotaris).Get<string>(r);
                    var _vtanggaldibuat = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.vTanggalDibuat).Get<DateTime?>(r);
                    var _vtanggaldikirim = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.vTanggalDikirim).Get<DateTime>(r);
                    var _vtanggalselesai = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.vTanggalSelesai).Get<DateTime>(r);
                    var _vketerangan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.vKeterangan).Get<string>(r);

                    Action<string, string> chkerr = (_name, st) =>
                    {
                        if (!string.IsNullOrEmpty(st))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Keterangan = $"Error: {_name.Substring(1)}:{st}"
                            };
                            failures.Add(fail);
                        }
                    };

                    chkerr.Invoke(nameof(_project), _project.err);
                    chkerr.Invoke(nameof(_desa), _desa.err);
                    chkerr.Invoke(nameof(_ptsk), _ptsk.err);
                    chkerr.Invoke(nameof(_nominal), _nominal.err);
                    chkerr.Invoke(nameof(_type), _subtype.err);
                    chkerr.Invoke(nameof(_subtype), _subtype.err);

                    chkerr.Invoke(nameof(_tglDibuat), _tglDibuat.err);
                    chkerr.Invoke(nameof(_tglDiVerifikasi), _tglDiVerifikasi.err);
                    chkerr.Invoke(nameof(_tglDiLegal), _tglDiLegal.err);
                    chkerr.Invoke(nameof(_tglDiKasir), _tglDiKasir.err);
                    chkerr.Invoke(nameof(_tglDiAccounting), _tglDiAccounting.err);
                    chkerr.Invoke(nameof(_tglTerimaBuktiBayar), _tglTerimaBuktiBayar.err);
                    chkerr.Invoke(nameof(_tglDiSerahkan), _tglDiSerahkan.err);
                    chkerr.Invoke(nameof(_tglExpired), _tglExpired.err);

                    chkerr.Invoke(nameof(_njop), _njop.err);
                    chkerr.Invoke(nameof(_npoptkp), _npoptkp.err);
                    chkerr.Invoke(nameof(_tahunpengaktifan), _tahunpengaktifan.err);
                    chkerr.Invoke(nameof(_keterangan), _keterangan.err);
                    chkerr.Invoke(nameof(_tahun1), _tahun1.err);
                    chkerr.Invoke(nameof(_tahun2), _tahun2.err);
                    chkerr.Invoke(nameof(_tahun3), _tahun3.err);
                    chkerr.Invoke(nameof(_tahun4), _tahun4.err);
                    chkerr.Invoke(nameof(_tahun5), _tahun5.err);
                    chkerr.Invoke(nameof(_tahun6), _tahun6.err);
                    chkerr.Invoke(nameof(_tahun7), _tahun7.err);
                    chkerr.Invoke(nameof(_tahun8), _tahun8.err);
                    chkerr.Invoke(nameof(_tahun9), _tahun9.err);
                    chkerr.Invoke(nameof(_denda1), _denda1.err);
                    chkerr.Invoke(nameof(_denda2), _denda2.err);
                    chkerr.Invoke(nameof(_denda3), _denda3.err);
                    chkerr.Invoke(nameof(_denda4), _denda4.err);
                    chkerr.Invoke(nameof(_denda5), _denda5.err);
                    chkerr.Invoke(nameof(_denda6), _denda6.err);
                    chkerr.Invoke(nameof(_denda7), _denda7.err);
                    chkerr.Invoke(nameof(_denda8), _denda8.err);
                    chkerr.Invoke(nameof(_denda9), _denda9.err);
                    chkerr.Invoke(nameof(_nominal1), _nominal1.err);
                    chkerr.Invoke(nameof(_nominal2), _nominal2.err);
                    chkerr.Invoke(nameof(_nominal3), _nominal3.err);
                    chkerr.Invoke(nameof(_nominal4), _nominal4.err);
                    chkerr.Invoke(nameof(_nominal5), _nominal5.err);
                    chkerr.Invoke(nameof(_nominal6), _nominal6.err);
                    chkerr.Invoke(nameof(_nominal7), _nominal7.err);
                    chkerr.Invoke(nameof(_nominal8), _nominal8.err);
                    chkerr.Invoke(nameof(_nominal9), _nominal9.err);

                    chkerr.Invoke(nameof(_vbpnnotaris), _vbpnnotaris.err);
                    chkerr.Invoke(nameof(_vtanggaldibuat), _vtanggaldibuat.err);
                    chkerr.Invoke(nameof(_vtanggaldikirim), _vtanggaldikirim.err);
                    chkerr.Invoke(nameof(_vtanggalselesai), _vtanggalselesai.err);
                    chkerr.Invoke(nameof(_vketerangan), _vketerangan.err);

                    var project = _project.data.Cast<string>().FirstOrDefault();
                    var desa = _desa.data.Cast<string>().FirstOrDefault();
                    var ptsk = _ptsk.data.Cast<string>().FirstOrDefault();
                    var nominal = _nominal.data.Cast<double?>().FirstOrDefault();
                    var type = _type.data.Cast<string>().FirstOrDefault();
                    var subtype = _subtype.data.Cast<string>().FirstOrDefault();

                    var tgldibuat = _tglDibuat.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiverifikasi = _tglDiVerifikasi.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldilegal = _tglDiLegal.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldikasir = _tglDiKasir.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiaccounting = _tglDiAccounting.data.Cast<DateTime?>().FirstOrDefault();
                    var tglterimabuktibayar = _tglTerimaBuktiBayar.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiserahkan = _tglDiSerahkan.data.Cast<DateTime?>().FirstOrDefault();
                    var tglexpired = _tglExpired.data.Cast<DateTime?>().FirstOrDefault();

                    var njop = _njop.data.Cast<double?>().FirstOrDefault();
                    var npoptkp = _npoptkp.data.Cast<double?>().FirstOrDefault();
                    var tahunpengaktifan = _tahunpengaktifan.data.FirstOrDefault().ToString();
                    var keterangan = _keterangan.data.Cast<string>().FirstOrDefault();
                    var tahun1 = _tahun1.data.Cast<double?>().FirstOrDefault();
                    var tahun2 = _tahun2.data.Cast<double?>().FirstOrDefault();
                    var tahun3 = _tahun3.data.Cast<double?>().FirstOrDefault();
                    var tahun4 = _tahun4.data.Cast<double?>().FirstOrDefault();
                    var tahun5 = _tahun5.data.Cast<double?>().FirstOrDefault();
                    var tahun6 = _tahun6.data.Cast<double?>().FirstOrDefault();
                    var tahun7 = _tahun7.data.Cast<double?>().FirstOrDefault();
                    var tahun8 = _tahun8.data.Cast<double?>().FirstOrDefault();
                    var tahun9 = _tahun9.data.Cast<double?>().FirstOrDefault();
                    var denda1 = _denda1.data.Cast<double?>().FirstOrDefault();
                    var denda2 = _denda2.data.Cast<double?>().FirstOrDefault();
                    var denda3 = _denda3.data.Cast<double?>().FirstOrDefault();
                    var denda4 = _denda4.data.Cast<double?>().FirstOrDefault();
                    var denda5 = _denda5.data.Cast<double?>().FirstOrDefault();
                    var denda6 = _denda6.data.Cast<double?>().FirstOrDefault();
                    var denda7 = _denda7.data.Cast<double?>().FirstOrDefault();
                    var denda8 = _denda8.data.Cast<double?>().FirstOrDefault();
                    var denda9 = _denda9.data.Cast<double?>().FirstOrDefault();
                    var nominal1 = _nominal1.data.Cast<double?>().FirstOrDefault();
                    var nominal2 = _nominal2.data.Cast<double?>().FirstOrDefault();
                    var nominal3 = _nominal3.data.Cast<double?>().FirstOrDefault();
                    var nominal4 = _nominal4.data.Cast<double?>().FirstOrDefault();
                    var nominal5 = _nominal5.data.Cast<double?>().FirstOrDefault();
                    var nominal6 = _nominal6.data.Cast<double?>().FirstOrDefault();
                    var nominal7 = _nominal7.data.Cast<double?>().FirstOrDefault();
                    var nominal8 = _nominal8.data.Cast<double?>().FirstOrDefault();
                    var nominal9 = _nominal9.data.Cast<double?>().FirstOrDefault();

                    var Vbpnnotaris = _vbpnnotaris.data.Cast<string>().FirstOrDefault();
                    var vtanggaldibuat = _vtanggaldibuat.data.Cast<DateTime?>().FirstOrDefault();
                    var vtanggaldikirim = _vtanggaldikirim.data.Cast<DateTime?>().FirstOrDefault();
                    var vtanggalselesai = _vtanggalselesai.data.Cast<DateTime?>().FirstOrDefault();
                    var vketerangan = _vketerangan.data.Cast<string>().FirstOrDefault();

                    if ((nominal ?? 0) <= 0)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            RFP = RFP,
                            Keterangan = "Nilai nominal tidak benar"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var subtypes = prosesTypes.Where(x => x.nameHead.ToLower().Trim().Contains((type ?? "").ToLower().Trim()) && x.nameSub.ToLower().Trim().Contains((subtype ?? "").ToLower().Trim())).FirstOrDefault();

                    //var subtypes = prosesTypes.Where(x => (x.nameSub.ToLower().Trim().Contains(subtype.ToLower().Trim()))).FirstOrDefault();

                    if (subtypes == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            RFP = RFP,
                            Type = type,
                            Subtype = subtype,
                            Keterangan = "Type/Subtype tidak ditemukan di database"
                        };
                        failures.Add(fail);
                        continue;

                        //failures.Add($"Error: File {name} Row {i + 2} RFP {RFP} IdBidang {idbidang}, subtype tidak eksis di database");
                        //continue;
                    }

                    var nunggak = "tunggakan";

                    var listPinalti = new List<Pinalti>();

                    if (subtypes.nameSub.ToLower().Trim().Contains(nunggak))
                    {
                        //tahun 1
                        if ((tahun1 ?? 0) < 0 && ((nominal1 ?? 0) < 0 || (denda1 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 1 tidak benar"
                            };
                            failures.Add(fail);
                            continue;

                        }

                        if ((tahun1 ?? 0) > 0)
                        {
                            if ((nominal1 ?? 0) < 0 && (denda1 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 1 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun1.ToString(), denda = denda1, nominal = nominal1, total = (denda1 + nominal1) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 2
                        if ((tahun2 ?? 0) < 0 && ((nominal2 ?? 0) < 0 || (denda2 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 2 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun2 ?? 0) > 0)
                        {
                            if ((nominal2 ?? 0) < 0 && (denda2 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 2 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun2.ToString(), denda = denda2, nominal = nominal2, total = (denda2 + nominal2) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 3
                        if ((tahun3 ?? 0) <= 0 && ((nominal3 ?? 0) < 0 || (denda3 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 3 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun3 ?? 0) > 0)
                        {
                            if ((nominal3 ?? 0) < 0 && (denda3 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 3 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun3.ToString(), denda = denda3, nominal = nominal3, total = (denda3 + nominal3) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 4
                        if ((tahun4 ?? 0) < 0 && ((nominal4 ?? 0) < 0 || (denda4 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 4 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun4 ?? 0) > 0)
                        {
                            if ((nominal4 ?? 0) < 0 && (denda4 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 4 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun4.ToString(), denda = denda4, nominal = nominal4, total = (denda4 + nominal4) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 5
                        if ((tahun5 ?? 0) < 0 && ((nominal5 ?? 0) < 0 || (denda5 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 5 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun5 ?? 0) > 0)
                        {
                            if ((nominal5 ?? 0) < 0 && (denda5 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 5 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun5.ToString(), denda = denda5, nominal = nominal5, total = (denda5 + nominal5) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 6
                        if ((tahun6 ?? 0) < 0 && ((nominal6 ?? 0) < 0 || (denda6 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 6 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun6 ?? 0) > 0)
                        {
                            if ((nominal6 ?? 0) < 0 && (denda6 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 6 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun6.ToString(), denda = denda6, nominal = nominal6, total = (denda6 + nominal6) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 7
                        if ((tahun7 ?? 0) < 0 && ((nominal7 ?? 0) < 0 || (denda7 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 7 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun7 ?? 0) > 0)
                        {
                            if ((nominal7 ?? 0) < 0 && (denda7 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 7 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun7.ToString(), denda = denda7, nominal = nominal7, total = (denda7 + nominal7) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 8
                        if ((tahun8 ?? 0) < 0 && ((nominal8 ?? 0) < 0 || (denda8 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 8 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun8 ?? 0) > 0)
                        {
                            if ((nominal8 ?? 0) < 0 && (denda8 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 8 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun8.ToString(), denda = denda8, nominal = nominal8, total = (denda8 + nominal8) };

                            listPinalti.Add(pinalti);
                        }

                        // tahun 9
                        if ((tahun9 ?? 0) < 0 && ((nominal9 ?? 0) < 0 || (denda9 ?? 0) < 0))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Pengisian data di Tahun 9 tidak benar"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if ((tahun9 ?? 0) > 0)
                        {
                            if ((nominal9 ?? 0) < 0 && (denda9 ?? 0) <= 0)
                            {
                                var fail = new Failures
                                {
                                    File = name,
                                    Row = i + 2,
                                    IdBidang = idbidang,
                                    RFP = RFP,
                                    Type = type,
                                    Subtype = subtype,
                                    Keterangan = "Nilai nominal/denda di Tahun 9 tidak benar"
                                };
                                failures.Add(fail);
                                continue;
                            }

                            var pinalti = new Pinalti { tahun = tahun9.ToString(), denda = denda9, nominal = nominal9, total = (denda9 + nominal9) };

                            listPinalti.Add(pinalti);
                        }

                        if (listPinalti.Count() == 0)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Tidak ada data tunggakan"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var pnlt = listPinalti.Select(x => x.total).Sum();
                        if (nominal != pnlt)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Nilai nominal tidak sama dengan total tunggakan"
                            };
                            failures.Add(fail);
                            continue;
                        }

                    }

                    var vlds = "validasi";
                    var validasi = new Validasi();

                    if (subtypes.nameSub.ToLower().Trim().Contains(vlds))
                    {
                        if (vtanggaldibuat == null || vtanggaldikirim == null || vtanggalselesai == null || string.IsNullOrEmpty(Vbpnnotaris))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Data validasi tidak lengkap"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        validasi = new Validasi { BPN_Notaris = Vbpnnotaris, tanggaldiBuat = vtanggaldibuat, tanggaldiKirim = vtanggaldikirim, tanggalSelesai = vtanggalselesai, keterangan = vketerangan };
                    }

                    var pajak = context.pajaks.FirstOrDefault(x => x.rfp.nomor == RFP);

                    if (pajak == null)
                    {
                        var user = context.users.FirstOrDefault(x => x.identifier == "importer");
                        var host = HostServicesHelper.GetProsesHost(services);

                        var keyproject = locations.FirstOrDefault(x => project.ToLower().Trim().Contains(x.project.identity.ToLower().Trim())).project?.key;
                        var keydesa = locations.FirstOrDefault(x => desa.ToLower().Trim().Contains(x.desa.identity.ToLower().Trim())).desa?.key;
                        var keyptsk = ptsks.FirstOrDefault(x => ptsk.ToLower().Trim().Contains(x.name.ToLower().Trim()))?.key;

                        var rfp = new RFP
                        {
                            nomor = RFP,
                            tglBuat = tgldibuat,
                            tglVerifikasi = tgldiverifikasi,
                            tglLegal = tgldilegal,
                            tglAccounting = tgldiaccounting,
                            tglKasir = tgldikasir,
                            tglTerimaBukti = tglterimabuktibayar,
                            keterangan = "From Importer"
                        };

                        var ent = new Pajak
                        {
                            key = entity.MakeKey,
                            type = subtypes.keyHead,
                            rfp = rfp,
                            keyDesa = keydesa,
                            keyProject = keyproject,
                            keyPTSK = keyptsk,
                            keyCreator = user.key,
                            ExpiredDate = tglexpired,
                        };

                        ent.CreateGraphInstance(user, "pajak", subtypes.keyHead);

                        host.AddPajak(ent);

                        MakeConnectionGraph();

                        var inskey = ent.instkey;
                        var Gs = ghost.Get(inskey).GetAwaiter().GetResult() ?? new GraphMainInstance();
                        Gs.lastState.state = ToDoState.complished_;
                        Gs.lastState.time = DateTime.Now;
                        Gs.closed = true;

                        ghost.Update(Gs, contextGraph);

                        pajak = context.pajaks.Query(x => x.key == ent.key && x.invalid != true).FirstOrDefault();

                        var dtlExists = pajak.details.Any(x => x.keyPersil == persil.key);

                        if (dtlExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "ID Bidang sudah eksis di RFP yang sama"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var pajakdtl = new PajakDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.key };
                        pajak.AddDetail(pajakdtl);

                        context.pajaks.Update(pajak);
                        context.SaveChanges();

                        pajak = context.pajaks.Query(x => x.key == ent.key && x.invalid != true).FirstOrDefault();
                        var detail = pajak.details.Where(x => x.keyPersil == persil.key)?.FirstOrDefault();
                        var subExists = detail.subTypes.Any(x => x.subType == subtypes.keySub);

                        if (subExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Subtype sudah eksis di RFP dan Bidang yang sama"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var subTypeDetail = new PajakSubTypeDetail
                        {
                            nominal = nominal,
                            subType = subtypes.keySub,
                            NJOP = njop,
                            NPOPTKP = npoptkp,
                            keterangan = keterangan,
                            pinaltis = listPinalti.ToArray(),
                            tahunPengaktifan = tahunpengaktifan,
                            validasi = validasi
                        };

                        detail.AddSubTypes(subTypeDetail);

                        context.pajaks.Update(pajak);
                        context.SaveChanges();

                        if (tgldiserahkan != null)
                        {
                            var pajakdate = new pajakdate
                            {
                                key = pajak.key,
                                rfp = pajak.rfp.nomor,
                                keypersil = persil.key,
                                tglDiserahkan = tgldiserahkan
                            };

                            context.pajakdates.Insert(pajakdate);
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        var key = pajak.key;
                        var dtlExists = pajak.details.Any(x => x.keyPersil == persil.key);

                        var detail = pajak.details.Where(x => x.keyPersil == persil.key)?.FirstOrDefault() ?? new PajakDetail();
                        var subExists = detail.subTypes.Any(x => x.subType == subtypes.keySub);

                        if (dtlExists && subExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Subtype sudah eksis di RFP dan Bidang yang sama"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if (dtlExists)
                        {
                            var subTypeDetail = new PajakSubTypeDetail
                            {
                                nominal = nominal,
                                subType = subtypes.keySub,
                                NJOP = njop,
                                NPOPTKP = npoptkp,
                                keterangan = keterangan,
                                pinaltis = listPinalti.ToArray(),
                                tahunPengaktifan = tahunpengaktifan,
                                validasi = validasi
                            };

                            detail.AddSubTypes(subTypeDetail);

                            context.pajaks.Update(pajak);
                            context.SaveChanges();
                        }
                        else
                        {
                            var pajakdtl = new PajakDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.key };
                            var subTypeDetail = new PajakSubTypeDetail
                            {
                                nominal = nominal,
                                subType = subtypes.keySub,
                                NJOP = njop,
                                NPOPTKP = npoptkp,
                                keterangan = keterangan,
                                pinaltis = listPinalti.ToArray(),
                                tahunPengaktifan = tahunpengaktifan,
                                validasi = validasi
                            };

                            pajakdtl.AddSubTypes(subTypeDetail);
                            pajak.AddDetail(pajakdtl);

                            context.pajaks.Update(pajak);
                            context.SaveChanges();
                        }

                        if (tgldiserahkan != null)
                        {
                            var pajakdate = new pajakdate
                            {
                                key = pajak.key,
                                rfp = pajak.rfp.nomor,
                                keypersil = persil.key,
                                tglDiserahkan = tgldiserahkan
                            };

                            context.pajakdates.Insert(pajakdate);
                            context.SaveChanges();
                        }
                    }
                }

                var csv = MakeCsv(failures.ToArray());

                return new ContentResult { Content = csv.ToString(), ContentType = "text/csv" };

            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("import-proses-sertifikasi")]
        public IActionResult ImportProsesSertifikasi(IFormFile file)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var ColumnInfoes = ColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

                var name = file.FileName;
                var strm = file.OpenReadStream();
                var data = new byte[strm.Length];
                strm.Read(data, 0, data.Length);

                Stream stream = new MemoryStream(data);

                var reader = ExcelReaderFactory.CreateReader(stream).AsDataSet();

                var failures = new List<Failures>();
                var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
                if (table == null)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dapat diproses"
                    };
                    failures.Add(fail);
                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };

                }

                var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
                        .Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
                        .Where(x => !String.IsNullOrEmpty(x.s)).ToList();

                foreach (var (s, i) in firstrow)
                {
                    var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s.Trim().ToLower()));

                    if (col != null)
                        switch (col)
                        {
                            case ColInfoS cs: cs.number = i; break;
                            case ColInfoM cm: cm.numbers.Add(i); break;
                        }
                }

                var colRFP = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.RFP);
                var noRFP = ((ColInfoS)colRFP).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 2 || noRFP == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);
                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var colId = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.IdBidang);
                var noIdBidang = ((ColInfoS)colId).number;
                if (ColumnInfoes.Where(c => c.exists).Count() < 2 || noIdBidang == -1)
                {
                    var fail = new Failures
                    {
                        File = name,
                        Keterangan = "File tidak dipersiapkan dengan benar"
                    };
                    failures.Add(fail);
                    var result = MakeCsv(failures.ToArray());
                    return new ContentResult { Content = result.ToString(), ContentType = "text/csv" };
                }

                var rows = table.Rows.Cast<DataRow>().Skip(1).Select((r, i) => (r, i))
                .Where(x => x.r[noIdBidang] != DBNull.Value).ToArray();

                var locations = context.GetVillages().ToArray().AsParallel();
                var ptsks = context.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray().AsParallel();
                var notarist = context.db.GetCollection<mod2.Notaris>("masterdatas").Find("{_t:'notaris',invalid:{$ne:true}}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var prosesTypes = context.GetCollections(new ProsesType(), "prosestype", "{}", "{_id:0}").ToList()
                                    .SelectMany(x => x.subType, (y, z) => new { keyHead = y.key, nameHead = y.desc, keySub = z.key, nameSub = z.desc }).ToArray();

                foreach (var (r, i) in rows)
                {
                    var objidbidang = r[noIdBidang];
                    var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
                    if (string.IsNullOrWhiteSpace(idbidang))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            Keterangan = "Bidang kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var objRFP = r[noRFP];
                    var RFP = objRFP == DBNull.Value ? null : objRFP.ToString();
                    if (string.IsNullOrWhiteSpace(RFP))
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            Keterangan = "RFP kosong"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var persil = context.persils.FirstOrDefault(p => p.IdBidang == idbidang);
                    if (persil == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            Keterangan = "Bidang tidak ditemukan"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var _project = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Project).Get<string>(r);
                    var _desa = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Desa).Get<string>(r);
                    var _ptsk = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PTSK).Get<string>(r);
                    var _nominal = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Nominal).Get<double>(r);
                    var _type = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Type).Get<string>(r);
                    var _subtype = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Subtype).Get<string>(r);

                    var _tglDibuat = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDibuat).Get<DateTime>(r);
                    var _tglDiVerifikasi = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiVerifikasi).Get<DateTime>(r);
                    var _tglDiLegal = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiLegal).Get<DateTime>(r);
                    var _tglDiKasir = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiKasir).Get<DateTime>(r);
                    var _tglDiAccounting = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiAccounting).Get<DateTime>(r);
                    var _tglTerimaBuktiBayar = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglTerimaBuktiBayar).Get<DateTime>(r);
                    var _tglTerimaPIC = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglTerimaPIC).Get<DateTime>(r);
                    var _tglDiSerahkanPIC = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglDiserahkanPIC).Get<DateTime>(r);
                    var _tglTerimaInvoice = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.tglTerimaInvoice).Get<DateTime>(r);

                    var _patok = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.patok).Get<double>(r);
                    var _satuan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.satuan).Get<double>(r);
                    var _budget = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.budget).Get<double>(r);
                    var _jenisDokumenHilang = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.jenisDokumen).Get<string>(r);
                    var _jumlahAktaHilang = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.jumlahAkta).Get<double>(r);
                    var _aktor = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.aktor).Get<string>(r);

                    var _anomorakta = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.aNomorAkta).Get<string>(r);
                    var _atglakta = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.aTanggalAkta).Get<DateTime>(r);
                    var _abiayaperbuku = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.aBiayaPerBuku).Get<double>(r);
                    var _atotalharga = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.aTotalHarga).Get<double>(r);

                    Action<string, string> chkerr = (_name, st) =>
                    {
                        if (!string.IsNullOrEmpty(st))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Keterangan = $"Error: {_name.Substring(1)}:{st}"
                            };
                            failures.Add(fail);
                        }
                    };
                    chkerr.Invoke(nameof(_project), _project.err);
                    chkerr.Invoke(nameof(_desa), _desa.err);
                    chkerr.Invoke(nameof(_ptsk), _ptsk.err);
                    chkerr.Invoke(nameof(_nominal), _nominal.err);
                    chkerr.Invoke(nameof(_type), _type.err);
                    chkerr.Invoke(nameof(_subtype), _subtype.err);

                    chkerr.Invoke(nameof(_tglDibuat), _tglDibuat.err);
                    chkerr.Invoke(nameof(_tglDiVerifikasi), _tglDiVerifikasi.err);
                    chkerr.Invoke(nameof(_tglDiLegal), _tglDiLegal.err);
                    chkerr.Invoke(nameof(_tglDiKasir), _tglDiKasir.err);
                    chkerr.Invoke(nameof(_tglDiAccounting), _tglDiAccounting.err);
                    chkerr.Invoke(nameof(_tglTerimaBuktiBayar), _tglTerimaBuktiBayar.err);
                    chkerr.Invoke(nameof(_tglTerimaPIC), _tglTerimaPIC.err);
                    chkerr.Invoke(nameof(_tglDiSerahkanPIC), _tglDiSerahkanPIC.err);
                    chkerr.Invoke(nameof(_tglTerimaInvoice), _tglTerimaInvoice.err);

                    chkerr.Invoke(nameof(_patok), _patok.err);
                    chkerr.Invoke(nameof(_satuan), _satuan.err);
                    chkerr.Invoke(nameof(_budget), _budget.err);
                    chkerr.Invoke(nameof(_jenisDokumenHilang), _jenisDokumenHilang.err);
                    chkerr.Invoke(nameof(_jumlahAktaHilang), _jumlahAktaHilang.err);
                    chkerr.Invoke(nameof(_aktor), _aktor.err);

                    chkerr.Invoke(nameof(_anomorakta), _anomorakta.err);
                    chkerr.Invoke(nameof(_atglakta), _atglakta.err);
                    chkerr.Invoke(nameof(_abiayaperbuku), _abiayaperbuku.err);
                    chkerr.Invoke(nameof(_atotalharga), _jenisDokumenHilang.err);


                    var project = _project.data.Cast<string>().FirstOrDefault();
                    var desa = _desa.data.Cast<string>().FirstOrDefault();
                    var ptsk = _ptsk.data.Cast<string>().FirstOrDefault();
                    var nominal = _nominal.data.Cast<double?>().FirstOrDefault();
                    var type = _type.data.Cast<string>().FirstOrDefault();
                    var subtype = _subtype.data.Cast<string>().FirstOrDefault();

                    var tgldibuat = _tglDibuat.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiverifikasi = _tglDiVerifikasi.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldilegal = _tglDiLegal.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldikasir = _tglDiKasir.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiaccounting = _tglDiAccounting.data.Cast<DateTime?>().FirstOrDefault();
                    var tglterimabuktibayar = _tglTerimaBuktiBayar.data.Cast<DateTime?>().FirstOrDefault();
                    var tglterimapic = _tglTerimaPIC.data.Cast<DateTime?>().FirstOrDefault();
                    var tgldiserahkanpic = _tglDiSerahkanPIC.data.Cast<DateTime?>().FirstOrDefault();
                    var tglterimainvoice = _tglTerimaInvoice.data.Cast<DateTime?>().FirstOrDefault();

                    var patok = _patok.data.Cast<double?>().FirstOrDefault();
                    var satuan = _satuan.data.Cast<double?>().FirstOrDefault();
                    var budget = _budget.data.Cast<double?>().FirstOrDefault();
                    var jenisdokumenhilang = _jenisDokumenHilang.data.Cast<string?>().FirstOrDefault();
                    var jumlahaktahilang = _jumlahAktaHilang.data.Cast<double?>().FirstOrDefault();
                    var aktor = _aktor.data.Cast<string>().FirstOrDefault();

                    var anomorakta = _anomorakta.data.Cast<string>().FirstOrDefault();
                    var atglakta = _atglakta.data.Cast<DateTime?>().FirstOrDefault();
                    var abiayaperbuku = _abiayaperbuku.data.Cast<double?>().FirstOrDefault();
                    var atotalharga = _atotalharga.data.Cast<double?>().FirstOrDefault();

                    var subtypes = prosesTypes.Where(x => x.nameHead.ToLower().Trim().Contains((type ?? "").ToLower().Trim()) && x.nameSub.ToLower().Trim().Contains((subtype ?? "").ToLower().Trim())).FirstOrDefault();

                    if (subtypes == null)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            RFP = RFP,
                            Type = type,
                            Subtype = subtype,
                            Keterangan = "Type/Subtype tidak di temukan di database"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var akt = "akta";
                    var akta = new mod4.Akta();

                    if (subtypes.nameSub.ToLower().Trim().Contains(akt))
                    {
                        if (atglakta == null || (abiayaperbuku ?? 0) <= 0 || (atotalharga ?? 0) <= 0 || string.IsNullOrEmpty(anomorakta))
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Data Akta tidak lengkap"
                            };
                            failures.Add(fail);
                            continue;

                        }


                        akta = new mod4.Akta { nomorAkta = anomorakta, biayaPerBuku = abiayaperbuku, tanggalAkta = atglakta, totalHarga = atotalharga };
                        nominal = atotalharga;
                    }

                    if ((nominal ?? 0) <= 0)
                    {
                        var fail = new Failures
                        {
                            File = name,
                            Row = i + 2,
                            IdBidang = idbidang,
                            RFP = RFP,
                            Type = type,
                            Subtype = subtype,
                            Keterangan = "Nilai nominal tidak benar"
                        };
                        failures.Add(fail);
                        continue;
                    }

                    var sertifikasi = context.sertifikasis.FirstOrDefault(x => x.rfp.nomor == RFP);

                    if (sertifikasi == null)
                    {
                        var user = context.users.FirstOrDefault(x => x.identifier == "importer");
                        var host = HostServicesHelper.GetProsesHost(services);

                        var keyproject = locations.FirstOrDefault(x => project.ToLower().Trim().Contains(x.project.identity.ToLower().Trim())).project.key;
                        var keydesa = locations.FirstOrDefault(x => desa.ToLower().Trim().Contains(x.desa.identity.ToLower().Trim())).desa.key;
                        var keyptsk = ptsks.FirstOrDefault(x => ptsk.ToLower().Trim().Contains(x.name.ToLower().Trim())).key;
                        var notaris = notarist.FirstOrDefault(x => (aktor ?? "").ToLower().Trim().Contains(x.name.ToLower().Trim()))?.key;

                        var rfp = new RFP
                        {
                            nomor = RFP,
                            tglBuat = tgldibuat,
                            tglVerifikasi = tgldiverifikasi,
                            tglLegal = tgldilegal,
                            tglAccounting = tgldiaccounting,
                            tglKasir = tgldikasir,
                            tglTerimaBukti = tglterimabuktibayar,
                            keterangan = "From Importer"
                        };

                        var ent = new Sertifikasi
                        {
                            key = entity.MakeKey,
                            type = subtypes.keyHead,
                            rfp = rfp,
                            keyDesa = keydesa,
                            keyProject = keyproject,
                            keyPTSK = keyptsk,
                            keyCreator = user.key,
                            keyAktor = notaris,
                        };

                        ent.CreateGraphInstance(user, "sertifikasi", subtypes.keyHead);

                        host.AddSertifikasi(ent);

                        MakeConnectionGraph();

                        var inskey = ent.instkey;
                        var Gs = ghost.Get(inskey).GetAwaiter().GetResult() ?? new GraphMainInstance();
                        Gs.lastState.state = ToDoState.complished_;
                        Gs.lastState.time = DateTime.Now;
                        Gs.closed = true;

                        ghost.Update(Gs, contextGraph);

                        sertifikasi = context.sertifikasis.Query(x => x.key == ent.key && x.invalid != true).FirstOrDefault();

                        var dtlExists = sertifikasi.details.Any(x => x.keyPersil == persil.key);

                        if (dtlExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Bidang sudah eksis di RFP yang sama"
                            };
                            failures.Add(fail);
                            continue;

                        }

                        var sertifikasiDetail = new SertifikasiDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.key };
                        sertifikasi.AddDetail(sertifikasiDetail);

                        context.sertifikasis.Update(sertifikasi);
                        context.SaveChanges();

                        sertifikasi = context.sertifikasis.Query(x => x.key == ent.key && x.invalid != true).FirstOrDefault();
                        var detail = sertifikasi.details.Where(x => x.keyPersil == persil.key)?.FirstOrDefault();
                        var subExists = detail.subTypes.Any(x => x.subType == subtypes.keySub);

                        if (subExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Subtype sudah eksis di Bidang dan RFP yang sama"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        var subTypeDetail = new SertifikasiSubTypeDetail
                        {
                            nominal = nominal,
                            subType = subtypes.keySub,
                            akta = akta,
                            budget = budget,
                            jenisDokumen = jenisdokumenhilang,
                            jumlahAkta = Convert.ToInt32(jumlahaktahilang),
                            patok = patok,
                            satuan = satuan

                        };

                        detail.AddSubTypes(subTypeDetail);

                        context.sertifikasis.Update(sertifikasi);
                        context.SaveChanges();

                        if (tgldiserahkanpic != null && tglterimapic != null && tglterimainvoice != null)
                        {
                            var sertifikasidate = new sertifikasidate
                            {
                                key = sertifikasi.key,
                                rfp = sertifikasi.rfp.nomor,
                                keypersil = persil.key,
                                tglTerimaPIC = tglterimapic,
                                tglDiserahkanPIC = tgldiserahkanpic,
                                tglTerimaInvoice = tglterimainvoice

                            };

                            context.sertifikasidates.Insert(sertifikasidate);
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        var key = sertifikasi.key;
                        var dtlExists = sertifikasi.details.Any(x => x.keyPersil == persil.key);

                        var detail = sertifikasi.details.Where(x => x.keyPersil == persil.key)?.FirstOrDefault();
                        var subExists = detail.subTypes.Any(x => x.subType == subtypes.keySub);

                        if (dtlExists && subExists)
                        {
                            var fail = new Failures
                            {
                                File = name,
                                Row = i + 2,
                                IdBidang = idbidang,
                                RFP = RFP,
                                Type = type,
                                Subtype = subtype,
                                Keterangan = "Subtype sudah eksis di Bidang dan RFP yang sama"
                            };
                            failures.Add(fail);
                            continue;
                        }

                        if (dtlExists)
                        {
                            var subTypeDetail = new SertifikasiSubTypeDetail
                            {
                                nominal = nominal,
                                subType = subtypes.keySub,
                                akta = akta,
                                budget = budget,
                                jenisDokumen = jenisdokumenhilang,
                                jumlahAkta = Convert.ToInt32(jumlahaktahilang),
                                patok = patok,
                                satuan = satuan

                            };

                            detail.AddSubTypes(subTypeDetail);

                            context.sertifikasis.Update(sertifikasi);
                            context.SaveChanges();
                        }
                        else
                        {
                            var sertifikasiDetail = new SertifikasiDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = persil.key };
                            var subTypeDetail = new SertifikasiSubTypeDetail
                            {
                                nominal = nominal,
                                subType = subtypes.keySub,
                                akta = akta,
                                budget = budget,
                                jenisDokumen = jenisdokumenhilang,
                                jumlahAkta = Convert.ToInt32(jumlahaktahilang),
                                patok = patok,
                                satuan = satuan

                            };

                            sertifikasiDetail.AddSubTypes(subTypeDetail);
                            sertifikasi.AddDetail(sertifikasiDetail);

                            context.sertifikasis.Update(sertifikasi);
                            context.SaveChanges();
                        }

                        if (tgldiserahkanpic != null && tglterimapic != null && tglterimainvoice != null)
                        {
                            var sertifikasidate = new sertifikasidate
                            {
                                key = sertifikasi.key,
                                rfp = sertifikasi.rfp.nomor,
                                keypersil = persil.key,
                                tglTerimaPIC = tglterimapic,
                                tglDiserahkanPIC = tgldiserahkanpic,
                                tglTerimaInvoice = tglterimainvoice

                            };

                            context.sertifikasidates.Insert(sertifikasidate);
                            context.SaveChanges();
                        }
                    }
                }

                var csv = MakeCsv(failures.ToArray());
                return new ContentResult { Content = csv.ToString(), ContentType = "text/csv" };

            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        private void MakeConnectionGraph()
        {
            var appsets = Config.AppSettings;
            if (appsets == null)
                throw new InvalidOperationException("Unable to retrieve database connection informations");

            string encoded, server, replica, ssl, uid, pwd, database, protocol, name;
            string dataset = "graph";

            bool enc = appsets.TryGet($"{dataset}:encoded", out encoded) ? encoded == "True" : false;
            if (!appsets.TryGet($"{dataset}:server", out server) || !appsets.TryGet($"{dataset}:replica", out replica) ||
                !appsets.TryGet($"{dataset}:ssl", out ssl) || !appsets.TryGet($"{dataset}:uid", out uid) ||
                !appsets.TryGet($"{dataset}:pwd", out pwd) ||
                !appsets.TryGet($"{dataset}:database", out database)
                )
                throw new Exception("Invalid Doc Repository connection informations");
            if (enc)
                pwd = encryption.Decode64(pwd);
            protocol = "mongodb";
            appsets.TryGet($"{dataset}:protocol", out protocol);

            string url = $"{protocol}://{uid}:{pwd}@{server}/admin?ssl={ssl}&authSource=admin";
            contextGraph = new graph.mod.GraphContext(url, "admin");
            contextGraph.ChangeDB(database);
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

        private static string CsvFormatCorrection(object objVal)
        {
            string value = objVal != null ? objVal.ToString() : "";
            string correctedFormat = "";
            correctedFormat = value.Contains(",") ? "\"" + value + "\"" : value;
            return correctedFormat;
        }

        enum CollKind
        {
            RFP,
            IdBidang,
            Project,
            Desa,
            PTSK,
            Nominal,
            Type,
            Subtype,
            tglDibuat,
            tglDiVerifikasi,
            tglDiLegal,
            tglDiKasir,
            tglDiAccounting,
            tglTerimaBuktiBayar,

            tglDiserahkan,
            tglExpired,

            NJOP,
            NPOPTKP,
            TahunPengaktifan,
            Keterangan,
            TTahun1,
            Denda1,
            TNominal1,
            TTahun2,
            Denda2,
            TNominal2,
            TTahun3,
            Denda3,
            TNominal3,
            TTahun4,
            Denda4,
            TNominal4,
            TTahun5,
            Denda5,
            TNominal5,
            TTahun6,
            Denda6,
            TNominal6,
            TTahun7,
            Denda7,
            TNominal7,
            TTahun8,
            Denda8,
            TNominal8,
            TTahun9,
            Denda9,
            TNominal9,

            vBpnNotaris,
            vTanggalDibuat,
            vTanggalDikirim,
            vTanggalSelesai,
            vKeterangan,

            patok,
            satuan,
            budget,
            jenisDokumen,
            jumlahAkta,
            aktor,

            tglTerimaInvoice,
            tglTerimaPIC,
            tglDiserahkanPIC,

            aNomorAkta,
            aTanggalAkta,
            aBiayaPerBuku,
            aTotalHarga
        };

        static (CollKind kind, string caption, bool many)[] ColumnFacts = {
            (CollKind.RFP, "rfp", false),
            (CollKind.IdBidang, "idbidang", false),
            (CollKind.Project, "project", false),
            (CollKind.Desa, "desa", false),
            (CollKind.PTSK, "ptsk", false),
            (CollKind.Nominal, "nominal", false),
            (CollKind.Type, "type", false),
            (CollKind.Subtype, "subtype", false),
            (CollKind.tglDibuat, "tanggal buat", false),
            (CollKind.tglDiVerifikasi, "tanggal verifikasi", false),
            (CollKind.tglDiLegal, "tanggal legal", false),
            (CollKind.tglDiKasir, "tanggal kasir", false),
            (CollKind.tglDiAccounting, "tanggal accounting", false),
            (CollKind.tglTerimaBuktiBayar, "tanggal terima bukti bayar", false),

            (CollKind.tglDiserahkan, "tanggal diserahkan", false),
            (CollKind.tglExpired, "tanggal expired", false),

            (CollKind.NJOP, "njop per meter", false),
            (CollKind.NPOPTKP, "npoptkp", false),
            (CollKind.TahunPengaktifan, "tahun pengaktifan", false),
            (CollKind.Keterangan, "keterangan", false),
            (CollKind.TTahun1, "tahun 1", false),
            (CollKind.Denda1, "denda 1", false),
            (CollKind.TNominal1, "nominal 1", false),
            (CollKind.TTahun2, "tahun 2", false),
            (CollKind.Denda2, "denda 2", false),
            (CollKind.TNominal2, "nominal 2", false),
            (CollKind.TTahun3, "tahun 3", false),
            (CollKind.Denda3, "denda 3", false),
            (CollKind.TNominal3, "nominal 3", false),
            (CollKind.TTahun4, "tahun 4", false),
            (CollKind.Denda4, "denda 4", false),
            (CollKind.TNominal4, "nominal 4", false),
            (CollKind.TTahun5, "tahun 5", false),
            (CollKind.Denda5, "denda 5", false),
            (CollKind.TNominal5, "nominal 5", false),
            (CollKind.TTahun6, "tahun 6", false),
            (CollKind.Denda6, "denda 6", false),
            (CollKind.TNominal6, "nominal 6", false),
            (CollKind.TTahun7, "tahun 7", false),
            (CollKind.Denda7, "denda 7", false),
            (CollKind.TNominal7, "nominal 7", false),
            (CollKind.TTahun8, "tahun 8", false),
            (CollKind.Denda8, "denda 8", false),
            (CollKind.TNominal8, "nominal 8", false),
            (CollKind.TTahun9, "tahun 9", false),
            (CollKind.Denda9, "denda 9", false),
            (CollKind.TNominal9, "nominal 9", false),

            (CollKind.vBpnNotaris, "bpn/notaris", false),
            (CollKind.vTanggalDibuat, "validasi dibuat", false),
            (CollKind.vTanggalDikirim, "validasi dikirim", false),
            (CollKind.vTanggalSelesai, "validasi selesai", false),
            (CollKind.vKeterangan, "validasi keterangan", false),

            (CollKind.patok, "patok", false),
            (CollKind.satuan, "satuan", false),
            (CollKind.budget, "budget", false),
            (CollKind.jenisDokumen, "jenis dokumen hilang", false),
            (CollKind.jumlahAkta, "jumlah dokumen hilang", false),
            (CollKind.aktor, "notaris", false),

            (CollKind.tglTerimaInvoice, "tanggal terima invoice", false),
            (CollKind.tglTerimaPIC, "tanggal terima dari pic", false),
            (CollKind.tglDiserahkanPIC, "tanggal diserahkan pic", false),

            (CollKind.aNomorAkta, "nomor akta", false),
            (CollKind.aTanggalAkta, "tanggal akta", false),
            (CollKind.aBiayaPerBuku, "biaya per buku", false),
            (CollKind.aTotalHarga, "total harga", false)
        };

        abstract class ColInfo
        {
            public CollKind kind;
            public string[] captions;

            public ColInfo(CollKind kind, string caption)
            {
                this.kind = kind;
                this.captions = caption.Split('|');
            }

            public abstract (T[] data, string err) Get<T>(DataRow row);
            public abstract bool exists { get; }
        }

        class ColInfoS : ColInfo
        {
            public int number;

            public override bool exists => number != -1;

            public ColInfoS(CollKind kind, string caption)
                : base(kind, caption)
            {
                number = -1;
            }

            public override (T[] data, string err) Get<T>(DataRow row)
            {
                if (number == -1)
                    return (new T[0], null);
                try
                {
                    var obj = row[number];
                    return (obj == DBNull.Value ? new T[0] : new T[] { (T)obj }, null);
                }
                catch (Exception ex)
                {
                    return (new T[0], ex.Message);
                }
            }
        }

        class ColInfoM : ColInfo
        {
            public List<int> numbers = new List<int>();

            public override bool exists => numbers.Any();

            public ColInfoM(CollKind kind, string caption)
                : base(kind, caption)
            {
            }

            public override (T[] data, string err) Get<T>(DataRow row)
            {
                try
                {
                    var Ts = new T[numbers.Count];
                    Array.Fill(Ts, default);
                    var objs = numbers.Select((n, i) => (i, obj: row[n])).Where(x => x.obj != DBNull.Value).ToArray();
                    foreach (var x in objs)
                        Ts[x.i] = (T)x.obj;
                    return (Ts, null);
                }
                catch (Exception ex)
                {
                    return (new T[0], ex.Message);
                }
            }
        }

        class Failures
        {
            public string File { get; set; }
            public int Row { get; set; }
            public string RFP { get; set; }
            public string IdBidang { get; set; }
            public string Type { get; set; }
            public string Subtype { get; set; }
            public string Keterangan { get; set; }
        }
    }
}

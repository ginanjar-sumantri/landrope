#define test
using auth.mod;
using landrope.common;
using landrope.mod2;
using landrope.mod4;
using landrope.mod;
using landrope.material;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using landrope.mod4.classes;
using landrope.hosts;
using APIGrid;
using Tracer;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using mongospace;
using landrope.api3.Models;
using GraphConsumer;
using graph.mod;
using landrope.consumers;
using GenWorkflow;
using flow.common;
using landrope.mod3;
using System.Net;
using Microsoft.Extensions.Configuration;
using landrope.mod.cross;
using TriangleNet;
using DocumentFormat.OpenXml.Bibliography;
using System.Data;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using MailSender;
using MailSender.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using ClosedXML.Excel;
using System.Security.Policy;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using HttpAccessor;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace landrope.api3.Controllers
{
    [Route("api/bayar")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class BayarController : ControllerBase
    {
        IServiceProvider services;
        //GraphContext gcontext;
        //GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        GraphHostConsumer ghost;

        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextes = Contextual.GetContextExt();
        LandropePayContext contextpay = Contextual.GetContextPay();
        LandropePlusContext contextplus = Contextual.GetContextPlus();

        public BayarController(IServiceProvider services)
        {
            this.services = services;
            context = services.GetService<LandropeContext>();
            contextpay = services.GetService<LandropePayContext>();
            ghost = HostServicesHelper.GetGraphHostConsumer(services);
        }

        [NeedToken("PAY_LAND_REVIEW,PAY_LAND_FULL")]
        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var host = HostServicesHelper.GetBayarHost(services);
                var bayars = host.OpenedBayar().Cast<Bayar>().ToArray().AsParallel();
                var locations = contextpay.GetVillages().ToArray().AsParallel();
                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray().AsParallel();

                var keycreators = bayars.Select(x => x.keyCreator).Distinct();
                var creators = string.Join(',', keycreators.Select(k => $"'{k}'"));

                var users = contextpay.GetCollections(new user(), "securities", $"<key: <$in:[{creators}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();


                var datas = bayars.Where(x => x.details != null && x.bidangs != null).OrderByDescending(x => x.created).ToArray()
                    .Select(x =>
                    new BayarView
                    {
                        key = x.key,
                        nomorTahap = x.nomorTahap,
                        jmlBidang = x.bidangs?.Count() == null ? 0 : x.bidangs.Count(),
                        details = x.details.Select(x => new BayarDtlXView { key = x.key, jenisBayar = x.jenisBayar }).ToArray(),
                        created = x.created.ToLocalTime(),
                        group = x.group,
                        keyDesa = x.keyDesa,
                        keyProject = x.keyProject,
                        creator = users.FirstOrDefault(y => y.key == x.keyCreator)?.FullName,
                        //project = locations.FirstOrDefault(z => z.desa.key == x.keyDesa).project?.identity,
                        //desa = locations.FirstOrDefault(z => z.desa.key == x.keyDesa).desa?.identity,
                        ptsk = ptsks.FirstOrDefault(z => z.key == x.keyPTSK)?.name
                    }).ToArray().AsParallel();

                datas = datas.Join(locations, d => d.keyDesa, l => l.desa.key, (d, l) => d.SetLocation(l.project.identity, l.desa.identity)).AsParallel();

                var xlst = ExpressionFilter.Evaluate(datas, typeof(List<BayarView>), typeof(BayarView), gs);
                var data = xlst.result.Cast<BayarView>().ToArray().OrderByDescending(x => x.created);

                return Ok(data.GridFeed(gs));
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [NeedToken("PAY_LAND_REVIEW,PAY_LAND_FULL")]
        [HttpGet("list/bdg")] //List Bidang yang akan di Tambahkan ke detail bidang
        public IActionResult GetListBidang(string bkey, [FromQuery] AgGridSettings gs)
        {
            var qry = string.Empty;
            var host = HostServicesHelper.GetBayarHost(services);
            var byr = host.GetBayar(bkey) as Bayar;
            try
            {
                //if (byr.group == string.Empty)
                //    qry = $"<$match:< en_state: { (int)StatusBidang.belumbebas},'basic.current.keyProject' : '{byr.keyProject}','basic.current.keyDesa' : '{byr.keyDesa}'>>";
                //else
                //    qry = $"<$match:< en_state: { (int)StatusBidang.belumbebas},'basic.current.keyProject' : '{byr.keyProject}','basic.current.keyDesa' : '{byr.keyDesa}', 'basic.current.group' : '{byr.group}'>>";

                qry = $"<$match:< en_state: {(int)StatusBidang.belumbebas},'basic.current.keyProject' : '{byr.keyProject}','basic.current.keyDesa' : '{byr.keyDesa}'>>";

                var tm0 = DateTime.Now;
                var result = contextpay.GetDocuments(new GroupBidang(), "persils_v2",
                    $@"{qry}".Replace("<", "{").Replace(">", "}"),
                     @"{$lookup:{from: 'bayars',let: { key: '$key'},pipeline:[{$unwind: '$bidangs'},
                        {$match: {$expr: {$eq:['$bidangs.keyPersil','$$key']}}}],as:'matchbayar'}}",
                    "{$unwind: { path: '$matchbayar',preserveNullAndEmptyArrays: true} }",
                    "{$match: {$expr: {$eq:[{$ifNull:['$matchbayar',null]},null]} } }",
                    "{$match: {$expr:{$ne:['$deal', null]}}}",
                    @"{'$project' :
                            {
                            key: '$key',
                            _t : '$_t',
                            IdBidang: '$IdBidang',
                            en_state : '$en_state',
		                    deal : '$deal',
                            luasDibayar: '$basic.current.luasDibayar',
                            luasSurat: '$basic.current.luasSurat',
                            luasUkur: '$basic.current.luasGU',
                            luasFix:  {$ifNull:['$luasfix', null]},
                            Pemilik: '$basic.current.pemilik',
                            AlasHak: '$basic.current.surat.nomor',
		                    satuan: '$basic.current.satuan',
		                    total: '$basic.current.total',
                            noPeta: '$basic.current.noPeta',
                            keyPenampung: '$basic.current.keyPenampung',
                            keyPTSK: '$basic.current.keyPTSK',
                            keyParent: '$basic.current.keyParent',
                            _id: 0
                        }
                            }").Where(x => x.deal != null && string.IsNullOrEmpty(x.keyParent)).ToArray().AsParallel();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<GroupBidang>), typeof(GroupBidang), gs);
                var data = xlst.result.Cast<GroupBidang>().Select(a => a.toView2(contextpay, ptsks)).ToArray();

                return Ok(data.GridFeed(gs));
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }

        }

        [HttpGet("list/bdg/nogroup")]
        public IActionResult GetListBidangNoGroup(string pkey, string dkey, string? keyPenampung, string? keyPTSK, [FromQuery] AgGridSettings gs)
        {

            var host = HostServicesHelper.GetBayarHost(services);

            try
            {
                var tm0 = DateTime.Now;
                var result = contextpay.GetDocuments(new GroupBidang(), "persils_v2",
                    $@"<$match:<en_state: {(int)StatusBidang.belumbebas},
                    'basic.current.keyProject' : '{pkey}',
                    'basic.current.keyDesa' : '{dkey}'>
                            >".Replace("<", "{").Replace(">", "}"),
                     @"{$lookup:{from: 'bayars',let: { key: '$key'},pipeline:[{$unwind: '$bidangs'},
                        {$match: {$expr: {$eq:['$bidangs.keyPersil','$$key']}}}],as:'matchbayar'}}",
                    "{$unwind: { path: '$matchbayar',preserveNullAndEmptyArrays: true} }",
                    "{$match: {$expr: {$eq:[{$ifNull:['$matchbayar',null]},null]} } }",
                    "{$match: {$expr:{$ne:['$deal', null]}}}",
                    @"{'$project' :
                            {
                            key: '$key',
                            _t : '$_t',
                            IdBidang: '$IdBidang',
                            en_state : '$en_state',
		                    deal : '$deal',
                            luasDibayar: '$basic.current.luasDibayar',
                            luasSurat: '$basic.current.luasSurat',
                            luasUkur: '$basic.current.luasGU',
                            luasFix:  {$ifNull:['$luasfix', null]},
                            Pemilik: '$basic.current.pemilik',
                            AlasHak: '$basic.current.surat.nomor',
		                    satuan: '$basic.current.satuan',
		                    total: '$basic.current.total',
                            noPeta: '$basic.current.noPeta',
                            keyPenampung: '$basic.current.keyPenampung',
                            keyPTSK: '$basic.current.keyPTSK',
                            _id: 0
                        }
                            }").Where(x => x.deal != null).ToArray().AsParallel();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                           .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<GroupBidang>), typeof(GroupBidang), gs);
                var data = xlst.result.Cast<GroupBidang>().Select(a => a.toView2(contextpay, ptsks)).ToArray();

                return Ok(data.GridFeed(gs));

            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }

        }

        [NeedToken("PAY_LAND_REVIEW,PAY_LAND_FULL")]
        [HttpGet("list/dtl")]
        public IActionResult GetListDtl([FromQuery] string token, string bkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.details == null)
                    byr.details = new BayarDtl[0];

                var details = byr.details;
                var persilKeys = string.Join(',', details.Where(x => !string.IsNullOrEmpty(x.keyPersil)).Select(k => $"'{k.keyPersil}'"));
                var instkeys = string.Join(',', details.Select(k => $"'{k.instkey}'"));

                var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in :[{persilKeys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var instances = contextpay.GetDocuments(new { instkey = "", status = 0, createdDate = DateTime.Now }, "graphables",
                    $"<$match: <key: <$in:[{instkeys}]>>>".Replace("<", "{").Replace(">", "}"),
                    @" {$project: {
                         instkey : '$key',
                         status : '$lastState.state',
                         createdDate : {$arrayElemAt: ['$states.time',0]},
                        _id:0
                    }}").ToList();

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();
                var xlst = ExpressionFilter.Evaluate(byr.details, typeof(List<BayarDtl>), typeof(BayarDtl), gs);
                var data = xlst.result.Cast<BayarDtl>().Select(a => a.toView(contextpay, byr, staticCollections, persils, instances.Select(x => (x.instkey, (ToDoState)x.status, x.createdDate)).ToArray())).ToArray().AsParallel();

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

        [NeedToken("PAY_LAND_REVIEW,PAY_LAND_FULL")]
        [HttpGet("list/dtl-deposit")]
        public IActionResult GetListDtlDeposit([FromQuery] string token, string bkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.deposits == null)
                    byr.deposits = new BayarDtlDeposit[0];

                var deposits = byr.deposits;
                var persilKeys = string.Join(',', deposits.Where(x => !string.IsNullOrEmpty(x.keyPersil)).Select(k => $"'{k.keyPersil}'"));
                var instkeys = string.Join(',', deposits.Select(k => $"'{k.instkey}'"));

                var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in :[{persilKeys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var instances = contextpay.GetDocuments(new { instkey = "", status = 0, createdDate = DateTime.Now }, "graphables",
                    $"<$match: <key: <$in:[{instkeys}]>>>".Replace("<", "{").Replace(">", "}"),
                    @" {$project: {
                         instkey : '$key',
                         status : '$lastState.state',
                         createdDate : {$arrayElemAt: ['$states.time',0]},
                        _id:0
                    }}").ToList();

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();
                var xlst = ExpressionFilter.Evaluate(byr.deposits, typeof(List<BayarDtlDeposit>), typeof(BayarDtlDeposit), gs);
                var data = xlst.result.Cast<BayarDtlDeposit>().Select(a => a.toView(contextpay, byr, staticCollections, persils, instances.Select(x => (x.instkey, (ToDoState)x.status, x.createdDate)).ToArray())).ToArray().AsParallel();

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

        [HttpGet("list/dtl-app")]
        public IActionResult GetListDtlApprovegrp([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var privs = user.privileges.Select(a => a.identifier).ToArray();
                var host = HostServicesHelper.GetBayarHost(services);
                var res = new List<MasterPay>();
                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var creator = priv.Contains("PAY_LAND_FULL");
                var monitor = priv.Contains("PAY_LAND_VIEW") && !creator;
                var review = priv.Contains("PAY_LAND_REVIEW");
                var accounting = priv.Contains("PAY_ACCTG");
                var leader = priv.Contains("PAY_CASHIER_LEADER");
                var cashier = priv.Contains("PAY_CASHIER");
                var pratrans = priv.Contains("PAY_PRA_TRANS");

#if test
                var Gs1 = ghost.List(user).GetAwaiter().GetResult() ?? new GraphTree[0];
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
                var Gs1 = graphhost.List(user).ToArray() ?? new GraphTree[0];
#endif
                var Gs = Gs1.Where(x => x.subs.Any()).Select(x => (x.main, inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes)).ToArray();

                var instkeys = Gs.Select(g => g.main.key).ToArray();

                var bayars = host.OpenedBayar(instkeys).Cast<Bayar>().ToArray().AsParallel();
                var details = bayars.SelectMany(x => x.details.Where(x => instkeys.Contains(x.instkey)), (y, z) => new MasterPay
                {
                    Tkey = y.key,
                    noTahap = y.nomorTahap,
                    key = z.key,
                    keyPersil = z.keyPersil,
                    keyProject = y.keyProject,
                    keyDesa = y.keyDesa,
                    keyPTSK = y.keyPTSK,
                    group = y.group,
                    instkey = z.instkey,
                    jenisBayar = z.jenisBayar,
                    noMemo = z.noMemo,
                    Jml = z.Jumlah,
                    tgl = z.tglBayar,
                    contactPerson = z.contactPerson,
                    noTlpCP = z.noTlpCP,
                    tembusan = z.tembusan,
                    memoSigns = z.memoSigns,
                    note = z.note,
                    giro = z.giro,
                    reasons = z.reasons
                }).Where(x => x.tgl == null).AsParallel().ToArray();

                var deposits = bayars.SelectMany(x => x.deposits.Where(x => instkeys.Contains(x.instkey)), (y, z) => new MasterPay
                {
                    Tkey = y.key,
                    noTahap = y.nomorTahap,
                    key = z.key,
                    keyPersil = z.keyPersil,
                    keyProject = y.keyProject,
                    keyDesa = y.keyDesa,
                    keyPTSK = y.keyPTSK,
                    group = y.group,
                    instkey = z.instkey,
                    jenisBayar = z.jenisBayar,
                    noMemo = z.noMemo,
                    Jml = z.Jumlah,
                    tgl = z.tglBayar,
                    contactPerson = z.contactPerson,
                    noTlpCP = z.noTlpCP,
                    tembusan = z.tembusan,
                    memoSigns = z.memoSigns,
                    note = z.note,
                    giro = z.giro,
                    reasons = z.reasons
                }).Where(x => x.tgl == null).AsParallel().ToArray();

                var result = details.Union(deposits).ToList();

                var persilKeys = string.Join(',', result.Where(x => !string.IsNullOrEmpty(x.keyPersil)).Select(k => $"'{k.keyPersil}'"));

                var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in :[{persilKeys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();
                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var locations = contextpay.GetVillages();

                var view = result.Cast<MasterPay>().Select(a => a.toView(contextpay, staticCollections, ptsks, persils)).ToArray();
                var xlst = ExpressionFilter.Evaluate(view, typeof(List<BayarDtlView>), typeof(BayarDtlView), gs);
                var filter = xlst.result.Cast<BayarDtlView>().ToList();
                var data = filter.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToArray();

                var nxdata = data.Join(Gs, a => a.instkey, g => g.main?.key,
                                        (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault())).ToArray().AsParallel();

                var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes)).ToArray().AsParallel();
                var ndatax = ndata.Select(x => (x, y: BayarDtlViewExt.Upgrade(x.a))).ToArray().AsParallel();

                var data2 = ndatax.Where(X => X.y != null).Select(X => X.y?
                 .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.Title(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                 .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)
                 .SetStatus(X.x.i?.lastState?.state.AsStatus(), X.x.i?.lastState?.time)
                 .SetCreator(user.FullName == X.x.a.creator)
                 .SetMilestones(X.x.i?.states.LastOrDefault(s => s.state == ToDoState.created_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.issued_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.submissionSubmitted_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.reviewApproved_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.accountingApproved_)?.time,
                                                 X.x.i?.states.LastOrDefault(s => s.state == ToDoState.finalApproved_)?.time)
                ).ToArray();


                if (cashier && data2.Select(x => x.state).ToList().Contains(ToDoState.cashierApproved_) && !leader)
                {
                    var dt = data2.ToList();
                    dt.RemoveAll(x => x.state == ToDoState.cashierApproved_ && x.jenisBayar != JenisBayar.UTJ);
                    data2 = dt.AsParallel().ToArray();
                }
                else if (pratrans && data2.Select(x => x.state).ToList().Contains(ToDoState.cashierApproved_))
                {
                    data2 = data2.Where(x => x.jenisBayar != JenisBayar.UTJ).AsParallel().ToArray();
                }

                var sorted = data2.GridFeed(gs, tm0, new Dictionary<string, object> { { "role", creator ? 1 : monitor ? 3 : accounting ? 4 : cashier ? 5 : 0 } });
                return Ok(sorted);

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

        [HttpGet("list/dtl/bdg")]
        public IActionResult GetListDtlBidang([FromQuery] string token, string bkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(bkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                var deposits = byr.deposits ?? new BayarDtlDeposit[0];
                var subsDeposit = deposits.SelectMany(x => x.subdetails).ToArray();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(byr.bidangs, typeof(List<BayarDtlBidang>), typeof(BayarDtlBidang), gs);
                var data = xlst.result.Cast<BayarDtlBidang>().Select(a => a.toView(contextpay, byr, subsDeposit, ptsks)).ToArray();

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

        [HttpGet("list/dtl/all")]
        public IActionResult GetListDtlAll([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var result = contextpay.GetDocuments(new MasterPay(), "bayars",
                    "{$unwind: { path: '$details'} }",
                    @"{$match:
                    {
                        'details.tglBayar' : null
                      }

                }",
                @"{'$project':
                        {
                            Tkey: '$key',
                            noTahap : '$nomorTahap',
                            key: '$details.key',
                            keyPersil: '$details.keyPersil',
                            keyDesa: '$keyDesa',
                            group: '$group',
                            instkey: '$details.instkey',
                            jenisBayar: '$details.jenisBayar',
                            noMemo : '$details.noMemo',
                            Jml: '$details.Jumlah',
                            tgl: '$details.tglBayar',
                            _id: 0
                        }
                    }").ToArray().AsParallel();

                var persilKeys = string.Join(',', result.Where(x => !string.IsNullOrEmpty(x.keyPersil)).Select(k => $"'{k.keyPersil}'"));
                var instkeys = string.Join(',', result.Select(k => $"'{k.instkey}'"));

                var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in :[{persilKeys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var instances = contextpay.GetDocuments(new { instkey = "", status = 0, createdDate = DateTime.Now }, "graphables",
                   $"<$match: <key: <$in:[{instkeys}]>>>".Replace("<", "{").Replace(">", "}"),
                   @" {$project: {
                         instkey : '$key',
                         status : '$lastState.state',
                         createdDate : {$arrayElemAt: ['$states.time',0]},
                        _id:0
                    }}").ToArray();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<MasterPay>), typeof(MasterPay), gs);
                var data = xlst.result.Cast<MasterPay>().Select(a => a.toViewExt(contextpay, persils, instances.Select(x => (x.instkey, (ToDoState)x.status, x.createdDate)).ToArray())).ToArray();

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

        [HttpGet("list/pay/bdg")] //List Bidang yang akan di tambahkan ke detail pembayaran
        public IActionResult ListBidangForPay([FromQuery] string token, string bkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                var details = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true).ToList();
                var subdetails = details.SelectMany(x => x.subdetails).ToArray();
                var bidangs = byr.bidangs.ToList();
                var qry =
                            (from c in bidangs
                             where !(from o in subdetails
                                     select o.keyPersil)
                                    .Contains(c.keyPersil)
                             select c).ToArray().AsParallel();

                var datas = qry.Select(a => a.toViewPay(contextpay, byr)).ToArray();

                var xlst = ExpressionFilter.Evaluate(datas, typeof(List<BayarDtlBidangView>), typeof(BayarDtlBidangView), gs);
                var data = xlst.result.Cast<BayarDtlBidangView>().ToArray();

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

        [HttpGet("list/deposit/bdg")] //List detail bidang di satu deposit
        public IActionResult GetListBidangDeposit([FromQuery] string token, string bkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(bkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                var deposit = byr.deposits.Where(x => x.invalid != true);
                var subdetails = deposit.SelectMany(x => x.subdetails).ToArray();
                var bidangs = byr.bidangs.ToArray();

                var locations = contextpay.GetVillages().ToArray();

                var sumSubs = subdetails.Select(x => new
                {
                    keyPersil = x.keyPersil,
                    deposit = (x.vBiayaBN ?? 0) + (x.vGantiBlanko ?? 0) + (x.vKompensasi ?? 0) + (x.vMandor ?? 0) + (x.vPajakLama ?? 0) + (x.vPajakWaris ?? 0) + (x.vPembatalanNIB ?? 0)
                    + (x.vpph21 ?? 0) + (x.vTunggakanPBB ?? 0) + (x.vValidasiPPH ?? 0) + (x.biayalainnya.Count() > 0 ? x.biayalainnya.Sum(b => b.nilai) : 0)
                }).ToArray();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(bidangs, typeof(List<BayarDtlBidang>), typeof(BayarDtlBidang), gs);
                var data = xlst.result.Cast<BayarDtlBidang>().Select(a => a.toView(contextpay, byr, subdetails, locations, ptsks)).ToArray();

                //cari bidang yang sudah habis depositnya
                var datax = data.Select(x => new
                {
                    keyPersil = x.keyPersil,
                    deposit = (x.BalikNama ?? 0) + (x.GantiBlanko ?? 0) + (x.Kompensasi ?? 0) + (x.Mandor ?? 0) + (x.PajakLama ?? 0) + (x.PajakWaris ?? 0) + (x.PembatalanNIB ?? 0)
                    + (x.TotalPPH21 ?? 0) + (x.TunggakanPBB ?? 0) + (x.TotalValidasiPPH21 ?? 0) + (x.biayalainnyas.Count() > 0 ? x.biayalainnyas.Sum(b => b.nilai) : 0)
                }).Where(x => x.deposit == 0).Select(x => x.keyPersil).ToArray();

                data = data.Where(x => !datax.Contains(x.keyPersil)).ToArray();

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

        [HttpGet("list/view/bdg")] //List detail bidang di satu pembayaran
        public IActionResult GetListBidangView([FromQuery] string token, string bkey, string pkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(bkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                var subdetails = byr.details.Where(x => x.key == pkey).SelectMany(x => x.subdetails).ToArray();
                var bidangs = byr.bidangs.ToArray();

                var locations = contextpay.GetVillages().ToArray();

                var qry =
                        (from c in bidangs
                         where (from o in subdetails
                                select o.keyPersil)
                                .Contains(c.keyPersil)
                         select c).ToArray().AsParallel();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(qry, typeof(List<BayarDtlBidang>), typeof(BayarDtlBidang), gs);
                var data = xlst.result.Cast<BayarDtlBidang>().Select(a => a.toView(contextpay, byr, locations, ptsks)).ToArray();

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

        [HttpGet("list/view/bdg-deposit")]
        public IActionResult GetListBidangDepositView([FromQuery] string token, string bkey, string pkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(bkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.deposits == null)
                    byr.deposits = new BayarDtlDeposit[0];

                var subdetails = byr.deposits.Where(x => x.key == pkey).SelectMany(x => x.subdetails).ToArray();
                var bidangs = byr.bidangs.ToArray();

                var locations = contextpay.GetVillages().ToArray();

                var qry =
                        (from c in bidangs
                         where (from o in subdetails
                                select o.keyPersil)
                                .Contains(c.keyPersil)
                         select c).ToArray().AsParallel();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var xlst = ExpressionFilter.Evaluate(qry, typeof(List<BayarDtlBidang>), typeof(BayarDtlBidang), gs);
                var data = xlst.result.Cast<BayarDtlBidang>().Select(a => a.toViewDeposit(contextpay, byr, locations, ptsks, subdetails)).ToArray();

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


        //[HttpGet("list/januari")]
        //public IActionResult GetBidangInJanuari()
        //{
        //    try
        //    {
        //        var host = HostServicesHelper.GetBayarHost(services);
        //        var bayars = host.OpenedBayar().Cast<Bayar>().ToArray().AsParallel();

        //        var dtls = bayars.SelectMany(x => x.details);
        //        var filter = dtls.Where(x => x.instance != null).ToArray();
        //        var filter2 = filter.
        //        var cashApp = filter.Where(x => x.instance.states.Any(x => x.state == ToDoState.cashierApproved_)).ToArray();
        //        var cashAppJan = cashApp.SelectMany(x => x.instance.states, (y, z) => new { dtl = y, grp = z })
        //            .Where(x => x.grp.time.Month == 1).Select(x => x.dtl).ToArray();

        //        var persils = cashAppJan.SelectMany(x => x.subdetails).Select(x => x.keyPersil).Distinct();

        //        return new JsonResult(persils.ToArray());
        //    }
        //    catch (UnauthorizedAccessException exa)
        //    {
        //        return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //    }
        //    catch (Exception ex)
        //    {
        //        MyTracer.TraceError2(ex);
        //        return new UnprocessableEntityObjectResult(ex.Message);
        //    }
        //}


        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("save")]
        [Consumes("application/json")]
        public IActionResult Save([FromQuery] string token, [FromBody] BayarCore byr)
        {
            try
            {
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var ent = new Bayar(user);

                ent.FromCore(byr);
                ent.key = entity.MakeKey;
                byr.key = ent.key;

                if (byr.bidangs.Count() > 0)
                {
                    foreach (var k in byr.bidangs)
                    {
                        if (k.keyPersil != null && k.keyPersil != string.Empty)
                        {
                            var byrdtl = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = k.keyPersil };
                            ent.AddDetailBidang(byrdtl);
                        }
                    }
                }

                int num = 0;
                int lastnumber = 0;

                var main = contextpay.mainprojects.FirstOrDefault(x => x.en_proses == JenisProses.standar && x.projects.Contains(byr.keyProject));

                lastnumber = main.nomorTahap;

                if (lastnumber > 0)
                {
                    num = lastnumber;
                }

                num++;
                ent.nomorTahap = num;

                host.Add(ent);

                main.nomorTahap = num;
                contextpay.mainprojects.Update(main);
                contextpay.SaveChanges();

                var pik6 = new string[] { "PROJECT006T", "5E76F53E20C4177BBC72251E", "PROJECT006L" };

                if (pik6.Contains(byr.keyProject))
                {
                    var config = (IConfigurationRoot)HttpAccessor.Config.configuration;
                    var contextcross = new LandropeCrossContext(config);

                    var cross = contextcross.mainprojects.FirstOrDefault(x => x.en_proses == JenisProses.standar && x.projects.Contains(byr.keyProject));

                    cross.nomorTahap = num;
                    contextcross.mainprojects.Update(cross);
                    contextcross.SaveChanges();
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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("edit-ptsk")]
        public IActionResult EditPTSK([FromQuery] string token, string key, string keyptsk)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(key) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (string.IsNullOrEmpty(keyptsk))
                    return new UnprocessableEntityObjectResult("PTSK belum dipilih");

                byr.keyPTSK = keyptsk;

                host.Update(byr);

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

        [NeedToken("PAY_LAND_FULL")]
        [HttpGet("get-ptsk")]
        public IActionResult GetPTSK([FromQuery] string token, string key)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var view = new BayarView();

                var byr = host.GetBayar(key) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var location = contextpay.GetVillages().FirstOrDefault(x => x.desa.key == byr.keyDesa);

                var penampung = contextpay.companies.Query(x => x.invalid != true).ToList()
                                           .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray().AsParallel();

                var bidangs = byr.bidangs.Select(x => x.keyPersil).ToArray();
                if (bidangs.Count() == 0)
                {

                    view = new BayarView
                    {
                        key = byr.key,
                        nomorTahap = byr.nomorTahap,
                        project = location.project.identity,
                        keyProject = byr.keyProject,
                        desa = location.desa.identity,
                        keyDesa = byr.keyDesa,
                        keyPenampung = penampung.FirstOrDefault(x => x.key == byr.keyPenampung)?.name,
                        keyPTSK = "",
                        group = byr.group
                    };

                    return new JsonResult(view);
                }

                var persilKeys = string.Join(',', bidangs.Select(k => $"'{k}'"));
                var keyPTSKs = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in :[{persilKeys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                    .Select(x => x.basic.current.keyPTSK).Distinct();

                if (keyPTSKs.Count() > 1)
                    return new UnprocessableEntityObjectResult("Bidang-bidang dalam tahap belum satu PTSK");

                view = new BayarView
                {
                    key = byr.key,
                    nomorTahap = byr.nomorTahap,
                    project = location.project.identity,
                    keyProject = byr.keyProject,
                    desa = location.desa.identity,
                    keyDesa = byr.keyDesa,
                    keyPenampung = penampung.FirstOrDefault(x => x.key == byr.keyPenampung)?.name,
                    keyPTSK = keyPTSKs.FirstOrDefault(),
                    group = byr.group
                };

                return new JsonResult(view);

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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/save")]
        public IActionResult SaveDetail([FromQuery] string token, string bkey, [FromBody] BayarDtlCore core, [FromQuery] string opr)
        {

            var predicate = opr switch
            {
                "add" => "menambah",
                _ => "memperbarui"
            } + " Detail Tahap";

            try
            {
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
#if test
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
#endif
                var byr = host.GetBayar(bkey) as Bayar;
                var byrdtl = new BayarDtl();

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                //Check to PraDeals
                var keys = !string.IsNullOrEmpty(core.keyPersil) ? string.Join(',', core.keyPersil.Split(',').Select(k => $"'{k}'")) : string.Join(',', byr.bidangs.Select(k => $"'{k.keyPersil}'"));
                (bool ok, string error) = PraDealValidation(keys, core.jenisBayar);
                if (!ok)
                    return new UnprocessableEntityObjectResult(error);

                var dealstatus = new deals { deal = DateTime.Now, dealer = user.key, status = StatusDeal.Deal1A };
                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();


                var res = opr switch
                {
                    "add" => Add(),
                    _ => Edit(),
                };

                return res;

                IActionResult Add()
                {
                    if (core.jenisBayar == JenisBayar.UTJ)
                    {
                        if (Math.Round(core.Jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                            return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                        if (byr.details != null)
                        {
                            if (byr.details.Any(d => d.jenisBayar == core.jenisBayar && d.invalid != true))
                                return new UnprocessableEntityObjectResult("UTJ Sudah ada");
                            else if (byr.details.Any(d => (d.jenisBayar == JenisBayar.DP && d.invalid != true) || (d.jenisBayar == JenisBayar.Lunas && d.invalid != true)))
                                return new UnprocessableEntityObjectResult("UTJ tidak bisa dibuat");
                        }

                        if (byr.bidangs.Count() > 0)
                        {
                            byrdtl.fromCore(core);
                            byrdtl.fillStaticInfo(staticCollections);
                            var allLuas = byr.AllLuas(contextpay);

                            var keys = string.Join(',', byr.bidangs.Select(k => $"'{k.keyPersil}'"));
                            var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in : [{keys}]>>".MongoJs(), "{_id:0}").ToList();

                            foreach (var persil in persils)
                            {
                                (double? luasdiBayar, double? luas) = (0, 0);
                                var luasNIB = PersilHelper.GetLuasBayar(persil);

                                luas = persil.basic.current.luasDibayar == null ? 0 : persil.basic.current.luasDibayar;
                                luasdiBayar = (persil.luasFix ?? true) ? luas : (luasNIB == 0 ? luas : luasNIB);

                                var proposional = core.fgProposional == ProposionalBayar.Luas ? ProposionalLuas((luasdiBayar ?? 0), core.Jumlah, allLuas) : ProposionalRata(core.Jumlah, byr.bidangs.Count());

                                var byrSubDtl = new BayarSubDtl();
                                byrSubDtl.fromCore(persil.key, proposional);
                                byrdtl.AddSubDetail(byrSubDtl);
                            }

                            foreach (var core in core.pemecahanGiro)
                            {
                                var giro = new Giro();
                                giro.fromCore(core);
                                byrdtl.AddGiro(giro);
                            }

                            byrdtl.SelisihPembulatan();
#if test
                            byrdtl.CreateGraphInstance(user, core.jenisBayar);
#else
                            byrdtl.CreateGraphInstance(user, core.jenisBayar, graphhost);
#endif
                            byr.AddDetail(byrdtl);
                        }
                        else
                        {
                            byrdtl.fromCore(core);
                            byrdtl.fillStaticInfo(staticCollections);
                            foreach (var core in core.pemecahanGiro)
                            {
                                var giro = new Giro();
                                giro.fromCore(core);
                                byrdtl.AddGiro(giro);
                            }

                            byrdtl.SelisihPembulatan();
#if test
                            byrdtl.CreateGraphInstance(user, core.jenisBayar);
#else
                            byrdtl.CreateGraphInstance(user, core.jenisBayar, graphhost);
#endif
                            byr.AddDetail(byrdtl);
                        }
                    }
                    else if (core.jenisBayar == JenisBayar.DP)
                    {
                        if (byr.bidangs.Count() <= 0)
                            return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                        byrdtl.fromCore(core);
                        byrdtl.fillStaticInfo(staticCollections);

                        foreach (var core in core.pemecahanGiro)
                        {
                            var giro = new Giro();
                            giro.fromCore(core);
                            byrdtl.AddGiro(giro);
                        }

                        var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in : [{keys}]>>".MongoJs(), "{_id:0}").ToList();
                        var DPSettings = contextpay.GetDocuments(new DPSettings(), "static_collections",
                            "{$match: {_t : 'settingDP'}}",
                            "{$unwind: '$details'}",
                            "{$project:{ enJenis : '$details.en_jenis', percentage : '$details.percent', _id : 0}}").ToList();

                        foreach (var persil in persils)
                        {
                            var exists = byr.IsLunas(persil.key);
                            var DPSetting = DPSettings.FirstOrDefault(x => x.enJenis == persil.basic.current.en_jenis);

                            if (exists)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} sudah lunas");

                            (double jumlah, double luasdiBayar, double luas, double luasNIB, double satuan) = (0, 0, 0, 0, 0);

                            luasNIB = PersilHelper.GetLuasBayar(persil);

                            luas = persil.basic.current.luasDibayar ?? 0;
                            satuan = persil.basic.current.satuan ?? 0;
                            luasdiBayar = (persil.luasFix ?? true) ? luas : (luasNIB == 0 ? luas : luasNIB);

                            var total = (luasdiBayar * persil.basic.current.satuan);
                            var allHarga = byr.AllHarga(contextpay, persils.ToArray());

                            if (core.Jumlah == 0)
                                jumlah = (allHarga * core.Persentase ?? 0) / 100;
                            else
                                jumlah = core.Jumlah;

                            var proposional = ProposionaHargaTotal(Convert.ToDouble(total), jumlah, allHarga);

                            var percentage = (proposional / total) * 100;
                            var earlyPay = persil.earlyPay == null ? false : persil.earlyPay;

                            if (earlyPay != true)
                            {
                                if (percentage > DPSetting.percentage)
                                {
                                    return new UnprocessableEntityObjectResult($"Bidang {persil.IdBidang}, DP tidak boleh lebih besar dari {DPSetting.percentage}%");
                                }
                            }

                            byrdtl.Jumlah = jumlah;

                            if (Math.Round(jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                                return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                            var byrSubDtl = new BayarSubDtl();
                            byrSubDtl.fromCore(persil.key, proposional);

                            byrdtl.AddSubDetail(byrSubDtl);
                        }

                        byrdtl.SelisihPembulatan();
#if test
                        byrdtl.CreateGraphInstance(user, core.jenisBayar);
#else
                            byrdtl.CreateGraphInstance(user, core.jenisBayar, graphhost);
#endif
                        byr.AddDetail(byrdtl);
                    }
                    else if (core.jenisBayar == JenisBayar.Lunas)
                    {
                        if (Math.Round(core.Jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                            return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                        if (byr.bidangs.Count() <= 0)
                            return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                        byrdtl.fromCore(core);
                        byrdtl.fillStaticInfo(staticCollections);
                        foreach (var giro in core.pemecahanGiro)
                        {
                            var dtlGiro = new Giro();
                            dtlGiro.fromCore(giro);
                            byrdtl.AddGiro(dtlGiro);
                        }

                        var keys = string.Empty;
                        if (!string.IsNullOrEmpty(core.keyPersil))
                        {
                            keys = string.Join(',', core.keyPersil.Split(',').Select(k => $"'{k}'"));
                        }
                        else
                        {
                            keys = string.Join(',', byr.bidangs.Select(k => $"'{k.keyPersil}'"));
                        }

                        var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key : <$in : [{keys}]>>".MongoJs(), "{_id:0}").ToList();

                        foreach (var persil in persils)
                        {
                            if (persil == null)
                                return new UnprocessableEntityObjectResult("Bidang tidak ditemukan");

                            var exists = byr.IsLunas(persil.key);

                            if (exists)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} sudah lunas");

                            if (persil.luasFix != true)
                            {
                                var luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
                                if (luasNIB != 0 && luasNIB != null)
                                {
                                    //persil.luasPelunasan = luasNIB;

                                    var pelunasan = new pelunasan();
                                    pelunasan.keyCreator = user.key;
                                    pelunasan.luasPelunasan = Convert.ToDouble(luasNIB);
                                    pelunasan.created = DateTime.Now;
                                    pelunasan.reason = "Dari Pembayaran";

                                    var lst = new List<pelunasan>();
                                    if (persil.paidHistories != null)
                                        lst = persil.paidHistories.ToList();

                                    lst.Add(pelunasan);

                                    persil.paidHistories = lst.ToArray();

                                    //For Entries
                                    var last = persil.basic.entries.LastOrDefault();

                                    if (last.reviewed == null)
                                    {
                                        last.item.luasDibayar = luasNIB;
                                        persil.basic.current = last.item;
                                        contextpay.persils.Update(persil);
                                    }
                                    else
                                    {
                                        var item = new PersilBasic();
                                        item = persil.basic.current;
                                        item.luasDibayar = luasNIB;
                                        if (!string.IsNullOrEmpty(last.item.reason))
                                            item.reason = last.item.reason;

                                        persil.basic.current = item;
                                        contextpay.persils.Update(persil);
                                    }
                                }
                                else
                                {
                                    return new UnprocessableEntityObjectResult($"NIB Perorangan IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} belum diinput");
                                }
                            }
                            else
                            {
                                //persil.luasPelunasan = persil.basic.current.luasDibayar;

                                var pelunasan = new pelunasan();
                                pelunasan.keyCreator = user.key;
                                pelunasan.luasPelunasan = Convert.ToDouble(persil.basic.current.luasDibayar);
                                pelunasan.created = DateTime.Now;
                                pelunasan.reason = "Dari Pembayaran";

                                var lst = new List<pelunasan>();
                                if (persil.paidHistories != null)
                                    lst = persil.paidHistories.ToList();

                                lst.Add(pelunasan);

                                persil.paidHistories = lst.ToArray();

                                contextpay.persils.Update(persil);
                            }

                            (var sisapelunasan, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);

                            var byrSubDtl = new BayarSubDtl();
                            byrSubDtl.fromCore(persil.key, sisapelunasan);
                            byrdtl.AddSubDetail(byrSubDtl);
                        }

                        #region "Check Persil is Close or Not"
                        // asharhe 2021-12-09

                        //var findBayar = contextpay.bayars.FirstOrDefault(x => x.key == bkey);
                        //bool dtl = findBayar.details
                        //            .SelectMany(x => x.subdetails, (detail, subDetail) => new { detail, subDetail })
                        //            .Where(x => persils.Contains(x.subDetail.keyPersil))
                        //            .Select(x => x.detail.instance)
                        //            .Any(x => x.closed == false);

                        //if(dtl) // di comment sementara 29-12-2021
                        //    return new UnprocessableEntityObjectResult("Proses pembayaran satu bidang atau lebih belum selesai, Harap selesaikan terlebih dahulu.");

                        #endregion

#if test
                        byrdtl.CreateGraphInstance(user, core.jenisBayar);
#else
                        byrdtl.CreateGraphInstance(user, core.jenisBayar, graphhost);
#endif
                        byr.AddDetail(byrdtl);

                    }
                    else if (core.jenisBayar == JenisBayar.Lainnya)
                    {
                        return new UnprocessableEntityObjectResult("Jenis Pembayaran ini sudah tidak bisa digunakan, silahkan memilih jenis pembayaran yang lain!");
                    }
                    else if (core.jenisBayar == JenisBayar.Mandor)
                    {
                        return new UnprocessableEntityObjectResult("Jenis Pembayaran ini sudah tidak bisa digunakan, silahkan memilih jenis pembayaran yang lain!");
                    }

                    contextpay.SaveChanges(); //For Save contextpay.persils.Update(persil);
                    host.Update(byr);

                    DealStatusUpdate(byr, byrdtl, StatusDeal.Deal1A, user);
                    return Ok();
                }


                IActionResult Edit()
                {
                    var old = byr.details.FirstOrDefault(a => a.key == core.key);
                    if (old == null)
                        return new UnprocessableEntityObjectResult("Detil penugasan dimaksud tidak ditemukan");
                    if (JenisBayar.UTJ == core.jenisBayar)
                    {
                        if (byr.details != null)
                        {
                            if (byr.details.Any(d => d.jenisBayar == core.jenisBayar))
                                return new UnprocessableEntityObjectResult("UTJ Sudah ada");
                        }
                    }
                    old.fromCore(core);
                    old.fillStaticInfo(staticCollections);

                    contextpay.bayars.Update(byr);
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
                return new UnprocessableEntityObjectResult($"Gagal: {predicate}, harap beritahu support");
            }

        }

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/save-deposit")]
        public IActionResult SaveDeposit([FromQuery] string token, string bkey, [FromBody] BayarDtlDepositCore core)
        {
            try
            {
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
#if test
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
#endif
                var byr = host.GetBayar(bkey) as Bayar;
                var byrDeposit = new BayarDtlDeposit();

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                string[] keys;

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                if (Math.Round(core.Jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                    return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                if (byr.bidangs.Count() <= 0)
                    return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                byrDeposit.fromCore(core);
                byrDeposit.fillStaticInfo(staticCollections);
                foreach (var giro in core.pemecahanGiro)
                {
                    var dtlGiro = new Giro();
                    dtlGiro.fromCore(giro);
                    byrDeposit.AddGiro(dtlGiro);
                }

                foreach (var sub in core.subdetails)
                {
                    var persil = GetPersil(sub.keyPersil);

                    if (persil == null)
                        return new UnprocessableEntityObjectResult("Bidang tidak ditemukan");

                    var exists = byr.IsLunas(persil.key);

                    if (!exists)
                        return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} belum ada pelunasan");

                    var deposit = byr.Deposit(persil);

                    if (deposit == 0)
                        return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} tidak memiliki deposit");

                    var byrSubDtl = new BayarSubDtlDeposit();
                    byrSubDtl.fromCore(sub);
                    byrDeposit.AddSubDetail(byrSubDtl);
                }

#if test
                byrDeposit.CreateGraphInstance(user, core.jenisBayar);
#else
                byrDeposit.CreateGraphInstance(user, core.jenisBayar, graphhost);
#endif
                byr.AddDeposit(byrDeposit);

                contextpay.SaveChanges();
                host.Update(byr);

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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/save/lunas")] // Satu tahap langsung lunas
        public IActionResult DirectPaidOff([FromQuery] string token, string bkey, [FromBody] BayarDtlCore core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
#if test
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
#endif
                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (byr.bidangs.Count() <= 0)
                    return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                var persils = byr.bidangs.Select(x => x.keyPersil).ToArray();
                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                var byrdtl = new BayarDtl();
                byrdtl.fromCore(core);
                byrdtl.fillStaticInfo(staticCollections);

                foreach (var giro in core.pemecahanGiro)
                {
                    var dtlGiro = new Giro();
                    dtlGiro.fromCore(giro);
                    byrdtl.AddGiro(dtlGiro);
                }

                foreach (var key in persils)
                {
                    var persil = GetPersil(key);

                    if (persil == null)
                        return new UnprocessableEntityObjectResult("Bidang tidak ditemukan");

                    var exists = byr.IsLunas(persil.key);

                    if (exists)
                        return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} sudah lunas");

                    if (persil.luasFix != true)
                    {
                        //var luasNIB = PersilHelper.GetLuasBayar(persil);

                        var luasNIB = persil.basic.current.luasNIBTemp;

                        if (luasNIB != 0)
                        {
                            //persil.luasPelunasan = luasNIB;

                            var pelunasan = new pelunasan();
                            pelunasan.keyCreator = user.key;
                            pelunasan.luasPelunasan = Convert.ToDouble(luasNIB);
                            pelunasan.created = DateTime.Now;
                            pelunasan.reason = "Dari Pembayaran";

                            var lst = new List<pelunasan>();
                            if (persil.paidHistories != null)
                                lst = persil.paidHistories.ToList();

                            lst.Add(pelunasan);

                            persil.paidHistories = lst.ToArray();

                            //For Entries
                            var last = persil.basic.entries.LastOrDefault();

                            if (last.reviewed == null)
                            {
                                last.item.luasDibayar = luasNIB;
                                persil.basic.current = last.item;
                                contextes.persils.Update(persil);
                            }
                            else
                            {
                                var item = new PersilBasic();
                                item = persil.basic.current;
                                item.luasDibayar = luasNIB;
                                if (!string.IsNullOrEmpty(last.item.reason))
                                    item.reason = last.item.reason;

                                persil.basic.current = item;
                                contextes.persils.Update(persil);
                            }
                        }
                        else
                        {
                            return new UnprocessableEntityObjectResult($"NIB Perorangan IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} belum diinput");
                        }
                    }
                    else
                    {
                        //persil.luasPelunasan = persil.basic.current.luasDibayar;

                        var pelunasan = new pelunasan();
                        pelunasan.keyCreator = user.key;
                        pelunasan.luasPelunasan = Convert.ToDouble(persil.basic.current.luasDibayar);
                        pelunasan.created = DateTime.Now;
                        pelunasan.reason = "Dari Pembayaran";

                        var lst = new List<pelunasan>();
                        if (persil.paidHistories != null)
                            lst = persil.paidHistories.ToList();

                        lst.Add(pelunasan);

                        persil.paidHistories = lst.ToArray();

                        contextes.persils.Update(persil);
                    }

                    (var sisapelunasan, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);
                    var byrSubDtl = new BayarSubDtl();
                    byrSubDtl.fromCore(persil.key, sisapelunasan);

                    byrdtl.AddSubDetail(byrSubDtl);
                }

#if test
                byrdtl.CreateGraphInstance(user, JenisBayar.Lunas);
#else
                byrdtl.CreateGraphInstance(user, JenisBayar.Lunas, graphhost);
#endif
                byrdtl.Jumlah = byrdtl.subdetails.Select(x => x.Jumlah).Sum();
                byr.AddDetail(byrdtl);

                host.Update(byr);
                contextes.SaveChanges();

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/bdg/add")]
        public IActionResult AddBidang([FromQuery] string token, string bkey, [FromBody] string[] pkeys)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var DPExists = byr.details.Any(x => x.jenisBayar == JenisBayar.DP && x.invalid != true);
                var lunasExists = byr.details.Any(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true);

                if (lunasExists)
                    return new UnprocessableEntityObjectResult("Pembayaran sudah ada Pelunasan, tidak bisa menambah bidang");

                var UTJExists = byr.details.FirstOrDefault(x => x.jenisBayar == JenisBayar.UTJ && x.invalid != true);

                foreach (var k in pkeys)
                {
                    if (byr.bidangs != null)
                    {
                        if (byr.bidangs.Any(d => d.keyPersil == k))
                            continue;
                    }
                    var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };

                    byr.AddDetailBidang(byrdtlbidang);

                    if (UTJExists != null)
                    {
                        var persil = GetPersil(k);
                        if (persil == null)
                            continue;

#if test
                        var gMain = ghost.Get(UTJExists.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                        var graphhost = HostServicesHelper.GetGraphHost(services);
                        var gMain = graphhost.Get(UTJExists.instkey) ?? new GraphMainInstance();
#endif

                        var LastStatus = gMain.lastState?.state;

                        if (persil.en_state != StatusBidang.bebas && (LastStatus == ToDoState.complished_ || LastStatus == ToDoState.cashierApproved_))
                        {
                            var es = persil.en_state;
                            var deal = persil.deal;
                            var dealer = persil.dealer;
                            var statechanger = persil.statechanger;
                            var tgl = persil.statechanged;

                            (es, deal, dealer, statechanger, tgl) = (StatusBidang.bebas, deal, dealer, user.key, DateTime.Now);

                            persil.FromCore(es, deal, dealer, statechanger, tgl);
                            contextes.persils.Update(persil);
                            contextes.SaveChanges();
                        }

                        var deals = LastStatus switch
                        {
                            ToDoState.created_ => StatusDeal.Deal1A,
                            ToDoState.reviewerApproval_ => StatusDeal.Deal2,
                            ToDoState.auditApproval_ => StatusDeal.Deal2A,
                            ToDoState.accountingApproval_ => StatusDeal.Deal3,
                            ToDoState.reviewAndAcctgApproved_ => StatusDeal.Deal4,
                            ToDoState.cashierApproved_ => StatusDeal.Bebas,
                            _ => (StatusDeal.Deal)
                        };

                        var dealstatus = new deals
                        {
                            deal = DateTime.Now,
                            dealer = user.key,
                            status = deals
                        };

                        var lst = new List<deals>();
                        if (persil.dealStatus != null)
                            lst = persil.dealStatus.ToList();

                        lst.Add(dealstatus);

                        persil.dealStatus = lst.ToArray();
                        contextes.persils.Update(persil);
                        contextes.SaveChanges();

                    }
                }

                host.Update(byr);

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

        [NeedToken("PAY_LAND_FULL")]
        [HttpGet("dtl/bdg/delete")]
        public IActionResult DeleteBidang([FromQuery] string token, string bkey, string pkey)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(bkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var exists = byr.details.Where(x => x.invalid != true).SelectMany(x => x.subdetails).Where(x => x.keyPersil == pkey).Any();

                if (exists)
                    return new UnprocessableEntityObjectResult("Bidang memiliki Pembayaran yang aktif");

                var bidangs = byr.bidangs.ToList();
                bidangs.RemoveAll(x => x.keyPersil == pkey);
                byr.bidangs = bidangs.ToArray();

                host.Update(byr);

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

        [NeedToken("PAY_LAND_REVIEW")]
        [HttpPost("dtl/edit-memo")]
        public IActionResult EditDetilMemo([FromQuery] string token, [FromBody] BayarDtlXMemo core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(core.Tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (core.jenisBayar != JenisBayar.Deposit)
                {
                    var details = byr.details.Where(x => x.key == core.key).FirstOrDefault();

                    if (details == null)
                        return new UnprocessableEntityObjectResult("Pembayaran tidak ada!");

#if test
                    var gMain = ghost.Get(details.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                    var graphhost = HostServicesHelper.GetGraphHost(services);
                    var gMain = graphhost.Get(details.instkey) ?? new GraphMainInstance();
#endif

                    var LastStatus = gMain.lastState?.state;

                    if (LastStatus != ToDoState.issued_ && LastStatus != ToDoState.created_ && LastStatus != ToDoState.reviewerApproval_)
                        return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                    details.fromCore(core.noMemo);
                }
                else
                {
                    var deposit = byr.deposits.Where(x => x.key == core.key).FirstOrDefault();

                    if (deposit == null)
                        return new UnprocessableEntityObjectResult("Pembayaran tidak ada!");
#if test
                    var gMain = ghost.Get(deposit.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                    var graphhost = HostServicesHelper.GetGraphHost(services);
                    var gMain = graphhost.Get(deposit.instkey) ?? new GraphMainInstance();
#endif

                    var LastStatus = gMain.lastState?.state;

                    if (LastStatus != ToDoState.issued_ && LastStatus != ToDoState.created_ && LastStatus != ToDoState.reviewerApproval_)
                        return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                    deposit.fromCore(core.noMemo);
                }

                host.Update(byr);

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


        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/bdg/edit")]
        public IActionResult EditBidang([FromQuery] string token, string opr, [FromBody] BidangCommand cmd)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(cmd.Tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var exists = byr.IsLunas(cmd.keyPersil);
                if (exists)
                    return new UnprocessableEntityObjectResult("Bidang Sudah Lunas!");

                var persil = GetPersil(cmd.keyPersil);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                if (opr == "pbt")
                    persil.luasFix = false;
                else
                    persil.luasFix = true;

                persil.isEdited = true;

                var last = persil.basic.entries.LastOrDefault();

                var item = new PersilBasic();
                item = persil.basic.current;
                item.luasInternal = cmd.luasInternal;
                item.luasDibayar = cmd.luasDibayar;
                item.reason = cmd.reason;

                var newEntries1 =
                    new ValidatableEntry<PersilBasic>
                    {
                        created = DateTime.Now,
                        en_kind = ChangeKind.Update,
                        keyCreator = user.key,
                        keyReviewer = user.key,
                        reviewed = DateTime.Now,
                        approved = true,
                        item = item
                    };

                persil.basic.entries.Add(newEntries1);

                persil.basic.current = item;
                contextes.persils.Update(persil);
                contextes.SaveChanges();

                if (last != null && last.reviewed == null)
                {
                    var item2 = new PersilBasic();
                    item2 = last.item;
                    item2.luasInternal = cmd.luasInternal;
                    item2.luasDibayar = cmd.luasDibayar;
                    item2.reason = cmd.reason;

                    var newEntries2 =
                        new ValidatableEntry<PersilBasic>
                        {
                            created = last.created,
                            en_kind = last.en_kind,
                            keyCreator = last.keyCreator,
                            keyReviewer = last.keyReviewer,
                            reviewed = last.reviewed,
                            approved = last.approved,
                            item = item2
                        };

                    persil.basic.entries.Add(newEntries2);

                    contextes.persils.Update(persil);
                    contextes.SaveChanges();
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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/bdg/edit-biaya")]
        public IActionResult EditBidangBiaya([FromQuery] string token, [FromBody] BiayaCore core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(core.Tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var exists = byr.IsLunas(core.Pkey);

                if (exists)
                    return new UnprocessableEntityObjectResult("Bidang Sudah Lunas!");

                var persil = GetPersil(core.Pkey);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada!");

                var list = new List<biayalainnya>();
                foreach (var item in core.biayalainnyaCore)
                {
                    var lainnya = new biayalainnya();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fgLainnya = item.fglainnya;

                    list.Add(lainnya);
                }

                (persil.luasFix, persil.basic.current.satuan, persil.pph21, persil.ValidasiPPH, persil.ValidasiPPHValue,
                      persil.basic.current.satuanAkte, persil.mandor, persil.pembatalanNIB, persil.BiayaBN, persil.gantiBlanko, persil.kompensasi, persil.pajakLama,
                      persil.pajakWaris, persil.tunggakanPBB, persil.biayalainnya) = (core.luasFix, core.satuan, core.pph21, core.ValidasiPPH, core.ValidasiPPHValue, core.satuanAkta,
                       core.fgmandor == false ? core.Mandor * -1 : core.Mandor,
                        core.fgpembatalanNIB == false ? core.PembatalanNIB * -1 : core.PembatalanNIB,
                        core.fgbaliknama == false ? core.BalikNama * -1 : core.BalikNama,
                        core.fggantiblanko == false ? core.GantiBlanko * -1 : core.GantiBlanko,
                        core.fgkompensasi == false ? core.Kompensasi * -1 : core.Kompensasi,
                        core.fgpajaklama == false ? core.PajakLama * -1 : core.PajakLama,
                        core.fgpajakwaris == false ? core.PajakWaris * -1 : core.PajakWaris,
                        core.fgtunggakanPBB == false ? core.TunggakanPBB * -1 : core.TunggakanPBB, list.ToArray());

                if (persil.basic.current.satuan != core.satuan || persil.basic.current.satuanAkte != core.satuanAkta)
                    ForEntriesSatuan(persil, user, core.satuan, core.satuanAkta);

                contextes.persils.Update(persil);
                contextes.SaveChanges();

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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/edit")]
        public IActionResult EditPembayaran([FromQuery] string token, [FromBody] BayarDtlCore core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(core.Tkey) as Bayar;
                var detail = byr.details.Where(x => x.key == core.key);
                var jenisBayar = detail.Select(det => det.jenisBayar).FirstOrDefault();

                if (Math.Round(core.Jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                    return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");
                if (byr.bidangs.Count() <= 0)
                    return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                if (jenisBayar == JenisBayar.UTJ)
                {
                    UpdateUTJ(byr, detail, core, staticCollections);
                }
                else if (jenisBayar == JenisBayar.DP)
                {
                    var detail_dp = byr.details.Where(x => x.jenisBayar == JenisBayar.DP && x.key == core.key).FirstOrDefault();
                    detail_dp.fromCore(core);
                    detail_dp.fillStaticInfo(staticCollections);

                    string[] keyPersil_old = detail_dp.subdetails.Select(x => x.keyPersil).ToArray();
                    string[] keyPersil_new = core.keyPersil.Split(",");

                    var lstNewGiro = new List<Giro>();
                    List<BayarSubDtl> lstByrSubDtl = new List<BayarSubDtl>();
                    detail_dp.Jumlah = core.Jumlah;

                    if (string.IsNullOrEmpty(core.keyPersil))
                    {
                        foreach (var item in detail_dp.subdetails)
                        {
                            var persil = GetPersil(item.keyPersil);
                            var exists = byr.IsLunas(item.keyPersil);
                            if (exists)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} sudah lunas");

                            (double _jumlah, double _luasdiBayar, double _luas) = (0, 0, 0);

                            var _luasNIB = PersilHelper.GetLuasBayar(persil);

                            _luas = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                            if (persil.luasFix != true)
                                _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                            else
                                _luasdiBayar = _luas;

                            var total = (_luasdiBayar * persil.basic.current.satuan);

                            var allHarga = byr.AllHarga(contextpay, keyPersil_old);

                            if (core.Jumlah == 0)
                                _jumlah = (allHarga * core.Persentase ?? 0) / 100;
                            else
                                _jumlah = core.Jumlah;

                            var proposional = ProposionaHargaTotal(Convert.ToDouble(total), _jumlah, allHarga);
                            var percentage = (proposional / total) * 100;
                            var earlyPay = persil.earlyPay == null ? false : persil.earlyPay;
                            if (earlyPay != true)
                            {
                                if (percentage > 80)
                                {
                                    return new UnprocessableEntityObjectResult("DP tidak boleh lebih besar dari 80%");
                                }
                            }
                            detail_dp.Jumlah = _jumlah;

                            if (Math.Round(_jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                                return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                            var byrSubDtl = new BayarSubDtl();
                            byrSubDtl.fromCore(item.keyPersil, proposional);
                            lstByrSubDtl.Add(byrSubDtl);
                        }
                    }
                    else
                    {
                        foreach (var key in keyPersil_new)
                        {
                            var persil = GetPersil(key.Trim());
                            var exists = byr.IsLunas(key.Trim());
                            if (exists)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} sudah lunas");

                            (double _jumlah, double _luasdiBayar, double _luas) = (0, 0, 0);

                            var _luasNIB = PersilHelper.GetLuasBayar(persil);
                            _luas = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);
                            if (persil.luasFix != true)
                                _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                            else
                                _luasdiBayar = _luas;

                            var total = (_luasdiBayar * persil.basic.current.satuan);
                            var allHarga = byr.AllHarga(contextpay, keyPersil_old);

                            if (core.Jumlah == 0)
                                _jumlah = (allHarga * core.Persentase ?? 0) / 100;
                            else
                                _jumlah = core.Jumlah;

                            var proposional = ProposionaHargaTotal(Convert.ToDouble(total), _jumlah, allHarga);
                            var percentage = (proposional / total) * 100;
                            var earlyPay = persil.earlyPay == null ? false : persil.earlyPay;
                            if (earlyPay != true)
                            {
                                if (percentage > 80)
                                {
                                    return new UnprocessableEntityObjectResult("DP tidak boleh lebih besar dari 80%");
                                }
                            }
                            detail_dp.Jumlah = _jumlah;

                            if (Math.Round(_jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                                return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");

                            var byrSubDtl = new BayarSubDtl();
                            byrSubDtl.fromCore(key.Trim(), proposional);
                            lstByrSubDtl.Add(byrSubDtl);
                        }
                    }

                    foreach (var item in core.pemecahanGiro)
                    {
                        var newGiro = new Giro();
                        newGiro.jenis = item.Jenis;
                        newGiro.accPenerima = item.AccountPenerima;
                        newGiro.bankPenerima = item.BankPenerima;
                        newGiro.namaPenerima = item.NamaPenerima;
                        newGiro.nominal = item.Nominal;
                        lstNewGiro.Add(newGiro);
                    }

                    detail_dp.subdetails = lstByrSubDtl.ToArray();
                    detail_dp.giro = lstNewGiro.ToArray();
                    host.Update(byr);
                    context.SaveChanges();
                }
                else if (core.jenisBayar == JenisBayar.Lunas)
                {
                    var details = detail.Where(x => x.jenisBayar == JenisBayar.Lunas).FirstOrDefault();
                    details.fromCore(core);
                    details.fillStaticInfo(staticCollections);
                    List<BayarSubDtl> bayarSubDtls = new List<BayarSubDtl>();
                    List<Giro> lstNewGiro = new List<Giro>();

                    string[] persilsCore;
                    if (!string.IsNullOrEmpty(core.keyPersil))
                    {
                        persilsCore = core.keyPersil.Split(',').ToList().Union(details.subdetails.Select(sub => sub.keyPersil)).ToArray();
                    }
                    else
                    {
                        persilsCore = byr.bidangs.Select(x => x.keyPersil).ToArray();
                    }

                    details.subdetails = new BayarSubDtl[0];
                    host.Update(byr);
                    contextes.SaveChanges();

                    foreach (var key in persilsCore)
                    {
                        var persil = GetPersil(key);
                        bool isUpdate = false;
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ditemukan");

                        var luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
                        if (persil.luasFix != true && luasNIB != 0 && luasNIB != null)
                            return new UnprocessableEntityObjectResult($"NIB Perorangan IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} belum diinput");

                        var luasPelunasan = persil.luasFix != true ? luasNIB : persil.basic.current.luasDibayar;
                        persil.luasPelunasan = luasPelunasan;

                        if (!string.IsNullOrEmpty(core.keyPersil))
                        {
                            bool isPersilKeyInCore = core.keyPersil.Contains(key);
                            bool isPersilKeyinBayar = details.subdetails.Where(sub => sub.keyPersil.Contains(key)).Count() > 0 ? true : false;
                            if (isPersilKeyinBayar && isPersilKeyInCore)
                            {
                                isUpdate = true;
                                foreach (var item in persil.paidHistories)
                                {
                                    var pelunasan = new pelunasan();
                                    pelunasan.keyCreator = user.key;
                                    pelunasan.luasPelunasan = Convert.ToDouble(luasPelunasan);
                                    pelunasan.created = DateTime.Now;
                                    pelunasan.reason = "Dari Pembayaran";

                                    var lst = new List<pelunasan>();
                                    if (persil.paidHistories != null)
                                        lst = persil.paidHistories.ToList();

                                    lst.Add(pelunasan);

                                    persil.paidHistories = lst.ToArray();

                                    //For Entries
                                    var last = persil.basic.entries.LastOrDefault();

                                    if (last.reviewed == null)
                                    {
                                        last.item.luasDibayar = luasPelunasan;
                                        persil.basic.current = last.item;
                                        contextes.persils.Update(persil);
                                    }
                                    else
                                    {
                                        var itemPersil = new PersilBasic();
                                        itemPersil = persil.basic.current;
                                        itemPersil.luasDibayar = luasPelunasan;
                                        if (!string.IsNullOrEmpty(last.item.reason))
                                            itemPersil.reason = last.item.reason;
                                        persil.basic.current = itemPersil;
                                        contextes.persils.Update(persil);
                                    }
                                }
                            }
                            else if (isPersilKeyinBayar && !isPersilKeyInCore)
                            {
                                isUpdate = false;
                            }
                            else if (!isPersilKeyinBayar && isPersilKeyInCore)
                            {
                                isUpdate = true;
                                var pelunasan = new pelunasan();
                                pelunasan.luasPelunasan = Convert.ToDouble(luasPelunasan);
                                pelunasan.created = DateTime.Now;
                                pelunasan.reason = "Dari Pembayaran";

                                var lst = new List<pelunasan>();
                                if (persil.paidHistories != null)
                                    lst = persil.paidHistories.ToList();
                                lst.Add(pelunasan);
                                persil.paidHistories = lst.ToArray();

                                //For Entries
                                var last = persil.basic.entries.LastOrDefault();

                                if (last.reviewed == null)
                                {
                                    last.item.luasDibayar = luasPelunasan;
                                    persil.basic.current = last.item;
                                    contextes.persils.Update(persil);
                                }
                                else
                                {
                                    var itemPersil = new PersilBasic();
                                    itemPersil = persil.basic.current;
                                    itemPersil.luasDibayar = luasPelunasan;
                                    if (!string.IsNullOrEmpty(last.item.reason))
                                        itemPersil.reason = last.item.reason;
                                    persil.basic.current = itemPersil;
                                    contextes.persils.Update(persil);
                                }
                            }
                        }
                        else
                        {
                            isUpdate = true;
                            var pelunasan = new pelunasan();
                            pelunasan.keyCreator = user.key;
                            pelunasan.luasPelunasan = Convert.ToDouble(luasPelunasan);
                            pelunasan.created = DateTime.Now;
                            pelunasan.reason = "Dari Pembayaran";

                            var lst = new List<pelunasan>();
                            if (persil.paidHistories != null)
                                lst = persil.paidHistories.ToList();

                            lst.Add(pelunasan);

                            persil.paidHistories = lst.ToArray();
                        }
                        contextes.persils.Update(persil);
                        if (isUpdate)
                        {
                            (var sisapelunasan, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);
                            var byrSubDtl = new BayarSubDtl();
                            byrSubDtl.fromCore(persil.key, sisapelunasan);
                            bayarSubDtls.Add(byrSubDtl);
                        }
                    }

                    details.subdetails = bayarSubDtls.ToArray(); //Update Subdetails
                    foreach (var item in core.pemecahanGiro)
                    {
                        var newGiro = new Giro();
                        newGiro.jenis = item.Jenis;
                        newGiro.accPenerima = item.AccountPenerima;
                        newGiro.bankPenerima = item.BankPenerima;
                        newGiro.namaPenerima = item.NamaPenerima;
                        newGiro.nominal = item.Nominal;
                        lstNewGiro.Add(newGiro);
                    }

                    details.giro = lstNewGiro.ToArray();
                    host.Update(byr);
                    contextes.SaveChanges();
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

        [NeedToken("PAY_LAND_FULL")]
        [HttpPost("dtl/edit-deposit")]
        public IActionResult EditDeposit([FromQuery] string token, [FromBody] BayarDtlDepositCore core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(core.Tkey) as Bayar;
                var deposit = byr.deposits.Where(x => x.key == core.key).FirstOrDefault();

                if (Math.Round(core.Jumlah) != core.pemecahanGiro.Select(x => x.Nominal).Sum())
                    return new UnprocessableEntityObjectResult("Nilai Nominal pada giro tidak sama dengan Nilai Pembayaran, harap input ulang!");
                if (byr.bidangs.Count() <= 0)
                    return new UnprocessableEntityObjectResult("Tambahkan Bidang terlebih dahulu");

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                var lstNewGiro = new List<Giro>();

                deposit.fillStaticInfo(staticCollections);
                if (byr != null && deposit != null)
                {
                    foreach (var sub in core.subdetails)
                    {
                        var old = deposit.subdetails.Where(x => x.keyPersil == sub.keyPersil).FirstOrDefault();
                        if (old != null)
                        {
                            var persil = GetPersil(sub.keyPersil);
                            old.fromCore(sub);
                        }
                        else
                        {
                            var persil = GetPersil(sub.keyPersil);

                            if (persil == null)
                                return new UnprocessableEntityObjectResult("Bidang tidak ditemukan");

                            var exists = byr.IsLunas(persil.key);

                            if (!exists)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} belum ada pelunasan");

                            var anyDeposit = byr.Deposit(persil);

                            if (anyDeposit == 0)
                                return new UnprocessableEntityObjectResult($"IdBidang {persil.IdBidang} dengan Alashak {persil.basic.current.surat.nomor} tidak memiliki deposit");

                            var byrSubDtl = new BayarSubDtlDeposit();
                            byrSubDtl.fromCore(sub);
                            deposit.AddSubDetail(byrSubDtl);
                        }


                    }

                    foreach (var item in core.pemecahanGiro)
                    {
                        var newGiro = new Giro();
                        newGiro.jenis = item.Jenis;
                        newGiro.accPenerima = item.AccountPenerima;
                        newGiro.bankPenerima = item.BankPenerima;
                        newGiro.namaPenerima = item.NamaPenerima;
                        newGiro.nominal = item.Nominal;
                        lstNewGiro.Add(newGiro);
                    }

                    deposit.giro = lstNewGiro.ToArray();
                    deposit.fromCore(core);
                    host.Update(byr);
                    context.SaveChanges();
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

        private void UpdateUTJ(Bayar byr, IEnumerable<BayarDtl> bayarDtl, BayarDtlCore core, StaticCollection staticCollections)
        {
            var host = HostServicesHelper.GetBayarHost(services);
            var byrdtl = bayarDtl.Where(x => x.jenisBayar == JenisBayar.UTJ).FirstOrDefault();
            var lstNewGiro = new List<Giro>();
            byrdtl.fromCore(core);
            byrdtl.fillStaticInfo(staticCollections);
            if (byr != null && byrdtl != null)
            {
                var allLuas = byr.AllLuas(contextpay);
                foreach (var bidang in byr.bidangs)
                {
                    double _luasdiBayar = 0;
                    double _luas = 0;

                    var old = byrdtl.subdetails.Where(x => x.keyPersil == bidang.keyPersil).FirstOrDefault();
                    if (old != null)
                    {
                        var _persil = GetPersil(bidang.keyPersil);
                        var _luasNIB = PersilHelper.GetLuasBayar(_persil);

                        _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

                        if (_persil.luasFix != true)
                            _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                        else
                            _luasdiBayar = _luas;

                        var jml = core.fgProposional == ProposionalBayar.Luas ? ProposionalLuas(_luasdiBayar, core.Jumlah, allLuas) : ProposionalRata(core.Jumlah, byr.bidangs.Count());
                        old.fromCore(jml);
                    }
                    else
                    {
                        var byrSubDtl = new BayarSubDtl();
                        var _persil = GetPersil(bidang.keyPersil);

                        var _luasNIB = PersilHelper.GetLuasBayar(_persil);

                        _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

                        if (_persil.luasFix != true)
                            _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                        else
                            _luasdiBayar = _luas;

                        byrSubDtl.keyPersil = bidang.keyPersil;
                        byrSubDtl.Jumlah = core.fgProposional == ProposionalBayar.Luas ? ProposionalLuas(_luasdiBayar, byrdtl.Jumlah, allLuas) : ProposionalRata(core.Jumlah, byr.bidangs.Count());
                    }
                }

                foreach (var item in core.pemecahanGiro)
                {
                    var newGiro = new Giro();
                    newGiro.jenis = item.Jenis;
                    newGiro.accPenerima = item.AccountPenerima;
                    newGiro.bankPenerima = item.BankPenerima;
                    newGiro.namaPenerima = item.NamaPenerima;
                    newGiro.nominal = item.Nominal;
                    lstNewGiro.Add(newGiro);
                }

                byrdtl.giro = lstNewGiro.ToArray();
                host.Update(byr);
                context.SaveChanges();
            }
        }


        [NeedToken("PAY_LAND_FULL")]
        [ProducesResponseType(typeof(BayarDtlViewExt2), (int)HttpStatusCode.OK)]
        [HttpGet("dtl/get")]
        public IActionResult GetDetail([FromQuery] string token, string tkey, string bkey)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                if (!byr.details.Any(x => x.key == bkey))
                    return new UnprocessableEntityObjectResult("Pembayaran tidak ada");

                var details = byr.details.Where(x => x.key == bkey).ToList().FirstOrDefault();

#if test
                var gMain = ghost.Get(details.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
                var gMain = graphhost.Get(details.instkey) ?? new GraphMainInstance();
#endif
                var LastStatus = gMain.lastState?.state;

                if (LastStatus != ToDoState.created_ && LastStatus != ToDoState.rejected_)
                    return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                var subdetails = details.subdetails.ToList();
                var isLunasORDPExist = (from b in byr.details
                                        where (b.jenisBayar == JenisBayar.Lunas
                                                || b.jenisBayar == (details.jenisBayar == JenisBayar.DP ?
                                                                    JenisBayar.Lunas : JenisBayar.DP
                                                                   )
                                               )
                                        select b.subdetails.Where(sub => subdetails.Select(sub2 => sub2.keyPersil).Contains(sub.keyPersil)).FirstOrDefault()
                                        ).FirstOrDefault() != null;
                if (details.jenisBayar != JenisBayar.Lunas && isLunasORDPExist)
                    return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                var bidangs = byr.bidangs.ToList();
                var subdetails_all = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas).Select(det => det.subdetails);

                var qryBidSelected = (from c in bidangs
                                      where (from o in subdetails
                                             select o.keyPersil
                                            ).Contains(c.keyPersil)
                                      select c).ToList();
                var bidangSelect = qryBidSelected.Select(bid => bid.toViewSelected(contextpay, byr, true));

                //bidang yang gak ada di pembayaran lain dan bidang yg belum dipilih
                var qryBidElse = new List<BayarDtlBidang>();
                if (details.jenisBayar == JenisBayar.Lunas || details.jenisBayar == JenisBayar.DP)
                    qryBidElse = (from c in bidangs
                                  where !(from o in subdetails_all
                                          select o.Where(sub => sub.keyPersil.Contains(c.keyPersil))
                                         ).Equals(true)
                                  select c).ToList()
                                           .Where(a => false == (bidangSelect.Select(bid => bid.keyPersil)
                                           .Contains(a.keyPersil))).ToList();

                var bidangNotSelected = qryBidElse.Select(bid => bid.toViewSelected(contextpay, byr, false));
                var bidang_all = bidangSelect.Union(bidangNotSelected).Distinct().ToList();

                var view = details.toView(contextpay, byr, bidang_all, staticCollections);

                return new JsonResult(view);
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

        [NeedToken("PAY_LAND_FULL")]
        [ProducesResponseType(typeof(BayarDtlViewExt2), (int)HttpStatusCode.OK)]
        [HttpGet("dtl/get-deposit")]
        public IActionResult GetDetailDeposit([FromQuery] string token, string tkey, string bkey)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var byr = host.GetBayar(tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (byr.bidangs == null)
                    byr.bidangs = new BayarDtlBidang[0];

                if (!byr.deposits.Any(x => x.key == bkey))
                    return new UnprocessableEntityObjectResult("Deposit tidak ada");

                var deposits = byr.deposits.Where(x => x.key == bkey).ToList().FirstOrDefault();

#if test
                var gMain = ghost.Get(deposits.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
                var gMain = graphhost.Get(deposits.instkey) ?? new GraphMainInstance();
#endif

                var LastStatus = gMain.lastState?.state;

                if (LastStatus != ToDoState.created_ && LastStatus != ToDoState.rejected_)
                    return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();

                var view = deposits.toView(contextpay, staticCollections, byr.key);

                return new JsonResult(view);
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

        [NeedToken("PAY_LAND_FULL")]
        [HttpGet("dtl/bdg/get-biaya")]
        public IActionResult GetBiayaBidang([FromQuery] string token, string Tkey, string Pkey)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(Tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                var exists = byr.bidangs.Select(x => x.keyPersil).Contains(Pkey);
                if (!exists)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada dalam tahap tersebut");

                //var detaillunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas).ToList();
                //var lunas = detaillunas.SelectMany(x => x.subdetails, (y, z) => new { key = z.keyPersil }).Any(x => x.key == Pkey);

                var lunas = byr.IsLunas(Pkey);

                if (lunas)
                    return new UnprocessableEntityObjectResult("Bidang Sudah Lunas! tidak bisa diubah");

                var persil = GetPersil(Pkey);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada!");

                var view = new BiayaView();
                var basic = persil.basic.current;

                var biayalainnyas = new List<BiayalainnyaCore>();
                foreach (var item in persil.biayalainnya)
                {
                    var lainnya = new BiayalainnyaCore();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fglainnya = item.fgLainnya;

                    biayalainnyas.Add(lainnya);
                }

                (view.luasFix, view.Tkey, view.Pkey, view.AlasHak, view.luasSurat, view.luasUkur, view.luasDibayar, view.satuan, view.pph21, view.ValidasiPPH, view.ValidasiPPHValue,
                       view.satuanAkta, view.Mandor, view.PembatalanNIB, view.BalikNama, view.GantiBlanko, view.Kompensasi, view.PajakLama,
                       view.PajakWaris, view.TunggakanPBB, view.biayalainnya) = (persil.luasFix == null ? false : persil.luasFix, Tkey, Pkey, basic.surat.nomor, basic.luasSurat, basic.luasInternal, basic.luasDibayar,
                       basic.satuan, persil.pph21, persil.ValidasiPPH, persil.ValidasiPPHValue,
                      basic.satuanAkte, persil.mandor, persil.pembatalanNIB, persil.BiayaBN, persil.gantiBlanko, persil.kompensasi, persil.pajakLama,
                      persil.pajakWaris, persil.tunggakanPBB, biayalainnyas.ToArray());

                return Ok(view);

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


        [NeedToken("PAY_LAND_REVIEW")]
        [HttpGet("dtl/get-pay")]
        public IActionResult GetDetailMemo([FromQuery] string token, string Tkey, string Dkey, JenisBayar jns)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
                var view = new BayarDtlXMemo();

                var byr = host.GetBayar(Tkey) as Bayar;
                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");

                if (jns != JenisBayar.Deposit)
                {
                    var detail = byr.details.Where(x => x.key == Dkey).FirstOrDefault();

                    if (detail == null)
                        return new UnprocessableEntityObjectResult("Pembayaran tidak ada!");
#if test
                    var gMain = ghost.Get(detail.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                    var graphhost = HostServicesHelper.GetGraphHost(services);
                    var gMain = graphhost.Get(detail.instkey) ?? new GraphMainInstance();
#endif


                    var LastStatus = gMain.lastState?.state;

                    if (LastStatus != ToDoState.issued_ && LastStatus != ToDoState.created_ && LastStatus != ToDoState.reviewerApproval_)
                        return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                    (view.Tkey, view.jenisBayar, view.key, view.Instkey, view.noMemo) = (byr.key, detail.jenisBayar, detail.key, detail.instkey, detail.noMemo);
                }
                else
                {
                    var deposit = byr.deposits.Where(x => x.key == Dkey).FirstOrDefault();

                    if (deposit == null)
                        return new UnprocessableEntityObjectResult("Pembayaran tidak ada!");

#if test
                    var gMain = ghost.Get(deposit.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                    var graphhost = HostServicesHelper.GetGraphHost(services);
                    var gMain = graphhost.Get(deposit.instkey) ?? new GraphMainInstance();
#endif

                    var LastStatus = gMain.lastState?.state;

                    if (LastStatus != ToDoState.issued_ && LastStatus != ToDoState.created_ && LastStatus != ToDoState.reviewerApproval_)
                        return new UnprocessableEntityObjectResult("Pembayaran Sudah tidak bisa diubah!");

                    (view.Tkey, view.jenisBayar, view.key, view.Instkey, view.noMemo) = (byr.key, deposit.jenisBayar, deposit.key, deposit.instkey, deposit.noMemo);
                }

                return Ok(view);

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
        [HttpGet("groups")]
        public IActionResult GetGroupList(string prokey, string desaKey)

        {
            var mcontext = Contextual.GetContextPlus();
            try
            {
                var data = mcontext.GetDocuments(new MasterGroup(), "persils_v2",
                @"{$project: { _id: { keyProject: '$basic.current.keyProject', 
                keyDesa: '$basic.current.keyDesa', group: '$basic.current.group'} } }",
                @"{$project:
                            {
                            keyProject: '$_id.keyProject',
                    keyDesa: '$_id.keyDesa',
                    group: '$_id.group',
                    _id: 0}}").ToList().Where(x => x.keyProject == prokey && x.keyDesa == desaKey)
                .GroupBy(p => new { p.keyProject, p.keyDesa, p.group })
                .Select(g => g.First())
                .ToList();

                return new JsonResult(data.OrderBy(d => d.keyProject).ToArray());
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpGet("entries")]
        public IActionResult GetLastEntries([FromQuery] string token, string key, double? luas, double? _satuan)
        {
            try
            {
                var user = contextes.FindUser(token);
                var now = DateTime.Now;

                var persil = contextes.persils.FirstOrDefault(p => p.key == key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Invalid persil key givem");

                var last = persil.basic.entries.LastOrDefault();

                var item = new PersilBasic();
                item = persil.basic.current;
                item.luasSurat = luas;
                item.satuan = _satuan;

                var newEntries1 =
                    new ValidatableEntry<PersilBasic>
                    {
                        created = DateTime.Now,
                        en_kind = ChangeKind.Update,
                        keyCreator = user.key,
                        keyReviewer = user.key,
                        reviewed = DateTime.Now,
                        approved = true,
                        item = item
                    };

                persil.basic.entries.Add(newEntries1);

                persil.basic.current = item;
                contextes.persils.Update(persil);
                contextes.SaveChanges();


                if (last != null && last.reviewed == null)
                {
                    var item2 = new PersilBasic();
                    item2 = last.item;

                    var newEntries2 =
                        new ValidatableEntry<PersilBasic>
                        {
                            created = DateTime.Now,
                            en_kind = ChangeKind.Update,
                            keyCreator = user.key,
                            keyReviewer = user.key,
                            reviewed = DateTime.Now,
                            approved = true,
                            item = item2
                        };

                    persil.basic.entries.Add(newEntries2);

                    persil.basic.current = item2;
                    contextes.persils.Update(persil);
                    contextes.SaveChanges();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpPost("split")]
        public IActionResult Split([FromQuery] string token, string bkey, [FromBody] string pkeys)
        {
            var pcontext = Contextual.GetContextExt();
            var user = contextes.FindUser(token);
            var host = HostServicesHelper.GetBayarHost(services);

            var byr = host.GetBayar(bkey) as Bayar;
            if (byr == null)
                return new UnprocessableEntityObjectResult("Tahap tidak ada");
            double totaldiBayar = 0;
            if (byr.details != null)
                totaldiBayar = byr.details.Sum(x => x.Jumlah);
            var totalBidang = GetTotalLuasdiBayar(pkeys);
            var tot = new List<double>();

            foreach (var item in byr.bidangs)
            {
                var res = GetPersil(item.keyPersil);
                double value = 0;
                if (Convert.ToBoolean(res.luasFix))
                {
                    (var luas, var satuan) = (res.basic.current.luasSurat == null ? 0 : Convert.ToDouble(res.basic.current.luasSurat),
                        res.basic.current.satuan == null ? 0 : Convert.ToDouble(res.basic.current.satuan));

                    value = luas * satuan;
                }
                else
                {
                    (var luas, var satuan) = (res.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(res.basic.current.luasDibayar),
                       res.basic.current.satuan == null ? 0 : Convert.ToDouble(res.basic.current.satuan));

                    value = luas * satuan;
                }

                tot.Add(Convert.ToDouble(res));
            }

            var totalAllBidang = tot.Sum();

            var newDP = totaldiBayar * (totalBidang / totalAllBidang);

            var newBayar = new Bayar(user);
            var newBayarBdg = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = pkeys };
            var newBayarDtl = new BayarDtl() { key = mongospace.MongoEntity.MakeKey, jenisBayar = JenisBayar.DP, Jumlah = newDP, tglBayar = DateTime.Now };

            newBayar.keyProject = byr.keyProject;
            newBayar.keyDesa = byr.keyDesa;
            newBayar.group = byr.group;

            int num = 0;

            var lastnumber = contextpay.bayars.All().ToList().Count() == null ? 0 : contextpay.bayars.All().ToList().Count();
            if (lastnumber > 0)
            {
                num = contextpay.bayars.All().ToList().Max(x => x.nomorTahap);
            }
            num++;
            newBayar.nomorTahap = num;

            newBayar.AddDetail(newBayarDtl);
            newBayar.AddDetailBidang(newBayarBdg);

            //host.Add(newBayar);

            var old = byr.bidangs.FirstOrDefault(a => a.key == pkeys);
            byr.DelDetailBidang(old);
            //host.Update(byr);

            return Ok();
        }

        [HttpPost("step")]
        [Consumes("application/json")]
        [PersilCSMaterializer(Auto = false)]
        public IActionResult Step([FromQuery] string token, [FromBody] BayarDtlCommand data)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
#if test
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
#endif
                var byr = host.GetBayar(data.tkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.invalid == true)
                    return new UnprocessableEntityObjectResult("Tahap tidak aktif");

                var detail = byr.details.FirstOrDefault(x => x.key == data.pkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Pembayaran tidak ada");
#if test
                var instance = ghost.Get(detail.instkey).GetAwaiter().GetResult();
#else
                var instance = graphhost.Get(detail.instkey);
#endif
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Pembayaran belum lengkap");
                if (detail.tglBayar != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Pembayaran telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == data.rkey) && n.Active == true);
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow penugasan tidak jelas");
                var route = node.routes.First(r => r.key == data.rkey);

#if test
                (var ok, var reason) = ghost.Take(user, detail.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, detail.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#else
                (var ok, var reason) = graphhost.Take(user, detail.instkey, data.rkey);
                if (ok)
                    (ok, reason) = graphhost.Summary(user, detail.instkey, data.rkey, data.control.ToString("g"), null);
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#endif

                var priv = route.privs.FirstOrDefault();
                string error = null;
                if (data.control == ToDoControl._)
                    (ok, error) = route?._verb switch
                    {
                        //ToDoVerb.paymentSubmit_ => (AppDetail(byr, detail)),
                        ToDoVerb.cashierApprove_ => (BidangBebas(byr, detail, user)),
                        ToDoVerb.issue_ => (DealStatusUpdate(byr, detail, StatusDeal.Deal2, user)),
                        ToDoVerb.reissue_ => (DealStatusUpdate(byr, detail, StatusDeal.Deal2, user)),
                        ToDoVerb.abort_ => (Abort(byr, detail, node._state, priv.ToArray(), data.reason, ToDoVerb.abort_, user)),
                        ToDoVerb.confirmAbort_ => (FinalAbort(byr, detail, user)),
                        _ => (true, null)
                    };

                else if (data.control == ToDoControl.yes_)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.approve_ => (
                                                   DealStatusUpdate(byr, detail, node._state == ToDoState.reviewerApproval_ ?
                                                   StatusDeal.Deal2A : node._state == ToDoState.accountingApproval_ ?
                                                   StatusDeal.Deal4 : StatusDeal.Deal3, user)),
                        _ => (true, null)
                    };

                else if (data.control == ToDoControl.no_)
                {
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.approve_ => (
                                                DealStatusUpdate(byr, detail, node._state == ToDoState.reviewerApproval_ ?
                                                StatusDeal.Deal1A : node._state == ToDoState.accountingApproval_ ?
                                                StatusDeal.Deal2 : StatusDeal.Deal2, user)),
                        _ => (true, null)
                    };

                    (ok, error) = AddReason(byr, detail.key, node._state, priv.ToArray(), data.reason, route?._verb, user);
                }

                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status pembayaran" : error);
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

        [HttpPost("step-deposit")]
        [Consumes("application/json")]
        [PersilCSMaterializer(Auto = false)]
        public IActionResult StepDeposit([FromQuery] string token, [FromBody] BayarDtlCommand data)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);
#if test
#else
                var graphhost = HostServicesHelper.GetGraphHost(services);
#endif
                var byr = host.GetBayar(data.tkey) as Bayar;

                if (byr == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (byr.invalid == true)
                    return new UnprocessableEntityObjectResult("Tahap tidak aktif");

                var detail = byr.deposits.FirstOrDefault(x => x.key == data.pkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Pembayaran tidak ada");

#if test
                var instance = ghost.Get(detail.instkey).GetAwaiter().GetResult();
#else
                var instance = graphhost.Get(detail.instkey);
#endif
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Pembayaran belum lengkap");
                if (detail.tglBayar != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Pembayaran telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == data.rkey) && n.Active == true);
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow penugasan tidak jelas");
                var route = node.routes.First(r => r.key == data.rkey);

#if test
                (var ok, var reason) = ghost.Take(user, detail.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, detail.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#else
                (var ok, var reason) = graphhost.Take(user, detail.instkey, data.rkey);
                if (ok)
                    (ok, reason) = graphhost.Summary(user, detail.instkey, data.rkey, data.control.ToString("g"), null);
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#endif

                var priv = route.privs.FirstOrDefault();
                string error = null;
                if (data.control == ToDoControl._)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.abort_ => (AbortDeposit(byr, detail, node._state, priv.ToArray(), data.reason, ToDoVerb.abort_, user)),
                        ToDoVerb.confirmAbort_ => (FinalAbortDeposit(byr, detail, user)),
                        _ => (true, null)
                    };
                else if (data.control == ToDoControl.no_)
                {
                    (ok, error) = AddReason(byr, detail.key, node._state, priv.ToArray(), data.reason, route?._verb, user);
                }

                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status pembayaran" : error);
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

        [HttpGet("remainpay")]
        public IActionResult GetSisaPelunasan([FromQuery] string Tkey, string pkey, [FromQuery] string token)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var byr = host.GetBayar(Tkey) as Bayar;
                var persil = GetPersil(pkey);

                (var SisaLunas, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);

                return Ok(new
                {
                    sisaLunas = SisaLunas
                });
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }

        }

        [HttpGet("remainall")]
        public IActionResult GetAllSisaPelunasan([FromQuery] string Tkey, [FromQuery] string token)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetBayarHost(services);

                var totalSisaLunas = new List<double>();
                var byr = host.GetBayar(Tkey) as Bayar;
                var details = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas).ToList();
                var subdetails = details.SelectMany(x => x.subdetails).ToList();
                var bidangs = byr.bidangs.ToList();
                var keys =
                            (from c in bidangs
                             where !(from o in subdetails
                                     select o.keyPersil)
                                    .Contains(c.keyPersil)
                             select c.keyPersil).ToList();

                foreach (var key in keys)
                {
                    var persil = GetPersil(key);
                    (var SisaLunas, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);

                    totalSisaLunas.Add(SisaLunas);
                }

                return Ok(new
                {
                    sisaLunas = totalSisaLunas.Sum()
                });
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        [NeedToken("PAY_LAND_REVIEW,PAY_LAND_FULL")]
        [HttpGet("persil/pay")]
        public IActionResult GetPersilBayars()
        {
            try
            {
                var tm0 = DateTime.Now;
                var host = HostServicesHelper.GetBayarHost(services);
                var bayars = host.OpenedBayar().Cast<Bayar>().ToArray().AsParallel();

                var keypersils = bayars.SelectMany(x => x.bidangs).Select(x => x.keyPersil).Distinct();
                var keys = string.Join(',', keypersils.Select(k => $"'{k}'"));

                var persils = contextpay.GetCollections(new Persil(), "persils_v2", $"<key: <$in:[{keys}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var data = persils.Select(x => new
                {
                    IdBidang = x.IdBidang,
                    Total = x.basic.current.luasDibayar * x.basic.current.satuan,
                    pph = x.pph21 == true ? ((x.basic.current.luasDibayar * x.basic.current.satuan) * 2.5) / 100 : 0,
                    validasipph = x.ValidasiPPH == true ? x.ValidasiPPHValue : 0,
                    mandor = x.mandor != null && x.mandor > 0 ? x.mandor : 0,
                    pembatalanNIB = x.pembatalanNIB != null && x.pembatalanNIB > 0 ? x.pembatalanNIB : 0,
                    biayaBN = x.BiayaBN != null && x.BiayaBN > 0 ? x.BiayaBN : 0,
                    gantiBlanko = x.gantiBlanko != null && x.gantiBlanko > 0 ? x.gantiBlanko : 0,
                    kompensasi = x.kompensasi != null && x.kompensasi > 0 ? x.kompensasi : 0,
                    pajakLama = x.pajakLama != null && x.pajakLama > 0 ? x.pajakLama : 0,
                    pajakWaris = x.pajakWaris != null && x.pajakWaris > 0 ? x.pajakWaris : 0,
                    tunggakanPBB = x.tunggakanPBB != null && x.tunggakanPBB > 0 ? x.tunggakanPBB : 0,
                    biayaLainnya = x.biayalainnya.Where(z => z.fgLainnya == true).Select(t => t.nilai).Sum()
                }).Select(b => new
                {
                    IdBidang = b.IdBidang,
                    TotalBayar = b.Total - (b.pph + b.validasipph + b.mandor + b.pembatalanNIB + b.biayaBN + b.gantiBlanko + b.kompensasi + b.pajakLama + b.pajakWaris + b.tunggakanPBB + b.biayaLainnya)
                }).ToArray();

                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("tanggalbayar")]
        public IActionResult UpdateTanggalBayar()
        {
            try
            {
                var host = HostServicesHelper.GetBayarHost(services);

                var details = contextpay.GetDocuments(new { key = "", keydetail = "", instkey = "" }, "bayars",
                    @"{$unwind: '$details'}",
                    "{$match: {'details.tglBayar' : null}}",
                    @"{
                        $project: {
                            key : '$key',
                            keydetail : '$details.key',
                            instkey : '$details.instkey',
                            _id :0
                        }
                    }").ToArray();

                var keys = string.Join(',', details.Select(k => $"'{k.instkey}'"));

                var grapphables = contextpay.GetDocuments(new { key = "", PersetujuanKasir = DateTime.Now }, "graphables",
                    $@"<$match: <'key' : <$in: [{keys}]>>>".MongoJs(),
                    @"{$unwind: '$Core.nodes'}",
                    @"{$match: {
                                'Core.nodes.status' : 'Persetujuan Kasir',
                                'Core.nodes.Done' : true,
                                'Core.nodes.Active' : false
                                }
                    }",
                    @"{$project: {
                                'key' : '$key',
                                'PersetujuanKasir' : '$Core.nodes.Taken.on',
                                _id : 0
                                }
                    }").ToArray();

                foreach (var dt in details)
                {
                    var graph = grapphables.FirstOrDefault(x => x.key == dt.instkey);

                    if (graph == null)
                        continue;

                    var byr = host.GetBayar(dt.key) as Bayar;
                    var detail = byr.details.FirstOrDefault(x => x.key == dt.keydetail);

                    detail.tglBayar = graph.PersetujuanKasir;
                    host.Update(byr);
                }

                host.Reload();

                return Ok();
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet("ReloadData")]
        public IActionResult Reload()
        {
            var host = HostServicesHelper.GetBayarHost(services);
            host.Reload();

            return Ok();
        }

        (bool OK, string error) ApprovePembayaran(Bayar byr, BayarDtl byrdtl) // Update Tanggal Bayar
        {
            try
            {
                var host = HostServicesHelper.GetBayarHost(services);
                var old = byr.details.FirstOrDefault(a => a.key == byrdtl.key);
                old.Approved();
                return (host.Update(byr), null);
            }
            catch (Exception ex)
            {

                return (false, ex.Message);
            }

        }

        (bool OK, string error) DealStatusUpdate(Bayar byr, BayarDtl dtl, StatusDeal deal, user user)
        {
            try
            {
                //Update Status Deal
                string[] keys;

                var byrdtl = byr.details.FirstOrDefault(a => a.key == dtl.key);

                var dealstatus = new deals { deal = DateTime.Now, dealer = user.key, status = deal };

                byrdtl.AddDeals(dealstatus);

                var host = HostServicesHelper.GetBayarHost(services);
                host.Update(byr);

                keys = byrdtl.subdetails.Select(x => x.keyPersil).ToArray();

                var persildeals = contextes.persils.Query(x => keys.Contains(x.key)).Where(x => (x.en_state != StatusBidang.bebas || x.en_state != null)).ToList();
                if (persildeals.Count > 0)
                {
                    foreach (var item in persildeals)
                    {
                        var lst = new List<deals>();
                        if (item.dealStatus != null)
                            lst = item.dealStatus.ToList();

                        //var highestOnPersil = lst.Count() == 0 ? 0 : lst.Select(x => (int)x.status).Max();
                        var highestOnPersil = lst.Count() == 0 ? 0 : lst.OrderByDescending(x => x.deal).FirstOrDefault().status;

                        lst.Add(dealstatus);

                        var b = host.GetBayar(byr.key) as Bayar;
                        var c = b.details.Where(x => x.invalid != true).Count();

                        if (c > 1)
                        {
                            var highestOnByr = b.details.Where(x => x.invalid != true).Where(x => x.deals.Count() > 0)
                                                .SelectMany(x => x.subdetails, (z, y) => new
                                                {
                                                    lastDeal = z.deals.OrderByDescending(x => x.deal).FirstOrDefault().status,
                                                    subdtl = y
                                                }).Where(x => x.subdtl.keyPersil == item.key)
                                                .Select(x => (int)x.lastDeal).Max();

                            if ((int)highestOnPersil != highestOnByr)
                                item.dealStatus = lst.ToArray();
                        }
                        else
                            item.dealStatus = lst.ToArray();

                    }

                    contextes.persils.Update(persildeals);
                    contextes.SaveChanges();
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

        }

        (bool OK, string error) BidangBebas(Bayar byr, BayarDtl byrdtl, user user)
        {
            try
            {
                (var ok, var reason) = ApprovePembayaran(byr, byrdtl);
                //var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();
                if (ok)
                {
                    var bidangs = byrdtl.subdetails.Select(x => x.keyPersil).ToArray();
                    var persils = contextpay.persils.Query(x => bidangs.Contains(x.key));

                    foreach (var persil in persils)
                    {
                        //var persil = GetPersil(bidang.keyPersil);
                        if (persil == null)
                            return (false, "Persil not found");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);

                        if (persil.en_state != StatusBidang.bebas)
                        {
                            var es = persil.en_state;
                            var deal = persil.deal;
                            var dealer = persil.dealer;
                            var statechanger = persil.statechanger;
                            var tgl = persil.statechanged;

                            (es, deal, dealer, statechanger, tgl) = (StatusBidang.bebas, deal, dealer, user.key, DateTime.Now);

                            persil.FromCore(es, deal, dealer, statechanger, tgl);

                            contextpay.persils.Update(persil);
                            contextpay.SaveChanges();

                            var persilChild = contextpay.persils.Query(x => x.basic.current.keyParent == persil.key);
                            persilChild.ForEach(x => x.FromCore(es, deal, dealer, statechanger, tgl));
                            contextpay.persils.Update(persilChild);
                            contextpay.SaveChanges();
                        }

                        if (byrdtl.jenisBayar == JenisBayar.Lunas)
                        {
                            persil.luasPelunasan = (persil.luasFix ?? true) ? persil.basic.current.luasDibayar : (persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp);
                            //persil.luasPelunasan = persil.basic.current.luasDibayar;
                            contextpay.persils.Update(persil);
                            contextpay.SaveChanges();
                        }
                    }

                    (ok, reason) = DealStatusUpdate(byr, byrdtl, StatusDeal.Bebas, user);
                }

                return (ok, reason);
            }
            catch (Exception ex)
            {

                return (false, ex.Message);
            }


        }

        (bool OK, string error) AddReason(Bayar byr, string dtlkey, ToDoState state, string[] privs, string desc, ToDoVerb? verb, user user)
        {
            var old = byr.details.FirstOrDefault(a => a.key == dtlkey);

            var reason = new Reason
            {
                tanggal = DateTime.Now,
                flag = verb == ToDoVerb.abort_ ? true : false,
                keyCreator = user.key,
                privs = "",
                state = state,
                description = desc
            };

            old.AddReason(reason);

            var host = HostServicesHelper.GetBayarHost(services);
            host.Update(byr);

            return (true, null);
        }

        (bool OK, string error) Abort(Bayar byr, BayarDtl byrdtl, ToDoState state, string[] privs, string desc, ToDoVerb? verb, user user)
        {
            DealStatusUpdate(byr, byrdtl, StatusDeal.Deal1A, user);

            var old = byr.details.FirstOrDefault(a => a.key == byrdtl.key);

            var reason = new Reason
            {
                tanggal = DateTime.Now,
                flag = true,
                keyCreator = user.key,
                privs = "",
                state = state,
                description = desc
            };

            old.AddReason(reason);

            if (state == ToDoState.created_)
                old.Abort();

            var host = HostServicesHelper.GetBayarHost(services);
            host.Update(byr);

            return (true, null);
        }

        (bool OK, string error) AbortDeposit(Bayar byr, BayarDtlDeposit byrdtl, ToDoState state, string[] privs, string desc, ToDoVerb? verb, user user)
        {
            var old = byr.deposits.FirstOrDefault(a => a.key == byrdtl.key);

            var reason = new Reason
            {
                tanggal = DateTime.Now,
                flag = true,
                keyCreator = user.key,
                privs = "",
                state = state,
                description = desc
            };

            old.AddReason(reason);

            if (state == ToDoState.created_)
                old.Abort();

            var host = HostServicesHelper.GetBayarHost(services);
            host.Update(byr);

            return (true, null);
        }

        (bool OK, string error) FinalAbort(Bayar byr, BayarDtl byrdtl, user user)
        {

            DealStatusUpdate(byr, byrdtl, StatusDeal.Deal1, user);
            var old = byr.details.FirstOrDefault(a => a.key == byrdtl.key);
            old.Abort();
            var host = HostServicesHelper.GetBayarHost(services);
            host.Update(byr);

            return (true, null);
        }

        (bool OK, string error) FinalAbortDeposit(Bayar byr, BayarDtlDeposit byrdtl, user user)
        {

            var old = byr.deposits.FirstOrDefault(a => a.key == byrdtl.key);
            old.Abort();
            var host = HostServicesHelper.GetBayarHost(services);
            host.Update(byr);

            return (true, null);
        }

        double ProposionalLuas(double luasdiBayar, double jumlahPembayaran, double allLuas)
        {
            var result = Math.Round((jumlahPembayaran * luasdiBayar) / allLuas);

            if (double.IsNaN(Convert.ToDouble(result)))
            {
                result = 0;
            }

            return result;
        }

        double ProposionalRata(double jumlahPembayaran, double jumlahbidang)
        {
            var result = Math.Round(jumlahPembayaran / jumlahbidang);

            if (double.IsNaN(Convert.ToDouble(result)))
            {
                result = 0;
            }

            return result;
        }

        double ProposionaHargaTotal(double hargaTotal, double jumlahPembayaran, double allHarga)
        {
            var result = Math.Round((jumlahPembayaran * hargaTotal) / allHarga);

            if (double.IsNaN(Convert.ToDouble(result)))
            {
                result = 0;
            }

            return result;
        }

        private double GetTotalLuasdiBayar(string key)
        {
            double result = 0;
            try
            {
                var persil = contextes.persils.FirstOrDefault(p => p.key == key);
                if (Convert.ToBoolean(persil.luasFix))
                {
                    (var luasdiBayar, var satuan) = (persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(persil.basic.current.luasSurat),
                        persil.basic.current.satuan == null ? 0 : Convert.ToDouble(persil.basic.current.satuan));

                    result = luasdiBayar * satuan;
                }
                else
                {
                    (var luasdiBayar, var satuan) = (persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar),
                        persil.basic.current.satuan == null ? 0 : Convert.ToDouble(persil.basic.current.satuan));

                    result = luasdiBayar * satuan;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        private Persil GetPersil(string key)
        {
            return contextpay.persils.FirstOrDefault(p => p.key == key);
        }

        bool ForEntries(Persil persil, user user, double luas, string reason)
        {
            //For Entries
            var last = persil.basic.entries.LastOrDefault();

            var item = new PersilBasic();
            item = persil.basic.current;
            item.luasDibayar = luas;
            item.reason = reason;

            var newEntries1 =
                new ValidatableEntry<PersilBasic>
                {
                    created = DateTime.Now,
                    en_kind = ChangeKind.Update,
                    keyCreator = user.key,
                    keyReviewer = user.key,
                    reviewed = DateTime.Now,
                    approved = true,
                    item = item
                };

            persil.basic.entries.Add(newEntries1);

            persil.basic.current = item;
            contextes.persils.Update(persil);
            contextes.SaveChanges();

            if (last != null && last.reviewed == null)
            {
                var item2 = new PersilBasic();
                item2 = last.item;
                item2.luasDibayar = luas;
                item2.reason = reason;

                var newEntries2 =
                    new ValidatableEntry<PersilBasic>
                    {
                        created = last.created,
                        en_kind = last.en_kind,
                        keyCreator = last.keyCreator,
                        keyReviewer = last.keyReviewer,
                        reviewed = last.reviewed,
                        approved = last.approved,
                        item = item2
                    };

                persil.basic.entries.Add(newEntries2);

                contextes.persils.Update(persil);
                contextes.SaveChanges();
            }

            return true;
        }

        bool ForEntriesSatuan(Persil persil, user user, double? satuan, double? satuanAkta)
        {
            //For Entries
            var last = persil.basic.entries.LastOrDefault();

            var item = new PersilBasic();
            item = persil.basic.current;
            item.satuan = satuan;
            item.satuanAkte = satuanAkta;

            var newEntries1 =
                new ValidatableEntry<PersilBasic>
                {
                    created = DateTime.Now,
                    en_kind = ChangeKind.Update,
                    keyCreator = user.key,
                    keyReviewer = user.key,
                    reviewed = DateTime.Now,
                    approved = true,
                    item = item
                };

            persil.basic.entries.Add(newEntries1);

            persil.basic.current = item;
            contextes.persils.Update(persil);
            contextes.SaveChanges();

            if (last != null && last.reviewed == null)
            {
                var item2 = new PersilBasic();
                item2 = last.item;
                item2.satuan = satuan;
                item2.satuanAkte = satuanAkta;

                var newEntries2 =
                    new ValidatableEntry<PersilBasic>
                    {
                        created = last.created,
                        en_kind = last.en_kind,
                        keyCreator = last.keyCreator,
                        keyReviewer = last.keyReviewer,
                        reviewed = last.reviewed,
                        approved = last.approved,
                        item = item2
                    };

                persil.basic.entries.Add(newEntries2);

                contextes.persils.Update(persil);
                contextes.SaveChanges();
            }

            return true;
        }

        (bool OK, string error) PraDealValidation(string keys, JenisBayar jnsBayar)
        {
            try
            {
                var pra = contextpay.GetDocuments(new PraValidation(), "praDeals",
                            "{'$unwind': '$details'}",
                            $"<$match: <'details.keyPersil' : <'$in': [{keys}]>>>".MongoJs(),
                            @"{'$project': {
                                            keyPersil : '$details.keyPersil',
                                            dealUTJ : '$details.dealUTJ'
                                            dealDP : '$details.dealDP',
                                            dealLunas : '$details.dealLunas',
                                            _id : 0}}"
                            ).ToList();

                var persils = contextpay.GetDocuments(new { key = "", IdBidang = "" }, "persils_v2",
                    $"<$match: <'key': <'$in' : [{keys}]>>>".MongoJs(),
                    "{'$project' : {key : '$key', IdBidang : '$IdBidang', _id : 0}}").ToList();


                var noDealYet = jnsBayar == JenisBayar.UTJ ? pra.Where(x => x.dealUTJ == null).ToList() :
                            jnsBayar == JenisBayar.DP ? pra.Where(x => x.dealDP == null).ToList() :
                            pra.Where(x => x.dealLunas == null).ToList();

                var find = noDealYet.Any();
                if (find)
                {
                    var IdBidangs = noDealYet.Join(persils, n => n.keyPersil, p => p.key, (n, p) => new { IdBidang = p.IdBidang }).ToArray();
                    var IdBidang = string.Join(',', IdBidangs.Select(k => $"{k.IdBidang}"));

                    return (false, $"Bidang {IdBidang} tidak dapat melakukan pembayaran {common.Helpers.JenisByr(jnsBayar)}, karena belum ada pengajuan dari menu PRA PEMBEBASAN");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return (false, ex.Message);
            }

        }

        public class MasterGroup
        {
            public string keyProject { get; set; }
            public string keyDesa { get; set; }
            public string group { get; set; }
        }
        public class GroupBidang
        {
            public string key { get; set; }
            public string _t { get; set; }
            public string IdBidang { get; set; }
            public StatusBidang? en_state { get; set; }
            public DateTime? deal { get; set; }
            public double? luasDibayar { get; set; }
            public double? luasSurat { get; set; }
            public double? luasUkur { get; set; }
            public bool? luasFix { get; set; }
            public string Pemilik { get; set; }
            public string AlasHak { get; set; }
            public double? satuan { get; set; }
            public double? total { get; set; }
            public string noPeta { get; set; }
            public string? keyPenampung { get; set; }
            public string? keyPTSK { get; set; }
            public string keyParent { get; set; }
            public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == key);

            public GroupBidangView toView2(LandropePayContext context, cmnItem[] ptsks)
            {
                var view = new GroupBidangView();
                var persil = this.persil(context);
                double luas = 0;

                var luasNIB = PersilHelper.GetLuasBayar(persil) == null ? 0 : Convert.ToDouble(PersilHelper.GetLuasBayar(persil));
                var luasSurat = persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(persil.basic.current.luasSurat);
                var luasDibayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                if (persil.luasFix != true)
                    luas = luasNIB == 0 ? luasDibayar : luasNIB;
                else
                    luas = luasDibayar;


                (view.key, view.IdBidang, view.en_state, view.deal, view.luasSurat, view.luasDibayar, view.satuan, view.total,
                    view.AlasHak, view.Pemilik, view.noPeta, view.keyPenampung, view.keyPTSK, view.ptsk) =
                    (key, IdBidang,
                    (Enum.TryParse<StatusBidang>(en_state.ToString(), out StatusBidang stp) ? stp : default),
                    deal, luasSurat, luas, satuan, (luas * satuan), AlasHak, Pemilik, noPeta, keyPenampung, keyPTSK, ptsks.FirstOrDefault(x => x.key == keyPTSK)?.name);

                return view;
            }
        }
        public class MasterPay
        {
            public string Tkey { get; set; }
            public string group { get; set; }
            public string keyProject { get; set; }
            public string keyDesa { get; set; }
            public string keyPTSK { get; set; }
            public string project { get; set; }
            public int noTahap { get; set; }
            public string key { get; set; }
            public string keyPersil { get; set; }
            public string instkey { get; set; }
            public JenisBayar jenisBayar { get; set; }
            public string? noMemo { get; set; }
            public double Jml { get; set; }
            public object tgl { get; set; }
            public string contactPerson { get; set; }
            public string noTlpCP { get; set; }
            public string tembusan { get; set; }
            public string memoSigns { get; set; }
            public DateTime? tglPenyerahan { get; set; }
            public string note { get; set; }
            public string memoTo { get; set; }
            public Giro[] giro { get; set; } = new Giro[0];
            public Reason[] reasons { get; set; } = new Reason[0];

            public BayarDtlView toView(LandropePayContext contextes, StaticCollection staticCollections, cmnItem[] ptsks, List<Persil> persils)
            {
                var view = new BayarDtlView();
                (double luas, double luasDibayar, double luasNIB, double luasSurat, string noPBT, string noNIB, string bidangs) = (0, 0, 0, 0, "", "", "");

                var persil = persils.FirstOrDefault(p => p.key == keyPersil);
                if (persil != null)
                {
                    (noPBT, noNIB) = PersilHelper.GetNomorPBT(persil);
                    luasSurat = persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(persil.basic.current.luasSurat);
                    luasDibayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                    if (persil.luasFix != true)
                    {
                        luasNIB = PersilHelper.GetLuasBayar(persil);
                        luas = luasNIB == 0 ? luasDibayar : luasNIB;
                    }
                    else
                        luas = luasDibayar;
                }
                else
                {
                    luas = GetTotalLuas(contextes, Tkey, key);
                }

                (string _tembusan, string _memoSign, string _contactPerson, string _noTlpCP) =
                   (string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan,
                    string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns,
                    string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson,
                    string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP);

                if (string.IsNullOrEmpty(keyPersil))
                    bidangs = "...";
                else if (!string.IsNullOrEmpty(keyPersil))
                    if (persil != null)
                        bidangs = persil.IdBidang;
                    else
                        bidangs = "...";

                var lstGiro = new List<GiroCore>();
                if (giro.Count() > 0)
                {
                    for (int i = 0; i < giro.Count(); i++)
                    {
                        var girocore = new GiroCore();
                        girocore.key = giro[i].key;
                        girocore.Jenis = giro[i].jenis;
                        girocore.BankPenerima = giro[i].bankPenerima;
                        girocore.NamaPenerima = giro[i].namaPenerima;
                        girocore.AccountPenerima = giro[i].accPenerima;
                        girocore.Nominal = giro[i].nominal;
                        lstGiro.Add(girocore);
                    }
                }

                var lstReason = new List<ReasonCore>();
                if (reasons.Count() > 0)
                    foreach (var item in reasons)
                    {
                        var reason = new ReasonCore
                        {
                            tanggal = item.tanggal,
                            description = item.description,
                            flag = item.flag,
                            keyCreator = item.keyCreator,
                            privs = item.privs,
                            state = item.state,
                            state_ = item.state.AsStatus(),
                            creator = contextes.users.FirstOrDefault(y => y.key == item.keyCreator)?.FullName
                        };

                        lstReason.Add(reason);
                    }

                (view.Tkey,
                    view.nomorTahap,
                    view.key,
                    view.keyPersil,
                    view.noMemo,
                    view.instkey,
                    view.idBidang,
                    view.jenisBayar,
                    view.Jml,
                    view.NoPBT,
                    view.Pemilik,
                    view.AlasHak,
                    view.LuasSurat,
                    view.LuasDibayar,
                    view.group,
                    view.keyDesa,
                    view.keyProject,
                    view.tglPenyerahan,
                    view.ContactPerson,
                    view.noTlpCP,
                    view.tembusan,
                    view.note,
                    view.memoSigns,
                    view.memoTo,
                    view.giro,
                    view.reasons,
                    view.ptsk
                    )
                    =
                        (Tkey,
                        noTahap,
                        key,
                        keyPersil,
                        noMemo,
                        instkey,
                        bidangs,
                        (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                        Jml, noPBT == string.Empty ? "..." : noPBT,
                        persil == null ? "..." : persil.basic.current.pemilik,
                        persil == null ? "..." : persil.basic.current.surat.nomor,
                        luasSurat,
                        luas,
                        group,
                        keyDesa,
                        keyProject,
                        tglPenyerahan,
                        _contactPerson,
                        _noTlpCP,
                        _tembusan,
                        note,
                        _memoSign,
                        memoTo,
                        lstGiro.ToArray(),
                        lstReason.ToArray(),
                        ptsks.FirstOrDefault(x => x.key == keyPTSK)?.name
                        );

                return view;
            }

            public BayarDtlViewExt toViewExt(LandropePayContext contextes, List<Persil> persils, (string inskey, ToDoState status, DateTime createDate)[] instances)
            {
                var view = new BayarDtlViewExt();
                double luas = 0;
                double luasDibayar = 0;
                double luasNIB = 0;
                double luasSurat = 0;
                string noPBT = string.Empty;
                string noNIB = string.Empty;

                var persil = persils.FirstOrDefault(p => p.key == keyPersil);
                var main = instances.FirstOrDefault(x => x.inskey == this.instkey);
                var LastStatus = main.status.AsStatus();
                var createdDate = main.createDate;
                (var project, var desa) = contextes.GetVillage(keyDesa);

                if (persil != null)
                {
                    (noPBT, noNIB) = PersilHelper.GetNomorPBT(persil);
                    luasNIB = PersilHelper.GetLuasBayar(persil);
                    luasSurat = persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(persil.basic.current.luasSurat);
                    luasDibayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                    if (persil.luasFix != true)
                        luas = luasNIB == 0 ? luasDibayar : luasNIB;
                    else
                        luas = luasDibayar;
                }
                else
                {
                    luas = GetTotalLuas(contextes, Tkey);
                }

                var bidangs = string.Empty;

                if (string.IsNullOrEmpty(keyPersil))
                    bidangs = "...";
                else if (!string.IsNullOrEmpty(keyPersil))
                    if (persil != null)
                        bidangs = persil.IdBidang;
                    else
                        bidangs = "...";

                (view.Tkey,
                 view.nomorTahap,
                 view.key,
                 view.keyPersil, view.noMemo,
                 view.instkey, view.idBidang,
                 view.jenisBayar,
                 view.Jml, view.NoPBT, view.Pemilik, view.AlasHak, view.LuasSurat, view.LuasDibayar, view.group, view.keyDesa, view.status, view.tgl, view.desa, view.project) =
                        (Tkey, noTahap, key,
                        keyPersil,
                        noMemo, instkey,
                        bidangs,
                        (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                        Jml, noPBT == string.Empty ? "..." : noPBT,
                        persil == null ? "..." : persil.basic.current.pemilik,
                        persil == null ? "..." : persil.basic.current.surat.nomor,
                        luasSurat, luas, group, keyDesa, LastStatus, createdDate, desa.identity, project.identity
                        );

                return view;
            }

            static double GetTotalLuas(LandropePayContext pc, string key)
            {
                (double result, double luasNIB, double totalLuas, double luasDibayar) = (0, 0, 0, 0);

                try
                {
                    var bayars = pc.bayars.FirstOrDefault(p => p.key == key);
                    if (bayars == null)
                        return result;

                    if (bayars.bidangs != null)
                    {
                        var keys = string.Join(',', bayars.bidangs.Select(k => $"'{k.keyPersil}'"));
                        var persils = pc.GetDocuments(new { key = "", luasfix = true, basic = new PersilBasic() }, "persils_v2",
                        $@"<$match: <key:<$in:[{keys}]>>>".Replace("<", "{").Replace(">", "}"),
                        $@"<$project:<key: '$key', luasfix : '$luasfix', basic:'$basic.current',_id: 0>>".Replace("<", "{").Replace(">", "}")).ToList();

                        result = persils.Select(x => new
                        {
                            luasDibayar = x.luasfix == true ? (x.basic?.luasDibayar == null ? 0 : Convert.ToDouble(x.basic?.luasDibayar))
                            : PersilHelper.GetLuasBayar(pc.persils.FirstOrDefault(p => p.key == x.key))

                        }).Sum(x => x.luasDibayar);

                        //foreach (var item in bayars.bidangs)
                        //{
                        //    var persil = pc.persils.FirstOrDefault(p => p.key == item.keyPersil);

                        //    luasNIB = PersilHelper.GetLuasBayar(persil);
                        //    luasDibayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                        //    if (persil.luasFix != true)
                        //        totalLuas = luasNIB == 0 ? luasDibayar : luasNIB;
                        //    else
                        //        totalLuas = luasDibayar;

                        //    result = result + totalLuas;
                        //}
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return result;
            }

            static double GetTotalLuas(LandropePayContext pc, string Tkey, string dkey)
            {
                (double result, double luasNIB, double totalLuas, double luasDibayar, string keys) = (0, 0, 0, 0, "");

                try
                {
                    var bayars = pc.bayars.FirstOrDefault(p => p.key == Tkey);
                    if (bayars == null)
                        return result;
                    var dtl = bayars.details.FirstOrDefault(x => x.key == dkey);
                    var deposit = bayars.deposits.FirstOrDefault(x => x.key == dkey);

                    if (bayars.bidangs != null)
                    {
                        if (dtl != null)
                            keys = string.Join(',', dtl.subdetails.Select(k => $"'{k.keyPersil}'"));
                        else if (deposit != null)
                            keys = string.Join(',', deposit.subdetails.Select(k => $"'{k.keyPersil}'"));
                        else
                            return result;

                        var persils = pc.GetDocuments(new { key = "", luasfix = true, basic = new PersilBasic() }, "persils_v2",
                        $@"<$match: <key:<$in:[{keys}]>>>".Replace("<", "{").Replace(">", "}"),
                        $@"<$project:<key: '$key', luasfix : '$luasfix', basic:'$basic.current',_id: 0>>".Replace("<", "{").Replace(">", "}")).ToList();

                        result = persils.Select(x => new
                        {
                            luasDibayar = x.basic?.luasDibayar == null ? 0 : Convert.ToDouble(x.basic?.luasDibayar)

                        }).Sum(x => x.luasDibayar);

                        //foreach (var item in bayars.bidangs)
                        //{
                        //    var persil = pc.persils.FirstOrDefault(p => p.key == item.keyPersil);

                        //    luasNIB = PersilHelper.GetLuasBayar(persil);
                        //    luasDibayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

                        //    if (persil.luasFix != true)
                        //        totalLuas = luasNIB == 0 ? luasDibayar : luasNIB;
                        //    else
                        //        totalLuas = luasDibayar;

                        //    result = result + totalLuas;
                        //}
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                return result;
            }
        }
        public class DPSettings
        {
            public JenisAlasHak enJenis { get; set; }
            public int percentage { get; set; }
        }

       
        public class PraValidation
        {
            public string keyPersil { get; set; }
            public DateTime? dealUTJ { get; set; }
            public DateTime? dealDP { get; set; }
            public DateTime? dealLunas { get; set; }
        }

        [HttpGet("BayarPendingAccounting")]
        public async Task<IActionResult> GetPembayaranPendingAccounting()
        {
            try
            {
                var summary = context.GetDocuments(new BayarPendingSumView(), "bayars",
                    "{$unwind : '$details' }",
                    @"{ $lookup : {
                        from : 'graphables',
                        let : { instKey : '$details.instkey' },
                        pipeline : [
                            { $match:
                                { $expr:
            			            { $and : [
            			                { $in : ['$closed', [false, null]] },
                                        { $eq:['$key','$$instKey'] }
            			            ]}
                                }
                            },
                            { $unwind : '$Core.nodes' },
                            { $match : 
                                {   
                                    $expr : {
                                        $and : 
                                        [
                                            { $in : ['$Core.nodes._state', [49,51]] },
                                            { $eq : ['$Core.nodes.Active', true ] }
                                        ]
                                }}
                            }
                        ],
                        as : 'dataGraph'
                    }}",
                    @" {
                        $match: {
                            'dataGraph' : {$size: 1}
                        }
                    } ",
                    @"  { $lookup:{
                        from:'maps',
                        let: { keyProject : ""$keyProject"", keyDesa : ""$keyDesa""},
                        pipeline : [
                            {$unwind:'$villages'},
                            {$project:{
                                keyProject:'$key',
                                keyDesa:'$villages.key',
                                namaProject:'$identity',
                                namaDesa:'$villages.identity'
                            }},
                            { $match:
                                 { $expr:
                                    { $and:[
                                        { $eq: [ ""$keyProject"",  ""$$keyProject"" ] },
                                        { $eq: [ ""$keyDesa"",  ""$$keyDesa"" ] }
                                    ]}
                                 }
                            }
                       ], 
                        as:'maps'
                    }}",
                    @"  { $match :
                            {'$expr': {$gte: [ {$size:'$maps'},0] }}
                    }",
                    @"  {
                      $lookup: {
                             from :""masterdatas"",
                             let : { keyPTSK : ""$keyPTSK""},
                             pipeline : [
                                 {$match: 
                                    {$expr: 
                                    {
                                        $and : [{$eq:[""$key"", ""$$keyPTSK""]}, {$eq:[""$_t"", ""ptsk""]}]
                                    }
                                    }
                                 }
                             ],
                             as: ""ptsks""
                           }  
                    }",
                    @"  { $addFields : {
                        jenisBayar : {
			                $switch: {
          		                branches: [
         			                { case: { $eq: [ '$details.jenisBayar', 1 ] }, then: ""UTJ"" },
                                    { case: { $eq: [ '$details.jenisBayar', 2 ] }, then: ""DP"" },
                                    { case: { $eq: [ '$details.jenisBayar', 3 ] }, then: ""Pelunasan"" }
                                  ],
                                  default: ""Unknown""
                               }
		                    }
                        }
	                } ",
                    @"{ $lookup: {
	                   from: ""static_collections"",
	                   let: { stateNum : {$arrayElemAt: [""$dataGraph.Core.nodes._state"",0]} },
	                   pipeline : [
	                    { $unwind: '$detail'},
	                    { $match : 
	                        { $expr  : 
	                            { $and : 
	                                [
	                                 {$eq:[""$detail.state"", ""$$stateNum""]},
	                                 {$eq:[""$_t"", ""ReminderPembayaran""]}
	                                ]    
                                }
	                        }
	                    }],
	                   as: ""emailTo""
	                 }}",
                    @" {
                        $project: 
                        {
                            _id : 0,
                            nomorTahap : ""$nomorTahap"",
                            namaProject : {$arrayElemAt: [""$maps.namaProject"",0]},
                            namaDesa : {$arrayElemAt: [""$maps.namaDesa"",0]},
                            PTSK : {$arrayElemAt: [""$ptsks.identifier"",0]},
                            jenisBayar : 1,
                            totalBayar : ""$details.Jumlah"",
                            details : ""$details.subdetails"",
                            state : {$arrayElemAt: [""$dataGraph.Core.nodes._state"", 0]},
                            to : {$arrayElemAt: ['$emailTo.detail.to',0] }
                        }
                    } "
                );

                byte[] resultByte = new byte[0];
                string fileName = @"PembayaranPendingAccounting_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";

                if (summary != null)
                {
                    if (summary.Count > 0)
                    {
                        summary.ForEach(a => a.totalBidang = a.details.Count());
                        var dtPersil = new List<BayarPendingSumPersilView>();
                        summary.ForEach(a => dtPersil.AddRange(a.details.ToList()));

                        var details = summary.SelectMany(x => x.details, (y, z) => new
                        {
                            nomorTahap = y.nomorTahap,
                            project = y.namaProject,
                            desa = y.namaDesa,
                            ptsk = y.PTSK,
                            jenisBayar = y.jenisBayar,
                            keyPersil = z.keyPersil,
                            jumlahBayar = z.Jumlah,
                            state = y.state
                        });

                        var keys = details.Select(x => x.keyPersil).Distinct();
                        var persils = contextpay.persils.Query(x => keys.Contains(x.key));

                        var finalDetails = details.Join(persils, d => d.keyPersil, p => p.key, (d, p) => new BayarPendingDtView
                        {
                            idBidang = p.IdBidang,
                            alasHak = p.basic.current.surat.nomor,
                            nomorPeta = p.basic.current.noPeta,
                            nomorTahap = d.nomorTahap,
                            namaProject = d.project,
                            namaDesa = d.desa,
                            PTSK = d.ptsk,
                            jenisBayar = d.jenisBayar,
                            totalBayarPersil = d.jumlahBayar,
                            state = d.state
                        }).ToList();

                        string[] skip = new string[] { "state", "to", "details" };

                        DataSet sheetSum = summary.Where(a => a.state == ToDoState.accountingApproval_).ToList().ToDataSet(skip);

                        DataSet sheetDetail = finalDetails.Where(a => a.state == ToDoState.accountingApproval_).ToList().ToDataSet(skip);

                        DataSExcelSheet[] excelDataSheet = new DataSExcelSheet[] {
                                                                new DataSExcelSheet { dataS = sheetSum, sheetName = "Summary" },
                                                                new DataSExcelSheet { dataS = sheetDetail, sheetName = "Detail" }};

                        resultByte = DataSetHelper.ExportMultipleDataSetToExcelByte(excelDataSheet);
                        var base64 = Convert.ToBase64String(resultByte);

                        EmailStructure email = new EmailStructure {
                            Receiver = (summary.Where(a => a.state == ToDoState.accountingApproval_).Select(b => b.to).FirstOrDefault()).Split(";"),
                            Subject = "[Pembayaran Alert: Pembayaran Pending di Accounting]",
                            Body = @"<h4>Dear All,</h4>
                                    <p>Berikut adalah detail pembayaran yang masih pending. Harap untuk segera diselesaikan.</p>
                                    <p>&nbsp; <br></p>
                                    <p align='left'>Regards,</p>
                                    <p align='left'>Alert Notification Pembayaran</p>
                                    ",
                            ContentType = FileType.Excel,
                            Attachment = new List<EmailAttachment> { new EmailAttachment { FileBase64 = base64, FileName = fileName } }
                        };

                        string url;
                        var appsets = Config.AppSettings;
                        if (!appsets.TryGet("sendEmail:url", out url))
                            return new InternalErrorResult("Invalid url. Cek kembali file pengaturan anda");
                        
                        var myContent = JsonConvert.SerializeObject(email);
                        var content = new StringContent(myContent, Encoding.UTF8, "application/json");
                        content.Headers.ContentType.CharSet = "";

                        HttpClient client = new HttpClient();
                        var response = await client.PostAsync(url + "api/mailsender/sendemail", content);
                        if (response.IsSuccessStatusCode)
                            return Ok();
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpPost("BayarPendingKasir")]
        public async Task<IActionResult> GetPembayaranPendingKasir()
        {
            try
            {
                var summary = context.GetDocuments(new BayarPendingSumView(), "bayars",
                    "{$unwind : '$details' }",
                    @"{ $lookup : {
                        from : 'graphables',
                        let : { instKey : '$details.instkey' },
                        pipeline : [
                            { $match:
                                { $expr:
            			            { $and : [
            			                { $in : ['$closed', [false, null]] },
                                        { $eq:['$key','$$instKey'] }
            			            ]}
                                }
                            },
                            { $unwind : '$Core.nodes' },
                            { $match : 
                                {   
                                    $expr : {
                                        $and : 
                                        [
                                            { $eq : ['$Core.nodes._state', 51] },
                                            { $eq : ['$Core.nodes.Active', true ] }
                                        ]
                                }}
                            }
                        ],
                        as : 'dataGraph'
                    }}",
                    @" {
                        $match: {
                            'dataGraph' : {$size: 1}
                        }
                    } ",
                    @"  { $lookup:{
                        from:'maps',
                        let: { keyProject : ""$keyProject"", keyDesa : ""$keyDesa""},
                        pipeline : [
                            {$unwind:'$villages'},
                            {$project:{
                                keyProject:'$key',
                                keyDesa:'$villages.key',
                                namaProject:'$identity',
                                namaDesa:'$villages.identity'
                            }},
                            { $match:
                                 { $expr:
                                    { $and:[
                                        { $eq: [ ""$keyProject"",  ""$$keyProject"" ] },
                                        { $eq: [ ""$keyDesa"",  ""$$keyDesa"" ] }
                                    ]}
                                 }
                            }
                       ], 
                        as:'maps'
                    }}",
                    @"  { $match :
                            {'$expr': {$gte: [ {$size:'$maps'},0] }}
                    }",
                    @"  {
                      $lookup: {
                             from :""masterdatas"",
                             let : { keyPTSK : ""$keyPTSK""},
                             pipeline : [
                                 {$match: 
                                    {$expr: 
                                    {
                                        $and : [{$eq:[""$key"", ""$$keyPTSK""]}, {$eq:[""$_t"", ""ptsk""]}]
                                    }
                                    }
                                 }
                             ],
                             as: ""ptsks""
                           }  
                    }",
                    @"  { $addFields : {
                        jenisBayar : {
			                $switch: {
          		                branches: [
         			                { case: { $eq: [ '$details.jenisBayar', 1 ] }, then: ""UTJ"" },
                                    { case: { $eq: [ '$details.jenisBayar', 2 ] }, then: ""DP"" },
                                    { case: { $eq: [ '$details.jenisBayar', 3 ] }, then: ""Pelunasan"" }
                                  ],
                                  default: ""Unknown""
                               }
		                    }
                        }
	                } ",
                    @"{ $lookup: {
	                   from: ""static_collections"",
	                   let: { stateNum : {$arrayElemAt: [""$dataGraph.Core.nodes._state"",0]} },
	                   pipeline : [
	                    { $unwind: '$detail'},
	                    { $match : 
	                        { $expr  : 
	                            { $and : 
	                                [
	                                 {$eq:[""$detail.state"", ""$$stateNum""]},
	                                 {$eq:[""$_t"", ""ReminderPembayaran""]}
	                                ]    
                                }
	                        }
	                    }],
	                   as: ""emailTo""
	                 }}",
                    @" {
                        $project: 
                        {
                            _id : 0,
                            nomorTahap : ""$nomorTahap"",
                            namaProject : {$arrayElemAt: [""$maps.namaProject"",0]},
                            namaDesa : {$arrayElemAt: [""$maps.namaDesa"",0]},
                            PTSK : {$arrayElemAt: [""$ptsks.identifier"",0]},
                            jenisBayar : 1,
                            totalBayar : ""$details.Jumlah"",
                            details : ""$details.subdetails"",
                            state : {$arrayElemAt: [""$dataGraph.Core.nodes._state"", 0]},
                            to : {$arrayElemAt: ['$emailTo.detail.to',0] }
                        }
                    } "
                );

                byte[] resultByte = new byte[0];
                string fileName = @"PembayaranPendingKasir_" + DateTime.Now.ToString("yyyyMMdd") + "`.xlsx";

                if (summary != null)
                {
                    if (summary.Count > 0)
                    {
                        summary.ForEach(a => a.totalBidang = a.details.Count());
                        var dtPersil = new List<BayarPendingSumPersilView>();
                        summary.ForEach(a => dtPersil.AddRange(a.details.ToList()));

                        var details = summary.SelectMany(x => x.details, (y, z) => new
                        {
                            nomorTahap = y.nomorTahap,
                            project = y.namaProject,
                            desa = y.namaDesa,
                            ptsk = y.PTSK,
                            jenisBayar = y.jenisBayar,
                            keyPersil = z.keyPersil,
                            jumlahBayar = z.Jumlah,
                            state = y.state
                        });

                        var keys = details.Select(x => x.keyPersil).Distinct();
                        var persils = contextpay.persils.Query(x => keys.Contains(x.key));

                        var finalDetails = details.Join(persils, d => d.keyPersil, p => p.key, (d, p) => new BayarPendingDtView
                        {
                            idBidang = p.IdBidang,
                            alasHak = p.basic.current.surat.nomor,
                            nomorPeta = p.basic.current.noPeta,
                            nomorTahap = d.nomorTahap,
                            namaProject = d.project,
                            namaDesa = d.desa,
                            PTSK = d.ptsk,
                            jenisBayar = d.jenisBayar,
                            totalBayarPersil = d.jumlahBayar,
                            state = d.state
                        }).ToList();

                        string[] skip = new string[] { "state", "to", "details" };

                        DataSet sheetSum = summary.Where(a => a.state == ToDoState.reviewAndAcctgApproved_).ToList().ToDataSet(skip);

                        DataSet sheetDetail = finalDetails.Where(a => a.state == ToDoState.reviewAndAcctgApproved_).ToList().ToDataSet(skip);

                        DataSExcelSheet[] excelDataSheet = new DataSExcelSheet[] { new DataSExcelSheet { dataS = sheetSum, sheetName = "Summary" },
                                                                                   new DataSExcelSheet { dataS = sheetDetail, sheetName = "Detail" }};

                        resultByte = DataSetHelper.ExportMultipleDataSetToExcelByte(excelDataSheet);
                        var base64 = Convert.ToBase64String(resultByte);
                        
                        EmailStructure email = new EmailStructure
                        {
                            Receiver = (summary.Where(a => a.state == ToDoState.reviewAndAcctgApproved_).Select(b => b.to).FirstOrDefault()).Split(";"),
                            Subject = "[Pembayaran Alert: Pembayaran Pending di Kasir]",
                            Body = @"<h4>Dear All,</h4>
                                    <p>Berikut adalah detail pembayaran yang masih pending. Harap untuk segera diselesaikan.</p>
                                    <p>&nbsp; <br></p>
                                    <p align='left'>Regards,</p>
                                    <p align='left'>Alert Notification Pembayaran</p>
                                    ",
                            ContentType = FileType.Excel,
                            Attachment = new List<EmailAttachment> { new EmailAttachment { FileBase64 = base64, FileName = fileName } }
                        };

                        string url;
                        var appsets = Config.AppSettings;
                        if (!appsets.TryGet("sendEmail:url", out url))
                            return new InternalErrorResult("Invalid url. Cek kembali file pengaturan anda");

                        var myContent = JsonConvert.SerializeObject(email);
                        var content = new StringContent(myContent, Encoding.UTF8, "application/json");
                        content.Headers.ContentType.CharSet = "";

                        HttpClient client = new HttpClient();
                        var response = await client.PostAsync(url + "api/mailsender/sendemail", content);
                        if (response.IsSuccessStatusCode)
                            return Ok();
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}

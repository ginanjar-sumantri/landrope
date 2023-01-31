#define test
using graph.mod;
using landrope.mod;
using landrope.mod2;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Tracer;
using landrope.common;
using MongoDB.Driver;
using GraphConsumer;
using GenWorkflow;
using flow.common;
using APIGrid;
using landrope.api3.Models;
using auth.mod;
using landrope.consumers;
using mongospace;
using BundlerConsumer;
using landrope.documents;
using MongoDB.Bson;
using landrope.mod3;
using DynForm.shared;
using Newtonsoft.Json;
using landrope.mod3.shared;
using FileRepos;
using System.Threading.Tasks;
using landrope.mod4.classes;
using landrope.mod4;
using System.Collections.ObjectModel;
using landrope.hosts;
using landrope.material;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using HttpAccessor;
using core.helpers;
using System.Text;
using landrope.mod.cross;
using MailSender;
using MailSender.Models;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using System.Linq.Expressions;
using ClosedXML.Excel;
using System.Diagnostics;
using System.Data;
using DocumentFormat.OpenXml.Drawing;
using System.IO;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace landrope.api3.Controllers
{
    [Route("api/prabebas")]
    [ApiController]
    public class PraPembebasanController : Controller
    {
        IServiceProvider services;
        FileGrid fgrid;
#if test
        //GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        GraphHostConsumer ghost;
#else
        GraphHost.GraphHostSvc graphost;
        
#endif
        //BundlerHostConsumer bhost => ContextService.services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
        BundlerHostConsumer bhost;
        LandropePlusContext contextplus = Contextual.GetContextPlus();
        LandropePayContext contextpay = Contextual.GetContextPay();
        IConfiguration configuration;
        static GraphContext gContext;


        private LandropeCrossContext contextCross = Contextual.GetContextCross();

        public PraPembebasanController(IServiceProvider service, IConfiguration iconfig)
        {
            this.services = service;
            fgrid = services.GetService<FileGrid>();
            contextplus = services.GetService<LandropePlusContext>();
            configuration = iconfig;
#if test
            ghost = HostServicesHelper.GetGraphHostConsumer(services);
#else
                    graphost = HostServicesHelper.GetGraphHost(services);
#endif
            contextCross = service.GetService<LandropeCrossContext>();
            bhost = HostServicesHelper.GetBundlerHostConsumer(services);
            MakeConnectionGraph();
        }

        /// <summary>
        /// Get List Pra Pembebasan
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,MAP_REVIEW,MAP_FULL,PAY_MKT")]
        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] string tfind, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm = DateTime.Now;
                var user = contextplus.FindUser(token);

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();
                var creator = priv.Contains("PAY_MKT");
                var review = priv.Intersect(new[] { "MAP_FULL", "MAP_REVIEW", "PAY_PRA_REVIEW" }).Any();
                var monitor = priv.Intersect(new[] { "PAY_PRA_TRANS" }).Any() && !creator;

#if test
                var Gs1 = ghost.List(user).GetAwaiter().GetResult() ?? new GraphTree[0];
#else
                var Gs1 = graphost.List(user).ToArray() ?? new GraphTree[0];
#endif

                var Gs = Gs1.Where(x => x.subs.Any()).Select(x => (x.main,
                                                                   inmain: (x.subs.FirstOrDefault(s => s.instance.key == x.main.key) ?? new GraphSubTree(x.main, new GraphTreeNode[0]))?.nodes,
                                                                   insubs: (from xs in x.subs
                                                                            from s in xs.nodes
                                                                            where xs.instance.key != x.main.key
                                                                            select xs
                                                                           ).ToArray()
                                                             ));

                var strict = review && !monitor ? true : false;

                var workers = contextplus.workers.All();
                var locations = contextplus.GetVillages();
                var notarists = contextplus.notarists.Query(x => x.invalid != true);
                var pradeals = contextplus.praDeals.Query(x => x.key != "templatePraDeals").ToList();

                var data = new List<PraBebasView>();

                if (string.IsNullOrEmpty(tfind))
                {
                    data = pradeals.Select(x => new PraBebasView
                    {
                        key = x.key,
                        NoRequest = x.identifier,
                        keyReff = x.keyReff,
                        InstKey = x.instancesKey,
                        KeyManager = x.manager,
                        Manager = workers.FirstOrDefault(w => w.key == x.manager)?.FullName,
                        KeySales = x.sales,
                        Sales = workers.FirstOrDefault(w => w.key == x.sales)?.FullName,
                        KeyMediator = x.mediator,
                        Mediator = workers.FirstOrDefault(w => w.key == x.mediator)?.FullName,
                        tanggalProses = Convert.ToDateTime(x.tanggalProses).ToLocalTime(),
                        luasDeal = x.luasDeal,
                        Created = Convert.ToDateTime(x.created).ToLocalTime()
                    }).ToList();
                }
                else
                {
                    var keyPersils = pradeals.SelectMany(x => x.details).Select(x => x.keyPersil).Where(x => x != null);
                    var persils = contextpay.persils.Query(x => keyPersils.Contains(x.key)).Select(x => new { key = x.key, IdBidang = x.IdBidang }).ToList();

                    var xdata = pradeals.SelectMany(x => x.details, (y, z) => new
                    {
                        pra = y,
                        mng = workers.FirstOrDefault(w => w.key == y.manager)?.FullName,
                        sls = workers.FirstOrDefault(w => w.key == y.sales)?.FullName,
                        mdr = workers.FirstOrDefault(w => w.key == y.mediator)?.FullName,
                        nts = notarists.FirstOrDefault(n => n.key == y.notaris)?.identifier,
                        detail = z,
                        IdBidang = persils.FirstOrDefault(x => x.key == z.keyPersil)?.IdBidang
                    });

                    var searching = xdata.Where(x => (x.detail.alasHak ?? "").Contains(tfind)
                                                     || (x.detail.pemilik ?? "").Contains(tfind)
                                                     || (x.detail.alias ?? "").Contains(tfind)
                                                     || (x.detail.group ?? "").Contains(tfind)
                                                     || (x.IdBidang ?? "").Contains(tfind)
                                                     || (x.detail.desa ?? "").Contains(tfind)
                                                     || (x.pra.identifier ?? "").Contains(tfind)
                                                     || (x.mng ?? "").Contains(tfind)
                                                     || (x.sls ?? "").Contains(tfind)
                                                     || (x.mdr ?? "").Contains(tfind)
                                                     || (x.nts ?? "").Contains(tfind)).Select(x => x.pra.key).Distinct().ToList();

                    data = pradeals.Where(x => searching.Contains(x.key)).Select(x => new PraBebasView
                    {
                        key = x.key,
                        NoRequest = x.identifier,
                        keyReff = x.keyReff,
                        InstKey = x.instancesKey,
                        KeyManager = x.manager,
                        Manager = workers.FirstOrDefault(w => w.key == x.manager)?.FullName,
                        KeySales = x.sales,
                        Sales = workers.FirstOrDefault(w => w.key == x.sales)?.FullName,
                        KeyMediator = x.mediator,
                        Mediator = workers.FirstOrDefault(w => w.key == x.mediator)?.FullName,
                        tanggalProses = Convert.ToDateTime(x.tanggalProses).ToLocalTime(),
                        luasDeal = x.luasDeal,
                        Created = Convert.ToDateTime(x.created).ToLocalTime()
                    }).ToList();
                }

                #region huft banget
                //List<string> stages = new()
                //{
                //    "{$match: {key:{$ne:'templatePraDeals'}}}",
                //    "{$lookup:{ from:'workers', localField: 'manager', foreignField: 'key', as : 'mng'}}",
                //    "{$unwind:{ path:'$mng', preserveNullAndEmptyArrays: true}}",

                //    "{$lookup:{ from:'workers', localField: 'sales', foreignField: 'key', as : 'sls'}}",
                //    "{$unwind:{ path:'$sls', preserveNullAndEmptyArrays: true}}",

                //    "{$lookup:{ from:'workers', localField: 'mediator', foreignField: 'key', as : 'med'}}",
                //    "{$unwind:{ path:'$med', preserveNullAndEmptyArrays: true}}",

                //    @"{$project : {
                //        _id: 0,
                //        key: 1,
                //        NoRequest: '$identifier',
                //        keyReff: '$keyReff',
                //        InstKey: '$instancesKey',
                //        KeyManager: '$manager',
                //        Manager: '$mng.FullName',
                //        KeySales: '$sales',
                //        Sales: '$sls.FullName',
                //        KeyMediator: '$mediator',
                //        Mediator: '$med.FullName',
                //        tanggalProses : '$tanggalProses',
                //        luasDeal: '$luasDeal',
                //        Created: '$created'
                //    }}"
                //};
                //if (!string.IsNullOrEmpty(tfind))
                //{
                //    tfind = tfind.ToEscape();
                //    List<string> findQueries = new()
                //    {
                //        "{$match: {key:{$ne:'templatePraDeals'}}}",
                //        "{$unwind: '$details'}",
                //        "{$lookup: { from: 'persils_v2', localField: 'details.keyPersil', foreignField: 'key', as : 'persils'}}",
                //        "{$unwind: { path: '$persils', preserveNullAndEmptyArrays: true}}",
                //       @"{$lookup: {from: 'maps',let: { key: '$persils.basic.current.keyDesa'},
                //          pipeline:[{$unwind: '$villages'},
                //                   {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                //                   {$project: {key: '$villages.key', 
                //                   identity: '$villages.identity'} }], 
                //        as:'desas'}}",
                //       "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",

                //       "{$lookup:{ from: 'workers', localField: 'manager', foreignField: 'key', as : 'mng'}}",
                //       "{$unwind: { path: '$mng', preserveNullAndEmptyArrays: true} }",
                //       "{$lookup: { from: 'workers', localField: 'sales', foreignField: 'key', as : 'sls'} }",
                //       "{$unwind: { path: '$sls', preserveNullAndEmptyArrays: true} }",
                //       "{$lookup: { from: 'workers', localField: 'mediator', foreignField: 'key', as : 'med'}}",
                //       "{$unwind: { path: '$med', preserveNullAndEmptyArrays: true}}",

                //      @"{$lookup: { from: 'masterdatas', 
                //                    let: {keyNotaris: '$details.notaris'}, 
                //                    pipeline: [   
                //                        {$match: {$and : [{$expr: {$eq:['$key','$$keyNotaris']}}, {$expr: {$eq:['$_t','notaris']}} ]}},
                //                        {$project : {_id : 0, identifier : 1}}
                //                    ],
                //                    as: 'notaris'}}",
                //      "{$unwind: { path: '$notaris', preserveNullAndEmptyArrays: true}}",

                //      "{$addFields: {luasDeal: {$toString: '$luasDeal'}}}",
                //      "{$addFields: {luasSurat: {$toString : '$details.luasSurat'}}}",
                //       @$"<
                //            $match: <$or : [
                //                <'details.alasHak': /{tfind}/i>,
                //                <'details.pemilik': /{tfind}/i>,
                //                <'details.alias': /{tfind}/i>,
                //                <'details.group': /{tfind}/i>,
                //                <'details.keyPersil': /{tfind}/i>,
                //                <'persils.IdBidang': /{tfind}/i>,
                //                <'details.desa': /{tfind}/i>,
                //                <'details.hargaSatuan': /{tfind}/i>,
                //                <'desas.identity': /{tfind}/i>,
                //                <'luasSurat': /{tfind}/i>,
                //                <'identifier': /{tfind}/i>,
                //                <'mng.FullName': /{tfind}/i>,
                //                <'sls.FullName': /{tfind}/i>,
                //                <'med.FullName': /{tfind}/i>,
                //                <'tanggalProses': /{tfind}/i>,
                //                <'created': /{tfind}/i>,
                //                <'notaris.identifier': /{tfind}/i>,
                //                <'luasDeal': /{tfind}/i>
                //            ]>
                //        >".MongoJs(),
                //      @"{$project : {
                //            _id: 0,
                //            key: 1,
                //            NoRequest: '$identifier',
                //            keyReff: '$keyReff',
                //            InstKey: '$instancesKey',
                //            KeyManager: '$manager',
                //            Manager: '$mng.FullName',
                //            KeySales: '$sales',
                //            Sales: '$sls.FullName',
                //            KeyMediator: '$mediator',
                //            Mediator: '$med.FullName',
                //            tanggalProses : '$tanggalProses',
                //            luasDeal: '$luasDeal', 
                //            Created: '$created'
                //        }}",
                //      @"{$group: {_id :{
                //            key: '$key',
                //            NoRequest: '$NoRequest',
                //            keyReff: '$keyReff',
                //            InstKey: '$InstKey',
                //            KeyManager: '$KeyManager',
                //            Manager: '$Manager',
                //            KeySales: '$KeySales',
                //            Sales: '$Sales',
                //            KeyMediator: '$KeyMediator',
                //            Mediator: '$Mediator',
                //            tanggalProses : '$tanggalProses',
                //            luasDeal: '$luasDeal',
                //            Created: '$Created'  
                //        }}}",
                //       @"{$project: {
                //            _id : 0,
                //            key: '$_id.key',
                //            NoRequest: '$_id.NoRequest',
                //            keyReff: '$_id.keyReff',
                //            InstKey: '$_id.InstKey',
                //            KeyManager: '$_id.KeyManager',
                //            Manager: '$_id.Manager',
                //            KeySales: '$_id.KeySales',
                //            Sales: '$_id.Sales',
                //            KeyMediator: '$_id.KeyMediator',
                //            Mediator: '$_id.Mediator',
                //            tanggalProses : '$_id.tanggalProses',
                //            luasDeal: '$_id.luasDeal',
                //            Created: '$_id.Created'
                //        }}"
                //    };
                //    stages = findQueries.ToList();
                //}
                //var data = contextplus.GetDocuments(new PraBebasView(), "praDeals", stages.ToArray()).ToList();
                #endregion

                var instanceKeys = data.Select(d => d.InstKey).ToList();
                string instKeys = string.Join(",", instanceKeys);
                var stepsto = Enum.GetValues(typeof(ToDoState)).Cast<ToDoState>().Select(a => a.AsStatus()).ToList();
                string StepsToGet = string.Join(",", stepsto);

#if test
                var Steps = (ghost.GetMany(instKeys, StepsToGet).GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
#else
                var Steps = (graphost.GetMany(instKeys, StepsToGet).ToArray() ?? new GraphMainInstance[0]);
#endif

                var nxdata = (strict ? data.Join(Gs, a => a.InstKey, g => g.main?.key,
                    (a, g) => (a, i: g.main,
                                 nm: g.inmain?.LastOrDefault(),
                                 ns: g.insubs?.LastOrDefault(s => s.instance.lastState.time == g.insubs?.Max(ss => ss.instance.lastState.time)))) :
                            data.GroupJoin(Gs, a => a.InstKey, g => g.main?.key,
                                                (a, sg) => (a, g: sg.FirstOrDefault()))
                                                .Select(x => (x.a,
                                                                        i: x.g.main,
                                                                        nm: x.g.inmain?.LastOrDefault(),
                                                                        ns: x.g.insubs?.LastOrDefault(s => s.instance.lastState.time == x.g.insubs?.Max(ss => ss.instance.lastState.time))
                                                                    ))
                            ).ToArray().AsParallel();

                var ndata = nxdata.Select(x => (x.a, x.i, nm: (x.nm ?? new GraphTreeNode(null, null, null)), routes: (x.nm?.routes.ToArray() ?? new GraphRoute[0]))).ToArray();
                var ndatax = ndata.Select(x => (x, y: PraBebasView.Upgrade(x.a))).ToArray();

                var data2 = ndatax.Where(X => X.y != null).Select(X => X.y?
                .SetRoutes(monitor ? null : X.x.routes?.Select(x => (x.key, x._verb.Title(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                .SetState(X.x.nm?.node?._state ?? ToDoState.unknown_)
                .SetStatus(Steps.FirstOrDefault(s => s.key == X.y.InstKey) != null ? Steps.FirstOrDefault(s => s.key == X.y.InstKey)?.lastState?.state.AsStatus() : ToDoState.unknown_.AsStatus()
                           , X.x.i?.lastState?.time)
                ).ToList();

                var xlst = ExpressionFilter.Evaluate(data2, typeof(List<PraBebasView>), typeof(PraBebasView), gs);
                var filteredData = xlst.result.Cast<PraBebasView>().ToList();
                var sorted = filteredData.GridFeed(gs, tm, new Dictionary<string, object> { { "role", creator ? 1 : monitor ? 2 : 0 } });

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

        /// <summary>
        /// Get List Detail Pra Pembebasan
        /// </summary>
        [HttpGet("list/dtl")]
        public IActionResult GetListDtl([FromQuery] string token, [FromQuery] string reqKey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                PraPembebasan praDeals = contextplus.praDeals.FirstOrDefault(x => x.key == reqKey);
                if (praDeals == null)
                    return new UnprocessableEntityObjectResult("Request Pra Pembebasan tidak ada");

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var creator = priv.Intersect(new[] { "PAY_MKT" }).Any();
                var monitor = priv.Intersect(new[] { "PAY_PRA_TRANS" }).Any();
                var reviewer = priv.Intersect(new[] { "MAP_FULL", "MAP_REVIEW" }).Any();
                var manager = priv.Intersect(new[] { "PAY_PRA_REVIEW" }).Any() && !monitor;

#if test
                var instance = ghost.Get(praDeals.instancesKey).GetAwaiter().GetResult();
#else
                var instance = graphost.Get(praDeals.instancesKey);
#endif

                //var notaris = context.db.GetCollection<Notaris>("masterdatas").Find("{_t:'notaris', invalid:{$ne:true}}").ToList();
                //var idBidang = context.GetDocuments(new { IdBidang = "", key = "" }, "persils_v2",
                //    "{$match : {invalid:{$ne:true}}}",
                //   @"{$lookup: {from: 'maps',let:
                //               {key : '$basic.current.keyDesa'},
                //                pipeline:[{$unwind: '$villages'},
                //                {$match:  {$expr: {$eq:['$villages.key','$$key']}}}
                //            ], as:'desas'}}",
                //   "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",
                //   "{$match : {'desas' : {$ne : null }}}",
                //   "{$project : { key:1, IdBidang:1, _id:0 }}").ToList();
                var securities = contextplus.GetDocuments(new user(), "securities", "{$match: {'FullName': {$exists: true}}}", "{$project: {_id:0}}").ToList();
                //var followUp = context.GetDocuments(new { key = "", nomor = "" }, "bidangFollowUp", "{$project: {_id:0, key:1, nomor:1}}").ToList();

                //var villages = contextplus.GetVillages().Select(v => v.desa).ToList();

                //var data = praDeals.details.Select(d => d.ToView(praDeals.key,
                //                                                 notaris.FirstOrDefault(x => x.key == d.notaris),
                //                                                 securities,
                //                                                 idBidang.FirstOrDefault(x => x.key == d.keyPersil) == null && !string.IsNullOrEmpty(d.keyPersil) ? "(Ada, di server lain)" :
                //                                                 idBidang.FirstOrDefault(x => x.key == d.keyPersil) == null ? "" :
                //                                                 idBidang.FirstOrDefault(x => x.key == d.keyPersil).IdBidang,
                //                                                 followUp.FirstOrDefault(f => f.key == d.keyFollowUp)?.nomor,
                //                                                 villages
                //                                                )
                //                                  );

                var villages = contextplus.GetVillages();

                var data = contextpay.GetDocuments(new DetailPraDeal(), "praDeals",
                    "{$match : {'key' : '" + reqKey + "' }}",
                    "{$unwind : '$details'}",
                    "{$lookup : {from: 'persils_v2', localField: 'details.keyPersil', foreignField: 'key', as: 'persils'}}",
                    "{$unwind: { path: '$persils', preserveNullAndEmptyArrays: true}}",
                   @"{$lookup: {from: 'maps',let: {key : '$persils.basic.current.keyDesa'}, 
                                pipeline:[{$unwind: '$villages'},
                                          {$match:  {$expr: {$eq:['$villages.key','$$key']}}}], 
                                as:'desas'}}",
                   "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",

                   @"{$lookup: { from: 'masterdatas', 
                                let: {keyNotaris: '$details.notaris'}, 
                                pipeline: [   
                                    {$match: {$and : [{$expr: {$eq:['$key','$$keyNotaris']}}, {$expr: {$eq:['$_t','notaris']}} ]}},
                                    {$project : {_id : 0, identifier : 1}}
                                ],
                                as: 'notaris'}}",
                   "{$unwind: { path: '$notaris', preserveNullAndEmptyArrays: true}}",
                   "{$lookup : {from: 'bidangFollowUp', localField: 'keyFollowUp', foreignField: 'key', as: 'followUp'}}",
                   "{$addFields : {'keyPersilExists' : {$ne:['$details.keyPersil', null]}}}",
                   "{$addFields : {'desaExists' : {$ne: [{$ifNull : ['$desas.villages.identity','']}, '']}}}",
                   "{$addFields : {'persilExists': {$ne: [{$ifNull : ['$persils', '']}, '']}}}",
                   @"{$project: {
                           _id:0,
                           reqKey : '$key',
                           key: '$details.key',
                           AlasHak: '$details.alasHak',
                           Pemilik: '$details.pemilik',
                           Group: '$details.group',
                           keyNotaris: '$details.notaris',
                           notaris: '$notaris.identifier',
                           luasSurat: '$details.luasSurat',
                           keyBundle: '$details.keyBundle',
                           idBidang: {$switch : {
                               branches:[
                                    {case: {$and:  [{$eq: ['$persilExists', false]}, {$ne:[{$ifNull: ['$details.keyPersil', '']}, '']}]}, then: '(Ada di Server lain)'},
                                    {case: {$and : [{$eq: ['$keyPersilExists', true]}, {$eq: ['$desaExists', true]}]},then: '$persils.IdBidang'},
                                    {case: {$and : [{$eq: ['$keyPersilExists', true]}, {$ne: ['$desaExists', true]}]},then: '(Ada di Server lain)'}
                                   ],
                                   default: '' 
                           }},
                           dealUTJ: '$details.dealUTJ',
                           dealDP: '$details.dealDP',
                           dealLunas: '$details.dealLunas',
                           statusDeal: {$cond : {if: {$or : [{$ne: ['$details.dealDP', null ]}, {$ne: ['$details.dealLunas', null ]} ]}, then:'Deal', else: ''}},
                           hargaSatuan: '$details.hargaSatuan',
                           created: '$details.created',
                           Alias:{$ifNull : ['$details.alias', '']} ,
                           followUpKey: {$ifNull: ['$details.keyFollowUp', '']},
                           nomorFollowUp: {$ifNull : ['', '']},
                           keyProject: '$details.keyProject',
                           keyDesa: '$details.keyDesa',
                           desa: {$switch : {
                               branches:[
                                    {case: {$eq: ['$details.keyDesa', null ]}, then: '$details.desa'}
                                   ],
                                   default: '$desas.villages.identity'
                           }},
                           reasons :'$details.reasons',
                           jenisAlasHak: {$switch: {
                                branches:[
                                    {case: {$eq: ['$details.jenisAlasHak', 1 ]}, then: 'GIRIK'},
                                    {case: {$eq: ['$details.jenisAlasHak', 2 ]}, then: 'SHP'},
                                    {case: {$eq: ['$details.jenisAlasHak', 3 ]}, then: 'HGB'},
                                    {case: {$eq: ['$details.jenisAlasHak', 4 ]}, then: 'SHM'}
                                   ],
                                   default: ''
                           }},
                           kepemilikan: {$switch: {
                                branches:[
                                    {case: {$eq: ['$details.kepemilikan', 0 ]}, then: 'Perorangan'},
                                    {case: {$eq: ['$details.kepemilikan', 1 ]}, then: 'Waris'},
                                    {case: {$eq: ['$details.kepemilikan', 2 ]}, then: 'PT'}
                                   ],
                                   default: ''
                           }}
                       }}"
                    ).ToList().Select(v => v.ToView(securities, villages));

                var docKeys = data.Select(d => d.key).ToArray();

#if test
                var subs = ghost.GetFromDetailsExt(user, instance.key, docKeys, (monitor || reviewer) ? true : false).GetAwaiter().GetResult();
#else
                var subs = graphost.GetMany(user, instance.key, docKeys, (creator || monitor || reviewer) ? true : false);
#endif

                var res2 = data.GroupJoin(subs, d => d.key, s => s.key,
                            (d, ss) => (d, s: ss.FirstOrDefault().trees?.FirstOrDefault()))
                            .Select(x => (x.d, inst: instance, node: x.s?.nodes?.OrderBy(n => n.viewOnly == !(creator) ? 0 : 1)
                            .LastOrDefault()))
                            .Select(x => (x, y: PraDealsDetailView.Upgrade(x.d))).ToArray();

                var res = res2.Select(X => (X.y, X.x.node, X.x.node?.viewOnly, X.x.inst, route: X.x.node?.routes?.ToArray(), d: X.x.d)).AsParallel()
                                           .Select(X => X.y?
                                           .SetState(GetActualState(X.inst, X.route?.First().key))
                                           .SetRoutes(X.viewOnly == true ? null : X.route?.Where(x => x.privs[0].Intersect(priv).Any()).ToList().Distinct()
                                           .Select(x => (x.key, x._verb.Title(), x._verb, x.branches.Select(b => b._control).Distinct()
                                           .ToArray()))
                                           .ToArray())
                                           .SetStatus(GetActualStatus(X.inst, X.route?.First().key))
                                     ).ToArray().OrderBy(x => x.created);

                var statsReview = new[] { "diterbitkan", "tuntas", "baru dibuat" };

                res = (reviewer ? res.ToArray().Where(r => !statsReview.Contains(r.Status?.ToLower())).ToArray().OrderBy(x => x.created) :
                      res.ToArray().OrderBy(x => x.created));

                var xlst = ExpressionFilter.Evaluate(res, typeof(List<PraDealsDetailView>), typeof(PraDealsDetailView), gs);
                var filteredData = xlst.result.Cast<PraDealsDetailView>().ToList();
                return Ok(filteredData.GridFeed(gs, null, new Dictionary<string, object> { { "role", creator ? 1 : monitor ? 2 : 0 } }));
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

        [HttpGet("list/dtl/ext")]
        public IActionResult GetListDtlExt([FromQuery] string token, [FromQuery] string reqKey, [FromQuery] string detKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var praDeal = contextplus.praDeals.FirstOrDefault(x => x.key == reqKey);
                if (praDeal == null)
                    return new UnprocessableEntityObjectResult("Request Pembebasan Tidak ditemukan !");
                var detail = praDeal.details.FirstOrDefault(x => x.key == detKey);
                if (detail.key == null)
                    return new UnprocessableEntityObjectResult("Detail Tidak ditemukan !");

                var notaris = contextplus.db.GetCollection<Notaris>("masterdatas").Find("{_t:'notaris', invalid:{$ne:true}}").ToList();
                //var idBidang = contextplus.GetDocuments(new { IdBidang = "", key = "" }, "persils_v2", "{$match : {invalid:{$ne:true}}}", "{$project : { key:1, IdBidang:1, _id:0 }}").ToList();
                var persil = GetPersil(detail.keyPersil);
                var categories = contextplus.GetDocuments(new Category(), "categories_new",
                @"{$project: {
                                _id:0
                                ,key:1
                                ,segment:1
                                ,desc:1
                             }
                 }").ToList();

                var persilCategories = contextplus.persilCat.All().ToList();

                var securities = contextplus.GetDocuments(new user(), "securities", "{$match: {'FullName': {$exists: true}}}", "{$project: {_id:0}}").ToList();
                var villages = contextplus.GetVillages();
                var followUp = contextplus.GetDocuments(new { key = "", nomor = "" }, "bidangFollowUp", "{$project: {_id:0, key:1, nomor:1}}").ToList();

                var data = detail.ToViewExt(reqKey, notaris,
                                                 //idBidang.FirstOrDefault(x => x.key == detail.keyPersil) == null ? "" : idBidang.FirstOrDefault(x => x.key == detail.keyPersil).IdBidang,
                                                 persil == null ? string.Empty : persil.IdBidang,
                                                 categories,
                                                 securities,
                                                 villages,
                                                 followUp.FirstOrDefault(f => f.key == detail.keyFollowUp)?.nomor,
                                                 persilCategories
                                         );
                return Ok(data);
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

        /// <summary>
        /// Saving Header
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT")]
        [HttpPost("save/header")]
        public IActionResult SaveHeader([FromQuery] string token, [FromQuery] string opr, [FromBody] PraPembebasanCore core)
        {
            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + "Pra Pembebasan";
            try
            {
                var user = contextplus.FindUser(token);
                var res = opr switch
                {
                    "add" => Add(user),
                    "del" => Del(user),
                    _ => Edit(user),
                };
                return res;
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

            IActionResult Add(user user)
            {
                if (string.IsNullOrEmpty(core.manager) && string.IsNullOrEmpty(core.keyReff))
                    return new UnprocessableEntityObjectResult("Mohon pilih manager !");
                if (core.tanggalProses == null)
                    return new UnprocessableEntityObjectResult("Tanggal Kesepakatan harus diisi !");
                if ((core.luasDeal == null && core.keyReff == null) || (core.luasDeal == 0 && core.keyReff == null))
                    return new UnprocessableEntityObjectResult("Luas Bidang Kesepakatan harus diisi !");

                var mgInitial = contextplus.db.GetCollection<Worker>("workers").Find("{key: '" + core.manager + "'}").FirstOrDefault()?.InitialName ?? "";

                var prevPraDeal = contextplus.db.GetCollection<PraPembebasan>("praDeals").Find("{manager :'" + core.manager + "'}").ToList()
                                            .OrderByDescending(x => x.created).ToList();

                var prevPraDealCross = contextCross.db.GetCollection<PraPembebasan>("praDeals").Find("{manager :'" + core.manager + "'}").ToList()
                                            .OrderByDescending(x => x.created).FirstOrDefault();

                PraPembebasan praDeals = new PraPembebasan();
                if (!string.IsNullOrEmpty(core.keyReff))
                {
                    prevPraDeal = prevPraDeal.Where(p => p.keyReff == core.keyReff || p.key == core.keyReff).ToList();
                    praDeals = prevPraDeal.OrderByDescending(p => p.created).LastOrDefault();
                    praDeals = praDeals.FromCore(user, core, prevPraDeal, praDeals.identifier);
                }
                else
                {
                    prevPraDeal.Add(prevPraDealCross);
                    prevPraDeal = prevPraDeal.OrderByDescending(x => x.identifier).ThenByDescending(x => x.created).ToList();
                    prevPraDeal = prevPraDeal.Where(p => p.keyReff == null).ToList();
                    praDeals = praDeals.FromCore(user, core, prevPraDeal, mgInitial, null);
                }

                contextplus.praDeals.Insert(praDeals);
                contextplus.SaveChanges();

                var returnPost = new ReturnPost(praDeals.identifier);

                return new JsonResult(returnPost);
            }

            IActionResult Edit(user user)
            {
                if (string.IsNullOrEmpty(core.manager))
                    return new UnprocessableEntityObjectResult("Mohon pilih manager !");

                if (core.tanggalProses == null)
                    return new UnprocessableEntityObjectResult("Tanggal Kesepakatan harus diisi !");

                PraPembebasan oldPraDeals = contextplus.praDeals.FirstOrDefault(x => x.key == core.key);
                if (oldPraDeals == null)
                    return new UnprocessableEntityObjectResult("Request yang dimaksud tidak ditemukan!");
#if test
                var instances = ghost.Get(oldPraDeals.instancesKey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                var instances = graphost.Get(oldPraDeals.instancesKey) ?? new GraphMainInstance();
#endif
                var LastStatus = instances.lastState?.state;

                if (LastStatus != ToDoState.created_)
                    return new UnprocessableEntityObjectResult("Request sudah diterbikan tidak dapat diedit !");

                if (core.keyReff != null)
                {
                    string keyInstanceParent = contextplus.praDeals.FirstOrDefault(x => x.key == core.keyReff)?.instancesKey;
                    var instancesParent = ghost.Get(keyInstanceParent).GetAwaiter().GetResult() ?? new GraphMainInstance();
                    var parentLastStatus = instances.lastState?.state;
                    if (parentLastStatus != ToDoState.created_)
                        return new UnprocessableEntityObjectResult("Parent Request sudah diterbikan tidak dapat diedit !");
                }

                var mgInitial = contextplus.db.GetCollection<Worker>("workers").Find("{key: '" + core.manager + "'}").FirstOrDefault()?.InitialName ?? "";

                var prevPraDeal = contextplus.db.GetCollection<PraPembebasan>("praDeals").Find("{manager :'" + core.manager + "'}").ToList()
                                         .OrderByDescending(x => x.created).ToList();

                PraPembebasan newPraDeals = new PraPembebasan();

                string newIdentifier = oldPraDeals.identifier;
                if (core.manager != oldPraDeals.manager)
                    newIdentifier = newPraDeals.GenerateReqIdentifier(core, prevPraDeal, mgInitial);

                if (core.keyReff != null)
                {
                    prevPraDeal = prevPraDeal.Where(p => p.keyReff == core.keyReff || p.key == core.keyReff).ToList();
                    oldPraDeals = oldPraDeals.FromCore3(newIdentifier, core);
                    contextplus.praDeals.Update(oldPraDeals);
                    contextplus.SaveChanges();
                    foreach (var praDeal in prevPraDeal)
                    {
                        praDeal.luasDeal = core.luasDeal;
                        contextplus.praDeals.Update(praDeal);
                        contextplus.SaveChanges();
                    }
                }
                else
                {
                    prevPraDeal = prevPraDeal.Where(p => p.keyReff == core.key).ToList();
                    newPraDeals = oldPraDeals.FromCore2(core, oldPraDeals, newIdentifier);
                    contextplus.praDeals.Update(newPraDeals);
                    contextplus.SaveChanges();

                    foreach (var praDeal in prevPraDeal)
                    {
                        praDeal.luasDeal = core.luasDeal;
                        contextplus.praDeals.Update(praDeal);
                        contextplus.SaveChanges();
                    }
                }

                var returnPost = new ReturnPost(newPraDeals.identifier);
                return new JsonResult(returnPost);
            }

            IActionResult Del(user user)
            {
                PraPembebasan oldPraDeals = contextplus.praDeals.FirstOrDefault(x => x.key == core.key);
                if (oldPraDeals == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ditemukan");

#if test
                var instances = ghost.Get(oldPraDeals.instancesKey).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                var instances = graphost.Get(oldPraDeals.instancesKey) ?? new GraphMainInstance();
#endif

                var LastStatus = instances.lastState?.state;
                if (LastStatus != ToDoState.created_)
                    return new UnprocessableEntityObjectResult("Request sudah diterbitkan !");

                contextplus.praDeals.Remove(oldPraDeals);
                contextplus.SaveChanges();
#if test
                ghost.Del(oldPraDeals.instancesKey).Wait();
#else
                graphost.Del(oldPraDeals.instancesKey);
#endif
                var returnPost = new ReturnPost(oldPraDeals.identifier);

                return new JsonResult(returnPost);
            }
        }

        /// <summary>
        /// Save Detail Request
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT")]
        [HttpPost("save/detail")]
        public IActionResult SaveDetails([FromQuery] string token, [FromQuery] string opr, [FromBody] DetailsPraBebasCore core)
        {

            var predicate = opr switch
            {
                "edit" => "memperbarui",
                "del" => "menghapus",
                _ => "menambah"
            } + "Pra Pembebasan";
            try
            {
                var user = contextplus.FindUser(token);
                var res = opr switch
                {
                    "edit" => Edit(user),
                    "del" => Delete(user),
                    _ => Add(user),
                };
                return res;
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

            IActionResult Add(user user)
            {
                if (string.IsNullOrEmpty(core.alasHak) && string.IsNullOrEmpty(core.pemilik) && string.IsNullOrEmpty(core.group))
                    return new UnprocessableEntityObjectResult("Salah satu dari Alas Hak / Pemilik / Group, harus diisi !");

                PraPembebasan praDeal = contextplus.praDeals.FirstOrDefault(x => x.key == core.reqKey);
                DetailsPraBebas detailPraBebas = new DetailsPraBebas();

                var followUpMkt = GetBidangFollowUp(core.followUpKey);
                var (anySisaLuas, sisaLuas) = AnyRestLuasFollowUp(followUpMkt?.key, followUpMkt?.luas);
                if (!string.IsNullOrEmpty(core.followUpKey) && !anySisaLuas)
                    return new UnprocessableEntityObjectResult("Seluruh Luas Bidang Follow Up Sudah di Claim !");
                var detKey = InsertDetails(praDeal, detailPraBebas, user, core, followUpMkt);

                string keyReff = praDeal.keyReff ?? praDeal.key;
                double totalLuasDetail = GetJumlahLuasDeal(keyReff);
                bool isLuasOver = AlertAreaRestriction(praDeal, core, user, totalLuasDetail).Result;
                detKey.isAlert = isLuasOver;

                return Ok(detKey);
            }

            IActionResult Edit(user user)
            {
                if (core.reqKey == null || core.detKey == null)
                    return new UnprocessableEntityObjectResult("Request / Detail tidak tersedia !");

                var followUpMkt = GetBidangFollowUp(core.followUpKey);
                var (anySisaLuas, sisaLuas) = AnyRestLuasFollowUp(followUpMkt?.key, followUpMkt?.luas, core.detKey, core.luasSurat);
                if (!string.IsNullOrEmpty(core.followUpKey) && !anySisaLuas)
                    return new UnprocessableEntityObjectResult("Luas yang anda input lebih besar dari luas seharusnya!");

                PraPembebasan praDeals = contextplus.praDeals.FirstOrDefault(x => x.key == core.reqKey);
                var praDealsDetail = praDeals.details.FirstOrDefault(x => x.key == core.detKey);
                string keyPersilCat = UpdatePersilCategories(user, praDealsDetail, core);
                var old = praDealsDetail.FromCore(core, followUpMkt?.categories, keyPersilCat);

                if (old.jenisAlasHak != null && old.kepemilikan != null)
                    CreateDocSettings(user, old.key, old.jenisAlasHak, old.kepemilikan);

                var details = praDeals.details.Where(x => x.key != core.detKey).ToList();
                details.Add(old);
                praDeals.details = details.ToArray();
                contextplus.praDeals.Update(praDeals);
                contextplus.SaveChanges();

                var formPra = contextplus.formPraBebas.FirstOrDefault(x => x.key == old.keyForm);
                if (formPra != null)
                {
                    formPra = formPra.FromCore(core);
                    contextplus.formPraBebas.Update(formPra);
                    contextplus.SaveChanges();
                }
                string keyReff = praDeals.keyReff ?? praDeals.key;
                double totalLuasDetail = GetJumlahLuasDeal(keyReff);
                bool isLuasOver = AlertAreaRestriction(praDeals, core, user, totalLuasDetail).Result;
                var returnPost = new ReturnPost(isLuasOver, praDealsDetail.key);
                return new JsonResult(returnPost);
            }

            IActionResult Delete(user user)
            {
                if (core.reqKey == null || core.detKey == null)
                    return new UnprocessableEntityObjectResult("Request / Detail tidak tersedia !");

                PraPembebasan praDeals = contextplus.praDeals.FirstOrDefault(x => x.key == core.reqKey);
#if test
                var instance = ghost.Get(praDeals.instancesKey).GetAwaiter().GetResult();
#else
                var instance = graphost.Get(praDeals.instancesKey);
#endif
                var LastStatus = instance.lastState?.state;
                var details = praDeals.details.Where(x => x.key != core.detKey).ToArray();
                praDeals.details = details.ToArray();
                contextplus.praDeals.Update(praDeals);
                var formPra = contextplus.formPraBebas.FirstOrDefault(x => x.keyPraDeal == core.reqKey
                                                                       && x.keyDetailPraDeal == core.detKey
                                                                     );
                contextplus.formPraBebas.Remove(formPra);
                contextplus.SaveChanges();

                var returnPost = new ReturnPost(praDeals.identifier);

                return Ok();
            }
        }

        /// <summary>
        /// Get List Document Pre Bundle
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_REVIEW,MAP_FULL")]
        [HttpGet("list/doc")]
        public IActionResult GetListDocs([FromQuery] string token, string praDealKey, string detKey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var privs = user.privileges.Select(p => p.identifier).ToArray();
                var privScanDocs = contextplus.GetDocuments(new DocsAccess(), "static_collections", "{$match: {'_t': 'praDealDocAcceess'}}", "{$project: {_id:0,_t:0 }}").ToList();
                var privScanDoc = privScanDocs.Where(a => a.privs.Intersect(privs).Any()).FirstOrDefault();
                var allowedDocs = privScanDoc?.jnsDoks ?? new string[0];

                var bundle = bhost.PreBundleGet(detKey).GetAwaiter().GetResult() as PreBundle;
                if (bundle == null)
                {
                    CheckAndUpdatePreBundle(praDealKey, detKey, user);
                    bundle = bhost.PreBundleGet(detKey).GetAwaiter().GetResult() as PreBundle;
                }

                var ddata = bundle.doclist.ToArray();
                var data = ddata.Select(d => d.ToView(allowedDocs)).ToArray();

                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        (DateTime? issued, DateTime? accepted, DateTime? complished) GetTimeFrames(GraphMainInstance i)
        {
            if (i == null)
                return (null, null, null);

            var states = i.states.ToArray() ?? new GraphState[0];
            return (
                    states.LastOrDefault(s => s.state == ToDoState.issued_)?.time,
                    states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time,
                    states.LastOrDefault(s => s.state == ToDoState.complished_)?.time
                   );
        }

        /// <summary>
        /// Get List Request for Refference
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_REVIEW,MAP_FULL")]
        [HttpGet("list/request")]
        public IActionResult GetListRequestReff([FromQuery] string token, string workerKey, bool isAll = false)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var data = contextplus.db.GetCollection<PraPembebasan>("praDeals")
                                  .Find("{_t:'PraPembebasan',invalid:{$ne:true}}").ToList()
                                  .Where(p => p.manager.Trim() == workerKey.Trim() &&
                                               (isAll ? true : string.IsNullOrEmpty(p.keyReff))
                                        )
                                  .Select(p => new cmnItem()
                                  {
                                      key = p.key,
                                      name = p.identifier
                                  });

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

        /// <summary>
        /// Continue Flow for Heaader
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT")]
        [HttpPost("step")]
        public IActionResult Step([FromQuery] string token, [FromBody] PraBebasCommand data)
        {
            try
            {
                var user = contextplus.FindUser(token);
                PraPembebasan praDeals = contextplus.praDeals.FirstOrDefault(x => x.key == data.dkey);
                if (praDeals == null)
                    return new UnprocessableEntityObjectResult("Request Pra Pembebasan tidak ada");
                if (praDeals.invalid == true)
                    return new UnprocessableEntityObjectResult("Request Pra Pembebasan tidak aktif");

#if test
                var instance = ghost.Get(praDeals.instancesKey).GetAwaiter().GetResult();
#else
                var instance = graphost.Get(praDeals.instancesKey);
#endif

                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Request Pra Pembebasan belum lengkap");
                if (instance.closed)
                    return new UnprocessableEntityObjectResult("Pra Pembebasan telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == data.rkey));
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow Pra Pembebasan tidak jelas");
                var route = node.routes.First(r => r.key == data.rkey);

                var issuing = node._state == ToDoState.created_ && route._verb == ToDoVerb.issue_;
                if (issuing && !praDeals.details.Any())
                    return new UnprocessableEntityObjectResult("Pra Pembebasan hanya bisa diterbitkan jika sudah ditambahkan detail yang akan diproses");

                if (issuing)
                {
#if test
                    ghost.RegisterDocs(instance.key, praDeals.details.Select(d => d.key).ToArray(), true).Wait();
#else
                    graphost.RegisterDocs(instance.key, praDeals.details.Select(d => d.key).ToArray(), true);
#endif
                }
#if test
                (var ok, var reason) = ghost.Take(user, praDeals.instancesKey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, praDeals.instancesKey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
#else
                (var ok, var reason) = graphost.Take(user, praDeals.instancesKey, data.rkey);
                if (ok)
                    (ok, reason) = graphost.Summary(user, praDeals.instancesKey, data.rkey, data.control.ToString("g"), null);
#endif
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);

                return Ok();
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

        /// <summary>
        /// Continue Flow for Details
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_REVIEW,MAP_FULL")]
        [HttpPost("step/dtl")]
        public IActionResult StepDtl([FromQuery] string token, [FromBody] PraBebasDetailCommand data)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var pra = contextplus.praDeals.FirstOrDefault(p => p.key == data.dkey);
                if (pra == null)
                    return new UnprocessableEntityObjectResult("Pra Pembebasan tidak ada");
                if (pra.invalid == true)
                    return new UnprocessableEntityObjectResult("Pra Pembebasan tidak aktif");

                var detail = pra.details.FirstOrDefault(d => d.key == data.detKey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Pra Pembebasan tidak valid");

#if test
                var instance = ghost.Get(pra.instancesKey).GetAwaiter().GetResult();
#else
                var instance = graphost.Get(pra.instancesKey);
#endif
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi belum lengkap");
                if (pra.closed != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Request telah selesai diproses");
#if test
                var subinst = ghost.GetSub(instance.key, data.detKey).GetAwaiter().GetResult();
#else
                var subinst = graphost.GetSub(instance.key, data.detKey);
#endif
                if (subinst == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Detail Penugasan belum lengkap");
                if (subinst.closed)
                    return new UnprocessableEntityObjectResult("Detail Penugasan telah selesai diproses");
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                (var ok, var reason) = (true, "");
                var route = subinst.Core.nodes.OfType<GraphNode>()
                                        .SelectMany(n => n.routes).FirstOrDefault(r => r.key == data.rkey);

                var analyst = privs.Intersect(new[] { "MAP_REVIEW", "MAP_FULL" }).Any();
                var praPayment = privs.Intersect(new[] { "PAY_PRA_TRANS", "PAY_PRA_REVIEW" }).Any();

                if (data.control == ToDoControl._)
                    (ok, reason) = route?._verb switch
                    {
                        ToDoVerb.utjPay_ => UTJApproved(pra, user, data.detKey),
                        ToDoVerb.dpPay_ => (detail.jenisAlasHak != null && detail.kepemilikan != null) ? BidangChecking(detail.key, detail.keyPersil, "DP") : ValidasiBidang(detail.keyPersil),
                        ToDoVerb.lunasPay_ => (detail.jenisAlasHak != null && detail.kepemilikan != null) ? BidangChecking(detail.key, detail.keyPersil, "LUNAS") : ValidasiBidang(detail.keyPersil),
                        ToDoVerb.mapReview_ => TransferToBundle(pra, user, data.detKey),
                        _ => (true, null)
                    };

                if (data.control == ToDoControl.yes_ && praPayment)
                    (ok, reason) = route?._verb switch
                    {
                        ToDoVerb.approveDp_ => DpApproved(pra, user, data.detKey),
                        ToDoVerb.complish_ => LunasApproved(pra, user, data.detKey),
                        _ => (true, null)
                    };

                if (data.control == ToDoControl.no_ && praPayment)
                    (ok, reason) = route?._verb switch
                    {
                        ToDoVerb.approveDp_ => Reject(pra, user, data.detKey, data.reason, JenisBayar.DP),
                        ToDoVerb.complish_ => Reject(pra, user, data.detKey, data.reason, JenisBayar.Lunas),
                        _ => (true, null)
                    };


                if (ok)
                    if (ok)
#if test
                        (ok, reason) = ghost.Take(user, pra.instancesKey, data.rkey).GetAwaiter().GetResult();
#else
                        (ok, reason) = graphost.Take(user, pra.instancesKey, data.rkey);
#endif
                if (ok)
#if test
                    (ok, reason) = ghost.Summary(user, pra.instancesKey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
#else
                    (ok, reason) = graphost.Summary(user, pra.instancesKey, data.rkey, data.control.ToString("g"), null);
#endif
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
                else
                    return Ok();
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

        /// <summary>
        /// Get List Bidang to Match
        /// </summary>
        [NeedToken("MAP_REVIEW,MAP_FULL")]
        [HttpGet("bidang/list")]
        public IActionResult GetListBidang([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var keyPersilSelected = contextplus.GetDocuments(new { keyPersil = "" }, "praDeals", "{$unwind: '$details'}",
                    "{$project: {keyPersil:{$ifNull:['$details.keyPersil', '']}, _id:0}}").Select(x => x.keyPersil).ToList();

                var persils = contextplus.persils.Query(x => !keyPersilSelected.Contains(x.key) && x.en_state == StatusBidang.belumbebas
                                                                       && x.invalid != true
                                                                       && x.deal == null
                                                                       && x.basic.current != null
                                                                       && x.basic.current.keyParent == null).Where(x =>
                                                                       (x.basic.entries.LastOrDefault().item.keyParent == null ||
                                                                            (
                                                                                x.basic.entries.LastOrDefault().item.keyParent != null &&
                                                                                x.basic.entries.LastOrDefault().approved == false
                                                                             )
                                                                            )
                                                                      ).ToList();

                var villages = contextplus.GetVillages();

                var result = persils.Join(villages, p => p.basic.current.keyDesa, v => v.desa.key, (p, v) => new GroupBidang()
                {
                    key = p.key,
                    en_state = p.en_state,
                    IdBidang = p.IdBidang,
                    luasDibayar = p.basic.current.luasDibayar,
                    luasSurat = p.basic.current.luasSurat,
                    luasUkur = p.basic.current.luasGU,
                    luasFix = p.luasFix,
                    Pemilik = p.basic.current?.pemilik,
                    group = p.basic.current?.group,
                    AlasHak = p.basic.current?.surat?.nomor,
                    satuan = p.basic.current?.satuan,
                    total = p.basic.current?.total,
                    noPeta = p.basic.current?.noPeta,
                    keyPenampung = p.basic.current?.keyPenampung,
                    keyPTSK = p.basic.current?.keyPTSK,
                    keyProject = v.project?.key,
                    project = v.project?.identity,
                    keyDesa = v.desa?.key,
                    desa = v.desa?.identity
                });

                //result = result.Where(r => !keyPersilSelected.Contains(r.key)).ToList();

                var bundles = contextplus.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
                            "{$match: {'_t' : 'mainBundle'}}",
                            "{$unwind: '$doclist'}",
                            "{$match : {$and : [{'doclist.keyDocType':'JDOK032'}, {'doclist.entries' : {$ne : []}}]}}",
                            @"{$project : {
                                  _id: 0
                                , key: 1
                                , doclist: 1
                            }}").ToList();

                var type = MetadataKey.Luas.ToString("g");
                var cleanbundle = bundles.Select(x => new { key = x.key, entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value })
                    .Select(x => new { key = x.key, dyn = x.entries.props.TryGetValue(type, out Dynamic val) ? val : null })
                    .Select(x => new { key = x.key, luasNIB = Convert.ToDouble(x.dyn?.Value ?? 0) }).ToList();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<GroupBidang>), typeof(GroupBidang), gs);
                var data = xlst.result.Cast<GroupBidang>().Select(a => a.toView2((cleanbundle.Select(x => (x.key, x.luasNIB)).ToList()))).ToArray();

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

        /// <summary>
        /// Select Matched Bidang
        /// </summary>
        [NeedToken("MAP_REVIEW,MAP_FULL")]
        [HttpPost("bidang/match")]
        public IActionResult MatchBidang([FromQuery] string token, [FromBody] MatchBidangCore core)
        {
            try
            {
                var user = contextplus.FindUser(token);
                if (string.IsNullOrEmpty(core.keyPersil))
                    return new UnprocessableEntityObjectResult("Key Bidang tidak Boleh Kosong");

                var persil = GetPersil(core.keyPersil.Trim());
                var praDeals = contextplus.praDeals.FirstOrDefault(x => x.key == core.reqKey.Trim());

                var subinst = ghost.GetSub(praDeals.instance.key, core.detKey).GetAwaiter().GetResult();
                var lastState = subinst.lastState.state;
                if (lastState != ToDoState.issued_)
                    return new UnprocessableEntityObjectResult("Detail telah direview, bidang tidak bisa dimatch ulang!");

                var dtl = praDeals.details.FirstOrDefault(x => x.key == core.detKey.Trim());
                string keyPersilCat = UpdatePersilCategories(user, dtl);

                // Remove detail yang diedit di list details
                var lstDtl = praDeals.details.ToList();
                lstDtl.Remove(dtl);

                // Input nilai baru detail
                dtl.keyDesa = core.keyDesa;
                dtl.keyProject = core.keyProject;
                dtl.keyPersil = persil.key;
                dtl.keyPersilCat = keyPersilCat;

                // Add detail yang sudah mempunyai nilai baru
                lstDtl.Add(dtl);

                //var detailPra = praDeals.details.Select(x => new DetailsPraBebas()
                //{
                //    key = x.key,
                //    desa = x.desa,
                //    keyDesa = x.key == dtl.key ? core.keyDesa : x.keyDesa,
                //    keyProject = x.key == dtl.key ? core.keyProject : x.keyProject,
                //    alasHak = x.alasHak,
                //    pemilik = x.pemilik,
                //    alias = x.alias,
                //    group = x.group,
                //    infoes = x.infoes,
                //    created = x.created,
                //    keyPersil = x.key != dtl.key ? x.keyPersil : persil.key,
                //    notaris = x.notaris,
                //    luasSurat = x.luasSurat,
                //    dealDP = x.dealDP,
                //    dealLunas = x.dealLunas,
                //    keyBundle = x.keyBundle,
                //    hargaSatuan = x.hargaSatuan,

                //    keyForm = x.keyForm,
                //    keyFollowUp = x.keyFollowUp,
                //    FileToNotary = x.FileToNotary,
                //    FileToPra = x.FileToPra,
                //    keyPersilCat = keyPersilCat,
                //    Categories = x.Categories,
                //    NotesBelumLengkap = x.NotesBelumLengkap,
                //    AdminToPraDP = x.AdminToPraDP,
                //    AdminToPraLunas = x.AdminToPraLunas,
                //    PPJB = x.PPJB,
                //    NIBPerorangan = x.NIBPerorangan,
                //    CekSertifikat = x.CekSertifikat,
                //    BukaBlokir = x.BukaBlokir,
                //    RevisiNop = x.RevisiNop,
                //    RevisiGeoKPP = x.RevisiGeoKPP,
                //    BalikNama = x.BalikNama,
                //    Shm = x.Shm
                //}).ToList();

                praDeals.details = lstDtl.ToArray();
                contextplus.praDeals.Update(praDeals);
                contextplus.SaveChanges();
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

        /// <summary>
        /// Get Form Dynamic for Edit Metadata
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_FULL,MAP_REVIEW")]
        [HttpGet("list/req/doc")]
        public IActionResult GetListReqDoc(string token, [FromQuery] string detKey, [FromQuery] string jnsDok)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var bundle = bhost.PreBundleGet(detKey).GetAwaiter().GetResult() as PreBundle;
                if (bundle == null)
                    return new NoContentResult();
                var data = bundle.doclist.FirstOrDefault(d => d.keyDocType == jnsDok);
                var layout = data.MakeLayout();
                var fdata = data.ToCore();
                var dcontext = new DynamicContext<RegisteredDocCore>(layout, fdata);

                return Ok(dcontext);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }


        /// <summary>
        /// Save Metadata to Pre Bundle
        /// </summary>
        [NeedToken("PAY_PRA_TRANS,PAY_PRA_REVIEW,PAY_MKT,MAP_FULL,MAP_REVIEW")]
        [HttpPost("save/doc")]
        public IActionResult SaveResultDoc([FromQuery] string token, [FromQuery] string detKey, [FromQuery] string typeKey,
                                            [FromBody] RegisteredDocCore core)
        {
            try
            {
                var user = contextplus.FindUser(token);
                if (core == null)
                    return new NotModifiedResult("Invalid data to save");

                DateTime timestamp = DateTime.Now;
                var bundle = bhost.PreBundleGet(detKey).GetAwaiter().GetResult() as PreBundle;
                if (bundle == null)
                    return new NotModifiedResult("Invalid data to save");

                BundledDoc docex = bundle.doclist.FirstOrDefault(d => d.keyDocType == core.keyDocType);
                if (docex == null)
                    return new NotModifiedResult("Invalid data to save");

                docex.FromCore(core, user, timestamp);
                var log = new LogPreBundle(core, user, bundle.key, timestamp, LogActivityType.Metadata, LogActivityModul.Pradeals);
                contextplus.logPreBundle.Insert(log);
                contextplus.SaveChanges();

                bhost.PreUpdate(bundle, true).Wait();
                /*         context.mainBundles.Update(bundle);
                            context.SaveChanges();
                */
                //MethodBase.GetCurrentMethod().GetCustomAttribute<MainBundleMaterializerAttribute>()
                //   .ManualExecute(context, bundkey);

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Get All Desa Name
        /// </summary>
        [HttpGet("desa/list")]
        public IActionResult GetDesaDdl([FromQuery] string token)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var villages = contextplus.GetVillages();
                var desa = villages.Select(v => v.desa.identity).Distinct();

                return Ok(desa);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Get Bidang Follow Up
        /// </summary>
        [HttpGet("follow-up/list")]
        public IActionResult GetBidangFollowUp([FromQuery] string token, string keyPraBebas, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var praDeal = contextplus.praDeals.FirstOrDefault(x => x.key == keyPraBebas);
                string basePraDealNumber = praDeal.identifier.Split("-")[0];

                var bidangFollowUp = contextplus.GetDocuments(new BidangFollowUp(), "bidangFollowUp",
                    @"{$match: {$and: [ {'result': 3},
                               {$or:  [ {'keyPraDeals': {$eq:null}}, {'keyPraDeals': {$exists:false}} ]}
                       ]}}"
                    ).ToList();

                var followUpSelected = contextplus.GetDocuments(new { keyFollowUp = "" }, "praDeals",
                    "{$match:   {'identifier': {$nin: ['" + basePraDealNumber + "', '" + praDeal.identifier + "']}}}",
                    "{$unwind:  '$details'}",
                    "{$match:   {'details.keyFollowUp': {$exists:true} }}",
                    "{$project: {_id:0, keyFollowUp: '$details.keyFollowUp'}}"
                    ).Select(f => f.keyFollowUp).ToList();

                var claimedLuas = contextplus.GetDocuments(new { keyFollowUp = "", claimedLuas = (double)0 }, "praDeals",
                                   "{$match: {'invalid' : {$ne: true }}}",
                                   "{$unwind: '$details'}",
                                  @"{$project: {
                                        _id:0,
                                        keyFollowUp : '$details.keyFollowUp',
                                        claimedLuas : {$ifNull :[ '$details.luasSurat',0]}
                                   }}").GroupBy(x => x.keyFollowUp)
                                   .Select(x => new { keyFollowUp = x.Key, claimedLuas = x.Sum(c => c.claimedLuas) })
                                   .ToList();

                List<Worker> dataWorker = GetDataWorker();
                List<Category> dataCategory = GetDataCategory();

                var result = bidangFollowUp.Where(r => !followUpSelected.Contains(r.key))
                                           .Select(r => r.ToView(dataWorker,
                                                                 dataCategory,
                                                                 (r?.luas ?? 0) - (claimedLuas.FirstOrDefault(c => c.keyFollowUp == r.key)?.claimedLuas ?? 0)
                                                                 )).ToList();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<BidangFollowUpView>), typeof(BidangFollowUpView), gs);
                var filteredData = xlst.result.Cast<BidangFollowUpView>().ToList();

                return Ok(filteredData.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// To Export Old Pre Bundle to New Pre Bundle
        /// </summary>
        [HttpPost("export/old-pre-bundle")]
        public IActionResult ExportOldPreBundle()
        {
            try
            {
                (bool ok, string error) = (true, null);
                var praDeals = contextplus.praDeals.All().Where(x => x.invalid != true && x.key != "templatePraDeals").ToList();
                foreach (var praDeal in praDeals)
                {
                    foreach (var detail in praDeal.details)
                    {
                        if (!string.IsNullOrEmpty(detail.keyBundle) || detail.infoes == null)
                            continue;
                        else
                        {
                            (ok, error) = TransferToNewPreBundle(praDeal, detail, services).GetAwaiter().GetResult();
                        }
                    }
                }

                if (!ok)
                    return new UnprocessableEntityObjectResult(error);

                return Ok();
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Get All Bidang by its Request (DEAL Number) including its descendant Deal
        /// </summary>
        [HttpGet("bidang/deal")]
        public IActionResult GetBdidangByDeal([FromQuery] string token, [FromQuery] string keyRequest, [FromQuery] AgGridSettings gs)
        {
            try
            {
                user user = contextplus.FindUser(token);

                var bidangDeals = contextplus.GetDocuments(new BidangsDealView(), "praDeals",
                   $"<$match  : <$or : [<'key' : '{keyRequest}'>, <keyReff : '{keyRequest}'>]>>".MongoJs(),
                    "{$unwind : '$details'}",
                    "{$lookup : {from: 'persils_v2', localField : 'details.keyBundle', foreignField: 'key', as : 'persil' }}",
                   @"{$lookup : {
                                    from: 'masterdatas', 
                                    let: {keyNotaris : '$details.notaris'}, 
                                    pipeline: [
                                        {$match : {$and : [{'_t' : 'notaris'},
                                                           {$expr:{$eq:['$$keyNotaris','$key']}}
                                                          ]
                                        }},
                                        {$project : {_id : 0}}
                                    ],
                                    as : 'praNotaris'
                     }}",
                   @"{$project : {
                         _id         : 0
                        keyRequest  : '$key'
                        keyDetail   : '$details.key',
                        idBidang    : {$ifNull : [{$arrayElemAt : ['$persil.IdBidang', -1]}, '']},
                        alasHak     : {$ifNull : ['$details.alasHak', '']} ,
                        luasSurat   : '$details.luasSurat',
                        notaris     : {$ifNull : [{$arrayElemAt : ['$praNotaris.identifier',-1]}, '']},
                        desa        : '$details.desa',
                        pemilik     : '$details.pemilik',
                        group       : '$details.group'
                    }}").ToList();

                var xlst = ExpressionFilter.Evaluate(bidangDeals, typeof(List<BidangsDealView>), typeof(BidangsDealView), gs);
                var filteredData = xlst.result.Cast<BidangsDealView>().ToList();
                return Ok(filteredData.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpPost("request/measurement")]
        public IActionResult CreateFormMeasurementRequest([FromQuery] string token, [FromBody] MeasurmentReqCore core)
        {
            try
            {
                user user = contextplus.FindUser(token);

                if (string.IsNullOrEmpty(core.reason))
                    return new UnprocessableEntityObjectResult("Alasan Request Harus diisi !");
                if (core.keyDetails.Count() == 0)
                    return new UnprocessableEntityObjectResult("Tidak ada bidang yang dipilih !");

                DateTime currentTime = DateTime.Now;
                foreach (var item in core.keyDetails)
                {
                    var request = contextplus.measurement.FirstOrDefault(m => m.key == item) ?? new MeasurementRequest();
                    bool isNewReq = string.IsNullOrEmpty(request.key);
                    request = request.FromCore(item, core.reason, currentTime, user.key);
                    var history = new HistoryMeasurementRequest()
                    {
                        key = request.key,
                        reason = request.reason,
                        requestDate = request.requestDate,
                        requestor = request.requestor
                    };
                    request.AddHistory(history);

                    if (isNewReq)
                        contextplus.measurement.Insert(request);
                    else
                        contextplus.measurement.Update(request);
                    contextplus.SaveChanges();
                }

                var view = GetMeasurementRequestForm(core, user, currentTime);

                return new JsonResult(view);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpGet("request/measurement/history")]
        public IActionResult GetHistoryMeasurementRequest([FromQuery] string token, [FromQuery] string keyDetail)
        {
            try
            {
                user user = contextplus.FindUser(token);
                var request = contextplus.measurement.FirstOrDefault(m => m.key == keyDetail) ?? new MeasurementRequest();
                List<user> listUser = contextplus.GetDocuments(new user(), "securities",
                              "{$match: {$and: [{'_t': 'user'}, {'invalid' : {$ne: true}}]}}").ToList();

                var view = request.historyRequest.OrderByDescending(h => h.requestDate).Select(x => new HistoryMeasurementRequestView()
                {
                    key = x.key,
                    reason = x.reason,
                    requestor = listUser.FirstOrDefault(u => u.key == x.requestor)?.FullName,
                    requestDate = x.requestDate
                }).ToList();

                return Ok(view);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }



        private async Task<(bool OK, string err)> TransferBundle(PraPembebasan praDeal, DetailsPraBebas detail, user user, IServiceProvider services, Persil persil)
        {
            MyTracer.TraceInfo2($"key persil: {detail.keyPersil}");

            var preBundle = bhost.PreBundleGet(detail.key).GetAwaiter().GetResult() as PreBundle;
            if (preBundle == null)
                return (false, "Bundle dimaksud tidak ada");

            var insta = praDeal.instance;
            var subinst = insta.FindDoc(detail.key);
            if (subinst == null)
                return (false, "Detail Pra Pembebasan tidak mempunyai flow");

            MainBundle main = contextplus.mainBundles.FirstOrDefault(b => b.key == persil.key);
            //if (main != null)
            //    contextplus.mainBundles.Remove(main);
            //MakeBundle(persil.key);
            //bhost.BundleReload(persil.key);
            //MainBundle bundle = bhost.MainGet(persil.key).GetAwaiter().GetResult() as MainBundle;

            if (main == null)
            {
                MakeBundle(persil.key);
                bhost.BundleReload(persil.key);
            }

            MainBundle bundle = bhost.MainGet(persil.key).GetAwaiter().GetResult() as MainBundle;
            if (bundle == null)
                return (false, "Bundle tidak terbentuk");

            bundle.IdBidang = persil.IdBidang;
            var joinedBundle = bundle.doclist.GroupJoin(preBundle.doclist,
                                                      main => main.docType,
                                                      pre => pre.docType,
                                                      (main, pre) => new BundledDoc(main.keyDocType)
                                                      {
                                                          keyDocType = main.keyDocType,
                                                          SpecialReqs = pre.FirstOrDefault()?.SpecialReqs ?? main.SpecialReqs,
                                                          reservations = pre.FirstOrDefault()?.reservations ?? main.reservations,
                                                          entries = pre.FirstOrDefault()?.entries ?? main.entries
                                                      });

            bundle.doclist = new ObservableCollection<BundledDoc>(joinedBundle);

            bool OK = true;
            List<string> errors = new List<string>();
            List<string> stringDoc = new List<string>();

            foreach (var doc in preBundle.doclist.Where(x => x.entries.Count() != 0))
            {
                ValidateEntry<ParticleDocChain> lastEntries = doc.entries.LastOrDefault();
                bool docIsExist = lastEntries.Item.Values.Any(v => v.exists.Any(ex => ex.ex == Existence.Soft_Copy && ex.cnt == 1));

                if (docIsExist || doc.keyDocType.ToLower() == "jdok066")
                {
                    (var ok, var error) = RenamePraDeal(praDeal.key, detail.key, detail.keyPersil, doc.keyDocType, user.key);
                    OK = OK && ok;
                    if (!ok)
                        errors.Add($"[{doc.keyDocType}]{error}");
                    stringDoc.Add(doc.keyDocType);
                }
            }


            var logPreBundle = contextplus.logPreBundle.All().Where(x => x.keyPersil == detail.key);
            foreach (var item in logPreBundle)
            {
                var logBundle = new LogBundle(persil.key, item.keyDocType, item.keyCreator, item.created.GetValueOrDefault(), item.activityType, item.modul);
                contextplus.logBundle.Insert(logBundle);
                contextplus.SaveChanges();
            }

            MyTracer.TraceInfo2($"errors = {errors}");
            if (OK)
            {
                var oldDetail = praDeal.details.Where(x => x.key != detail.key).ToList();
                detail.keyBundle = detail.keyPersil;
                oldDetail.Add(detail);
                praDeal.details = oldDetail.ToArray();
                contextplus.praDeals.Update(praDeal);
                contextplus.SaveChanges();

                var logDeal = new LogDeal();
                logDeal.keyPersil = detail.keyPersil;
                logDeal.tanggalKesepakatan = praDeal.tanggalProses;
                logDeal.jenisDokumen = stringDoc.ToArray();
                contextplus.logDeal.Insert(logDeal);
                contextplus.SaveChanges();

                bhost.MainUpdate(bundle, true).GetAwaiter().GetResult();
                //bhost.PreDelete(detail.key, true).Wait();
                //bhost.PreUpdate(preBundle, true).Wait();
            }

            return (OK, string.Join(",", errors));
        }

        private (bool OK, string err) RenamePraDeal(string keyAssign, string keyBundleOld, string keyBundleNew, string keyDocType, string userkey)
        {
            try
            {
                var bucket = fgrid.GetBucket();
                var oldname = $"{keyAssign}-{keyBundleOld}{keyDocType}";
                var newname = $"{keyBundleNew}{keyDocType}";
                var files = bucket.Find($"{{filename: '{oldname}'}}").ToList();

                foreach (var file in files)
                {
                    if (file == null)
                        return (false, "document not found");
                    bucket.Rename(file.Id, newname);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static DynElement[] MakeLayout2(ResultDoc doc, bool editable = false)
        {
            var docname = DocType.List.FirstOrDefault(d => d.invalid != true && d.key == doc.keyDT)?.identifier;
            var dyns = doc.props.Select(k => new DynElement
            {
                visible = "true",
                editable = $"{editable}".ToLower(),
                group = $"{docname}|Properties",
                label = k.mkey.ToString("g").Replace("_", " "),
                value = $"#props.${k.mkey:g}",
                nullable = true,
                cascade = "",
                dependency = "",
                correction = false,
                inittext = "",
                options = "",
                swlabels = new string[0],
                type = k.val.type switch
                {
                    Dynamic.ValueType.Int => ElementType.Numeric,
                    Dynamic.ValueType.Number => ElementType.Number,
                    Dynamic.ValueType.Bool => ElementType.Check,
                    Dynamic.ValueType.Date => ElementType.Date,
                    _ => ElementType.Text,
                },
            });

            var exi = new (Existence key, string type, string label)[]
            {
            //(Existence.Soft_Copy,"Check","Di-scan"),
            (Existence.Asli,"Check","Asli"),
                (Existence.Copy,"Numeric","Copy"),
                (Existence.Salinan,"Numeric","Salinan"),
                (Existence.Legalisir,"Numeric","Legalisir"),
                //(Existence.Avoid,"Check","Tidak Diperlukan/Memo"),
            };

            var existencies = exi.Select(x => new DynElement
            {
                visible = "true",
                editable = $"{editable}".ToLower(),
                group = $"{docname}|Keberadaan",
                label = x.label,
                value = $"#{x.key:g}",
                nullable = false,
                cascade = "",
                dependency = "",
                correction = false,
                inittext = "",
                options = "",
                swlabels = new string[0],
                xtype = x.type
            });

            var res = dyns.Union(existencies).ToArray();
            return res;
        }

        private void MakeDealBundle(DealBundle template, string reqKey, string detKey)
        {
            if (template == null)
                return;
            DealBundle dealBundle = template;
            dealBundle._id = ObjectId.Empty;
            dealBundle.keyPraDeals = reqKey;
            dealBundle.keyParent = detKey;
            dealBundle.key = mongospace.MongoEntity.MakeKey;
            contextplus.DealBundle.Insert(dealBundle);
            contextplus.SaveChanges();
        }

        private void MakeBundle(string detKey)
        {
            var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();
            if (template == null)
                return;
            MainBundle bundle = template;
            bundle._id = ObjectId.Empty;
            bundle.key = detKey;
            bundle.IdBidang = "";
            contextplus.mainBundles.Insert(bundle);
            contextplus.SaveChanges();

            var bhost = new BundlerHostConsumer();
            bhost.BundleReload(detKey);
        }

        private void MakePreBundle(string detKey)
        {
            var template = contextplus.GetCollections(new PreBundle(), "bundles", "{key:'templatePreBundle'}", "{}").FirstOrDefault();
            if (template == null)
                return;
            PreBundle preBundle = template;
            preBundle._id = ObjectId.Empty;
            preBundle.key = detKey;
            preBundle.IdBidang = "";
            contextplus.preBundles.Insert(preBundle);
            contextplus.SaveChanges();

            var bhost = new BundlerHostConsumer();
            bhost.PreUpdate(preBundle, true);
        }

        //[PersilMaterializer]
        //private void UpdatePersil(user user, Persil persil, PraPembebasan praDeal, DetailsPraBebas details)
        //{
        //    var item = new PersilBasic();
        //    item = persil.basic.current;
        //    item.surat.nomor = details.alasHak;
        //    item.pemilik = details.pemilik;
        //    item.alias = details.alias;
        //    item.group = details.group;
        //    item.luasSurat = details.luasSurat;
        //    item.satuan = details.hargaSatuan;

        //    persil.basic.current.surat.nomor = details.alasHak;
        //    persil.basic.current.pemilik = details.pemilik;
        //    persil.basic.current.alias = details.alias;
        //    persil.basic.current.group = details.group;
        //    persil.basic.current.luasSurat = details.luasSurat;
        //    persil.PraNotaris = details.notaris;
        //    persil.basic.current.satuan = details.hargaSatuan;

        //    var newEntries1 = new ValidatableEntry<PersilBasic>
        //    {
        //        created = DateTime.Now,
        //        en_kind = ChangeKind.Update,
        //        keyCreator = praDeal.keyCreator,
        //        keyReviewer = user.key,
        //        reviewed = DateTime.Now,
        //        approved = true,
        //        item = item
        //    };

        //    //Update Categories
        //    if (!string.IsNullOrEmpty(details.keyFollowUp))
        //    {
        //        var followUpMkt = GetBidangFollowUp(details.keyFollowUp);
        //        var fuCat = followUpMkt.categories.ToList();
        //        var listCat = persil.categories.ToList();
        //        listCat.AddRange(fuCat);
        //        persil.categories = listCat.ToArray();
        //    }

        //    persil.basic.entries.Add(newEntries1);
        //    contextplus.persils.Update(persil);
        //    contextplus.SaveChanges();

        //    //Create Request Persil Approval
        //    string discriminator = persil.en_state == StatusBidang.bebas ? "EDIT_SB" : "EDIT_BB";
        //    string jrIdentifier = (persil.en_state ?? 0) == StatusBidang.bebas ? "UPD-SB" : "UPD-BB";


        //    MyTracer.TraceInfo2($"PraPembebasan - UpdatePersil : Create request Pemutakhiran for Persil : {persil.key}");
        //    PersilRequest newPersil = new PersilRequest(user, persil.key, discriminator);
        //    var dno = contextplus.docnoes.FirstOrDefault(d => d.key == "PersilApproval");
        //    newPersil.identifier = dno.Generate(DateTime.Now, true, string.Empty);
        //    newPersil.identifier = newPersil.identifier.Replace("{JR}", jrIdentifier);
        //    HostReqPersil.Add(newPersil);

        //    MyTracer.TraceInfo2("PraPembebasan - UpdatePersil : Update Log worklist");
        //    var lastState = newPersil.instance.lastState;
        //    LogWorkListCore logWLCore = new LogWorkListCore(newPersil.instKey, user.key, lastState.time,
        //                     lastState?.state ?? ToDoState.unknown_, ToDoVerb.create_, "");

        //    MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

        //}
        private void UpdatePersil(user user, Persil persil, PraPembebasan praDeal, DetailsPraBebas details)
        {
            var item = new PersilBasic();
            item = persil.basic.current;
            item.surat.nomor = details.alasHak;
            item.pemilik = details.pemilik;
            item.alias = details.alias;
            item.group = details.group;
            item.luasSurat = details.luasSurat;
            item.satuan = details.hargaSatuan;
            item.en_jenis = details.jenisAlasHak ?? JenisAlasHak.unknown;

            persil.basic.current.en_jenis = details.jenisAlasHak ?? JenisAlasHak.unknown;
            persil.basic.current.surat.nomor = details.alasHak;
            persil.basic.current.pemilik = details.pemilik;
            persil.basic.current.alias = details.alias;
            persil.basic.current.group = details.group;
            persil.basic.current.luasSurat = details.luasSurat;
            persil.PraNotaris = details.notaris;
            persil.basic.current.satuan = details.hargaSatuan;

            var newEntries1 = new ValidatableEntry<PersilBasic>
            {
                created = DateTime.Now,
                en_kind = ChangeKind.Update,
                keyCreator = praDeal.keyCreator,
                keyReviewer = user.key,
                reviewed = DateTime.Now,
                approved = true,
                item = item
            };

            //Update Categories
            if (!string.IsNullOrEmpty(details.keyFollowUp))
            {
                var followUpMkt = GetBidangFollowUp(details.keyFollowUp);
                var fuCat = followUpMkt.categories.ToList();
                var listCat = persil.categories.ToList();
                listCat.AddRange(fuCat);
                persil.categories = listCat.ToArray();
            }

            persil.basic.entries.Add(newEntries1);
            contextplus.persils.Update(persil);
            contextplus.SaveChanges();

            var last = persil.basic.entries.LastOrDefault();

            if (last != null && last.reviewed == null)
            {
                var item2 = new PersilBasic();
                item2 = last.item;
                item2.surat.nomor = details.alasHak;
                item2.pemilik = details.pemilik;
                item2.group = details.group;
                item2.luasSurat = details.luasSurat;
                item2.satuan = details.hargaSatuan;
                item2.alias = details.alias;
                item2.en_jenis = details.jenisAlasHak ?? JenisAlasHak.unknown;

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
                contextplus.persils.Update(persil);
                contextplus.SaveChanges();
            }
        }

        private void UpdateStatusDeal(Persil persil, user user, string pembayaran = null)
        {
            var deal = pembayaran switch
            {
                "dp" => StatusDeal.Deal1,
                "lunas" => StatusDeal.Deal1,
                _ => StatusDeal.Deal
            };

            var dealstatus = new deals
            {
                deal = DateTime.Now,
                dealer = user.key,
                status = deal
            };
            var lst = new List<deals>();
            if (persil.dealStatus != null)
                lst = persil.dealStatus.ToList();
            lst.Add(dealstatus);

            persil.dealStatus = lst.ToArray();
            contextplus.persils.Update(persil);
            contextplus.SaveChanges();
        }

        private (bool, string) TransferToBundle(PraPembebasan praDeal, user user, string detKey)
        {
            (bool Ok, string error) = (true, null);
            var details = praDeal.details.FirstOrDefault(x => x.key == detKey);
            if (string.IsNullOrEmpty(details.keyPersil))
                return (Ok, error) = (false, "Bidang belum ditentukan");

            var persil = GetPersil(details.keyPersil);
            string configDocDb = configuration.GetSection("data").GetSection("database").Value;
            string otherServer = configDocDb.Contains("new") ? "https://cs.agungsedayu.com:4567" : "https://land.agungsedayu.com:4567";

            if (persil == null)
                return (Ok, error) = (false, $"Bidang tidak ditemukan di server ini, Harap untuk melanjutkan di {otherServer}");

            (Ok, error) = ValidasiBidang(details.keyPersil, "mapReview");
            if (!Ok)
                return (Ok, error);

            // Untuk Bidang
            //if (!ValidatePersilRequest(details.keyPersil))
            //    return (Ok, error) = (false, "Mohon Selesaikan Terlebih Dahulu Request Perubahan Data untuk Bidang yang dipilih !");

            var bucket = fgrid.GetBucket();
            var oldname = $"{praDeal.key}-{detKey}JDOK066";
            var lastfile = bucket.Find($"{{filename: '{oldname}'}}").ToList().LastOrDefault();
            if (lastfile == null)
                return (Ok, error) = (false, "Peta Lokasi belum di upload");

            //UpdateMainBundle(persil, detKey);
            UpdateStatusDeal(persil, user, null);
            UpdatePersil(user, persil, praDeal, details);
            var (ok, err) = TransferBundle(praDeal, details, user, services, persil).GetAwaiter().GetResult();

            if (!ok)
                (Ok, error) = (false, err);
            return (Ok, error);
        }

        private string GetActualStatus(GraphMainInstance inst, string routekey, string state = null)
        {
            if (routekey == null)
                return inst.lastState?.state.AsStatus() ?? ToDoState.unknown_.AsStatus();
            var sub = inst.children.Select(c => c.Value).FirstOrDefault(c => c.Core.nodes.OfType<GraphNode>().Any(n => n.routes.Any(r => r.key == routekey)));
            var sub2 = sub.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == routekey));
            return sub2.status.ToString();
        }

        private ToDoState GetActualState(GraphMainInstance inst, string routekey)
        {
            if (routekey == null)
                return inst.lastState?.state ?? ToDoState.unknown_;

            var sub = inst.children.Select(c => c.Value).FirstOrDefault(c => c.Core.nodes.OfType<GraphNode>().Any(n => n.routes.Any(r => r.key == routekey)));
            return sub?.lastState?.state ?? inst.lastState?.state ?? ToDoState.unknown_;
        }

        private (bool, string) UTJApproved(PraPembebasan praDeal, user user, string detKey)
        {
            var details = praDeal.details.FirstOrDefault(x => x.key == detKey.Trim());

            bool ok = false;
            string message = string.Empty;

            if (details.jenisAlasHak != null && details.kepemilikan != null)
            {
                (ok, message) = BidangChecking(details.key, details.keyPersil, "UTJ");
                if (!ok)
                    return (false, message);
            }
            else
            {
                (ok, message) = ValidasiBidang(details.keyPersil);
                if (!ok)
                    return (false, message);
            }

            var oldDetail = praDeal.details.Where(x => x.key != detKey).ToList();
            details.dealUTJ = DateTime.Now;
            oldDetail.Add(details);
            praDeal.details = oldDetail.ToArray();
            contextplus.praDeals.Update(praDeal);
            contextplus.SaveChanges();

            var persil = GetPersil(details.keyPersil);
            persil.dealer = user.key;
            persil.deal = praDeal.tanggalProses;
            contextplus.persils.Update(persil);
            contextplus.SaveChanges();

            return (true, null);
        }
        private (bool, string) DpApproved(PraPembebasan praDeal, user user, string detKey)
        {
            var details = praDeal.details.FirstOrDefault(x => x.key == detKey.Trim());

            (bool ok, string message) = ValidasiBidang(details.keyPersil);
            if (!ok)
                return (false, message);

            var oldDetail = praDeal.details.Where(x => x.key != detKey).ToList();
            details.dealDP = DateTime.Now;
            oldDetail.Add(details);
            praDeal.details = oldDetail.ToArray();
            contextplus.praDeals.Update(praDeal);
            contextplus.SaveChanges();

            var persil = GetPersil(details.keyPersil);
            UpdateStatusDeal(persil, user, "dp");
            persil.dealer = user.key;
            persil.deal = praDeal.tanggalProses;
            contextplus.persils.Update(persil);
            contextplus.SaveChanges();

            return (true, null);
        }

        private (bool, string) LunasApproved(PraPembebasan praDeal, user user, string detKey)
        {
            var detail = praDeal.details.FirstOrDefault(x => x.key == detKey);
            (bool ok, string message) = ValidasiBidang(detail.keyPersil);
            if (!ok)
                return (false, message);

            var oldDetail = praDeal.details.Where(x => x.key != detKey).ToList();
            detail.dealLunas = DateTime.Now;
            oldDetail.Add(detail);
            praDeal.details = oldDetail.ToArray();
            contextplus.praDeals.Update(praDeal);
            contextplus.SaveChanges();

            var persil = GetPersil(detail?.keyPersil);

            if (persil.deal == null)
            {
                UpdateStatusDeal(persil, user, "lunas");
                persil.dealer = user.key;
                persil.deal = praDeal.tanggalProses;
                //persil.dealSystem = detail.dealLunas;
                contextplus.persils.Update(persil);
                contextplus.SaveChanges();
            }

            return (true, null);
        }

        private ReturnPost InsertDetails(PraPembebasan praDeal, DetailsPraBebas detail, user user, DetailsPraBebasCore core, BidangFollowUp followUpMkt)
        {
            string keyPersilCat = UpdatePersilCategories(user, detail, core);
            detail = detail.FromCore(user, core, detail.infoes, followUpMkt?.categories, keyPersilCat);
            core.detKey = detail.key;

            UpdateFormPraBebas(core);

            detail.keyForm = contextplus.formPraBebas.FirstOrDefault(x => x.keyPraDeal == praDeal.key && x.keyDetailPraDeal == detail.key)?.key;
            List<DetailsPraBebas> listDetail = praDeal.details.ToList();
            listDetail.Add(detail);
            praDeal.details = listDetail.ToArray();
            contextplus.praDeals.Update(praDeal);
            contextplus.SaveChanges();
            MakePreBundle(detail.key);
            if (detail.jenisAlasHak != null && detail.kepemilikan != null)
                CreateDocSettings(user, detail.key, detail.jenisAlasHak, detail.kepemilikan);

            var value = new ReturnPost(praDeal.key);

            return value;
        }

        private bool IsPersilChange(Persil old, DetailsPraBebas new_)
        {
            if
            (
                old.PraNotaris != new_.notaris ||
                old.basic.current.pemilik != new_.pemilik ||
                old.basic.current.group != new_.group ||
                old.basic.current.luasSurat != new_.luasSurat ||
                old.basic.current.surat.nomor != new_.alasHak ||
                old.basic.current.satuan != new_.hargaSatuan ||
                old.basic.current.alias != new_.alias
            )
                return true;
            else
                return false;
        }

        private (bool, string) Reject(PraPembebasan praDeal, user user, string detKey, string reason, JenisBayar jenisBayar)
        {
            var newdDetail = praDeal.details.FirstOrDefault(x => x.key == detKey);
            (bool ok, string message) = ValidasiBidang(newdDetail.keyPersil, "rejection");
            if (!ok)
                return (false, message);

            var oldDetail = praDeal.details.Where(x => x.key != detKey).ToList();

            var oldReason = newdDetail.reasons.ToList();
            var newReeason = new ReasonPraDeal(reason, jenisBayar, DateTime.Now, user.key);
            oldReason.Add(newReeason);
            newdDetail.reasons = oldReason.ToArray();
            oldDetail.Add(newdDetail);
            praDeal.details = oldDetail.ToArray();

            contextplus.praDeals.Update(praDeal);
            contextplus.SaveChanges();

            return (true, null);
        }

        private void UpdateFormPraBebas(DetailsPraBebasCore core)
        {
            var form = new FormPraBebas();
            form = form.FromCore(core);
            contextplus.formPraBebas.Insert(form);
            contextplus.SaveChanges();
        }

        private string UpdatePersilCategories(user user, DetailsPraBebas detail, DetailsPraBebasCore core = null, string keyPersil = null)
        {
            var persilCat = GetPersilCategories(detail.keyPersilCat);
            List<category> listCat = persilCat == null ? new List<category>() : persilCat.categories1.ToList();
            string[] coreCat = core != null ? core.Categories.Select(x => string.IsNullOrWhiteSpace(x) ? "" : x).ToArray() : new string[0];
            var cat = new category(user)
            {
                tanggal = DateTime.Now,
                keyCategory = coreCat
            };
            listCat.Add(cat);

            if (persilCat == null && coreCat != new string[0])
            {
                persilCat = new PersilCategories();
                persilCat.key = entity.MakeKey;
                persilCat.keyPersil = detail.keyPersil;
                persilCat.categories1 = listCat.ToArray();
                contextplus.persilCat.Insert(persilCat);
                contextplus.SaveChanges();
            }
            else
            {
                var persilCatPraDeals = GetPersilCategories(detail.keyPersilCat, detail.keyPersil);
                var lastCat = persilCat.categories1.LastOrDefault();
                string[] keyCat = lastCat.keyCategory;
                bool isCatSame = coreCat.SequenceEqual(keyCat);
                if (persilCat.key != detail.keyPersilCat && persilCat != null && !string.IsNullOrEmpty(detail.keyPersil))
                {
                    var listCatPraDeals = persilCat.categories1.ToList();
                    persilCatPraDeals.categories1 = listCatPraDeals.ToArray();
                    contextplus.persilCat.Remove(persilCat);
                    contextplus.SaveChanges();
                    contextplus.persilCat.Update(persilCatPraDeals);
                }
                else if (core != null)
                {
                    persilCat.keyPersil = detail.keyPersil;
                    persilCat.categories1 = listCat.ToArray();
                    contextplus.persilCat.Update(persilCat);
                }
                else
                {
                    persilCat.keyPersil = keyPersil;
                    contextplus.persilCat.Update(persilCat);
                }

                contextplus.SaveChanges();
            }
            return persilCat.key;
        }

        private PersilCategories GetPersilCategories(string keyPersilcat, string keyPersil = null)
        {
            if (!string.IsNullOrEmpty(keyPersil))
                return contextplus.persilCat.FirstOrDefault(pc => pc.keyPersil == keyPersil);
            else
                return contextplus.persilCat.FirstOrDefault(pc => pc.key == keyPersilcat);
        }

        private class GroupBidang
        {
            public string key { get; set; }
            public string _t { get; set; }
            public string IdBidang { get; set; }
            public string keyProject { get; set; }
            public string project { get; set; }
            public string keyDesa { get; set; }
            public string desa { get; set; }
            public StatusBidang? en_state { get; set; }
            public DateTime? deal { get; set; }
            public double? luasDibayar { get; set; }
            public double? luasSurat { get; set; }
            public double? luasUkur { get; set; }
            public bool? luasFix { get; set; }
            public string Pemilik { get; set; }
            public string group { get; set; }
            public string AlasHak { get; set; }
            public double? satuan { get; set; }
            public double? total { get; set; }
            public string noPeta { get; set; }
            public string keyPenampung { get; set; }
            public string keyPTSK { get; set; }

            public Persil persil(LandropePlusContext context) => context.persils.FirstOrDefault(p => p.key == key);

            public GroupBidangView toView2(List<(string key, double luas)> luasDocs)
            {
                var view = new GroupBidangView();
                double luas = 0;

                var luasNIB = luasDocs.Where(x => x.key == this.key) == null ? 0 : luasDocs.Where(x => x.key == this.key).FirstOrDefault().luas;
                var luasSurat = this.luasSurat == null ? 0 : Convert.ToDouble(this.luasSurat);
                var luasDibayar = this.luasDibayar == null ? 0 : Convert.ToDouble(this.luasDibayar);

                if (luasFix != true)
                    luas = luasNIB == 0 ? luasDibayar : luasNIB;
                else
                    luas = luasDibayar;

                (
                    view.key, view.IdBidang, view.en_state, view.deal, view.luasSurat, view.luasDibayar, view.satuan, view.total,
                    view.AlasHak, view.Pemilik, view.noPeta, view.keyPenampung, view.keyPTSK,
                    view.keyProject, view.keyDesa, view.project, view.desa, view.group
                )
                =
                (
                    key, IdBidang,
                    (Enum.TryParse<StatusBidang>(en_state.ToString(), out StatusBidang stp) ? stp : default),
                    deal, luasSurat, luas, satuan, (luas * satuan), AlasHak, Pemilik, noPeta, keyPenampung, keyPTSK,
                    this.keyProject, this.keyDesa, this.project, this.desa, this.group
                );

                return view;
            }
        }

        private Persil GetPersil(string key)
        {
            return contextplus.persils.FirstOrDefault(p => p.key == key);
        }

        private List<Worker> GetDataWorker()
        {
            var dataWorker = contextplus.db.GetCollection<Worker>("workers").Find("{_t:'worker',invalid:{$ne:true}}").ToList();
            return dataWorker;
        }

        private List<Category> GetDataCategory(int bebas = 0)
        {
            var categories = contextplus.GetCollections(new Category(), "categories_new",
                $"<bebas: {bebas}>".MongoJs(),
                "{_id:0}").ToList();
            return categories;
        }

        private BidangFollowUp GetBidangFollowUp(string keyFollowUp)
        {
            return contextpay.bidangFollowUp.FirstOrDefault(x => x.key == keyFollowUp);
        }

        private (bool, double) AnyRestLuasFollowUp(string keyFollowUp, double? luasFollowUp, string keyDetail = "", double? inputLuas = 0)
        {
            var claimedLuas = contextplus.GetDocuments(new { claimedLuas = (double)0 }, "praDeals",
                "{$match: {'invalid' : {$ne: true }}}",
                "{$unwind: '$details'}",
               @"{$match: {$and: [ 
                                    {'details.keyFollowUp': '" + keyFollowUp + @"'},
                                    {'details.key': {$ne: '" + keyDetail + @"'}}
                           ]}}",
                "{$project: {_id:0, claimedLuas : {$ifNull:['$details.luasSurat', 0]}}}"
                ).Sum(x => x.claimedLuas);

            double sisaLuas = luasFollowUp.GetValueOrDefault(0) - claimedLuas - (keyDetail != "" ? (inputLuas ?? 0) : 0);

            if (sisaLuas > 0)
                return (true, sisaLuas);
            else
                return (false, sisaLuas);
        }

        /// <summary>
        /// Use to Check and  Transfer old PreBundle (in detail.infoes) to The Newest Model of PreBundle (in bundle collection)
        /// </summary>
        private (bool, string) CheckAndUpdatePreBundle(string keyPraDeals, string keyDetail, user user)
        {
            (bool ok, string error) = (true, "success");

            var praDeal = contextplus.praDeals.FirstOrDefault(x => x.key == keyPraDeals);
            var detail = praDeal.details.FirstOrDefault(x => x.key == keyDetail);
            if (detail.infoes != null)
            {
                (ok, error) = TransferToNewPreBundle(praDeal, detail, services).GetAwaiter().GetResult();
            }
            return (ok, error);
        }

        /// <summary>
        /// Transfer Pre Bundle Lama ke Pre Bundle Baru
        /// </summary>
        private async Task<(bool OK, string err)> TransferToNewPreBundle(PraPembebasan praDeal, DetailsPraBebas detail, IServiceProvider services)
        {
            MyTracer.TraceInfo2($"key persil: {detail.keyPersil}");
            var bundle = contextplus.bundles.FirstOrDefault(x => x.key == detail.key);
            if (bundle != null)
            {
                contextplus.bundles.Remove(bundle);
                contextplus.SaveChanges();
            }

            MakePreBundle(detail.key);
            var preBundle = bhost.PreBundleGet(detail.key).GetAwaiter().GetResult() as PreBundle;

            var infoes = detail.infoes.Select(i => (i.keyDT,
                    part: new ParticleDoc
                    {
                        props = i.props.Select(j => j.AsKV()).ToList().Distinct()
                                                .ToDictionary(x => x.Key, x => x.Value),
                        exists = i.exs
                    }));

            infoes.Select(i => i.keyDT).ToList().ForEach(i =>
            {
                if (!preBundle.doclist.Any(d => d.keyDocType == i))
                    preBundle.doclist.Add(new BundledDoc(i));
            });

            var bdocs = infoes.Join(preBundle.doclist, i => i.keyDT, d => d.keyDocType, (i, d) => (d, i.part));
            MyTracer.TraceInfo2($"bdocs count: {bdocs.Count()}");
            foreach (var bd in bdocs)
            {
                if (bd.part.props.Count() == 0)
                    continue;
                var pd = new ParticleDocChain(new[] { new KeyValuePair<string, ParticleDoc>(MongoEntity.MakeKey, bd.part) });
                MyTracer.TraceInfo2($"doc in bdocs: {pd.Select(x => (x.Key, value: x.Value.props.Select(p => (p.Key, p.Value.type, p.Value.val)).ToArray())).ToArray()}");
                var vale = new ValDocList
                {
                    approved = true,
                    created = DateTime.Now,
                    keyCreator = praDeal.keyCreator,
                    keyReviewer = praDeal.keyCreator,
                    kind = ChangeKind.Add,
                    reviewed = DateTime.Now,
                    sourceFile = $"PraDeals {praDeal.identifier}",
                    Item = pd
                };
                MyTracer.TraceInfo2($"vale = {JsonConvert.SerializeObject(vale)}");
                bd.d.Add(vale);
                bd.d.current = pd;
            }
            bool OK = true;
            List<string> errors = new List<string>();
            MyTracer.TraceInfo2($"errors = {errors}");
            bhost.PreUpdate(preBundle, true).Wait();
            contextplus.preBundles.Update(preBundle);
            contextplus.SaveChanges();
            return (OK, string.Join(",", errors));
        }

        private bool IsPersilRequestExists(string keyPersil)
        {
            PersilRequest persilRequest = contextpay.persilApproval.FirstOrDefault(pA => pA.info.keyPersil == keyPersil);
            if (persilRequest == null)
                return false;
#if test
            GraphMainInstance instances = ghost.Get(persilRequest.instKey).GetAwaiter().GetResult();
#else
            GraphMainInstance instances = graphost.Get(persilRequest.instKey);
#endif
            if (instances.lastState?.state != ToDoState.complished_)
                return true;
            else
                return false;
        }

        [HttpGet("reload-data")]
        public IActionResult ReloadData([FromQuery] string key)
        {
            try
            {
                var main = gContext.MainInstances.FirstOrDefault(x => x.key == key);
                if (main != null)
                {
#if test
                    ghost.GraphReload(key).GetAwaiter().GetResult();
#else
                    var mainMemory = graphost.Get(key);
                    if (mainMemory != null)                        
                        graphost.ReloadData(mainMemory);
                    else
                        graphost.ReloadData(main);
#endif


                }

                var bundle = contextplus.mainBundles.FirstOrDefault(x => x.key == key);
                if (bundle != null)
                    bhost.BundleReload(key);

                var pradeals = contextplus.preBundles.FirstOrDefault(x => x.key == key);
                if (pradeals != null)
                    bhost.PreBundleReload(key);

                return Ok();
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("template-setting")]
        public IActionResult GetJenisAndOwner()
        {
            try
            {
                var static_Col = contextpay.GetDocuments(new { Jenis = 0, Owner = 0 }, "static_collections",
                    "{'$match' : {_t : 'TemplateMandatoryDocSetting'}}",
                    "{'$unwind' : '$Details'}",
                    @"{'$project' : {
                        'Jenis' : '$Details.Jenis',
                        'Owner' : '$Details.Owner',
                        _id : 0 }}"
                    ).ToArray();

                var result = static_Col.GroupBy(x => x.Jenis, (y, z) => new TemplateMandatory
                {
                    jenis = Enum.TryParse<JenisAlasHak>(y.ToString(), out JenisAlasHak jns) ? jns : JenisAlasHak.unknown,
                    owner = z.Where(x => x.Jenis == y).Select(x => Enum.TryParse<LandOwnershipType>(x.Owner.ToString(), out LandOwnershipType land) ? land : LandOwnershipType._).ToArray()
                }).ToArray();

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                throw ex;
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
            gContext = new graph.mod.GraphContext(url, "admin");
            gContext.ChangeDB(database);
        }

        public class reloadKeys
        {
            public string[] graphkeys { get; set; }
            public string[] bundlekeys { get; set; }
        }

        private MeasurementRequestFormView GetMeasurementRequestForm(MeasurmentReqCore core, user user, DateTime currentTime)
        {
            string keyDetail = string.Join(",", core.keyDetails.Select(key => $"'{key}'"));

            string mengetahui = contextplus.GetDocuments(new { mengetahui = "" }, "static_collections",
                "{$match : {'_t' : 'StaticSuratRequestPengukuranTanah'}}",
                "{$project : {_id : 0, mengetahui : '$mengetahui'}}"
                ).FirstOrDefault()?.mengetahui;

            var table = contextplus.GetDocuments(new TableMeasurementRequestForm(), "praDeals",
                "{$unwind : '$details'}",
                $"<$match : <'details.key' : <$in : [{keyDetail}]>>>".MongoJs(),
               @"{$project : {
                    _id          :  0,
                    nomorRequest : '$identifier',
                    desa         : '$details.desa',
                    alasHak      : '$details.alasHak',
                    pemilik      : '$details.pemilik',
                    group        : '$details.group'
                }}"
                ).ToArray();

            MeasurementRequestFormView view = new MeasurementRequestFormView()
            {
                requestDate = currentTime,
                requestor = user.FullName,
                Reason = core.reason,
                table = table,
                mengetahui = mengetahui
            };

            return view;
        }

        [HttpPost("fill/log-key")]
        public IActionResult FillLogsNullKey()
        {
            try
            {
                var bundles = contextplus.logBundle.All()
                                         .Where(lb => lb.key == null)
                                         .ToList();

                foreach (var bundle in bundles)
                {
                    bundle.key = GetRandomString(25).ToUpper();
                    contextplus.logBundle.Update(bundle);
                    contextplus.SaveChanges();
                };

                var preBundles = contextplus.logPreBundle.All()
                                         .Where(lb => lb.key == null)
                                         .ToList();
                foreach (var preBundle in preBundles)
                {
                    preBundle.key = GetRandomString(25).ToUpper();
                    contextplus.logPreBundle.Update(preBundle);
                    contextplus.SaveChanges();
                };

                return Ok();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Add by Ricky 20220921
        [HttpPost("update/docsetting")]
        public IActionResult UpdateDocSetting([FromQuery] string token, string key, [FromBody] FormDocSetting docs)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var docSetting = contextpay.docSettings.FirstOrDefault(x => x.key == docs.key);
                if (docSetting == null)
                    return new UnprocessableEntityObjectResult("DocSetting tidak ada");
                if (docs.docsSetting.Any(x => (x.UTJ && !x.DP) || (x.UTJ && !x.Lunas) || (x.DP && !x.Lunas)))
                    return new UnprocessableEntityObjectResult("DocSetting tidak valid!");
                if (docs.docsSetting.Any(x => (x.DP && x.totalSetDP < x.totalSetUTJ) || (x.Lunas && x.totalSetLunas < x.totalSetUTJ) || (x.Lunas && x.totalSetLunas < x.totalSetDP)))
                    return new UnprocessableEntityObjectResult("Total Set tidak valid!");

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();
                if (priv.Intersect(new[] { "DOC_MANDATORY_EDIT_STS" }).Any())
                    docSetting.status = docs.status;
                if (priv.Intersect(new[] { "DOC_MANDATORY_EDIT_REQ" }).Any())
                    docSetting.UpdateDocSetting(docs.docsSetting.Select(x => new DocumentsProperty
                    {
                        keyDocType = x.keyDocType,
                        DP = x.DP,
                        UTJ = x.UTJ,
                        Lunas = x.Lunas,
                        totalSetDP = x.totalSetDP,
                        totalSetLunas = x.totalSetLunas,
                        totalSetUTJ = x.totalSetUTJ
                    }).ToArray(), user.key);
                contextpay.docSettings.Update(docSetting);
                contextpay.SaveChanges();
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

        private string GetRandomString(int stringLength)
        {
            StringBuilder sb = new StringBuilder();
            int numGuidsToConcat = (((stringLength - 1) / 32) + 1);
            Console.WriteLine("String Legnth: " + stringLength + "; Iteration Times: " + numGuidsToConcat);
            for (int i = 1; i <= numGuidsToConcat; i++)
            {
                Console.WriteLine("Appending GUID");
                sb.Append(Guid.NewGuid().ToString("N"));
            }

            Console.WriteLine(sb.ToString() + " -- Final Total Random String");

            return sb.ToString(0, stringLength);
        }


        private (bool, string) ValidasiBidang(string keyPersil, string optional = null)
        {
            //var idBidang = contextplus.GetDocuments(new { IdBidang = "", key = "", en_state = (int?)0 }, "persils_v2",
            //    "{$match : {invalid:{$ne:true}}}",
            //   @"{$lookup: {from: 'maps',let:
            //                               {key : '$basic.current.keyDesa'},
            //                                pipeline:[{$unwind: '$villages'},
            //                                {$match:  {$expr: {$eq:['$villages.key','$$key']}}}
            //                            ], as:'desas'}}",
            //   "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",
            //   "{$match : {'desas' : {$ne : null }}}",
            //   "{$project : { key:1, IdBidang:1, en_state : 1, _id:0 }}").ToList();

            var persil = GetPersil(keyPersil);
            //if ((idBidang.Count(x => x.key == keyPersil) == 0 || persil == null) & optional != "mapReview")
            //    return (false, "Bidang tidak tersedia di server ini !");
            //else if (optional != "rejection" & idBidang.FirstOrDefault(x => x.key == keyPersil)?.en_state == 2)
            //    return (false, "Bidang yang anda pilih berstatus batal !");
            //else
            //    return (true, null);

            if ((persil == null) & optional != "mapReview")
                return (false, "Bidang tidak tersedia di server ini !");
            else if (optional != "rejection" & persil.en_state == StatusBidang.batal)
                return (false, "Bidang yang anda pilih berstatus batal !");
            else
                return (true, null);
        }

        private bool ValidatePersilRequest(string keyPersil)
        {
            PersilRequest existing = contextpay.persilApproval.FirstOrDefault(x => x.info.keyPersil == keyPersil);
            if (existing == null)
                return true;
            ToDoState lastState = existing.instance.lastState?.state ?? ToDoState.unknown_;
            if (lastState != ToDoState.complished_)
                return false;
            return true;
        }

        private double GetJumlahLuasDeal(string keyPraDeal)
        {
            var luasDetails = contextplus.GetDocuments(new { _id = "", totalLuas = (double)(0) }, "praDeals",
                "{$unwind: '$details'}",
                "{$match: {$or: [ {'key':'" + keyPraDeal + "'}, {'keyReff':'" + keyPraDeal + "'}]}}",
               @"{$group: {
                    _id: '$manager', totalLuas: {$sum: {$ifNull: ['$details.luasSurat', 0]} }
                 }}").ToList();
            return luasDetails.Sum(x => x?.totalLuas ?? 0);
        }

        private async Task<bool> AlertAreaRestriction(PraPembebasan praDeal, DetailsPraBebasCore core, user user, double totalLuas)
        {
            var alertSettings = contextplus.GetDocuments(new AlertLuasPraDeal(), "static_collections",
                "{$match: {'_t': 'AlertLuasPraDeal'}}",
                "{$project: {_id:0}}"
                ).FirstOrDefault();
            bool isLuasOver = totalLuas > ((praDeal?.luasDeal ?? 0) + alertSettings.tolerance) && praDeal?.luasDeal != null;
            string receivers = string.Join(";", alertSettings.receivers.Select(x => x?.email ?? "")) ?? "";
            string receiversCC = string.Join(";", alertSettings.receiversCC.Select(x => x?.email ?? "")) ?? "";
            string receiversBCC = string.Join(";", alertSettings.receiversBCC.Select(x => x?.email ?? "")) ?? "";
            if (isLuasOver)
            {
                var mailServices = services.GetService<MailSenderClass>() as MailSenderClass;
                EmailList email = new()
                {
                    Pk = Guid.NewGuid(),
                    Subject = "[Pra Pembebasan Alert: Luas Total Bidang Melebihi Luas Kesepakatan]",
                    Body = @"<h4>Dear All,</h4>
                             <p>Penambahan detail pada kesepakatan dibawah ini telah melebihi luas kesepakatan seharusnya:</p>
                             <br>
                             <table cellpadding='0' cellspacing='0' width='640' border='1' style='margin-bottom:20px;font-size:11px;vertical-align:middle;'>
                                <tr style='font - weight:bold; background:#d3d3d3;'>
                                    <th> No.Request </th>
                                    <th> Pemilik </th>
                                    <th> Group  </th>
                                    <th> Alas Hak </th>
                                    <th> Desa </th>
                                    <th> Luas (m<sup>2</sup>)</th>
                                    <th> Luas Detail Total (m<sup>2</sup>)</th>
                                    <th> Luas Deal (m<sup>2</sup>)</th>
                                    <th> User </th>
                                </tr>
                                <tr>
                                    <td>" + praDeal.identifier + @"</td>
                                    <td>" + core.pemilik + @"</td>
                                    <td>" + core.group + @"</td>
                                    <td>" + core.alasHak + @"</td>
                                    <td>" + core.desa + @"</td>
                                    <td>" + core.luasSurat + @"</td>
                                    <td>" + totalLuas + @"</td>
                                    <td>" + praDeal.luasDeal + @"</td>
                                    <td>" + user.FullName + @"</td>
                                </tr>
                            </table>
                            <p>&nbsp; <br></p>
                            <p align='left'>Regards,</p>
                            <p align='left'>Alert Notification Pra Pembebasan</p>
                            ",
                    Receivers = receivers,
                    ReceiversCC = receiversCC,
                    ReceiversBCC = receiversBCC,
                    ProccessCount = 0,
                    IsSuccess = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Landrope Web Pra Deals Alert Notification"
                };
                await mailServices.SendEmailAsync(email);
            }
            return isLuasOver;
        }

        //[NeedToken("RPT_DETAIL_PRAPEMBEBASAN")]
        [HttpGet("getdetailrpt")]
        public ActionResult GetPraDealsDetailRpt([FromQuery] string token, [FromQuery] string action)
        {
            // WITH MQL (MongoDB Query Language)
            try
            {
                //var user = contextplus.FindUser(token);

                //var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();
                //if (!priv.Contains("RPT_DETAIL_PRAPEMBEBASAN"))
                //    throw new UnauthorizedAccessException();

                var view = contextplus.GetCollections(new PraDealsDtRpt(), "RPT_PraBebas_Details", "{}", "{_id:0}");

                /*
                var rpt = contextpay.GetDocuments(new PraDealsDtRpt(), "praDeals",
                    "{ $unwind : '$details' }",
                    @"{ $lookup: {
                        from: ""persils_v2"",
                        let: { keyPersil : ""$details.keyPersil"" },
                        pipeline : 
                        [
                            { 
                                $lookup:
                                {
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
                                    as:'mapsPersil'
                                }
                            },
                            {
                                $lookup: {
                                    from: ""masterdatas"",
                                    localField: ""PraNotaris"",
                                    foreignField: ""key"",
                                    as: ""notaris""
                                }
                            },
                            {
                                ""$match"": {
                                    ""$expr"": {
                                        ""$eq"": [""$key"", ""$$keyPersil""]
                                    }
                                }
                            }
                        ],
                        as: ""persil""
                    }}",
                    @"{ $lookup: {
                        from:'maps',
                        let: { keyProject : ""$details.keyProject"", keyDesa : ""$details.keyDesa""},
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
                        as:'mapsPraDeals'
                    }}",
                    @"{
                        $lookup: {
                            from: ""masterdatas"",
                            localField: ""details.notaris"",
                            foreignField: ""key"",
                            as: ""notaris""
                        }
                    }",
                    @"{ 
                        $lookup:
                        {
                            from:""workers"",
                            localField: ""manager"",
                            foreignField: ""key"",
                            as: ""dataManager"" 
                        }
                    }",
                    @"{ 
                        $lookup:
                        {
                            from:""workers"",
                            localField: ""sales"",
                            foreignField: ""key"",
                            as: ""dataSales"" 
                        }
                    }",
                    @"{ 
                        $lookup:
                        {
                            from:""categories_new"",
                            localField: ""details.Categories.0"",
                            foreignField: ""key"",
                            as: ""category"" 
                        }
                    }",
                    @"{
                        $addFields: {
                            'bundleExists': {
                                $ifNull: [""$details.keyBundle"",false]
                            }
                        }
                    }",
                    @"{ 
                        $project: {
                            _id: 0,
                            IdBidang: {
                                $cond: [
                                    {
                                        $in: [""$details.keyPersil"",['', null]]
                                    },
                                    '',
                                    { $ifNull: [ { $arrayElemAt: [""$persil.IdBidang"",0]} , ""Id Bidang dari server lain"" ] }
                            ]},
                            Manager: { $ifNull: [ {$arrayElemAt: [""$dataManager.FullName"", 0]} , '' ]},
                            Sales: { $ifNull: [ {$arrayElemAt: [""$dataSales.FullName"", 0]} , '' ]},
                            NoKJB: ""$identifier"",
                            TglKJB: ""$tanggalProses"",
                            NamaNotaris:
                            {
                                $cond: [{ $eq: [ ""$bundleExists"",true ]},
                                        {
                                            $arrayElemAt: [""$persil.notaris.identifier"", 0]
                                        },
                                        {
                                            $arrayElemAt: [""$notaris.identifier"", 0]
                                        }]
                            },
                            TglTandaTerimaNotaris: ""$details.FileToPra"",
                            TglInputTandaTerimaNotaris: ""$details.created"",
                            KategoriGrup:{ $ifNull: [ { ""$arrayElemAt"": [""$category.desc"",0]} , '']},
                            Grup:{
                                    $cond: [{$eq: [""$bundleExists"", true]},
                                    {
                                        $arrayElemAt: [""$persil.basic.current.group"", 0]
                                    },
                                    ""$details.group""
                            ]},
                            NamaPenjual:
                            {
                                $cond: [{$eq: [""$bundleExists"", true]},
                                        {
                                            $ifNull: 
                                            [
                                                { $arrayElemAt: [""$persil.basic.current.pemilik"", 0] } , 
                                                { $arrayElemAt: [""$persil.basic.current.surat.nama"", 0] } 
                                            ]
                                        },
                                        ""$details.pemilik""
                            ]},
                            Project:
                            {
                                $cond: [{ $eq: [ ""$bundleExists"", true]},
                                        {
                                            $arrayElemAt: [""$persil.mapsPersil.namaProject"", 0]
                                        },
                                        {
                                            $arrayElemAt: [""$mapsPraDeals.namaProject"", 0]
                                        }
                            ]},
                            Desa:
                            {
                                $cond: [{$eq: [""$bundleExists"",true]},
                                    {
                                        $arrayElemAt: [""$persil.mapsPersil.namaDesa"", 0]
                                    },
                                    {
                                        $ifNull:[{$arrayElemAt: [""$mapsPraDeals.namaDesa"", 0]}, ""$details.desa""]
                                    }
                            ]},
                            AlasHak:
                            {
                                $cond: [{$eq: [""$bundleExists"", true]},
                                        {
                                            $arrayElemAt: [""$persil.basic.current.surat.nomor"", 0]
                                        },
                                        ""$details.alasHak""
                            ]},
                            Sertifikat:
                            {
                                $cond: [{$eq: [""$bundleExists"", true]},
                                        {
                                            $toString: { $arrayElemAt: [""$persil.basic.current.en_jenis"", 0] }
                                        },
                                        { $toString: ""$details.jenisAlasHak"" }
                            ]},
                            Luas:
                            {
                                $cond: [{ $eq: [""$bundleExists"", true ]},
                                        {
                                            $arrayElemAt: [""$persil.basic.current.luasSurat"", 0]
                                        },
                                        ""$details.luasSurat""
                            ]}
                        }
                    }"
                );
                */

                int count = 1;
                var rpt = view.ToList();
                rpt.ForEach(a => a.No = count++);
                rpt.Where(a => !string.IsNullOrEmpty(a.Sertifikat)).ToList().ForEach(s =>
                {
                    s.Sertifikat = ((JenisAlasHak?)Convert.ToInt32(s.Sertifikat)).ToDesc();
                });
                DataSet sheet = rpt.ToList().ToDataSet();

                DataSExcelSheet[] excelDataSheet = new DataSExcelSheet[] { new DataSExcelSheet { dataS = sheet, sheetName = "Detail Pra Pembebasan" } };

                byte[] resultByte = DataSetHelper.ExportMultipleDataSetToExcelByte(excelDataSheet);

                if (action == "print")
                    return File(resultByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report");
                else
                    return new JsonResult(rpt);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //[NeedToken("RPT_DETAIL_PRAPEMBEBASAN")]
        [HttpGet("getdetailrpt2")]
        public ActionResult GetPraDealsDetailRpt2([FromQuery] string token, [FromQuery] string action)
        {
            // WITH LINQ
            try
            {
                //var user = contextplus.FindUser(token);

                //var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();
                //if (!priv.Contains("RPT_DETAIL_PRAPEMBEBASAN"))
                //    throw new UnauthorizedAccessException();

                var workers = contextplus.workers.Query(x => x.invalid != true);
                var notarists = contextplus.notarists.Query(x => x.invalid != true);
                var categories = contextplus.categorys.Query(x => x.invalid != true && x.segment == 1);
                var villages = contextpay.GetVillages();

                var pradeals = contextplus.praDeals.Query(x => x.invalid != true).ToArray();
                var pradealsDt = pradeals.SelectMany(x => x.details, (y, z) =>
                new
                {
                    keyHd = y.key,
                    noKJB = y.identifier,
                    tglKJB = y.tanggalProses,
                    manager = workers.FirstOrDefault(w => w.key == y.manager)?.FullName,
                    sales = workers.FirstOrDefault(w => w.key == y.sales)?.FullName,
                    notaris = notarists.FirstOrDefault(n => n.key == z.notaris)?.identifier,
                    kategori = categories.FirstOrDefault(c => c.key == z.Categories?.FirstOrDefault())?.desc,
                    project = villages.FirstOrDefault(v => v.project.key == z.keyProject).project?.identity,
                    desa = string.IsNullOrEmpty(z.keyDesa) ? z.desa : villages.FirstOrDefault(v => v.desa.key == z.keyDesa).desa?.identity,
                    details = z
                });

                var keys = pradealsDt.Select(x => x.details.keyPersil).ToList();
                var persils = contextplus.persils.Query(x => keys.Contains(x.key))
                    .Select(x => new
                    {
                        key = x.key,
                        IdBidang = x.IdBidang,
                        group = x.basic?.current?.group,
                        pemilik = x.basic?.current?.pemilik ?? x.basic?.current?.surat?.nama,
                        keyProject = x.basic?.current?.keyProject,
                        keyDesa = x.basic?.current?.keyDesa,
                        project = villages.FirstOrDefault(v => v.desa.key == x.basic?.current?.keyDesa).project?.identity,
                        desa = villages.FirstOrDefault(v => v.desa.key == x.basic?.current?.keyDesa).desa?.identity,
                        alashak = x.basic?.current?.surat?.nomor,
                        en_jenis = x.basic?.current?.en_jenis,
                        luasSurat = x.basic?.current?.luasSurat,
                        praNotaris = notarists.FirstOrDefault(n => n.key == x.PraNotaris)?.identifier
                    });

                var view = pradealsDt.Select(x => new PraDealsDtRpt
                {
                    IdBidang = x.details.keyPersil == null ? "" : persils.FirstOrDefault(p => p.key == x.details?.keyPersil) != null ? persils.FirstOrDefault(p => p.key == x.details?.keyPersil).IdBidang : "Id Bidang di Server Lain",
                    Manager = x.manager,
                    Sales = x.sales,
                    NoKJB = x.noKJB,
                    NamaNotaris = String.IsNullOrEmpty(x.details.keyBundle) ? x.notaris : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.praNotaris,
                    TglTandaTerimaNotaris = x.details.FileToPra,
                    TglInputTandaTerimaNotaris = x.details.created,
                    KategoriGrup = x.kategori,
                    Grup = String.IsNullOrEmpty(x.details.keyBundle) ? x.details.group : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.group,
                    NamaPenjual = String.IsNullOrEmpty(x.details.keyBundle) ? x.details.pemilik : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.pemilik,
                    Project = String.IsNullOrEmpty(x.details.keyBundle) ? x.project : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.project,
                    Desa = String.IsNullOrEmpty(x.details.keyBundle) ? x.desa : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.desa,
                    AlasHak = String.IsNullOrEmpty(x.details.keyBundle) ? x.details.alasHak : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.alashak,
                    Sertifikat = String.IsNullOrEmpty(x.details.keyBundle) ? x.details.jenisAlasHak.ToDesc() : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.en_jenis.ToDesc(),
                    Luas = String.IsNullOrEmpty(x.details.keyBundle) ? x.details.luasSurat : persils.FirstOrDefault(p => p.key == x.details.keyPersil)?.luasSurat,
                });

                int count = 1;
                var listView = view.ToList();
                listView.ForEach(a => a.No = count++);
                DataSet sheet = listView.ToDataSet();

                DataSExcelSheet[] excelDataSheet = new DataSExcelSheet[] { new DataSExcelSheet { dataS = sheet, sheetName = "Detail Pra Pembebasan" } };

                byte[] resultByte = DataSetHelper.ExportMultipleDataSetToExcelByte(excelDataSheet);

                if (action == "print")
                    return File(resultByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report2");
                else
                    return new JsonResult(listView);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> ReminderKelengkapanDokumen(string[] fileLocations)
        {
            var alertSettings = contextplus.GetDocuments(new AlertLuasPraDeal(), "static_collections",
                "{$match: {'_t': 'AlertLuasPraDeal'}}",
                "{$project: {_id:0}}"
                ).FirstOrDefault();

            string receivers = string.Join(";", alertSettings.receivers.Select(x => x?.email ?? "")) ?? "";
            string receiversCC = string.Join(";", alertSettings.receiversCC.Select(x => x?.email ?? "")) ?? "";
            string receiversBCC = string.Join(";", alertSettings.receiversBCC.Select(x => x?.email ?? "")) ?? "";

            var mailServices = services.GetService<MailSenderClass>() as MailSenderClass;
            EmailList email = new()
            {
                Pk = Guid.NewGuid(),
                Subject = "[Pra Pembebasan Alert: Luas Total Bidang Melebihi Luas Kesepakatan]",
                Body = @"",
                Receivers = receivers,
                ReceiversCC = receiversCC,
                ReceiversBCC = receiversBCC,
                ProccessCount = 0,
                IsSuccess = false,
                CreatedAt = DateTime.Now,
                CreatedBy = "Landrope Web Pra Deals Reminder Kelengkapan Dokumen"
            };
            await mailServices.SendEmailAsync(email);

            return true;
        }

        private List<DocumentsProperty> GetDocSettingTemplate(string template)
        {
            var listJnsDoc = contextpay.GetCollections(new DocType(), "jnsDok", "{invalid:{$ne:true}}", "{_id:0}").ToList().ToArray();
            var settingTemplate = contextpay.docSettings.Query(x => x.key == template && x.invalid != true)
                                                    .SelectMany(x => x.docsSetting,
                                                    (a, b) => new
                                                    {
                                                        key = a.key,
                                                        keyDetail = a.keyDetail,
                                                        keyDocType = b?.keyDocType,
                                                        UTJ = b?.UTJ,
                                                        totalSetUTJ = b?.totalSetUTJ,
                                                        DP = b?.DP,
                                                        totalSetDP = b?.totalSetDP,
                                                        Lunas = b?.Lunas,
                                                        totalSetLunas = b?.totalSetLunas
                                                    });

            var dataJoin = listJnsDoc.GroupJoin(settingTemplate,
                                                doc => doc.key,
                                                fu => fu.keyDocType,
                                                (a, b) => new DocumentsProperty()
                                                {
                                                    keyDocType = a.key,
                                                    UTJ = b.Count() == 0 ? false : b.FirstOrDefault()?.UTJ ?? false,
                                                    totalSetUTJ = b.Count() == 0 ? 0 : b.FirstOrDefault()?.totalSetUTJ ?? 0,
                                                    DP = b.Count() == 0 ? false : b.FirstOrDefault()?.DP ?? false,
                                                    totalSetDP = b.Count() == 0 ? 0 : b.FirstOrDefault().totalSetDP ?? 0,
                                                    Lunas = b.Count() == 0 ? false : b.FirstOrDefault()?.Lunas ?? false,
                                                    totalSetLunas = b.Count() == 0 ? 0 : b.FirstOrDefault()?.totalSetLunas ?? 0,
                                                    //noteSet = new string[0]
                                                }).ToList();

            return dataJoin;
        }

        private void CreateDocSettings(user user, string keyDetail, JenisAlasHak? jenisAlasHak, LandOwnershipType? kepemilikan)
        {
            string template = "";

            template = contextpay.GetDocuments(new { Jenis = 0, Owner = 0, KeyTemplate = "" }, "static_collections",
                    "{'$match' : {_t : 'TemplateMandatoryDocSetting'}}",
                    "{'$unwind' : '$Details'}",
                    @"{'$project' : {
                        'Jenis' : '$Details.Jenis',
                        'Owner' : '$Details.Owner',
                        'KeyTemplate' : '$Details.keyTemplate',
                        _id : 0 }}"
                    ).ToList().FirstOrDefault(x => x.Jenis == (int)jenisAlasHak && x.Owner == (int)kepemilikan)?.KeyTemplate;

            List<DocumentsProperty> docSettings = GetDocSettingTemplate(template);
            var docSetting = contextpay.docSettings.FirstOrDefault(d => d.keyDetail == keyDetail);
            if (docSetting != null)
            {
                docSetting.docsSetting = docSettings.ToArray();
                docSetting.created = DateTime.Now;
                docSetting.keyCreator = user.key;
                docSetting.status = DocSettingStatus.notset;

                var docEntry = new DocSettingsEntries()
                {
                    keyCreator = docSetting.keyCreator,
                    created = docSetting.created,
                    docsSetting = docSettings.ToArray()
                };
                docSetting.AddEntry(docEntry);

                contextpay.docSettings.Update(docSetting);
            }
            else
            {
                DocSettings fuDoc = new DocSettings()
                {
                    key = MongoEntity.MakeKey,
                    keyDetail = keyDetail,
                    docsSetting = docSettings.ToArray(),
                    created = DateTime.Now,
                    keyCreator = user.key
                };
                var docEntry = new DocSettingsEntries()
                {
                    keyCreator = fuDoc.keyCreator,
                    created = fuDoc.created,
                    docsSetting = docSettings.ToArray()
                };
                fuDoc.AddEntry(docEntry);

                contextpay.docSettings.Insert(fuDoc);
            }
            contextpay.SaveChanges();
        }

        [HttpGet("notifikasi-email")]
        public IActionResult Notifications([FromQuery] string job)
        {
            try
            {
                var pradeals = contextplus.praDeals.Query(x => x.invalid != true).ToArray();
                var combos = pradeals.Where(x => x.closed == null)
                    .SelectMany(a => a.details.Select(d => (a, a.instancesKey, d, dtlkey: d.key))).ToArray();

                var combokeys = combos.Select(x => (x.instancesKey, x.dtlkey)).ToArray();
                var chains = ghost.GetSubs(combokeys).GetAwaiter().GetResult().Where(c => c.sub != null && !c.sub.closed).ToArray();
                var detailkeys = chains.Select(x => x.dockey).ToArray();

                var prakeys = pradeals.SelectMany(d => d.details)
                    .Where(x => detailkeys.Contains(x.key) && !string.IsNullOrEmpty(x.keyPersil))
                    .Select(x => new { keydtl = x.key, keypersil = x.keyPersil }).ToArray();

                var pkeys = string.Join(',', prakeys.Select(k => $"'{k.keypersil}'"));
                var dkeys = string.Join(',', prakeys.Select(k => $"'{k.keydtl}'"));

                var persils = contextpay.GetDocuments(new Persil(), "persils_v2",
                    $@"<$match : <key : <$in: [{pkeys}]>>>".MongoJs(),
                    "{$project: {_id: 0}}").ToArray();

                var bundles = contextplus.GetDocuments(new MainBundle(), "bundles",
                    $@"<$match : <key : <$in: [{pkeys}]>>>".MongoJs(),
                    "{$project: {_id: 0}}").ToList();

                var locations = contextpay.GetVillages();
                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).Select(x => new { key = x.key, identifier = x.identifier });

                var praDocLists = bundles.Select(x => new
                {
                    keyPersil = x.key,
                    keyDetail = prakeys.FirstOrDefault(y => y.keypersil == x.key).keydtl,
                    doclist = x.doclist.Select(z => new
                    {
                        keyDocType = z.keyDocType,
                        totalSet = (z?.entries == null || z.entries.Count() == 0) ? 0 : z.entries.LastOrDefault().Item.Count
                    }).ToArray()
                }).ToArray();

                var docSettings = contextplus.GetDocuments(new DocSettings(), "docSettings",
                $"<$match: <'keyDetail': <$in: [{dkeys}]>>>".MongoJs(),
                 "{$project: {_id: 0}}").ToList();

                var praDocRequireds = docSettings.Select(x => new
                {
                    keyDetail = x.keyDetail,
                    docReq = x.docsSetting.ToArray()
                }).ToArray();

                var persilExistsBayar = contextpay.GetDocuments(new PersilExistsBayar(), "bayars",
                    "{$unwind: '$details'}",
                    "{$match : {'details.invalid' : {$ne:true} }}",
                    "{$unwind: '$details.subdetails'}",
                    @"{$project: {
                                    'JenisBayar' : '$details.jenisBayar',
                                    'keyPersil' : '$details.subdetails.keyPersil',
                                    'TerbitGiro' : '$details.tglBayar',
                                    _id:0}}").ToArray();

                var listPraChecking = new List<PraChecking>();
                var dsList = new List<DataSExcelSheet>();


                foreach (var praDocList in praDocLists)
                {
                    if (persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.Lunas && x.TerbitGiro != null))
                        continue;

                    var status = string.Empty;
                    var praDocRequired = praDocRequireds.FirstOrDefault(x => x.keyDetail == praDocList.keyDetail);

                    if (praDocRequired == null)
                        continue;

                    var docList = praDocList.doclist.ToArray();

                    var persilx = persilExistsBayar.Where(x => x.keyPersil == praDocList.keyPersil);

                    // Check UTJ Begin
                    var docReqUTJ = praDocRequired.docReq.Where(x => x.UTJ == true).ToArray();
                    var checkUTJ = docReqUTJ.Join(docList, a => a.keyDocType, b => b.keyDocType, (a, b) => new Checking
                    {
                        doc = b.totalSet,
                        match = (a.totalSetUTJ <= b.totalSet),
                        keyDocType = a.keyDocType
                    }).ToArray();

                    if (!checkUTJ.Any(x => x.match == false))
                    {
                        var exists = persilExistsBayar.Where(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.UTJ);
                        if (exists.Any())
                            status = exists.FirstOrDefault().TerbitGiro == null ? "Proses UTJ" : "Sudah UTJ";
                        else if (persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.DP))
                            status = status;
                        else if (!persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.UTJ))
                            status = "Siap UTJ";
                    }
                    // Check UTJ End

                    // Check DP Begin
                    var docReqDP = praDocRequired.docReq.Where(x => x.DP == true).ToArray();
                    var checkDP = docReqDP.Join(docList, a => a.keyDocType, b => b.keyDocType, (a, b) => new Checking
                    {
                        doc = b.totalSet,
                        match = (a.totalSetUTJ <= b.totalSet),
                        keyDocType = a.keyDocType
                    }).ToArray();

                    if (!checkDP.Any(x => x.match == false))
                    {
                        var exists = persilExistsBayar.Where(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.DP);
                        if (exists.Any())
                            status += exists.FirstOrDefault().TerbitGiro == null ? string.IsNullOrEmpty(status) ? "Proses DP" : ", Proses DP" : string.IsNullOrEmpty(status) ? "Sudah DP" : ", Sudah DP";
                        else if (persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.Lunas))
                            status = status;
                        else if (!persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.DP))
                            status += string.IsNullOrEmpty(status) ? "Siap DP" : ", Siap DP";
                    }
                    // Check DP End

                    // Check Lunas Begin
                    var docReqLunas = praDocRequired.docReq.Where(x => x.Lunas == true).ToArray();
                    var checkLunas = docReqLunas.Join(docList, a => a.keyDocType, b => b.keyDocType, (a, b) => new Checking
                    {
                        doc = b.totalSet,
                        match = (a.totalSetUTJ <= b.totalSet),
                        keyDocType = a.keyDocType
                    }).ToArray();

                    if (!checkLunas.Any(x => x.match == false))
                    {
                        var exists = persilExistsBayar.Where(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.Lunas);
                        if (exists.Any())
                            status += exists.FirstOrDefault().TerbitGiro == null ? string.IsNullOrEmpty(status) ? "Proses DP" : ", Proses DP" : "";
                        if (!persilExistsBayar.Any(x => x.keyPersil == praDocList.keyPersil && x.JenisBayar == JenisBayar.Lunas))
                            status += string.IsNullOrEmpty(status) ? "Siap Pelunasan" : ", Siap Pelunasan";
                    }
                    // Check Lunas End


                    if (!checkUTJ.Any(x => x.match == false) && !checkDP.Any(x => x.match == false) && !checkLunas.Any(x => x.match == false))
                        listPraChecking.Add(new PraChecking
                        {
                            keyDetail = praDocList.keyDetail,
                            keyPersil = praDocList.keyPersil,
                            status = status
                        });
                }

                var xdata = listPraChecking.Join(persils, e => e.keyPersil, p => p.key, (e, p) => new PersilEmail
                {
                    Project = locations.FirstOrDefault(x => x.project.key == p.basic?.current?.keyProject).project?.identity,
                    Desa = locations.FirstOrDefault(x => x.desa.key == p.basic?.current?.keyDesa).desa?.identity,
                    PTSK = ptsks.FirstOrDefault(x => x.key == p.basic?.current?.keyPTSK)?.identifier,
                    IdBidang = p.IdBidang,
                    AlasHak = p.basic?.current?.surat?.nomor,
                    Group = p.basic?.current?.group,
                    Pemilik = p.basic?.current?.pemilik ?? p.basic?.current?.surat?.nama,
                    Luas = p.basic?.current?.luasSurat ?? 0,
                    Status = e.status
                }).ToList();

                if (job == "grid")
                {
                    return new JsonResult(xdata.ToArray());
                }
                else
                {
                    var ndata = xdata.ToDataSet();
                    dsList.Add(new DataSExcelSheet { dataS = ndata, sheetName = "Bidangs" });
                    var files = DataSetHelper.ExportMultipleDataSetToExcelByte(dsList.ToArray());
                    string filename = "Bidang Siap Untuk Proses Pembayaran_" + DateTime.Now.ToShortDateString().Replace("/", "-") + ".xlsx";

                    if (job == "file")
                    {

                        return File(files, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                    }
                    else if (job == "email")
                    {
                        var alertSettings = contextplus.GetDocuments(new ReminderSiapPembayaran(), "static_collections",
                            "{$match: {'_t': 'ReminderSiapPembayaran'}}",
                            "{$project: {_id:0}}").FirstOrDefault();
                        
                        string[] receivers = alertSettings.receivers.Select(x => x?.email ?? "").ToArray();
                        string[] receiversCC = alertSettings.receiversCC.Select(x => x?.email ?? "").ToArray();
                        string[] receiversBCC = alertSettings.receiversBCC.Select(x => x?.email ?? "").ToArray();

                        var base64File = Convert.ToBase64String(files);
                        EmailStructure email = new EmailStructure
                        {
                            Receiver = receivers,
                            CC = receiversCC,
                            BCC = receiversBCC,
                            Subject = "[Pembayaran Alert: Bidang Siap Dilakukan Pembayaran]",
                            Body = @"<h4>Dear All,</h4>
                                    <p>Berikut adalah bidang-bidang yang secara dokumen sudah siap untuk dilakukan Pembayaran.</p>
                                    <p>&nbsp; <br></p>
                                    <p align='left'>Regards,</p>
                                    <p align='left'>Alert Notification Bidang Siap Dilakukan Pembayaran</p>
                                    ",
                            ContentType = FileType.Excel,
                            Attachment = new List<EmailAttachment> { new EmailAttachment { FileBase64 = base64File, FileName = filename } }
                        };

                        string url;
                        var appsets = Config.AppSettings;
                        if (!appsets.TryGet("sendEmail:url", out url))
                            return new InternalErrorResult("Invalid url. Cek kembali file pengaturan anda");

                        var myContent = JsonConvert.SerializeObject(email);
                        var content = new StringContent(myContent, Encoding.UTF8, "application/json");
                        content.Headers.ContentType.CharSet = "";

                        HttpClient client = new HttpClient();
                        var response = client.PostAsync(url + "api/mailsender/sendemail", content).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                            return Ok();
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }

                return Ok();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public class EmailNotifikasi
        {
            public PraChecking[] UTJ { get; set; } = new PraChecking[0];
            public PraChecking[] DP { get; set; } = new PraChecking[0];
            public PraChecking[] Lunas { get; set; } = new PraChecking[0];
        }
        public class PraChecking
        {
            public string keyDetail { get; set; }
            public string keyPersil { get; set; }
            public string status { get; set; }
        }
        public class Checking
        {
            public int doc { get; set; }
            public bool match { get; set; }
            public string keyDocType { get; set; }
        }
        public class PersilExistsBayar
        {
            public JenisBayar JenisBayar { get; set; }
            public string keyPersil { get; set; }
            public DateTime? TerbitGiro { get; set; }
        }
        public class PersilEmail
        {
            public string Project { get; set; }
            public string Desa { get; set; }
            public string PTSK { get; set; }
            public string IdBidang { get; set; }
            public string AlasHak { get; set; }
            public string Group { get; set; }
            public string Pemilik { get; set; }
            public double Luas { get; set; }
            public string Status { get; set; }
        }

        private (bool, string) CheckKelengkapanDokumen(string process, string keyPersil, string keyDetail)
        {
            (bool ok, string message) = (true, null);

            //Edit by : Ginanjar Sumantri 24-11-2022
            //Cut off untuk kapan akan dicek kelengkapan dokumen di docsetting 
            var cutoff = contextpay.GetDocuments(new { value = DateTime.Now }, "static_collections",
                "{$match: {_t : 'cutoff', 'details.modul' : 'PraPembebasan', 'details.method' : 'CheckKelengkapanDokumen'}}",
                "{$unwind: '$details'}",
                "{$project: {value : '$details.value', _id:0}}").Select(x => x.value).FirstOrDefault();

            var docStatus = contextpay.docSettings.Query(x => x.keyDetail == keyDetail).FirstOrDefault();

            if (docStatus.created.Date < cutoff.Date)
                return (true, null);

            if (docStatus.status != DocSettingStatus.ready)
                return (false, "Document Status Not Set Ready");

            var bundle = contextplus.GetDocuments(new MainBundle(), "bundles",
                "{$match: {'key': '" + keyPersil + "'}}",
                "{$project: {_id: 0}}").FirstOrDefault();
            var doclistEntries = bundle.doclist.Select(x =>
            new
            {
                keyDocType = x.keyDocType,
                totalSet = (x?.entries == null || x.entries.Count() == 0) ? 0 : x.entries.LastOrDefault().Item.Count
            });

            var docSettings = contextplus.GetDocuments(new DocSettings(), "docSettings",
                "{$match: {'keyDetail': '" + keyDetail + "'}}").FirstOrDefault();

            var docReq = process == "DP" ? docSettings.docsSetting.Where(x => x.DP == true).ToArray() :
                         process == "LUNAS" ? docSettings.docsSetting.Where(x => x.Lunas == true).ToArray() :
                                                docSettings.docsSetting.Where(x => x.UTJ == true).ToArray();

            var jnsDoks = contextplus.GetCollections(new { key = "", identifier = "" }, "jnsDok", "{}", "{key : 1, identifier : 1, _id: 0}").ToList();

            var resultChecking = docReq.Join(doclistEntries, a => a.keyDocType,
                                                   b => b.keyDocType,
                                                   (a, b) => new
                                                   {
                                                       req = process == "DP" ? a.totalSetDP : process == "LUNAS" ? a.totalSetLunas : a.totalSetUTJ,
                                                       doc = b.totalSet,
                                                       match = process == "DP" ? (a.totalSetDP <= b.totalSet) : process == "LUNAS" ? (a.totalSetLunas <= b.totalSet) : (a.totalSetUTJ <= b.totalSet),
                                                       keyDocType = a.keyDocType,
                                                       docName = jnsDoks.FirstOrDefault(x => x.key == a.keyDocType)?.identifier
                                                   });
            if (resultChecking.Any(x => x.match == false))
            {
                foreach (var item in resultChecking.Where(x => x.match == false))
                {
                    message += $"Dokumen {item.docName} Total Set ({item.doc}) belum sesuai dengan Document Setting ({item.req}) untuk pengajuan {process} \n";
                }
                return (false, message);
            }
            return (ok, message);
        }

        private (bool, string) BidangChecking(string keyDetail, string keyPersil, string optional)
        {
            (bool ok, string message) = (true, null);
            (ok, message) = CheckKelengkapanDokumen(optional, keyPersil, keyDetail);
            if (!ok)
                return (ok, message);

            (ok, message) = ValidasiBidang(keyPersil);
            if (!ok)
                return (ok, message);

            return (ok, message);
        }

        /// <summary>
        /// Perbaikan data notaris double
        /// </summary>
        [HttpPost("fix-data-notaris")]
        public IActionResult FixDataNotaris() 
        {
            try
            {
                var static_Col = contextpay.GetDocuments(new { KeyLama = "", KeyBaru = "", Identifier = "" }, "static_collections",
                    "{'$match' : {_t : 'PerbaikanDataNotaris'}}",
                    "{'$unwind' : '$details'}",
                    @"{'$project' : {
                        'KeyLama' : '$details.keyLama',
                        'KeyBaru' : '$details.keyBaru',
                        'Identifier' : '$details.identifier',
                        _id : 0 }}"
                    ).ToArray();

                var listKey = static_Col.Select(a => a.KeyLama).Distinct().ToList();
                var praDeals = contextplus.praDeals.Query(p => p.details.Any(d => listKey.Contains(d.notaris))).ToList();

                if (praDeals?.Count > 0)
                {
                    praDeals.Where(p => listKey.Contains(p.notaris)).ToList().ForEach(u =>
                    {
                        u.notaris = static_Col.ToList().Find(s => u.notaris == s.KeyLama).KeyBaru;
                    });

                    Parallel.ForEach(praDeals, i =>
                    {
                        i.details.Where(a => static_Col.Any(s => a.notaris == s.KeyLama)).ToList().ForEach(p =>
                        {
                            p.notaris = static_Col.ToList().Find(s => p.notaris == s.KeyLama).KeyBaru;

                        });
                    });

                    contextplus.praDeals.Update(praDeals);
                    contextplus.SaveChanges();
                }
                
                return Ok();
            } 
            catch (Exception ex) 
            {
                throw ex;
            }
        }

    }
}

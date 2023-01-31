#define test
using APIGrid;
using graph.mod;
using GraphConsumer;
using landrope.consumers;
using landrope.hosts;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;
using GenWorkflow;
using flow.common;
using System.Text;
using System.Text.Json;
using auth.mod;
using landrope.mod;
using MongoDB.Bson;
using landrope.mod.shared;
using System.Reflection;
using landrope.material;
using MongoDB.Driver;
using landrope.api3.Models;
using Shape = landrope.mod4.Shape;
using auth.mod.ext;
using mongospace;
using System.Text.RegularExpressions;

namespace landrope.api3.Controllers
{
    [Route("api/request")]
    [ApiController]
    public class UpdateRequestController : Controller
    {
        IServiceProvider services;
        LandropePlusContext contextPlus;
        LandropePayContext contextPay;
        PersilApprovalHost HostReqPersil;

#if test
        GraphHostConsumer ghost;
#else
        GraphHost.GraphHostSvc graphhost;
#endif
        public UpdateRequestController(IServiceProvider services)
        {
            this.services = services;
            contextPlus = services.GetService<mod3.LandropePlusContext>();
            contextPay = services.GetService<LandropePayContext>();

#if test
            //ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ghost = HostServicesHelper.GetGraphHostConsumer(services);
#else
            graphhost = HostServicesHelper.GetGraphHost(services);
#endif
            HostReqPersil = HostServicesHelper.GetBidangHost(this.services);
        }


        #region MAP REQUEST
        /// <summary>
        /// Create New Approval Request
        /// </summary>
        [HttpPost("map/save")]
        public IActionResult CreateMapApprovalReq([FromQuery] string token, [FromQuery] string opr, [FromBody] MapApprovalReqCore core)
        {
            var predicate = opr switch
            {
                "edit" => "memperbarui",
                "del" => "menghapus",
                _ => "menambah"
            } + "Request Approval Map";

            try
            {
                var user = contextPay.FindUser(token);
                var res = opr switch
                {
                    "edit" => Edit(user),
                    "del" => Del(user),
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
                contextPay.DiscardChanges();
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }

            IActionResult Add(user user)
            {
                if (core.idBidang == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk melengkapi data request !");

                MyTracer.TraceInfo2($"Param Core : {System.Text.Json.JsonSerializer.Serialize(core)}");
                MyTracer.TraceInfo2($"FindExistingRequest");
                MapRequest existingReq = RequestByIdBidang(core.idBidang);
                MyTracer.TraceInfo2($"Done FindExistingRequest");

                MyTracer.TraceInfo2($"Create Instance MapRequest");
                (bool ok, string message) = (false, null);
                MapRequest request = new MapRequest();
                MyTracer.TraceInfo2($"Done Create Instance MapRequest");

                MyTracer.TraceInfo2($"Get PersilByIdBidang");
                Persil persil = GetPersilByIdBidang(core.idBidang);
                MyTracer.TraceInfo2($"Done Get PersilByIdBidang");
                if (persil == null)
                {
                    MyTracer.TraceError3("Bidang yang anda maksud tidak tersedia !");
                    return new UnprocessableEntityObjectResult("Bidang yang anda maksud tidak tersedia !");
                }


                if (existingReq.key != null)
                {
                    request = existingReq;
                    // Insert to PersilMap Collection
                    MyTracer.TraceInfo2($"SavePersilMap {user.identifier}, {persil.key}, {core.location}");
                    (ok, message) = SavePersilMap(user, persil.key, core.location);
                    if (!ok)
                        return new UnprocessableEntityObjectResult($"Error Add Maps : {message}. harap hubungi support !");
                    MyTracer.TraceInfo2($"SavePersilMap : {ok}, {message}");
                }
                else
                {
                    // Insert to Request Collection
                    MyTracer.TraceInfo2($"Create Request Baru {user.identifier}, {persil.key}, {core.location}");
                    MapRequest requestNew = new MapRequest(core, user, persil.key);
                    request = requestNew;

                    MyTracer.TraceInfo2("Insert Request");
                    contextPay.mapRequest.Insert(request);

                    MyTracer.TraceInfo2("Save Request");
                    contextPay.SaveChanges();

                    // Insert to PersilMap Collection
                    MyTracer.TraceInfo2("Get Persil By Id Bidang");

                    MyTracer.TraceInfo2("Save Persil Map");
                    (ok, message) = SavePersilMap(user, persil.key, core.location);
                    if (!ok)
                        return new UnprocessableEntityObjectResult($"Error Add Maps : {message}. harap hubungi support !");
                }

                MyTracer.TraceInfo2("Get LastState");
                var lastStateRun = request.instance?.lastState;
                MyTracer.TraceInfo2("create Log WL");
                LogWorkListCore logWLCore = new LogWorkListCore(request.instKey, user.key, (existingReq.key != null ? DateTime.Now : lastStateRun.time),
                                                 lastStateRun?.state ?? ToDoState.created_, ToDoVerb.create_, core.note);
                MyTracer.TraceInfo2("Insert Log WL");
                (ok, message) = InsertLogWorklist(logWLCore);
                MyTracer.TraceInfo2("Done Insert Log WL");
                if (!ok)
                {
                    MyTracer.TraceError3($"Error Add Log Worklist : {message}. harap hubungi support !");
                    return new UnprocessableEntityObjectResult($"Error Add Log Worklist : {message}. harap hubungi support !");
                }

                return Ok();
            }

            IActionResult Edit(user user)
            {
                if (core.reqKey == null || core.idBidang == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk melengkapi data request !");

                MapRequest existingReq = RequestByKey(core.reqKey);

                if (existingReq == null)
                    return new UnprocessableEntityObjectResult("Request tidak ditemukan !");

                Persil persil = GetPersilByIdBidang(core.idBidang);

                if (persil.key != existingReq.info.keyPersil)
                    return new UnprocessableEntityObjectResult("Bidang yang akan diedit harus sama !");

                var LastStatus = existingReq.instance.lastState?.state;

                (bool ok, string message) = SavePersilMap(user, persil.key, core.location);
                if (!ok)
                    return new UnprocessableEntityObjectResult($"Error Edit Maps : {message}. harap hubungi support !");

                //Run The Flow
                var cmd = new UpdateRequestCommand()
                {
                    routeKey = core.routeKey,
                    verb = core.verb,
                    control = core.control,
                    reason = core.reason,
                    reqKey = core.reqKey
                };

                RuningFlow(user, cmd, existingReq.instance);

                return Ok();
            }

            IActionResult Del(user user)
            {
                MapRequest existingReq = contextPay.mapRequest.FirstOrDefault(m => m.key == core.reqKey);
                var LastStatus = existingReq.instance.lastState?.state;

                if (LastStatus != ToDoState.created_)
                    existingReq.invalid = false;
                contextPay.mapRequest.Update(existingReq);
                contextPay.SaveChanges();

                return Ok();
            }
        }

        /// <summary>
        /// Create Request Multiple Bidang
        /// </summary>
        [HttpPost("map/multiple/save")]
        public IActionResult CreateMultipleMapApprovalReq([FromQuery] string token, [FromBody] MultipleMapApprovalReq core)
        {
            try
            {
                var user = contextPay.FindUser(token);
                if (core.request.Any(r => r.idBidang == null))
                    return new UnprocessableEntityObjectResult("Mohon untuk melengkapi data request !");

                MyTracer.TraceInfo2($"Param Core : {System.Text.Json.JsonSerializer.Serialize(core)}");

                MyTracer.TraceInfo2($"Create Instance MapRequest");
                (bool ok, string message) = (false, null);
                MapRequest request = new MapRequest();
                MyTracer.TraceInfo2($"Done Create Instance MapRequest");

                MyTracer.TraceInfo2($"Get PersilByIdBidang");
                List<Persil> listPersil = GetPersilsByBidangs(core.request.Select(r => r.idBidang).ToList());
                MyTracer.TraceInfo2($"Done Get PersilByIdBidang");

                if (listPersil.Count() != core.request.Count())
                {
                    MyTracer.TraceError3($"Salah satu bidang yang anda pilih tidak tersedia !");
                    return new UnprocessableEntityObjectResult("Bidang yang anda maksud tidak tersedia !");
                }

                // Insert to Request Collection
                MyTracer.TraceInfo2($"Create Request Baru {user.identifier}, {System.Text.Json.JsonSerializer.Serialize(core)}");
                MapRequest requestNew = new MapRequest(user, listPersil.Select(lP => lP.key).ToArray());
                request = requestNew;

                MyTracer.TraceInfo2("Insert Request");
                contextPay.mapRequest.Insert(request);

                MyTracer.TraceInfo2("Save Request");
                contextPay.SaveChanges();

                // Insert to PersilMap Collection
                foreach (var item in core.request)
                {
                    MyTracer.TraceInfo2("Save Persil Map");
                    (ok, message) = SavePersilMap(user, listPersil.FirstOrDefault(x => x.IdBidang == item.idBidang)?.key, item.location);
                    if (!ok)
                        return new UnprocessableEntityObjectResult($"Error Add Maps for Bidang {item.idBidang} : {message}. harap hubungi support !");
                }

                MyTracer.TraceInfo2("Get Last State");
                var lastStateRun = request.instance?.lastState;

                MyTracer.TraceInfo2("create Log WL");
                LogWorkListCore logWLCore = new LogWorkListCore(request.instKey, user.key, lastStateRun?.time ?? DateTime.Now,
                                                 lastStateRun?.state ?? ToDoState.created_, ToDoVerb.create_, core.command.reason);
                MyTracer.TraceInfo2("Insert Log WL");
                (ok, message) = InsertLogWorklist(logWLCore);
                MyTracer.TraceInfo2("Done Insert Log WL");
                if (!ok)
                {
                    MyTracer.TraceError3($"Error Add Log Worklist : {message}. harap hubungi support !");
                    return new UnprocessableEntityObjectResult($"Error Add Log Worklist : {message}. harap hubungi support !");
                }

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                contextPay.DiscardChanges();
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Get Request Map detail
        /// </summary>
        [HttpGet("map/detail")]
        public IActionResult GetDetailMap([FromQuery] string token, [FromQuery] string key)
        {
            try
            {
                var user = contextPay.FindUser(token);
                MapRequest apprReq = contextPay.mapRequest.FirstOrDefault(m => m.key == key);
                if (apprReq == null)
                    return new UnprocessableEntityObjectResult("Request tidak tersedia !");

                List<Persil> listPersil = GetPersilByKeys(apprReq.info.keyPersils);
                Persil persil = GetPersilBykey(apprReq.info.keyPersil);
                if (persil != null)
                    listPersil.Add(persil);

                string keyPersils = string.Join(",", listPersil.Select(p => $"'{p.key}'"));

                List<DetailMap> listDetailMap = contextPay.GetDocuments(new DetailMap(), "persilMaps",
                                 "{$addFields: {entry:{$arrayElemAt: ['$entries',-1]}}}",
                                 "{$addFields: {careas: {$cond : [ {$ne : ['$entry.map.careas', BinData(0,'')]}, {$ifNull: ['$entry.map.careas', null]}, null ]}}}",
                                 "{$match : {'key' : {$in : [" + keyPersils + "] }} }",
                                @"{$project : {
                                                _id : 0,
                                                key : '$key',
                                             careas : '$careas'
                                   }}");

                var details = listDetailMap.Join(listPersil, map => map.key,
                                                         persil => persil.key,
                                                         (map, persil) => new MapApprovalDetail()
                                                         {
                                                             keyRequest = apprReq.key,
                                                             keyPersil = persil.key,
                                                             keyDesa = persil.basic.current != null ? persil.basic.current.keyDesa :
                                                                       persil.basic.entries != null ? persil.basic.entries.LastOrDefault().item?.keyDesa : "",
                                                             IdBidang = persil?.IdBidang ?? "",
                                                             map = map.careas != null ? JsonSerializer.Deserialize<List<Shape>>(Encoding.ASCII.GetString(map.careas).Replace("'", "\"")).ToArray() : new Shape[0]
                                                         });

                return Ok(details.ToArray());
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
        /// Get Locations
        /// </summary>
        /// <returns></returns>
        [HttpGet("map/locations")]
        public IActionResult GetLocations([FromQuery] string token)
        {
            try
            {
                user user = contextPay.FindUser(token);
                var userProjects = UserExtension.GetProjects(user);
                string allowedKeyProjects = string.Join(",", userProjects);
                allowedKeyProjects = string.Join(",", allowedKeyProjects.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                var projects = contextPay.GetDocuments(new LocationView(), "maps",
                    "{$match : {key : {$in : [" + allowedKeyProjects + "]}}}",
                    @"{$project:{
                          _id : 0,
                          ID : '$key',
                          ParentID : null,
                          Name : '$identity'
                    }}");

                var villages = contextPay.GetDocuments(new LocationView(), "maps",
                    "{$match : {key : {$in : [" + allowedKeyProjects + "]}}}",
                    "{$unwind: '$villages'}",
                    @"{$project:{
                          _id : 0,
                          ID : '$villages.key',
                          ParentID : '$key',
                          Name : '$villages.identity'
                    }}");


                List<LocationView> result = projects.Union(villages).ToList();

                return Ok(result);
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
        /// Get Map info by key Desa
        /// </summary>
        [HttpGet("map/info/by-desa")]
        public IActionResult GetMapObjectByKeyDesa([FromQuery] string token, [FromQuery] string keyDesa)
        {
            try
            {
                user user = contextPay.FindUser(token);
                var objectMap = contextPay.GetDocuments(new ObjectByKeyDesa(), "persils_v2",
                "{$match: {$and : [{$and:[ { 'basic.current.keyDesa' : '" + keyDesa + "'}, { 'en_state' : {$ne : 2 }}]}, {'invalid' : {$ne: true}} ]}}",
                "{$lookup: { from: 'persilMaps', localField: 'key', foreignField: 'key' , as:'pm' }}",
                    "{$unwind: { path: '$pm', preserveNullAndEmptyArrays: true}}",
                    "{$addFields: { pm: '$pm'}}",
                    "{$addFields: {entry:{$arrayElemAt: ['$pm.entries',-1]}}}",
                    "{$addFields: {careas: {$cond : [ {$ne : ['$entry.map.careas', BinData(0,'')]}, {$ifNull: ['$entry.map.careas', null]}, null ]}}}",
                    @"{$project:{  
                                 _id: 0,
                                 idBidang: '$IdBidang',
                                 en_state: {$ifNull : ['$en_state', 0]},
                                 en_proses : {$ifNull : ['$basic.current.en_proses', 0]},     
                                 created : '$created',
                                 luasSurat : {$ifNull: ['$basic.current.luasSurat', 0]},
                                 luasDibayar : {$ifNull : ['$basic.current.luasDibayar',0]},
                                 nomorSurat : '$basic.current.surat.nomor',
                                 namaSurat : '$basic.current.surat.nama',
                                 careas : '$careas'
                    }} ").ToList();

                var result = objectMap.Select(oM => new ObjectByKeyDesaview()
                {
                    idBidang = oM.idBidang,
                    bebas = oM.en_state != 0 ? false : true,
                    kind = oM.en_proses?.UpdateMapProsesDesc(),
                    created = Convert.ToDateTime(oM.created).ToLocalTime(),
                    luasSurat = oM.luasSurat,
                    luasDibayar = oM.luasDibayar,
                    surat = new ObjectByDesaSurat(oM.nomorSurat, oM.namaSurat),
                    map = oM.careas != null ? JsonSerializer.Deserialize<List<ShapeCore>>
                            (Encoding.ASCII.GetString(oM.careas).Replace("'", "\"")).ToArray() :
                            new ShapeCore[0]
                });

                return Ok(result);
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
        /// Get Map Location By Id Bidang
        /// </summary>
        [HttpGet("map/by-bidang")]
        public IActionResult GetMapByIdBidang([FromQuery] string token, [FromQuery] string idBidang)
        {
            try
            {
                var user = contextPay.FindUser(token);
                Persil persil = GetPersilByIdBidang(idBidang);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ditemukan !");

                DetailMap detailMap = contextPay.GetDocuments(new DetailMap(), "persilMaps",
                                 "{$addFields: {entry:{$arrayElemAt: ['$entries',-1]}}}",
                                 "{$addFields: {careas1: {$cond:[ {$ne:['$current.careas', BinData(0, '')]}, {$ifNull:['$current.careas', null]}, null ]}}}",
                                $"<$match: <'key': '{persil.key}'>>".Replace("<", "{").Replace(">", "}"),
                                 "{$addFields: {careas1: {$cond:[ {$ne:['$current.careas', BinData(0, '')]}, {$ifNull:['$current.careas', null]}, null ]}}}",
                                @"{$project : {
                                                _id : 0,
                                                key : '$key',
                                             careas : '$careas1'
                                  }}"
                                ).FirstOrDefault();
                var result = detailMap.ToMap();

                return Ok(result);
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
        #endregion

        #region SUMMARY
        /// <summary>
        /// Get History Worklist by key request
        /// </summary>
        /// 
        [HttpGet("history/worklist")]
        public IActionResult GetHistoryState([FromQuery] string token, [FromQuery] string keyRequest)
        {
            try
            {
                var user = contextPay.FindUser(token);
                return new JsonResult(GetLogStates(keyRequest));
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

        [HttpGet("history/worklist2")]
        public IActionResult GetHistoryStateWithPaginate([FromQuery] string token, [FromQuery] string keyRequest, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var logStates = GetLogStates(keyRequest);
                var xlst = ExpressionFilter.Evaluate(logStates, typeof(List<StateHistoryView>), typeof(StateHistoryView), gs);
                var data = xlst.result.Cast<StateHistoryView>().ToList();
                var rs = data.GridFeed(gs);

                return Ok(rs);

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

        [HttpGet("remark/worklist")]
        public IActionResult GetRemark([FromQuery] string token, [FromQuery] string keyRequest)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var request = contextPay.GetCollections(new { remark = "" }, "update_request_data", $"<key : '{keyRequest}'>".MongoJs(), "{_id:0}").ToList().FirstOrDefault();

                return new JsonResult(request.remark);

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
        /// Get Kulit Desa
        /// </summary>
        [HttpGet("map/kulit/desa")]
        public IActionResult GetKulitDesa([FromQuery] string token, [FromQuery] string keyDesa)
        {
            try
            {
                var user = contextPay.FindUser(token);

                DetailMap detailMap = new DetailMap();
                var userProjects = UserExtension.GetProjects(user);
                string allowedKeyProjects = string.Join(",", userProjects.Select(uP => $"'{uP}'"));

                var detailMaps = contextPay.GetDocuments(new DetailMap(), "maps",
                "{$match : {key : {$in : [" + allowedKeyProjects + "]}}}",
                "{$unwind : '$villages'}",
                $"<$match: <'villages.key': '{keyDesa}'>>".Replace("<", "{").Replace(">", "}"),
                "{$addFields: {careas: {$cond : [ {$ne : ['$villages.careas', BinData(0,'')]}, {$ifNull: ['$villages.careas', null]}, null ]}}}",
                @"{$project : {
                                _id : 0,
                                careas : '$careas'
                     }}");

                detailMap = detailMaps.Count != 0 ? detailMaps.FirstOrDefault() : detailMap;

                var result = detailMap.ToMap();

                return Ok(result);
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
        #endregion

        #region REQUEST STATE
        [NeedToken("CREATE_EDIT_SB,CREATE_EDIT_BB")]
        [HttpPost("state/list")]
        public IActionResult GetListStateRequest(string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextPay.FindUser(token);
                var host = HostServicesHelper.GetStateRequestHost(services);
                var query = host.OpenedStateRequest().Cast<StateRequest>();

                var locations = contextPay.GetVillages().ToArray();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var keycreators = query.Select(x => x.creator).Distinct();
                var creators = string.Join(',', keycreators.Select(k => $"'{k}'"));

                var users = contextPay.GetCollections(new user(), "securities", $"<key: <$in:[{creators}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var view = query.Select(x => x.ToView(contextPay)).ToArray();

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<UpdStateRequestView>), typeof(UpdStateRequestView), gs);
                var data = xlst.result.Cast<UpdStateRequestView>().ToArray();

                return Ok(data.GridFeed(gs));
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

        /// <summary>
        /// Get List Detail State Request
        /// </summary>
        /// 
        [NeedToken("CREATE_EDIT_SB,CREATE_EDIT_BB")]
        [HttpGet("state/view/detail")]
        public IActionResult GetListStateDetail([FromQuery] string token, string key)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextPay.FindUser(token);
                var host = HostServicesHelper.GetStateRequestHost(services);
                var query = host.GetStateRequest(key) as StateRequest;

                if (query == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");
                if (query.info.details == null)
                    query.info.details = new StateRequestDetail[0];

                var villages = contextPay.GetVillages().ToArray();

                var view = query.info.details.Select(x => x.ToView(contextPay, villages)).ToArray();

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

        /// <summary>
        /// Save Header State Request
        /// </summary>
        [NeedToken("CREATE_EDIT_SB,CREATE_EDIT_BB")]
        [HttpPost("state/save")]
        public IActionResult SaveStateRequest([FromQuery] string token, [FromBody] StateApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                (bool ok, string message) = CheckExistsRequest(core.keyPersils.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var sudahBebas = priv.Contains("CREATE_EDIT_SB");
                var belumBebas = priv.Contains("CREATE_EDIT_BB");
                //var discriminator = sudahBebas == true ? "EDIT_SB" : belumBebas == true ? "EDIT_BB" : null;

                var host = HostServicesHelper.GetStateRequestHost(services);

                var ent = new StateRequest(user, core);

                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "StateApproval");

                ent.identifier = dno.Generate(DateTime.Now, true, string.Empty);

                host.Add(ent);

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

        /// <summary>
        /// Save Detail State Request
        /// </summary>
        [NeedToken("CREATE_EDIT_SB,CREATE_EDIT_BB")]
        [HttpPost("state/edit/detail")]
        public IActionResult AddBidang([FromQuery] string token, string key, [FromBody] StateRequestCommad cmd)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var host = HostServicesHelper.GetStateRequestHost(services);
                var state = host.GetStateRequest(key) as StateRequest;

                if (state == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                var list = new List<StateRequestDetail>();
                foreach (var k in cmd.keyPersils)
                {
                    //if (state.info.details != null)
                    //{
                    //    if (state.info.details.Any(d => d.keyPersil == k))
                    //        continue;
                    //}

                    var detail = new StateRequestDetail() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };
                    list.Add(detail);
                }

                state.info.EditDetail(list.ToArray());
                host.Update(state);

                RuningFlow(user, cmd, state.instance);

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

        /// <summary>
        /// Get List Persil for State Request 
        /// </summary>
        [NeedToken("CREATE_EDIT_SB,CREATE_EDIT_BB")]
        [HttpGet("state/persils/list")]
        public IActionResult GetListPersilForCategory([FromQuery] string token, RequestState opr, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var tm0 = DateTime.Now;

                bool isBB = user.privileges.Any(x => x.identifier == "CREATE_EDIT_BB");
                bool isSB = user.privileges.Any(x => x.identifier == "CREATE_EDIT_SB");
                bool isBBSB = isBB & isSB;

                var keys = opr switch
                {
                    RequestState.Masuk => new object[] { 5 },
                    RequestState.Batal => isBBSB ? new object[] { 0, 1, 5, null } : isBB ? new object[] { 1, 5 } : isSB ? new object[] { 0, 5, null } : new object[] { 99 },
                    RequestState.Lanjut => new object[] { 2 },
                    RequestState.Keluar => isBBSB ? new object[] { 0, 1, null } : isBB ? new object[] { 1 } : isSB ? new object[] { 0, null } : new object[] { 99 },
                    _ => new object[] { 0, 1, 2, 3, 4, 5, null }
                };

                var prokeys = string.Join(',', keys.Select(k => $"{(k == null ? "null" : k)}"));

                var StillOn = contextPay.GetDocuments(new { keyPersil = "", lastState = 0 }, "update_request_data",
                    "{$match: {_t:'StateRequest', 'info.details': {$ne : []}}}",
                    @"{$lookup: {
                           from: 'graphables',
                           localField: 'instKey',
                           foreignField: 'key',
                           as: 'graph'
                         }}",
                    "{$unwind : '$info.details'}",
                    "{$addFields : {keyPersil : '$info.details.keyPersil'}}",
                    @" {$project : {
                            _id : 0,
                            keyPersil : 1,
                            lastState : {$arrayElemAt : ['$graph.lastState.state', 0]}
                        }}",
                    "{$match: {lastState : {$ne:20}}}").ToList();

                var persilkeys = string.Join(',', StillOn.Select(k => $"'{k.keyPersil}'"));

                var persils = contextPay.GetDocuments(new PersilCore4(), "persils_v2",
                    $@"<$match:<$and: [<en_state:<$in:[{prokeys}]>>,<key:<$nin:[{persilkeys}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps',let:< key: '$basic.current.keyDesa'>,pipeline:[<$unwind: '$villages'>,<$match: <$expr: <$eq:['$villages.key','$$key']>>>,
                    <$project: <key: '$villages.key', identity: '$villages.identity'>>], as:'desas'>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps', localField:'basic.current.keyProject',foreignField:'key',as:'projects'>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$project:<key:'$key',IdBidang: '$IdBidang',en_state: '$en_state',
                    alasHak : '$basic.current.surat.nomor',
                    noPeta : '$basic.current.noPeta',
                    desa : <$arrayElemAt:['$desas.identity', -1]>,
                    project: <$arrayElemAt:['$projects.identity',-1]>,
                    luasSurat : '$basic.current.luasSurat',
                    group: '$basic.current.group',
                    pemilik: '$basic.current.pemilik',
                    statehistories: '$statehistories',
                    _id: 0>>".Replace("<", "{").Replace(">", "}")).ToList();

                if (opr == RequestState.Masuk || opr == RequestState.Batal)
                {
                    StatusBidang[] states = isBBSB ? new StatusBidang[] { StatusBidang.bebas, StatusBidang.belumbebas } : isBB ? new StatusBidang[] { StatusBidang.belumbebas } : new StatusBidang[] { StatusBidang.bebas };
                    StatusBidang[] statesQry = { StatusBidang.bebas, StatusBidang.belumbebas };

                    var persilOut = persils.Where(x => x.en_state == StatusBidang.keluar)
                        .Select(x => new
                        {
                            lastState = x.statehistories.Count() == 0 ? StatusBidang.belumbebas : x.statehistories.Where(x => statesQry.Contains(x.en_state))?
                                                                                                    .OrderByDescending(x => x.date).FirstOrDefault()?.en_state,
                            persil = x
                        }).Where(x => !states.Contains((StatusBidang)x.lastState)).ToList();


                    persils.RemoveAll(x => persilOut.Select(x => x.persil.key).Contains(x.key));
                }

                var xlst = ExpressionFilter.Evaluate(persils, typeof(List<PersilCore4>), typeof(PersilCore4), gs);
                var data = xlst.result.Cast<PersilCore4>().ToArray();

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

        #endregion

        #region Persil Approval

        /// <summary>
        /// Create Request to Add New Bidang 
        /// </summary>
        [HttpPost("persil/save")]
        public IActionResult CreatePersilRequest([FromQuery] string token, [FromQuery] string opr, [FromBody] ReqPersilApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var privs = user.privileges.Select(p => p.identifier).ToArray();
                bool restricted = !privs.Intersect(new[] { "CREATE_ADD_BB" }).Any();

                if (restricted)
                    return new UnprocessableEntityObjectResult("user tidak memliki akses untuk request !");

                if (string.IsNullOrEmpty(core.keyDesa) || string.IsNullOrEmpty(core.keyProject) || string.IsNullOrEmpty(core.alasHak))
                    return new UnprocessableEntityObjectResult("Data tidak boleh kosong !");

                (bool ok, string message, string keyPersil) = SavePersilNew(core, user);
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                PersilRequest newPersil = new PersilRequest(user, keyPersil, "ADD_BB");

                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "PersilApproval");
                newPersil.identifier = dno.Generate(DateTime.Now, true, string.Empty);
                newPersil.identifier = newPersil.identifier.Replace("{JR}", "NEW-BB");
                HostReqPersil.Add(newPersil);

                var lastState = newPersil.instance.lastState;
                LogWorkListCore logWLCore = new LogWorkListCore(newPersil.instKey, user.key, lastState.time,
                                 lastState?.state ?? ToDoState.unknown_, ToDoVerb.create_, "");

                (ok, message) = InsertLogWorklist(logWLCore);
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

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
        /// Get Data Persil to be edited
        /// </summary>
        [HttpGet("persil/edit")]
        public IActionResult GetDataPersilToEdit([FromQuery] string token, [FromQuery] string reqKey, string keyPersil)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var request = contextPay.persilApproval.FirstOrDefault(p => p.key == reqKey);

                if (request == null && keyPersil == null)
                    return new UnprocessableEntityObjectResult("Request tidak tersedia !");

                Persil persil = GetPersilBykey(keyPersil != null ? keyPersil : request.info.keyPersil);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ditemukan !");

                var lastEntry = !string.IsNullOrEmpty(reqKey) ? persil.basic.entries.LastOrDefault() : persil.basic.entries.LastOrDefault(x => x.approved == true);
                var lastEntryItem = lastEntry.item;
                string requestType = !string.IsNullOrEmpty(reqKey) ? "new" : "update";

                ReqUpdatePersilApprovalCore result = FillReqUpdApprovalCore(lastEntryItem, lastEntry.rejectNote, requestType);

                return new JsonResult(result);
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

        private ReqUpdatePersilApprovalCore FillReqUpdApprovalCore(PersilBasic entry, string rejectNote, string requestType)
        {
            string reject = UpdatePersilRejectNote(rejectNote);
            Persil persilParent = GetPersilBykey(entry.keyParent);
            ReqUpdatePersilApprovalCore core = new ReqUpdatePersilApprovalCore()
            {
                requestType = requestType,
                keyParent = entry.keyParent,
                idBidangParent = !string.IsNullOrEmpty(entry.keyParent) ? persilParent.IdBidang : null,
                jenisAlasHak = entry.en_jenis,
                jenisProses = entry.en_proses,
                jenisLahan = entry.en_lahan,
                statusBerkas = entry.en_status,

                keyProject = entry.keyProject,
                keyDesa = entry.keyDesa,
                nomorPeta = entry.noPeta,
                penampung = entry.keyPenampung,
                ptsk = entry.keyPTSK,

                terimaBerkas = entry.terimaBerkas,
                inputBerkas = entry.inputBerkas,
                pemilik = entry.pemilik,
                telpPemilik = entry.telpPemilik,
                group = entry.group,
                mediator = entry.mediator,
                telpMediator = entry.telpMediator,
                alias = entry.alias,

                // Surat
                nomorSurat = entry.surat.nomor,
                namaSurat = entry.surat.nama,
                tanggalTerimaSurat = entry.surat.tglTerima,
                keteranganSurat = entry.surat.note,
                jenisSurat = entry.en_jenis,

                luasSurat = entry.luasSurat,
                luasInternal = entry.luasInternal,
                kekurangan = entry.kekurangan,
                keterangan = entry.note,

                command = new UpdateRequestCommand()
                {
                    reason = reject
                }
            };
            return core;
        }

        private string UpdatePersilRejectNote(string rejectNote)
        {
            if (rejectNote == null)
                return "";
            rejectNote = (rejectNote ?? "").Replace("\n", "");
            string[] note = (rejectNote ?? "").Split("##>");
            string koreksi = note.Count() != 2 ? "" : note[1];
            string rejectString = note[0];
            string[] rejectFields = rejectString.Split(";");

            var notes = rejectFields.Select(r => new
            {
                note = MatchReasonWithOurModel(r)
            });

            return string.Join("; ", notes.Select(n => n.note)) + (note.Count() != 2 ? $"" : $"##> {koreksi}");
        }

        private string MatchReasonWithOurModel(string field)
        => field switch
        {
            "proses" => "jenisProses",
            "lahan" => "jenisLahan",
            "jenis" => "jenisAlasHak",
            "status" => "statusBerkas",
            "keyProject" => "keyProject",
            "keyDesa" => "keyDesa",
            "noPeta" => "nomorPeta",
            "keyPenampung" => "penampung",
            "keyPTSK" => "ptsk",
            "terimaBerkas" => "terimaBerkas",
            "inputBerkas" => "inputBerkas",
            "pemilik" => "pemilik",
            "telpPemilik" => "telpPemilik",
            "group" => "group",
            "mediator" => "mediator",
            "telpMediator" => "telpMediator",
            "alias" => "alias",
            "surat.jnsalas" => "jenisSurat",
            "surat.nomor" => "nomorSurat",
            "surat.nama" => "namaSurat",
            "surat.tglTerima" => "tanggalTerimaSurat",
            "surat.note" => "keteranganSurat",
            "luasSurat" => "luasSurat",
            "kekurangan" => "kekurangan",
            "note" => "keterangan",
            "IdBidangParent" => "idBidangParent",
            _ => field
        };

        private string SwitchReasonToValidateModel(string field)
        => field switch
        {
            "jenisProses" => "proses",
            "jenisLahan" => "lahan",
            "jenisAlasHak" => "jenis",
            "statusBerkas" => "status",
            "nomorPeta" => "noPeta",
            "penampung" => "keyPenampung",
            "ptsk" => "keyPTSK",
            "terimaBerkas" => "terimaBerkas",
            "inputBerkas" => "inputBerkas",
            "pemilik" => "pemilik",
            "telpPemilik" => "telpPemilik",
            "group" => "group",
            "mediator" => "mediator",
            "telpMediator" => "telpMediator",
            "alias" => "alias",
            "jenisSurat" => "surat.jnsalas",
            "nomorSurat" => "surat.nomor",
            "namaSurat" => "surat.nama",
            "tanggalTerimaSurat" => "surat.tglTerima",
            "keteranganSurat" => "surat.note",
            "luasSurat" => "luasSurat",
            "kekurangan" => "kekurangan",
            "keterangan" => "note",
            _ => field
        };

        /// <summary>
        /// Edit Persil Data in Request
        /// </summary>
        [HttpPost("persil/edit/save")]
        public IActionResult EditRequestedPersil([FromQuery] string token, [FromQuery] string reqKey, [FromBody] ReqUpdatePersilApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var request = contextPay.persilApproval.FirstOrDefault(p => p.key == reqKey);
                if (request == null)
                    return new UnprocessableEntityObjectResult("Request tidak ditemukan !");

                var lastState = request.instance.lastState;
                if (lastState?.state != ToDoState.created_ && lastState?.state != ToDoState.rejected_)
                    return new UnprocessableEntityObjectResult("Edit tidak dapat dilakukan !");

                Persil persil = GetPersilBykey(request.info.keyPersil);
                (bool ok, string message) = UpdatePersilEntries(core, user, persil);

                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                RuningFlow(user, core.command, request.instance);

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
        /// Create Request to Update Persil Data
        /// </summary>
        [HttpPost("persil/update/save")]
        public IActionResult CreateUpdatePersil([FromQuery] string token, [FromQuery] string keyPersil, [FromBody] ReqUpdatePersilApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                string[] privs = user.privileges.Select(p => p.identifier).ToArray();

                if (string.IsNullOrEmpty(core.keyDesa) || string.IsNullOrEmpty(core.keyProject) || string.IsNullOrEmpty(core.nomorSurat))
                    return new UnprocessableEntityObjectResult("Data tidak boleh kosong !");

                List<string> keyPersils = new List<string>();
                keyPersils.Add(keyPersil);
                (bool ok, string message) = CheckExistsRequest(keyPersils);
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                if (!ValidatePersilRequest(keyPersil))
                    return new UnprocessableEntityObjectResult("Request sudah dibuat untuk bidang yang anda pilih !");

                Persil persil = GetPersilBykey(keyPersil);
                string discriminator = (persil.en_state ?? 0) == StatusBidang.bebas ? "EDIT_SB" : "EDIT_BB";
                string jrIdentifier = (persil.en_state ?? 0) == StatusBidang.bebas ? "UPD-SB" : "UPD-BB";

                bool bb = privs.Intersect(new[] { "CREATE_EDIT_BB" }).Any();
                bool sb = privs.Intersect(new[] { "CREATE_EDIT_SB" }).Any();
                bool bbsb = bb && sb;

                bool restricted = (bbsb == false && bb && discriminator == "EDIT_SB") || (bbsb == false && sb && discriminator == "EDIT_BB");

                if (restricted)
                    return new UnprocessableEntityObjectResult("Privilege anda tidak berhak untuk melakukan request pemutakhiran bidang berikut !");

                //Update Entries Persil
                (ok, message) = UpdatePersilEntries(core, user, persil);

                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                //Create Request
                PersilRequest newPersil = new PersilRequest(user, keyPersil, discriminator);
                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "PersilApproval");
                newPersil.identifier = dno.Generate(DateTime.Now, true, string.Empty);
                newPersil.identifier = newPersil.identifier.Replace("{JR}", jrIdentifier);
                HostReqPersil.Add(newPersil);

                var lastState = newPersil.instance.lastState;
                LogWorkListCore logWLCore = new LogWorkListCore(newPersil.instKey, user.key, lastState.time,
                                 lastState?.state ?? ToDoState.unknown_, ToDoVerb.create_, "");

                (ok, message) = InsertLogWorklist(logWLCore);
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

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
        /// Get List Bidang to be Update
        /// </summary>
        [HttpGet("persil/update/list")]
        public IActionResult GetListPersilToUpdate([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);

                bool isBB = user.privileges.Any(x => x.identifier == "CREATE_EDIT_BB");
                bool isSB = user.privileges.Any(x => x.identifier == "CREATE_EDIT_SB");
                bool isBBSB = isBB & isSB;

                int[] en_states = isBB ? new[] { 1 } : isSB ? new[] { 0 } : isBBSB ? new[] { 0, 1 } : new[] { 99 };

                //List Request
                List<PersilRequest> requests = contextPay.persilApproval.All().ToList();

                var instanceKeys = requests.Select(d => d.instKey).ToList();
                string instKeys = string.Join(",", instanceKeys);

#if test
                var instances = (ghost.GetMany(instKeys, "").GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
#else
                var instances = (graphhost.GetMany(instKeys, "") ?? new GraphMainInstance[0]);
#endif

                List<string> keyInstance = instances.Where(i => i.lastState?.state != ToDoState.complished_)
                                                    .Select(i => i.key).ToList();

                List<string> keyPersils = requests.Where(r => keyInstance.Contains(r.instKey))
                                                  .Select(r => r.info.keyPersil).ToList();

                ////List Persil
                Persil[] listPersil = contextPay.persils.All()
                                                        .Where(p => p.invalid != true
                                                            && en_states.Contains((int?)p.en_state ?? 0)
                                                            && p.basic.current != null
                                                            && !keyPersils.Contains(p.key)
                                                               )
                                                        .ToArray();

                ////PTSK
                var dataPTSK = contextPay.db.GetCollection<Company>("masterdatas").Find("{_t:'ptsk',invalid:{$ne:true}}").ToList();

                ////PT
                var dataPenampung = contextPay.db.GetCollection<Company>("masterdatas").Find("{_t:'pt',invalid:{$ne:true},status:1}").ToList();

                ////Villages
                var villages = contextPay.GetVillages();

                List<PersilCore> result = listPersil.Select(lP => new PersilCore()
                {
                    key = lP.key,
                    IdBidang = lP.IdBidang,
                    PTSK = dataPTSK.FirstOrDefault(p => p.key == lP.basic?.current?.keyPTSK)?.identifier,
                    penampung = dataPenampung.FirstOrDefault(p => p.key == lP.basic?.current?.keyPenampung)?.identifier,
                    project = villages.FirstOrDefault(v => v.project.key == lP.basic?.current?.keyProject).project?.identity,
                    desa = villages.FirstOrDefault(v => v.desa.key == lP.basic?.current?.keyDesa).desa?.identity,
                    pemilik = lP.basic?.current?.pemilik,
                    alias = lP.basic?.current?.alias,
                    group = lP.basic?.current?.group,
                    mediator = lP.basic?.current?.mediator,
                    noPeta = lP.basic?.current?.noPeta,
                    namaSurat = lP.basic?.current?.surat?.nama,
                    nomorSurat = lP.basic?.current?.surat?.nomor,
                    luasSurat = lP.basic?.current?.luasSurat,
                    luasPelunasan = lP.basic?.current?.luasDibayar
                }).ToList();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<PersilCore>), typeof(PersilCore), gs);
                var filteredData = xlst.result.Cast<PersilCore>().ToList();
                var sorted = filteredData.GridFeed(gs);

                return new JsonResult(sorted);
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
        /// Validating Requested Persil Data
        /// </summary>
        [HttpPost("persil/approval")]
        public IActionResult ValidatingPersilRequest([FromQuery] string token, [FromBody] PersilApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                PersilRequest request = contextPay.persilApproval.FirstOrDefault(p => p.key == core.command.reqKey);

                if (request == null)
                    return new UnprocessableEntityObjectResult("request tidak ditemukan !");

                Persil persil = GetPersilBykey(request.info.keyPersil);

                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ditemukan !");

                RuningFlow(user, core.command, request.instance, core.approve);

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

        [PersilMaterializer(Auto = false)]
        private (bool, string) UpdatePersilEntries(ReqUpdatePersilApprovalCore core, user user, Persil persil, bool reqUpdate = false)
        {
            (bool ok, string message) = (true, null);
            try
            {
                ValidatableEntry<PersilBasic> lastEntry = persil.basic.entries.LastOrDefault();
                bool isNew = lastEntry == null;

                if (!string.IsNullOrEmpty(core.keyParent)) { }
                (ok, message) = InsertEntryForKeyParent(user, core.keyParent, persil.key);
                if (!ok)
                    return (ok, message);

                PersilBasic entry = isNew ? new PersilBasic() : lastEntry.item;

                Persil persilParent = GetPersilBykey(core.keyParent);

                entry.keyParent = core.keyParent;
                entry.IdBidangParent = !string.IsNullOrEmpty(core.keyParent) ? persilParent.IdBidang : null;
                entry.en_jenis = core.jenisAlasHak;
                entry.en_proses = core.jenisProses;
                entry.en_lahan = core.jenisLahan;
                entry.en_status = core.statusBerkas;
                entry.keyProject = core.keyProject;
                entry.keyDesa = core.keyDesa;
                entry.noPeta = core.nomorPeta;
                entry.keyPenampung = core.penampung;
                entry.keyPTSK = core.ptsk;

                entry.terimaBerkas = core.terimaBerkas;
                entry.inputBerkas = core.inputBerkas;
                entry.pemilik = core.pemilik;
                entry.telpPemilik = core.telpPemilik;
                entry.group = core.group;
                entry.mediator = core.mediator;
                entry.telpMediator = core.telpMediator;
                entry.alias = core.alias;

                entry.surat = new landrope.mod2.AlasHak
                {
                    nomor = core.nomorSurat,
                    nama = core.namaSurat,
                    tglTerima = core.tanggalTerimaSurat,
                    note = core.keteranganSurat,
                    en_jnsalas = core.jenisAlasHak,
                    jnsalas = core.jenisAlasHak.ToString("g")
                };

                entry.luasSurat = core.luasSurat;
                entry.luasInternal = core.luasInternal;
                entry.kekurangan = core.kekurangan;
                entry.note = core.keterangan;

                persil.basic.PutEntry(entry, ChangeKind.Update, user.key);
                contextPay.persils.Update(persil);
                contextPay.SaveChanges();

                MyTracer.TraceInfo2($"Update Entries to Materializer for Persil : {persil.key}");
                MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                (ok, message) = (true, "Berhasil Update Persil!");
            }
            catch (Exception ex)
            {
                contextPay.DiscardChanges();
                (ok, message) = (false, $"Gagal input Persil, {ex.Message}!");
            }
            finally
            { }

            return (ok, message);
        }

        /// <summary>
        /// Get List of Persil Parent
        /// </summary>
        [HttpGet("list/persil/parent")]
        public IActionResult GetListPersilParent([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var persils = contextPay.persils.All().Where(x => x.en_state != StatusBidang.batal
                                                       && x.invalid != true
                                                       && x.basic.current != null
                                                       && x.basic.current.keyParent == null
                                                       && (x.basic.entries.LastOrDefault().item.keyParent == null ||
                                                            (
                                                                x.basic.entries.LastOrDefault().item.keyParent != null &&
                                                                x.basic.entries.LastOrDefault().approved == false
                                                             )
                                                            )
                                                      ).ToList();

                var villages = contextPay.GetVillages();

                var result = persils.Join(villages, p => p.basic.current.keyDesa, v => v.desa.key, (p, v) => new PersilParentCore()
                {
                    key = p.key,
                    IdBidang = p.IdBidang,
                    desa = v.project.identity,
                    project = v.desa.identity,
                    group = p.basic.current?.group,
                    nomorSurat = p.basic.current?.surat?.nomor,
                    luasSurat = p.basic.current?.luasSurat,
                    noPeta = p.basic.current?.noPeta,
                    pemilik = p.basic.current?.pemilik ?? p.basic.current?.surat?.nama,
                });

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<PersilParentCore>), typeof(PersilParentCore), gs);
                var filteredData = xlst.result.Cast<PersilParentCore>().ToList();
                var sorted = filteredData.GridFeed(gs);

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

        #endregion

        #region Common Function
        [HttpGet("worklist")]
        public IActionResult GetWorklist([FromQuery] string token, string opr, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var creator = priv.Intersect(new[] { "CREATE_ADD_SB", "CREATE_ADD_BB", "CREATE_EDIT_SB", "CREATE_EDIT_BB", "CREATE_ADD_MAP",
                                                     "CREATE_EDIT_MAP", "MAPTOOLS_FULL", "CREATE_PROSES_BB", "CREATE_PROSES_SB", "CREATE_PROJECT_BB", "CREATE_PROJECT_SB"}).Any();
                var approval = priv.Intersect(new[] { "APPR1_ADD_SB", "APPR1_EDIT_SB", "APPR1_ADD_BB", "APPR1_ADD_BB", "APPR1_EDIT_BB",
                                                      "APPR2_ADD_SB", "APPR2_EDIT_SB", "APPR2_EDIT_SB", "APPR2_ADD_BB", "APPR2_EDIT_BB",
                                                      "APPR1_ADD_MAP", "APPR2_ADD_MAP", "APPR1_EDIT_MAP", "APPR2_EDIT_MAP", "APPR1_PROSES_BB",
                                                      "APPR1_PROSES_SB", "APPR2_PROSES_BB", "APPR2_PROSES_SB", "APPR1_PROJECT_BB",
                                                      "APPR1_PROJECT_SB", "APPR2_PROJECT_BB", "APPR2_PROJECT_SB"
                                                    }).Any();

#if test
                var graphTree = ghost.ListTree(user, FlowModul.LandApprove).GetAwaiter().GetResult() ?? new GraphTree[0];
#else
                var graphTree = graphhost.ListTree(user, FlowModul.LandApprove).ToArray() ?? new GraphTree[0];
#endif

                var graphMainNodes = graphTree.Where(x => x.subs.Any()).Select(x => (x.main, inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes)).ToArray();
                var instkeys = graphMainNodes.Select(g => g.main.key).ToArray();

                var prokeys = string.Join(',', instkeys.Select(k => $"'{k}'"));

                bool isAlwaysStrict = opr.ToLower() != "maprequest";
                (var keys, var strict) = (approval || isAlwaysStrict) ? (prokeys, true) : (creator) ? (null, false) : (string.Empty, true);

                string stage = "";


                if (strict)
                    stage = $"<instKey : <$in: [{keys}]>, _t : '{opr}', invalid : <$ne : true>>".Replace("<", "{").Replace(">", "}");

                else
                    stage = $"<invalid : <$ne : true>, _t: '{opr}'>".Replace("<", "{").Replace(">", "}");

                var qry = contextPay.GetCollections(new UpdRequestData(), "update_request_data", stage, "{}").ToList();

                if (opr == "StateRequest")
                {
                    var sb = priv.Intersect(new[] { "CREATE_EDIT_SB", "APPR1_EDIT_SB", "APPR2_EDIT_SB" }).Any();
                    var bb = priv.Intersect(new[] { "CREATE_EDIT_BB", "APPR1_EDIT_BB", "APPR2_EDIT_BB" }).Any();
                    var edit = bb || sb;
                    var all = bb && sb;

                    if (!edit)
                        return new UnprocessableEntityObjectResult("Anda tidak mempunyai hak akses");

                    var newQry = new StateRequest[0];

                    if (all)
                        newQry = qry.Cast<StateRequest>().ToArray();
                    else if (sb)
                        newQry = qry.Cast<StateRequest>().Where(x => x.info.type == TypeState.bebas).ToArray();
                    else if (bb)
                        newQry = qry.Cast<StateRequest>().Where(x => x.info.type == TypeState.belumbebas).ToArray();

                    var view = newQry.Select(x => x.ToView(contextPay)).ToArray();

                    var nxdata = view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()));

                    //var nxdata = (strict ? view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                    //                            (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()))
                    //                       :
                    //                      view.GroupJoin(graphMainNodes, a => a.instKey, g => g.main?.key,
                    //                            (a, sg) => (a, g: sg.FirstOrDefault()))
                    //                            .Select(x => (x.a, i: x.g.main, nm: x.g.inmain?.LastOrDefault()))).ToArray().AsParallel();

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<UpdStateRequestView>), typeof(UpdStateRequestView), gs);
                    var filteredData = xlst.result.Cast<UpdStateRequestView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
                }
                else if (opr == "PersilRequest")
                {
                    bool allowedAccess = priv.Intersect(new[] { "CREATE_ADD_BB", "CREATE_EDIT_SB", "CREATE_EDIT_BB", "APPR1_ADD_BB",
                                                                "APPR2_ADD_BB", "APPR1_EDIT_SB", "APPR2_EDIT_SB", "APPR1_EDIT_BB", "APPR2_EDIT_BB" }).Any();

                    if (!allowedAccess)
                        return new UnprocessableEntityObjectResult("Anda tidak mempunyai hak akses");

                    bool isAdd = priv.Any(p => p.Contains("ADD_BB"));
                    bool isEditBB = priv.Any(p => p.Contains("EDIT_BB"));
                    bool isEditSB = priv.Any(p => p.Contains("EDIT_SB"));

                    PersilRequest[] newQry = new PersilRequest[0];

                    if (isAdd)
                        newQry = newQry.Union(qry.Cast<PersilRequest>().Where(q => (q.identifier ?? "").Contains("NEW-BB"))).ToArray();
                    if (isEditBB)
                        newQry = newQry.Union(qry.Cast<PersilRequest>().Where(q => (q.identifier ?? "").Contains("UPD-BB"))).ToArray();
                    if (isEditSB)
                        newQry = newQry.Union(qry.Cast<PersilRequest>().Where(q => (q.identifier ?? "").Contains("UPD-SB"))).ToArray();

                    var villages = contextPay.GetVillages();
                    List<user> listUser = GetListUser();
                    string[] keyPersils = newQry.Select(nQ => nQ.info.keyPersil).ToArray();
                    List<Persil> listPersils = GetPersilByKeys(keyPersils);

                    List<PersilApprovalView> view = newQry.Select(lb => lb.ToView(listUser, listPersils, user, villages)).ToList();

                    var nxdata = (strict ? view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()))
                                            :
                                           view.GroupJoin(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, sg) => (a, g: sg.FirstOrDefault()))
                                                 .Select(x => (x.a, i: x.g.main, nm: x.g.inmain?.LastOrDefault()))).ToArray().AsParallel();

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<PersilApprovalView>), typeof(PersilApprovalView), gs);
                    var filteredData = xlst.result.Cast<PersilApprovalView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
                }
                else if (opr == "ProjectRequest")
                {
                    //var sb = priv.Intersect(new[] { "CREATE_PROJECT_SB", "APPR1_PROJECT_SB", "APPR2_PROJECT_SB" }).Any();
                    //var bb = priv.Intersect(new[] { "CREATE_PROJECT_BB", "APPR1_PROJECT_BB", "APPR2_PROJECT_BB" }).Any();
                    //var edit = bb || sb;
                    //var all = bb && sb;

                    //if (!edit)
                    //    return new UnprocessableEntityObjectResult("Anda tidak mempunyai hak akses");

                    var locations = contextPay.GetVillages().ToArray();
                    var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                                .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                    var newQry = new ProjectRequest[0];

                    //if (all)
                    //    newQry = qry.Cast<ProjectRequest>().ToArray();
                    //else if (sb)
                    //    newQry = qry.Cast<ProjectRequest>().Where(x => x.info.type == TypeState.bebas).ToArray();
                    //else if (bb)
                    //    newQry = qry.Cast<ProjectRequest>().Where(x => x.info.type == TypeState.belumbebas).ToArray();

                    newQry = qry.Cast<ProjectRequest>().ToArray();

                    var view = newQry.Select(x => x.ToView(contextPay, locations, ptsks)).ToArray();

                    var nxdata = view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()));

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<ProjectRequestView>), typeof(ProjectRequestView), gs);
                    var filteredData = xlst.result.Cast<ProjectRequestView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
                }
                else if (opr == "EnProsesRequest")
                {
                    //bool sb = priv.Intersect(new[] { "CREATE_PROSES_SB", "APPR1_PROSES_SB", "APPR2_PROSES_SB" }).Any();
                    //bool bb = priv.Intersect(new[] { "CREATE_PROSES_BB", "APPR1_PROSES_BB", "APPR2_PROSES_BB" }).Any();
                    //bool edit = bb || sb;
                    //bool all = bb && sb;

                    //if (!edit)
                    //    return new UnprocessableEntityObjectResult("Anda tidak mempunyai hak akses");

                    EnProsesRequest[] newQry = new EnProsesRequest[0];
                    //if (all)
                    //    newQry = newQry.Union(qry.Cast<EnProsesRequest>()).ToArray();
                    //else if (sb)
                    //    newQry = newQry.Union(qry.Cast<EnProsesRequest>().Where(q => (q.info.type == TypeState.bebas))).ToArray();
                    //else if (bb)
                    //    newQry = newQry.Union(qry.Cast<EnProsesRequest>().Where(q => (q.info.type == TypeState.belumbebas))).ToArray();

                    //string typeRequest = all ? "0, 1" : bb ? "1" : sb ? "0" : "99";

                    newQry = newQry.Union(qry.Cast<EnProsesRequest>()).ToArray();

                    List<user> users = contextPay.GetDocuments(new user(), "securities", "{$match: {_t: 'user'}}", "{$project:{_id: 0}}").ToList();
                    var villages = contextPay.GetVillages();
                    var ptsk = contextPay.GetDocuments(new PTSK(), "masterdatas", "{$match: {$and: [{_t:'ptsk'}, {'invalid': {$ne:true}}]}}", "{$project: {_id: 0}}").ToList();

                    List<EnProsesApprovalView> view = newQry.Select(x => x.ToView(users, villages, ptsk)).ToList();

                    var nxdata = view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()));

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)
                                     ).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<EnProsesApprovalView>), typeof(EnProsesApprovalView), gs);
                    var filteredData = xlst.result.Cast<EnProsesApprovalView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
                }
                else if (opr == "LandSplitRequest")
                {
                    var locations = contextPay.GetVillages().ToArray();
                    var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                                .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                    var newQry = new LandSplitRequest[0];
                    newQry = qry.Cast<LandSplitRequest>().ToArray();

                    var view = newQry.Select(x => x.ToView(contextPay, locations, ptsks)).ToArray();

                    var nxdata = view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()));

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<LandSplitRequestView>), typeof(LandSplitRequestView), gs);
                    var filteredData = xlst.result.Cast<LandSplitRequestView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
                }
                else
                {
                    var newQry = qry.Cast<MapRequest>().ToArray();

                    MyTracer.TraceInfo2("Get Villages");
                    var villages = contextPay.GetVillages();

                    MyTracer.TraceInfo2("Get ListPersils");
                    string keyPersils = string.Join(",", newQry.Where(nQ => nQ.info.keyPersil != null)
                                                .Select(nQ => $"'{nQ.info.keyPersil}'"));

                    keyPersils += string.Join(",", newQry.Where(x => x.info.keyPersils.Count() != 0)
                                                     .Select(x => string.Join(",", x.info.keyPersils.Select(x => $"'{x}'"))));

                    List<Persil> listPersils = GetPersilByKeysString(keyPersils);

                    MyTracer.TraceInfo2("GetListUser");
                    List<user> users = GetListUser();
                    string instKeys_ = string.Join(",", newQry.Where(x => x.invalid != true).Select(nQ => nQ.instKey));
                    instKeys_ = string.Join(",", instKeys_.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                    MyTracer.TraceInfo2("GetWorklist");
                    List<LogWorklist> logWorklist = GetListHistoryWorklist(instKeys_);

                    MyTracer.TraceInfo2("ToView");
                    var view = newQry.Select(x => x.ToView(villages, users, listPersils, logWorklist)).ToArray();

                    bool enable_restriction = user.privileges.Any(x => x.identifier == "MAPTOOLS_FULL");
                    if (enable_restriction)
                    {
                        var userProjects = UserExtension.GetProjects(user);
                        string allowedKeyProjects = string.Join(",", userProjects.Select(uP => $"'{uP}'"));
                        view = view.Where(v => v.info.bidangs.Where(b => allowedKeyProjects.Contains(b.keyProject)).Count() != 0).ToArray();
                    }

                    var nxdata = (strict ? view.Join(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault()))
                                            :
                                           view.GroupJoin(graphMainNodes, a => a.instKey, g => g.main?.key,
                                                 (a, sg) => (a, g: sg.FirstOrDefault()))
                                                 .Select(x => (x.a, i: x.g.main, nm: x.g.inmain?.LastOrDefault()))).ToArray().AsParallel();

                    var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes.ToArray() ?? new GraphRoute[0])).ToArray().AsParallel();
                    var ndatax = ndata.Select(x => (x, y: x.a)).ToArray().AsParallel();

                    var data = ndatax.Where(X => X.y != null).Select(X => X.y?
                                     .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.TitleR(), x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
                                     .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)
                                     ).ToArray();

                    var xlst = ExpressionFilter.Evaluate(data, typeof(List<UpdMapRequestView>), typeof(UpdMapRequestView), gs);
                    var filteredData = xlst.result.Cast<UpdMapRequestView>().ToList();
                    var sorted = filteredData.OrderByDescending(x => x.created).GridFeed(gs);

                    return Ok(sorted);
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

        /// <summary>
        /// Run The Flow to Next Step 
        /// </summary>
        [HttpPost("step")]
        [PersilMaterializer(Auto = false)]
        public IActionResult Step([FromQuery] string token, [FromBody] UpdateRequestCommand cmd)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var main = new GraphMainInstance();

                var res = cmd._t switch
                {
                    "MapRequest" => Map(),
                    "StateRequest" => State(),
                    "ProjectRequest" => Project(),
                    "EnProsesRequest" => EnProses(),
                    "LandSplitRequest" => LandSplit(),
                    _ => Persil(),
                };

                return RuningFlow(user, cmd, main);

                IActionResult Map()
                {
                    MapRequest request = contextPay.mapRequest.FirstOrDefault(r => r.key == cmd.reqKey);
                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

                    return Ok();
                }

                IActionResult State()
                {
                    var host = HostServicesHelper.GetStateRequestHost(services);
                    var request = host.GetStateRequest(cmd.reqKey) as StateRequest;

                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

                    return Ok();
                }

                IActionResult Persil()
                {
                    PersilRequest request = HostReqPersil.GetPersilAppByKey(cmd.reqKey) as PersilRequest;

                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

                    return Ok();
                }

                IActionResult Project()
                {
                    ProjectRequest request = contextPay.projectRequests.FirstOrDefault(x => x.key == cmd.reqKey) as ProjectRequest;
                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

                    return Ok();
                }

                IActionResult EnProses()
                {
                    EnProsesRequest request = contextPay.enprosesRequest.FirstOrDefault(x => x.key == cmd.reqKey) as EnProsesRequest;
                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

                    return Ok();
                }

                IActionResult LandSplit()
                {
                    LandSplitRequest request = contextPay.landsplitRequests.FirstOrDefault(x => x.key == cmd.reqKey) as LandSplitRequest;
                    if (request == null)
                        return new UnprocessableEntityObjectResult("Request tidak ada");
                    if (request.invalid == true)
                        return new UnprocessableEntityObjectResult("Request tidak aktif");

                    main = request.instance;

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
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        //[NeedToken("CREATE_ADD_BB,CREATE_EDIT_BB,CREATE_EDIT_SB,CREATE_PROSES_BB,CREATE_PROSES_SB,APPR1_PROSES_BB,APPR1_PROSES_SB,APPR2_PROSES_BB,APPR2_PROSES_SB,APPR1_ADD_BB,APPR1_EDIT_BB,APPR1_EDIT_SB,APPR2_ADD_BB,APPR2_EDIT_BB,APPR2_EDIT_SB")]
        [HttpGet("list")]
        public IActionResult GetRequestList([FromQuery] string token, string opr, string tfind, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);

                if (opr == "StateRequest")
                {
                    return Ok();
                }
                else if (opr == "PersilRequest")
                {
                    List<PersilApprovalView> persilApproval = contextPay.GetDocuments(new PersilApprovalView(), "update_request_data",
                        "{$match: { '_t': 'PersilRequest'}}",
                        "{$lookup: {from: 'graphables', localField: 'instKey', foreignField: 'key', as: 'graph'}}",
                        "{$unwind: {path: '$graph', preserveNullAndEmptyArrays: true}}",
                        "{$lookup: {from: 'securities', localField: 'creator', foreignField: 'key', as: 'creator'}}",
                        "{$unwind: {path: '$creator', preserveNullAndEmptyArrays: true}}",
                        "{$lookup: {from: 'persils_v2', localField: 'info.keyPersil', foreignField: 'key', as:'persil'}}",
                        "{$unwind: {path: '$persil', preserveNullAndEmptyArrays: true}}",
                       @"{$lookup:
                        {from: 'maps', let: { keyDesa: '$persil.basic.current.keyDesa'}, pipeline:
                            [
                            {$unwind: '$villages'},
                            {$match: {$expr: {$eq: ['$villages.key' , '$$keyDesa']} } },
                            {$project:
                                {
                                _id: 0,
                                desa: '$villages.identity',
                                project: '$identity'}
                            }
                        ], as: 'map'}}",
                       "{$unwind: { path: '$map', preserveNullAndEmptyArrays: true}}",
                       "{$addFields: { status: '$graph.lastState.state'}}",
                       @"
                        {$project:
                        {
                        _id: 0,
                            key: '$key',
                            creator: '$creator.FullName',
                            created: '$created',
                            instKey: '$instKey',
                            status:  {$switch : {
                                        branches:
                                        [
                                                {case: {$eq: ['$graph.lastState.state', 1]}, then: 'Approval Tingkat 1'},
                                                {case: {$eq: ['$graph.lastState.state', 2]}, then: 'Approval Tingkat 1'},
                                                {case: {$eq: ['$graph.lastState.state', 53]}, then: 'Perbaikan'},
                                                {case: {$eq: ['$graph.lastState.state', 70]}, then: 'Approval Tingkat 2'},
                                                {case: {$eq: ['$graph.lastState.state', 71]}, then: 'Ditolak Approval Tingkat 2'},
                                        ],
                                                default: 'Unknown'}
                                       },
                            state: {$literal: 0},
                            info:
                            {
                                keyPersil: '$info.keyPersil',
                                idBidang: {$ifNull: ['$persil.IdBidang', '(Belum Ada)']},
                                keyCreator: '$creator.key',
                                keyProject: '$persil.basic.current.keyProject',
                                project: '$map.project',
                                keyDesa: '$persil.basic.current.keyDesa',
                                desa: '$map.desa',
                                jenisAlasHak:
                                {$switch : {
                                    branches:
                                        [
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_jenis', -1]}, 1]}, then: 'Girik'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_jenis', -1]}, 2]}, then: 'SHP'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_jenis', -1]}, 3]}, then: 'HGB'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_jenis', -1]}, 4]}, then: 'SHM'}
                                                       ],
                                                       default: 'Unknown'
                                               }
                                },
                                jenisProses:
                                {$switch : {
                                    branches:
                                        [
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_proses', -1]}, 1]}, then: 'Batal'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_proses', -1]}, 2]}, then: 'Overlap'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_proses', -1]}, 3]}, then: 'Lokal'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_proses', -1]}, 4]}, then: 'Hibah'},
                                                        {case: {$eq: [{$arrayElemAt: ['$persil.basic.entries.item.en_proses', -1]}, 5]}, then: 'Parent'}
                                                       ],
                                                       default: 'Standar'
                                               }
                                },
                                luas: {$ifNull: [{$arrayElemAt: ['$persil.basic.entries.item.luasSurat', -1]}, 0]},
                                pemilik: {$arrayElemAt: ['$persil.basic.entries.item.pemilik',-1]},
                                alasHak: {$arrayElemAt: ['$persil.basic.entries.item.surat.nomor',-1]},
                                nomorPeta: {$arrayElemAt: ['$persil.basic.entries.item.noPeta',-1]},
                                group: {$arrayElemAt: ['$persil.basic.entries.item.group',-1]},
                                alias: {$arrayElemAt: ['$persil.basic.entries.item.alias',-1]}
                            }}}").ToList();
                    var xlst = ExpressionFilter.Evaluate(persilApproval, typeof(List<PersilApprovalView>), typeof(PersilApprovalView), gs);
                    var data = xlst.result.Cast<PersilApprovalView>().ToList();
                    var result = data.GridFeed(gs);
                    return Ok(result);
                }
                else
                {
                    var privs = user.privileges.Select(p => p.identifier).ToArray();

                    //bool isBB = privs.Intersect(new[] { "CREATE_PROSES_BB", "APPR1_PROSES_BB", "APPR2_PROSES_BB" }).Any();
                    //bool isSB = privs.Intersect(new[] { "CREATE_PROSES_SB", "APPR1_PROSES_SB", "APPR2_PROSES_SB" }).Any();
                    //bool isBBSB = isBB & isSB;
                    //string typeRequest = isBBSB ? "0, 1" : isBB ? "1" : isSB ? "0" : "99";

                    //var privileges = contextPay.GetDocuments(new { _id = "", priv = new[] { "" } }, "graphables",
                    //"{$match : {'Core.name' : 'LAND_APPROVAL'}}",
                    //"{$unwind: '$Core.nodes'}",
                    //"{$unwind: '$Core.nodes.routes'}",
                    //"{$unwind: '$Core.nodes.routes.privs'}",
                    //"{$unwind: '$Core.nodes.routes.privs'}",
                    //"{$group: { _id: {key:'$key',privs:'$Core.nodes.routes.privs'}}}",
                    //"{$project: {key:'$_id.key',privs:'$_id.privs'}}",
                    //"{$group: { _id: '$key',priv:{$push:'$privs'}}}"
                    //).ToList(); 

                    var privileges = contextPay.GetCollections(new { _id = "", priv = new[] { "" } }, "WF_PRIVS", "{}", "{}").ToList();

                    var canView = privileges.Where(x => privs.Intersect(x.priv).Any()).ToList();
                    var instkeys = string.Join(",", canView.Select(p => $"'{p._id}'"));

                    string[] stagesQuery = new[]
                    {
                        "{$match: {$and: [{_t: 'EnProsesRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                        "{$project: {_id: 0}}"
                    };
                    if (!string.IsNullOrEmpty(tfind))
                    {
                        tfind = tfind.ToEscape();
                        string jenisProses = tfind.ToEnumJenisProses();
                        stagesQuery = new[]
                            {
                                "{$match: {$and: [{_t: 'EnProsesRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                                "{$unwind: '$info.details'}",
                                "{$lookup: {from: 'persils_v2', localField: 'info.details.keyPersil', foreignField: 'key', as: 'persil'}}",
                                "{$unwind: { path: '$persil', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'maps',let: { key: '$info.details.keyDesa'},
                                      pipeline:[{$unwind: '$villages'},
                                              {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                                              {$project: {key: '$villages.key', 
                                              desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                                    as:'desas'}}",
                                "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'maps',let: { key: '$persil.basic.current.keyDesa'},
                                      pipeline:[{$unwind: '$villages'},
                                              {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                                              {$project: {key: '$villages.key', 
                                              desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                                    as:'desasPersil'}}",
                                "{$unwind: { path: '$desasPersil', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from : 'bayars', let: {keyPersil: '$persil.key'},
                                                            pipeline:[
                                                                {$unwind: '$bidangs'},
                                                                {$match: {$expr: {$eq: ['$bidangs.keyPersil', '$$keyPersil']}}},
                                                                {$project: {
                                                                    _id: 0, tahap: '$nomorTahap',
                                    
                                                                }}
                                                            ], as:'bayarTahap'
                                    }}",
                               "{$unwind: { path: '$bayarTahap', preserveNullAndEmptyArrays: true}}",
                               @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$info.details.keyPtsk'}, pipeline: [
                                            {$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                                            {$project: {_id: 0, name: '$identifier'}}
                                    ], as: 'ptsk'}}",

                               "{$unwind: { path: '$ptsk', preserveNullAndEmptyArrays: true}}",
                              @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$persil.basic.current.keyPTSK'}, pipeline: 
                                    [{$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                                    {$project: {_id: 0, name: '$identifier'}}], as: 'ptskPersil'}}",
                              "{$unwind: { path: '$ptskPersil', preserveNullAndEmptyArrays: true}}",

                              "{$addFields: {tahapPersil: {$toString : '$bayarTahap.tahap'}}}",
                              "{$addFields: {tahap: {$toString : '$info.details.tahap'}}}",
                              "{$addFields: {en_jenis: {$toString : '$info.details.en_jenis'}}}",
                              "{$addFields: {en_jenisPersil: {$toString : '$persil.basic.current.en_jenis'}}}",
                              "{$addFields: {en_jenisPersilDest: {$toString : '$details.info.en_proses'}}}",

                             @$"<$match: <$or : [
                                    <'identifier': /{tfind}/i>,        
                                    <'info.details.IdBidang': /{tfind}/i>,
                                    <'info.details.alasHak': /{tfind}/i>,
                                    <'info.details.group': /{tfind}/i>,
                                    <'info.details.noPeta': /{tfind}/i>,
                                    <'desas.desaIdentity': /{tfind}/i>,
                                    <'desas.projectIdentity': /{tfind}/i>,
                                    <'desasPersil.desaIdentity': /{tfind}/i>,
                                    <'desasPersil.projectIdentity': /{tfind}/i>,
                                    <'persil.IdBidang': /{tfind}/i>,
                                    <'persil.basic.current.surat.nomor': /{tfind}/i>,
                                    <'persil.basic.current.group': /{tfind}/i>,
                                    <'persil.basic.current.noPeta': /{tfind}/i>,
                                    <'tahapPersil': /{tfind}/i>,
                                    <'tahap': /{tfind}/i>,
                                    <'en_jenis': /{jenisProses}/i>,
                                    <'en_jenisPersil': /{jenisProses}/i>,
                                    <'en_jenisPersilDest': /{jenisProses}/i>,
                                    <'ptsk.name': /{tfind}/i>,
                                    <'ptskPersil.name': /{tfind}/i>
                                ]>>".MongoJs(),
                             "{$project: {_id:0,key: 1}}",
                             "{$group: {_id:'$key'}}",
                             "{$project: {key: '$_id'}}",
                             "{$lookup: {from: 'update_request_data', localField: 'key', foreignField: 'key', as: 'urd'}}",
                             "{$unwind: '$urd'}",
                            @"{$project: {_id: 0, 
                                    _t: '$urd._t',
                                    key: '$urd.key',
                                    invalid: '$urd.invalid',
                                    identifier: '$urd.identifier',
                                    instKey: '$urd.instKey',
                                    creator: '$urd.creator',
                                    created: '$urd.created',
                                    info: '$urd.info'
                               }}"
                            };
                    };

                    List<EnProsesRequest> enprosesRequests = contextPay.GetDocuments(new EnProsesRequest(), "update_request_data", stagesQuery).ToList();

                    List<user> users = contextPay.GetDocuments(new user(), "securities", "{$match: {_t: 'user'}}", "{$project:{_id: 0}}").ToList();

                    var villages = contextPay.GetVillages();
                    var ptsk = contextPay.GetDocuments(new PTSK(), "masterdatas", "{$match: {$and: [{_t:'ptsk'}, {'invalid': {$ne:true}}]}}", "{$project: {_id: 0}}").ToList();

                    List<EnProsesApprovalView> enProsesRequest = enprosesRequests.Select(x => x.ToView(users, villages, ptsk)).ToList();

                    var xlst = ExpressionFilter.Evaluate(enProsesRequest, typeof(List<EnProsesApprovalView>), typeof(EnProsesApprovalView), gs);
                    var data = xlst.result.Cast<EnProsesApprovalView>().ToList();
                    var result = data.OrderByDescending(x => x.created).GridFeed(gs);
                    return Ok(result);
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

        private IActionResult RuningFlow(user user, UpdateRequestCommand cmd, GraphMainInstance instance, bool isApproved = false)
        {
            try
            {
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Request belum lengkap");
                if (instance.closed)
                    return new UnprocessableEntityObjectResult("Request telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == cmd.routeKey));
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow Request tidak jelas");
                var route = node.routes.First(r => r.key == cmd.routeKey);
#if test
                (var ok, var reason) = ghost.Take(user, instance.key, cmd.routeKey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, instance.key, cmd.routeKey, cmd.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#else
                (var ok, var reason) = graphhost.Take(user, instance.key, cmd.routeKey);
                if (ok)
                    (ok, reason) = graphhost.Summary(user, instance.key, cmd.routeKey, cmd.control.ToString("g"), null);
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
#endif

                var priv = route.privs.FirstOrDefault();
                string error = null;

                if (cmd.control == ToDoControl._ && cmd._t == "PersilRequest")
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.landReject1_ => (ApprovingRequest(user, cmd, cmd._t, isApproved)),
                        ToDoVerb.landReject2_ => (ApprovingRequest(user, cmd, cmd._t, isApproved)),
                        ToDoVerb.landApprove1_ => (ApprovingRequest(user, cmd, cmd._t, isApproved)),
                        ToDoVerb.landApprove2_ => (ApprovingRequest(user, cmd, cmd._t, isApproved)),
                        _ => (true, null)
                    };

                else if (cmd.control == ToDoControl._)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.confirmAbort_ => (AbortingRequest(user, cmd.reqKey, cmd._t)),
                        ToDoVerb.complish_ => (ApprovingRequest(user, cmd, cmd._t, isApproved)),
                        _ => (true, null)
                    };

#if test
                var instances = ghost.Get(instance.key).GetAwaiter().GetResult() ?? new GraphMainInstance();
#else
                var instances = graphhost.Get(instance.key) ?? new GraphMainInstance();
#endif

                var lastState = instances.lastState;

                string[] notes = (cmd.reason ?? "").Split("##>");
                cmd.reason = notes.Count() == 2 ? notes[1] : notes[0];

                LogWorkListCore logWLCore = new LogWorkListCore(instances.key, user.key, lastState.time,
                                                                 lastState.state, cmd.verb, cmd.reason);
                (ok, error) = InsertLogWorklist(logWLCore);

                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status bidang" : error);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [PersilCSMaterializer]
        private (bool, string, string) SavePersilNew(ReqPersilApprovalCore core, user user)
        {
            (bool ok, string message, string keyPersil) = (true, null, null);

            if (!ValidateProject(core.keyProject) && !ValidateDesa(core.keyDesa))
                return (false, "Invalid project / desa key given !", null);

            JenisProses pros = core.jenisProses;

            JenisAlasHak jalas = core.jenisAlasHak;

            Persil persil = (pros, jalas) switch
            {
                (JenisProses.hibah, _) => new PersilHibah(),
                (JenisProses.parent or JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.girik) => new PersilGirik(),
                (JenisProses.parent or JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.shm) => new PersilSHM(),
                (JenisProses.parent or JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.shp) => new PersilSHP(),
                (JenisProses.parent or JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.hgb) => new PersilHGB(),
                _ => null
            };

            if (persil == null)
                return (false, "Kombinasi jenis proses + jenis alas hak tidak valid", null);

            persil.key = mongospace.MongoEntity.MakeKey;
            persil.en_state = pros switch
            {
                JenisProses.batal => StatusBidang.batal,
                _ => StatusBidang.belumbebas
            };
            persil.regular = true;

            MyTracer.TraceInfo2(@$"Insert New Persil... key:{persil.key}, 
                                   en_state:{persil.en_state},
                                   project:{core.keyProject},
                                   desa:{core.keyDesa},
                                   alashak:{core.alasHak}");


            if (!string.IsNullOrEmpty(core.keyParent))
                (ok, message) = InsertEntryForKeyParent(user, core.keyParent);
            if (!ok)
                return (ok, message, persil.key);

            var persilParent = GetPersilBykey(core.keyParent);

            var entry = new PersilBasic
            {
                keyParent = core.keyParent,
                IdBidangParent = !string.IsNullOrEmpty(core.keyParent) ? persilParent.IdBidang : null,
                en_jenis = jalas,
                en_proses = pros,
                keyProject = core.keyProject,
                keyDesa = core.keyDesa,
                luasSurat = core.luas,
                noPeta = core.nomorPeta,
                pemilik = core.pemilik,
                group = core.group,
                alias = core.alias,
                surat = new landrope.mod2.AlasHak { nomor = core.alasHak }
            };

            try
            {
                persil.basic.PutEntry(entry, ChangeKind.Add, user.key);
                contextPay.persils.Insert(persil);
                contextPay.SaveChanges();
                (ok, message) = (true, "Berhasil input Persil!");
                MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);
            }
            catch (Exception ex)
            {
                contextPay.DiscardChanges();
                (ok, message) = (false, $"Gagal input Persil, {ex.Message}!");
            }
            finally
            {
            }

            return (ok, message, persil.key);
        }

        private (bool, string) SavePersilMap(user user, string keyPersil, ShapeCore[] location)
        {
            (bool ok, string message) = (false, "");
            try
            {
                var permaps = contextPay.persilmaps.FirstOrDefault(p => p.key == keyPersil);
                bool isNew = permaps == null;

                if (isNew)
                    permaps = new persilMap { key = keyPersil };

                string jsonLocation = JsonSerializer.Serialize(location);

                landrope.mod2.Map map = new landrope.mod2.Map
                {
                    ID = mongospace.MongoEntity.MakeKey,
                    Area = 0,
                    careas = Encoding.ASCII.GetBytes(jsonLocation)
                };

                var entry = new MapEntry
                {
                    kind = isNew ? ChangeKind.Add : ChangeKind.Update,
                    uploaded = DateTime.Now,
                    keyUploader = user.key,
                    map = map,
                    sourceFile = "Request By Coordinates",
                    metadata = new MapMeta { created = DateTime.Now, updated = DateTime.Now, filesize = 0, updater = user.identifier }
                };
                permaps.AddEntry(entry);

                if (isNew)
                    contextPay.persilmaps.Insert(permaps);
                else
                    contextPay.persilmaps.Update(permaps);
                contextPay.SaveChanges();


                string[] stages = new[] { $"<$match:<key:'{keyPersil}'>>".Replace("<", "{").Replace(">", "}"),
                                           "{$merge:{into:'material_persil_map',on:'key',whenMatched:'replace'}}"
                };
                var materilize = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
                var coll = contextPay.db.GetCollection<BsonDocument>("persilMapsProject");
                coll.AggregateAsync(materilize);

                (ok, message) = (true, "Success");
            }
            catch (Exception ex)
            {
                MyTracer.TraceError3($"Save Persil Map Error : {ex.Message}");
                (ok, message) = (false, ex.Message);
            }
            finally
            {

            }
            return (ok, message);
        }

        private (bool, string) AbortingRequest(user user, string reqKey, string opr)
        {
            (var ok, var error) = opr switch
            {
                "MapRequest" => Map(user, reqKey),
                "StateRequest" => State(),
                _ => Persil()
            };

            return (ok, error);

            (bool, string) Map(user user, string reqKey)
            {
                MapRequest existingReq = contextPay.mapRequest.FirstOrDefault(r => r.key == reqKey);

                existingReq.invalid = true;
                contextPay.mapRequest.Update(existingReq);
                contextPay.SaveChanges();

                Persil persil = GetPersilBykey(existingReq.info.keyPersil);

                //Update PersilMaps
                persilMap permaps = contextPay.persilmaps.FirstOrDefault(p => p.key == persil.key);
                MapEntry lastEntries = permaps.entries.LastOrDefault();
                lastEntries.approved = false;
                lastEntries.keyReviewer = user.key;
                lastEntries.reviewed = DateTime.Now;
                lastEntries.kind = ChangeKind.Delete;

                permaps.AddEntry(lastEntries);
                contextPay.persilmaps.Update(permaps);
                contextPay.SaveChanges();

                return (true, null);
            }

            (bool, string) State()
            {
                var host = HostServicesHelper.GetStateRequestHost(services);
                var state = host.GetStateRequest(reqKey) as StateRequest;

                state.invalid = true;

                host.Update(state);

                return (true, null);
            }

            (bool, string) Persil()
            {
                return (true, null);
            }
        }

        private (bool, string) ApprovingRequest(user user, UpdateRequestCommand cmd, string opr, bool isApproved = false)
        {
            var ok = false;
            string error = null;
            DateTime now = DateTime.Now;
            try
            {
                (ok, error) = opr switch
                {
                    "MapRequest" => Map(),
                    "StateRequest" => State(),
                    "ProjectRequest" => Project(),
                    "EnProsesRequest" => EnProses(),
                    "LandSplitRequest" => LandSplit(),
                    _ => Persil(),
                };

                return (ok, error);

                (bool, string) Map()
                {
                    MapRequest existingReq = contextPay.mapRequest.FirstOrDefault(r => r.key == cmd.reqKey);
                    Persil persil = GetPersilBykey(existingReq.info.keyPersil);
                    persilMap permaps = contextPay.persilmaps.FirstOrDefault(p => p.key == persil.key);

                    //Last Entries Approved
                    MapEntry lastEntries = permaps.entries.LastOrDefault();
                    lastEntries.approved = true;
                    lastEntries.keyReviewer = user.key;
                    lastEntries.reviewed = DateTime.Now;
                    lastEntries.kind = ChangeKind.Unchanged;

                    permaps.AddEntry(lastEntries);
                    //Update Current Map
                    permaps.current = lastEntries.map;
                    contextPay.persilmaps.Update(permaps);

                    //Save Changes
                    contextPay.SaveChanges();

                    return (true, null);
                }

                (bool, string) State()
                {
                    var host = HostServicesHelper.GetStateRequestHost(services);
                    var state = host.GetStateRequest(cmd.reqKey) as StateRequest;

                    (ok, error) = state.info.request switch
                    {
                        RequestState.Masuk => Masuk(),
                        RequestState.Batal => Batal(),
                        RequestState.Lanjut => Lanjutkan(),
                        RequestState.Keluar => Keluar()
                    };

                    return (ok, error);

                    (bool, string) Masuk()
                    {
                        List<Persil> listPersil = new List<Persil>();
                        List<StateHistories> history = new List<StateHistories>();
                        var lastStateHistories = StatusBidang.belumbebas;

                        foreach (var dtl in state.info.details)
                        {
                            var persil = dtl.persil(contextPay, dtl.keyPersil);
                            if (persil == null)
                                return (false, "Bidang tidak ada");

                            MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                            var currentState = persil.en_state ?? StatusBidang.belumbebas;

                            StatusBidang[] states = { StatusBidang.bebas, StatusBidang.belumbebas };

                            if (persil.statehistories != null && persil.statehistories.Count() > 0)
                            {
                                history = persil.statehistories.Where(x => states.Contains(x.en_state)).ToList();
                                lastStateHistories = history.OrderByDescending(x => x.date).FirstOrDefault().en_state;
                            }
                            else
                            {
                                lastStateHistories = StatusBidang.belumbebas;
                            }

                            var hist = new StateHistories(user)
                            {
                                en_state = currentState,
                                date = DateTime.Now
                            };

                            var lst = new List<StateHistories>();
                            if (persil.statehistories != null)
                                lst = persil.statehistories.ToList();

                            lst.Add(hist);

                            persil.en_state = lastStateHistories;
                            persil.statehistories = lst.ToArray();

                            listPersil.Add(persil);
                        }

                        contextPay.persils.Update(listPersil);
                        contextPay.SaveChanges();

                        return (true, null);
                    }

                    (bool, string) Lanjutkan()
                    {
                        List<Persil> listPersil = new List<Persil>();
                        List<StateHistories> history = new List<StateHistories>();
                        var lastStateHistories = StatusBidang.belumbebas;

                        foreach (var dtl in state.info.details)
                        {
                            var persil = dtl.persil(contextPay, dtl.keyPersil);
                            if (persil == null)
                                return (false, "Bidang tidak ada");

                            MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                            var currentState = persil.en_state ?? StatusBidang.belumbebas;

                            StatusBidang[] states = { StatusBidang.bebas, StatusBidang.belumbebas };

                            if (persil.statehistories != null && persil.statehistories.Count() > 0)
                            {
                                history = persil.statehistories.Where(x => states.Contains(x.en_state)).ToList();
                                lastStateHistories = history.OrderByDescending(x => x.date).FirstOrDefault().en_state;
                            }
                            else
                            {
                                lastStateHistories = StatusBidang.belumbebas;
                            }

                            var hist = new StateHistories(user)
                            {
                                en_state = currentState,
                                date = DateTime.Now
                            };

                            var lst = new List<StateHistories>();
                            if (persil.statehistories != null)
                                lst = persil.statehistories.ToList();

                            lst.Add(hist);

                            persil.en_state = lastStateHistories;
                            persil.statehistories = lst.ToArray();

                            listPersil.Add(persil);
                        }

                        contextPay.persils.Update(listPersil);
                        contextPay.SaveChanges();

                        return (true, null);
                    }

                    (bool, string) Batal()
                    {
                        List<Persil> listPersil = new List<Persil>();

                        foreach (var dtl in state.info.details)
                        {
                            var persil = dtl.persil(contextPay, dtl.keyPersil);
                            if (persil == null)
                                return (false, "Bidang tidak ada");

                            MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                            var currentState = persil.en_state ?? StatusBidang.bebas;

                            var newhist = new StateHistories(user)
                            {
                                en_state = currentState,
                                date = DateTime.Now
                            };

                            var lstnewhist = new List<StateHistories>();
                            if (persil.statehistories != null)
                                lstnewhist = persil.statehistories.ToList();

                            lstnewhist.Add(newhist);

                            persil.en_state = StatusBidang.batal;
                            persil.statehistories = lstnewhist.ToArray();

                            listPersil.Add(persil);
                        }

                        contextPay.persils.Update(listPersil);
                        contextPay.SaveChanges();

                        return (true, null);
                    }

                    (bool, string) Keluar()
                    {
                        List<Persil> listPersil = new List<Persil>();

                        foreach (var dtl in state.info.details)
                        {
                            var persil = dtl.persil(contextPay, dtl.keyPersil);
                            if (persil == null)
                                return (false, "Bidang tidak ada");

                            MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                            var currentState = persil.en_state ?? StatusBidang.bebas;

                            var newhist = new StateHistories(user)
                            {
                                en_state = currentState,
                                date = DateTime.Now
                            };

                            var lstnewhist = new List<StateHistories>();
                            if (persil.statehistories != null)
                                lstnewhist = persil.statehistories.ToList();

                            lstnewhist.Add(newhist);

                            persil.en_state = StatusBidang.keluar;
                            persil.statehistories = lstnewhist.ToArray();

                            listPersil.Add(persil);
                        }

                        contextPay.persils.Update(listPersil);
                        contextPay.SaveChanges();

                        return (true, null);
                    }
                }

                (bool, string) Persil()
                {
                    (ok, error) = ValidatingPersil(user, cmd, isApproved);
                    return (ok, error);
                }

                (bool, string) Project()
                {
                    var request = contextPay.projectRequests.Query(x => x.key == cmd.reqKey).FirstOrDefault();
                    var preReviewer = contextPay.GetDocuments(new { preReviewer = "", preReviewDate = DateTime.Now }, "graphables",
                        "{$match: {$and: [{'key': '" + request.instKey + "'}]}}",
                        "{$unwind: '$Core.nodes'}",
                        "{$match: {'Core.nodes._state': " + 1 + "}}",
                       @"{$project: {
                            _id: 0,
                            preReviewDate: '$Core.nodes.Taken.on',
                            preReviewer: '$Core.nodes.Taken.key'
                        }}").ToList();

                    //For Entries
                    foreach (var p in request.info.details)
                    {
                        var persil = contextPay.persils.FirstOrDefault(x => x.key == p.keyPersil);
                        var last = persil.basic.entries.LastOrDefault();

                        var item = new PersilBasic();
                        item = persil.basic.current;
                        item.keyProject = request.info.keyProject;
                        item.keyDesa = request.info.keyDesa;
                        item.keyPTSK = request.info.keyPTSK;

                        var newEntries1 =
                            new ValidatableEntry<PersilBasic>
                            {
                                //created = request.created,
                                created = now,
                                en_kind = ChangeKind.Update,
                                keyCreator = request.creator,
                                keyReviewer = preReviewer.FirstOrDefault().preReviewer,
                                reviewed = now,
                                approved = true,
                                /*//keyPreReviewer = preReviewer.FirstOrDefault().preReviewer,
                                //preReviewed = preReviewer.FirstOrDefault().preReviewDate,
                                //preApproved = true,8*/
                                item = item
                            };

                        persil.basic.entries.Add(newEntries1);

                        persil.basic.current = item;
                        contextPay.persils.Update(persil);
                        contextPay.SaveChanges();

                        if (last != null && last.reviewed == null)
                        {
                            var item2 = new PersilBasic();
                            item2 = last.item;
                            item2.keyProject = request.info.keyProject;
                            item2.keyDesa = request.info.keyDesa;
                            item2.keyPTSK = request.info.keyPTSK;

                            var newEntries2 =
                                new ValidatableEntry<PersilBasic>
                                {
                                    created = last.created,
                                    en_kind = last.en_kind,
                                    keyCreator = last.keyCreator,
                                    keyReviewer = last.keyReviewer,
                                    reviewed = last.reviewed,
                                    approved = last.approved,
                                    keyPreReviewer = last.keyPreReviewer,
                                    preReviewed = last.preReviewed,
                                    preApproved = true,
                                    item = item2
                                };

                            persil.basic.entries.Add(newEntries2);

                            contextPay.persils.Update(persil);
                            contextPay.SaveChanges();
                        }
                    }

                    return (ok, error);
                }

                (bool, string) EnProses()
                {
                    EnProsesRequest request = contextPay.enprosesRequest.FirstOrDefault(x => x.key == cmd.reqKey);
                    var preReviewer = contextPay.GetDocuments(new { preReviewer = "", preReviewDate = DateTime.Now }, "graphables",
                        "{$match: {$and: [{'key': '" + request.instKey + "'}]}}",
                        "{$unwind: '$Core.nodes'}",
                        "{$match: {'Core.nodes._state': " + 1 + "}}",
                       @"{$project: {
                            _id: 0,
                            preReviewDate: '$Core.nodes.Taken.on',
                            preReviewer: '$Core.nodes.Taken.key'
                        }}").ToList();

                    foreach (var p in request.info.details)
                    {
                        var persil = contextPay.persils.FirstOrDefault(x => x.key == p.keyPersil);
                        var last = persil.basic.entries.LastOrDefault();

                        var item = new PersilBasic();
                        item = persil.basic.current;
                        item.en_proses = request.info.en_proses;

                        var newEntries1 =
                            new ValidatableEntry<PersilBasic>
                            {
                                //created = request.created,
                                created = now,
                                en_kind = ChangeKind.Update,
                                keyCreator = request.creator,
                                keyReviewer = preReviewer.FirstOrDefault().preReviewer,
                                reviewed = now,
                                approved = true,
                                //keyPreReviewer = preReviewer.FirstOrDefault().preReviewer,
                                //preReviewed = preReviewer.FirstOrDefault().preReviewDate,
                                //preApproved = true,
                                item = item
                            };

                        persil.basic.entries.Add(newEntries1);

                        persil.basic.current = item;
                        contextPay.persils.Update(persil);
                        contextPay.SaveChanges();

                        if (last != null && last.reviewed == null)
                        {
                            var item2 = new PersilBasic();
                            item2 = last.item;
                            item2.en_proses = request.info.en_proses;

                            var newEntries2 =
                                new ValidatableEntry<PersilBasic>
                                {
                                    created = last.created,
                                    en_kind = last.en_kind,
                                    keyCreator = last.keyCreator,
                                    keyReviewer = last.keyReviewer,
                                    reviewed = last.reviewed,
                                    approved = last.approved,
                                    keyPreReviewer = last.keyPreReviewer,
                                    preReviewed = last.preReviewed,
                                    preApproved = true,
                                    item = item2
                                };

                            persil.basic.entries.Add(newEntries2);

                            contextPay.persils.Update(persil);
                            contextPay.SaveChanges();
                        }
                    }

                    return (ok, error);
                }

                (bool, string) LandSplit()
                {
                    var request = contextPay.landsplitRequests.Query(x => x.key == cmd.reqKey).FirstOrDefault();
                    var preReviewer = contextPay.GetDocuments(new { preReviewer = "", preReviewDate = DateTime.Now }, "graphables",
                        "{$match: {$and: [{'key': '" + request.instKey + "'}]}}",
                        "{$unwind: '$Core.nodes'}",
                        "{$match: {'Core.nodes._state': " + 1 + "}}",
                       @"{$project: {
                            _id: 0,
                            preReviewDate: '$Core.nodes.Taken.on',
                            preReviewer: '$Core.nodes.Taken.key'
                        }}").ToList();

                    var parentPersil = contextPay.persils.FirstOrDefault(x => x.key == request.info.keyPersil);

                    //For Entries
                    foreach (var p in request.info.details)
                    {
                        var persil = contextPay.persils.FirstOrDefault(x => x.key == p.keyPersil);
                        var last = persil.basic.entries.LastOrDefault();

                        var item = new PersilBasic();
                        item = persil.basic.current;
                        item.keyParent = request.info.keyPersil;

                        var newEntries1 =
                            new ValidatableEntry<PersilBasic>
                            {
                                created = request.created,
                                en_kind = ChangeKind.Update,
                                keyCreator = request.creator,
                                keyReviewer = preReviewer.FirstOrDefault().preReviewer,
                                reviewed = preReviewer.FirstOrDefault().preReviewDate,
                                approved = true,
                                item = item
                            };

                        persil.basic.entries.Add(newEntries1);

                        persil.basic.current = item;

                        if (parentPersil.en_state == StatusBidang.bebas)
                        {
                            persil.en_state = StatusBidang.bebas;
                        }

                        contextPay.persils.Update(persil);
                        contextPay.SaveChanges();

                        if (last != null && last.reviewed == null)
                        {
                            var item2 = new PersilBasic();
                            item2 = last.item;
                            item2.keyParent = request.info.keyPersil;

                            var newEntries2 =
                                new ValidatableEntry<PersilBasic>
                                {
                                    created = last.created,
                                    en_kind = last.en_kind,
                                    keyCreator = last.keyCreator,
                                    keyReviewer = last.keyReviewer,
                                    reviewed = last.reviewed,
                                    approved = last.approved,
                                    keyPreReviewer = last.keyPreReviewer,
                                    preReviewed = last.preReviewed,
                                    preApproved = true,
                                    item = item2
                                };

                            persil.basic.entries.Add(newEntries2);

                            contextPay.persils.Update(persil);
                            contextPay.SaveChanges();
                        }
                    }

                    return (true, error);
                }

            }
            catch (Exception ex)
            {
                (ok, error) = (false, ex.Message);
            }
            finally
            {
            }
            return (ok, error);
        }

        private (bool, string) InsertLogWorklist(LogWorkListCore core)
        {
            try
            {
                LogWorklist existingLog = contextPay.logWorklist.FirstOrDefault(r => r.key == core.instKey);

                bool isNew = existingLog == null;
                if (isNew)
                    existingLog = new LogWorklist(core.instKey);

                LogWorklistEntry entry = new LogWorklistEntry()
                {
                    keyCreator = core.keyCreator,
                    created = core.created,
                    state = core.state,
                    verb = core.verb,
                    reason = core.reason
                };

                existingLog.AddLogEntry(entry);

                if (isNew)
                    contextPay.logWorklist.Insert(existingLog);
                else
                    contextPay.logWorklist.Update(existingLog);

                contextPay.SaveChanges();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private MapRequest RequestByIdBidang(string idBidang)
        {
            return contextPay.mapRequest.FirstOrDefault(mR => mR.info.idBidang == idBidang) ?? new MapRequest();
        }

        private MapRequest RequestByKey(string key)
        {
            return contextPay.mapRequest.FirstOrDefault(mR => mR.key == key);
        }

        private List<user> GetListUser()
        {
            List<user> listUser = contextPay.GetDocuments(new user(), "securities",
                "{$match: {$and: [{'_t': 'user'}, {'invalid' : {$ne: true}}]}}").ToList();

            return listUser;
        }

        private List<user> GetListUser(string[] creators)
        {
            List<user> listUser = contextPlus.users.Query(x => creators.Contains(x.key)).ToList();

            return listUser;
        }

        private Persil GetPersilByIdBidang(string IdBidang)
        {
            return contextPay.persils.FirstOrDefault(x => x.invalid != true && x.IdBidang == IdBidang);
        }

        private Persil GetPersilBykey(string keyPersil)
        {
            return contextPay.persils.FirstOrDefault(x => x.invalid != true && x.key == keyPersil);
        }

        private List<Persil> GetPersilsByBidangs(List<string> listBidang)
        {
            if (listBidang.Count == 0)
                return new List<Persil>();
            return contextPay.persils.All().Where(x => x.invalid != true &&
                                  listBidang.Contains(x.IdBidang)).ToList();
        }

        private bool ValidateProject(string keyProject)
        {
            return contextPay.GetVillages().Any(v => v.project.key == keyProject);
        }

        private bool ValidateDesa(string keyDesa)
        {
            return contextPay.GetVillages().Any(v => v.desa.key == keyDesa);
        }

        private List<Persil> GetPersilByKeys(string[] keyPersils)
        {
            return contextPay.persils.All().Where(p => p.invalid != true && keyPersils.Contains(p.key)).ToList();
        }

        private List<Persil> GetPersilByKeysString(string keyPersils)
        {
            return contextPay.GetDocuments(new Persil(), "persils_v2",
                "{$match : {$and : [{'invalid' : {$ne : true}}, {'key' : {$in : [ " + keyPersils + " ]}} ]}}",
                "{$project : {_id : 0}}").ToList();
        }

        (IEnumerable<PersilCore> data, int count) CollectPersil(string token,
                        IEnumerable<string> prestages,
                        IEnumerable<string> poststages,
                        IEnumerable<string> sortstages,
                        int skip, int limit)
        {
            var allstages = prestages./*Union(aggs).*/Union(poststages).ToArray();

            var countstage = "{$count:'count'}";
            var totstages = allstages.Union(new[] { countstage }).ToArray();

            var limstages = new List<string>();
            if (skip > 0)
                limstages.Add($"{{$skip:{skip}}}");
            if (limit > 0)
                limstages.Add($"{{$limit:{limit}}}");
            limstages.Add("{$project:{regular:0,en_state:0}}");

            allstages = allstages.Union(sortstages).Union(limstages)
                .Union(new[] { "{$project:{_id:0}}" })
                .ToArray();

            var countpipe = PipelineDefinition<BsonDocument, BsonDocument>.Create(totstages);
            var resultpipe = PipelineDefinition<BsonDocument, PersilCore>.Create(allstages);

            int count = 0;
            List<PersilCore> result = new List<PersilCore>();

            var coll = contextPay.db.GetCollection<BsonDocument>("material_persil_core");
            //var coll2 = contextex.db.GetCollection<BsonDocument>("persil_core_2");
            var task1 = Task.Run(() =>
            {
                var xcounter = coll.Aggregate<BsonDocument>(countpipe);
                var counter = xcounter.FirstOrDefault();
                if (counter != null)
                    count = counter.Names.Any(s => s == "count") ? counter.GetValue("count").AsInt32 : 0;
            });
            var task2 = Task.Run(() =>
            {
                var res = coll.Aggregate<PersilCore>(resultpipe);
                result = res.ToList();
            });
            Task.WaitAll(task1, task2);
            return (result, count);// (int)Math.Truncate(count.count));
        }

        private (PropertyInfo pinfo, object shell) GetShell(Persil data, string part = "basic")
        {
            if (data == null || string.IsNullOrEmpty(part))
                return (null, null);
            var type = data.GetType();
            var props = type.GetProperties();
            var propns = props.Select(p => (p, a: p.GetCustomAttribute(typeof(StepAttribute))))
                                    .Where(p => p.a != null)
                                    .Select(x => (x.p, an: ((StepAttribute)x.a).name))
                                    .ToList();
            var prop = propns.FirstOrDefault(p => p.an == part).p;
            if (prop == null)
                return (null, null);

            var shell = prop.GetValue(data);
            return (prop, shell);
        }

        [PersilMaterializer(Auto = false)]
        private (bool, string) ValidatingPersil(user user, UpdateRequestCommand cmd, bool isApproved)
        {
            (bool ok, string message) = (true, null);
            try
            {
                PersilRequest request = contextPay.persilApproval.FirstOrDefault(p => p.key == cmd.reqKey);

                if (request == null)
                    return (false, "request tidak ditemukan !");

                Persil persil = GetPersilBykey(request.info.keyPersil);

                if (persil == null)
                    return (false, "Bidang tidak ditemukan !");

                var xshell = GetShell(persil);
                var shell = (ValidatableShell)xshell.shell;

                if (shell == null)
                    return (false, "basic has no entry");
                if (cmd.verb == ToDoVerb.landReject1_ || cmd.verb == ToDoVerb.landApprove1_)
                    shell.PreValidate(user?.key ?? "", isApproved, cmd.reason);
                else
                {
                    shell.NewValidate(user?.key ?? "", isApproved, cmd.reason);
                    if (isApproved)
                    {
                        contextPay.persils.Update(persil);
                        contextPay.SaveChanges();
                        var LastEntries = persil.basic.entries.Where(p => p.approved == true).LastOrDefault();
                        string keyParent = LastEntries.item.keyParent;

                        if (!string.IsNullOrEmpty(keyParent))
                        {
                            Persil persilParent = GetPersilBykey(keyParent);
                            bool isAlreadyParent = persilParent.basic.current.en_proses == JenisProses.parent;
                            if (!isAlreadyParent)
                            {
                                var xshellParent = GetShell(persilParent);
                                var shellParent = (ValidatableShell)xshellParent.shell;
                                shellParent.NewValidate(user?.key ?? "", isApproved, "");
                                contextPay.persils.Update(persilParent);
                                contextPay.SaveChanges();
                            }
                        }
                    }
                }

                contextPay.db.GetCollection<Persil>("persils_v2").ReplaceOne($"{{key:'{persil.key}'}}", persil);
                MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                (ok, message) = (true, "Persil Validated");
            }
            catch (Exception ex)
            {
                (ok, message) = (false, ex.Message);
            }

            return (ok, message);
        }

        private List<LogWorklist> GetListHistoryWorklist(string instKeys)
        {
            return contextPay.GetDocuments(new LogWorklist(), "logWorklist",
                "{$match : {'key': {$in :[ " + instKeys + " ]}}}").ToList();
        }

        private bool ValidatePersilRequest(string keyPersil)
        {
            PersilRequest existing = contextPay.persilApproval.FirstOrDefault(x => x.info.keyPersil == keyPersil);
            if (existing == null)
                return true;
            ToDoState lastState = existing.instance.lastState?.state ?? ToDoState.unknown_;
            if (lastState != ToDoState.complished_)
                return false;
            return true;
        }

        private (bool, string) InsertEntryForKeyParent(user user, string keyParent, string keyChild = null)
        {
            try
            {
                var persilParent = GetPersilBykey(keyParent);
                var persilChild = GetPersilBykey(keyChild);
                var lastEntries = keyChild == null ? null : persilChild.basic.entries.LastOrDefault()?.item;
                bool isParentChange = keyChild == null ? false : !string.IsNullOrEmpty(lastEntries.keyParent) && lastEntries.keyParent != keyParent;

                if (persilParent.basic.current.en_proses == JenisProses.parent && !isParentChange)
                    return (true, "Bidang Sudah Parent");
                else
                {
                    if (isParentChange && keyChild != null)
                    {
                        var oldPersilParent = GetPersilBykey(lastEntries.keyParent);
                        bool isAlreadyParent = oldPersilParent.basic.current.en_proses == JenisProses.parent;
                        if (!isAlreadyParent)
                        {
                            (bool ok, string message) = ValidateOldParentAlreadyUse(lastEntries.keyParent, keyChild, user);
                            if (!ok)
                            {
                                return (ok, message);
                            }
                        }
                    }

                    if (persilParent.basic.current.en_proses != JenisProses.parent)
                    {
                        var lastEntriesParent = persilParent.basic.entries.LastOrDefault()?.item;
                        lastEntriesParent.en_proses = JenisProses.parent;
                        persilParent.basic.PutEntry(lastEntriesParent, ChangeKind.Update, user.key);
                        contextPay.persils.Update(persilParent);
                        contextPay.SaveChanges();
                    }
                    return (true, "Success");
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private (bool, string) ValidateOldParentAlreadyUse(string keyParent, string keyChild, user user)
        {
            try
            {
                var persil = contextPay.persils.All().Where(p => p.basic.entries.LastOrDefault().item.keyParent == keyParent);
                if (persil.Where(p => p.key != keyChild).ToList().Count() == 0)
                {
                    var persilParent = GetPersilBykey(keyParent);
                    var lastApprovedEntry = persilParent.basic.entries.LastOrDefault(e => e.approved == true);
                    persilParent.basic.entries.Add(lastApprovedEntry);
                    contextPay.persils.Update(persilParent);
                    contextPay.SaveChanges();
                }
                return (true, "Successfuly Rollback Parent Candidate");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private (bool, string) CheckExistsRequest(List<string> listPersil)
        {
            (bool ok, string message) = (true, null);
            try
            {
                string keyPersils = string.Join(",", listPersil.Select(x => $"'{x}'"));
                var existingRequest = contextPay.GetDocuments(new { noRequest = "", _t = "", IdBidang = "" }, "update_request_data",
                    "{$match: {'_t': {$in: ['EnProsesRequest', 'StateRequest', 'ProjectRequest']}}}",
                    "{$lookup: {from: 'graphables', localField: 'instKey', foreignField: 'key', as: 'graphs'}}",
                    "{$unwind: '$info.details'}",
                    "{$project: {_id: 0, noRequest: '$identifier', _t: '$_t', keyPersil: '$info.details.keyPersil', close : '$graphs.closed'}}",
                   @"{$unionWith: {
                        coll: 'update_request_data',
                        pipeline: [
                                {$match: {'_t': 'PersilRequest'}},
                                {$lookup: {from: 'graphables', localField: 'instKey', foreignField: 'key', as: 'graphs'}},
                                {$project: {_id: 0, noRequest:'$identifier', _t: '$_t', keyPersil: '$info.keyPersil', close : '$graphs.closed'}}
                            ]
                    }}",
                    "{$match: {'keyPersil': {$in: [" + keyPersils + "]}, 'close':false}}",
                    "{$lookup: {from: 'persils_v2', localField: 'keyPersil', foreignField: 'key', as: 'persil' }}",
                    "{$unwind: { path: '$persil', preserveNullAndEmptyArrays: true }}",
                    "{$project: {_id: 0, noRequest: '$noRequest', _t: '$_t',IdBidang: '$persil.IdBidang'}}"
                    ).ToList();

                if (existingRequest.Count() != 0)
                {
                    (ok, message) = (false, "Mohon selesaikan Request untuk bidang " + string.Join(", ", existingRequest.Select(x => $" {x.IdBidang} di Nomor Request {x.noRequest} ")) + " terlebih dahulu !");
                }
                return (ok, message);
            }
            catch (Exception ex)
            {
                (ok, message) = (false, ex.Message);
                return (ok, message);
            }
        }

        #endregion

        #region PROJECT REQUEST
        [HttpGet("project/list")]
        public IActionResult ListProjectRequest(string token, [FromQuery] AgGridSettings gs, string tfind)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var privileges = contextPay.GetCollections(new { _id = "", priv = new[] { "" } }, "WF_PRIVS", "{}", "{}").ToList();

                var canView = privileges.Where(x => privs.Intersect(x.priv).Any()).ToList();
                var instkeys = string.Join(",", canView.Select(p => $"'{p._id}'"));


                string[] stagesQuery = new[]
                    {
                        "{$match: {$and: [{_t: 'ProjectRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                        "{$project: {_id: 0}}"
                    };

                if (!string.IsNullOrEmpty(tfind))
                {
                    tfind = tfind.ToEscape();
                    stagesQuery = new[]
                        {
                                "{$match: {$and: [{_t: 'ProjectRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                                "{$unwind: '$info.details'}",
                                "{$lookup: {from: 'persils_v2', localField: 'info.details.keyPersil', foreignField: 'key', as: 'persil'}}",
                                "{$unwind: { path: '$persil', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'maps',let: { key: '$info.keyDesa'},
                                        pipeline:[{$unwind: '$villages'},
                                  {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                                  {$project: {key: '$villages.key', 
                                  desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                                    as:'desas'}}",
                               "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'maps',let: { key: '$info.details.keyDesa'},
                                        pipeline:[{$unwind: '$villages'},
                                  {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                                  {$project: {key: '$villages.key', 
                                  desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                                    as:'desasDetail'}}",
                                "{$unwind: { path: '$desasDetail', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'maps',let: { key: '$persil.basic.current.keyDesa'},
                                        pipeline:[{$unwind: '$villages'},
                                              {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                                              {$project: {key: '$villages.key', 
                                              desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                                    as:'desasPersil'}}",
                                "{$unwind: { path: '$desasPersil', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from : 'bayars', let: {keyPersil: '$persil.key'},
                                                            pipeline:[
                                                                {$unwind: '$bidangs'},
                                                                {$match: {$expr: {$eq: ['$bidangs.keyPersil', '$$keyPersil']}}},
                                                                {$project: {
                                                                    _id: 0, tahap: '$nomorTahap',
                                    
                                                                }}
                                                            ], as:'bayarTahap'
                                    }}",
                               "{$unwind: { path: '$bayarTahap', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$info.keyPTSK'}, pipeline: [
                                            {$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                                            {$project: {_id: 0, name: '$identifier'}}
                                    ], as: 'ptsk'}}",
                               "{$unwind: { path: '$ptsk', preserveNullAndEmptyArrays: true}}",

                               @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$info.details.keyPtsk'}, pipeline: [
                                            {$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                                            {$project: {_id: 0, name: '$identifier'}}
                                    ], as: 'ptskDetail'}}",
                               "{$unwind: { path: '$ptskDetail', preserveNullAndEmptyArrays: true}}",

                                @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$persil.basic.current.keyPTSK'}, pipeline: 
                                    [{$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                                    {$project: {_id: 0, name: '$identifier'}}], as: 'ptskPersil'}}",
                                "{$unwind: { path: '$ptskPersil', preserveNullAndEmptyArrays: true}}",

                              "{$addFields: {tahapPersil: {$toString : '$bayarTahap.tahap'}}}",
                              "{$addFields: {tahap: {$toString : '$info.details.tahap'}}}",

                             @$"<$match: <$or : [
                                    <'identifier': /{tfind}/i>,        
                                    <'info.details.IdBidang': /{tfind}/i>,
                                    <'info.details.alasHak': /{tfind}/i>,
                                    <'info.details.group': /{tfind}/i>,
                                    <'info.details.noPeta': /{tfind}/i>,
                                    <'desas.desaIdentity': /{tfind}/i>,
                                    <'desas.projectIdentity': /{tfind}/i>,
                                    <'desasDetail.desaIdentity': /{tfind}/i>,
                                    <'desasDetail.projectIdentity': /{tfind}/i>,
                                    <'desasPersil.desaIdentity': /{tfind}/i>,
                                    <'desasPersil.projectIdentity': /{tfind}/i>,
                                    <'persil.IdBidang': /{tfind}/i>,
                                    <'persil.basic.current.surat.nomor': /{tfind}/i>,
                                    <'persil.basic.current.group': /{tfind}/i>,
                                    <'persil.basic.current.noPeta': /{tfind}/i>,
                                    <'tahapPersil': /{tfind}/i>,
                                    <'tahap': /{tfind}/i>,
                                    <'ptsk.name': /{tfind}/i>,
                                    <'ptskDetail.name': /{tfind}/i>
                                    <'ptskPersil.name': /{tfind}/i>
                                ]>>".MongoJs(),
                             "{$project: {_id:0,key: 1}}",
                             "{$group: {_id:'$key'}}",
                             "{$project: {key: '$_id'}}",
                             "{$lookup: {from: 'update_request_data', localField: 'key', foreignField: 'key', as: 'urd'}}",
                             "{$unwind: '$urd'}",
                            @"{$project: {_id: 0, 
                                    _t: '$urd._t',
                                    key: '$urd.key',
                                    invalid: '$urd.invalid',
                                    identifier: '$urd.identifier',
                                    instKey: '$urd.instKey',
                                    creator: '$urd.creator',
                                    created: '$urd.created',
                                    info: '$urd.info'
                               }}"
                            };
                };

                var requests = contextPay.GetDocuments(new ProjectRequest(), "update_request_data", stagesQuery).ToList();

                var locations = contextPay.GetVillages().ToArray();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var keycreators = requests.Select(x => x.creator).Distinct();
                var creators = string.Join(',', keycreators.Select(k => $"'{k}'"));

                var users = contextPay.GetCollections(new user(), "securities", $"<key: <$in:[{creators}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var view = requests.Select(x => x.ToView(contextPay, locations, ptsks)).ToArray();

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<ProjectRequestView>), typeof(ProjectRequestView), gs);
                var data = xlst.result.Cast<ProjectRequestView>().OrderByDescending(x => x.created).ToArray();

                string prepregx(string val)
                {
                    //var st = val;//.Substring(1,val.Length-2); // remove the string marker
                    var st = val.Replace(@"\", @"\\");
                    st = new Regex(@"([\(\)\{\}\[\]\.\,\+\?\|\^\$\/])").Replace(st, @"\$1");
                    st = new Regex(@"\s+").Replace(st, @"\s+");
                    return st;
                }

                return Ok(data.GridFeed(gs));
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

        [HttpGet("project/list/detail")]
        public IActionResult ListProjectRequestDetail([FromQuery] string token, string key)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextPay.FindUser(token);
                var request = contextPay.projectRequests.Query(x => x.key == key).FirstOrDefault();

                if (request == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");
                if (request.info.details == null)
                    request.info.details = new ProjectRequestDetail[0];

                var locations = contextPay.GetVillages().ToArray();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var keys = string.Join(',', request.info.details.Select(k => $"'{k.keyPersil}'"));
                var persils = contextPay.GetCollections(new Persil(), "persils_v2", $"<key : <$in: [{keys}]>>".MongoJs(), "{_id:0}").ToList();

                var view = request.info.details.Select(x => x.ToView(persils.ToArray(), locations, ptsks)).ToArray();

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

        [HttpPost("project/save")]
        public IActionResult SaveProjectRequest([FromQuery] string token, [FromBody] ProjectApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var creator = user.privileges.Select(p => p.identifier).Where(x => x.Contains("CREATE_PROJECT")).Any();
                if (!creator)
                    return new UnprocessableEntityObjectResult("Anda tidak memiliki hak menambahkan request");

                (bool ok, string message) = CheckExistsRequest(core.keyPersils.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var keys = string.Join(',', core.keyPersils.Select(k => $"'{k}'"));
                var persils = contextPay.GetCollections(new Persil(), "persils_v2", $"<key : <$in: [{keys}]>>".MongoJs(), "{_id:0}").ToList();
                var tahaps = contextPay.GetDocuments(new { key = "", nomorTahap = 0 }, "bayars",
                    "{$unwind: '$bidangs'}",
                    $@"<$match:<'bidangs.keyPersil' : <$in: [{keys}]>>>".MongoJs(),
                    @"{$project:{
                        key : '$bidangs.keyPersil',
                        nomorTahap : '$nomorTahap',
                        _id:0
                    }}").Select(x => (x.key, x.nomorTahap)).ToArray();

                var ent = new ProjectRequest(user, core, persils.ToArray(), tahaps);
                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "ProjectApproval");

                ent.identifier = dno.Generate(DateTime.Now, true, string.Empty);
                ent.remark = core.remark;

                contextPay.projectRequests.Insert(ent);
                contextPay.SaveChanges();

                return new JsonResult(ent.key);
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

        [HttpPost("project/edit/detail")]
        public IActionResult EditProjectRequestDetail([FromQuery] string token, string key, [FromBody] ProjectRequestCommad cmd)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var request = contextPay.projectRequests.Query(x => x.key == key).FirstOrDefault();

                if (request == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                var existsDtls = request.info.details.Select(x => x.keyPersil).ToList();
                var newDtls = cmd.keyPersils.Select(x => x).Where(x => !existsDtls.Contains(x)).ToList();

                (bool ok, string message) = CheckExistsRequest(newDtls.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var keys = string.Join(',', cmd.keyPersils.Select(k => $"'{k}'"));
                var persils = contextPay.GetCollections(new Persil(), "persils_v2", $"<key : <$in: [{keys}]>>".MongoJs(), "{_id:0}").ToList();
                var tahaps = contextPay.GetDocuments(new { key = "", nomorTahap = 0 }, "bayars",
                    "{$unwind: '$bidangs'}",
                    $@"<$match:<'bidangs.keyPersil' : <$in: [{keys}]>>>".MongoJs(),
                    @"{$project:{key : '$bidangs.keyPersil',nomorTahap : '$nomorTahap',_id:0}}").Select(x => (x.key, x.nomorTahap)).ToArray();

                var list = new List<ProjectRequestDetail>();
                foreach (var k in cmd.keyPersils)
                {
                    var persil = persils.FirstOrDefault(x => x.key == k);
                    var nomorTahap = tahaps.FirstOrDefault(x => x.key == k).nomorTahap;
                    var detail = new ProjectRequestDetail(persil, nomorTahap);
                    list.Add(detail);
                }

                request.EditDetail(cmd, list.ToArray());

                contextPay.projectRequests.Update(request);
                contextPay.SaveChanges();

                RuningFlow(user, cmd, request.instance);

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

        //[HttpGet("project/persil/list")]
        //public IActionResult ListPersilForProjectRequest(string token, TypeState type, [FromQuery] AgGridSettings gs)
        //{
        //    try
        //    {
        //        var user = contextPay.FindUser(token);
        //        var tm0 = DateTime.Now;

        //        var types = type switch
        //        {
        //            TypeState.bebas => new object[] { null, 0 },
        //            TypeState.belumbebas => new object[] { 1 }
        //        };

        //        var state = string.Join(',', types.Select(k => $"{(k == null ? "null" : k)}"));

        //        //var StillOn = contextPay.GetDocuments(new { keyPersil = "", lastState = 0 }, "update_request_data",
        //        //    "{$match: {_t:'StateRequest', 'info.details': {$ne : []}}}",
        //        //    @"{$lookup: {
        //        //           from: 'graphables',
        //        //           localField: 'instKey',
        //        //           foreignField: 'key',
        //        //           as: 'graph'
        //        //         }}",
        //        //    "{$unwind : '$info.details'}",
        //        //    "{$addFields : {keyPersil : '$info.details.keyPersil'}}",
        //        //    @" {$project : {
        //        //            _id : 0,
        //        //            keyPersil : 1,
        //        //            lastState : {$arrayElemAt : ['$graph.lastState.state', 0]}
        //        //        }}",
        //        //    "{$match: {lastState : {$ne:20}}}").ToList();

        //        //var keys = string.Join(',', StillOn.Select(k => $"'{k.keyPersil}'"));

        //        var persils = contextPay.GetDocuments(new PersilProject(), "persils_v2",
        //            $@"<$match:<$and: [<'basic.current.keyParent':null>,<en_state:<$in:[{state}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".MongoJs(),
        //            @"{$lookup:{from:'maps',let:{ key: '$basic.current.keyDesa'},
        //                        pipeline:[
        //                                    {$unwind: '$villages'},
        //                                    {$match: {$expr: {$eq:['$villages.key','$$key']}}},
        //                                    {$project: {key: '$villages.key', identity: '$villages.identity'}}], as:'desas'}}",
        //            @"{$lookup: {
        //                            from: 'masterdatas',
        //                            let:{key : '$basic.current.keyPTSK'},
        //                            pipeline:[
        //                                        {$match:{$expr: {
        //                                                            $and: [{$eq : ['$key', '$$key']}, 
        //                                                                    {$eq : ['$_t', 'ptsk']}]}}}, 
        //                            {$project:{key : '$key', identity: '$identifier'}}], as:'ptsk'
        //            }}",
        //            @"{$lookup:{from:'maps', localField:'basic.current.keyProject',foreignField:'key',as:'projects'}}",
        //            @"{$project:{key:'$key',IdBidang: '$IdBidang',en_state: '$en_state',
        //            alasHak : '$basic.current.surat.nomor',
        //            noPeta : '$basic.current.noPeta',
        //            desa : {$arrayElemAt:['$desas.identity', -1]},
        //            project: {$arrayElemAt:['$projects.identity',-1]},
        //            ptsk : {$arrayElemAt: ['$ptsk.identity',0]},
        //            luasSurat: '$basic.current.luasSurat',
        //            group: '$basic.current.group',
        //            pemilik: '$basic.current.pemilik',
        //            _id: 0}}").ToList();

        //        var xlst = ExpressionFilter.Evaluate(persils, typeof(List<PersilProject>), typeof(PersilProject), gs);
        //        var data = xlst.result.Cast<PersilProject>().ToArray();

        //        return Ok(data.GridFeed(gs));
        //    }
        //    catch (UnauthorizedAccessException exa)
        //    {
        //        return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //    }
        //    catch (Exception ex)
        //    {
        //        MyTracer.TraceError2(ex);
        //        return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
        //    }
        //}

        [HttpGet("project/persil/list")]
        public IActionResult CollectPersils(string token, TypeState type, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var tm0 = DateTime.Now;

                var types = type switch
                {
                    TypeState.bebas => new object[] { null, 0 },
                    TypeState.belumbebas => new object[] { 1, 3 }
                };

                var state = string.Join(',', types.Select(k => $"{(k == null ? "null" : k)}"));

                #region still on
                //var StillOn = contextPay.GetDocuments(new { keyPersil = "", lastState = 0 }, "update_request_data",
                //        //    "{$match: {_t:'StateRequest', 'info.details': {$ne : []}}}",
                //        //    @"{$lookup: {
                //        //           from: 'graphables',
                //        //           localField: 'instKey',
                //        //           foreignField: 'key',
                //        //           as: 'graph'
                //        //         }}",
                //        //    "{$unwind : '$info.details'}",
                //        //    "{$addFields : {keyPersil : '$info.details.keyPersil'}}",
                //        //    @" {$project : {
                //        //            _id : 0,
                //        //            keyPersil : 1,
                //        //            lastState : {$arrayElemAt : ['$graph.lastState.state', 0]}
                //        //        }}",
                //        //    "{$match: {lastState : {$ne:20}}}").ToList();

                //        //var keys = string.Join(',', StillOn.Select(k => $"'{k.keyPersil}'"));
                #endregion

                var persils = contextPay.GetDocuments(new PersilProjectExt(), "persils_v2",
                    $@"<$match:<$and: [<'basic.current.keyParent':null>,<en_state:<$in:[{state}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".MongoJs(),
                    @"{
                        '$project': {
                        'key': '$key',
                        'IdBidang': '$IdBidang',
                        'en_state': '$en_state',
                        'alasHak': '$basic.current.surat.nomor',
                        'noPeta': '$basic.current.noPeta',
                        'keyDesa': '$basic.current.keyDesa',
                        'keyProject': '$basic.current.keyProject',
                        'keyPTSK': '$basic.current.keyPTSK',
                        'luasSurat': '$basic.current.luasSurat',
                        'group': '$basic.current.group',
                        'pemilik': '$basic.current.pemilik',
                        '_id': 0
                    }
                }").ToList();

                var locations = contextPay.GetVillages().ToList();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var ndata = persils.GroupJoin(locations, d => d.keyDesa, p => p.desa?.key, (d, p) => d.setLocation(p.FirstOrDefault().desa?.identity, p.FirstOrDefault().project?.identity)).ToArray();
                var nxdata = ndata.GroupJoin(ptsks, d => d.keyPTSK, p => p.key, (d, p) => d.setPT(p.FirstOrDefault()?.name)).ToArray();

                var xlst = ExpressionFilter.Evaluate(nxdata, typeof(List<PersilProject>), typeof(PersilProject), gs);
                var data = xlst.result.Cast<PersilProject>().ToArray();

                return Ok(data.GridFeed(gs));
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

        #endregion
        private List<StateHistoryView> GetLogStates(string keyRequest)
        {
            StateLogger stateLogger = contextPay.GetDocuments(new StateLogger(), "update_request_data",
                    $"<$match: <'key': '{keyRequest}'>>".Replace("<", "{").Replace(">", "}"),
                    @"{$project : {
                        _id : 0,
                        key : 1,
                        instKey: 1
                     }}"
                ).FirstOrDefault();

            if (stateLogger == null)
                return new List<StateHistoryView>();

            var logWork = contextPay.logWorklist.All().FirstOrDefault(m => m.key == stateLogger.instKey);

            // users list
            var keyCreator = logWork?.entries.Select(x => x.keyCreator).ToArray();
            var securities = keyCreator == null ? GetListUser() : GetListUser(keyCreator);

            var mainGraph = contextPay.GetDocuments(new GraphMainInstance(), "graphables",
                $"<$match: <'key': '{stateLogger.instKey}'>>".Replace("<", "{").Replace(">", "}"),
                @"{$project : { _id : 0 }}"
            ).FirstOrDefault();

            if (mainGraph != null && mainGraph.Core != null)
            {
                LogWorklistEntry[] entries = logWork?.entries ?? new LogWorklistEntry[0];

                var states = mainGraph.states;
                var statusNodes = mainGraph.Core.nodes.OfType<GraphNode>().Select(x => new { state = x._state, status = x.status }).ToArray();
                var graphEnd = mainGraph.Core.nodes.OfType<GraphEnd>().Select(x => new { state = x._state, status = x.status }).ToArray();

                StateHistoryView[] stateHistoryViews = entries.Select(s =>
                {
                    return new StateHistoryView()
                    {
                        state = s.state == ToDoState.complished_ ? graphEnd.FirstOrDefault(x => x.state == s.state)?.status : statusNodes.FirstOrDefault(x => x.state == s.state)?.status,
                        created = s.created.ToLocalTime(),
                        creator = securities.FirstOrDefault(f => f.key == s?.keyCreator)?.FullName,
                        verb = ((ToDoVerb)(s?.verb ?? ToDoVerb.unknown_)).Title().Split('|').FirstOrDefault(),
                        reason = s?.reason
                    };

                }).ToArray();

                return stateHistoryViews.ToList();

            }

            return new List<StateHistoryView>();
        }

        #region EN_PROSES

        [HttpPost("enproses/save")]
        public IActionResult SaveRequestEnProses([FromQuery] string token, EnProsesApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var creator = user.privileges.Select(p => p.identifier).Where(x => x.Contains("CREATE_PROSES")).Any();
                if (!creator)
                    return new UnprocessableEntityObjectResult("Anda tidak memiliki hak menambahkan request");

                (bool ok, string message) = CheckExistsRequest(core.keyPersils.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var corePersils = string.Join(", ", core.keyPersils.Select(x => $"'{x}'"));

                var persils = contextPay.GetDocuments(new Persil(), "persils_v2",
                    "{$match: {key: {$in: [" + corePersils + "]}}}",
                    "{$project: {_id: 0}}").ToList();

                var tahapBayar = contextPay.GetDocuments(new { tahap = (int?)0, keyPersil = "" }, "bayars", "{$unwind: '$bidangs'}",
                @"{$project: {
                                    _id:0,
                                    tahap: '$nomorTahap',
                                    keyPersil: '$bidangs.keyPersil'
                                }}"
                ).Select(x => (x.tahap, x.keyPersil)).ToList();

                EnProsesRequest ent = new(user, core, persils, tahapBayar);

                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "EnProsesApproval");
                ent.identifier = dno.Generate(DateTime.Now, true, string.Empty);
                ent.remark = core.remark;

                contextPay.enprosesRequest.Insert(ent);

                var lastState = contextPlus.GetDocuments(new { time = DateTime.Now, state = (ToDoState?)0 }, "graphables",
                    "{$match: {key: '" + ent.instKey + "'}}",
                   @"{$project: {
                        _id: 0,
                        time: '$lastState.time',
                        state: '$lastState.state'
                    }}").FirstOrDefault();

                LogWorkListCore logWLCore = new LogWorkListCore(ent.instKey, user.key, (ent.key != null ? DateTime.Now : lastState.time),
                                 lastState?.state ?? ToDoState.created_, ToDoVerb.create_, null);
                MyTracer.TraceInfo2("Insert Log WL");
                (ok, message) = InsertLogWorklist(logWLCore);
                MyTracer.TraceInfo2("Done Insert Log WL");
                if (!ok)
                {
                    MyTracer.TraceError3($"Error Add Log Worklist : {message}. harap hubungi support !");
                    return new UnprocessableEntityObjectResult($"Error Add Log Worklist : {message}. harap hubungi support !");
                }

                return Ok(ent.key);
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


        [HttpPost("enproses/edit")]
        public IActionResult EditRequestEnProses([FromQuery] string token, EnProsesApprovalCoreExt core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var ent = contextPay.enprosesRequest.FirstOrDefault(x => x.key == core.key);

                if (ent == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                var existsDtls = ent.info.details.Select(x => x.keyPersil).ToList();
                var newDtls = core.keyPersils.Select(x => x).Where(x => !existsDtls.Contains(x)).ToList();

                (bool ok, string message) = CheckExistsRequest(newDtls.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var lastState = contextPlus.GetDocuments(new { time = DateTime.Now, state = (ToDoState?)0 }, "graphables",
                   "{$match: {key: '" + ent.instKey + "'}}",
                  @"{$project: {
                        _id: 0,
                        time: '$lastState.time',
                        state: '$lastState.state'
                    }}").FirstOrDefault();
                if (lastState?.state != ToDoState.created_ && lastState.state != ToDoState.rejected_)
                    return new UnprocessableEntityObjectResult("Revisi Hanya bisa dilakukan apabila pengajuan di tolak !");

                var corePersils = string.Join(", ", core.keyPersils.Select(x => $"'{x}'"));
                var persils = contextPay.GetDocuments(new Persil(), "persils_v2",
                    "{$match: {key: {$in: [" + corePersils + "]}}}",
                    "{$project: {_id: 0}}").ToList();

                var tahapBayar = contextPay.GetDocuments(new { tahap = (int?)0, keyPersil = "" }, "bayars", "{$unwind: '$bidangs'}",
                @"{$project: {
                                    _id:0,
                                    tahap: '$nomorTahap',
                                    keyPersil: '$bidangs.keyPersil'
                                }}"
                ).Select(x => (x.tahap, x.keyPersil)).ToList();
                ent.remark = core.remark;
                ent.UpdateInfo(core, persils, tahapBayar);
                contextPay.enprosesRequest.Update(ent);

                RuningFlow(user, core.cmd, ent.instance);

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

        [HttpGet("enproses/persils/list")]
        public IActionResult GetListPersilForCategory([FromQuery] string token, JenisProses opr, TypeState type, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var tm0 = DateTime.Now;

                var types = type switch
                {
                    TypeState.bebas => new object[] { null, 0 },
                    TypeState.belumbebas => new object[] { 1, 3 }
                };

                var state = string.Join(',', types.Select(k => $"{(k == null ? "null" : k)}"));

                #region still on
                //var StillOn = contextPay.GetDocuments(new { keyPersil = "", lastState = 0 }, "update_request_data",
                //    "{$match: {_t:'EnProsesRequest', 'info.details': {$ne : []}}}",
                //    @"{$lookup: {
                //           from: 'graphables',
                //           localField: 'instKey',
                //           foreignField: 'key',
                //           as: 'graph'
                //         }}",
                //    "{$unwind : '$info.details'}",
                //    "{$addFields : {keyPersil : '$info.details.keyPersil'}}",
                //    @" {$project : {
                //            _id : 0,
                //            keyPersil : 1,
                //            lastState : {$arrayElemAt : ['$graph.lastState.state', 0]}
                //        }}",
                //    "{$match: {lastState : {$ne:20}}}").ToList();

                //var persilkeys = string.Join(',', StillOn.Select(k => $"'{k.keyPersil}'"));
                #endregion

                var persils = contextPay.GetDocuments(new PersilCore5Ext(), "persils_v2",
                   $@"<$match:<$and: [<'basic.current.keyParent':null>,<'basic.current.en_proses':{Convert.ToString((int)opr)}>,<en_state:<$in:[{state}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".MongoJs(),
                   @"{
                        '$project': {
                        'key': '$key',
                        'IdBidang': '$IdBidang',
                        'en_state': '$en_state',
                        'alasHak': '$basic.current.surat.nomor',
                        'noPeta': '$basic.current.noPeta',
                        'keyDesa': '$basic.current.keyDesa',
                        'keyProject': '$basic.current.keyProject',
                        'keyPTSK': '$basic.current.keyPTSK',
                        'luasSurat': '$basic.current.luasSurat',
                        'group': '$basic.current.group',
                        'en_proses': '$basic.current.en_proses',
                        'pemilik': '$basic.current.pemilik',
                        '_id': 0
                    }
                }").ToList();

                var locations = contextPay.GetVillages().ToList();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var ndata = persils.GroupJoin(locations, d => d.keyDesa, p => p.desa?.key, (d, p) => d.setLocation(p.FirstOrDefault().desa?.identity, p.FirstOrDefault().project?.identity));
                var nxdata = ndata.GroupJoin(ptsks, d => d.keyPTSK, p => p.key, (d, p) => d.setPT(p.FirstOrDefault()?.name));

                var xlst = ExpressionFilter.Evaluate(nxdata, typeof(List<PersilCore5>), typeof(PersilCore5), gs);
                var data = xlst.result.Cast<PersilCore5>().ToArray();

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

        private List<EnProsesPersilDetail> GetBidangDetailEnProsesRequest(string typeRequest)
        {
            var bidangDetails = contextPay.GetDocuments(new EnProsesPersilDetail(), "update_request_data",
                        "{$match: {$and: [{_t: 'EnProsesRequest'}, {'invalid': {$ne: true}}, {'info.type' : {$in: [" + typeRequest + "]}}] }}",
                        "{$unwind: '$info.details'}",
                        "{$project: { _id: 0, key: '$key', keyPersil: '$info.details.keyPersil'}}",
                        "{$lookup: { from: 'persils_v2', localField: 'keyPersil', foreignField: 'key', as: 'persil'}}",
                        "{$unwind: { path: '$persil', preserveNullAndEmptyArrays: true }}",
                       @"{$lookup: {
                                    from: 'maps', let: { keyDesa: '$persil.basic.current.keyDesa' }, pipeline:
                                        [
                                            { $unwind: '$villages' },
                                            { $match: { $expr: { $eq: ['$villages.key', '$$keyDesa'] } } },
                                            {
                                                $project:
                                                {
                                                    _id: 0,
                                                    desa: '$villages.identity',
                                                    project: '$identity'
                                                }
                                            }
                                        ], as: 'map'
                            }}",
                       "{$unwind: { path: '$map', preserveNullAndEmptyArrays: true }}",
                      @"{$lookup: {
                            from: 'masterdatas',
                            let: { 'keyPTSK': '$persil.basic.current.keyPTSK' },
                            pipeline: [
                                { $match: { $expr: {$and: [{ $eq: ['$_t', 'ptsk']}, { $eq: ['$key', '$$keyPTSK']}]}}},
                                   {
                                $project: {
                                    _id: 0,
                                    ptskName: '$identifier'
                                        }
                                    }
                                ],
                            as: 'ptsk'
                        }}",
                        "{ $unwind: { path: '$ptsk', preserveNullAndEmptyArrays: true }}",
                       @"{$project: {
                                _id: 0,
                                keyRequest: '$key',
                                keyPersil: '$persil.key',
                                idBidang: '$persil.IdBidang',
                                project: '$map.project',
                                desa: '$map.desa',
                                ptsk: '$ptsk.ptskName',
                                alasHak: '$persil.basic.current.surat.nomor',
                                noPeta: '$persil.basic.current.nomorPeta',
                                group: '$persil.basic.current.group',
                                statusTanah: '$persil.basic.current.en_proses'
                         }}").ToList();

            return bidangDetails;
        }
        #endregion

        #region LANDSPLITTING

        [HttpGet("landsplit/list")]
        public IActionResult ListLandSplitRequest(string token, [FromQuery] AgGridSettings gs, string tfind)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var privileges = contextPay.GetCollections(new { _id = "", priv = new[] { "" } }, "WF_PRIVS", "{}", "{}").ToList();

                var canView = privileges.Where(x => privs.Intersect(x.priv).Any()).ToList();
                //var instkeys = string.Join(",", canView.Select(p => $"'{p._id}'"));
                var instkeys = canView.Select(c => c._id).ToArray();

                #region huftbanget
                //string[] stagesQuery = new[]
                //    {
                //        "{$match: {$and: [{_t: 'LandSplitRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                //        "{$project: {_id: 0}}"
                //    };

                //if (!string.IsNullOrEmpty(tfind))
                //{
                //    tfind = tfind.ToEscape();
                //    stagesQuery = new[]
                //        {
                //                "{$match: {$and: [{_t: 'LandSplitRequest'}, {'invalid': {$ne: true}}, {'instKey' : {$in: [" + instkeys + "]}}] }}",
                //                "{$lookup: {from: 'persils_v2', localField: 'info.keyPersil', foreignField: 'key', as: 'persilParent'}}",
                //                "{$unwind: { path: '$persilParent', preserveNullAndEmptyArrays: true}}",
                //                "{$unwind: '$info.details'}",
                //                "{$lookup: {from: 'persils_v2', localField: 'info.details.keyPersil', foreignField: 'key', as: 'persil'}}",
                //                "{$unwind: { path: '$persil', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from: 'maps',let: { key: '$info.keyDesa'},
                //                        pipeline:[{$unwind: '$villages'},
                //                  {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                //                  {$project: {key: '$villages.key', 
                //                  desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                //                    as:'desas'}}",
                //               "{$unwind: { path: '$desas', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from: 'maps',let: { key: '$info.details.keyDesa'},
                //                        pipeline:[{$unwind: '$villages'},
                //                  {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                //                  {$project: {key: '$villages.key', 
                //                  desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                //                    as:'desasDetail'}}",
                //                "{$unwind: { path: '$desasDetail', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from: 'maps',let: { key: '$persil.basic.current.keyDesa'},
                //                        pipeline:[{$unwind: '$villages'},
                //                              {$match: {$expr: {$eq:['$villages.key','$$key']}}},
                //                              {$project: {key: '$villages.key', 
                //                              desaIdentity: '$villages.identity', projectIdentity: '$identity'} }], 
                //                    as:'desasPersil'}}",
                //                "{$unwind: { path: '$desasPersil', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from : 'bayars', let: {keyPersil: '$persil.key'},
                //                                            pipeline:[
                //                                                {$unwind: '$bidangs'},
                //                                                {$match: {$expr: {$eq: ['$bidangs.keyPersil', '$$keyPersil']}}},
                //                                                {$project: {
                //                                                    _id: 0, tahap: '$nomorTahap',

                //                                                }}
                //                                            ], as:'bayarTahap'
                //                    }}",
                //               "{$unwind: { path: '$bayarTahap', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$info.keyPTSK'}, pipeline: [
                //                            {$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                //                            {$project: {_id: 0, name: '$identifier'}}
                //                    ], as: 'ptsk'}}",
                //               "{$unwind: { path: '$ptsk', preserveNullAndEmptyArrays: true}}",

                //               @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$info.details.keyPtsk'}, pipeline: [
                //                            {$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                //                            {$project: {_id: 0, name: '$identifier'}}
                //                    ], as: 'ptskDetail'}}",
                //               "{$unwind: { path: '$ptskDetail', preserveNullAndEmptyArrays: true}}",

                //                @"{$lookup: {from: 'masterdatas', let: {keyPTSK: '$persil.basic.current.keyPTSK'}, pipeline: 
                //                    [{$match: {$expr: {$and: [{$eq: ['$_t', 'ptsk']}, {$eq: ['$key', '$$keyPTSK']}, {$ne:['$invalid', true]} ]}}},
                //                    {$project: {_id: 0, name: '$identifier'}}], as: 'ptskPersil'}}",
                //                "{$unwind: { path: '$ptskPersil', preserveNullAndEmptyArrays: true}}",

                //              "{$addFields: {tahapPersil: {$toString : '$bayarTahap.tahap'}}}",
                //              "{$addFields: {tahap: {$toString : '$info.details.tahap'}}}",

                //             @$"<$match: <$or : [
                //                    <'identifier': /{tfind}/i>,        
                //                    <'info.details.IdBidang': /{tfind}/i>,
                //                    <'info.details.alasHak': /{tfind}/i>,
                //                    <'info.details.group': /{tfind}/i>,
                //                    <'info.details.noPeta': /{tfind}/i>,
                //                    <'desas.desaIdentity': /{tfind}/i>,
                //                    <'desas.projectIdentity': /{tfind}/i>,
                //                    <'desasDetail.desaIdentity': /{tfind}/i>,
                //                    <'desasDetail.projectIdentity': /{tfind}/i>,
                //                    <'desasPersil.desaIdentity': /{tfind}/i>,
                //                    <'desasPersil.projectIdentity': /{tfind}/i>,
                //                    <'persil.IdBidang': /{tfind}/i>,
                //                    <'persil.basic.current.surat.nomor': /{tfind}/i>,
                //                    <'persil.basic.current.group': /{tfind}/i>,
                //                    <'persil.basic.current.noPeta': /{tfind}/i>,
                //                    <'tahapPersil': /{tfind}/i>,
                //                    <'tahap': /{tfind}/i>,
                //                    <'ptsk.name': /{tfind}/i>,
                //                    <'ptskDetail.name': /{tfind}/i>
                //                    <'ptskPersil.name': /{tfind}/i>
                //                ]>>".MongoJs(),
                //             "{$project: {_id:0,key: 1}}",
                //             "{$group: {_id:'$key'}}",
                //             "{$project: {key: '$_id'}}",
                //             "{$lookup: {from: 'update_request_data', localField: 'key', foreignField: 'key', as: 'urd'}}",
                //             "{$unwind: '$urd'}",
                //            @"{$project: {_id: 0, 
                //                    _t: '$urd._t',
                //                    key: '$urd.key',
                //                    invalid: '$urd.invalid',
                //                    identifier: '$urd.identifier',
                //                    instKey: '$urd.instKey',
                //                    creator: '$urd.creator',
                //                    created: '$urd.created',
                //                    info: '$urd.info'
                //               }}"
                //            };
                //};

                //var requests = contextPay.GetDocuments(new LandSplitRequest(), "update_request_data", stagesQuery).ToList();
                #endregion

                var locations = contextPay.GetVillages().ToArray();

                var requests = new List<LandSplitRequest>();
                requests = contextPay.landsplitRequests.Query(x => x.invalid != true && instkeys.Contains(x.instKey)).ToList();

                if (!string.IsNullOrEmpty(tfind))
                {
                    var keyPersils = requests.Select(x => x.info.keyPersil).ToList();
                    keyPersils.AddRange(requests.SelectMany(x => x.info.details).Select(x => x.keyPersil));
                    var persils = contextPay.persils.Query(x => keyPersils.Contains(x.key)).ToList();

                    var projects = locations.Select(x => x.project).Distinct().ToList();
                    var desas = locations.Select(x => x.desa).ToList();

                    var parents = requests.Join(persils, r => r.info.keyPersil, p => p.key, (r, p) => new { r, p })
                                            .Select(x => new
                                            {
                                                r = x.r,
                                                p = x.p,
                                                proj = projects.FirstOrDefault(pj => pj.key == x.r.info.keyProject),
                                                des = desas.FirstOrDefault(ds => ds.key == x.r.info.keyDesa)
                                            }).ToList();

                    var parentsChilds = parents.SelectMany(x => x.r.info.details, (p, c) => new { parent = p, child = c }).ToList();

                    var all = parentsChilds.Join(persils, r => r.child.keyPersil, p => p.key, (r, p) => new { parent = r.parent, child = r.child, p })
                                            .Select(x => new
                                            {
                                                parent = x.parent,
                                                child = x.child,
                                                p = x.p,
                                                proj = projects.FirstOrDefault(pj => pj.key == x.child.keyProject),
                                                des = desas.FirstOrDefault(ds => ds.key == x.child.keyDesa)
                                            }).ToList();

                    var searching = all.Where(x => x.parent.r.identifier.Contains(tfind)
                                                   || x.parent.p.IdBidang.Contains(tfind)
                                                   || (x.parent.p.basic.current.surat == null ? "" : x.parent.p.basic.current.surat.nomor == null ? "" : x.parent.p.basic.current.surat.nomor).Contains(tfind)
                                                   || (x.parent.p.basic.current.group ?? "").Contains(tfind)
                                                   || (x.parent.p.basic.current.noPeta ?? "").Contains(tfind)
                                                   || x.parent.proj.identity.Contains(tfind)
                                                   || x.parent.des.identity.Contains(tfind)
                                                   || x.p.IdBidang.Contains(tfind)
                                                   || (x.p.basic.current.surat == null ? "" : x.p.basic.current.surat.nomor == null ? "" : x.p.basic.current.surat.nomor).Contains(tfind)
                                                   || (x.p.basic.current.group ?? "").Contains(tfind)
                                                   || (x.p.basic.current.noPeta ?? "").Contains(tfind)
                                                   || x.proj.identity.Contains(tfind)
                                                   || x.des.identity.Contains(tfind)).Select(x => x.parent.r.key).Distinct().ToList();

                    requests = requests.Where(x => searching.Contains(x.key)).ToList();
                }

                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToArray();

                var keycreators = requests.Select(x => x.creator).Distinct();
                var creators = string.Join(',', keycreators.Select(k => $"'{k}'"));

                var users = contextPay.GetCollections(new user(), "securities", $"<key: <$in:[{creators}]>>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                var view = requests.Select(x => x.ToView(contextPay, locations, ptsks)).ToArray();

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<LandSplitRequestView>), typeof(LandSplitRequestView), gs);
                var data = xlst.result.Cast<LandSplitRequestView>().OrderByDescending(x => x.created).ToArray();

                string prepregx(string val)
                {
                    //var st = val;//.Substring(1,val.Length-2); // remove the string marker
                    var st = val.Replace(@"\", @"\\");
                    st = new Regex(@"([\(\)\{\}\[\]\.\,\+\?\|\^\$\/])").Replace(st, @"\$1");
                    st = new Regex(@"\s+").Replace(st, @"\s+");
                    return st;
                }

                return Ok(data.GridFeed(gs));
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

        [HttpPost("landsplit/save")]
        public IActionResult SaveLandSplitRequest([FromQuery] string token, [FromBody] LandSplitApprovalCore core)
        {
            try
            {
                var user = contextPay.FindUser(token);

                var creator = user.privileges.Select(p => p.identifier).Where(x => x.Contains("CREATE_LANDSPLIT")).Any();
                if (!creator)
                    return new UnprocessableEntityObjectResult("Anda tidak memiliki hak menambahkan request");

                var keyPersils = core.keyPersils.ToList();
                if (!string.IsNullOrEmpty(core.keyPersil))
                    keyPersils.Add(core.keyPersil);

                var keys = string.Join(',', keyPersils.Select(k => $"'{k}'"));
                var persils = contextPay.GetCollections(new Persil(), "persils_v2", $"<key : <$in: [{keys}]>>".MongoJs(), "{_id:0}").ToList();

                bool ok = false;
                string message = null;

                (ok, message) = CheckingParent(core.keyPersil, core.keyPersils, persils.ToArray());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                (ok, message) = CheckExistsRequest(core.keyPersils.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var tahaps = contextPay.GetDocuments(new { key = "", nomorTahap = 0 }, "bayars",
                   "{$unwind: '$bidangs'}",
                   $@"<$match:<'bidangs.keyPersil' : <$in: [{keys}]>>>".MongoJs(),
                   @"{$project:{
                        key : '$bidangs.keyPersil',
                        nomorTahap : '$nomorTahap',
                        _id:0
                    }}").Select(x => (x.key, x.nomorTahap)).ToArray();

                var ent = new LandSplitRequest(user, persils.ToArray(), core, tahaps);
                var dno = contextPlus.docnoes.FirstOrDefault(d => d.key == "LandSplitApproval");

                ent.identifier = dno.Generate(DateTime.Now, true, string.Empty);
                ent.remark = core.remark;

                contextPay.landsplitRequests.Insert(ent);
                contextPay.SaveChanges();

                return new JsonResult(ent.key);
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

        [HttpPost("landsplit/edit")]
        public IActionResult EditLandSplitRequest([FromQuery] string token, [FromBody] LandSplitRequestCommad cmd)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var request = contextPay.landsplitRequests.Query(x => x.key == cmd.reqKey).FirstOrDefault();

                if (request == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                var existsDtls = request.info.details.Select(x => x.keyPersil).ToList();
                var newDtls = cmd.keyPersils.Select(x => x).Where(x => !existsDtls.Contains(x)).ToList();

                var keyPersils = cmd.keyPersils.ToList();
                if (!string.IsNullOrEmpty(cmd.keyPersil))
                    keyPersils.Add(cmd.keyPersil);

                var keys = string.Join(',', keyPersils.Select(k => $"'{k}'"));
                var persils = contextPay.GetCollections(new Persil(), "persils_v2", $"<key : <$in: [{keys}]>>".MongoJs(), "{_id:0}").ToList();

                bool ok = false;
                string message = null;
                (ok, message) = CheckingParent(cmd.keyPersil, cmd.keyPersils, persils.ToArray());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                (ok, message) = CheckExistsRequest(newDtls.ToList());
                if (!ok)
                    return new UnprocessableEntityObjectResult(message);

                var tahaps = contextPay.GetDocuments(new { key = "", nomorTahap = 0 }, "bayars",
                    "{$unwind: '$bidangs'}",
                    $@"<$match:<'bidangs.keyPersil' : <$in: [{keys}]>>>".MongoJs(),
                    @"{$project:{key : '$bidangs.keyPersil',nomorTahap : '$nomorTahap',_id:0}}").Select(x => (x.key, x.nomorTahap)).ToArray();

                var list = new List<LandSplitRequestDetail>();
                foreach (var k in cmd.keyPersils)
                {
                    var persil = persils.FirstOrDefault(x => x.key == k);
                    var nomorTahap = tahaps.FirstOrDefault(x => x.key == k).nomorTahap;
                    var detail = new LandSplitRequestDetail(persil, nomorTahap);
                    list.Add(detail);
                }

                request.EditDetail(cmd, list.ToArray(), persils.ToArray(), tahaps);

                contextPay.landsplitRequests.Update(request);
                contextPay.SaveChanges();

                RuningFlow(user, cmd, request.instance);

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

        [HttpGet("landsplit/persil")]
        public IActionResult PersilLandSplitRequest(string token, bool isParent, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextPay.FindUser(token);
                var tm0 = DateTime.Now;

                #region still on
                //var StillOn = contextPay.GetDocuments(new { keyPersil = "", lastState = 0 }, "update_request_data",
                //        //    "{$match: {_t:'StateRequest', 'info.details': {$ne : []}}}",
                //        //    @"{$lookup: {
                //        //           from: 'graphables',
                //        //           localField: 'instKey',
                //        //           foreignField: 'key',
                //        //           as: 'graph'
                //        //         }}",
                //        //    "{$unwind : '$info.details'}",
                //        //    "{$addFields : {keyPersil : '$info.details.keyPersil'}}",
                //        //    @" {$project : {
                //        //            _id : 0,
                //        //            keyPersil : 1,
                //        //            lastState : {$arrayElemAt : ['$graph.lastState.state', 0]}
                //        //        }}",
                //        //    "{$match: {lastState : {$ne:20}}}").ToList();

                //        //var keys = string.Join(',', StillOn.Select(k => $"'{k.keyPersil}'"));
                #endregion

                string stage = string.Empty;
                string projection = @"{
                        '$project': {
                        'key': '$key',
                        'IdBidang': '$IdBidang',
                        'en_state': '$en_state',
                        'alasHak': '$basic.current.surat.nomor',
                        'noPeta': '$basic.current.noPeta',
                        'keyDesa': '$basic.current.keyDesa',
                        'keyProject': '$basic.current.keyProject',
                        'keyPTSK': '$basic.current.keyPTSK',
                        'luasSurat': '$basic.current.luasSurat',
                        'group': '$basic.current.group',
                        'pemilik': '$basic.current.pemilik',
                        '_id': 0
                    }
                }";
                string[] stages = new string[0];
                var persils = new List<PersilLandSplit>();

                if (isParent)
                {
                    stage = $@"<$match:<$and: [<'basic.current.keyParent':null>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".MongoJs();

                    stages = new[]
                    {
                        stage,
                        projection
                    };

                    persils = contextPay.GetDocuments(new PersilLandSplit(), "persils_v2", stages).ToList();
                }
                else
                {
                    stage = $@"<$match:<$and: [<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".MongoJs();
                    stages = new[]
                    {
                        stage,
                        projection
                    };

                    var parents = contextPay.GetDocuments(new { parent = "" }, "persils_v2",
                        "{$match: {invalid : {$ne : true}, 'basic.current.keyParent' : {$ne: null}}}",
                        "{$project: {parent : '$basic.current.keyParent',_id:0}}").ToList().Select(x => x.parent).ToArray();

                    persils = contextPay.GetDocuments(new PersilLandSplit(), "persils_v2", stages).ToList().Where(x => !parents.Contains(x.key)).ToList();
                }

                var locations = contextPay.GetVillages().ToList();
                var ptsks = contextPay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var ndata = persils.GroupJoin(locations, d => d.keyDesa, p => p.desa?.key, (d, p) => d.setLocation(p.FirstOrDefault().desa?.identity, p.FirstOrDefault().project?.identity)).ToArray();
                var nxdata = ndata.GroupJoin(ptsks, d => d.keyPTSK, p => p.key, (d, p) => d.setPT(p.FirstOrDefault()?.name)).ToArray();

                var xlst = ExpressionFilter.Evaluate(nxdata, typeof(List<PersilLandSplit>), typeof(PersilLandSplit), gs);
                var data = xlst.result.Cast<PersilLandSplit>().ToArray();

                return Ok(data.GridFeed(gs));
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

        private (bool Ok, string message) CheckingParent(string parent, string[] childs, Persil[] persils)
        {
            var request = contextPay.landsplitRequests.Query(x => x.invalid != true).ToArray();
            if (request != null)
            {
                var parents = request.Select(x => x.info.keyPersil).ToArray();
                var details = request.SelectMany(x => x.info.details).Select(x => x.keyPersil).ToArray();

                if (details.Any(x => x == parent))
                {
                    var p = persils.FirstOrDefault(x => x.key == parent)?.IdBidang;
                    return (false, $"{p} adalah CHILD dari bidang lain");
                }

                if (childs.Where(x => parents.Contains(x)).Any())
                {
                    var px = persils.Where(x => parents.Contains(x.key)).ToList();
                    var p = string.Join(',', px.Select(x => $"{x.IdBidang}"));
                    return (false, $"{p} adalah PARENT dari bidang lain");
                }
            }

            return (true, null);
        }

        #endregion
    }
}
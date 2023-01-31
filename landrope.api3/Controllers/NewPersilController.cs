//using APIGrid;
//using AssignerConsumer;
//using BundlerConsumer;
//using graph.mod;
//using GraphConsumer;
//using landrope.consumers;
//using landrope.hosts;
//using landrope.mod2;
//using landrope.mod3;
//using landrope.mod4;
//using landrope.common;
//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Tracer;
//using GenWorkflow;
//using flow.common;

//namespace landrope.api2.Controllers
//{

//    [ApiController]
//    [Route("api/new-persil")]
//    [EnableCors(nameof(landrope))]
//    public class NewPersilController : ControllerBase
//    {
//        IServiceProvider services;
//        LandropePlusContext contextPlus;
//        ExtLandropeContext contextExt;
//        LandropePayContext contextPay;
//        GraphContext gcontext;
//        GraphHostConsumer ghost;
//        PersilApprovalHost host;

//        public NewPersilController(IServiceProvider services)
//        {
//            this.services = services;
//            contextPlus = services.GetService<mod3.LandropePlusContext>();
//            contextExt = services.GetService<ExtLandropeContext>();
//            contextPay = services.GetService<LandropePayContext>();
//            gcontext = services.GetService<GraphContext>();
//            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
//            host = HostServicesHelper.GetBidangHost(this.services);
//        }

//        /// <summary>
//        /// List Bidang in Pagination Format
//        /// </summary>
//        [HttpGet("list")]
//        public IActionResult Getlist([FromQuery] string token, [FromQuery] AgGridSettings gs)
//        {
//            try
//            {
//                var user = contextPay.FindUser(token);
//                var listRequest = host.OpenReqNewPersil().Cast<ReqNewPersil>().ToArray().AsParallel();
//                var villages = contextPay.GetVillages();

//                var instanceKeys = listRequest.Select(d => d.instKey).ToList();
//                string instKeys = string.Join(",", instanceKeys);
//                var Steps = (ghost.GetMany(instKeys, "").GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
//                List<NewPersilView> view = listRequest.Select(lb => lb.ToView(villages, Steps)).ToList();
//                var xlst = ExpressionFilter.Evaluate(view, typeof(List<NewPersilView>), typeof(NewPersilView), gs);
//                var data = xlst.result.Cast<NewPersilView>().ToArray();

//                return Ok(data.GridFeed(gs));
//            }
//            catch (UnauthorizedAccessException exa)
//            {
//                return new ContentResult { StatusCode = int.Parse(exa.Message) };
//            }
//            catch (Exception ex)
//            {
//                MyTracer.TraceError2(ex);
//                return new UnprocessableEntityObjectResult(ex.Message);
//            }
//        }

//        /// <summary>
//        /// Add new bidang
//        /// </summary>
//        [HttpPost("save")]
//        public IActionResult Save([FromQuery] string token, NewPersilCore core)
//        {
//            try
//            {
//                var user = contextPay.FindUser(token);
//                ReqNewPersil newPersil = new ReqNewPersil(core, user);
//                host.Add(newPersil);
//                return Ok();
//            }
//            catch (UnauthorizedAccessException exa)
//            {
//                return new ContentResult { StatusCode = int.Parse(exa.Message) };
//            }
//            catch (Exception ex)
//            {
//                MyTracer.TraceError2(ex);
//                return new UnprocessableEntityObjectResult(ex.Message);
//            }
//        }

//        /// <summary>
//        /// List Flow Request New Persil
//        /// </summary>
//        [HttpGet("list/flow")]
//        public IActionResult GetListFlow([FromQuery] string token, [FromQuery] AgGridSettings gs)
//        {
//            try
//            {
//                var user = contextPay.FindUser(token);
//                GraphTree[] Gs1 = ghost.List(user).GetAwaiter().GetResult() ?? new GraphTree[0];
//                var Gs = Gs1.Where(x => x.subs.Any()).Select(x => (x.main, inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes));
//                string[] instkeys = Gs.Select(g => g.main.key).ToArray();
//                ReqNewPersil[] listRequest = host.OpenReqNewPersilbyInstance(instkeys).Cast<ReqNewPersil>().ToArray();
//                var villages = contextPay.GetVillages();
//                List<NewPersilView> data = listRequest.Select(lb => lb.ToView(villages)).ToList();

//                var nxdata = data.Join(Gs, a => a.instKey, g => g.main?.key,
//                                             (a, g) => (a, i: g.main, nm: g.inmain?.LastOrDefault())).ToArray().AsParallel();

//                var ndata = nxdata.Select(x => (x.a, x.i, x.nm, routes: x.nm?.routes)).ToArray().AsParallel();
//                var ndatax = ndata.Select(x => (x, y: NewPersilViewExt.Upgrade(x.a))).ToArray().AsParallel();

//                var data2 = ndatax.Where(X => X.y != null).Select(X => X.y?
//                 .SetRoutes(X.x.routes.Select(x => (x.key, x._verb.Title(),
//                                                    x._verb, x.branches.Select(b => b._control).ToArray())).ToArray())
//                 .SetState(X.x.nm?.node._state ?? ToDoState.unknown_)).ToArray();

//                var xlst = ExpressionFilter.Evaluate(data2, typeof(List<NewPersilViewExt>), typeof(NewPersilViewExt), gs);
//                var filteredData = xlst.result.Cast<NewPersilViewExt>().ToList();
//                var sorted = filteredData.GridFeed(gs);

//                return Ok(sorted);
//            }
//            catch (UnauthorizedAccessException exa)
//            {
//                return new ContentResult { StatusCode = int.Parse(exa.Message) };
//            }
//            catch (Exception ex)
//            {
//                MyTracer.TraceError2(ex);
//                return new UnprocessableEntityObjectResult(ex.Message);
//            }
//        }

//        /// <summary>
//        /// Run Flow for Request New Persil
//        /// </summary>
//        [HttpPost("step")]
//        public IActionResult RunFlow([FromQuery] string token, ReqNewPersilCommand cmd)
//        {
//            try
//            {
//                var user = contextPay.FindUser(token);
//                ReqNewPersil newPersil = host.GetBidang(cmd.reqKey) as ReqNewPersil;

//                if (newPersil == null)
//                    return new UnprocessableEntityObjectResult("Request tidak ada");
//                if (newPersil.invalid == true)
//                    return new UnprocessableEntityObjectResult("Request tidak aktif");

//                var instance = ghost.Get(newPersil.instKey).GetAwaiter().GetResult();
//                if (instance == null)
//                    return new UnprocessableEntityObjectResult("Konfigurasi Request belum lengkap");
//                if (instance.closed)
//                    return new UnprocessableEntityObjectResult("Request telah selesai");

//                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == cmd.routeKey));
//                if (node == null)
//                    return new UnprocessableEntityObjectResult("Posisi Flow Request tidak jelas");
//                var route = node.routes.First(r => r.key == cmd.routeKey);

//                (var ok, var reason) = ghost.Take(user, newPersil.instKey, cmd.routeKey).GetAwaiter().GetResult();
//                if (ok)
//                    (ok, reason) = ghost.Summary(user, newPersil.instKey, cmd.routeKey, cmd.control.ToString("g"), null).GetAwaiter().GetResult();
//                if (!ok)
//                    return new UnprocessableEntityObjectResult(reason);

//                var priv = route.privs.FirstOrDefault();
//                string error = null;
//                if (cmd.control == ToDoControl._)
//                    (ok, error) = route?._verb switch
//                    {
//                        ToDoVerb.reissue_ => (CancelRequest(cmd.reqKey, 1)),
//                        ToDoVerb.landReject1_ => (CancelRequest(cmd.reqKey, 2)),
//                        ToDoVerb.landReject2_ => (CancelRequest(cmd.reqKey, 2)),
//                        ToDoVerb.confirmAbort_ => (AbortRequest(cmd.reqKey)),
//                        _ => (true, null)
//                    };

//                if (ok)
//                    return Ok();
//                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status bidang" : error);
//            }
//            catch (UnauthorizedAccessException exa)
//            {
//                return new ContentResult { StatusCode = int.Parse(exa.Message) };
//            }
//            catch (Exception ex)
//            {
//                MyTracer.TraceError2(ex);
//                return new UnprocessableEntityObjectResult(ex.Message);
//            }
//        }

//        private (bool, string) AbortRequest(string reqKey)
//        {
//            //
//            return (true, null);
//        }

//        private (bool, string) CancelRequest(string reqKey, int rejectBy)
//        {
//            //
//            return (true, null);
//        }
//    }
//}

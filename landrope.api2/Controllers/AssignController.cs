#define _LOOSE
using APIGrid;
using auth.mod;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DynForm.shared;
using flow.common;
using GenWorkflow;
using graph.mod;
using GraphHost;
using landrope.api2.Models;
using landrope.common;
using landrope.documents;
using landrope.engines;
//using landrope.hosts;
using landrope.layout;
using landrope.material;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod3.shared;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
//using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using Tracer;
using Microsoft.Extensions.DependencyInjection;
using GraphConsumer;
using landrope.consumers;
using AssignerConsumer;
using BundlerConsumer;
using MongoDB.Bson;
using DocumentFormat.OpenXml.Wordprocessing;

//using GridMvc.Server;

namespace landrope.api2.Controllers
{
    [ApiController]
    [Route("api/assign")]

    [EnableCors(nameof(landrope))]
    public class AssignController : ControllerBase
    {
        IServiceProvider services;
        LandropePlusContext contextplus;
        ExtLandropeContext contextex;
        LandropeContext context;
        GraphContext gcontext;
        GraphHostConsumer ghost;
        AssignerHostConsumer ahost;
        BundlerHostConsumer bhost;

        public AssignController(IServiceProvider services)
        {
            this.services = services;
            contextplus = services.GetService<mod3.LandropePlusContext>();
            contextex = services.GetService<ExtLandropeContext>();
            context = services.GetService<LandropeContext>();
            gcontext = services.GetService<GraphContext>();
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
            bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
        }

        [NeedToken("TASK_VIEW,TASK_STEP")]
        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var ret = ahost.ListAssignmentViews(user.key, gs).GetAwaiter().GetResult();
                return Ok(ret);
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


        //[NeedToken("TASK_VIEW,TASK_STEP")]
        //[HttpGet("list")]
        //public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        //{
        //	//HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        //	try
        //	{
        //		var user = contextex.FindUser(token);
        //		var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

        //		var creator = priv.Intersect(new[] { "BPN_CREATOR", "NOT_CREATOR", "GPS_CREATOR" }).Any();
        //		var actor = priv.Contains("TASK_STEP");
        //		var monitor = priv.Contains("TASK_VIEW") && !creator;
        //		var admin = priv.Intersect(new[] { "ADMIN_LAND1", "ADMIN_LAND2", "BPN_ADMIN" }).Any();
        //		var archive = priv.Intersect(new[] { "ARCHIVE_FULL", "ARCHIVE_REVIEW" }).Any();

        //		var tm0 = DateTime.Now;

        //		var Gs1 = ghost.List(user).GetAwaiter().GetResult() ?? new GraphTree[0];
        //		var Gs = Gs1.Select(x => (x.main, inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes,
        //							insubs: x.subs.Where(s => s.instance.key != x.main.key)
        //							.SelectMany(s => s.nodes).ToArray()));
        //		//var instkeys = Gs.Select(g => $"'{g.main.key}'");
        //		//var stkeys = String.Join(",", instkeys);
        //		var instkeys = Gs.Select(g => g.main.key).ToArray();

        //		/*				var prestages = new List<string>();
        //						var poststages = new List<string>();
        //						var sortstages = new List<string>();*/
        //		(var keys, var strict) = (creator, actor, monitor,admin) switch
        //		{
        //			(true, _, _,_) =>  (null, false),
        //			(_, true, _, _) => (instkeys, true),
        //			(_, _, _, true) => (instkeys, true),
        //			(_, _, true,_) =>  (null, false),
        //			_ => (new string[] { }, true)
        //		};

        //		var posts = ahost.OpenedAssignments(keys).GetAwaiter().GetResult().Cast<Assignment>().ToList();

        //		var xlst = ExpressionFilter.Evaluate(posts, typeof(List<Assignment>), typeof(Assignment), gs);
        //		var data = xlst.result.Cast<Assignment>().Select(a=>a.ToView(contextplus)).ToArray();

        //		int skip = (gs.pageIndex - 1) * gs.pageSize;
        //		int limit = gs.pageSize > 0 ? gs.pageSize : 0;

        //		var nxdata = strict ? data.Join(Gs, a => a.instkey, g => g.main?.key, 
        //							(a, g) => (a, i: g.main, 
        //													nm: g.inmain?.LastOrDefault(), 
        //													ns: g.insubs?.LastOrDefault())) :
        //									data.GroupJoin(Gs, a => a.instkey, g => g.main?.key,
        //														(a, sg) => (a,g:sg.FirstOrDefault()))
        //														.Select(x=> (x.a,
        //																				i: x.g.main,
        //																				nm: x.g.inmain?.LastOrDefault(),
        //																				ns: x.g.insubs?.LastOrDefault()
        //																			)).ToArray();
        //		var ndata = nxdata.Select(x=>(x.a,x.i,x.nm,x.ns,route:x.nm?.routes.FirstOrDefault())).ToArray();
        //		var ndatax = ndata.Select(x => (x,y:AssignmentViewExt.Upgrade(x.a))).ToArray();
        //		var data2 = ndatax
        //			.Where(X => X.y != null).Select(X => X.y?
        //		.SetRoute(X.x.route?.key)
        //		.SetState(X.x.nm?.node._state ?? ToDoState.unknown_)
        //		.SetStatus(X.x.i?.lastState?.state.AsStatus(), X.x.i?.lastState?.time)
        //		.SetTodo(X.x.route?._verb.Title())
        //		.SetVerb(X.x.route?._verb ?? ToDoVerb.unknown_)
        //		.SetCmds(X.x.route?.branches.Select(b => b._control).ToArray())
        //		.SetCreator(user.FullName == X.x.a.creator)
        //		.SetMilestones(X.x.i?.states.LastOrDefault(s => s.state == ToDoState.issued_)?.time,
        //										X.x.i?.states.LastOrDefault(s => s.state == ToDoState.delegated_)?.time,
        //										X.x.i?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time,
        //										X.x.i?.states.LastOrDefault(s => s.state == ToDoState.complished_)?.time)
        //		).ToArray();
        //		var sorted = data2.GridFeed(gs, tm0, new Dictionary<string, object> { { "role", creator ? 1 : admin ? 3 : archive ? 4 : actor ? 2 : 0 } });
        //		return Ok(sorted);

        //		//int totalRecords = ndata.Length;
        //		//int totalPages = limit <= 0 ? 1 : (totalRecords - 1) / limit + 1;
        //		//var lresult = sorted;
        //		//var tms = DateTime.Now - tm0;

        //		//return Ok(new
        //		//{
        //		//	total = totalPages,
        //		//	page = gs.pageIndex,
        //		//	records = totalRecords,
        //		//	rows = lresult,
        //		//	role = creator ? 1: admin? 3 : archive? 4 : actor ? 2 : 0,
        //		//	tm = tms
        //		//});
        //	}
        //	catch (UnauthorizedAccessException exa)
        //	{
        //		return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //	}
        //	catch (Exception ex)
        //	{
        //		MyTracer.TraceError2(ex);
        //		return new UnprocessableEntityObjectResult(ex.Message);
        //	}

        //	string prepregx(string val)
        //	{
        //		//var st = val;//.Substring(1,val.Length-2); // remove the string marker
        //		var st = val.Replace(@"\", @"\\");
        //		st = new Regex(@"([\(\)\{\}\[\]\.\,\+\?\|\^\$\/])").Replace(st, @"\$1");
        //		st = new Regex(@"\s+").Replace(st, @"\s+");
        //		return st;
        //	}
        //}

        /*		(IEnumerable<AssignmentView> data, int count) CollectAssignment(string token,
										IEnumerable<string> prestages,
										IEnumerable<string> poststages,
										IEnumerable<string> sortstages,
										int skip, int limit)
				{
					var countstage = "{$count:'count'}";
					var totstages = prestages.Union(poststages).Union(new[] { countstage }).ToArray();

					var limstages = new List<string>();
					if (skip > 0)
						limstages.Add($"{{$skip:{skip}}}");
					if (limit > 0)
						limstages.Add($"{{$limit:{limit}}}");

					prestages = prestages.Union(poststages).Union(sortstages).Union(limstages).Union(new[] { "{$project:{_id:0}}" }).ToArray();

					var countpipe = PipelineDefinition<BsonDocument, BsonDocument>.Create(totstages);
					var resultpipe = PipelineDefinition<BsonDocument, AssignmentView>.Create(prestages);

					int count = 0;
					List<AssignmentView> dbresult = new List<AssignmentView>();

					var coll = context.db.GetCollection<BsonDocument>("material_assignment_core");
					var task1 = Task.Run(() =>
					{
						var xcounter = coll.Aggregate<BsonDocument>(countpipe);
						var counter = xcounter.FirstOrDefault();
						if (counter != null)
							count = counter.Names.Any(s => s == "count") ? counter.GetValue("count").AsInt32 : 0;
					});
					var task2 = Task.Run(() =>
					{
						var res = coll.Aggregate<AssignmentView>(resultpipe).ToList();
						dbresult = res.ToList();
					});
					Task.WaitAll(task1, task2);
					return (dbresult, count);// (int)Math.Truncate(count.count));
				}*/

        (IEnumerable<AssignmentView> data, int count) CollectAssignment(AssignerHostConsumer host,
                                string token,
                                Func<Assignment, bool> prestages,
                                Func<Assignment, bool> poststages,
                                Func<IEnumerable<Assignment>, IOrderedEnumerable<Assignment>> sortstages,
                                int skip, int limit)
        {
            /*			var countstage = "{$count:'count'}";
						var totstages = prestages.Union(poststages).Union(new[] { countstage }).ToArray();
			*/
            var limstages = (Func<IEnumerable<Assignment>, IEnumerable<Assignment>>)(
                    x => x.Skip(skip).Take(limit));

            var paramex = Expression.Parameter(typeof(Assignment));
            var xprepoststages = Expression.And(Expression.Call(prestages.Method, paramex), Expression.Call(poststages.Method, paramex));

            /*			var countpipe = PipelineDefinition<BsonDocument, BsonDocument>.Create(totstages);
						var resultpipe = PipelineDefinition<BsonDocument, AssignmentView>.Create(prestages);*/

            //var host = HostServicesHelper.GetAssignmentHost(ControllerContext.HttpContext.RequestServices);
            var xprepostmeth = xprepoststages.Method;

            var posts = host.OpenedAssignments(null).GetAwaiter().GetResult().Cast<Assignment>();
            if (xprepostmeth != null)
                posts = posts.Where(a => (bool)xprepostmeth.Invoke(a, null));
            var count = posts.Count();

            var xsortmeth = sortstages.Method;
            if (xsortmeth != null)
                posts = (IEnumerable<Assignment>)xsortmeth.Invoke(posts, null);

            var xlimitmeth = limstages.Method;
            if (xlimitmeth != null)
                posts = (IEnumerable<Assignment>)xlimitmeth.Invoke(posts, null);
            var result = posts.Select(a => a.ToView(contextplus)).ToList();
            return (result, count);// (int)Math.Truncate(count.count));
        }

        //internal static hosts.AssignmentHost ahost(IServiceProvider services) => HostServicesHelper.GetAssignmentHost(services);
        //internal static BundleHost bhost(IServiceProvider services) => HostServicesHelper.GetBundleHost(services);

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("get")]
        public IActionResult Get([FromQuery] string token, [FromQuery] string key)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                var user = contextex.FindUser(token);
                if (!(ahost.GetAssignment(key).GetAwaiter().GetResult() is Assignment data))
                    return new NoContentResult();

                var curdir = ControllerContext.GetContentRootPath();
                //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
                var rights = user.getPrivileges(null)?.Select(a => a.identifier).ToArray();
                var layout = LayoutMaster.LoadLayout(typeof(Assignment).Name, curdir, rights).ToList();
                //if (data.valid.corrections.Any())
                //	layout.ForEach(l => l.correction = data.valid.corrections.Contains(l.value));

                data.extras = contextplus.GetXtras(layout, null,
                new KeyValuePair<string, Func<option[]>>("lstSteps", () => GetSteps(rights)));

                AssignmentCore fdata = data.ToCore();

                var context = new DynamicContext<AssignmentCore>(layout.ToArray(), fdata);
                var json = JsonConvert.SerializeObject(context, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }

        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("get2")]
        public IActionResult Get2([FromQuery] string token, [FromQuery] string key)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                var user = contextex.FindUser(token);
                if (!(ahost.GetAssignment(key).GetAwaiter().GetResult() is Assignment data))
                    return new NoContentResult();

                var curdir = ControllerContext.GetContentRootPath();
                //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
                var rights = user.getPrivileges(null)?.Select(a => a.identifier).ToArray();
                var layout = LayoutMaster.LoadLayout(typeof(Assignment).Name, curdir, rights).ToList();
                //if (data.valid.corrections.Any())
                //	layout.ForEach(l => l.correction = data.valid.corrections.Contains(l.value));

                data.extras = contextplus.GetXtras(layout, null,
                new KeyValuePair<string, Func<option[]>>("lstSteps", () => GetSteps(rights)));

                AssignmentCore fdata = data.ToCore();

                //var context = new DynamicContext<AssignmentCore>(layout.ToArray(), fdata);
                var json = JsonConvert.SerializeObject(fdata, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }


        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("dtl/list")]
        public IActionResult GetListDtl([FromQuery] string token, [FromQuery] string asgnkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var assg = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
                if (assg == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                assg.Inject(ghost);

                var instance = assg.instance;
                //var ghost = new GraphConsumer.GraphHostConsumer();

                var data = assg.details
                            .Select(d => d.ToView(contextplus)).ToList();
                var docKeys = data.Select(d => d.key).ToArray();
                var subs = ghost.GetFromDetails(user, instance.key, docKeys).GetAwaiter().GetResult();
                //var subs = data.Select(d => (d.key, tree: ));
                var res2 = data.GroupJoin(subs, d => d.key, s => s.key,
                            (d, ss) => (d, s: ss.FirstOrDefault().trees?.FirstOrDefault()))
                            .Select(x => (x.d, inst: instance, node: x.s?.nodes?.LastOrDefault(), view: x.s?.nodes?.LastOrDefault().viewOnly))
                                    .Select(x => (x, y: AssignmentDtlViewExt.Upgrade(x.d))).ToArray();

                var res = res2.Select(X => (X.y, X.x.node?.node, X.x.inst, route: X.x.node?.routes?.FirstOrDefault(), view: X.x.view ?? false)).AsParallel()
                                            .Select(X => X.y.SetAttributes(
                                                    X.view ? null : X.route?.key,
                                                    GetActualState(X.inst, X.route?.key),// X.node?._state ?? ToDoState.unknown_,
                                                    X.inst?.lastState?.time,
                                                    X.view ? ToDoVerb.unknown_ : (X.route?._verb ?? ToDoVerb.unknown_),
                                                    X.view ? string.Empty : X.route?._verb.Title(),
                                                    X.view ? null : X.route?.branches.Select(b => b._control).ToArray())
                                    //.SetRoute(X.x.node?.routes?.FirstOrDefault()?.key)
                                    //.SetState(X.x.node?.node?._state ?? ToDoState.unknown_)
                                    //.SetStatus(X.x.inst?.lastState?.state.AsStatus(), X.x.inst?.lastState?.time)
                                    //.SetTodo(X.x.node?.routes?.FirstOrDefault()?._verb.Title())
                                    //.SetCmds(X.x.node?.routes?.FirstOrDefault()?.branches.Select(b => b._control).ToArray())
                                    //.SetVerb(X.x.node?.routes?.FirstOrDefault()?._verb ?? ToDoVerb.unknown_)
                                    ).ToArray();

                return Ok(res.GridFeed(gs));
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

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("exp/list")]
        public IActionResult GetListExp([FromQuery] string token, [FromQuery] string asgnkey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var assg = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
                if (assg == null)
                    return new NoContentResult();

                var data = assg.expenses
                .Select(x => x.ToCore()).ToList();

                return Ok(data.GridFeed(gs));
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_STEP,BPN_CREATOR,NOT_CREATOR,BUNDLE_VIEW,BUNDLE_FULL,ARCHIVE_FULL")]
        [HttpGet("dtl/bundle")]
        public IActionResult GetDtlBundle([FromQuery] string token, [FromQuery] string asgnkey, [FromQuery] string dtlkey)
        {
            try
            {
                //var bhost = HostServicesHelper.GetBundleHost(services);
                //var ahost = HostServicesHelper.GetAssignmentHost(services);
                var assign = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
                if (assign == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                var detail = assign.details.FirstOrDefault(d => d.key == dtlkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak ada");
                if (assign.issued != null && detail.keyBundle == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak memiliki bundle penugasan");
                var preview = assign.issued == null && detail.keyBundle == null;

                (ITaskBundle tbun, string reason) = preview ? bhost.MakeTaskBundle(token, dtlkey, false).GetAwaiter().GetResult() :
                                (bhost.TaskGet(detail.keyBundle).GetAwaiter().GetResult(), (string)null);
                if (tbun == null)
                    return new UnprocessableEntityObjectResult("Bidang dimaksud tidak memiliki bundle penugasan");

                var tbundle = (TaskBundle)tbun;
                var doctypes = DocType.List;

                var predocs = tbundle.doclist.SelectMany(d => d.docs.SelectMany(dd => dd.Value.ConvertBack2().Select(
                                dx => (d.keyDocType, chainkey: dd.Key, dx))));
                var docs = predocs.Where(d => d.dx.cnt > 0)
                    .GroupBy(d => (d.keyDocType, d.chainkey)).Select(g => (g.Key.keyDocType, g.Key.chainkey, exis: g.Select(d => d.dx).ToArray()));

                var emptydict = new Dictionary<string, string>();
                /*				var props = bhost.ListProps(tbundle.keyParent).Join(doctypes, p => p.keyDocType, t => t.key,
                                            (p, t) => (p.key, p.keyDocType, t.identifier, p.chainkey, p.stprops));*/
                var props = bhost.ListProps(tbundle.keyParent).GetAwaiter().GetResult()?
                    .Select(p => (p.key, p.keyDocType, p.chainkey, p.stprops)) ?? new (string, string, string, Dictionary<string, string>)[0];
                var result = docs.Join(doctypes, d => d.keyDocType, t => t.key, (d, t) => (d, t))
                        .GroupJoin(props, x => (x.d.keyDocType, x.d.chainkey), p => (p.keyDocType, p.chainkey), (d, sp) => (d, p: sp.FirstOrDefault()))
                    .Select(x => new BundleFact { keyDocType = x.d.t.key, doctype = x.d.t.identifier,/*chainkey=x.d.d.chainkey,*/props = x.p.stprops ?? emptydict, exis = x.d.d.exis });
                return Ok(result);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("new")]
        public IActionResult NewAssign(string token)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = new Assignment(user);
            odata.key = null;
            var curdir = ControllerContext.GetContentRootPath();
            //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
            var layout = LayoutMaster.LoadLayout(typeof(Assignment).Name, curdir, rights);
            odata.extras = contextplus.GetXtras(layout, null,
                new KeyValuePair<string, Func<option[]>>("lstSteps", () => GetSteps(rights)));
            AssignmentCore fdata = odata.ToCore();

            var context = new DynamicContext<AssignmentCore>(layout, fdata);
            return Ok(context);
        }

        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("new2")]
        public IActionResult NewAssign2(string token)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = new Assignment(user);
            odata.key = null;
            //var curdir = ControllerContext.GetContentRootPath();
            //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
            //var layout = LayoutMaster.LoadLayout(typeof(Assignment).Name, curdir, rights);
            //odata.extras = contextplus.GetXtras(layout, null,
            //    new KeyValuePair<string, Func<option[]>>("lstSteps", () => GetSteps(rights)));
            AssignmentCore fdata = odata.ToCore();

            var json = JsonConvert.SerializeObject(fdata, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
            return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
        }

        option[] GetSteps(string[] privs)
        {
            var dsteps = new List<DocProcessStep>();
            if (privs.Contains("GPS_CREATOR"))
                dsteps.Add(DocProcessStep.Riwayat_Tanah);
            //dsteps.Add(DocProcessStep.GPS_Dan_Ukur);
            if (privs.Contains("NOT_CREATOR"))
                dsteps.AddRange(new[]{DocProcessStep.AJB,DocProcessStep.AJB_Hibah,DocProcessStep.Akta_Notaris,DocProcessStep.SPH,
									//DocProcessStep.Riwayat_Tanah
				});
            if (privs.Contains("BPN_CREATOR"))
                dsteps.AddRange(new[]{DocProcessStep.Balik_Nama,DocProcessStep.Cetak_Buku,DocProcessStep.PBT_Perorangan,
                                    DocProcessStep.PBT_PT,DocProcessStep.Peningkatan_Hak,
                                    DocProcessStep.Penurunan_Hak, DocProcessStep.SK_BPN});
            var isteps = dsteps.Select(s => (int)s);

            var steps = contextex.GetAllSteps();
            var res = steps.SelectMany(s => s.steps.Join(isteps, x => x, i => i, (x, i) => x)
            .Select(st => new option
            {
                keyparent = ContextExtensios.StrCats[s.disc],
                key = $"{(DocProcessStep)st:g}",
                identity = ((DocProcessStep)st).GetName()
            })).ToArray();
            return res;
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("dtl/new")]
        public IActionResult NewAssignDtl(string token, string asgnkey)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new NoContentResult();
            if (asgn.invalid == true)
                return new UnprocessableEntityObjectResult("Penugasan sedang di-nonaktifkan, tidak bisa menambahkan detail");
            if (asgn.issued != null)
                return new UnprocessableEntityObjectResult("Penugasan telah/sedang dikerjakan, tidak bisa lagi menambahkan detail");
            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = new AssignmentDtl();
            var curdir = ControllerContext.GetContentRootPath();
            //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
            //var layout = LayoutMaster.LoadLayout(typeof(AssignmentDtl).Name, curdir, rights);
            AssignmentDtlCore fdata = odata.ToCore(contextplus);
            //fdata.extras = contextplus.GetXtras(layout, null);

            var context = new DynamicContext<AssignmentDtlCore>(null, fdata);
            return Ok(context);
        }

        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("dtl/new2")]
        public IActionResult NewAssignDtl2(string token, string asgnkey)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new NoContentResult();
            if (asgn.invalid == true)
                return new UnprocessableEntityObjectResult("Penugasan sedang di-nonaktifkan, tidak bisa menambahkan detail");
            if (asgn.issued != null)
                return new UnprocessableEntityObjectResult("Penugasan telah/sedang dikerjakan, tidak bisa lagi menambahkan detail");
            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = new AssignmentDtl();
            var curdir = ControllerContext.GetContentRootPath();

            AssignmentDtlCore fdata = odata.ToCore(contextplus);

            var json = JsonConvert.SerializeObject(fdata, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
            return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("dtl/get")]
        public IActionResult GetAssignDtl(string token, string dtlkey)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var asgn = ahost.GetAssignment(dtlkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new NoContentResult();
            var dtl = asgn.details.FirstOrDefault(d => d.key == dtlkey);
            var IdBidang = contextplus.GetCollections(new { key = "", IdBidang = "" }, "persils_ID", $"{{key:'{dtl.keyPersil}'}}",
                                            "{_id:0}").FirstOrDefault()?.IdBidang;

            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = dtl.ToCore(contextplus);
            //var curdir = ControllerContext.GetContentRootPath();
            //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
            //var layout = LayoutMaster.LoadLayout(typeof(AssignmentDtl).Name, curdir, rights);
            //odata.extras = contextplus.GetXtras(layout, null);

            var context = new DynamicContext<AssignmentDtlCore>(null, odata);
            return Ok(context);
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_FULL,BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpPost("save")]
        [Consumes("application/json")]
        [AssigmmentMaterializer(Auto = false)]
        public IActionResult Save([FromQuery] string token, [FromQuery] string opr, [FromBody] AssignmentCore assg)
        {
            MethodBase.GetCurrentMethod().SetKeyValue<AssignmentSuppMaterializerAttribute>(assg.key);
            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + " penugasan";
            var user = context.FindUser(token);
            try
            {
                var res = opr switch
                {
                    "add" => Add(),
                    "del" => Del(),
                    _ => Edit(),
                };
                //MethodBase.GetCurrentMethod().GetCustomAttribute<AssigmmentMaterializerAttribute>().ManualExecute(contextex, assg.key);
                return res;
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal {predicate}, harap beritahu support");
            }

            IActionResult Edit()
            {
                var old = ahost.GetAssignment(assg.key).GetAwaiter().GetResult() as Assignment;// contextplus.assignments.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ditemukan");
                old.FromCore(assg);

                ahost.Update(old).Wait();
                return Ok();
            }

            IActionResult Del()
            {
                var old = ahost.GetAssignment(assg.key).GetAwaiter().GetResult() as Assignment;// contextplus.assignments.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ditemukan");

                ahost.Delete(assg.key).Wait();
                return Ok();
            }

            IActionResult Add()
            {
                var ent = Assignment.Create(contextplus, assg, user);
                ahost.Add(ent).Wait();
                return Ok();
            }
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_FULL,BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpPost("dtl/save")]
        public IActionResult SaveDtl([FromQuery] string token, string asgnkey, AssignmentDtlCore assg, string opr)
        {
            var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new UnprocessableEntityObjectResult("Penugasan tidak ada");
            if (asgn.issued != null)
                return new UnprocessableEntityObjectResult("Penugasan sudah diterbitkan dan tidak bida dimudifikasi lagi");

            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + " detail penugasan";
            try
            {
                var user = context.GetUser(token);

                return opr switch
                {
                    "add" => Add(),
                    "del" => Del(),
                    _ => Edit(),
                };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal {predicate}, harap beritahu support");
            }

            IActionResult Edit()
            {
                var old = asgn.details.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil penugasan dimaksud tidak ditemukan");
                if (asgn.details.Any(d => d.key != old.key && d.keyPersil == assg.keyPersil))
                    return new UnprocessableEntityObjectResult("Bidang ini sudah ada dalam list");
                old.FromCore(assg);

                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Del()
            {
                var old = asgn.details.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil penugasan dimaksud tidak ditemukan");
                asgn.DelDetail(old);
                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Add()
            {
                //if (string.IsNullOrWhiteSpace(assg.keyPersil))
                //{
                //	if (!string.IsNullOrWhiteSpace(assg.IdBidang))
                //		assg.keyPersil = contextplus.FindPersilKey(assg.IdBidang);
                //}
                if (string.IsNullOrWhiteSpace(assg.keyPersil))
                    return new UnprocessableEntityObjectResult("Detil penugasan harus menentukan obyek bidangnya");
                if (asgn.details.Any(d => d.keyPersil == assg.keyPersil))
                    return new UnprocessableEntityObjectResult("Bidang ini telah ditambahkan sebelumnya");
                var asgdtl = new AssignmentDtl();
                asgdtl.FromCore(assg);
                asgdtl.key = mongospace.MongoEntity.MakeKey;
                asgn.AddDetail(asgdtl);
                ahost.Update(asgn).Wait();
                return Ok();
            }
        }

        [NeedToken("TASK_FULL,BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpPost("dtl/save2")]
        public IActionResult SaveDtl2([FromQuery] string token, string asgnkey, AssignmentDtlCore assg, string opr)
        {
            var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new UnprocessableEntityObjectResult("Penugasan tidak ada");
            if (asgn.issued != null)
                return new UnprocessableEntityObjectResult("Penugasan sudah diterbitkan dan tidak bida dimudifikasi lagi");

            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + " detail penugasan";
            try
            {
                var user = context.GetUser(token);

                return opr switch
                {
                    "add" => Add(),
                    "del" => Del(),
                    _ => Edit(),
                };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal {predicate}, harap beritahu support");
            }

            IActionResult Edit()
            {
                var old = asgn.details.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil penugasan dimaksud tidak ditemukan");
                if (asgn.details.Any(d => d.key != old.key && d.keyPersil == assg.keyPersil))
                    return new UnprocessableEntityObjectResult("Bidang ini sudah ada dalam list");
                old.FromCore(assg);

                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Del()
            {
                var old = asgn.details.FirstOrDefault(a => a.key == assg.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil penugasan dimaksud tidak ditemukan");
                asgn.DelDetail(old);
                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Add()
            {

                var persils = assg.keyPersil.Split(',');

                foreach (var keyPersil in persils)
                {
                    if (string.IsNullOrWhiteSpace(keyPersil))
                        return new UnprocessableEntityObjectResult("Detil penugasan harus menentukan obyek bidangnya");
                    if (asgn.details.Any(d => d.keyPersil == keyPersil))
                        return new UnprocessableEntityObjectResult("Bidang ini telah ditambahkan sebelumnya");
                    var asgdtl = new AssignmentDtl();
                    asgdtl.FromCore(assg.key, keyPersil);
                    asgdtl.key = mongospace.MongoEntity.MakeKey;
                    asgn.AddDetail(asgdtl);
                }

                ahost.Update(asgn).Wait();
                return Ok();
            }
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_FULL,BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpPost("dtl/add")]
        public IActionResult AddDetails([FromQuery] string token, string asgnkey, [FromBody] string[] keys)
        {
            try
            {
                var user = context.GetUser(token);
                var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.issued != null)
                    return new UnprocessableEntityObjectResult("Penugasan sudah diterbitkan dan tidak bida dimudifikasi lagi");

                foreach (var k in keys)
                {
                    if (asgn.details.Any(d => d.keyPersil == k))
                        continue;
                    var asgdtl = new AssignmentDtl() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };
                    asgn.AddDetail(asgdtl);
                }
                if (asgn.type != null && asgn.step != null)
                    asgn.CalcDuration();
                ahost.Update(asgn).Wait();
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

        //[EnableCors(nameof(landrope))]
        [NeedToken("TASK_FULL")]
        [HttpPost("exp/save")]
        public IActionResult SaveExp([FromQuery] string token, string asgnkey, ExpenseCore expc, string opr)
        {
            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + " detail pembebanan";

            var asgn = ahost.GetAssignment(asgnkey).GetAwaiter().GetResult() as Assignment;
            if (asgn == null)
                return new UnprocessableEntityObjectResult("Assignment not found");
            var user = context.GetUser(token);
            try
            {
                return opr switch
                {
                    "add" => Add(),
                    "del" => Del(),
                    _ => Edit(),
                };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal {predicate}, harap beritahu support");
            }

            IActionResult Edit()
            {
                var old = asgn.expenses.FirstOrDefault(a => a.key == expc.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil pembebanan dimaksud tidak ditemukan");
                old.FromCore(expc);

                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Del()
            {
                var old = asgn.expenses.FirstOrDefault(a => a.key == expc.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Detil pembebanan dimaksud tidak ditemukan");
                asgn.DelExpense(old);
                ahost.Update(asgn).Wait();
                return Ok();
            }

            IActionResult Add()
            {
                var exp = new Expense();
                exp.FromCore(expc);
                asgn.AddExpense(exp);
                ahost.Update(asgn).Wait();
                return Ok();
            }
        }

        ////[EnableCors(nameof(landrope))]
        //[NeedToken("TASK_FULL")]
        //[HttpPost("step")]
        //public IActionResult Step([FromQuery]string token, [FromQuery] string akey,[FromQuery] string rkey, ToDoControl cmd)
        //{
        //	var user = contextplus.FindUser(token);
        //	var asgn = contextplus.assignments.FirstOrDefault(a => a.key == akey);
        //	if (asgn == null)
        //		return new UnprocessableEntityObjectResult("Penugasan tidak ada");
        //	if (asgn.invalid == true)
        //		return new UnprocessableEntityObjectResult("Penugasan tidak aktif");
        //	var instance = asgn.instance;
        //	if (instance==null)
        //		return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
        //	if (asgn.closed!=null || instance.closed)
        //		return new UnprocessableEntityObjectResult("Penugasan telah selesai");

        //	var host = ContextService.services.GetService<GraphHostSvc>();
        //	var ok  = host.Take(user, asgn.instkey, rkey);
        //	if (ok)
        //		ok = host.Summary(user, asgn.instkey, rkey, cmd.ToString("g"), null);
        //	if (ok)
        //		return Ok();
        //	return new UnprocessableEntityObjectResult("Gagal mengeksekusi workflow");
        //}

        //////[EnableCors(nameof(landrope))]
        ////[NeedToken("TASK_FULL")]
        ////[HttpPost("dtl/review")]
        ////public IActionResult ReviewDtl(string token, string dtlkey, DateTime time, bool accept, string note)
        ////{
        ////	if (!accept && string.IsNullOrWhiteSpace(note))
        ////		return new UnprocessableEntityObjectResult("Jika menolak harus menuliskan alasan/keterangannya");

        ////	var user = contextplus.FindUser(token);
        ////	var asgn = contextplus.assignments.FirstOrDefault(a => a.details.Any(d => d.key == dtlkey));
        ////	if (asgn == null)
        ////		return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ada");
        ////	var dtl = asgn.details.First(d => d.key == dtlkey);

        ////	lock (dtl.preresults)
        ////	{
        ////		if (dtl.hasAccepted())
        ////			return new UnprocessableEntityObjectResult("Penginputan hasil bidang penugasan ini telah diterima sebelumnya");
        ////		var res = dtl.preresults.FirstOrDefault(r => r.submitted == time);
        ////		if (res==null)
        ////			return new UnprocessableEntityObjectResult("Penginputan hasil yang dimaksud tidak ada");
        ////		res.reviewed = DateTime.Now;
        ////		res.keyReviewer = user.key;
        ////		res.accepted = accept;
        ////		res.rejectNote = accept ? null : note;

        ////		////asgn.CheckClose();

        ////		contextplus.assignments.Update(asgn);
        ////		contextplus.SaveChanges();
        ////	}
        ////	return Ok();
        ////}

        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_COORD,NOT_COORD,GPS_COORD")]
        [HttpGet("pic/list")]
        public IActionResult ListPIC(string token, string key)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var asgn = ahost.GetAssignment(key).GetAwaiter().GetResult() as Assignment;
                if (asgn == null || asgn.type == null)
                    return NoContent();
                var priv = asgn.type.Value switch
                {
                    ToDoType.Proc_BPN => "BPN_PIC",
                    ToDoType.Proc_Non_BPN => "NOT_PIC",
                    ToDoType.Proc_Pengukuran => "GPS_PIC",
                    _ => "_PIC"
                };

                var actkey = contextex.actions.FirstOrDefault(a => a.identifier == priv)?.key;
                var roles = contextex.GetCollections(new { key = "" }, "securities", $"<_t:'role','roleactionkeys.actionkey':'{actkey}'>".MongoJs(),
                                        "{_id:0,key:1}").ToList().Select(k => k.key).ToArray();
                //var roles = contextex.roles.Query(r => r.roleactionkeys.Any(ra => ra.actionkey == priv)).Select(r=>r.key).ToArray();
                var users = contextex.users.Query(u => u.userinrolekeys.Any(ur => roles.Contains(ur.rolekey)))
                    .Select(x => new option { keyparent = null, key = x.key, identity = x.FullName })
                    .ToArray();
                return Ok(users);
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

        //[EnableCors(nameof(landrope))]
        [HttpGet("meta/list")]
        public IActionResult ListMetaKeys()
        {
            try
            {
                //var user = contextplus.FindUser(token);
                //var asgn = contextplus.assignments.FirstOrDefault(a => a.key == key);
                //if (asgn == null || asgn.type == null)
                //	return NoContent();

                var docs = StepDocType.List.Where(l => l.invalid != true);// l.step == asgn.step);
                var docmetas = docs.SelectMany(d => d.receive.Select(r => (d.step, d.disc, r.key, r.ex, r.req)))
                            .Join(DocType.List, s => s.key, d => d.key, (s, d) => (s.step, s.disc, s.key, d.identifier, d.metadata));

                var types = docmetas.GroupBy(d => (d.disc, d.step)).SelectMany(g => g.Select(d => (distep: g.Key, d.key, d.identifier)))
                                        .Select(t => new option { keyparent = $"{t.distep.disc}|{t.distep.step:g}", key = t.key, identity = t.identifier });

                var metas = docmetas.Select(d => (d.key, d.identifier, d.metadata)).Distinct()
                    .SelectMany(d => d.metadata.Join(landrope.common.MetadataType.types, m => m.key, t => t.Key, (m, t) => (m, type: t.Value))
                                        .Where(x => x.type == typeof(string)).Select(x => x.m)
                        .Select(m => ((d.key, d.identifier, meta: m.key.ToString("g")))))
                    .Select(x => new option { keyparent = x.key, key = x.meta, identity = x.meta.Replace("_", " ") })
                    .ToList();

                var dicts = new Dictionary<string, option[]>();
                dicts.Add("optDocs", types.ToArray());
                dicts.Add("optMetas", metas.ToArray());

                return Ok(dicts);
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


        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("persil/list")]
        public IActionResult ListPersils(string token, string akey, string pkeys)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var asgn = ahost.GetAssignment(akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null || asgn.step == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada!");
                if (asgn.issued != null)
                    return new UnprocessableEntityObjectResult("Penugasan sudah diissue!");
                if (asgn.keyCreator != user.key)
                    return new UnprocessableEntityObjectResult("Anda bukan kreator penugasan ini!");

                var exceptkeys = (pkeys ?? "").Split(",");

                (var keyProject, var keyDesa, var keyPTSK, var keyPenampung, var cat, var step) =
                    (asgn.keyProject, asgn.keyDesa, asgn.keyPTSK, asgn.keyPenampung, asgn.category, asgn.step);
                var pnxs = bhost.NextReadiesDiscrete(inclActive: false).GetAwaiter().GetResult()
                    .Where(p => !exceptkeys.Contains(p.key))
                    .Where(p => p.keyProject == keyProject && p.keyDesa == keyDesa &&
                    (keyPTSK != null && p.keyPTSK == keyPTSK || keyPenampung != null && p.keyPenampung == keyPenampung) && p.cat == cat && p._step == step)
                    .ToArray();

                var persils = contextex.GetDocuments(new { key = "", basic = new PersilBasic() },
                    "persils_v2",
                    "{$match:{en_state:{$in:[null,0]},invalid:{$ne:true},IdBidang:{$ne:null},'basic.current':{$ne:null}}}",
                    "{$project:{key:1,basic:'$basic.current',_id:0}}").ToList();
                var data = pnxs.Join(persils, n => n.key, p => p.key,
                        (n, p) => new PersilView
                        {
                            key = n.key,
                            IdBidang = n.IdBidang,
                            pemilik = p.basic.pemilik,
                            nama = p.basic.surat.nama,
                            alias = p.basic.alias,
                            group = p.basic.group,
                            noPeta = p.basic.noPeta,
                            noSurat = p.basic.surat.nomor,
                            luasSurat = p.basic.luasSurat,
                            luasDibayar = p.basic.luasDibayar,
                            keyParent = p.basic.keyParent
                        }).Where(x => string.IsNullOrEmpty(x.keyParent)).ToArray();

                var AllAsgn = ahost.OpenedAssignments(new[] { "*" }).GetAwaiter().GetResult().Cast<Assignment>().ToList()
                   .Where(x => x.instkey != null && x.details != null);

                var Gs = gcontext.MainInstances.Query(x => x.closed == false && x.Core.name.Contains("PROSES")).ToList();

                var dt = AllAsgn.Join(Gs, a => a.instkey, g => g.key, (a, g) => (a, g)).Where(x => x.g.closed == false).ToList();

                var mains = dt.Select(x => x.g).ToList();

                var nesteds = mains.Select(x => x.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault()).ToList();
                var instakeys = nesteds.SelectMany(x => x.instakeys).ToArray();
                var closeds = nesteds.SelectMany(x => x.closeds).ToArray();

                var chains = instakeys.Join(closeds, i => i.Value, c => c.Key, (i, c) => new { dtKey = i.Key, closed = c.Value }).ToArray();

                var ps = dt.SelectMany(x => x.a.details).Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToList();

                var keyPersils = chains.Join(ps, c => c.dtKey, p => p.key, (c, p) => new { keyPersil = p.keyPersil, close = c.closed })
                                .Where(x => x.close == false).Select(x => x.keyPersil).ToArray();

                var datas = data.Where(x => !keyPersils.Contains(x.key));

                //var details = dt.SelectMany(x => x.a.details).ToList();

                //var datas = (from c in data
                //             where !(from o in details
                //                     select o.keyPersil).Contains(c.key)
                //             select c).ToArray();

                return Ok(datas.OrderBy(r => r.IdBidang));
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

        internal static Dictionary<string, DocProcessStep[]> CatSteps = new Dictionary<string, DocProcessStep[]> {
            {"BPN_CREATOR", new[] { DocProcessStep.Balik_Nama, DocProcessStep.Cetak_Buku,
                                        DocProcessStep.PBT_Perorangan, DocProcessStep.PBT_PT, DocProcessStep.Peningkatan_Hak,
                                        DocProcessStep.Penurunan_Hak,DocProcessStep.SK_BPN} },
            {"NOT_CREATOR", new[] { DocProcessStep.AJB, DocProcessStep.AJB_Hibah,
                                        DocProcessStep.Akta_Notaris, DocProcessStep.SPH} }
        };

        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpGet("persil/list-all")]
        public IActionResult ListPersilsAll(string token)
        {
            try
            {
#if _LOOSE_
				var pnxs = host.NextReadies(true);
#else
                var pnxs = bhost.NextReadiesDiscrete(inclActive: false).GetAwaiter().GetResult();
#endif
                var projects = contextplus.GetVillages();
                var locations = projects.Select(p => (keyProject: p.project.key, keyDesa: p.desa.key, project: p.project.identity, desa: p.desa.identity));
                var companies = contextex.companies.All();
                /*				var ptsks = companies.Where(c => c.status == StatusPT.pembeli).ToArray();
                                var penampungs = companies.Where(c => c.status == StatusPT.penampung).ToArray();
                */
                var user = contextplus.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var result = new List<PersilNx>();
                if (privs.Contains("NOT_CREATOR"))
                    result.AddRange(pnxs.Where(p => CatSteps["NOT_CREATOR"].Contains(p._step)));
                if (privs.Contains("BPN_CREATOR"))
                    result.AddRange(pnxs.Where(p => CatSteps["BPN_CREATOR"].Contains(p._step)));

                var res = result.GroupBy(x => (x.keyProject, x.keyDesa, x.keyPTSK, x.keyPenampung, x.cat, x._step))
                    .Select(g => new PersilNextReady
                    {
                        keyProject = g.Key.keyProject,
                        keyDesa = g.Key.keyDesa,
                        keyPTSK = g.Key.keyPTSK,
                        keyPenampung = g.Key.keyPenampung,
                        PTSK = companies.FirstOrDefault(c => c.key == g.Key.keyPTSK)?.identifier,
                        penampung = companies.FirstOrDefault(c => c.key == g.Key.keyPenampung)?.identifier,
                        cat = g.Key.cat,
                        disc = g.Key.cat.ToString("g"),
                        _step = g.Key._step,
                        step = g.Key._step.ToString("g"),
                        count = g.Count()
                    }.SetLocation(locations.FirstOrDefault(l => l.keyDesa == g.Key.keyDesa))).ToArray();
                return Ok(res);
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
            /*			try
						{
							var user = contextplus.FindUser(token);
							var privs = user.privileges.Select(p => p.identifier).ToArray();

							var result = (privs.Intersect(new[] { "BPN_CREATOR", "NOT_CREATOR" }).Any()) ?
								contextplus.GetDocuments(new PersilNextReady(), "persil_next_step",
			"{$lookup:{from:'material_persil_core',localField:'key',foreignField:'key',as:'core'}}",
			"{$unwind:'$core'}",
			"{$lookup:{from:'persils_v2',localField:'key',foreignField:'key',as:'v2'}}",
			"{$unwind:'$v2'}",
			"{$lookup:{from:'masterdatas',localField:'keyPenampung',foreignField:'key',as:'comp'}}",
			"{$unwind:'$comp'}",
			@"{$project:{_id:0,keyDesa:1,keyPTSK:1,keyPenampung:1,disc:'$v2._t',step:1,
					project:'$core.project',desa:'$core.desa',PTSK:'$core.PTSK',penampung:'$comp.identifier'}}").ToList() :
					new List<PersilNextReady>();

							var stepBpns = privs.Contains("BPN_CREATOR") ? new[] { DocProcessStep.Balik_Nama, DocProcessStep.Cetak_Buku,
										DocProcessStep.PBT_Perorangan, DocProcessStep.PBT_PT, DocProcessStep.Peningkatan_Hak,
										DocProcessStep.Penurunan_Hak,DocProcessStep.SK_BPN} : new DocProcessStep[0];
							var stepNots = privs.Contains("NOT_CREATOR") ? new[] { DocProcessStep.AJB, DocProcessStep.AJB_Hibah,
										DocProcessStep.Akta_Notaris, DocProcessStep.SPH} : new DocProcessStep[0];

							var allsteps = stepBpns.Union(stepNots).ToArray();
							result = result.Join(allsteps, r => r._step, s => s, (r, s) => r).ToList();

							if (privs.Contains("GPS_CREATOR"))
							{
								var res2 = contextplus.GetDocuments(new PersilNextReady(), "material_persil_core",
				"{$match:{en_state:1}}",
				"{$lookup:{from:'persils_v2',localField:'key',foreignField:'key',as:'v2'}}",
				"{$unwind:'$v2'}",
				"{$project:{_id:0,keyDesa:'$v2.basic.current.keyDesa',project:1,desa:1,disc:'$v2._t',step:'Riwayat_Tanah'}}").ToList();
								result.AddRange(res2);
							}

							result.ForEach(r =>
							{
								r.cat = r.disc switch
								{
									"persilGirik" => AssignmentCat.Girik,
									"persilHGB" => AssignmentCat.HGB,
									"persilSHM" => AssignmentCat.SHM,
									"persilSHP" => AssignmentCat.SHP,
									"PersilHibah" => AssignmentCat.Hibah
								};
								r.disc = r.cat.ToString("g");

								r._step = Enum.TryParse<DocProcessStep>(r.step, out DocProcessStep stp) ? stp : DocProcessStep.Belum_Bebas;
							});

							var gresult = result.GroupBy(r => (r.keyDesa, r.keyPTSK, r.keyPenampung, r.cat, r._step))
								.Select(g => new PersilNextReady
								{
									keyDesa = g.Key.keyDesa,
									keyPTSK = g.Key.keyPTSK,
									keyPenampung = g.Key.keyPenampung,
									project = g.First().project,
									desa = g.First().desa,
									PTSK = g.First().PTSK,
									penampung = g.First().penampung,
									cat = g.Key.cat,
									disc = g.First().disc,
									_step = g.Key._step,
									step = g.First().step,
									count = g.Count()
								});
							return Ok(gresult);
						}
						catch (UnauthorizedAccessException exa)
						{
							return new ContentResult { StatusCode = int.Parse(exa.Message) };
						}
						catch (Exception ex)
						{
							return new UnprocessableEntityObjectResult(ex.Message);
						}*/
        }

        //[EnableCors(nameof(landrope))]
        [NeedToken("BPN_CREATOR,NOT_CREATOR,GPS_CREATOR")]
        [HttpPost("issue")]
        public IActionResult Issue(string token, string akey)
        {
            var services = ControllerContext.HttpContext.RequestServices;
            //var ghost = new GraphConsumer.GraphHostConsumer();// GraphServiceHelper.GetGraphHost(services);
            var acontext = new contextant<authEntities>().MyContext;
            try
            {
                var user = contextex.FindUser(token);
                var privs = user.privileges.Select(a => a.identifier).ToArray();
                var assign = ahost.GetAssignment(akey).GetAwaiter().GetResult() as Assignment;
                if (assign == null || assign.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ada atau tidak valid");
                if (assign.instkey == null)
                    assign.CreateGraphInstance(user);
                if (assign.closed != null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud sudah selesai");
                if (assign.issued != null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud telah diterbitkan");
                var creator = acontext.users.FirstOrDefault(u => u.key == assign.keyCreator);
                if (creator == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak dibuat dengan benar");
                var crprivs = creator.privileges.Where(a => a.identifier.EndsWith("_CREATOR")).Select(a => a.identifier);
                if (!privs.Intersect(crprivs).Any())
                    return new UnprocessableEntityObjectResult("Anda tidak berhak menerbitkan penugasan dimaksud");

                if (!assign.BeforeIssuing())
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud belum bisa diterbitkan");

                var tree = assign.instance.FindJob(user, null);
                if (tree == null || !tree.Any())
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak memiliki flow yang benar");
                var routes = tree.SelectMany(t => t.nodes.SelectMany(n => n.routes));
                var cpriv = creator.key == user.key ? "@CREATOR" : assign.type switch
                {
                    ToDoType.Proc_BPN => "BPN_CREATOR",
                    ToDoType.Proc_Non_BPN => "NOT_CREATOR",
                    _ => "GPS_CREATOR"
                };
                var route = routes.FirstOrDefault(r => r.privs.Any() && r.privs[0].Contains(cpriv));
                if (route == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak memiliki flow yang benar");
                (var OK, var reason) = ghost.Take(user, assign.instkey, route.key, null, false).GetAwaiter().GetResult();
                if (!OK)
                    return new UnprocessableEntityObjectResult(reason);
                (OK, reason) = ghost.Summary(user, assign.instkey, route.key, "_", null, false).GetAwaiter().GetResult();
                if (!OK)
                    return new UnprocessableEntityObjectResult(reason);
                var results = assign.details.Select(d => (d.persil(contextplus).IdBidang,
                                res: bhost.MakeTaskBundle(token, d.key, false).GetAwaiter().GetResult()));
                var failures = results.Where(r => r.res.bundle == null).Select(f => f.IdBidang).ToArray();
                if (failures.Any())
                {
                    //bhost.ContextRollback();
                    //ahost.ContextRollback();
                    contextex.DiscardChanges();
                    contextplus.DiscardChanges();
                    gcontext.DiscardChanges();
                    return new UnprocessableEntityObjectResult($"Ada bidang yang yang tidak bisa dibuat bundle penugasan: ({string.Join(",", failures)})");
                }
                //assign.instance.RegisterDocs(assign.details.Select(d => d.key).ToArray());
                //ghost.RegisterDocs(assign.instkey, assign.details.Select(d => d.key).ToArray());
                ahost.Update(akey).Wait();

                //bhost.ContextCommit();
                //ahost.ContextCommit();
                contextex.SaveChanges();
                contextplus.SaveChanges();
                gcontext.SaveChanges();

                var warnings = results.Where(r => r.res.bundle != null && !string.IsNullOrEmpty(r.res.reason))
                                    .Select(w => $"'{w.IdBidang}:{w.res.reason}'").ToArray();
                if (warnings.Any())
                    return Ok($"Berhasil dengan beberapa catatan: ({string.Join(",", warnings)})");
                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                contextex.DiscardChanges();
                contextplus.DiscardChanges();
                gcontext.DiscardChanges();
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("BPN_ADMIN,GPS_ADMIN,ADMIN_LAND1,ADMIN_LAND2,ARCHIVE_FULL,ARCHIVE_REVIEW,NOT_PIC,BPN_PIC")]
        [HttpGet("dtl/result/list")]
        public IActionResult ListResultDocs(string token, string dkey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var assg = ahost.GetAssignmentOfDtl(dkey).GetAwaiter().GetResult() as Assignment;
                if (assg == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (assg.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan sudah tidak aktif");
                var reqpriv = assg.type switch
                {
                    ToDoType.Proc_BPN => new[] { "BPN_ADMIN", "ADMIN_LAND2", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    ToDoType.Proc_Non_BPN => new[] { "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    _ => new[] { "GPS_ADMIN", "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" }
                };
                if (!privs.Intersect(reqpriv).Any())
                    return Ok();
                var dtl = assg.details.FirstOrDefault(d => d.key == dkey);
                if (dtl == null)
                    return Ok();

                if (dtl.result == null)
                    MakeResult(assg, dtl);
                var infoes = dtl.result.infoes.Select(i => new RegisteredDocView
                {
                    keyDocType = i.keyDT,
                    docType = DocType.List.FirstOrDefault(d => d.key == i.keyDT)?.identifier,
                }
                .SetProperties(i.props.Select(p => new KeyValuePair<MetadataKey, Dynamic>(p.mkey, p.val)).ToDictionary(k => k.Key, k => k.Value))
                .SetExistence(i.exs.Select(x => new KeyValuePair<Existence, int>(x.ex, x.cnt)).ToDictionary(x => x.Key, x => x.Value))
                );
                return Ok(infoes);
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

        private void MakeResult(Assignment assg, AssignmentDtl dtl)
        {
            var stepdoc = StepDocType.GetItem(assg.step.Value, dtl.persil(contextplus).Discriminator);
            /*					if (stepdoc?.receive == null || stepdoc.receive.Length == 0)
									return Ok();*/
            var validdts = DocType.List.Where(l => l.invalid != true);
            var shouldrcv = stepdoc?.receive?.Join(validdts, s => s.keyDocType, d => d.key, (s, d) => (s.keyDocType, s.ex, d.metadata))
                        .SelectMany(x => x.metadata.Select(y => (x.keyDocType, x.ex, mkey: y.key,
                        val: new Dynamic(landrope.common.MetadataType.types[y.key], null))));


            //var xresults = ((AssignmentResult)dtl.preresult).infoes.SelectMany(p =>
            //			p.props.Select(d => (keyDocType: p.keyDT, ex: Existence.Asli, d.mkey, d.val))).ToList();
            //shouldrcv = shouldrcv.Where(x => !xresults.Select(r => (r.keyDocType, r.mkey)).Contains((x.keyDocType, x.mkey)));
            //xresults.AddRange(shouldrcv);
            dtl.result = new AssignmentResult
            {
                infoes = shouldrcv.GroupBy(r => r.keyDocType).Select(g => (g.Key, g.First().ex, props: g.Select(d => (d.mkey, d.val)).ToArray()))
                        .Select(x => new ResultDoc
                        {
                            keyDT = x.Key,
                            exs = new Existency[] { new Existency { ex = x.ex, cnt = 1 } },
                            props = x.props.Select(p => new ResultMetaD { mkey = p.mkey, val = p.val }).ToArray()
                        }).ToArray()
            };
            //dtl.preresult = null;
            ahost.Update(assg).Wait();
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("BPN_ADMIN,GPS_ADMIN,ADMIN_LAND1,ADMIN_LAND2")]
        [HttpGet("dtl/result")]
        public IActionResult GetResultDoc(string token, [FromQuery] string dkey, [FromQuery] string typeKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var assg = ahost.GetAssignmentOfDtl(dkey).GetAwaiter().GetResult() as Assignment;
                if (assg == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (assg.invalid == true)
                    return NoContent();
                var reqpriv = assg.type switch
                {
                    ToDoType.Proc_BPN => new[] { "BPN_ADMIN", "ADMIN_LAND2", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    ToDoType.Proc_Non_BPN => new[] { "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    _ => new[] { "GPS_ADMIN", "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" }
                };
                if (!privs.Intersect(reqpriv).Any())
                    return Ok();
                var dtl = assg.details.FirstOrDefault(d => d.key == dkey);
                if (dtl == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak ada");

                var stepdoc = StepDocType.GetItem(assg.step.Value, dtl.persil(contextplus).Discriminator);
                if (stepdoc?.receive == null || stepdoc.receive.Length == 0)
                    return Ok();
                var validdts = DocType.List.Where(l => l.invalid != true);
                var shouldrcv = stepdoc.receive.Join(validdts, s => s.keyDocType, d => d.key, (s, d) => (s.keyDocType, s.ex, d.metadata))
                            .SelectMany(x => x.metadata.Select(y => (x.keyDocType, x.ex, mkey: y.key,
                            val: new Dynamic(landrope.common.MetadataType.types[y.key], null))));

                if (dtl.result == null)
                    MakeResult(assg, dtl);

                var doc = dtl.result.infoes.FirstOrDefault(d => d.keyDT == typeKey);
                if (doc == null)
                    return new UnprocessableEntityObjectResult("Dokumen dengan jenis dimaksud tidak ada");
                var core = new ResultDocCore();
                (core.keyDocType, core.docType) =
                    (doc.keyDT, DocType.List.FirstOrDefault(d => d.key == doc.keyDT)?.identifier);
                core.exis = doc.exs.Convert();
                core.props = doc.props.ToDictionary(k => k.mkey.ToString("g"), k => (object)k.val.val);

                var editable = privs.Except(new[] { "ARCHIVE_FULL", "ARCHIVE_REVIEW" }).Any();
                var layout = MakeLayout2(doc, editable);
                var fdata = core;
                var dcontext = new DynamicContext<ResultDocCore>(layout, fdata);

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

        public static DynElement[] MakeLayout2(ResultDoc doc, bool editable = false)
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


        [EnableCors(nameof(landrope))]
        [NeedToken("BPN_ADMIN,GPS_ADMIN,ADMIN_LAND1,ADMIN_LAND2")]
        [HttpPost("dtl/result/save")]
        [Consumes("application/json")]
        public IActionResult SaveResultDoc([FromQuery] string token, [FromQuery] string dkey, [FromQuery] string typeKey,
                                                    [FromBody] ResultDocCore data)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var privs = user.privileges.Select(p => p.identifier).ToArray();

                var assg = ahost.GetAssignmentOfDtl(dkey).GetAwaiter().GetResult() as Assignment;
                if (assg == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (assg.invalid == true)
                    return NoContent();
                var reqpriv = assg.type switch
                {
                    ToDoType.Proc_BPN => new[] { "BPN_ADMIN", "ADMIN_LAND2", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    ToDoType.Proc_Non_BPN => new[] { "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" },
                    _ => new[] { "GPS_ADMIN", "ADMIN_LAND1", "ARCHIVE_FULL", "ARCHIVE_REVIEW" }
                };
                if (!privs.Intersect(reqpriv).Any())
                    return new UnprocessableEntityObjectResult("Anda tidak berhak melakukan hal ini");
                var dtl = assg.details.FirstOrDefault(d => d.key == dkey);
                if (dtl == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak ada");

                var stepdoc = StepDocType.GetItem(assg.step.Value, dtl.persil(contextplus).Discriminator);
                if (stepdoc?.receive == null || stepdoc.receive.Length == 0)
                    return Ok();
                var validdts = DocType.List.Where(l => l.invalid != true);
                var shouldrcv = stepdoc.receive.Join(validdts, s => s.keyDocType, d => d.key, (s, d) => (s.keyDocType, s.ex, d.metadata))
                            .SelectMany(x => x.metadata.Select(y => (x.keyDocType, x.ex, mkey: y.key,
                            val: new Dynamic(landrope.common.MetadataType.types[y.key], null))))
                            .Where(s => s.keyDocType == data.keyDocType).ToArray();

                if (dtl.result == null)
                    MakeResult(assg, dtl);
                //return new UnprocessableEntityObjectResult("Detail Penugasan dimaksud belum memiliki dokumen hasil");
                var doc = dtl.result.infoes.FirstOrDefault(d => d.keyDT == typeKey);
                if (doc == null)
                    return new UnprocessableEntityObjectResult("Dokumen dengan jenis dimaksud tidak ada");

                var props = data.props.Join(shouldrcv, p => p.Key, s => s.mkey.ToString("g"), (p, s) => (s.mkey, s.val.type, Value: p.Value.ToString())).ToArray();
                var dataprops = data.props.ToArray();
                doc.props = props.Select(x => new ResultMetaD(x.mkey, new Dynamic(x.type, x.Value))).ToArray();
                doc.exs = data.exis.ConvertBack2();

                ahost.Update(assg).Wait();
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

        //[EnableCors(nameof(landrope))]
        /*		[NeedToken("BPN_ADMIN,BPN_PIC,ADMIN_LAND2,NOT_PIC,ADMIN_LAND1,ARCHIVE_FULL,ARCHIVE_REVIEW,GPS_ADMIN")]
				[HttpGet("result/props")]
				public IActionResult GetProps(string token, string dkey)
				{
					try
					{
						var user = contextplus.FindUser(token);
						var host = HostServicesHelper.GetAssignmentHost(ControllerContext.HttpContext.RequestServices);
						var assign = host.OpenedAssignments().Cast<Assignment>().FirstOrDefault(a => a.details.Any(d => d.key == dkey));
						if (assign == null)
							return new UnprocessableEntityObjectResult("Penugasan tidak ada");
						var detail = assign.details.FirstOrDefault(d => d.key == dkey);
						var persil = detail.persil(contextplus);

						if (detail.results!=null)
						{ }

						var empties = DocWithProps.Templates(assign.step.Value, persil.Discriminator);
						//if (!empties.Any())
						var results = detail.preresults.Join(DocType.List, p => p.info.keyDT, d => d.key,
									(p, d) => new DocWithProps(p.info.keyDT, d.identifier, p.info.props));
							results.Select(x => (x.keyDT, x.identifier, 
										props:x.props.Select(p => (key: p.mkey.ToString("g"), caption: p.mkey.GetDisplayName(), p.val)).ToArray()));
					}
					catch (UnauthorizedAccessException exa)
					{
						return new ContentResult { StatusCode = int.Parse(exa.Message) };
					}
					catch (Exception ex)
					{
						return new UnprocessableEntityObjectResult(ex.Message);
					}
				}*/


        /*		[EnableCors(nameof(landrope))]
				[NeedToken("BPN_ADMIN,NOT_ADMIN,GPS_ADMIN")]
				[HttpGet("dtl/result-doc")]
				public IActionResult GetResultDocs(string token, string akey, string dkey)
				{
					try
					{
						var user = contextplus.FindUser(token);
						var privs = user.privileges.Select(p => p.identifier).ToArray();

						var assg = contextplus.assignments.FirstOrDefault(a => a.key == akey);
						if (assg == null)
							return new UnprocessableEntityObjectResult("Penugasan tidak ada");
						if (assg.invalid == null)
							return NoContent();
						var reqpriv = assg.type switch
						{
							ToDoType.Proc_BPN => "BPN_ADMIN",
							ToDoType.Proc_Non_BPN => "NOT_ADMIN",
							_ => "GPS_ADMIN"
						};
						if (!privs.Contains(reqpriv))
							return new UnprocessableEntityObjectResult("Anda tidak berhak melakukan hal ini");
						var dtl = assg.details.FirstOrDefault(d => d.key == dkey);
						if (dtl == null)
							return new UnprocessableEntityObjectResult("Detail Penugasan tidak ada");

						var results = dtl.preresults.Select(r => (AssignmentResult)r).ToList();
						var layout =

						var data = new BundleFact { exists = vdoclist.Select(d => d.ToView()).ToArray(), missing = reqlist };
						return Ok(data);

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
		*/
        /*		public DynElement[] MakeLayout(DocProcessStep step)
				{
					var sdt = StepDocType.List.FirstOrDefault(s => step == step);
					if (sdt == null)
						return new DynElement[0];
					var keys = sdt.receive.Select(r => r.key).ToArray();
					var doctypes = DocType.List.Join(keys, dt => dt.key, k => k, (dt, k) => dt);
					var dmetas = doctypes.SelectMany(d => d.metadata.Select(m => (d, m)));
					var metas = dmetas.Join(MetadataType.types, d => d.m.key, t => t.Key, (d, t) => (d.d, d.m, t: t.Value))
								.GroupBy(m => m.d.key)
								.Select(g => (g.Key, g.First().d.identifier, metas: g.Select(d =>
																	 (d.m.key,
																	 capt: d.m.key.ToString("g").Replace("_", " "),
																	 type: d.t)).ToArray()))
								.ToArray();

					var reg = new Regex("[a-z]+");
					List<DynElement> elems = new List<DynElement>();
					foreach (var doc in metas)
					{
						foreach (var meta in doc.metas)
						{
							var tname = reg.Match(meta.type.Name.ToLower()).Value;
							var tnest = (tname == "nullable") ? meta.type.GenericTypeArguments[0].Name.ToLower() : "";
							var elem = new DynElement
							{
								visible = "True",
								editable = "True",
								group = doc.identifier,
								label = meta.capt,
								value = $"#{meta.key}",
								xtype = tname switch
								{
									"string" => "Text",
									"int" => "Numeric",
									"decimal" => "Number",
									"bool" => "Check",
									"datetime" => "Date",
									"nullable" => tnest switch
									{
										"int" => "Numeric",
										"decimal" => "Number",
										"bool" => "Check",
										"datetime" => "Date"
										_ => null
									},
									_ => null
								},
								cascade = "",
								correction = false,
								dependency = "",
								inittext = "",
								nullable = true,
								options = ""
							};
							elems.Add(elem);
						}
					}

					var docname = docType.identifier;

					var dyns = combos.Select(k => new DynElement
					{
						visible = "true",
						editable = "true",
						group = $"{docname}|Properties",
						label = k.p.key.ToString("g").Replace("_", " "),
						value = $"#docs.props.${k.p.key}",
						nullable = !k.p.req,
						cascade = "",
						dependency = "",
						correction = false,
						inittext = "",
						options = "",
						swlabels = new string[0],
						type = (k.Value.Name, k.Value.GenericTypeArguments.FirstOrDefault()?.Name) switch
						{
							("Nullable`1", "Int32") => ElementType.Numeric,
							("Nullable`1", "Decimal") => ElementType.Number,
							("Nullable`1", "Boolean") => ElementType.Check,
							("Nullable`1", "DateTime") => ElementType.Date,
							_ => ElementType.Text,
						},
					});

					var exi = new (string key, string type, string label)[]
					{
						("Soft_Copy","Check","Di-scan"),
						("Asli","Check","Asli"),
						("Copy","Numeric","Copy"),
						("Salinan","Numeric","Salinan"),
						("Legalisir","Numeric","Legalisir"),
						("Avoid","Check","Tidak Diperlukan/Memo"),
					};

					var existencies = exi.Select(x => new DynElement
					{
						visible = "true",
						editable = "true",
						group = $"{docname}|Keberadaan",
						label = x.label,
						value = $"#docs.{x.key}",
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
		*/

        [HttpGet("ptsk")]
        public IActionResult GetListPtsk([FromQuery] string token)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextex.FindUser(token);
                var result = contextex.GetDocuments(new { key = "" }, "persils_v2",
                        @"{'$match':
	                        {
	                            $expr:
                                    {
	                                $and:
                                        [

                                        {$ne:['$invalid', true]},
	                                    {$ne:['$basic.current', null]}
	                                ]
	                            }
                              }
                           }",
                                    @"{'$project': {
		                                key : '$basic.current.keyPTSK',
                                        _id : 0
		                                }
                                    }").ToList().ToArray();

                var cleanresult = result.Where(x => x.key != null).Select(x => x.key).ToList().Distinct();

                var ptsks = contextex.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var data = cleanresult.Join(ptsks, p => p, pt => pt.key, (p, pt) => new cmnItem { key = p, name = pt.name }).ToArray();

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

        [HttpGet("penampung")]
        public IActionResult GetListPenampung([FromQuery] string token)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextex.FindUser(token);
                var result = contextex.GetDocuments(new { key = "" }, "persils_v2",
                        @"{'$match':
	                        {
	                            $expr:
                                    {
	                                $and:
                                        [

                                        {$ne:['$invalid', true]},
	                                    {$ne:['$basic.current', null]}
	                                ]
	                            }
                              }
                           }",
                                    @"{'$project': {
		                                key : '$basic.current.keyPenampung',
                                        _id : 0
		                                }
                                    }").ToList().ToArray();

                var cleanresult = result.Where(x => x.key != null).Select(x => x.key).ToList().Distinct();

                var penampung = context.db.GetCollection<Company>("masterdatas").Find("{_t:'pt',invalid:{$ne:true}}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var data = cleanresult.Join(penampung, p => p, pt => pt.key, (p, pt) => new cmnItem { key = p, name = pt.name }).ToArray();

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

        [HttpGet("persil/list2")]
        public IActionResult ListPersil([FromQuery] string token, string akey, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextex.FindUser(token);

                //var asgn = ahost.GetAssignment(akey).GetAwaiter().GetResult() as Assignment;
                var asgn = contextplus.GetCollections(new Assignment(),"assignments", $"<key:'{akey}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                if (asgn == null || asgn.step == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.issued != null)
                    return new UnprocessableEntityObjectResult("Penugasan sudah diissue");
                if (asgn.keyCreator != user.key)
                    return new UnprocessableEntityObjectResult("Anda bukan Kreator Penugasan ini!"); 

                var persils = contextex.GetDocuments(new { key = "", IdBidang = "", _t = "", basic = new PersilBasic() },
                    "persils_v2",
                    "{$match:{en_state:{$in:[null,0]},invalid:{$ne:true},IdBidang:{$ne:null},'basic.current':{$ne:null}}}",
                    "{$project:{key:1,IdBidang:1,_t:1,basic:'$basic.current',_id:0}}").ToList()
                    //.Where(p => !exceptkeys.Contains(p.key))
                    .Where(p => p.basic.keyProject == asgn.keyProject && p.basic.keyDesa == asgn.keyDesa &&
                    (p.basic.keyPTSK != null && p.basic.keyPTSK == asgn.keyPTSK || p.basic.keyPenampung != null && p.basic.keyPenampung == asgn.keyPenampung) &&
                    (p.basic.en_jenis == common.Helpers.CategoryToJenis(asgn.category)))
                    .Select(x => new PersilView
                    {
                        key = x.key,
                        IdBidang = x.IdBidang,
                        pemilik = x.basic.pemilik,
                        nama = x.basic.surat.nama,
                        alias = x.basic.alias,
                        group = x.basic.group,
                        noPeta = x.basic.noPeta,
                        noSurat = x.basic.surat.nomor,
                        luasSurat = x.basic.luasSurat,
                        luasDibayar = x.basic.luasDibayar,
                        keyParent = x.basic.keyParent
                    }).Where(x => string.IsNullOrEmpty(x.keyParent)).ToArray();

                var persilsHibah = contextex.GetDocuments(new { key = "", IdBidang = "", _t = "", basic = new PersilBasic() },
                    "persils_v2_hibah",
                    "{$match:{en_state:{$in:[null,0]},invalid:{$ne:true},IdBidang:{$ne:null},'basic.current':{$ne:null}}}",
                    "{$project:{key:1,IdBidang:1,_t:1,basic:'$basic.current',_id:0}}").ToList()
                    //.Where(p => !exceptkeys.Contains(p.key))
                    .Where(p => p.basic.keyProject == asgn.keyProject && p.basic.keyDesa == asgn.keyDesa &&
                    (p.basic.keyPTSK != null && p.basic.keyPTSK == asgn.keyPTSK || p.basic.keyPenampung != null && p.basic.keyPenampung == asgn.keyPenampung) &&
                    (p.basic.en_jenis == common.Helpers.CategoryToJenis(asgn.category) &&
                    p.basic.en_proses == JenisProses.hibah))
                    .Select(x => new PersilView
                    {
                        key = x.key,
                        IdBidang = x.IdBidang,
                        pemilik = x.basic.pemilik,
                        nama = x.basic.surat.nama,
                        alias = x.basic.alias,
                        group = x.basic.group,
                        noPeta = x.basic.noPeta,
                        noSurat = x.basic.surat.nomor,
                        luasSurat = x.basic.luasSurat,
                        luasDibayar = x.basic.luasDibayar,
                        keyParent = x.basic.keyParent
                    }).Where(x => string.IsNullOrEmpty(x.keyParent)).ToArray();

                persils = persils.Union(persilsHibah).ToArray();
                
                var persilKeys = string.Join(",", persils.Select(x => $"'{x.key}'"));

                //var AllAsgn = ahost.OpenedAssignments(new[] { "*" }).GetAwaiter().GetResult().Cast<Assignment>().ToList()
                //  .Where(x => x.instkey != null && x.details != null);

                var AllAsgn = contextplus.GetCollections(new Assignment(), "assignments", $"<'details.keyPersil' : <$in:[{persilKeys}]>>".MongoJs(), "{_id:0}").ToList();
                var instkeys = AllAsgn.Select(x => x.instkey).ToArray(); 


                var Gs = gcontext.MainInstances.Query(x => x.closed == false && instkeys.Contains(x.key)).ToList();

                var dt = AllAsgn.Join(Gs, a => a.instkey, g => g.key, (a, g) => (a, g)).Where(x => x.g.closed == false).ToList();

                var mains = dt.Select(x => x.g).ToList();

                var nesteds = mains.Select(x => x.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault()).ToList();
                var childs = mains.SelectMany(x => x.children).ToArray();

                var instakeys = nesteds.SelectMany(x => x.instakeys).ToArray();

                //var closeds = nesteds.SelectMany(x => x.closeds).ToArray();

                var chains = instakeys.Join(childs, i => i.Value, c => c.Key, (i, c) => new { dtKey = i.Key, subinstance = c.Value }).ToArray();

                var ps = dt.SelectMany(x => x.a.details).Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToList();

                var keyPersils = chains.Join(ps, c => c.dtKey, p => p.key, (c, p) => new { keyPersil = p.keyPersil, sub = c.subinstance })
                                .Where(x => x.sub.closed == false).Select(x => x.keyPersil).ToArray();

                var datas = persils.Where(x => !keyPersils.Contains(x.key));

                var xlst = ExpressionFilter.Evaluate(datas, typeof(List<PersilView>), typeof(PersilView), gs);
                var data = xlst.result.Cast<PersilView>().ToList();

                return Ok(data.OrderBy(r => r.IdBidang));
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

        //public IActionResult ListPersil([FromQuery] string token, string akey, [FromQuery] AgGridSettings gs)
        //{
        //    try
        //    {
        //        var user = contextplus.FindUser(token);
        //        var asgn = ahost.GetAssignment(akey).GetAwaiter().GetResult() as Assignment;
        //        if (asgn == null || asgn.step == null)
        //            return new NoContentResult();
        //        if (asgn.issued != null)
        //            return new NoContentResult();
        //        if (asgn.keyCreator != user.key)
        //            return new NoContentResult();

        //        (var keyProject, var keyDesa, var keyPTSK, var keyPenampung, var cat, var step) =
        //            (asgn.keyProject, asgn.keyDesa, asgn.keyPTSK, asgn.keyPenampung, asgn.category, asgn.step);
        //        var pnxs = bhost.NextReadiesDiscrete(inclActive: false).GetAwaiter().GetResult()
        //            .Where(p => p.keyProject == keyProject && p.keyDesa == keyDesa &&
        //            (keyPTSK != null && p.keyPTSK == keyPTSK || keyPenampung != null && p.keyPenampung == keyPenampung) && p.cat == cat && p._step == step)
        //            .ToArray();

        //        var persils = contextex.GetDocuments(new { key = "", basic = new PersilBasic() },
        //            "persils_v2",
        //            "{$match:{en_state:{$in:[null,0]},invalid:{$ne:true},IdBidang:{$ne:null},'basic.current':{$ne:null}}}",
        //            "{$project:{key:1,basic:'$basic.current',_id:0}}").ToList();
        //        var data = pnxs.Join(persils, n => n.key, p => p.key,
        //                (n, p) => new PersilView
        //                {
        //                    key = n.key,
        //                    IdBidang = n.IdBidang,
        //                    pemilik = p.basic.pemilik,
        //                    nama = p.basic.surat.nama,
        //                    alias = p.basic.alias,
        //                    group = p.basic.group,
        //                    noPeta = p.basic.noPeta,
        //                    noSurat = p.basic.surat.nomor,
        //                    luasSurat = p.basic.luasSurat,
        //                    luasDibayar = p.basic.luasDibayar
        //                }).ToArray();

        //        var key = new[] { "*" };
        //        var Keys = ahost.OpenedAssignments(key).GetAwaiter().GetResult().Cast<Assignment>().ToList()
        //           .Where(x => x.step == step).SelectMany(x => x.details).Select(x => x.keyPersil).ToList();

        //        var datas = (from c in data
        //                     where !(from o in Keys
        //                             select o).Contains(c.key)
        //                     select c).ToArray();


        //        return Ok(datas.OrderBy(r => r.IdBidang));
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

        /// <summary>
        /// Perbaikan data notaris double
        /// </summary>
        [HttpPost("fix-data-notaris")]
        public IActionResult FixDataNotaris()
        {
            try
            {
                var static_Col = context.GetDocuments(new { KeyLama = "", KeyBaru = "", Identifier = "" }, "static_collections",
                   "{'$match' : {_t : 'PerbaikanDataNotaris'}}",
                   "{'$unwind' : '$details'}",
                   @"{'$project' : {
                        'KeyLama' : '$details.keyLama',
                        'KeyBaru' : '$details.keyBaru',
                        'Identifier' : '$details.identifier',
                        _id : 0 }}"
                   ).ToArray();

                var listKey = static_Col.Select(a => a.KeyLama).Distinct().ToList();
                var assign = contextplus.assignments.Query(p => listKey.Contains(p.keyNotaris)
                                                            || listKey.Contains(p.keyPic)).ToList();

                if (assign?.Count > 0)
                {
                    assign.Where(p => listKey.Contains(p.keyNotaris)).ToList().ForEach(u =>
                    {
                        u.keyNotaris = static_Col.ToList().Find(s => u.keyNotaris == s.KeyLama).KeyBaru;
                    });

                    assign.Where(p => listKey.Contains(p.keyPic)).ToList().ForEach(u =>
                    {
                        u.keyPic = static_Col.ToList().Find(s => u.keyPic == s.KeyLama).KeyBaru;
                    });

                    contextplus.assignments.Update(assign);
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

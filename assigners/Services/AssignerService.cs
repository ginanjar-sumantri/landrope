using auth.mod;
using GenWorkflow;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mongospace;
using protobsonser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracer;
using landrope.mod2;
using landrope.mod3;
using assigner.bridge;
using landrope.hosts.old;
using landrope.engines;
using APIGrid;
using GraphConsumer;
using landrope.consumers;
using landrope.mod3.shared;
using flow.common;
using static landrope.mod2.ExtLandropeContext;
using landrope.common;

namespace assigners
{
	public class AssignerService : Assigner.AssignerBase
	{
		readonly ILogger<AssignerService> _logger;
		IServiceProvider services;
		AssignmentHost AssgHost => services.GetService<IAssignmentHost>() as AssignmentHost;
		ExtLandropeContext extcontext => services.GetService<ExtLandropeContext>();
		LandropePlusContext contextplus => services.GetService<LandropePlusContext>();
		authEntities aucontext => services.GetService<authEntities>();
		GraphHostConsumer ghost => services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

		public AssignerService(IServiceProvider services, ILogger<AssignerService> logger)
		{
			MyTracer.TraceInfo2("Enter...");

			_logger = logger;
			this.services = services;
			MyTracer.TraceInfo2($"services is null: {this.services == null}");
			if (services != null)
			{
				var sb = new StringBuilder();
				sb.Append($"Constructing WorklowService... Inspecting Dependency Injections:");
				sb.Append($"Inspecting...GraphContext: {services.GetService<IAssignmentHost>() != null}");
				sb.Append($"Inspecting...authEntities: {services.GetService<authEntities>() != null}");
				sb.Append($"Inspecting...GraphHostSvc: {services.GetService<ExtLandropeContext>() != null}");
				sb.Append($"Inspecting...GraphHostSvc: {services.GetService<LandropePlusContext>() != null}");
				MyTracer.TraceInfo2(sb.ToString());
			}
			MyTracer.TraceInfo2("Exit...");
		}

		public override Task<BytesValue> OpenedAssignments(ListValue request, ServerCallContext context)
		{
			try
			{
				var keys = request.Values.Select(x => x.StringValue).ToArray();
				//var keys = request.Values.Cast<StringValue>().Select(sv => sv.Value).ToArray();
				var resp = AssgHost.OpenedAssignments(keys);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<AssignedPersilsReturn> AssignedPersils(Empty request, ServerCallContext context)
		{
			try
			{
				var resp = AssgHost.AssignedPersils();
				var ret = new AssignedPersilsReturn();
				ret.Items.AddRange(resp.Select(r=>new AssignedPersilsReturn.Types.AssignedPersil { Key=r.key, Step=(assigner.bridge.DocProcessStep)(int)r.step}));
				return Task.FromResult(ret);
			}
			catch (Exception ex)
			{
				return Task.FromException<AssignedPersilsReturn>(ex);
			}
		}

		public override Task<Empty> Delete(StringValue request, ServerCallContext context)
		{
			try
			{
				string key = request.Value;
				AssgHost.Delete(key);
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<Empty> Add(BytesValue request, ServerCallContext context)
		{
			try
			{
				AssgHost.Add(request.BsonDeserializeBV<Assignment>());
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}

		public override Task<BytesValue> AssignmentList(StringValue request, ServerCallContext context)
		{
			try
			{
				string key = request.Value;
				var resp = AssgHost.AssignmentList(key);
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> GetAssignment(StringValue request, ServerCallContext context)
		{
			try
			{
				string key = request.Value;
				var resp = AssgHost.GetAssignment(key) as Assignment;
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BoolValue> Update(StringValue request, ServerCallContext context)
		{
			try
			{
				string key = request.Value;
				var resp = AssgHost.Update(key);
				return Task.FromResult(new BoolValue { Value=resp});
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}

		public override Task<BoolValue> UpdateEx(UpdateRequest request, ServerCallContext context)
		{
			try
			{
				var resp = AssgHost.Update(request.Assg.BsonDeserializeBS<Assignment>(),request.Save);
				return Task.FromResult(new BoolValue { Value = resp });
			}
			catch (Exception ex)
			{
				return Task.FromException<BoolValue>(ex);
			}
		}

		public override Task<BytesValue> GetAssignmentofDtl(StringValue request, ServerCallContext context)
		{
			try
			{	
				var resp = AssgHost.GetAssignmentOfDtl(request.Value) as Assignment;
				return Task.FromResult(resp.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				return Task.FromException<BytesValue>(ex);
			}
		}

		public override Task<BytesValue> ListAssignmentViews(GAVRequest request, ServerCallContext context)
		{
			var tms = new List<System.TimeSpan>();
			var userkey = request.Userkey;
			try
			{
				var tm = DateTime.Now;
				var users = aucontext.users.All();//
				var gs = request.Gs.BsonDeserializeBS<AgGridSettings>();
				var user = aucontext.users.FirstOrDefault(u => u.key == userkey);
				var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

				var creatorBPN = priv.Contains("BPN_CREATOR");
				var creatorNot = priv.Contains("NOT_CREATOR");
				var creator = creatorBPN || creatorNot;
				var reviewBPN = priv.Contains("BPN_REVIEW");
				var reviewNot = priv.Contains("NOT_REVIEW");
				var review = reviewBPN || reviewNot;
				var actor = priv.Contains("TASK_STEP");
				var monitor = priv.Contains("TASK_VIEW") && !creator;
				var admin = priv.Intersect(new[] { "ADMIN_LAND1", "ADMIN_LAND2", "BPN_ADMIN" }).Any();
				var archive = priv.Intersect(new[] { "ARCHIVE_FULL", "ARCHIVE_REVIEW" }).Any();

				///--- preparation time
				var tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				var Gs1 = ghost.List(user).GetAwaiter().GetResult() ?? new GraphTree[0];
				var Gs = from x in Gs1.AsParallel()
								 select (
								 x.main, 
								 inmain: x.subs.FirstOrDefault(s => s.instance.key == x.main.key)?.nodes,
								 insubs: (from xs in x.subs
													from s in xs.nodes
													where xs.instance.key != x.main.key
													select xs).ToArray());

                var instkeys = Gs.Select(g => g.main.key).ToArray();

                ///--- get instances time
                tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				(var keys, var strict) = (admin || archive || actor || review) ? (instkeys, true) : ( creator|| monitor) ? (new string[0], false) : (new string[0], true);

				var data = AssgHost.OpenedAssignments(keys).Cast<Assignment>().ToArray().AsParallel();

				///--- get assignments time
				tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				//var xlst = ExpressionFilter.Evaluate(posts, typeof(List<Assignment>), typeof(Assignment), gs);
				//var data = xlst.result.Cast<Assignment>().Select(a => a.ToView(contextplus)).ToArray();

				//int skip = (gs.pageIndex - 1) * gs.pageSize;
				//int limit = gs.pageSize > 0 ? gs.pageSize : 0;

				var villages = contextplus.GetVillages().ToArray();
				var companies = contextplus.companies.All().Select(c => new entitas { key = c.key, identity = c.identifier }).ToArray();
				var eusers = users.Select(u => new entitas { key = u.key, identity = u.FullName }).ToArray();
				var notarists = contextplus.notarists.Query().Where(x => x.invalid != true).ToList();
				var internals = contextplus.internals.Query().Where(x => x.invalid != true).ToList();

				//filter penugasan sesuai project di server (ginanjar 17-11-2022)
				var projects = villages.Select(x => x.project.key).Distinct().ToList();
				data = data.Where(x => projects.Contains(x.keyProject)).ToArray().AsParallel();
				//--

				///--- preparation 2 time
				tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				var nxdata = (strict ? data.Join(Gs, a => a.instkey, g => g.main?.key,
									(a, g) => (a, i: g.main,
															nm: g.inmain?.LastOrDefault(),
															ns: g.insubs?.LastOrDefault(s => s.instance.lastState.time == g.insubs?.Max(ss=> ss.instance.lastState.time)))) :
											data.GroupJoin(Gs, a => a.instkey, g => g.main?.key,
																(a, sg) => (a, g: sg.FirstOrDefault()))
																.Select(x => (x.a,
																						i: x.g.main,
																						nm: x.g.inmain?.LastOrDefault(),
																						ns: x.g.insubs?.LastOrDefault(s => s.instance.lastState.time == x.g.insubs?.Max(ss => ss.instance.lastState.time))
																					))).ToArray().AsParallel();

				///--- joining time
				tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				//var ndata = (from x in nxdata
				//			 select (x.a, x.i, x.nm, x.ns, route: x.nm?.routes.FirstOrDefault())).ToArray().AsParallel();

				var ndata = nxdata.Select(x => (x.a, x.i, x.nm, x.ns, route:x.nm?.routes.FirstOrDefault())).ToArray().AsParallel();

				//var ndatax = (
				//                            from x in ndata
				//                            select x.a.ToViewExt(villages, companies, x.route?.key,
				//                             x.nm?.state?.state ?? ToDoState.unknown_,
				//                             $"{x.nm?.state?.state.AsStatus()} {(x.nm?.state?.partial == true ? "(partial)" : "")}", x.nm?.state?.time,
				//                             x.route?._verb ?? ToDoVerb.unknown_, x.route?.branches.Select(b => b._control).ToArray(),
				//                             user.key == x.a.keyCreator, GetTimeFrames(x.i))
				//                         ).ToArray();
				//var ndatax = (
				//						   from x in ndata
				//						   select x.a.ToViewExt2(villages, companies, notarists, internals, x.route?.key,
				//							x.nm?.state, x.ns?.instance.lastState,
				//							x.route?._verb ?? ToDoVerb.unknown_, x.route?.branches.Select(b => b._control).ToArray(),
				//							user.key == x.a.keyCreator, GetTimeFrames(x.i))
				//						).ToArray();

				var ndatax = ndata.Select(x => x.a.ToViewExt2(villages, companies, notarists, internals, x.route?.key, x.nm?.state, x.ns?.instance.lastState,
															x.route?._verb ?? ToDoVerb.unknown_, x.route?.branches.Select(b => b._control).ToArray(),
															user.key == x.a.keyCreator, GetTimeFrames(x.i))).ToArray();

				tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				var sorted = ndatax.GridFeed(gs,null,
								new Dictionary<string, object> { { "role", creator ? 1 : admin ? 3 : archive ? 4 : actor ? 2 : review ? 5 : 0 } } ) 
					as Dictionary<string, object>;

				///--- preparation 2 time
				tm1 = DateTime.Now;
				tms.Add(tm1 - tm);
				tm = tm1;
				///---

				sorted["tms"] = tms.ToArray();

				return Task.FromResult(sorted.BsonSerializeBV());
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return Task.FromException<BytesValue>(ex);
			}

			(DateTime? issued, DateTime? accepted, DateTime? complished) GetTimeFrames(GraphMainInstance i)
			{
				if (i == null)
					return (null, null, null);

				var states = i.states ?? new GraphState[0];
				return (states.LastOrDefault(s => s.state == ToDoState.issued_)?.time,
							states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time,
							states.LastOrDefault(s => s.state == ToDoState.complished_)?.time);
			}
		}

        public override Task<Empty> AssignReload(Empty request, ServerCallContext context)
        {
            try
            {
				AssgHost.ReloadData();
				return Task.FromResult<Empty>(new Empty());
			}
			catch (Exception ex)
			{
				return Task.FromException<Empty>(ex);
			}
		}
    }
}

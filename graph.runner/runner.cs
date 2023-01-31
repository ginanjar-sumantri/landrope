#define _TRIAL_
using System;
using System.Collections.Generic;
using System.Linq;
using AssignerConsumer;
using auth.mod;
using flow.common;
using GenWorkflow;
using GraphConsumer;
using landrope.common;
//using landrope.hosts;
using landrope.mod3;
using landrope.mod3.shared;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using mongospace;

namespace graph.runner
{
	public class Runner
	{
		LandropePlusContext context;
		//public AssignmentHost host;
		GraphHostConsumer graph;
		AssignerHostConsumer asgner;
		ILogger logger;

		public Runner(LandropePlusContext context, /*AssignmentHost host, */GraphHostConsumer graph, AssignerHostConsumer asgner, ILogger logger)
		{
			this.logger = logger;
			this.context = context;
			this.asgner = asgner;
			//this.host = host;
			this.graph = graph;
		}

		Assignment CreateAssignment(AssignmentCore core, user user)
		{
#if _TRIAL_
			var ent = Assignment.Create(context, core, user, false);
#else
			var ent = Assignment.Create(context, core, user);
#endif
			context.assignments.Insert(ent);
			context.SaveChanges();
			return ent;
		}

		(bool ok, string err) AddDetails(Assignment assg, params AssignmentDtlCore[] cores)
		{
			if (cores.Any(c => string.IsNullOrWhiteSpace(c.keyPersil)))
				return (false, "all detail proposals should determine the land area");
			if (cores.Any(c => assg.details.Any(d => d.keyPersil == c.keyPersil)))
				return (false, "some land areas already included");
			foreach (var core in cores)
			{
				var asgdtl = new AssignmentDtl();
				asgdtl.FromCore(core);
				asgdtl.key = mongospace.MongoEntity.MakeKey;
				assg.AddDetail(asgdtl);
			}
			context.assignments.Update(assg);
			context.SaveChanges();
			assg.BeforeIssuing();
			return (true, null);
		}

		static Dictionary<ToDoType, (bool sub, ToDoState state, ToDoState? entrystate, (ToDoVerb verb, ToDoControl cmd)[] verbs)[]> StateVerbsDict =
			new Dictionary<ToDoType, (bool sub, ToDoState state, ToDoState? entrystate, (ToDoVerb verb, ToDoControl cmd)[] verbs)[]>
		{
			{ ToDoType.Proc_Non_BPN,
				new[]{
					(false, ToDoState.created_, (ToDoState?)null, new (ToDoVerb,ToDoControl)[0]),
					(false, ToDoState.issued_, (ToDoState?)null, new[]{ (ToDoVerb.issue_,ToDoControl._) }),
					//(false, ToDoState.bundled_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_,ToDoControl._) }),
					//(false, ToDoState.delegated_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.delegate_,ToDoControl._) }),
					(false, ToDoState.bundleTaken_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_, ToDoControl._), 
											/*(ToDoVerb.delegate_,ToDoControl._)*/ }),
					(false, ToDoState.accepted_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_, ToDoControl._), 
											/*(ToDoVerb.delegate_,ToDoControl._), */(ToDoVerb.accept_,ToDoControl._) }),
					(true, ToDoState.accepted_, ToDoState.accepted_, new (ToDoVerb,ToDoControl)[0]),
					//(true, ToDoState.sentToAdmin_, ToDoState.accepted_, new[]{ (ToDoVerb.sendToAdmin_,ToDoControl._)}),
					(true, ToDoState.resultArchiving_, ToDoState.accepted_, new[]{ /*(ToDoVerb.sendToAdmin_, ToDoControl._), */(ToDoVerb.sendtoArchive_,ToDoControl._)}),
					(true, ToDoState.resultArchived_, ToDoState.accepted_, new[]{ /*(ToDoVerb.sendToAdmin_, ToDoControl._), */(ToDoVerb.sendtoArchive_,ToDoControl._),
											(ToDoVerb.archiveReceive_,ToDoControl.yes_)}),
					(true, ToDoState.resultValidated_, ToDoState.accepted_, new[]{ /*(ToDoVerb.sendToAdmin_, ToDoControl._), */(ToDoVerb.sendtoArchive_,ToDoControl._),
											(ToDoVerb.archiveReceive_,ToDoControl.yes_),(ToDoVerb.archiveValidate_,ToDoControl.approve_)
					}),
				}
			},
			{ ToDoType.Proc_BPN,
				new[]{
					(false, ToDoState.created_, (ToDoState?)null, new (ToDoVerb,ToDoControl)[0]),
					(false, ToDoState.issued_, (ToDoState?)null, new[]{ (ToDoVerb.issue_,ToDoControl._) }),
					(false, ToDoState.formFilling_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_,ToDoControl._) }),
					(false, ToDoState.bundleTaken_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_, ToDoControl._),
											(ToDoVerb.giveBundle_,ToDoControl._) }),
					//(false, ToDoState.bestowed_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_, ToDoControl._),
					//						(ToDoVerb.giveBundle_,ToDoControl._),(ToDoVerb.bestow_,ToDoControl._) }),
					(false, ToDoState.accepted_, (ToDoState?)null, new[]{ (ToDoVerb.issue_, ToDoControl._), (ToDoVerb.bundleComplete_, ToDoControl._),
											(ToDoVerb.giveBundle_,ToDoControl._),/*(ToDoVerb.bestow_,ToDoControl._),*/
											(ToDoVerb.accept_,ToDoControl._)}),
					(true, ToDoState.accepted_, ToDoState.accepted_, new (ToDoVerb,ToDoControl)[0]),
					(true, ToDoState.spsPaid_, ToDoState.accepted_, new[]{ (ToDoVerb.paySPS_,ToDoControl._)}),
					(true, ToDoState.resultArchiving_, ToDoState.accepted_, new[]{ (ToDoVerb.paySPS_, ToDoControl._), (ToDoVerb.sendtoArchive_,ToDoControl._)}),
					(true, ToDoState.resultArchived_, ToDoState.accepted_, new[]{ (ToDoVerb.paySPS_, ToDoControl._), (ToDoVerb.sendtoArchive_,ToDoControl._),
											(ToDoVerb.archiveReceive_,ToDoControl.yes_)}),
					(true, ToDoState.resultValidated_, ToDoState.accepted_, new[]{ (ToDoVerb.paySPS_, ToDoControl._), (ToDoVerb.sendtoArchive_,ToDoControl._),
											(ToDoVerb.archiveReceive_,ToDoControl.yes_),(ToDoVerb.archiveValidate_,ToDoControl.approve_)}),
				}
			}
		};

		(bool ok, string err) Stepping(Assignment assg, ToDoVerb verb, ToDoControl cmd, user user, DateTime? date)
		{
			var inst = assg.instance;
			var type = assg.type.Value;

			(string[] reqprivs, SummData sdata) = (type, verb) switch
			{
				(ToDoType.Proc_BPN, ToDoVerb.issue_) => (new[] { "@CREATOR" }, (SummData)null),
				(ToDoType.Proc_BPN, ToDoVerb.bundleComplete_) => (new[] { "ARCHIVE_FULL" }, null),
				(ToDoType.Proc_BPN, ToDoVerb.giveBundle_) => (new[] { "ADMIN_LAND2" }, null),
				//(ToDoType.Proc_BPN, ToDoVerb.bestow_) => (new[] { "BPN_ADMIN" }, null),
				(ToDoType.Proc_BPN, ToDoVerb.accept_) => (new[] { "BPN_PIC" }, null),
				(ToDoType.Proc_Non_BPN, ToDoVerb.issue_) => (new[] { "@CREATOR" }, null),
				//(ToDoType.Proc_Non_BPN, ToDoVerb.delegate_) => (new[] { "NOT_COORD" }, 
				//					new SummData("ACTOR", 
				//								ToDoState.delegated_.ToString("g"),
				//								PIC.key)),
				(ToDoType.Proc_Non_BPN, ToDoVerb.bundleComplete_) => (new[] { "ARCHIVE_FULL" }, null),
				(ToDoType.Proc_Non_BPN, ToDoVerb.accept_) => (new[] { "NOT_PIC" }, null),
				_ => (new string[0], null)
			};

			if (reqprivs.Length == 0)
				return (false, $"The given verb ({verb}) did not recognizable nor match with the assignment type");
			if (sdata == null)
				sdata = new SummData(null, null, null);

			var tree = graph.FindRoute(inst.key, null, reqprivs).GetAwaiter().GetResult();
			if (tree.main == null || !tree.subs.Any())
				return (false, "The given verb ({verb}) did not match with the assignment current state");
			var routes = tree.subs.SelectMany(s => s.nodes.SelectMany(n => n.routes)).Where(r => r._verb == verb).ToArray();
			var route = routes.FirstOrDefault(r => r.branches.Any(b => b._control == cmd));
			if (route == null)
				return (false, "Combination of given verb ({verb}) and command did not match with any of current routes of the assignment");

			(var ok, var err) = graph.Take(user, inst.key, route.key, date).GetAwaiter().GetResult();

			if (!ok)
				return (ok, err);
			(ok, err) = graph.Summary(user, inst.key, route.key, cmd.ToString("g"), sdata).GetAwaiter().GetResult();
			return (ok, err);
		}

		(bool ok, string err) Stepping(Assignment assg, GraphSubInstance subinst, ToDoVerb verb, ToDoControl cmd, user user, DateTime? date)
		{
			var inst = assg.instance;
			var type = assg.type.Value;

			(string[] reqprivs, SummData sdata) = (type, verb) switch
			{
				(ToDoType.Proc_BPN, ToDoVerb.paySPS_) => (new[] { "ADMIN_LAND2" }, (SummData)null),
				(ToDoType.Proc_BPN, ToDoVerb.sendtoArchive_) => (new[] { "ADMIN_LAND2" }, null),
				(ToDoType.Proc_BPN, ToDoVerb.archiveReceive_) => (new[] { "ARCHIVE_FULL" }, null),
				(ToDoType.Proc_BPN, ToDoVerb.archiveValidate_) => (new[] { "ARCHIVE_REVIEW" }, null),
				(ToDoType.Proc_Non_BPN, ToDoVerb.sendtoArchive_) => (new[] { "ADMIN_LAND1" }, null),
				(ToDoType.Proc_Non_BPN, ToDoVerb.archiveReceive_) => (new[] { "ARCHIVE_FULL" }, null),
				(ToDoType.Proc_Non_BPN, ToDoVerb.archiveValidate_) => (new[] { "ARCHIVE_REVIEW" }, null),
				_ => (new string[0], null)
			};

			if (reqprivs.Length == 0)
				return (false, "The given verb ({verb:g}) is not recognizable nor match with the assignment type");

			var tree = graph.FindRoute(inst.key, subinst.key, reqprivs).GetAwaiter().GetResult();
			if (tree.main == null || !tree.subs.Any())
				return (false, $"The given verb ({verb:g}) is not match with the assignment current state");
			var routes = tree.subs.SelectMany(s => s.nodes.SelectMany(n => n.routes)).Where(r => r._verb == verb).ToArray();
			var route = routes.FirstOrDefault(r => r.branches.Any(b => b._control == cmd));
			if (route == null)
				return (false, "Combination of given verb ({verb:g}) and command are not match with any of current routes of the assignment");

			(var ok, var err) = graph.Take(user, inst.key, route.key, date).GetAwaiter().GetResult();

			if (!ok)
				return (ok, err);
			(ok, err) = graph.Summary(user, inst.key, route.key, cmd.ToString("g"), sdata).GetAwaiter().GetResult();
			return (ok, err);
		}

		(bool ok, string err) StepTo(Assignment assg, bool sub, ToDoState state, user user, DateTime? date)
		{
			var inst = assg.instance;
			var type = assg.type.Value;

			var info = StateVerbsDict[type].FirstOrDefault(d => d.sub == sub && d.state == state);
			if (info.state == ToDoState.unknown_)
				return (false, "Invalid step defined");

			bool ok = true;
			string err = null;

			var entry = info.entrystate;
			if (sub)
			{
				if (entry != null)
				{
					var pre = StateVerbsDict[type].FirstOrDefault(d => !d.sub && d.state == entry.Value);
					if (pre.state == ToDoState.unknown_)
						return (false, "Invalid entry step defined");

					foreach (var v in pre.verbs)
					{
						logger.LogInformation($"Try stepping: {v.verb}/{v.cmd}");
						(ok, err) = Stepping(assg, v.verb, v.cmd, user, date);
						if (!ok)
							return (ok, $"[{v.verb:g}]: {err}");
					}
				}
				foreach (var dtl in assg.details)
				{
					var subinst = graph.GetSub(inst.key, dtl.key).GetAwaiter().GetResult();
					if (subinst == null)
						return (false, "Not all details registered in the assignment flow");

					foreach (var v in info.verbs)
					{
						logger.LogInformation($"Try stepping sub: {dtl.key}/{v.verb}/{v.cmd}");
						(ok, err) = Stepping(assg, subinst, v.verb, v.cmd, user, date);
						if (!ok)
							return (ok, $"[{v.verb:g}]: {err}");
					}
				}
			}
			else
			{
				foreach (var v in info.verbs)
				{
					logger.LogInformation($"Try stepping: {v.verb}/{v.cmd}");
					(ok, err) = Stepping(assg, v.verb, v.cmd, user, date);
					if (!ok)
						return (ok, $"[{v.verb:g}]: {err}");
				}
			}
			return (true, null);
		}

		(bool ok, string err) SubStepTo(Assignment assg, AssignmentDtl dtl, ToDoState state, user user, DateTime? date = null)
		{
			var inst = assg.instance;
			var type = assg.type.Value;

			var info = StateVerbsDict[type].FirstOrDefault(d => d.sub && d.state == state);
			if (info.state == ToDoState.unknown_)
				return (false, "Invalid step defined");

			bool ok = true;
			string err = null;

			var subinst = graph.GetSub(inst.key, dtl.key).GetAwaiter().GetResult();
			if (subinst == null)
				return (false, "The detail was not registered in the assignment flow");
			var entry = info.entrystate;
			if (entry != subinst.lastState?.state)
				return (false, "Entry step not match for the detail");


			foreach (var v in info.verbs)
			{
				logger.LogInformation($"Try stepping sub: {dtl.key}/{v.verb}/{v.cmd}");
				(ok, err) = Stepping(assg, subinst, v.verb, v.cmd, user, date);
				if (!ok)
					return (ok, $"[{v.verb:g}]: {err}");
			}

			return (true, null);
		}


		class SPS_ORDER
		{
			public string type { get; set; }
			public string nomor { get; set; }
			public DateTime date { get; set; }
			public int step { get; set; }
			public string keyPersil { get; set; }
			//public string keyProject { get; set; }
			//public string keyDesa { get; set; }
			//public string keyPTSK { get; set; }
			//public string keyPenampung { get; set; }
		}


		SPS_ORDER[] spses;
		SPS_ORDER[] orders;

		public (bool ok, string err) ImportPenugasan(user user, user NotPIC)
		{
			logger.LogInformation("get the collections...");
			var penugasans = context.GetCollections(new { IdBidang = "", step = DocProcessStep.Baru_Bebas, nomor = (string)null, tanggal = (DateTime?)null },
												"penugasan", "{}", "{_id:0}")
				.ToList();
			var pengantars = context.GetCollections(new { IdBidang = "", skKantah = false }, "pengantar", "{}", "{_id:0}")
				.ToList();
			var persils = context.persils.Query(p => (p.en_state == null || p.en_state == StatusBidang.bebas) && p.invalid != true);

			var total = penugasans.Count;

			var spos = context.GetDocuments(new SPS_ORDER(), "sps2",
				@"{$project:{
						keyPersil:1, 
						type:'$_t',nomor:'$nomer',date:1,step:1,_id:0}}"
				).ToList();
			spses = spos.Where(s => s.type == "SPS").ToArray();
			orders = spos.Where(s => s.type == "ORDER").ToArray();

			logger.LogInformation($"decide the import candidates from {total} records...");
			var candidates = penugasans.Join(persils, a => a.IdBidang, p => p.IdBidang, (a, p) => (persil: p, a, a.step))
				.GroupJoin(pengantars, p => p.persil.IdBidang, k => k.IdBidang,
							(p, sk) => (p.persil, p.a, p.step, skw: sk.FirstOrDefault()?.skKantah ?? true));

			var candidates2a = candidates.Select(x => (
					x.persil.key,
					cat: x.persil.Discriminator.Category(),
					persil: x.persil.basic.current,
					x.a.nomor,
					x.a.tanggal,
					x.step,
					x.skw,
					bpn: x.step.StepType() == ToDoType.Proc_BPN));
			var candidates2b = candidates2a.Select(x => (
				x.persil.keyProject,
				x.persil.keyDesa,
				x.persil.keyPTSK,
				x.persil.keyPenampung,
				x.key,
				x.cat,
				x.bpn,
				x.step,
				x.nomor,
				x.tanggal,
				x.skw));

			var candidates2g = candidates2b.GroupJoin(spos, c => (c.key, (int)c.step), s => (s.keyPersil, s.step), (c, ss) => (c, s: ss.FirstOrDefault()));
			var candidates2 = candidates2g.Select(g => (
							d: g.c,
							tugas_type: g.s?.type,
							tugas_no: g.c.nomor ?? g.s?.nomor,
							tugas_date: g.c.tanggal ?? g.s?.date,
							sps_no: g.s?.nomor,
							sps_date: g.s?.date)).ToArray();

			var total2 = candidates2.Length;
			logger.LogInformation($"Find {total2} cadidates from {total} records...");
			logger.LogInformation($"Try to grouping the cadidates...");

			var grps1 = candidates2.Where(x => x.tugas_type == "ORDER").GroupBy(x => (x.d.bpn, x.tugas_no, x.tugas_date, x.d.step,
												x.d.keyProject, x.d.keyDesa, x.d.keyPTSK, x.d.keyPenampung))
				.Select(g => (g.Key.bpn, g.Key.keyProject, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung,
								g.First().d.cat, g.Key.step, g.Key.tugas_no, g.Key.tugas_date, sps_no: (string)null, sps_date: (DateTime?)null,
								data: g.Select(x => (x.d.key, x.d.skw)).ToArray())).ToArray();
			var grps2 = candidates2.Where(x => x.tugas_type != "ORDER").GroupBy(x => (x.d.bpn, x.d.step,
										x.d.keyProject, x.d.keyDesa, x.d.keyPTSK, x.d.keyPenampung))
				.Select(g => (g.Key.bpn, g.Key.keyProject, g.Key.keyDesa, g.Key.keyPTSK, g.Key.keyPenampung, g.First().d.cat, g.Key.step,
													g.First().tugas_no, g.First().tugas_date, g.First().sps_no,g.First().sps_date,
								data: g.Select(x => (x.d.key, x.d.skw)).ToArray())).ToArray();
			var grps = grps1.Union(grps2).ToArray();

			bool ok = true;
			string err = null;

			int wrapped = 0;
			var list = new List<Assignment>();

			var coll = context.db.GetCollection<BsonDocument>("sps");
			logger.LogInformation("walking through groups...");
			foreach (var g in grps)
			{
				var execdate = g.tugas_date ?? null;
				var createddate = execdate != null ? execdate.Value.AddDays(-14d) : (DateTime?)null;

				var core = new AssignmentCore
				{
					keyProject = g.keyProject,
					keyDesa = g.keyDesa,
					keyPTSK = g.keyPTSK,
					keyPenampung = g.keyPenampung,
					created = DateTime.Now,
					creator = user.key,
					category = g.cat.ToString("g"),
					step = g.step.ToString("g")
				};

				logger.LogInformation($"{core.Parameters()} - Divides into segments, max 150 area per assignments...");
				var segments = g.data.Select((d, i) => (d, seg: i / 150)).GroupBy(x => x.seg)
					.Select(gg => (seg: gg.Key, items: gg.Select(x => x.d).ToArray())).ToArray();

				foreach (var seg in segments)
				{
					logger.LogInformation($"{core.Parameters()} - Creates assignments for {seg.items.Length} areas...");
					var assg = CreateAssignment(core, user);
					if (g.tugas_no != null)
						assg.identifier = g.tugas_no;
					if (g.tugas_date != null)
						assg.created = g.tugas_date;

					list.Add(assg);
					var dtlcores = seg.items.Select(d => new AssignmentDtlCore
					{
						keyPersil = d.key,
						//key=
					}).ToArray();
					logger.LogInformation($"{core.Parameters()} - Set the assignment details");
					AddDetails(assg, dtlcores);
					assg.CalcDuration();

					wrapped += segments.Length;
					logger.LogInformation($"{wrapped}/{total2} persils had been put into {list.Count} assignments");

					logger.LogInformation($"{core.Parameters()} - Steps the flow up to {ToDoState.accepted_:g}...");
					(ok, err) = StepTo(assg, true, ToDoState.accepted_, user, execdate);
					logger.LogInformation($"Steping result: {ok},{err}...");
					if (!ok)
						return (ok, err);

					if (assg.type == ToDoType.Proc_BPN)
					{
						foreach (var dtl in assg.details)
						{
							var sps2 = spses.FirstOrDefault(s => s.keyPersil == dtl.keyPersil && s.step == (int)assg.step);
							if (sps2 == null)
								continue;
							(ok, err) = SubStepTo(assg, dtl, ToDoState.spsPaid_, user, execdate);
							logger.LogInformation($"Steping detail result: {ok},{err}...");
							if (!ok)
								return (ok, err);
							var sps = new SPS { keyPersil = dtl.keyPersil, step = (DocProcessStep)assg.step, nomor = sps2.nomor, date = sps2.date };
							coll.InsertOne(sps.ToBsonDocument());
						}
					}
				}
			}
			return (true, null);
		}

	}
}

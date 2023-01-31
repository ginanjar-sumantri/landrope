using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using auth.mod;
using GenWorkflow;
using GraphHost;
using landrope.common;
using landrope.mcommon;
using landrope.mod2;
using landrope.mod3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace landrope.bundhost
{

	public class BundleHost : IBundleHost
	{
		IConfiguration configuration;
		LandropePlusContext contextplus;
		authEntities authcontext;
		IGraphHostSvc GraphHost;

		ObservableCollection<MainBundle> MainBundles = new ObservableCollection<MainBundle>();
		List<TaskBundle> TaskBundles = new List<TaskBundle>();
		List<MainBundleMem> MainBundleMems = new List<MainBundleMem>();
		ObservableCollection<Assignment> Assignments = new ObservableCollection<Assignment>();

		public BundleHost(IConfiguration configuration, IGraphHostSvc graph)
		{
			this.configuration = configuration;
			authcontext = new authEntities((IConfigurationRoot)configuration);
			contextplus = new LandropePlusContext((IConfigurationRoot)configuration);

			var persils = contextplus.persils.Query(p => p.invalid != true);
			// preload bundles
			MainBundles = new ObservableCollection<MainBundle>(contextplus.mainBundles.Query(b => b.invalid != true)
										.Join(persils, b => b.key, p => p.key, (b, p) => b));
			TaskBundles = contextplus.taskBundles.Query(b => b.invalid != true)
										.Join(MainBundles, b => b.keyParent, p => p.key, (b, p) => b).ToList();
			Assignments = new ObservableCollection<Assignment>(contextplus.assignments.Query(a => a.invalid != null && a.closed == null));

			MainBundleMems = MainBundles.Select(b => new MainBundleMem(b)).ToList();
			MainBundles.CollectionChanged += MainBundles_CollectionChanged;
			MainBundles.ToList().ForEach(b =>
			{
				b.doclist.CollectionChanged += (s, e) => Doclist_CollectionChanged(b, s, e);
				b.doclist.ToList().ForEach(d => d.reservations.CollectionChanged += (s, e) => Reservations_CollectionChanged(b, d, s, e));
			});
		}

		private void Reservations_CollectionChanged(MainBundle mb, BundledDoc d, object sender, NotifyCollectionChangedEventArgs e)
		{
			if (mb == null)
				return;
			var mem = MainBundleMems.FirstOrDefault(b => b.source?.key == mb.key);
			if (mem == null)
				return;
			var dest = mem.docs.FirstOrDefault(dm => dm.source.keyDocType == d.keyDocType);
			if (dest != null)
				dest.Reload();
		}

		private void Doclist_CollectionChanged(MainBundle mb, object sender, NotifyCollectionChangedEventArgs e)
		{
			if (mb == null)
				return;
			var mem = MainBundleMems.FirstOrDefault(b => b.source?.key == mb.key);
			if (mem == null)
				return;
			var source = sender as BundledDoc;
			if (source == null)
				return;
			var dest = mem.docs.FirstOrDefault(d => d.source.keyDocType == source.keyDocType);
			if (dest != null)
				dest.Reload();
		}

		private void MainBundles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					MainBundleMems.AddRange(e.NewItems.Cast<MainBundle>().Select(b => new MainBundleMem(b))); break;
				case NotifyCollectionChangedAction.Remove:
					var keys = e.OldItems.Cast<MainBundle>().Select(b => b.key).ToList();
					keys.ForEach(k =>
					{
						var pos = MainBundleMems.FindIndex(b => b.source?.key == k);
						if (pos != -1)
							MainBundleMems.RemoveAt(pos);
					}); break;
				case NotifyCollectionChangedAction.Reset:
					MainBundleMems = MainBundles.Select(b => new MainBundleMem(b)).ToList(); break;
			}
		}

		public MainBundle MainGet(string key)
		{
			var bundle = MainBundles.FirstOrDefault(b => b.key == key);
			if (bundle == null && contextplus.persils.FirstOrDefault(p => p.key == key) != null)
			{
				bundle = new MainBundle(contextplus, key);
				MainBundles.Add(bundle);
				contextplus.mainBundles.Insert(bundle);
				contextplus.SaveChanges();
			}
			return bundle;
		}

		public TaskBundle TaskGet(string key)
						=> TaskBundles.FirstOrDefault(b => b.key == key);

		public RegDoc[] Available(string key)
		{
			var bundle = MainGet(key);
			return bundle == null ? new RegDoc[0] : bundle.Available();
		}

		public RegDoc[] Reserved(string key, GraphHostSvc graph)
		{
			var bundle = MainGet(key);
			if (bundle == null)
				return new RegDoc[0];

			var assigns = contextplus.assignments.Query(a => a.invalid != true);
			var instances = assigns.Select(a => (a,inst:graph.Get(a.instkey)))
				.Where(x=>!x.inst.closed).ToList();

			var tbuns = TaskBundles.Where(t => t.keyParent == key && t.keyAssignment != null);
		}

		public RegDoc[] Current(string key)
		{
			var bundle = MainGet(key);
			return bundle == null ? new RegDoc[0] : bundle.Current();
		}

		public (TaskBundle bundle, bool complete, string reason) MakeTaskBundle(string token, string asgdtlkey, bool preview, bool rebuild)
		{
			var user = contextplus.FindUser(token);
			var asgn = contextplus.assignments.FirstOrDefault(a => a.details.Any(d => d.key == asgdtlkey));
			if (asgn == null)
				return (null, false, "Penugasan tidak ada");
			var asgdtl = asgn.details.First(d => d.key == asgdtlkey);
			var persil = asgdtl.persil(contextplus);
			if (persil == null)
				return (null, false, "Bidang yang dipilih tidak ada");

			lock (persil) //prevents another creation of taskbundle for the same persil/main bundle
			{
				var bundle = MainGet(asgdtl.keyPersil);
				if (bundle == null)
					return (null, false, "Bundle utama tidak ada");
				if (asgn.step == null)
					return (null, false, "Bundle utama belum diset secara benar");
				var step = asgn.step.Value;

				var child = TaskBundles.FirstOrDefault(b => b.keyParent == asgn.key);//bundle.children().FirstOrDefault(b => b.keyParent == asgn.key);
				if (child != null)
				{
					if (!rebuild)
					{
						var comp = !(child.reqlist.Any() || child.spcreqs.Any());
						return (child, comp, "Ada kebutuhan dokumen yang belum terpenuhi");
					}
					if (!preview)
					{
						TaskBundles.Remove(child);
						contextplus.taskBundles.Remove(child);
						contextplus.SaveChanges();
					}
				}

				var tbun = new TaskBundle(bundle, asgn);
				var stepreq = StepDocType.GetItem(step, persil.Discriminator);
				if (stepreq == null)
					return (null, false, "Kebutuhan dokumen untuk proses dan kategori ini tidak didefinisikan");
				var reqlist = stepreq.send;
				tbun.reqlist = reqlist;

				var spcreqs = bundle.doclist.SelectMany(d => d.SpecialReqs.Select(r => (d.keyDocType, r)))
												.Where(r => r.r.step == step).ToList();

				var bdocs = bundle.doclist.SelectMany(d => d.current.Select(c => (d.keyDocType, key: c.Key, c.Value.props, c.Value.exists)))
						.SelectMany(x => x.exists.Select(xx => (x.keyDocType, x.key, x.props, xx.ex, xx.cnt)))
						.Where(x => x.cnt > 0);

				var bdocs1 = bdocs.Join(reqlist.Where(r => r.req), d => (d.keyDocType, d.key, d.ex), r => (r.keyDocType, r.key, r.ex),
										(d, r) => (d.keyDocType, d.key, d.props, d.ex)).ToList();

				var spcr = spcreqs.Select((r, i) => (r, i)).ToArray();
				foreach (var rq in spcr)
				{
					var rkey = rq.r.r.prop.Key;
					var rval = rq.r.r.prop.Value;
					var rex = rq.r.r.exis;

					var doc = bdocs1.FirstOrDefault(d => d.keyDocType == rq.r.keyDocType && d.ex == rex && d.props.Keys.Any(k => k == rkey));
					if (doc.key == null || doc.props == null)
						continue;
					spcreqs.RemoveAt(rq.i);
				}

				var bdocs2 = bdocs.Join(spcreqs, d => (d.keyDocType, d.ex), s => (s.keyDocType, s.r.exis),
										(d, s) => (d, s))
					.Where(x => x.d.props.Any(p => p.Key == x.s.r.prop.Key && p.Value == x.s.r.prop.Value))
					.Select(x => (x.d.keyDocType, x.d.key, x.d.props, x.d.ex))
					.ToList();

				spcr = spcreqs.Select((r, i) => (r, i)).ToArray();
				foreach (var rq in spcr)
				{
					var rkey = rq.r.r.prop.Key;
					var rval = rq.r.r.prop.Value;
					var rex = rq.r.r.exis;

					var doc = bdocs2.FirstOrDefault(d => d.keyDocType == rq.r.keyDocType && d.ex == rex && d.props.Keys.Any(k => k == rkey));
					if (doc.key == null || doc.props == null)
						continue;
					spcreqs.RemoveAt(rq.i);
				}


				var doclist = bdocs1.Union(bdocs2).GroupBy(d => d.keyDocType)
					.Select(g => (keyDocType: g.Key, docs: g.Select(d => (d.key, d.props, d.ex)).ToArray()))
					.ToArray();

				tbun.doclist = doclist.Select(d => new DocSeries
				{
					keyDocType = d.keyDocType,
					docs = new TaskParticleDocChain(
						d.docs.Select(dd =>
							new KeyValuePair<string, TaskParticleDoc>(
								dd.key,
								new TaskParticleDoc(dd.props, dd.ex)
							)
						))
				}).ToArray();
				tbun.reqlist = reqlist.Except(doclist.SelectMany(d => d.docs.Select(dd =>
									 new DocRequirement { keyDocType = d.keyDocType, key = dd.key, ex = dd.ex }))).ToArray();
				tbun.spcreqs = spcreqs.Select(s => new DocProcessReq(s.keyDocType, s.r)).ToArray();

				var complete = !tbun.reqlist.Any() && !tbun.spcreqs.Any();
				var reas = "";
				if (tbun.reqlist.Any())
					reas = "Ada kebutuhan dokumen yang belum tersedia; ";
				if (tbun.spcreqs.Any())
					reas += "Ada kebutuhan khusus dokumen yang belum tersedia";

				if (!preview && complete)
				{
					if (!bundle.Reserve(tbun))
					{
						bundle.AbortReserve(asgn.key);
						return (tbun, false, "Gagal mencadangkan dokumen pada bundle utama");
					}
					TaskBundles.Add(tbun);
					contextplus.mainBundles.Update(bundle);
					contextplus.taskBundles.Insert(tbun);
					contextplus.SaveChanges();
				}

				return (tbun, complete, reas);
			}
		}

		public void Realize(string token, string key, string asgnkey)
		{
			var user = contextplus.FindUser(token);
			var bundle = MainGet(key);
			if (bundle == null)
				return;
			foreach (var regdoc in bundle.doclist)
				regdoc.Realize(asgnkey, user.key);
			contextplus.mainBundles.Update(bundle);
			contextplus.SaveChanges();
		}

		public bool Reserve(string token, string key, string asgnkey)
		{
			var user = contextplus.FindUser(token);
			var bundle = MainGet(key);
			if (bundle == null)
				return false;
			var assignment = contextplus.assignments.FirstOrDefault(a => a.key == asgnkey);
			if (assignment == null)
				return false;
			var step = assignment.step;
			if (step == null)
				return false;

			var persil = bundle.persil();
			var required = StepDocType.GetItem(step.Value, persil.Discriminator)?
											.send.Select(s => (keyDT: s.key, exis: (s.ex, s.req)))
											.GroupBy(s => s.keyDT).Select(g => (keyDT: g.Key, exs: g.Select(d => d.exis).ToArray()))
											.ToArray();
			var OKs = bundle.doclist.Join(required, d => d.keyDocType, r => r.keyDT, (d, r) => d.Reserve(asgnkey, r.exs));
			if (OKs.Any(o => !o))
			{
				bundle.AbortReserve(asgnkey);
				return false;
			}
			contextplus.mainBundles.Update(bundle);
			contextplus.SaveChanges();
			return true;
		}

		public void AddProcessRequirement(string token, string key, DocProcessStep step, string keyDocType, KeyValuePair<string, Dynamic> prop, Existence ex)
		{
			var user = contextplus.FindUser(token);
			var bundle = MainGet(key);
			if (bundle == null)
				throw new Exception("Bundle tidak ada");
			var docl = bundle.doclist.First(d => d.keyDocType == keyDocType);
			if (docl == null)
				throw new Exception("Jenis dokumen tidak ada dalam bundle");
			if (docl.SpecialReqs.Any(p => p.exis == ex && p.step == step && p.prop.Key == prop.Key))
				throw new Exception("Request untuk proses, property dan eksistensi dimaksud sudah ada");

			var req = new ProcessReq { exis = ex, step = step, prop = prop };
			docl.SpecialReqs.Add(req);

			contextplus.mainBundles.Update(bundle);
			var tbun = TaskBundles.FirstOrDefault(b => b.keyParent == key);
			if (tbun != null)
			{
				var lst = tbun.spcreqs.ToList();
				lst.Add(new DocProcessReq(keyDocType, req));
				tbun.spcreqs = lst.ToArray();
				contextplus.taskBundles.Update(tbun);
			}
			contextplus.SaveChanges();
		}

		public IEnumerable<PersilNextReady> NextReadies()
		{
			var facts=MainBundleMems.Select(b => (bundle: b.source, b.docs)).ToArray();
			StepDocType.GetItem
		}
	}
}

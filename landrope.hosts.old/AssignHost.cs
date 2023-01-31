using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using auth.mod;
using GenWorkflow;
/*using GraphHost;*/
using landrope.common;
using landrope.documents;
using landrope.engines;
using landrope.mod3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyInjection;
using GraphConsumer;
using System.Threading.Tasks;
using mongospace;
using landrope.consumers;

namespace landrope.hosts.old
{
	public class AssignmentHost : IAssignmentHost
	{
		IConfiguration configuration;
		mod3.LandropePlusContext contextplus;
		authEntities authcontext;
		IServiceProvider services;
		IServiceProvider scopeds;
		IServiceScope scope;

		GraphHostConsumer graph => services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

		public GraphHostConsumer GetGraphHost() => graph;

		ObservableCollection<Assignment> Assignments = new ObservableCollection<Assignment>();

		public List<IAssignment> OpenedAssignments(string[] keys)
		{
			if (keys!=null && keys.Length == 0)
				return new List<IAssignment>();

			var all = keys==null || keys.Contains("*");
			var qry = Assignments.Where(a => a.invalid != true && a.closed == null);
			if (all)
				return qry.ToList<IAssignment>();
			return qry.Where(a=>keys.Contains(a.instkey)).ToList<IAssignment>();
		}

		public IEnumerable<(string key, DocProcessStep step)> AssignedPersils()
		{
			var asgns = Assignments.Where(a => a.invalid != true && a.closed == null).ToArray();
			var combos = asgns.SelectMany(a => a.details.Select(d => (a, a.instkey, d, dtlkey: d.key))).ToArray();
			var combokeys = combos.Select(x => (x.instkey, x.dtlkey)).ToArray();
			var chains = graph.GetSubs(combokeys).GetAwaiter().GetResult().Where(c => c.sub != null && !c.sub.closed);
			var result = combos.Join(chains, co => (co.instkey, co.dtlkey), ch => (ch.instkey, ch.dockey),
				(co, ch) => (co.a, co.d, ch.sub))
				.Select(x => (x.d.keyPersil, x.a.step.Value))
				.ToArray();

			return result;
		}

		public AssignmentHost(IConfiguration config, IServiceProvider services)
		{
			this.configuration = config;
			this.services = services;
			this.scope = services.CreateScope();
			this.scopeds = scope.ServiceProvider;

			authcontext = scopeds.GetService<authEntities>();
			contextplus = scopeds.GetService<mod3.LandropePlusContext>();

			//graph = services.GetService<GraphHostConsumer>();

			Assignments = new ObservableCollection<Assignment>(contextplus.assignments.Query(a => a.invalid != true)
								.Where(a => a.closed == null)
								.Select(a => a.Inject(graph)));
			Assignments.CollectionChanged += Assignments_CollectionChanged;
		}

		public void Delete(string key)
		{
			var pos = Assignments.ToList().FindIndex(a => a.key == key);
			if (pos != -1)
			{
				var ass = Assignments[pos];
				if (ass.instkey != null)
				{
					var T = graph.Get(ass.instkey);
					T.Wait();
					var oldins = T.Result;
					if (oldins != null)
						graph.Del(ass.instkey).Wait();
				}
				Assignments.RemoveAt(pos);
				contextplus.assignments.Remove(ass);
				contextplus.SaveChanges();
			}
		}

		public void Add(Assignment assg)
		{
			//var graph = new GraphHostConsumer();
			if (assg.instkey == null && assg.type.HasValue)
			{
				var T = graph.Create(assg.keyCreator, assg.type.Value);
				T.Wait();
				assg.instkey = T.Result.key;
			}
			Assignments.Add(assg);
			contextplus.assignments.Insert(assg);
			contextplus.SaveChanges();

		}

		public void ReloadData()
		{
			Assignments.Clear();
			Assignments = new ObservableCollection<Assignment>(contextplus.assignments.Query(a => a.invalid != true)
										.Where(a => a.closed == null)
										.Select(a => a.Inject(graph)));
		}

		private void Assignments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Reset:
					Assignments = new ObservableCollection<Assignment>(contextplus.assignments.Query(a => a.invalid != true)
										.Where(a => a.closed == null)
										.Select(a => a.Inject(graph)));
					Assignments.CollectionChanged += Assignments_CollectionChanged;
					break;
			}
		}

		public List<IAssignment> AssignmentList(string key)
		{
			return Assignments.Where(a => a.invalid != true && a.details.Any(d => d.keyPersil == key)).Distinct(Assignment.comparer)
					.Where(a => GetGraphHost().Get(a.instkey).GetAwaiter().GetResult()?.closed == false)
					.ToList<IAssignment>();
		}

		public IAssignment GetAssignment(string key)
			=> Assignments.FirstOrDefault(a => a.key == key);

		public IAssignment GetAssignmentOfDtl(string dtlkey)
			=> Assignments.FirstOrDefault(a => a.details.Any(d=>d.key == dtlkey));

		public bool Update(string akey)
		{
			var assg = GetAssignment(akey) as Assignment;
			if (assg == null)
				return false;
			contextplus.assignments.Update(assg);
			contextplus.SaveChanges();
			return true;
		}

		public bool Update(IAssignment assg, bool dbSave = true)
		{
			if (assg == null)
				return false;
			var pos = Assignments.Select((a, i) => (a.key, i)).FirstOrDefault(x => x.key == ((Assignment)assg).key);
			if (pos.key == null)
				return false;
			Assignments[pos.i] = (Assignment)assg;

			contextplus.assignments.Update((Assignment)assg);
			if (dbSave)
				contextplus.SaveChanges();
			return true;
		}

		public void ContextCommit()
		{
			contextplus.SaveChanges();
		}

		public void ContextRollback()
		{
			contextplus.DiscardChanges();
		}

		/*		public (string key, string name) ProjectOfDesa(string keyDesa)
				{
					(var proj,var des) = contextplus.GetVillage(keyDesa);
					return (proj.key, proj.identity);
				}*/

		/*		static Dictionary<string, (int order, DocProcessStep step)[]> StepOrders = new Dictionary<string, (int order, DocProcessStep step)[]>
						{
							{ "persilHGB",new[]{ (0, DocProcessStep.Akta_Notaris), (1,DocProcessStep.AJB), (2, DocProcessStep.Balik_Nama) } },
							{ "persilSHM",new[]{ (0, DocProcessStep.Akta_Notaris), (1, DocProcessStep.Penurunan_Hak), (2, DocProcessStep.AJB), (3, DocProcessStep.Balik_Nama) } },
							{ "persilSHP",new[]{ (0, DocProcessStep.Akta_Notaris), (1, DocProcessStep.Peningkatan_Hak), (2, DocProcessStep.AJB), (3, DocProcessStep.Balik_Nama) } },
							{ "persilGirik",new[]{ (0, DocProcessStep.Akta_Notaris), (1, DocProcessStep.PBT_Perorangan), (2, DocProcessStep.SPH),
																			(3, DocProcessStep.PBT_PT),(4, DocProcessStep.SK_BPN),(5, DocProcessStep.Cetak_Buku),   } },
							{ "PersilHibah",new[]{ (0, DocProcessStep.AJB_Hibah), (1, DocProcessStep.SHM_Hibah), (2, DocProcessStep.Penurunan_Hak),
																			(3, DocProcessStep.AJB), (4, DocProcessStep.Balik_Nama) } }		
						};

						public static DocProcessStep GetLatets(string disc, params DocProcessStep[] steps)
						{
							(int order, DocProcessStep step)[] sto = null;
							if (!StepOrders.TryGetValue(disc, out sto))
								return DocProcessStep.Belum_Bebas;
							return sto.Join(steps,o=>o.step,s=>s,(o,s)=>o).OrderBy(s => s.order).Last().step;
						}*/
	}
}

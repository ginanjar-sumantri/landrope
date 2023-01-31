using DnsClient;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Tracer;

namespace landrope.mod
{
	public interface IMemoryContext
	{
		IEnumerable<Project> Raw { get; }
		IEnumerable<T> GetMap<T>(Expression<Func<Project, bool>> filter = null, Expression<Func<Project, T>> projection = null)
										where T : landbase;
		void Reload();
		void SetReloadTime(TimeSpan? next, TimeSpan? interval = null);
	}

	public class MemoryContext : IMemoryContext
	{
		List<Project> projects;
		List<EventWaitHandle> waiters = new List<EventWaitHandle>();

		public MemoryContext()
		{
			LoadAll();
		}

		void Wait()
		{
			EventWaitHandle evnt;
			lock (waiters)
			{
				if (loaded)
					return;
				evnt = new AutoResetEvent(false);
				waiters.Add(evnt);
			}
			evnt.WaitOne();
		}

		void Complish()
		{
			loaded = true;
			lock (waiters)
			{
				waiters.ForEach(w => w.Set());
			}
		}

		public IEnumerable<T> GetMap<T>(Expression<Func<Project, bool>> filter = null, Expression<Func<Project, T>> projection = null)
					where T : landbase
		{
			if (!loaded)
				Wait();
			var qry = projects.AsQueryable();
			if (filter != null)
			{
				var filfunc = filter.Compile();
				qry = qry.Where(p => filfunc.Invoke(p));
			}
			IQueryable<T> qry2;
			if (projection != null)
			{
				var prjfunc = projection.Compile();
				qry2 = qry.Select(p => prjfunc.Invoke(p));
			}
			else
				qry2 = qry.Cast<T>();
			return qry2.ToList();
		}

		bool loaded = false;

		public IEnumerable<Project> Raw
		{
			get
			{
				if (!loaded)
					Wait();
				return projects;
			}
		}

		void LoadAll()
		{
			try
			{
				MyTracer.TraceInfo2("Memory Context Loading, opening DB...");
				var context = new LandropeContext();
				context.db.GetCollection<Project>("maps").FindAsync<Project>("{invalid:{$ne:true}}").ContinueWith(p =>
				{
					if (p.Exception == null)
					{

					}
					projects = p.Result.ToList();
					var landls = context.GetCollections(new Land(), "lands", "{invalid:{$ne:true}}", "{_id:0}").ToList();
					projects.ForEach(pp =>
					{
						var slands = landls.Where(ll => pp.villages.Select(v => v.key).ToArray().Contains(ll.vilkey));
						pp.villages.ForEach(v =>
						{
							var vlands = slands.Where(xl => xl.vilkey == v.key);
							v.lands = vlands.ToList();
						});
						//pp.villages.GroupJoin(slands, v => v.key, ll => ll.vilkey, (v, sl) => SetLands(ref v,sl));
					});
					loaded = true;
					Complish();
				});
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				loaded = true;
				Complish();
				return;
			}
		}

		Village SetLands(ref Village v, IEnumerable<Land> lands)
		{
			v.lands = lands.ToList();
			return v;
		}

		Village AddLand(ref Village v, Land land)
		{
			v.lands.Add(land);// = lands.ToList();
			return v;
		}

		public void Reload()
		{
			if (!loaded)
				return;
			loaded = false;
			LoadAll();
		}

		System.Threading.Timer timerInit;
		public void SetReloadTime(TimeSpan? next, TimeSpan? interval=null)
		{
			TimeSpan t1 = next ?? new TimeSpan(-1);
			TimeSpan t2 = interval ?? new TimeSpan(-1);
			if (timerInit != null)
				timerInit.Change(t1, t2);
			else
				timerInit = new System.Threading.Timer(new TimerCallback(ontimer), null, t1, t2);
		}

		void ontimer(object state)
		{
			Reload();
		}
	}
}


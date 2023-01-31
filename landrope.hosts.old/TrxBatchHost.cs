using auth.mod;
using GenWorkflow;
using landrope.common;
using landrope.documents;
using landrope.engines;
using landrope.mod3;
using landrope.mod3.classes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using GraphConsumer;

namespace landrope.hosts.old
{
	public class TrxBatchHost : ITrxBatchHost
	{
		IConfiguration configuration;
		LandropePlusContext contextplus;
		authEntities authcontext;
		GraphHostConsumer graph;

		public ObservableCollection<trxBatch> batches = new ObservableCollection<trxBatch>();

		public TrxBatchHost(IConfiguration configuration)
		{
			this.configuration = configuration;
			//this.GraphHost = graph;

			authcontext = new authEntities((IConfigurationRoot)configuration);
			contextplus = new mod3.LandropePlusContext((IConfigurationRoot)configuration);
			graph = new GraphHostConsumer();

			batches = new ObservableCollection<trxBatch>(contextplus.batches.Query(b => b.invalid != true).Select(b => b.Inject(graph)));
			batches.CollectionChanged += Batches_CollectionChanged;
		}

		private void Batches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					contextplus.batches.Insert(e.NewItems.Cast<trxBatch>().ToArray());
					contextplus.SaveChanges();
					break;
				case NotifyCollectionChangedAction.Remove:
					contextplus.batches.Remove(e.OldItems.Cast<trxBatch>().ToArray());
					contextplus.SaveChanges();
					break;
				case NotifyCollectionChangedAction.Reset:
					batches = new ObservableCollection<trxBatch>(contextplus.batches.Query(b => b.invalid != true).Select(b => b.Inject(graph)));
					batches.CollectionChanged += Batches_CollectionChanged;
					break;
			}
		}

		public ITrxBatch Get(string key) => batches.FirstOrDefault(b => b.key == key);

		public List<ITrxBatch> ListByDocType(string key, bool activeOnly = false)
		{
			var ret = batches.Where(b => b.invalid != null && b.details.Any(d => d.keyDocType == key));
			if (activeOnly)
				ret = ret.Where(b => b.instkey != null && !b.Instance.closed);
			return ret.ToList<ITrxBatch>();
		}
		public List<ITrxBatch> ListByPersil(string key, bool activeOnly = false)
		{ 
			var ret = batches.Where(b => b.invalid != null && b.details.Any(d => d.key == key));
			if (activeOnly)
				ret = ret.Where(b => b.instkey != null && !b.Instance.closed);
			return ret.ToList<ITrxBatch>();
		}
		public List<ITrxBatch> ListByTrxType(TrxType type, bool activeOnly = false)
		{
			IEnumerable<trxBatch> qry = type switch
			{
				/*				TrxType.Masuk => typeof(entryBatch),*/
				TrxType.Peminjaman => batches.OfType<lendBatch>(),
				TrxType.Pengembalian => batches.OfType<returnBatch>(),
				_ => batches.OfType<entryBatch>()
			};
			var ret = qry.Where(b => b.invalid != null);
			if (activeOnly)
				ret = ret.Where(b => b.instkey != null && !b.Instance.closed);
			return ret.ToList<ITrxBatch>();
		}

		public List<ITrxBatch> ListActive()
			=> batches.Where(b => b.invalid != true && b.instkey != null && !b.Instance.closed).ToList<ITrxBatch>();

	}
}

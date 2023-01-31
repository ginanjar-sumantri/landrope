using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GenWorkflow;
using landrope.consumers;
using landrope.documents;
using landrope.engines;
using landrope.mod3.shared;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using Microsoft.Extensions.DependencyInjection;

namespace landrope.mod3
{
	[Entity("dealBundle", "bundles")]
	public class DealBundle : Bundle, IDealBundle
	{
		public DocSeries[] doclist { get; set; } = new DocSeries[0];
		public DocRequirement[] missing { get; set; } = new DocRequirement[0];
		public DocProcessReq[] spcreqs { get; set; } = new DocProcessReq[0];

		public string keyParent { get; set; }
		public string keyPraDeals { get; set; }
		public DealBundle()
			: base() { }

		public DealBundle(MainBundle parent, PraPembebasan praBebas)
		{
			key = MakeKey;
			keyParent = parent.key;
			keyPraDeals = praBebas.key;
		}

		public MainBundle parent(IBundlerHostConsumer host) => host != null ? (MainBundle)host.MainGet(keyParent).GetAwaiter().GetResult() : 
			MyContext().mainBundles.FirstOrDefault(b => b.key == keyParent);
		//public PraPembebasan praBebas() => keyPraDeals==null? null : (PraPembebasan)host..GetAssignment(keyAssignment).GetAwaiter().GetResult();
		//public PraPembebasan praBebas() => keyPraDeals == null ? null : (PraPembebasan)context

		public PraPembebasan praPembebasan(LandropePlusContext context) => context.praDeals.FirstOrDefault(p => p.key == keyPraDeals);
		public bool? obsolete { get; set; } = null;

		[BsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		protected bool? _realized { get; set; } = null;

		public bool? Realized {
			get => _realized;
			set { 
				var chg = value != _realized && value==true; 
				_realized = value;
				if (chg && OnRealized.GetInvocationList().Any())
					OnRealized.Invoke(this, new EventArgs());
			}
		}

		public event EventHandler OnRealized;

		public bool IsComplete() => Realized==true || missing?.Any() == true || spcreqs?.Any() == true;

		//public void ToCore(BundleCore core, LandropePlusContext context)
		//{
		//	base.ToCore(core);
		//	if (core is DealBundleCore dcore)
		//		dcore.praBebas = praPembebasan(context)?.ToCore();
		//}

		public DealBundleView ToView(IBundlerHostConsumer host, LandropePlusContext context)
		{
			var praDeal = praPembebasan(context);
			var parent = this.parent(host);
			var user =  (praDeal == null ? null : praDeal.manager);
			var v = new DealBundleView();
			(v.praDealKey, v.NoRequest, v.Manager, v.Sales, v.Mediator)
				=
			(praDeal.key, praDeal.identifier, praDeal.manager, praDeal.sales, praDeal.mediator);
			return v;
		}
	}
}

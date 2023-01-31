using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using landrope.common;
using landrope.documents;
using landrope.engines;
using landrope.mod2;
using landrope.mod3.shared;
using MongoDB.Driver;
using mongospace;

namespace landrope.mod3
{
	[Entity("mainBundle", "bundles")]
	public class MainBundle : Bundle, IMainBundle
	{
		public string IdBidang { get; set; }
		public string storkey { get; set; }
		public ObservableCollection<BundledDoc> doclist { get; set; }
		public Persil persil() => MyContext().persils.FirstOrDefault(p => p.key == key);

		public MainBundle()
			:base()
		{

		}

		public MainBundle(LandropePlusContext context, string keyPersil, string IdBidang=null)
		{
			var disc = context.GetCollections(new { _t = "" }, "persils_v2", $"{{key:'{keyPersil}'}}", "{_id:0,_t:1}").FirstOrDefault();
			if (disc == null)
				throw new InvalidOperationException();

			key = keyPersil;

			this.IdBidang = IdBidang!=null? IdBidang : persil()?.IdBidang;
			var keys = StepDocType.KeyDocTypePerDiscs[disc._t];
			doclist = new ObservableCollection<BundledDoc>(keys.Select(k=>new BundledDoc(k)));
		}

		public RegDoc[] Current()
			=>doclist.Select(d => new RegDoc(d.keyDocType, d.current)).ToArray();

		public override void ToCore(BundleCore core)
		{
			var context = MyContext();
			base.ToCore(core);
			if (core is MainBundleCore mcore) {
				var persil = this.persil()?.basic.current;
				var loc = context.GetCollections(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
					"villages", $"{{'village.key':'{persil?.keyDesa}'}}",
					"{_id:0,'project.key':1,'project.identity':1,'village.key':1,'village.identity':1}").FirstOrDefault();

				(mcore.IdBidang, mcore.noPeta, mcore.project, mcore.desa,
						mcore.storkey) =
					(IdBidang, persil?.noPeta, loc?.project?.identity, loc?.village?.identity, storkey);
			}
		}

		public override void FromCore(BundleCore core)
		{
			base.FromCore(core);
			if (core is MainBundleCore mcore)
				storkey = mcore.storkey;
		}

		public MainBundleView ToView()
		{
			var v = new MainBundleView();
			(v.key, v.IdBidang, v.noPeta, v.storkey) = 
				(key,IdBidang,persil()?.basic?.current?.noPeta,storkey);
			return v;
		}
	}

	[Entity("preBundle", "bundles")]
	public class PreBundle : Bundle, IPreBundle
    {
		public string IdBidang { get; set; }
		public string storkey { get; set; }
		public ObservableCollection<BundledDoc> doclist { get; set; }
		public Persil persil() => MyContext().persils.FirstOrDefault(p => p.key == key);

		public PreBundle()
			: base()
		{

		}

		public PreBundle(LandropePlusContext context, string keyPersil, string IdBidang = null)
		{
			var disc = context.GetCollections(new { _t = "" }, "persils_v2", $"{{key:'{keyPersil}'}}", "{_id:0,_t:1}").FirstOrDefault();
			if (disc == null)
				throw new InvalidOperationException();

			key = keyPersil;

			this.IdBidang = IdBidang != null ? IdBidang : persil()?.IdBidang;
			var keys = StepDocType.KeyDocTypePerDiscs[disc._t];
			doclist = new ObservableCollection<BundledDoc>(keys.Select(k => new BundledDoc(k)));
		}

		public RegDoc[] Current()
			=> doclist.Select(d => new RegDoc(d.keyDocType, d.current)).ToArray();

		public override void ToCore(BundleCore core)
		{
			var context = MyContext();
			base.ToCore(core);
			if (core is MainBundleCore mcore)
			{
				var persil = this.persil()?.basic.current;
				var loc = context.GetCollections(new { project = new { key = "", identity = "" }, village = new { key = "", identity = "" } },
					"villages", $"{{'village.key':'{persil?.keyDesa}'}}",
					"{_id:0,'project.key':1,'project.identity':1,'village.key':1,'village.identity':1}").FirstOrDefault();

				(mcore.IdBidang, mcore.noPeta, mcore.project, mcore.desa,
						mcore.storkey) =
					(IdBidang, persil?.noPeta, loc?.project?.identity, loc?.village?.identity, storkey);
			}
		}

		public override void FromCore(BundleCore core)
		{
			base.FromCore(core);
			if (core is MainBundleCore mcore)
				storkey = mcore.storkey;
		}

		public MainBundleView ToView()
		{
			var v = new MainBundleView();
			(v.key, v.IdBidang, v.noPeta, v.storkey) =
				(key, IdBidang, persil()?.basic?.current?.noPeta, storkey);
			return v;
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using landrope.common;
using landrope.documents;
using landrope.engines;
using landrope.mod.shared;
using landrope.mod3;
using landrope.mod3.shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using mongospace;

namespace landrope.mod3
{
	[Entity("stepDocType", "step_jnsDok")]

	public class StepDocType : entity3
	{
		public string disc { get; set; }
		public DocCompleteStep step { get; set; }
		[BsonIgnore]
		public DocProcessStep _step => step.ToProcessStep();
		public AssignmentTeam team { get; set; }
		public DocRequirement[] send { get; set; }
		public DocRequirement[] receive { get; set; }
		public DocRequirement rec_primary { get; set; }

		public static StepDocType[] List;

		public static StepDocType GetItem(DocProcessStep step, string disc)
		{
			var cstp = step.ToCompleteStep(); 
			return List.FirstOrDefault(sd => sd.invalid != true && sd.step == cstp && sd.disc == disc);
		}

		//public static DocProcessStep? Matching(MainBundle bundle)
		//{
		//	if (bundle == null)
		//		return null;
		//	var tbun = 
		//	foreach (var docl in bundle.doclist) {
		//		var actresv = docl.reservations.Where(r => r.aborted == null && r.realized == null);
		//		foreach (var resv in actresv)
		//		{
		//			var asgn = resv.keyAssignment
		//		}
		//	}

		//var currents = bundle.Current();
		//	if (currents == null || currents.Length == 0)
		//		return null;

		//}

		public static Dictionary<string, string[]> KeyDocTypePerDiscs;
		static StepDocType()
		{
			var context = namedentity3.Injector<LandropePlusContext>();
			if (context == null)
				context = LandropePlusContext.current;

			List = context.stepdocs.All().ToArray();
			KeyDocTypePerDiscs = List.SelectMany(sd =>
				sd.send.Where(ss=>ss.keyDocType!= "JDOK053").Select(ss => (sd.disc, ss.key)).Union(
					sd.receive.Select(ss => (sd.disc, ss.key))
				)
			).Distinct().GroupBy(d => d.disc).ToDictionary(g => g.Key, g => g.Select(d => d.key).ToArray());
		}
	}

	[Entity("Bundle", "bundles")]
	[BsonKnownTypes(typeof(MainBundle), typeof(TaskBundle), typeof(PreBundle))]
	public class Bundle : entity3
	{
		//[BsonIgnore]
		//[Newtonsoft.Json.JsonIgnore]
		//[System.Text.Json.Serialization.JsonIgnore]
		//protected IBundleHost host { get; set; }

		public T Inject<T>(IBundleHost host) where T:Bundle
		{
			//if (this.host == null) this.host = host;
			return (T)this;
		}

		public Bundle() { }
		//public Bundle(IBundleHost host)
		//{
		//	this.host = host;
		//}

		public virtual void ToCore(BundleCore core)
		{
			(core.key, core.invalid) =
				(key, invalid);
		}

		public virtual void FromCore(BundleCore core)
		{
			(key, invalid) =
				(core.key, core.invalid);
		}
	}
}

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
	[Entity("taskBundle", "bundles")]
	public class TaskBundle : Bundle, ITaskBundle
	{
		public DocSeries[] doclist { get; set; } = new DocSeries[0];
		public DocRequirement[] missing { get; set; } = new DocRequirement[0];
		public DocProcessReq[] spcreqs { get; set; } = new DocProcessReq[0];

		public string keyParent { get; set; }
		public string keyAssignment { get; set; }
		//public TandaTerima ttberkas { get; set; }
		//public List<BerkasBase> berkasmasuk { get; set; }

		IAssignerHostConsumer ahost => ContextService.services.GetService<IAssignerHostConsumer>(); 
		public TaskBundle()
			: base() { }

		//public TaskBundle(IBundleHost host)
		//	: base(host)
		//{
		//}

		//public TaskBundle(LandropePlusContext context, string keyAssignment, string keyPersil)
		//{
		//	var assignment = ahost.GetAssignment(keyAssignment).GetAwaiter().GetResult();
		//	//context.assignments.FirstOrDefault(a => a.key == keyAssignment);
		//	if (assignment == null)
		//		throw new InvalidDataException();

		//	var main = context.mainBundles.FirstOrDefault(b => b.key == keyPersil);
		//	if (main == null)
		//		throw new InvalidDataException();

		//	this.keyAssignment = keyAssignment;
		//	this.keyParent = main.key; // eq to keyPersil

		//	//(this.doclist, this.reqlist) = GetDocLists(main, assignment);
		//}

		//public TaskBundle(IBundleHost host, string keyAssignment, string keyPersil)
		//	: this()
		//{
		//	var assignment = ahost.GetAssignment(keyAssignment).GetAwaiter().GetResult() as Assignment;
		//	if (assignment == null)
		//		throw new InvalidDataException();

		//	var main = (MainBundle)host.MainGet(keyPersil);
		//	if (main == null)
		//		throw new InvalidDataException();

		//	this.keyAssignment = keyAssignment;
		//	this.keyParent = main.key; // eq to keyPersil
		//}

		public TaskBundle(MainBundle parent, Assignment assignment)
		{
			key = MakeKey;
			keyParent = parent.key;
			keyAssignment = assignment.key;
		}

		public MainBundle parent(IBundlerHostConsumer host) => host != null ? (MainBundle)host.MainGet(keyParent).GetAwaiter().GetResult() : 
			MyContext().mainBundles.FirstOrDefault(b => b.key == keyParent);
		public Assignment assignment() => keyAssignment==null? null : (Assignment)ahost.GetAssignment(keyAssignment).GetAwaiter().GetResult();

		public bool Issued() => assignment()?.issued != null;
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

/*		public static (DocSeries[], DocRequirement[]) GetDocLists(MainBundle parent, Assignment assignment)
		{
			//return (new DocSeries[0], new DocX[0]);
			var step = assignment.step;
			if (step == null)
				throw new Exception("Penugasan belum diset dengan benar");

			var persil = parent.persil();
			var disc = persil?.Discriminator;

			var sdreqs = StepDocType.GetItem(step.Value, disc)?.send;
			if (sdreqs == null || !sdreqs.Any())
				return (new DocSeries[0], new DocRequirement[0]);

			var docreqs = sdreqs.Where(s => s.req).Select(s => new DocRequirement { keyDocType = s.keyDocType, ex = s.ex, req = s.req });

			var doclists = parent.doclist.Select(d => (
							reserve: (d.keyDocType, reservations: d.reservations.FirstOrDefault(r => r.keyAssignment == assignment.key && r.realized == null)),
							series: new DocSeries { keyDocType = d.keyDocType, docs = d.entries.LastOrDefault()?.Item }
						));

			var reserved = (doclists.Any(d => d.reserve.reservations != null));
			var series = doclists.SelectMany(d => d.series.docs.SelectMany(
								dd => dd.Value.exists.Select(dx => (d.series.keyDocType, dx.ex, dx.cnt))).ToArray());
			var reserve = doclists.SelectMany(d => d.reserve.reservations.items.SelectMany(i => i.reserved));
			if (reserved)
				series = series.Join(reserve, s => (s.keyDocType, s.ex), r => (r.keyDocType, r.reservations.), (s, r) => s)

			var xdocs = (allreserved.Any()) ? allseries.Join(allreserved, s => s.keyDocType, r => r..Select(d => (d.docs, d.reserve))
											 .Select(dd => (dd.docs.keyDocType, dd.docs.docs.Join(dd.reserve.items, d => d.Key, r => r.key, (d, r) => d)))
											 .Select(dd => new DocSeries { keyDocType = dd. })

			var reserve = doclists.Select(d => d.reserve);
			if (reserve != null)
			{
				reserve.Select(r => r.items.Join(doclists, r => r.key, d => d.docs)
			}

			var stepdt = StepDocType.List.FirstOrDefault(s => s.disc == disc && s.step == step);
			if (stepdt == null)
				return (new DocSeries[0], new DocRequirement[0]);
			var reqdoclist = stepdt.send;

			var jdoclist = reqdoclist.GroupJoin(doclists, rd => rd.key, d => d.keyDocType, (rd, sd) => (rd, xdoc: sd.FirstOrDefault()));

			var xdoclist = jdoclist.Where(j => j.xdoc.keyDocType != null && j.xdoc.docs != null).Select(j => j.xdoc).ToArray();
			var rdoclist = jdoclist.Where(j => j.xdoc.keyDocType == null || j.xdoc.docs == null).Select(j => j.rd).ToArray();
			return (xdoclist, rdoclist);
		}*/

		public override void ToCore(BundleCore core)
		{
			base.ToCore(core);
			if (core is TaskBundleCore tcore)
				tcore.assignment = assignment()?.ToCore();
		}

		public TaskBundleView ToView(IBundlerHostConsumer host)
		{
			var assgn = assignment();
			var parent = this.parent(host);
			var user = assgn==null? null : assgn.keyPIC == null ? null : 
							MyContext().users.FirstOrDefault(u => u.key == assgn.keyPIC);
			var v = new TaskBundleView();

			(v.key, v.IdBidang, v.noPeta, v.noAssignment, v.step) =
				(key, parent?.IdBidang, parent.persil()?.basic?.current?.noPeta, assgn?.identifier,assgn?.step);
			return v;
		}
	}
}

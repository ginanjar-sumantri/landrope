using auth.mod;
using DynForm.shared;
using landrope.common;
using landrope.mod2;
using landrope.mod3.shared;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace landrope.documents
{
	using InnerDocCoreList = List<ParticleDocCore>;

	public class DocType
	{
		public string key { get; set; }
		public string identifier { get; set; }
		public bool? invalid { get; set; }
		public string Multiply { get; set; }
		public string tahunan { get; set; }
		public MetadataReq[] metadata { get; set; }

		public static DocType[] List;
		static DocType()
		{
			var context = namedentity2.Injector<ExtLandropeContext>();
			List = context.GetCollections(new DocType(), "jnsDok", "{invalid:{$ne:true}}", "{_id:0}").ToList().ToArray();
		}
	}

	//public class DocExists
	//{
	//	//public List<InnerDoc> docs { get; set; } = new List<InnerDoc>();
	//	//public Existency[] exists { get; set; } = NoExistencies;
	//}

	public class ParticleDocProp
	{
		[BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.Document)]
		public Dictionary<string, Dynamic> props { get; set; } = new Dictionary<string, Dynamic>();

		public static ParticleDocProp Distinct(ParticleDocProp[] items)
		{
			var idp = new ParticleDocProp();
			var first = items.FirstOrDefault();
			if (first == null)
				return idp;
			//idp.reqs = first.reqs;

			idp.props = items.SelectMany(i => i.props).Where(p => p.Value?.val != null && p.Value.type != Dynamic.ValueType.Null)
						.GroupBy(i => i.Key).Select(g => new KeyValuePair<string, Dynamic>(g.Key, g.Max(d => d.Value)))
						.ToDictionary(x => x.Key, x => x.Value);
			return idp;
		}
	}

	public class ParticleDoc : ParticleDocProp
	{
		[BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.Document)]
		public Dictionary<string, bool> reqs { get; set; } = new Dictionary<string, bool>();
		//public string DmsId { get; set; }
		public Existency[] exists { get; set; } = NoExistencies;
		public static Existency[] NoExistencies =>
					Enum.GetValues(typeof(Existence)).Cast<Existence>().Select(x => new Existency { ex = x, cnt = 0 }).ToArray();

		public ParticleDoc()
		{
		}

		public ParticleDoc(ParticleDocProp prop, Existency[] exists)
		{
			//reqs = prop.reqs;
			props = prop.props;
			exists = (Existency[])exists.Clone();
		}

		public ParticleDocCore ToCore(string key)
		{
			var core = new ParticleDocCore();
			(core.key, core.props, core.reqs, core.exists) =
				(key, props.Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value?.Value)).ToDictionary(l => l.Key, l => l.Value),
				reqs, exists);
			return core;
		}

		public void FromCore(ParticleDocCore core)
		{
			(exists, props) =
			(core.exists,
			core.props.Select(kv => new KeyValuePair<string, Dynamic>(kv.Key, new Dynamic(kv.Value))).ToDictionary(l => l.Key, l => l.Value));
		}

		public static string GetCoreKey(ParticleDocCore core)
		{
			var key = core.key;
			if (key.StartsWith("TMP_"))
				key = MongoEntity.MakeKey;
			return key;
		}

		public static explicit operator ParticleDoc(ParticleDocCore core)
		{
			var idoc = new ParticleDoc();
			idoc.FromCore(core);
			return idoc;
		}
	}

	public class TaskParticleDoc : ParticleDocProp
	{
		public Existence exis { get; set; }

		public TaskParticleDoc()
		{
		}

		public TaskParticleDoc(Dictionary<string, Dynamic> props, Existence exis)
		{
			this.props = props;
			this.exis = exis;
		}

		public ParticleDoc ToParticleDoc()
			=> new ParticleDoc(this, new Existency[] { new Existency { ex = this.exis, cnt = 1 } });
		/*		public ParticleDocCore ToCore(string key)
				{
					var core = new ParticleDocCore();
					(core.key, core.props, core.reqs, core.exists) =
						(key, props.Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value?.Value)).ToDictionary(l => l.Key, l => l.Value),
						reqs, exists);
					return core;
				}

				public void FromCore(ParticleDocCore core)
				{
					(exists, props) =
					(core.exists,
					core.props.Select(kv => new KeyValuePair<string, Dynamic>(kv.Key, new Dynamic(kv.Value))).ToDictionary(l => l.Key, l => l.Value));
				}

				public static string GetCoreKey(ParticleDocCore core)
				{
					var key = core.key;
					if (key.StartsWith("TMP_"))
						key = MongoEntity.MakeKey;
					return key;
				}

				public static explicit operator ParticleDoc(ParticleDocCore core)
				{
					var idoc = new ParticleDoc();
					idoc.FromCore(core);
					return idoc;
				}
		*/
	}

	//public class DocChain
	//{
	//	public string Key { get; set; }
	//	public ParticleDoc Doc { get; set; }
	//}

	public class ParticleDocChain : Dictionary<string, ParticleDoc>
	{
		public ParticleDocChain()
			: base()
		{ }

		public ParticleDocChain(IEnumerable<KeyValuePair<string, ParticleDoc>> src)
			: base(src)
		{
		}

		public ParticleDocChain(Dictionary<string, ParticleDoc> src)
			: base(src)
		{
		}

		public ParticleDocChain(ParticleDoc src)
			: base(new[] { new KeyValuePair<string, ParticleDoc>(ObjectId.GenerateNewId().ToString(), src) })
		{
		}

		public InnerDocCoreList ToCore()
		{
			return new InnerDocCoreList(this.Select(d => d.Value.ToCore(d.Key)));
		}

		public void FromCore(InnerDocCoreList core)
		{
			var list = core.Select(d => new KeyValuePair<string, ParticleDoc>(ParticleDoc.GetCoreKey(d), (ParticleDoc)d))
				.ToList();
			//.ToDictionary(d => d.Key, d => d.Value);
			Clear();
			list.ForEach(l => Add(l.Key, l.Value));
		}

		public static explicit operator ParticleDocChain(InnerDocCoreList core) =>
			new ParticleDocChain(core.Select(d => new KeyValuePair<string, ParticleDoc>(ParticleDoc.GetCoreKey(d), (ParticleDoc)d)));

		public ParticleDocChain MakeCopy() => new ParticleDocChain(this.ToArray().ToList());
	}

	public class FixExistencesDocChain : Dictionary<string, int[]>
	{
		public FixExistencesDocChain()
			: base()
		{ }

		public FixExistencesDocChain(IEnumerable<KeyValuePair<string, int[]>> src)
			: base(src)
		{
			if (src.Select(d => d.Value.Length).Any(l => l != 6))
				throw new InvalidOperationException("Array length is invalid");
		}

		public FixExistencesDocChain(Dictionary<string, int[]> src)
			: base(src)
		{
			if (src.Select(d => d.Value.Length).Any(l => l != 6))
				throw new InvalidOperationException("Array length is invalid");
		}

		/*		public ParticleDocChain ToParticleDocChain()
					=> new ParticleDocChain( this.Select(tpd => new KeyValuePair<string, ParticleDoc>(tpd.Key, tpd.Value.ToParticleDoc())) );*/
		/*		public InnerDocCoreList ToCore()
				{
					return new InnerDocCoreList(this.Select(d => d.Value.ToCore(d.Key)));
				}

				public void FromCore(InnerDocCoreList core)
				{
					var list = core.Select(d => new KeyValuePair<string, ParticleDoc>(ParticleDoc.GetCoreKey(d), (ParticleDoc)d))
						.ToList();
					//.ToDictionary(d => d.Key, d => d.Value);
					Clear();
					list.ForEach(l => Add(l.Key, l.Value));
				}

				public static explicit operator ParticleDocChain(InnerDocCoreList core) =>
					new ParticleDocChain(core.Select(d => new KeyValuePair<string, ParticleDoc>(ParticleDoc.GetCoreKey(d), (ParticleDoc)d)));

				public ParticleDocChain MakeCopy() => new ParticleDocChain(this.ToArray().ToList());*/
	}

	[BsonDiscriminator("DocChainVal")]
	[BsonKnownTypes(typeof(ValDocList))]
	public class DocChainVal : ValidateEntry<ParticleDocChain>
	{
		public override ParticleDocChain MakeItemCopy() => Item.MakeCopy();
	}
	[BsonDiscriminator("ValDocList")]
	public class ValDocList : DocChainVal
	{
		public static ValDocList MakeCopy(ValDocList other)
			=> JsonConvert.DeserializeObject<ValDocList>(JsonConvert.SerializeObject(other));
	}

	public interface IRegDoc
	{
		string keyDocType { get; set; }
		DocType docType()
		{
			try
			{
				return DocType.List.FirstOrDefault(d => d.key == keyDocType);
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}

	public class RegDoc : IRegDoc
	{
		public string keyDocType { get; set; }
		public ParticleDocChain docs { get; set; }

		public RegDoc(string keyDocType, ParticleDocChain list)
		{
			this.keyDocType = keyDocType;
			docs = list;
		}
	}

	public class BundledDoc : Validates<ParticleDocChain>, IRegDoc
	{
		public string keyDocType { get; set; }
		//[BsonIgnore]
		//public DocType docType => DocType.List.FirstOrDefault(d => d.key == keyDocType);
		//public List<InnerDoc> docs { get; set; } = new List<InnerDoc>();
		//public ValDocEx[] Existencies { get; set; } = new ValDocEx[0];

		public ObservableCollection<ProcessReq> SpecialReqs { get; set; } = new ObservableCollection<ProcessReq>();
		public object reservations { get; set; }

		public DocType docType => ((IRegDoc)this).docType();

		public BundledDoc()
		{
			current = new ParticleDocChain();
		}

		public BundledDoc(string keyDocType)
			: this()
		{
			this.keyDocType = keyDocType;
		}

		public ParticleDoc AddDoc()
		{
			var inn = new ParticleDoc();
			if (((IRegDoc)this).docType() != null)
				docType.metadata.ToList().ForEach(m =>
				{
					var key = m.key.ToString("g");
					inn.props.Add(key, null);
					inn.reqs.Add(key, m.req);
				});
			//docs.Add(inn);
			return inn;
		}

		public void AddExistence(string userkey, ParticleDocChain dex, string source, ChangeKind kind = ChangeKind.Add)
		{
			var val = new DocChainVal
			{
				kind = kind,
				created = DateTime.Now,
				keyCreator = userkey,
				Item = dex,
				reviewed = DateTime.Now,
				keyReviewer = userkey,
				approved = true,
				sourceFile = source
			};
			Add(val);
			Summarize();
		}

		public void UpdateExistence(string userkey, ParticleDocChain dex, string source, DateTime timestamp, ChangeKind kind = ChangeKind.Update)
		{
			var val = new DocChainVal
			{
				kind = kind,
				created = timestamp,
				keyCreator = userkey,
				Item = dex,
				sourceFile = source
			};
			Add(val);
			Summarize();
		}

		public int DelExistence(string source)
		{
			int count = 0;
			while (true)
			{
				var val = this.entries.LastOrDefault(e => e.sourceFile == source);
				if (val == null)
					break;
				if (Del(val))
					count++;
			}
			Summarize();
			return count;
		}

		public ParticleDocChain Summarize(bool valid = true)
		{
			var ordereds = valid ? entries.Where(ex => ex.approved == true).OrderBy(x => x.created) :
											entries.OrderBy(x => x.created);
			var last = ordereds.LastOrDefault();
			if (last == null)
			{
				if (valid)
					current.Clear();
				return new ParticleDocChain();
			}

			var start = ordereds.LastOrDefault(x => x.kind == ChangeKind.Update);
			if (start == null)
				start = ordereds.FirstOrDefault();

			var items = last.Item.GroupJoin(start.Item, x => x.Key, y => y.Key,
													(x, sy) => new KeyValuePair<string, ParticleDoc>(x.Key,
															new ParticleDoc
															{
																props = x.Value.props,
																reqs = x.Value.reqs,
																exists = sy.DefaultIfEmpty().First().Value?.exists ?? ParticleDoc.NoExistencies
															}));

			var list = ordereds.Where(x => x.created > start.created);
			var adds = list.Where(x => x.kind == ChangeKind.Add).SelectMany(x => x.Item.SelectMany(xx => xx.Value.exists.Select(xxx => (xx.Key, xxx.ex, xxx.cnt))));
			var subs = list.Where(x => x.kind == ChangeKind.Delete).SelectMany(x => x.Item.SelectMany(xx => xx.Value.exists.Select(xxx => (xx.Key, xxx.ex, cnt: -xxx.cnt))));

			var exitems = items.SelectMany(i => i.Value.exists.Select(d => (i.Key, d.ex, d.cnt))).Union(adds).Union(subs)
									.GroupBy(x => (x.Key, x.ex))
									.Select(g => (g.Key.Key, g.Key.ex, cnt: g.Sum(d => d.cnt)))
									.GroupBy(x => x.Key)
									.Select(g => (g.Key, exists: g.Select(d => new Existency { ex = d.ex, cnt = d.cnt }).ToArray()));
			items = items.GroupJoin(exitems, i => i.Key, e => e.Key,
				(i, se) => new KeyValuePair<string, ParticleDoc>(i.Key, new ParticleDoc
				{
					props = i.Value.props,
					reqs = i.Value.reqs,
					exists = se.DefaultIfEmpty().First().exists ?? ParticleDoc.NoExistencies
				}));
			var result = new ParticleDocChain(items);
			if (valid)
				this.current = result;
			return result;
		}

		/*		public (string key, Existence ex, int cnt)[] GetReservations()
					=> reservations.Where(r => r.since <= DateTime.Now && r.realized == null && r.aborted == null)
											.SelectMany(r => r.items.SelectMany(d => d.reserved.Select(x => (d.key, x.ex, x.cnt)))).ToArray();
		*/
		/*		public (string key, Existence ex, int cnt)[] Available()
					=> current.SelectMany(c => c.Value.exists.Select(d => (c.Key, d.ex, d.cnt)))
						.GroupJoin(GetReservations(), c => (c.Key, c.ex), r => (r.key, r.ex),
								(c, sr) => (c.Key, c.ex, cnt: c.cnt - sr.DefaultIfEmpty().First().cnt)).ToArray();
		*/
		/*		public (string key, ParticleDocProp props)[] AllDocs()
					=> entries.Where(e => e.approved == true).Select(e => e.Item.ToList())
								.SelectMany(i => i.Select(kv => (kv.Key, kv.Value)))
								.GroupBy(k => k.Key).Select(g => (g.Key, ParticleDocProp.Distinct(g.Select(d => d.Value).ToArray()))).ToArray();

				public ParticleDocChain AvailableD()
				{
					var avls = Available().GroupBy(a => a.key).Select(g => (g.Key, exists: g.Select(d => new Existency(d.ex, d.cnt)).ToArray()));
					var dprops = AllDocs();
					var dls = avls.Join(dprops, a => a.Key, d => d.key, (a, d) => new KeyValuePair<string, ParticleDoc>(a.Key, new ParticleDoc(d.props, a.exists)));
					return new ParticleDocChain(dls);
				}

				public ParticleDocChain Reserved()
				{
					var resvs = GetReservations().GroupBy(a => a.key).Select(g => (g.Key, exists: g.Select(d => new Existency(d.ex, d.cnt)).ToArray()));
					var dprops = AllDocs();
					var dls = resvs.Join(dprops, a => a.Key, d => d.key, (a, d) => new KeyValuePair<string, ParticleDoc>(a.Key, new ParticleDoc(d.props, a.exists)));
					return new ParticleDocChain(dls);
				}

				// key = series doc item
				public bool Reserve(string keyAssignment, (string key, Existence ex, bool req)[] docs)
				{
					//var rdocs = docs.Select(d => (d.ex, d.req, cnt: d.req ? 1 : 0))
					//						.Join(AllDocs(), d => 1, a => 1, (d, a) => (a.key, d.ex, d.req, d.cnt));

					var exi = Available();
					var qexi = exi.GroupJoin(docs, e => (e.key, e.ex), d => (d.key, d.ex),
						(c, sd) => (c.key, c.ex, c.cnt, d: sd.DefaultIfEmpty().First()))
						.Select(x => (x.key, x.ex, x.d.req, cnt: x.cnt - (x.d.req? 1:0)));

					if (qexi.Any(x => x.cnt < 0 && x.req))
						return false;

					var rsvdocs = qexi.Where(q => q.cnt >= 0 || !q.req).GroupBy(q => q.key)
						.Select(g => new ParticleDocReserve { key = g.Key, reserved = g.Select(d => new Existency(d.ex, d.cnt)).ToArray() })
						.ToArray();

					var reserve = new DocReservation { keyAssignment = keyAssignment, since = DateTime.Now, items = rsvdocs };
					//reservations.Add(reserve);
					return true;
				}

				public bool Reserve(string keyAssignment, (Existence ex, bool req)[] docs)
				{
					var keys = AllDocs().Select(d => d.key).Distinct();
					var ndocs = docs.Join(keys, d => 1, k => 1, (d, k) => (key: k, d.ex, d.req)).ToArray();
					return Reserve(keyAssignment, ndocs);
				}*/
		/*		public bool Reserve(TaskBundle tbun)
				{
					var akey = tbun.assignment.key;
					var doc = tbun.doclist.FirstOrDefault(d => d.keyDocType == this.keyDocType);
					if (doc == null)
						return true;
					var exis=doc.docs.Select(d => (key:d.Key, ex:d.Value.exis, req:true)).ToArray();
					return Reserve(akey, exis);
				}

		*/
		/*		public bool Realize(string keyAssignment, string keyUser)
				{
					var resv = reservations.FirstOrDefault(r => r.keyAssignment == keyAssignment);
					if (resv == null)
						return false;
					var avail = current;
					var val = new DocChainVal
					{
						created = DateTime.Now,
						reviewed = DateTime.Now,
						keyCreator = keyUser,
						kind = ChangeKind.Delete,
						keyReviewer = keyUser,
						approved = true,
						Item = new ParticleDocChain(resv.items.Join(avail, r => r.key, a => a.Key,
							(r, a) => new KeyValuePair<string, ParticleDoc>(r.key, new ParticleDoc { props = a.Value.props, reqs = a.Value.reqs, exists = r.reserved })))
					};
					entries = entries.Add(val);
					Summarize();
					return true;
				}
		*/
		protected override void WhenValidated()
		{
			Summarize();
		}

		public RegisteredDocCore ToCore()
		{
			var docs = Summarize(false);

			var core = new RegisteredDocCore();
			(core.keyDocType, core.docType, core.docs) =
			(keyDocType, docType.identifier,
			new InnerDocCoreList(docs.Select(d => d.Value.ToCore(d.Key))));
			if (!docs.Any())
			{
				var idoc = AddDoc().ToCore($"Tmp_{DateTime.Now.Ticks}");
				core.docs.Add(idoc);
			}
			return core;
		}

		public void FromCore(RegisteredDocCore core, user user, DateTime timestamp)
		{
			var docs = (ParticleDocChain)core.docs;
			var vl = new DocChainVal
			{
				created = timestamp,
				keyCreator = user.key,
				kind = ChangeKind.Add,
				Item = docs
			};
			entries = entries.Add(vl);
		}

		public RegisteredDocView ToView()
		{
			var view = new RegisteredDocView();
			var summ = Summarize(false);
			(view.keyDocType, view.docType, view.count, view.DmsId)
		  = (keyDocType, docType?.identifier, summ.Keys.Count, ""/*this.current.FirstOrDefault().Value?.DmsId*/);

			if (!summ.Any())
				return view;
			var first = summ.FirstOrDefault();
			//view.properties = string.Join("&nbsp;&nbsp;", first.Value.props.Select(p => $"<b>{p.Key}</b>:{p.Value.val}"));
			var dic = first.Value.exists.ToDictionary(x => x.ex, x => x.cnt);
			return view.SetProperties(first.Value.props).SetExistence(dic);
		}

		public RegisteredDocView ToView(string[] allowedJnsDok)
		{
			var view = new RegisteredDocView();
			var summ = Summarize(false);
			(view.keyDocType, view.docType, view.count, view.DmsId, view.isScanAllow)
				= (keyDocType, docType?.identifier, summ.Keys.Count, ""/*this.current.FirstOrDefault().Value?.DmsId*/, (allowedJnsDok.Contains(keyDocType) ? true : false));

			if (!summ.Any())
				return view;
			var first = summ.FirstOrDefault();
			//view.properties = string.Join("&nbsp;&nbsp;", first.Value.props.Select(p => $"<b>{p.Key}</b>:{p.Value.val}"));
			var dic = first.Value.exists.ToDictionary(x => x.ex, x => x.cnt);
			return view.SetProperties(first.Value.props).SetExistence(dic);
		}

		public ReportDocPraDeals ToReport(string project, string desa, string pemilik, string idbidang, double? luasSurat, string alasHak, string group)
		{
            var view = new ReportDocPraDeals();
            var summ = Summarize(false);
            (
				view.Project, view.Desa, view.Pemilik, view.IdBidang,
				view.AlasHak, view.LuasSurat, view.Group,
				view.keyDocType, view.docType, view.count, view.DmsId
			)
				= 
			(	project, desa, pemilik, idbidang, 
				alasHak, luasSurat, group,
				keyDocType, docType?.identifier, summ.Keys.Count, ""/*this.current.FirstOrDefault().Value?.DmsId*/
			);

            if (!summ.Any())
                return view;
            var first = summ.FirstOrDefault();
            //view.properties = string.Join("&nbsp;&nbsp;", first.Value.props.Select(p => $"<b>{p.Key}</b>:{p.Value.val}"));
            var dic = first.Value.exists.ToDictionary(x => x.ex, x => x.cnt);
            return view.SetProperties(first.Value.props).SetExistence(dic);
        }

        public DynElement[] MakeLayout()
		{
			var sttypes = MetadataType.types.ToList();
			var combos = docType.metadata.Join(sttypes, p => p.key, s => s.Key, (p, s) => (p, s.Value));

			var docname = docType.identifier;

			var dyns = combos.Select(k => new DynElement
			{
				visible = "true",
				editable = "true",
				group = $"{docname}|Properties",
				label = k.p.key.ToString("g").Replace("_", " "),
				value = $"#docs.props.${k.p.key}",
				nullable = !k.p.req,
				cascade = "",
				dependency = "",
				correction = false,
				inittext = "",
				options = "",
				swlabels = new string[0],
				type = (k.Value.Name, k.Value.GenericTypeArguments.FirstOrDefault()?.Name) switch
				{
					("Nullable`1", "Int32") => ElementType.Numeric,
					("Nullable`1", "Decimal") => ElementType.Number,
					("Nullable`1", "Boolean") => ElementType.Check,
					("Nullable`1", "DateTime") => ElementType.Date,
					_ => ElementType.Text,
				},
			});

			var exi = new (string key, string type, string label)[]
			{
				("Soft_Copy","Check","Di-scan"),
				("Asli","Check","Asli"),
				("Copy","Numeric","Copy"),
				("Salinan","Numeric","Salinan"),
				("Legalisir","Numeric","Legalisir"),
				("Avoid","Check","Tidak Diperlukan/Memo"),
			};

			var existencies = exi.Select(x => new DynElement
			{
				visible = "true",
				editable = x.label == "Di-scan" ? "false" : "true",
				group = $"{docname}|Keberadaan",
				label = x.label,
				value = $"#docs.{x.key}",
				nullable = false,
				cascade = "",
				dependency = "",
				correction = false,
				inittext = "",
				options = "",
				swlabels = new string[0],
				xtype = x.type
			});

			var res = dyns.Union(existencies).ToArray();
			return res;
		}
	}

	public class ParticleDocReserve
	{
		public string key { get; set; }
		public Existency[] reserved { get; set; } = ParticleDoc.NoExistencies;
	}

	public class DocReservation
	{
		public DateTime since { get; set; }
		public string keyAssignment { get; set; }
		public DateTime? aborted { get; set; }
		public DateTime? realized { get; set; }
		public ParticleDocReserve[] items { get; set; }

	}

	public class DocSeries : IRegDoc
	{
		public string keyDocType { get; set; }
		public FixExistencesDocChain docs { get; set; }

		/*		public DocSeriesView ToView()
				{
					var view = new DocSeriesView();
					(view.keyDocType, view.docType)
						= (keyDocType, ((IRegDoc)this).docType?.identifier); ;

					view.docs = docs.Select(d =>
					{
						var dic = d.Value.exists.ToDictionary(x => x.ex, x => x.cnt);
						var res = new DocSeriesView.MyInnerDoc
						{
							properties = string.Join("&nbsp;&nbsp;", d.Value.props.Select(p => $"<b>{p.Key}</b>:{p.Value.val}"))
						};
						res.SetExistence(dic);
						return res;
					}).ToArray();
					return view;
				}*/
	}

	public class DocRequirement : IRegDoc
	{
		[BsonIgnore]
		public string keyDocType
		{
			get => key;
			set => key = value;
		}
		public string key { get; set; }
		public Existence ex { get; set; }
		public bool req { get; set; }

		public DocRequirementView ToView()
		{
			var view = new DocRequirementView
			{
				keyDocType = this.keyDocType,
				ex = this.ex,
				req = this.req,
				docType = ((IRegDoc)this).docType()?.identifier
			};
			return view;
		}

	}

	public class ProcessReq
	{
		public DocProcessStep step { get; set; }
		public KeyValuePair<string, Dynamic> prop { get; set; }
		public Existence exis { get; set; }
		public Existence sec_exis { get; set; }
	}

	public class DocProcessReq : ProcessReq, IRegDoc
	{
		public string keyDocType { get; set; }

		public DocProcessReq(string keyDocType, ProcessReq req)
		{
			this.keyDocType = keyDocType;
			(step, prop, exis) = (req.step, req.prop, req.exis);
		}
	}

	public interface ITaskBundle { }
	public interface IMainBundle { }
	public interface IPreBundle { }
	public interface IAssignment { }
	public interface IAssignmentDtl { }
	public interface IPraPembebasan { }
	public interface IPraPembebasanDtl { }
	public interface ITrxBatch { }
	public interface IPraBebas { }
	public interface IPraBebasDtl { }
	public interface IDealBundle { }
}
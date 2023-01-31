using landrope.common;
using landrope.documents;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.engines
{
	/*	public class MainBundleMem
		{
			public MainBundle source { get; set; }
			public List<DocExistence> docs { get; set; } = new List<DocExistence>();

			public MainBundleMem(MainBundle source)
			{
				this.source = source;
				this.docs = source.doclist.SelectMany(d => Parse(source.key, d)).ToList();
			}

			List<DocExistence> Parse(string key, BundledDoc bdoc)
			{
				var ents = bdoc.entries.Where(s => s.kind != mod2.ChangeKind.Unchanged).ToList();
				var lastset = ents.Where(s => s.kind == mod2.ChangeKind.Update).OrderBy(s => s.created).Last();
				if (lastset != null)
				{
					ents = ents.Where(s => s.created > lastset.created).OrderBy(s => s.created).ToList();
					ents.Add(lastset);
				}
				var exists = ents.SelectMany(s => s.Item.SelectMany(p => p.Value.exists.Select(x =>
					(chainkey: p.Key, x.ex, cnt: x.cnt * (s.kind == mod2.ChangeKind.Delete ? -1 : 1)))));
				var exfinal = exists.GroupBy(e => (e.chainkey, e.ex)).Select(g => (g.Key.chainkey, g.Key.ex, cnt: g.Sum(d => d.cnt)))
											.GroupBy(x => x.chainkey).Select(g => (chainkey: g.Key, exis: g.Select(d => (d.ex, d.cnt)).ToArray()));

				return exfinal.Select(x => new DocExistence(key, bdoc.keyDocType, x.chainkey, x.exis)).ToList();
			}

		}
	*/

	public class DocExistence : DocPart
	{
		public int[] current { get; set; } = new int[6];
		int[] reserved { get; set; } = new int[6];
		public int[] available => current.Select((x, i) => (x, i))
															.Join(reserved.Select((y, i) => (y, i)), c => c.i, r => r.i, (c, r) => (c.i, cnt: c.x - r.y))
															.OrderBy(x => x.i).Select(x => x.cnt).ToArray();
		public int[] availableCopy
		{
			get
			{
				var avs = available.Clone() as int[];
				if (avs.Skip(1).Any(a => a > 0))
					avs[2] = 100;
				return avs;
			}
		}
		public List<string> reservers = new List<string>();

		public int[] Reserved => reserved;

		public void AddReserve(string resvkey, int[] exis)
		{
			if (reservers.Contains(resvkey))
				return;
			exis[0] = exis[5] = 0;
			reserved = reserved.Add(exis);
		}

		public void DelReserve(string resvkey, int[] exis)
		{
			if (!reservers.Contains(resvkey))
				return;
			exis[0] = exis[5] = 0;
			reserved = reserved.Sub(exis);
			reservers.Remove(resvkey);
		}

		public void Realize(string resvkey, int[] exis)
		{
			if (!reservers.Contains(resvkey))
				return;
			exis[0] = exis[5] = 0;
			current = current.Sub(exis);
			reserved = reserved.Sub(exis);
			reservers.Remove(resvkey);
		}

		public void ClearReserve()
		{
			reserved = new int[6];
			reservers.Clear();
		}

		public DocExistence(string key, string keyDocType, string chainkey, (Existence ex, int cnt)[] current)
			: base(key, keyDocType, chainkey)
		{
			(this.current, this.reserved) = (new int[6], new int[6]);
			foreach (var x in current)
				this.current[(int)x.ex] = x.cnt;
		}

		public DocExistence(string key, string keyDocType, string chainkey, int[] current)
			: base(key, keyDocType, chainkey)
		{
			(this.current, this.reserved) = (current, new int[6]);
		}

		/*		public void Reserve(int[] resvs)
				{
					if (resvs.Length != 6)
						throw new InvalidProgramException("Reservation is invalid");
					for (int i = 0; i < 6; i++)
						reserved[i] = resvs[i];
				}

				public void Reserve((Existence ex, int cnt)[] resvs, mod2.ChangeKind kind)
				{
					if (kind == mod2.ChangeKind.Unchanged)
						return;
					if (kind == mod2.ChangeKind.Update)
						reserved = new int[6];
					int fact = kind == mod2.ChangeKind.Delete ? -1 : 1;

					foreach (var x in resvs)
						this.reserved[(int)x.ex] += fact * x.cnt;
				}
		*/
		public static List<DocExistence> Parse(string key, BundledDoc bdoc)
		{
			var ents = bdoc.entries.Where(s => s.kind != mod2.ChangeKind.Unchanged).ToList();
			var lastset = ents.Where(s => s.kind == mod2.ChangeKind.Update).OrderBy(s => s.created).LastOrDefault();
			if (lastset != null)
			{
				ents = ents.Where(s => s.created > lastset.created).OrderBy(s => s.created).ToList();
				ents.Add(lastset);
			}
			var exists = ents.SelectMany(s => s.Item.SelectMany(p => p.Value.exists.Select(x =>
				(chainkey: p.Key, x.ex, cnt: x.cnt * (s.kind == mod2.ChangeKind.Delete ? -1 : 1)))));
			var exfinal = exists.GroupBy(e => (e.chainkey, e.ex)).Select(g => (g.Key.chainkey, g.Key.ex, cnt: g.Sum(d => d.cnt)))
										.GroupBy(x => x.chainkey).Select(g => (chainkey: g.Key, exis: g.Select(d => (d.ex, d.cnt)).ToArray()));

			return exfinal.Select(x => new DocExistence(key, bdoc.keyDocType, x.chainkey, x.exis)).ToList();
		}
	}

	public class DocProp : DocPart
	{
		public Dictionary<string, Dynamic> props { get; set; }
		[BsonIgnore]
		public Dictionary<string, string> stprops => props.Select(p => new KeyValuePair<string, string>(p.Key, p.Value.val))
								.ToDictionary(d => d.Key, d => d.Value);
		public DocProp(string key, string keyDocType, string chainkey, Dictionary<string, Dynamic> props)
			: base(key, keyDocType, chainkey)
		{
			this.props = props;
		}

		public static List<DocProp> Parse(string key, BundledDoc bdoc)
		{
			var props = bdoc.entries.SelectMany(s => s.Item.Select(p => (chainkey: p.Key, s.created, props: p.Value.props.ToList())))
									.SelectMany(x => x.props.Select(p => (x.chainkey, x.created, p.Key, p.Value)))
									//.OrderBy(x => x.created)
									.GroupBy(x => (x.chainkey, x.Key))
									.Select(g => (g.Key.chainkey, g.Key.Key, val: g.OrderBy(d => d.created).Last().Value))
									.Select(x => (x.chainkey, prop: new KeyValuePair<string, Dynamic>(x.Key, x.val)))
									.GroupBy(x => x.chainkey)
									.Select(g => (chainkey: g.Key, props: g.Select(d => d.prop).ToDictionary(x => x.Key, x => x.Value)));

			return props.Select(x => new DocProp(key, bdoc.keyDocType, x.chainkey, x.props)).ToList();
		}
	}

	public class DocFact : DocExistence
	{
		public Dictionary<string, Dynamic> props { get; set; }

		public DocFact(string key, string keyDocType, string chainkey, Dictionary<string, Dynamic> props, (Existence ex, int cnt)[] exis)
			: base(key, keyDocType, chainkey, exis)
		{
			this.props = props;
		}

		new public static DocFact[] Parse(string key, BundledDoc bdoc)
		{
			var ents = bdoc.entries.Where(s => s.kind != mod2.ChangeKind.Unchanged).ToList();
			var lastset = ents.Where(s => s.kind == mod2.ChangeKind.Update).OrderBy(s => s.created).LastOrDefault();
			if (lastset != null)
			{
				ents = ents.Where(s => s.created > lastset.created).OrderBy(s => s.created).ToList();
				ents.Add(lastset);
			}
			var exists = ents.SelectMany(s => s.Item.SelectMany(p => p.Value.exists.Select(x =>
				(chainkey: p.Key, x.ex, cnt: x.cnt * (s.kind == mod2.ChangeKind.Delete ? -1 : 1)))));
			var exfinal = exists.GroupBy(e => (e.chainkey, e.ex)).Select(g => (g.Key.chainkey, g.Key.ex, cnt: g.Sum(d => d.cnt)))
										.GroupBy(x => x.chainkey).Select(g => (chainkey: g.Key, exis: g.Select(d => (d.ex, d.cnt)).ToArray()));

			var props = bdoc.entries.SelectMany(s => s.Item.Select(p => (chainkey: p.Key, s.created, props: p.Value.props.ToList())))
									.SelectMany(x => x.props.Select(p => (x.chainkey, x.created, p.Key, p.Value)))
									//.OrderBy(x => x.created)
									.GroupBy(x => (x.chainkey, x.Key))
									.Select(g => (g.Key.chainkey, g.Key.Key, val: g.OrderBy(d => d.created).Last().Value))
									.Select(x => (x.chainkey, prop: new KeyValuePair<string, Dynamic>(x.Key, x.val)))
									.GroupBy(x => x.chainkey)
									.Select(g => (chainkey: g.Key, props: g.Select(d => d.prop).ToDictionary(x => x.Key, x => x.Value)));

			return exfinal.GroupJoin(props, x => x.chainkey, p => p.chainkey, (x, sp) =>
										 new DocFact(key, bdoc.keyDocType, x.chainkey, sp.FirstOrDefault().props, x.exis))
									.ToArray();
		}

		public (DocExistence exis, DocProp prop) Split()
		{
			return (new DocExistence(key, keyDocType, chainkey, current), new DocProp(key, keyDocType, chainkey, props));
		}
	}

	public static class EngineHelpers
	{
		public static void CheckLock(this object obj)
		{
			lock (obj) { }
		}


	}
}

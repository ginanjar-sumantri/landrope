using landrope.mod3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using landrope.mcommon;

namespace landrope.bundhost
{
	public class BundledDocMem
	{
		public BundledDoc source { get; set; }
		public ImmutableList<string> ChainKeys { get; set; }
		public int[,] matstocks { get; set; } = new int[0, 0];
		public Dictionary<string, int[,]> matreserves { get; set; } = new Dictionary<string, int[,]>();
		public int[,] matavails { get; set; } = new int[0, 0];

		public BundledDocMem(BundledDoc src)
		{
			this.source = src;
			BuildStocks();
		}

		public void Reload()
		{
			BuildStocks();
		}

		static int exlen = Enum.GetValues(typeof(Existence)).Cast<int>().Max() + 1;

		void BuildStocks()
		{
			ChainKeys = null;
			matstocks = new int[0,0];
			matreserves = new Dictionary<string, int[,]>();
			matavails = new int[0,0];

			if (source == null)
				return;

			ChainKeys = ImmutableList<string>.Empty.AddRange(source.entries.SelectMany(e => e.Item.Select(i => i.Key)).Distinct());
			var keylen = ChainKeys.Count;
			matstocks = new int[keylen, exlen];
			matreserves = new Dictionary<string, int[,]>();
			matavails = new int[keylen, exlen];

			int pos;
			if (source.current != null)
				foreach (var key in source.current.Keys)
				{
					pos = ChainKeys.IndexOf(key);
					if (pos == -1)
						throw new InvalidOperationException($"Document chain key {key} was not registered!");
					var doc = source.current[key];
					foreach (var ex in doc.exists)
						matstocks[pos, (int)ex.ex] = ex.cnt;
				}

			var validres = source.reservations.Where(r => r.aborted == null && r.realized == null);
			foreach (var res in validres)
			{
				var akey = res.keyAssignment;
				if (!matreserves.ContainsKey(akey))
					matreserves.Add(akey, new int[keylen, exlen]);
				var mats = matreserves[akey];

				foreach (var exi in res.items)
				{
					pos = ChainKeys.IndexOf(exi.key);
					if (pos == -1)
						throw new InvalidOperationException($"Document chain key {exi.key} was not registered!");
					foreach (var ex in exi.reserved)
					{
						var ix = (int)ex.ex;
						mats[pos, ix] = ex.cnt;
					}
				}
			}

			//var gavail = source.Available().GroupBy(a => a.key).Select(g => (g.Key, avs: g.Select(d => (d.ex, d.cnt)))).ToArray();
			//foreach (var avail in gavail)
			//{
			//	pos = ChainKeys.IndexOf(avail.Key);
			//	if (pos == -1)
			//		throw new InvalidOperationException($"Document chain key {avail.Key} was not registered!");
			//	foreach (var x in avail.avs)
			//	{
			//		var ix = (int)x.ex;
			//		matavails[pos, ix] = x.cnt;
			//	}
			//}

			var mats2 = new int[keylen, exlen];
			var allrevs = matreserves.Select(r => r.Value).ToList();
			allrevs.ForEach(m =>
			{
				for (int i = 0; i < keylen; i++)
					for (int j = 0; i < exlen; j++)
						mats2[i, j] += m[i, j];
			});

			matavails = ArrayHelper.Sub(matstocks, mats2, keylen, exlen);
		}

		public void PostReserved(LandropePlusContext context, string akey)
		{
			if (source == null)
				return;

			var keylen = ChainKeys.Count;

			var res = source.reservations.FirstOrDefault(r => r.aborted == null && r.realized == null && r.keyAssignment==akey);
			if (res == null)
				return;

			if (!matreserves.ContainsKey(akey))
				matreserves.Add(akey, new int[keylen, exlen]);
			var mats = matreserves[akey];

			foreach (var exi in res.items)
			{
				var pos = ChainKeys.IndexOf(exi.key);
				if (pos == -1)
					throw new InvalidOperationException($"Document chain key {exi.key} was not registered!");

				foreach (var ex in exi.reserved)
				{
					var ix = (int)ex.ex;
					mats[pos, ix] = ex.cnt;
				}
			}

			mats = new int[keylen, exlen];
			var allrevs = matreserves.Select(r => r.Value).ToList();
			allrevs.ForEach(m =>
			{
				for (int i = 0; i < keylen; i++)
					for (int j = 0; i < exlen; j++)
						mats[i, j] += m[i, j];
			});

			matavails = ArrayHelper.Sub(matstocks, mats, keylen, exlen);
		}
	}

	public static class ArrayHelper
	{
		public static int[] Slice(this int[] arr, int seglen, int segpos)
			=> arr.Skip(seglen * segpos).Take(seglen).ToArray();

		public static int[][] Jagging(this int[,] ori)
		{
			var arr = ori.Cast<int>().ToArray();
			Int32 j = 0;
			return arr.GroupBy(x => j++ / ori.GetLength(1)).Select(y => y.ToArray()).ToArray();
		}

		public static int[,] Add(int[,] left, int[,] right)
		{
			var xlen = left.GetLength(0);
			var ylen = left.GetLength(1);
			return Add(left, right, xlen, ylen);
		}

		public static int[,] Add(int[,] left, int[,] right, int xlen, int ylen)
		{
			var ret = new int[xlen, ylen];
			var jrange = Enumerable.Range(0, ylen).ToList();
			Enumerable.Range(0, xlen).ToList().ForEach(x => jrange.ForEach(y => ret[x, y] = left[x, y] + right[x, y]));
			return ret;
		}


		public static int[,] Sub(int[,] left, int[,] right)
		{
			var xlen = left.GetLength(0);
			var ylen = left.GetLength(1);
			return Sub(left, right, xlen, ylen);
		}

		public static int[,] Sub(int[,] left, int[,] right, int xlen, int ylen)
		{
			var ret = new int[xlen, ylen];
			var jrange = Enumerable.Range(0, ylen).ToList();
			Enumerable.Range(0, xlen).ToList().ForEach(x => jrange.ForEach(y => ret[x, y] = left[x, y] - right[x, y]));
			return ret;
		}
	}
}

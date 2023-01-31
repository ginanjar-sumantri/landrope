using landrope.mod.shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maps.mod
{
	public class BintangMapBase
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
		public int en_state { get; set; }
		public string keyDesa { get; set; }
	}

	public class BintangMapRead : BintangMapBase
	{
		public byte[] map { get; set; }
	}

	public class BintangMapInfo : BintangMapBase
	{
		public Shapes map { get; set; }

		public BintangMapInfo() { }

		public BintangMapInfo(BintangMapRead source)
		{
			(key, IdBidang, en_state, keyDesa) = (source.key, source.IdBidang, source.en_state, source.keyDesa);
			map = JsonConvert.DeserializeObject<Shapes>(ASCIIEncoding.ASCII.GetString(source.map));
		}
	}

	public record UpdateProcessInfo(string IdBidang, int newproses, string linkId, string note);

	public record RincikHibahInfo(string keyRincik, string keyBintang, string note=null);
}

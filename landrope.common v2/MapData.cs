using geo.shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landrope.common
{

	public class MapData
	{
		public string PTSK { get; set; }
		public string _t { get; set; }
		public string desa { get; set; }
		public Map desaMap { get; set; }
		public string group { get; set; }
		public string key { get; set; }
		public string keyDesa { get; set; }
		public string keyProject { get; set; }
		public double luasDibayar { get; set; }
		public double luasSurat { get; set; }
		public Map map { get; set; }
		public string noPeta { get; set; }
		public string pemilik { get; set; }
		public string project { get; set; }
	}

	public class Map
	{
		public double Area { get; set; }
		public geoPoint Center { get; set; }
		public byte[] careas { get; set; }
		public byte[] areas => MapCompression
	}

	public class ProjectMap
	{
		public string key { get; set; }
		public DesaMap[] desas { get; set; }
	}

	public class DesaMap
	{
		public string key { get; set; }

		public Map map { get; set; }
		public landMap[] lands { get; set; }
	}

	public class landMap
	{
		public string type { get; set; }
		public string noPeta { get; set; }
		public string pemilik { get; set; }
		public string group { get; set; }
		public string PTSK { get; set; }
		public double luasSurat { get; set; }
		public double luasDibayar { get; set; }
		public Map map { get; set; }
		public LandStatus2 status { get; set; }

		public static gmapObject ToGmap(IEnumerable<landMap> entities, bool byident = false)
		{
			geoFeatureCollection coll = new geoFeatureCollection();

			var aentities = entities.Where(e => e.map.careas.Any()).ToList();
			if (!aentities.Any())
				return coll;
			aentities.ForEach(e =>
			{
				if (e.map.Center.Latitude == double.NaN || e.map.Center.Longitude == double.NaN)
					e.map.Center = e.map.careas.First().coordinates.First();
			});
			var centers = aentities.Select(e => (e.p.key, pt: e.m.Center)).ToList();
			var feats1 = aentities.Select(e => (e.p.key, id: e.p.basic.current.noPeta, gp: (geoFeature)e.m.ToGmap(e.p.key))).ToList();
			if (!byident)
			{
				var feats2 = feats1.GroupJoin(centers, x => x.key, y => y.key, (x, sy) => (x.gp, sy.DefaultIfEmpty().First().pt));
				var feats3 = feats2.OrderByDescending(x => x.pt?.Latitude ?? 0).ThenBy(x => x.pt?.Longitude ?? 0);
				var feats = feats3.Select((x, i) => (x.gp, i)).ToList();
				feats.ForEach(x => x.gp.properties.Add("label", $"{x.i}"));
				coll.features = feats.Select(x => x.gp).ToArray();
			}
			else
			{
				feats1.ForEach(x => x.gp.properties.Add("label", x.id));
				coll.features = feats1.Select(x => x.gp).ToArray();
			}
			var ccount = centers.Count;

			coll.center = new Point(centers.Sum(c => c.pt.Longitude) / ccount, centers.Sum(c => c.pt.Latitude) / ccount);
			return coll;
		}
	}

}



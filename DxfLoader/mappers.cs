using System;
using System.Collections;
using System.Linq;
using mongospace;
using GeomHelper;
using System.Collections.Generic;
using landrope.mod;
using Microsoft.Net.Http.Headers;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Claims;
//using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using Tracer;
using landrope.common;
using geo.shared;
using landrope.mod.shared;

namespace CadLoader
{
	public abstract class Mapper
	{
		protected LandropeContext context;
		protected string mapkey;
		protected Shapes areas = new Shapes();
		protected double Area = 0;
		protected geoPoint Center = new geoPoint();

		public Mapper()
		{ }

		public Mapper(LandropeContext context, string key)
		{
			this.context = context;
			mapkey = key;
			CheckExistence();
		}

		protected abstract void CheckExistence();

		public bool PutUTMs(UtmPoint[][] Utms, UtmPoint Center, int Zone, bool south, double Area = 0, bool store=true)
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod().Name, "PU");
			try
			{
				MyTracer.PushProc("100");
				areas.Clear();
				areas.AddRange(Utms.Select(u => new Shape { coordinates = UtmConv.UTM2LatLon(u, Zone, south).ToList() }));
				MyTracer.PushProc("200");
				this.Center = UtmConv.UTM2LatLon(Center, Zone, south);
				CorrectingCenter();
				this.Area = Area;
				MyTracer.PushProc("300");
				return store? Store() : true;
			}
			finally
			{
				MyTracer.ClearTo("PU");
			}
		}

		protected void CorrectingCenter()
		{
			if (Center==null || double.IsNaN(Center.Latitude)||double.IsNaN(Center.Longitude)||(Center.Latitude==0 && Center.Longitude==0))
			{
				var coords = areas.SelectMany(a => a.coordinates);
				var lat = coords.Average(c => c.Latitude);
				var lon = coords.Average(c => c.Longitude);
				Center = new geoPoint(lat, lon);
			}
		}

		public abstract bool Store();
	}

	public class PMapper : Mapper
	{
		public PMapper()
			: base()
		{ }

		public PMapper(LandropeContext context, string key)
			: base(context, key)
		{
		}

		protected override void CheckExistence()
		{
			var map = context.Find(new { key = "" }, $"{{key:'{mapkey}'}}", "{key:1,_id:0}").FirstOrDefault();
			if (map == null)
				throw new Exception("Invalid Project's key given");
		}

		public override bool Store()
		{
			Project proj = context.Projects($"{{key:'{mapkey}'}}").FirstOrDefault();
			if (proj == null)
				return false;
			proj.areas = areas;
			proj.Center = Center;
			proj.Area = Area;

			return null != context.db.GetCollection<Project>("projects").FindOneAndReplace(new BsonDocument("key", mapkey), proj);
		}
	}

	public class VMapper : Mapper
	{
		protected string keyp;
		public VMapper()
			: base()
		{ }

		public VMapper(LandropeContext context, string key)
			: base(context, key)
		{
		}

		protected override void CheckExistence()
		{
			var map = context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
														"villages", $"{{'village.key':'{mapkey}'}}").FirstOrDefault();
			if (map == null)
				throw new Exception("Invalid Village's key given");
			keyp = map.project.key;
		}

		public override bool Store()
		{
			Project proj = context.Projects($"{{key:'{keyp}'}}").FirstOrDefault();
			if (proj == null)
				return false;
			Village vil = proj.villages.FirstOrDefault(v => v.key == mapkey);
			if (vil == null)
				return false;
			vil.areas = areas;
			vil.Center = Center;
			vil.Area = Area;
			var res = context.db.GetCollection<Project>("maps").ReplaceOne(new BsonDocument("key", keyp), proj);
			return res.MatchedCount != 0 && res.ModifiedCount != 0;
		}

		public void BackupAndClear()
		{
			var obj = context.GetCollections(new { project = new { keyp = "", identity = "" }, village = new Village() },
																	"villages", $"{{'$village.key':'{mapkey}'}}");
			if (obj == null)
				return;
			var list = context.GetCollections(new Land(),
																	"lands", $"{{vilkey:'{mapkey}'}}", "{_id:0}").ToList();
			if (!list.Any()) // no needs for backup
				return;

			var data = new { stamp = DateTime.Now, data = new { owner = obj, data = list } };
			context.db.GetCollection<BsonDocument>("backup").InsertOne(data.ToBsonDocument());

			context.db.GetCollection<Land>("lands").DeleteMany($"{{vilkey:'{mapkey}'}}");
		}
	}

	public class LMapper : Mapper
	{
		protected string keyp;
		protected LandStatus? newStatus = null;
		protected string kode;
		protected string pemilik;
		protected double? luasukur;
		protected double? luassurat;
		protected bool isNew = false;

		public LMapper()
			: base()
		{ }

		public LMapper(LandropeContext context, string key)
			: base(context, key)
		{
			isNew = false;
		}

		public LMapper(LandropeContext context, string vilkey,
			IEnumerable<IEnumerable<UtmPoint>> Utms, UtmPoint Center, int Zone, bool south,
			string kode, LandStatus? status, string pemilik, double? luasukur, double? luassurat, double Area)
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod().Name, "LM-C");
			this.context = context;
			isNew = true;
			this.kode = kode;
			MyTracer.PushProc("100");
			FindParents(vilkey);
			if (keyp == null)
				return;
			mapkey = MongoEntity.MakeKey;
			MyTracer.PushProc("200");
			PutAttribsAndUTMs(Utms.Select(u => u.ToArray()).ToArray(), Center, Zone, south, status, pemilik, luassurat, luasukur, Area);
			MyTracer.ClearTo("LM-C");
		}

		protected void FindParents(string vilkey)
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod().Name, "FP");
			var vil = context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
																			"villages", $"{{'village.key':'{vilkey}'}}").FirstOrDefault();
			if (vil == null)
				throw new Exception("Invalid Village's key given");
			keyp = vil.village.key;
			MyTracer.ClearTo("FP");
		}

		protected override void CheckExistence()
		{
			var map = context.GetCollections(new Land(),
														"lands", $"{{key:'{mapkey}'}}", "{_id:0}").FirstOrDefault();
			if (map == null)
				throw new Exception("Invalid Land's key given");
			keyp = map.vilkey;
		}

		public override bool Store()
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod(), "STR");
			Land lan;
			if (!isNew)
			{
				MyTracer.PushProc("100");
				lan = context.GetCollections(new Land(),
															"lands", $"{{key:'{mapkey}'}}", "{_id:0}").FirstOrDefault();
				if (lan == null)
					return false;
				if (newStatus != null)
					lan.ls_status = newStatus.Value;
				if (pemilik != null)
					lan.pemilik = pemilik;
				if (luassurat != null)
					lan.luassurat = luassurat;
				if (luasukur != null)
					lan.luasukur = luasukur;
				lan.areas.Clear();
				lan.areas.AddRange(areas);
				if (Area != 0)
					lan.Area = Area;
				CorrectingCenter();
				lan.Center = Center;

				MyTracer.PushProc("200");
				return null != context.db.GetCollection<Land>("lands").FindOneAndReplace($"{{key:'{mapkey}'}}", lan);
			}
			else
			{
				lan = new Land
				{
					vilkey = keyp,
					key = mapkey,
					identity = kode,
					pemilik = pemilik,
					ls_status = newStatus ?? LandStatus.Tanpa_status,
					luassurat = luassurat,
					luasukur = luasukur,
					areas = areas,
					Center = Center,
					Area = Area
				};
				MyTracer.PushProc("200");
				context.db.GetCollection<Land>("lands").InsertOne(lan);
				return true;
			}
		}

		public bool PutAttribsAndUTMs(UtmPoint[][] Utms, UtmPoint Center, int Zone, bool south,
							LandStatus? status, string pemilik = null, double? luassurat = null, double? luasukur = null, double Area = 0)
		{
			newStatus = status;
			this.pemilik = pemilik;
			this.luassurat = luassurat;
			this.luasukur = luasukur;
			return PutUTMs(Utms, Center, Zone, south, Area,false);
		}

		static int[] status_scores = { 0, 1, 1, 2, 1, 1, 3 };
		public static void ReduceDuplicates(List<(Point CenterXY, LMapper map)> maps)
		{
			//reduce the exactly identics
			var data1 = maps.GroupBy(m => (m.map.Area, m.CenterXY.x,m.CenterXY.y)).Select(g => (g.Key, count: g.Count()))
									.Where(x => x.count > 1).ToList();
			var combo1 = data1.GroupJoin(maps, x => x.Key, m => (m.map.Area, m.CenterXY.x, m.CenterXY.y), 
									(k, sm) => (k.Key, maps:sm.ToList())).ToList();
			var reduced1 = combo1.Select(c => c.maps.OrderByDescending(mm => (Score(mm.map))).First()).ToList();
			combo1.ForEach(c => c.maps.ForEach(m=>maps.Remove(m)));
			maps.AddRange(reduced1);

			//reduce the very closes
			var data2 = maps.OrderByDescending(m => m.map.Area)
									.Join(maps.OrderBy(m => m.map.Area), m1 => 1, m2 => 1,
												(m1, m2) => (m1.map.mapkey != m2.map.mapkey && m1.map.Area >= m2.map.Area &&
														Math.Abs(m1.map.Area - m2.map.Area) < 1e-4) ? (m1, m2) : (m1: default, m2: default))
									.Where(x => x.m1.map != null).ToList();
			var data2d = data2.Where(x => SqrDist(x.m1.CenterXY, x.m2.CenterXY) < 1e-2)
										.GroupBy(x => x.m1).Select(g => (g.Key,ls:g.Select(y=>y.m2).ToList())).ToList();
			data2d.ForEach(d => d.ls.Add(d.Key));
			var combo2 = data2d.Select(d => d.ls).ToList();

			var reduced2 = combo2.Select(d => d.OrderByDescending(mm => (Score(mm.map))).First()).ToList();
			combo2.ForEach(c => c.ForEach(m => maps.Remove(m)));
			maps.AddRange(reduced2);

			int Score(LMapper mm)
			{
				var score = string.IsNullOrEmpty(mm.kode) ? 6 : 0;
				score += string.IsNullOrEmpty(mm.pemilik) ? 1 : 0;
				score += (mm.luassurat??0) != 0 ? 1 : 0;
				score += (mm.luasukur??0) != 0 ? 1 : 0;
				score += status_scores[((int)(mm.newStatus??LandStatus.Tanpa_status))] * 12;
				return score;
			}

			double SqrDist(Point ct1, Point ct2)
			{
				var dx = ct1.x - ct2.x;
				var dy = ct1.y - ct2.y;
				return dx * dx + dy * dy;
			}
		}

	}
}

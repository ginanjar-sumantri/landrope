using System;
using System.Linq;
using GeomHelper;
using System.Collections.Generic;
using landrope.mod;
using MongoDB.Driver;
//using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using Tracer;
using landrope.common;
using landrope.mod2;
using auth.mod;
using landrope.mod.shared;
using geo.shared;

namespace CadLoader
{
	public class PlMapper
	{
		protected LandropeContext context;
		protected ExtLandropeContext contextex;
		protected string persilkey;
		protected Shapes areas = new Shapes();
		protected double Area = 0;
		protected geoPoint Center = new geoPoint();

		public PlMapper(LandropeContext context, ExtLandropeContext contextex, string key)
		{
			this.context = context;
			this.contextex = contextex;
			persilkey = key;
			CheckExistence();
		}

		public PlMapper(LandropeContext context, ExtLandropeContext contextex, Persil persil)
		{
			this.context = context;
			this.contextex = contextex;
			persilkey = persil.key;
		}

		public bool PutUTMs(UtmPoint[][] Utms, UtmPoint Center, int Zone, bool south, double Area = 0)
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
				return true;
			}
			finally
			{
				MyTracer.ClearTo("PU");
			}
		}

		protected void CorrectingCenter()
		{
			if (Center == null || double.IsNaN(Center.Latitude) || double.IsNaN(Center.Longitude) || (Center.Latitude == 0 && Center.Longitude == 0))
			{
				var coords = areas.SelectMany(a => a.coordinates);
				var lat = coords.Average(c => c.Latitude);
				var lon = coords.Average(c => c.Longitude);
				Center = new geoPoint(lat, lon);
			}
		}

		public PlMapper(LandropeContext context, ExtLandropeContext contextex, string key,
			IEnumerable<IEnumerable<UtmPoint>> Utms, UtmPoint Center, int Zone, bool south, double Area)
			: this(context, contextex, key)
		{
			MyTracer.PushProc(MethodBase.GetCurrentMethod().Name, "LM-C");
			MyTracer.PushProc("100");
			PutUTMs(Utms.Select(u => u.ToArray()).ToArray(), Center, Zone, south, Area);
			MyTracer.ClearTo("LM-C");
		}

		protected void CheckExistence()
		{
			var map = contextex.persils.FirstOrDefault(p => p.key == persilkey);
			if (map == null)
				throw new Exception("Invalid Land's key given");
		}

		public bool Store(string sourceName, user user, DxfFile source, int size)
		{
			try
			{
				MyTracer.PushProc(MethodBase.GetCurrentMethod(), "STR");
				MyTracer.PushProc("100");
				var permaps = contextex.persilmaps.FirstOrDefault(p => p.key == persilkey);
				var isnew = permaps == null;
				if (isnew)
					permaps = new persilMap { key = persilkey };

				landrope.mod2.Map map = new landrope.mod2.Map { ID = mongospace.MongoEntity.MakeKey, 
					Area = this.Area, 
					areas = this.areas, 
					Center = this.Center 
				};
				var entry = new MapEntry
				{
					uploaded = DateTime.Now,
					keyUploader = user.key,
					map = map,
					sourceFile = sourceName,
					metadata = new MapMeta { created = source.Created, updated = source.LastUpdate, filesize = size, updater = source.Updater }
				};
				permaps.AddEntry(entry);

				MyTracer.PushProc("200");
				if (isnew)
					contextex.persilmaps.Insert(permaps);
				else
					contextex.persilmaps.Update(permaps);
				contextex.SaveChanges();
				MyTracer.PushProc("300");
				return true;
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
				return false;
			}
			finally
			{
				MyTracer.ClearTo("STR");
			}
		}
	}
}


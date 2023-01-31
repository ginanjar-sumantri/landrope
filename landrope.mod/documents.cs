using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using mongospace;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using PolygonCuttingEar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.GZip;
using SharpCompress.Compressors.BZip2;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using SharpCompress.Writers;
using SharpCompress.Readers.GZip;
using System.Runtime.InteropServices;
using MongoDB.Driver;
#if (_USE_FIRESTORE_)
using Google.Cloud.Firestore;
#endif
#if (_USE_MONGODB_)
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;
#endif
using GeomHelper;
using landrope.common;
using geo.shared;
using landrope.mod.shared;

namespace landrope.mod
{
	using GeoPoint = geoPoint;

	using polygon = TriangleNet.Geometry.Polygon;
	using vertex = TriangleNet.Geometry.Vertex;
	using contour = TriangleNet.Geometry.Contour;
	using static TriangleNet.Geometry.ExtensionMethods;

	// mongodb atlas GCP key b3624418-a1dd-4a28-9dc8-2027b0daa113
	// sa password L4ndR0p3

	[BsonKnownTypes(typeof(Project), typeof(Village), typeof(Land))]
	public abstract class landbase
	{
		public static byte[] encode(Shapes shps) => MapCompression.encode(shps);

		public static Shapes decode(byte[] data) => MapCompression.decode(data);

		public static byte[] compress(Shapes shps)
		{
			var bytes = ASCIIEncoding.ASCII.GetBytes(JsonConvert.SerializeObject(shps));
			var strm = new MemoryStream();
			strm.Write(bytes, 0, bytes.Length);
			strm.Seek(0, SeekOrigin.Begin);
			using (var archive = GZipArchive.Create())
			{
				archive.AddEntry("1", strm);
				var strmres = new MemoryStream();
				archive.SaveTo(strmres, new WriterOptions(SharpCompress.Common.CompressionType.GZip));
				strmres.Seek(0, SeekOrigin.Begin);
				var cmpbytes = new byte[strmres.Length];
				strmres.Read(cmpbytes, 0, cmpbytes.Length);
				return cmpbytes;
			}
		}

		public static Shapes decompress(byte[] data)
		{
			if (data == null)
				return null;
			if (data.Length == 0 || data.Length == 2 && data[0] == '[' && data[1] == ']')
				return new Shapes();
			var strm = new MemoryStream();
			strm.Write(data, 0, data.Length);
			strm.Seek(0, SeekOrigin.Begin);
			using (var archive = GZipReader.Open(strm))
			{
				archive.MoveToNextEntry();
				var strmres = new MemoryStream();
				var estrm = archive.OpenEntryStream();
				estrm.CopyTo(strmres);
				strmres.Seek(0, SeekOrigin.Begin);
				var bytes = new byte[strmres.Length];
				strmres.Read(bytes, 0, bytes.Length);
				var decomp = ASCIIEncoding.ASCII.GetString(bytes);
				var shps = JsonConvert.DeserializeObject<Shapes>(decomp);
				return shps;
			}
		}

		[BsonRequired]
		public string key { get; set; }

		[BsonRequired]
		public string identity { get; set; }

		[BsonIgnore]
		public virtual Shapes areas //{ get; set; }
		{
			get => decode(careas);
			set
			{
				careas = encode(value);
			}
		}

		[JsonIgnore]
		public byte[] careas { get; set; } = new byte[0];


		public bool? inactive { get; set; }

		public GeoPoint Center { get; set; }

		public double Area { get; set; } = 0;
		public virtual double TheArea => Area == 0 ? ChildrenArea : Area;

		public abstract double ChildrenArea { get; }

		public virtual gmapObject ToGmap()
		{
			if (Center == null || double.IsNaN(Center.Latitude) || double.IsNaN(Center.Longitude))
			{
				var gps = areas.SelectMany(a => a.coordinates).ToList();
				if (!gps.Any())
					return new geoFeature();
				var lon = gps.Average(p => p.Longitude);
				var lat = gps.Average(p => p.Latitude);
				Center = new GeoPoint(lat, lon);
			}
			var feat = areas.ToGmap() as geoFeature;
			feat.properties.Add("key", key);
			feat.properties.Add("-idx-lat", Center.Latitude);
			feat.properties.Add("-idx-lon", Center.Longitude);

			return feat;
		}
		public static string finestring(string st) => String.IsNullOrEmpty(st) ? "-" : st;
	}

	[Entity("Project", "maps")]
	public class Project : landbase
	{
		[BsonRequired]
		public BsonObjectId Id { get; set; }
		public string deskripsi { get; set; }

		public override double ChildrenArea => villages.Sum(v => v.TheArea);

		//[BsonRequired]
		public List<Village> villages = new List<Village>();

		public override gmapObject ToGmap()
		{
			var feat = base.ToGmap() as geoFeature;
			if (!string.IsNullOrEmpty(identity))
				feat.properties.Add("-Project", finestring(identity));
			var thearea = TheArea;
			if (thearea > 0)
				feat.properties.Add("-Luas", $"{thearea / 10000:0,000} Ha");
			return feat;
		}
	}

	public class Village : landbase
	{
		[BsonRequired]
		public string kecamatan { get; set; } = "";
		[BsonRequired]
		public string kabupaten { get; set; } = "";
		[BsonRequired]
		public string provinsi { get; set; } = "";
		[BsonRequired]
		public string kades { get; set; } = "";

		public override double ChildrenArea => lands.Sum(v => v.TheArea);

		public override double TheArea => ChildrenArea;
		//[BsonRequired]
		public List<Desa> desas = new List<Desa>();
		public List<Land> lands = new List<Land>();

		public List<double> StatusAreas { get; set; } = null;
		public List<List<List<double>>> ComplexStatusAreas { get; set; } = null;

		public void DistributeAreas(LandropeContext context)
		{
			Console.WriteLine($"Distributing Area of village {key}-{identity}");
			var MyLands = context.GetCollections<Land>(new Land(), "lands", $"{{vilkey:'{key}'}}","{_id:0}").ToList();
			bool addsTXM = !MyLands.Any(l => l.ls_status==LandStatus.Transisi_murni);
			bool addsTXH = !MyLands.Any(l => l.ls_status == LandStatus.Transisi_hibah);
			if (addsTXM)
			{
				var land = new Land { key = MongoEntity.MakeKey, vilkey = key, identity = "TXM", ls_status = LandStatus.Transisi_murni };
				context.db.GetCollection<Land>("lands").InsertOne(land);
			}
			if (addsTXH)
			{
				var land = new Land { key = MongoEntity.MakeKey, vilkey = key, identity = "TXH", ls_status = LandStatus.Transisi_hibah };
				context.db.GetCollection<Land>("lands").InsertOne(land);
			}
			if (addsTXH || addsTXM)
				MyLands = context.GetCollections<Land>(new Land(), "lands", $"{{vilkey:'{key}'}}", "{_id:0}").ToList();

			if (StatusAreas == null)
			{
				MyLands.ForEach(l => l.UploadedArea = null);
				UpdateLands();
				return;
			}

			Console.Write("Collect Land's calc Area by status...");
			var xareas = MyLands.Select(l => (l.ls_status, l.Area))
																.GroupBy(x => x.ls_status).Select(g => (status: g.Key, div: g.Sum(l => l.Area)))
																.OrderBy(x=>x.status)
																.ToList();
			Console.Write("calc divisor by status ");
			var divisors = new double[Enum.GetValues(typeof(LandStatus)).Length];
			xareas.ForEach(x => divisors[(int)x.status] = x.div);
			Console.Write($"({divisors.Length} items)...");

			//bool newStatusCat = false;// (xareas.Any(x => x.status == LandStatus.Sudah_bebas__PBT_belum_terbit));
			//var SAreas = newStatusCat ? StatusAreas : StatusAreas.Take(7).ToList();
			//if (!newStatusCat)
			//	SAreas[(int)LandStatus.Hibah__PBT_belum_terbit] += StatusAreas[(int)LandStatus.Sudah_bebas__PBT_belum_terbit];

			var specials = new[] { LandStatus.Transisi_hibah, LandStatus.Transisi_murni };
			MyLands.ForEach(l =>
				l.UploadedArea = (specials.Contains(l.ls_status))? StatusAreas[(int)l.ls_status] : 
																			l.Area * StatusAreas[(int)l.ls_status] / divisors[(int)l.ls_status]
			);
			UpdateLands();
			Console.WriteLine($"{key}-{identity} done");

			void UpdateLands()
			{
				MyLands.ForEach(l =>
					context.db.GetCollection<Land>("lands").ReplaceOne($"{{key:'{l.key}'}}", l));
			}
		}



		public override gmapObject ToGmap()
		{
			var feat = base.ToGmap() as geoFeature;
			if (!string.IsNullOrEmpty(identity))
				feat.properties.Add("-Desa", finestring(identity));
			//var thearea = TheArea;
			//if (thearea > 0)
			//	feat.properties.Add("-Luas", $"{thearea / 10000:0,000} Ha");
			return feat;
/*			if (!string.IsNullOrEmpty(kecamatan))
				feat.properties.Add("-Kecamatan", finestring(kecamatan));
			if (!string.IsNullOrEmpty(kabupaten))
				feat.properties.Add("-Kabupaten", finestring(kabupaten));
			if (!string.IsNullOrEmpty(provinsi))
				feat.properties.Add("-Provinsi", finestring(provinsi));
			if (!string.IsNullOrEmpty(kades))
				feat.properties.Add("-Kades", finestring(kades));
			return feat;*/
		}
	}

	public class Land : landbase
	{
		public string vilkey { get; set; }
		public string surat { get; set; } = "";
		public string pemilik { get; set; } = "";
		public string penjual { get; set; } = "";
		public double? luassurat { get; set; }
		public double? luasukur { get; set; }
		public double? luasnib { get; set; }
		public string nib { get; set; } = "";

		[BsonRequired]
		[JsonIgnore]
		public LandStatus ls_status { get; set; } = LandStatus.Tanpa_status;

		[BsonIgnore]
		[JsonRequired]
		public string status => ls_status.ToString().Replace("___", "/").Replace("__", ",").Replace("_", " ");

		public override Shapes areas
		{
			get => decompress(careas);
			set
			{
				careas = compress(value);
			}
		}

		public override double ChildrenArea => Area;
		public double? UploadedArea = null;

		public override double TheArea => UploadedArea != null ? Math.Round(UploadedArea.Value) :
			(luasnib ?? 0) != 0 ? luasnib.Value :
			(luasukur ?? 0) != 0 ? luasukur.Value :
			(luassurat ?? 0) != 0 ? luassurat.Value :
			Math.Round(Area);
		public override gmapObject ToGmap()
		{
			var feat = base.ToGmap() as geoFeature;
			if (!string.IsNullOrEmpty(identity))
				feat.properties.Add("-Kode", finestring(identity));
			if (!string.IsNullOrEmpty(surat))
				feat.properties.Add("-Surat", finestring(surat));
			if (!string.IsNullOrEmpty(pemilik))
				feat.properties.Add("-Pemilik", finestring(pemilik));
			if (!string.IsNullOrEmpty(penjual))
				feat.properties.Add("-Penjual", finestring(penjual));
			if (luassurat.HasValue)
				feat.properties.Add("-Luas Surat", luassurat.HasValue ? luassurat.ToString() : "-");
			if (luasukur.HasValue)
				feat.properties.Add("-Luas Ukur", luasukur.HasValue ? luasukur.ToString() : "-");
			if (luasnib.HasValue)
				feat.properties.Add("-Luas NIB", luasnib.HasValue ? luasnib.ToString() : "-");
			if (Area > 0)
				feat.properties.Add("-Luas", $"{TheArea.FormHa()} M2");
			if (!string.IsNullOrEmpty(nib))
				feat.properties.Add("-NIB", finestring(nib));
			feat.properties.Add("status", $"{(int)ls_status}");
			if (ls_status != LandStatus.Tanpa_status)
			{
				feat.properties.Add("-Status", status);
			}
			return feat;
		}
	}


	public class Desa
	{
		public string key { get; set; }
		public string identity { get; set; }
		public string provinsi { get; set; }
		public string kabupaten { get; set; }
		public string kecamatan { get; set; }
		public string kades { get; set; } = "";
	}

	public static class PointHelper
	{
		public static geo.shared.Point ToPoint(this GeoPoint pt) => new geo.shared.Point(pt.Longitude, pt.Latitude);
		public static GeoPoint ToGeoPoint(this geo.shared.Point pt) => new GeoPoint(pt.y, pt.x);
		public static Segment NewSegment(GeoPoint pt1, GeoPoint pt2) => new Segment(pt1.Longitude, pt1.Latitude, pt2.Longitude, pt2.Latitude);


		public static vertex ToVertex(this GeoPoint pt) => new vertex(pt.Longitude, pt.Latitude);
		public static vertex ToVertex(this geo.shared.Point pt) => new vertex(pt.x, pt.y);
		public static geo.shared.Point ToPoint(this TriangleNet.Geometry.Point pt) => new geo.shared.Point(pt.X, pt.Y);
		public static bool IsInside(this TriangleNet.Geometry.ISegment segm, TriangleNet.Geometry.Vertex pt)
		{
			var vx0 = segm.GetVertex(segm.P0);
			var vx1 = segm.GetVertex(segm.P1);
			var Xminmax = vx0.X < vx1.X ? (min: vx0.X, max: vx1.X) : (min: vx1.X, max: vx0.X);
			var Yminmax = vx0.Y < vx1.Y ? (min: vx0.Y, max: vx1.Y) : (min: vx1.Y, max: vx0.Y);
			if (pt.X < Xminmax.min || pt.X > Xminmax.max || pt.Y < Yminmax.min || pt.Y > Yminmax.max)
				return false;
			return (pt.X - vx0.X) * (pt.Y - vx1.Y) == (pt.X - vx1.X) * (pt.Y - vx0.Y);
		}

		public static bool IsNone(this geo.shared.Point pt) => double.IsNaN(pt.x) || double.IsNaN(pt.y);

		public static CPoint2D ToCPoint(this geo.shared.Point pt) => new CPoint2D(pt.x, pt.y);

		public static string FormHa(this double area)
		{
			int x = (int)Math.Truncate(area);
			int y = x % 10;
			x /= 10;
			return $"{x:#,###}{y}";
		}
	}
}


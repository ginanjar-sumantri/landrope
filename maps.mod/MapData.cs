using geo.shared;
using landrope.common;
using landrope.mod.shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Binaron.Serializer;
using maps.mod;
using binaland;
using GeomHelper;
using ClipperLib;
using MongoDB.Bson.Serialization.Attributes;

namespace maps.mod
{
	public record MetaDataBase
	{
		public string IdBidang { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string group { get; set; }
		public double? luas { get; set; }
		public string noPeta { get; set; }
		public string pemilik { get; set; }
		public string nomor { get; set; }
		public string proses { get; set; }
		 
		public T SetNomor<T>(string nomor) where T : MetaDataBase
		{
			this.nomor = nomor;
			return this as T;
		}

		public MetaDataBase() { }

		public MetaDataBase(KulitStatus k, string project, string desa)
		{
			(
			this.project,
			this.desa,
			this.luas,
			this.pemilik,
			this.IdBidang,
			this.proses
			) =
			(
			project,
			desa,
			k.luas,
			k.pemilik,
			k.IdBidang,
			k.proses
			);
		}
	}

	public enum LandCat
	{
		Girik = 1,
		Hibah = 2,
		SHM = 3,
		SHP = 4,
		HGB = 5
	}

	public record MetaData : MetaDataBase
	{
		public LandCat cat { get; set; }
		public string PTSK { get; set; }
		public double? sisa { get; set; }
		public double? overlap { get; set; }
		public string kategori { get; set; }
		public string[] cats { get; set; }
		public string deal { get; set; }

		public MetaData SetDeal(DateTime? dt, int? stage)
		{
			this.deal = null;
			if (dt != null && dt != DateTime.MinValue && stage != null)
			{
				var info = stage switch
				{
					0 => "Buat Peta Lokasi",
					1 => "Buat SPK pembayaran pertama",
					2 => "Persiapan Memo Tanah",
					3 => "Memo dikirim ke Pak Ali",
					4 => "Disetujui GM",
					5 => "Sudah diperiksa Internal Audit",
					6 => "Disetujui Accounting",
					_ => "(Status tidak diketahui)"
				};
				this.deal = $"{dt:dd/MM/yy} > {info}";
			}
			return this;
		}

		public MetaData SetCats(string[] cats)
		{
			this.cats = cats;
			return this;
		}

		public MetaData() : base() { }

		public MetaData(KulitStatus k, string project, string desa)
		: base(k, project, desa)
		{
			this.sisa = k.sisaOverlap;
		}
	}

	public record MetaDataLight(double? luasDibayar, DateTime ceated) : MetaDataBase;

	public record MetaDataR : MetaData
	{
		public string keyParent { get; set; }
		public static MetaDataR CopyFrom(landMap lmap)
		{
			if (lmap == null)
				return new MetaDataR();
			if (lmap.meta == null)
				return new MetaDataR { keyParent = lmap.key };
			var json = JsonConvert.SerializeObject(lmap.meta);
			var meta = JsonConvert.DeserializeObject<MetaDataR>(json);
			meta.keyParent = lmap.key;
			return meta;
		}

		public MetaDataR() : base() { }

		public MetaDataR(KulitStatus k, string project, string desa)
			: base(k, project, desa)
		{ }
	}

	public class MapData
	{
		public string _t { get; set; }
		public string key { get; set; }
		public string keyDesa { get; set; }
		public string keyProject { get; set; }

		public string project { get; set; }
		public string desa { get; set; }
		public string PTSK { get; set; }
		public string group { get; set; }
		public double? luasDibayar { get; set; }
		public double? luasSurat { get; set; }
		public double? luasUkur { get; set; }
		public string noPeta { get; set; }
		public string pemilik { get; set; }
		public string IdBidang { get; set; }
		public string deal { get; set; }

		public MapData SetIdBidang(string newId)
		{
			IdBidang = newId;
			return this;
		}

		public static implicit operator MetaData(MapData md) =>
			new MetaData
			{
				cat = md._t switch
				{
					"PersilHibah" => LandCat.Hibah,
					"persilSHM" => LandCat.SHM,
					"persilSHP" => LandCat.SHP,
					"persilHGB" => LandCat.HGB,
					_ => LandCat.Girik
				},
				project = md.project,
				desa = md.desa,
				PTSK = md.PTSK,
				group = md.group,
				luas = md.luasDibayar ?? md.luasUkur ?? md.luasSurat,
				noPeta = md.noPeta,
				pemilik = md.pemilik,
				IdBidang = md.IdBidang,
				deal = md.deal
			};
	}

	public class MapData2 : MapData
	{
		public string nomor { get; set; }
		public string proses { get; set; }
		public maps.mod.LandState state { get; set; }
		public double? luasOverlap { get; set; }
		public double? sisaOverlap { get; set; }
		public MapData2(MapData other, (LandStatus mapin, string[] locs) status)
		{
			if (other != null)
				(
					this.key,
					this.keyProject,
					this.project,
					this.keyDesa,
					this.desa,
					this.PTSK,
					this.group,
					this.luasDibayar,
					this.luasSurat,
					this.luasUkur,
					this.luasOverlap,
					this.sisaOverlap,
					this.noPeta,
					this.pemilik,
					this.IdBidang,
					this.deal
				) =
				(
					other.key,
					status.mapin.keyProject,
					status.locs[0],
 					status.mapin.keyDesa,
					status.locs[1],
					other.PTSK,
					other.group,
					other.luasDibayar,
					other.luasSurat,
					other.luasUkur,
					status.mapin.luasOverlap,
					status.mapin.luas - status.mapin.luasOverlap,
					other.noPeta,
					other.pemilik,
					other.IdBidang,
					other.deal
				);
			else
				(
					this.key,
					this.keyProject,
					this.project,
					this.keyDesa,
					this.desa,
					this.PTSK,
					this.group,
					this.luasDibayar,
					this.luasSurat,
					this.luasUkur,
					this.luasOverlap,
					this.sisaOverlap,
					this.noPeta,
					this.pemilik,
					this.IdBidang
				) =
				(
					status.mapin.key,
					status.mapin.keyProject,
					status.locs[0],
					status.mapin.keyDesa,
					status.locs[1],
					"",
					"",
					status.mapin.luasDibayar,
					status.mapin.luasSurat,
					status.mapin.luas,
					status.mapin.luasOverlap,
					status.mapin.luas - status.mapin.luasOverlap,
					"",
					"",
					status.mapin.IdBidang
				);

			(this.state, this.bebas, this.luas, this.proses, this.keluar, this.claim, this.damai, this.damaiB, this.kulit) =
				(status.mapin.state, status.mapin.status.EndsWith("Sudah Bebas", true, null), status.mapin.luas, status.mapin.proses,
				status.mapin.kategori == "keluar", status.mapin.kategori == "claim", status.mapin.kategori == "damai",
				status.mapin.kategori == "damai*", status.mapin.proses == "k");
		}

		public MapData2(MapData other, LandStatus status)
		{
			if (other != null)
				(
					this.key,
					this.keyProject,
					this.project,
					this.keyDesa,
					this.desa,
					this.PTSK,
					this.group,
					this.luasDibayar,
					this.luasSurat,
					this.luasUkur,
					this.luasOverlap,
					this.sisaOverlap,
					this.noPeta,
					this.pemilik,
					this.IdBidang,
					this.deal
				) =
				(
					other.key,
					status.keyProject,
					other.project,
					status.keyDesa,
					other.desa,
					other.PTSK,
					other.group,
					other.luasDibayar,
					other.luasSurat,
					other.luasUkur,
					status.luasOverlap,
					status.luas - status.luasOverlap,
					other.noPeta,
					other.pemilik,
					other.IdBidang,
					other.deal
				);
			else
				(
					this.key,
					this.keyProject,
					this.project,
					this.keyDesa,
					this.desa,
					this.PTSK,
					this.group,
					this.luasDibayar,
					this.luasSurat,
					this.luasUkur,
					this.luasOverlap,
					this.sisaOverlap,
					this.noPeta,
					this.pemilik,
					this.IdBidang
				) =
				(
					status.key,
					status.keyProject,
					status.keyProject,
					status.keyDesa,
					status.keyDesa,
					"",
					"",
					status.luasDibayar,
					status.luasSurat,
					status.luas,
					status.luasOverlap,
					status.luas - status.luasOverlap,
					"",
					"",
					status.IdBidang
				);

			(this.state, this.bebas, this.luas, this.proses) =
				(status.state, status.status.EndsWith("Sudah Bebas", true, null), status.luas, status.proses);
			(this.state, this.bebas, this.luas, this.proses, this.keluar, this.claim, this.damai, this.damaiB, kulit) =
				(status.state, status.status.EndsWith("Sudah Bebas", true, null), status.luas, status.proses,
				status.kategori == "keluar", status.kategori == "claim", status.kategori == "damai", status.kategori == "damai*",
				status.proses == "k");
		}

		public bool bebas { get; set; }
		public bool keluar { get; set; }
		public bool claim { get; set; }
		public bool damai { get; set; }
		public bool damaiB { get; set; }
		public bool kulit { get; set; }
		public double? luas { get; set; }

		public static implicit operator MetaDataBase(MapData2 md) =>
			new MetaDataBase
			{
				project = md.project,
				desa = md.desa,
				group = md.group,
				luas = md.luasSurat,
				proses = md.proses,
				noPeta = md.noPeta,
				pemilik = md.pemilik,
				IdBidang = md.IdBidang,
				nomor = md.nomor,
			};

		public static implicit operator MetaData(MapData2 md) =>
				new MetaData
				{
					cat = md._t switch
					{
						"PersilHibah" => LandCat.Hibah,
						"persilSHM" => LandCat.SHM,
						"persilSHP" => LandCat.SHP,
						"persilHGB" => LandCat.HGB,
						_ => LandCat.Girik
					},
					project = md.project,
					desa = md.desa,
					PTSK = md.PTSK,
					group = md.group,
					luas = md.luas ?? 0,
					overlap = md.proses == "d" ? (md.luasOverlap ?? 0) : 0,
					proses = md.proses,
					sisa = md.proses switch
					{
						"b" => (double?)Math.Max(0, (md.luas ?? 0) - (md.luasOverlap ?? 0)),
						"s" => md.luas ?? 0,
						_ => 0
					},
					noPeta = md.noPeta,
					pemilik = md.pemilik,
					IdBidang = md.IdBidang,
					deal = md.deal,
					nomor = md.nomor
				};

		public static implicit operator MetaDataLight(MapData2 md) =>
			new MetaDataLight(md.luasDibayar, DateTime.MinValue)
			{
				project = md.project,
				desa = md.desa,
				group = md.group,
				luas = md.luasSurat,
				//luasDibayar = md.luasDibayar,
				proses = md.proses,
				noPeta = md.noPeta,
				pemilik = md.pemilik,
				IdBidang = md.IdBidang,
				nomor = md.nomor,
			};
	}

	public class LandStatusBase
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public double? luasSurat { get; set; }
		public double? luasDibayar { get; set; }
		public double? luasOverlap { get; set; }
		public double? luas { get; set; }
		public double? sisaOverlap { get; set; }
		public string pemilik { get; set; }
		public string proses { get; set; }
		public string desc { get; set; }
		public string status { get; set; }
	}

	public class LandStatus : LandStatusBase
	{
		public string kategori { get; set; }
		public maps.mod.LandState state { get; set; }

		public LandStatus() : base() { }
		public LandStatus(maps.mod.LandState state) : base()
		{
			this.state = state;
		}
	}

	public class KulitStatus : LandStatus
	{
		public byte[] careas { get; set; }
		public Shape[] GetShapes() => JsonConvert.DeserializeObject<Shape[]>(ASCIIEncoding.ASCII.GetString(careas));
	}

	public class DamaiStatus : LandStatusBase { }

	public class BintangStatus : LandStatusBase
	{
	}

	public class DBDesaMap
	{
		public virtual string key { get; set; }
		public Map map { get; set; }
	}

	public class DBPersilMap : DBDesaMap
	{
	}

	public class DBDesaMapD : DBDesaMap
	{
		public string keyDesa { get; set; }
		public override string key { get => keyDesa; set { keyDesa = value; } }
	}

	public class DBMapRincik : Map
	{
		public string IdBidang { get; set; }
		public string IdBidangBtg { get; set; }
		public float luas { get; set; }
		public string pemilik { get; set; }
	}

	public class MapBase
	{
		public double Area { get; set; }
		public geoPoint Center { get; set; }
	}

	public class Map : MapBase
	{
		public byte[] careas { get; set; }
		public BMap ToBMap() => new BMap(MapCompression.decode(careas)) { Area = Area, Center = Center };
	}

	public record MapLight(byte[] careas);

	public class BMap : MapBase
	{
		public BMap(Shapes areas)
		{
			SetAreas(areas);
		}

		public byte[] xareas { get; set; }

		public void SetAreas(Shapes value)
		{
			xareas = serializer.serial_bin(value);
		}

		public Shapes GetShapes() => serializer.deserial_bin(xareas);

		//public double[][][] GetCoords()
		//{
		//	try
		//	{
		//		var shapes = serializer.deserial_bin(xareas);
		//		if (shapes == null || !shapes.Any())
		//			return new double[0][][];
		//		return shapes.Select(s => s.AsArray()).ToArray();
		//	}
		//	catch (Exception ex)
		//	{
		//		return new double[0][][];
		//	}
		//}

		public XPointF[][] GetShape()
		{
			try
			{
				var shapes = serializer.deserial_bin(xareas);
				if (shapes == null || !shapes.Any())
					return new XPointF[0][];
				return shapes.Select(s => s.coordinates.Select(c => (XPointF)c).ToArray()).ToArray();
			}
			catch (Exception ex)
			{
				return new XPointF[0][];
			}
		}

		//public virtual gmapObject ToGmap(string key)
		//{
		//	var feat = new geoFeature();
		//	if (Center == null || double.IsNaN(Center.Latitude) || double.IsNaN(Center.Longitude))
		//	{
		//		var gps = GetCoords().SelectMany(c => c).ToList();
		//		if (!gps.Any())
		//			return feat;
		//		var lon = gps.Average(p => p[1]);
		//		var lat = gps.Average(p => p[0]);
		//		Center = new geoPoint(lat, lon);
		//	}
		//	feat.geometry.coordinates = GetCoords();
		//	feat.properties.Add("key", key);
		//	feat.properties.Add("-idx-lat", Center.Latitude);
		//	feat.properties.Add("-idx-lon", Center.Longitude);

		//	return feat;
		//}
	}

	public class ProjectMap
	{
		public string key { get; set; }
		public DesaMap[] desas { get; set; }

		public IEnumerable<FeatureBase> MakeFeatures()
		{
			var dsattribs = new polyattribs();

			foreach (var x in desas)//.ForEach(dm =>
			{
				var desa = new DesaFeature
				{
					key = x.key,
					nama = x.nama,
					project = x.project,
					prokey = this.key,
					deskey = x.key,
					shapes = x.map.GetShape()
				};
				yield return desa;

				foreach (var l in x.lands)
				{
					var land = new LandFeature
					{
						key = l.key,
						prokey = this.key,
						deskey = x.key,
						data = l.meta,
						//state = l.status,
						shapes = l.map?.GetShape() ?? new XPointF[0][]
					};
					yield return land;
				}
			}
		}
	}

	public class DesaMap
	{
		public string key { get; set; }
		public string project { get; set; }
		public string nama { get; set; }

		public BMap map { get; set; }
		public landMap[] lands { get; set; }
	}

	public class landMap
	{
		public string key { get; set; }
		public string type { get; set; }
		public MetaData meta { get; set; }
		public BMap map { get; set; }
		public landrope.common.LandState status { get; set; }

		protected RincikMap[] _rinciks = null;

		public RincikMap[] rinciks
		{
			get => _rinciks;
			set
			{
				_rinciks = value;
				_skin = null;
				foreach (var child in _rinciks)
					child.SetParent(this);
			}
		}

		[JsonIgnore]
		protected Shapes _skin = null;

		[JsonIgnore]
		protected Shapes _jskin = null;

		[BsonIgnore]
		public Shapes Skin
		{
			get
			{
				if (_skin == null && _jskin == null)
					_skin = GetskinMap();
				return _skin ?? _jskin;
			}
			set { _jskin = value; }
		}

		Shapes GetskinMap()
		{
			var polygons = rinciks.Select(r => r.map.GetShapes().FirstOrDefault().coordinates
								.Select(p => UtmConv.LatLon2UTM(p.Latitude, p.Longitude, 48, true)));
			var solutions = polygons.Union();
			return PolyHelper.ToShapes(solutions.Select(pp => new Shape
			{ coordinates = pp.Select(p => UtmConv.UTM2LatLon(p, 48, true)).Select(p => new geoPoint(p.Latitude, p.Longitude)).ToList() }
			));
		}
	}

	public class RincikMap
	{
		public string IdBidang { get; set; }
		public string IdParent { get; set; }
		public string Pemilik { get; set; }
		public double Luas { get; set; }
		public bool Damai { get; set; }
		public BMap map { get; set; }

		protected landMap _parent;
		public void SetParent(landMap parent)
		{
			_parent = parent;
		}

		public MetaDataR meta => MetaDataR.CopyFrom(_parent);
	}

	public static class PolyHelper
	{
		//internal class MyClipper : Clipper
		//{
		//	public bool free = true;
		//}

		//static List<MyClipper> clippers;

		//static PolyHelper()
		//{
		//	clippers = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(i => new MyClipper()).ToList();
		//}

		//static MyClipper theClipper
		//{
		//	get
		//	{
		//		lock (clippers)
		//		{
		//			var clp = clippers.FirstOrDefault(c => c.free);
		//			if (clp == null)
		//			{
		//				clp = new MyClipper() { free = false };
		//				clippers.Add(clp);
		//			}
		//			else
		//				clp.free = false;
		//			return clp;
		//		}
		//	}
		//}

		static Clipper clipper = new Clipper();

		static double dfactor = 1e10d;
		public static Func<double, Int64> d2i = d => (Int64)Math.Truncate(d * dfactor + 0.5d);
		public static Func<Int64, double> i2d = i => i / dfactor;
		public static Func<geo.shared.Point, IntPoint> pt2ip = pt => new IntPoint(d2i(pt.x), d2i(pt.y));
		public static Func<IntPoint, geo.shared.Point> ip2pt = ip => new geo.shared.Point(i2d(ip.X), i2d(ip.Y));
		//public static Func<Utm, IntPoint> ut2ip = ut => new IntPoint(d2i(ut.x), d2i(ut.y));
		public static Func<IntPoint, Utm> ip2ut = ip => new Utm(i2d(ip.X), i2d(ip.Y), 48, null, true);

		//public static IntPoint ut2ip(Utm ut) => new IntPoint(d2i(ut.x), d2i(ut.y));
		public static IntPoint ut2ip(Utm ut)
		{
			return new IntPoint(d2i(ut.x), d2i(ut.y));
		}
		public static IntPoint ut2ip(double x, double y)
		{
			return new IntPoint(d2i(x), d2i(y));
		}

		public static Shapes ToShapes(IEnumerable<Shape> shps)
		{
			var shapes = new Shapes();
			shapes.AddRange(shps);
			return shapes;
		}

		public static Utm[][] Union(this IEnumerable<IEnumerable<Utm>> polygons)
		{
			//var clipper = theClipper;
			//try
			//{
				var ipolygons = polygons.Select(r => r.Select(p => ut2ip(p)).ToList()).ToList();
				clipper.Clear();
				clipper.AddPaths(ipolygons, PolyType.ptSubject, true);
				var solution = new List<List<IntPoint>>();
				return (!clipper.Execute(ClipType.ctUnion, solution)) ? new Utm[0][] :
					solution.Select(s => s.Select(ip => ip2ut(ip)).ToArray()).ToArray();
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}
		public static PolyTree Intersect(params PolyTree[] polies)
		{
			if (polies.Length == 0)
				return null;

			var data = polies.Select((p, i) => (p, subj: i == 0)).ToArray();
			var nodes = data.SelectMany(t => t.p.Extract(t.subj)).ToArray();
			//var clipper = theClipper;
			//try
			//{
				clipper.Clear();
				foreach (var n in nodes)
					clipper.AddPath(n.verts, n.type, true);
				var solution = new PolyTree();
				return (clipper.Execute(ClipType.ctIntersection, solution)) ? solution : null;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static PolyTree Union(params PolyTree[] polies)
		{
			if (polies.Length == 0)
				return null;

			var data = polies.Select((p, i) => (p, subj: i == 0)).ToArray();
			var nodes = data.SelectMany(t => t.p.Extract(t.subj)).ToArray();
			//var clipper = theClipper;
			//try
			//{
				clipper.Clear();
				foreach (var n in nodes)
					clipper.AddPath(n.verts, n.type, true);
				var solution = new PolyTree();
				var ok = clipper.Execute(ClipType.ctUnion, solution);
				var solution2 = new List<List<IntPoint>>();
				ok = clipper.Execute(ClipType.ctUnion, solution2);
				return solution;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static PolyTree Xor(params PolyTree[] polies)
		{
			if (polies.Length == 0)
				return null;

			var data = polies.Select((p, i) => (p, subj: i == 0)).ToArray();
			var nodes = data.SelectMany(t => t.p.Extract(t.subj)).ToArray();
			//var clipper = theClipper;
			//try
			//{
				clipper.Clear();
				foreach (var n in nodes)
					clipper.AddPath(n.verts, n.type, true);
				var solution = new PolyTree();
				return (clipper.Execute(ClipType.ctXor, solution)) ? solution : null;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static PolyTree Xor(List<List<IntPoint>> polies)
		{
			if (polies.Count == 0)
				return null;

			var data = polies.Select((p, i) => (p, subj: i == 0)).ToArray();
			//var clipper = theClipper;
			//try
			//{
				clipper.Clear();
				foreach (var n in data)
					clipper.AddPath(n.p, n.subj ? PolyType.ptSubject : PolyType.ptClip, true);
				var solution = new PolyTree();
				return (clipper.Execute(ClipType.ctXor, solution)) ? solution : null;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static double ardivisor = dfactor * dfactor;

		public static (double area1, double area2, double areai, double areau) GetAreas(PolyTree p1, PolyTree p2)
		{
			var data = new[] { p1.Extract(true), p2.Extract(false) };
			//var clipper = theClipper;
			//try
			//{
				clipper.Clear();
				foreach (var n in data)
					n.ForEach(p => clipper.AddPath(p.verts, p.type, true));
				var ixsolution = new PolyTree();
				clipper.Execute(ClipType.ctIntersection, ixsolution);
				var uxsolution = new PolyTree();
				clipper.Execute(ClipType.ctUnion, uxsolution);

				var p1area = 0d;
				var p2area = 0d;
				var ixarea = 0d;
				var uxarea = 0d;
				var p1list = p1.Extract(true);
				var p2list = p2.Extract(true);
				var ixlist = ixsolution.Extract(true);
				var uxlist = uxsolution.Extract(true);
				p1list.ForEach(p => p1area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
				p2list.ForEach(p => p2area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
				ixlist.ForEach(p => ixarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
				uxlist.ForEach(p => uxarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
				p1area = p1area < 0 ? -p1area : p1area;
				p2area = p2area < 0 ? -p2area : p2area;
				ixarea = ixarea < 0 ? -ixarea : ixarea;
				uxarea = uxarea < 0 ? -uxarea : uxarea;
				return (p1area / ardivisor, p2area / ardivisor, ixarea / ardivisor, uxarea / ardivisor);
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static (double area1, double area2, double areai) GetIxAreas(PolyTree p1, PolyTree p2)
		{
			if (p1 == null || p2 == null)
				return (0, 0, 0);
			var data = new[] { p1.Extract(true), p2.Extract(false) };
			//var clipper = theClipper;
			//try
			//{
			clipper.Clear();
			foreach (var n in data)
				n.ForEach(p => clipper.AddPath(p.verts, p.type, true));
			var ixsolution = new PolyTree();
			clipper.Execute(ClipType.ctIntersection, ixsolution);
			var uxsolution = new PolyTree();
			clipper.Execute(ClipType.ctUnion, uxsolution);

			var p1area = 0d;
			var p2area = 0d;
			var ixarea = 0d;

			var p1list = p1.Extract(true);
			var p2list = p2.Extract(true);
			var ixlist = ixsolution.Extract(true);

			p1list.ForEach(p => p1area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			p2list.ForEach(p => p2area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			ixlist.ForEach(p => ixarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));

			p1area = p1area < 0 ? -p1area : p1area;
			p2area = p2area < 0 ? -p2area : p2area;
			ixarea = ixarea < 0 ? -ixarea : ixarea;

			return (p1area / ardivisor, p2area / ardivisor, ixarea / ardivisor);
		}

		public static double GetArea(PolyTree pol)
		{
			var data = pol.Extract(true);
			//var clipper = theClipper;
			//try
			//{
				data.ForEach(p => clipper.AddPath(p.verts, p.type, true));

				var parea = 0d;
				var plist = pol.Extract(true);
				plist.ForEach(p => parea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
				parea = parea < 0 ? -parea : parea;
				return parea / ardivisor;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public static List<(List<IntPoint> verts, PolyType type)> Extract(this PolyTree tree, bool asSubject)
		{
			var res = new List<(List<IntPoint> verts, PolyType type)>();
			if (tree.Contour.Any())
				res.Add((tree.Contour, asSubject ? PolyType.ptSubject : PolyType.ptClip));
			tree.Childs.ForEach(n => res.Add((n.Contour, (n.IsHole ^ !asSubject) ? PolyType.ptClip : PolyType.ptSubject)));
			return res;
		}

		public static (bool inside, bool online) PtInPoly(IntPoint pt, List<IntPoint> poly)
		{
			var res = Clipper.PointInPolygon(pt, poly);
			return (res == 1, res == -1);
		}

		public static (bool inside, bool online) PtInShape(geo.shared.Point pt, IEnumerable<geo.shared.Point> poly)
		{
			var ipt = pt2ip(pt);
			var ipoly = poly.Select(p => pt2ip(p)).ToList();
			return PtInPoly(ipt, ipoly);
		}
	}

	public class InsPolyHelper : IDisposable
	{
		public bool free = true;

		Clipper clipper = new Clipper();

		public void Dispose()
		{
			clipper = null;
			GC.Collect();
		}

		public PolyTree Xor(List<List<IntPoint>> polies)
		{
			if (polies.Count == 0)
				return null;

			var data = polies.Select((p, i) => (p, subj: i == 0)).ToArray();
			//var clipper = theClipper;
			//try
			//{
			clipper.Clear();
			foreach (var n in data)
				clipper.AddPath(n.p, n.subj ? PolyType.ptSubject : PolyType.ptClip, true);
			var solution = new PolyTree();
			return (clipper.Execute(ClipType.ctXor, solution)) ? solution : null;
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public (double area1, double area2, double areai, double areau) GetAreas(PolyTree p1, PolyTree p2)
		{
			var data = new[] { p1?.Extract(true) ?? new List<(List<IntPoint> verts, PolyType type)>(),
				p2?.Extract(false) ?? new List<(List<IntPoint> verts, PolyType type)>() };
			//var clipper = theClipper;
			//try
			//{
			clipper.Clear();
			foreach (var n in data)
				n.ForEach(p => clipper.AddPath(p.verts, p.type, true));
			var ixsolution = new PolyTree();
			clipper.Execute(ClipType.ctIntersection, ixsolution);
			var uxsolution = new PolyTree();
			clipper.Execute(ClipType.ctUnion, uxsolution);

			var p1area = 0d;
			var p2area = 0d;
			var ixarea = 0d;
			var uxarea = 0d;
			var p1list = p1?.Extract(true) ?? new List<(List<IntPoint> verts, PolyType type)>();
			var p2list = p2?.Extract(true) ?? new List<(List<IntPoint> verts, PolyType type)>();
			var ixlist = ixsolution.Extract(true);
			var uxlist = uxsolution.Extract(true);
			p1list.ForEach(p => p1area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			p2list.ForEach(p => p2area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			ixlist.ForEach(p => ixarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			uxlist.ForEach(p => uxarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			p1area = p1area < 0 ? -p1area : p1area;
			p2area = p2area < 0 ? -p2area : p2area;
			ixarea = ixarea < 0 ? -ixarea : ixarea;
			uxarea = uxarea < 0 ? -uxarea : uxarea;
			return (p1area / PolyHelper.ardivisor, p2area / PolyHelper.ardivisor, ixarea / PolyHelper.ardivisor, uxarea / PolyHelper.ardivisor);
			//}
			//finally
			//{
			//	clipper.free = true;
			//}
		}

		public (double area1, double area2, double areai) GetIxAreas(PolyTree p1, PolyTree p2)
		{
			if (p1 == null || p2 == null)
				return (0, 0, 0);
			var data = new[] { p1.Extract(true), p2.Extract(false) };
			//var clipper = theClipper;
			//try
			//{
			clipper.Clear();
			foreach (var n in data)
				n.ForEach(p => clipper.AddPath(p.verts, p.type, true));
			var ixsolution = new PolyTree();
			clipper.Execute(ClipType.ctIntersection, ixsolution);
			var uxsolution = new PolyTree();
			clipper.Execute(ClipType.ctUnion, uxsolution);

			var p1area = 0d;
			var p2area = 0d;
			var ixarea = 0d;

			var p1list = p1.Extract(true);
			var p2list = p2.Extract(true);
			var ixlist = ixsolution.Extract(true);

			p1list.ForEach(p => p1area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			p2list.ForEach(p => p2area += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));
			ixlist.ForEach(p => ixarea += (p.type == PolyType.ptSubject) ? Clipper.Area(p.verts) : -Clipper.Area(p.verts));

			p1area = p1area < 0 ? -p1area : p1area;
			p2area = p2area < 0 ? -p2area : p2area;
			ixarea = ixarea < 0 ? -ixarea : ixarea;

			return (p1area / PolyHelper.ardivisor, p2area / PolyHelper.ardivisor, ixarea / PolyHelper.ardivisor);
		}
	}
}



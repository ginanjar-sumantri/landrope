using GeomHelper;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Tracer;
using PolygonCuttingEar;
using Newtonsoft.Json;
using System.Xml.Linq;
using auth.mod;
using System.Runtime.ExceptionServices;
using System.IO;
using geo.shared;
//using Microsoft.CodeAnalysis.Operations;
//using Microsoft.FSharp.Core.CompilerServices;

namespace landrope.mod
{
#if (_USE_MONGODB_)
	using GeoPoint = geoPoint;
#endif
	using polygon = TriangleNet.Geometry.Polygon;
	using vertex = TriangleNet.Geometry.Vertex;
	using contour = TriangleNet.Geometry.Contour;
	using static TriangleNet.Geometry.ExtensionMethods;

	#region Single Shape
	//public class Shape --> moved to mod.shared
	//{
	//	public List<GeoPoint> coordinates { get; set; }

	//	[JsonIgnore]
	//	public double[][] AsArray
	//	{
	//		get
	//		{
	//			var verts = coordinates.ToList();
	//			if (verts[0] != verts[verts.Count - 1])
	//				verts.Add(verts[0]);
	//			return verts.Select(pt => new double[] { pt.Latitude, pt.Longitude, 0 }).ToArray();
	//		}
	//	}
	//}

	public class ShapeXY
	{
		public string id;
		public bool invalid = false;

		public ShapeXY() { id = MongoEntity.MakeKey; }

		List<geo.shared.Point> coords = new List<geo.shared.Point>();

		[BsonRequired]
		public List<geo.shared.Point> coordinates
		{
			get => coords;
			set
			{
				coords = RemoveDuplicate(value).Distinct().ToList();
				invalid = coords.Count < 3;
				//if (!invalid)
				//{
				//	MyTracer.PushProc($"Set Coordinates {id}", "SHPCOORD");
				//	CalcAreaNCheckpoint();
				//	MyTracer.ReplaceProc($"MakeSegments {id}");
				//	MakeSegments();
				//	MyTracer.ClearTo("SHPCOORD");
				//}
			}
		}

		private List<geo.shared.Point> RemoveDuplicate(List<geo.shared.Point> points)
		{
			var result = points.Take(1).ToList();
			for (int i = 1; i < points.Count; i++)
				if (points[i] != points[i - 1])
					result.Add(points[i]);
			return result;
		}

		internal IEnumerable<Segment> segments = new List<Segment>();
		internal (double min, double max) xminmax = default;
		internal (double min, double max) yminmax = default;
		public double Area = 0;
		public geo.shared.Point Center = new geo.shared.Point(0, 0);
		internal void MakeSegments()
		{
			if (invalid)
			{
				segments = new List<Segment>();
				xminmax = default;
				yminmax = default;
				return;
			}

			var koord2 = coords.Skip(1).ToList();
			koord2.Add(coords.First());

			//var par = coords.Count() > 100;
			var segs = coords.Select((p, i) => (p, i)).Join(koord2.Select((p, i) => (p, i)), x0 => x0.i, x1 => x1.i,
												(x0, x1) => new Segment(x0.p, x1.p)).ToList();
			segments = segs;// par ? (IEnumerable<Segment>)segs.AsParallel() : segs;
			xminmax = (min: coords.Min(k => k.x), max: coords.Max(k => k.x));
			yminmax = (min: coords.Min(k => k.y), max: coords.Max(k => k.y));
		}

		//public bool IsInside(Point obs)
		//{
		//	if (segments == null)
		//		MakeSegments();
		//	if (obs.x < xminmax.min || obs.x > xminmax.max || obs.y < yminmax.min || obs.y > yminmax.max)
		//		return false;

		//	var segtest = new Segment(new Point(xminmax.min - 10, obs.y), obs);
		//	var intx = segments.Select(s => s.IsIntersect2(segtest)).Where(s => s.ints).ToList();
		//	int cnt = intx.Count - intx.Where(x => x.invtx).Count();
		//	return cnt % 2 == 1;
		//}

		//public bool IsHole(ShapeXY main) => coords.All(c => main.IsInside(c));

		public void CalcAreaNCenter()
		{
			var poly = new CPolygon(coords.Select(p=>p.ToCPoint()));
			Area = poly.Area;
			Center = new Point(poly.Center.X,poly.Center.Y);
		}

		//void CalcAreaNCheckpoint()
		//{
		//	try
		//	{
		//		MyTracer.PushProc("CalcAreaNCheckpoint", "CANC");
		//		MyTracer.PushProc("Create Polygon");
		//		var pol = new CPolygon(coords.Select(p => p.ToCPoint()) ;// PolygonShape(coords.Select(p => p.ToCPoint()));
		//		MyTracer.ReplaceProc("CutEar");
		//		try
		//		{
		//			pol.CutEars();
		//		}
		//		catch (Exception ex)
		//		{
		//		}
		//		var meshes = pol.Triangles;
		//		MyTracer.ReplaceProc("Calc Area");
		//		var areas = meshes.Select((t, i) => (t.Area, i)).ToList();
		//		Area = areas.Select(t => t.Area).Sum();
		//		MyTracer.ReplaceProc("Find Center");
		//		var centers = meshes.Select((t, i) => (t.Center, i)).ToList();
		//		var combos = areas.Join(centers, a => a.i, c => c.i, (a, c) => (a.Area, c.Center)).ToList();
		//		Center = new Point(combos.Sum(x => x.Area * x.Center.X) / Area, combos.Sum(x => x.Area * x.Center.Y) / Area);
		//		MyTracer.ClearTo("CANC");
		//	}
		//	catch (Exception ex)
		//	{
		//		var proc = MyTracer.GetProcs("CANC");
		//		throw (ex);
		//	}
		//}

		//public static implicit operator Shape(ShapeXY shpx) => new Shape { coordinates = shpx.coordinates.Select(c=>c.ToLatLon()).ToList()};
	}
	#endregion //Single Shape

	#region Shape List
	//public class Shapes : List<Shape> --> moved to mod.shared
	//{
	//	public gmapObject ToGmap()
	//	{
	//		geoPolygon poly = new geoPolygon();
	//		poly.coordinates = this.Select(a => a.AsArray).ToArray();

	//		geoFeature feat = new geoFeature();

	//		feat.geometry = poly;
	//		return feat;
	//	}
	//}

	public class ShapesXY : List<ShapeXY>
	{
		IEnumerable<Segment> segments = new List<Segment>();
		(double min, double max) xminmax = default;
		(double min, double max) yminmax = default;

		public void Fill(IEnumerable<ShapeXY> shapes)
		{
			AddRange(shapes.Where(s => !s.invalid));
			CollectSegments();
		}

		public void Put(ShapeXY shp)
		{
			if (!shp.invalid)
				Add(shp);
		}

		void CollectSegments()
		{
			if (Count == 0)
			{
				segments = new List<Segment>();
				xminmax = yminmax = default;
				return;
			}
			this.ForEach(s => s.MakeSegments());
			segments = this.SelectMany(s => s.segments);
			xminmax = (min: this.Select(s => s.xminmax.min).Min(), max: this.Select(s => s.xminmax.max).Max());
			yminmax = (min: this.Select(s => s.yminmax.min).Min(), max: this.Select(s => s.yminmax.max).Max());
		}

		internal class PolyTreeNode
		{
			public CPolygon value;
			public PolyTreeNode outer = null;
			public PolyTreeNode inner = null;
			public PolyTreeNode sibling = null;

			public double GetArea() =>value.Area - inner?.GetArea()??0 + sibling?.GetArea()??0;

			public CPoint2D GetCenter()
			{
				var self = (value.Center, value.Area);
				var inners = inner == null ? (Center:new CPoint2D(0, 0), Area:0) : (Center:inner.GetCenter(), Area:inner.GetArea());
				var siblings = sibling == null ? (Center: new CPoint2D(0, 0), Area: 0) : (Center: sibling.GetCenter(), Area: sibling.GetArea());
				var totarea = self.Area + siblings.Area - inners.Area;
				if (totarea == 0)
					return value.Center;
				var X = (self.Center.X * self.Area + siblings.Center.X * siblings.Area - inners.Center.X * inners.Area) / totarea;
				var Y = (self.Center.Y * self.Area + siblings.Center.Y * siblings.Area - inners.Center.Y * inners.Area) / totarea;
				return new CPoint2D(X, Y);
			}
			public override int GetHashCode() => JsonConvert.SerializeObject(this).GetHashCode();

			public override bool Equals(object obj)
			{
				return obj is PolyTreeNode && this.GetHashCode()==obj.GetHashCode();
			}
			public void SetOuter(PolyTreeNode node)
			{
				outer = node;
				node.inner = this;
			}

			public void SetInner(PolyTreeNode node)
			{
				if (inner == null)
				{
					node.outer = this;
					inner = node;
					return;
				}
				var rel = inner.value.GetRelation(node.value);
				switch (rel)
				{
					case PolygonRelation.Inner:
						inner.SetOuter(node);
						inner = outer;
						break;
					case PolygonRelation.Outer:
						inner.SetInner(node); break;
					default:
						inner.SetSibling(node); break;
				}
			}

			public void SetSibling(PolyTreeNode node)
			{
				if (sibling==null)
				{
					sibling = node;
					return;
				}
				var rel = sibling.value.GetRelation(node.value);
				switch (rel)
				{
					case PolygonRelation.Outer:
						sibling.SetInner(node); break;
					case PolygonRelation.Inner:
						node.SetInner(sibling);
						sibling = node; 
						break;
					default:
						sibling.SetSibling(node); break;
				}
			}

			public static bool LogPoints = false;
			public PolyTreeNode AddPoly(CPolygon poly)
			{
				CPolygon.LogPoints = LogPoints;

				var node = new PolyTreeNode { value = poly };
				var rel = value.GetRelation(poly);
				if (rel == PolygonRelation.Inner) // this node is inner
				{
					SetOuter(node);
					return node;
				}
				if (rel == PolygonRelation.Outer)
					SetInner(node);
				else
					SetSibling(node);
				return this;
			}
		}

		internal class PolyTree
		{
			public PolyTreeNode top;

			public PolyTree(CPolygon poly)
			{
				top = new PolyTreeNode { value = poly };
				if (!Directory.Exists(@"C:\landrope\logpoints"))
					Directory.CreateDirectory(@"C:\landrope\logpoints");
			}

			public void Add(CPolygon poly)
			{
				//top.value.CalcAreaNCenter();
				top = top.AddPoly(poly);
			}
		}

		public (double area, geo.shared.Point center) GetAreaNCentroid()
		{
			MyTracer.PushProc("GANC Init", "GANC");
			if (this.Count==1)
			{
				MyTracer.ReplaceProc("Single Poly");
				var shp = this.First();
				MyTracer.ReplaceProc($"Single Poly <{shp.coordinates.Count}> ({shp.coordinates[0].x},{shp.coordinates[0].y})");
				shp.CalcAreaNCenter();
				MyTracer.ClearTo("GANC");
				return (area: shp.Area, center: shp.Center);
			}

			MyTracer.ReplaceProc("Multi Poly");
			var polies = this.Select(s => new CPolygon(s.coordinates.Select(c => c.ToCPoint())))
															.OrderByDescending(s=>s.Area);

			MyTracer.ReplaceProc("Init Tree");
			var first = polies.First();
			var tree = new PolyTree(first);
			MyTracer.ReplaceProc("Fill Tree");
			polies.Skip(1).ToList().ForEach(p=>tree.Add(p));
			MyTracer.ReplaceProc("Summing Area");
			var area = tree.top.GetArea();
			CPoint2D center = tree.top.GetCenter();// value.Center;
			MyTracer.ClearTo("GANC");
			return (area, new geo.shared.Point(center.X,center.Y));

			//double GetArea(IEnumerable<CPoint2D> points) =>new CPolygon(points).Area;

			//var polies = cpoints.Select((c, i) => (p: new CPolygon(c), i)).ToList();
			//polies.ForEach(p => p.p.CalcAreaNCenter());
			//var polrel = polies.Join(polies, p1 => 1, p2 => 1, (p1, p2) =>
			//			{
			//				if (p1.i == p2.i) return (p1: p1.p, p2: p1.p, rel: PolygonRelation.Undefined);
			//				return (p1: p1.p, p2: p2.p, rel: p1.p.GetRelation(p2.p));
			//			}).Where(p => p.rel != PolygonRelation.Undefined).ToList();
			//var outpols = polrel.GroupBy(p => p.p1).Select(g => (outer: g.Key, inners: g.Select(p => (node: p.p2, p.rel)).ToArray())).ToList();
			//var outers = outpols.Where(p => !p.inners.Any(i => i.rel != PolygonRelation.Inner));
			//var pouters = outers.Select(p => new PolyTreeNode
			//{
			//	value = p.outer,
			//	inners = p.inners.Select(i => new PolyTreeNode
			//	{
			//		value = i.node
			//	}).ToList()
			//}).ToList();
			//var inners = outpols.Where(p => !p.inners.Any(i => i.rel != PolygonRelation.Outer));
			//var pinners = inners.Select(p => new PolyTreeNode { value = p.outer });

			//var areas = this.Select((s, i) => (Area: s.Area * (IsHole(s) ? -1 : 1), i)).ToList();
			//var centers = this.Select((s, i) => (s.Center, i)).ToList();
			//var area = areas.Select(s => s.Area).Sum();
			//areas = areas.Where(a => a.Area > 0).ToList();
			//var combos = areas.Join(centers, a => a.i, c => c.i, (a, c) => (a.Area, c.Center)).ToList();
			//var totarea = areas.Sum(a => a.Area);
			//var center = new Point(combos.Sum(x => x.Area * x.Center.x) / totarea, combos.Sum(x => x.Area * x.Center.y) / totarea);
			//return (area, center);
		}

		bool IsHole(ShapeXY shp)
		{
			if (Count == 1)
				return false;
			var obs = shp.Center;
			var segtestX = new Segment(new Point(xminmax.min - (xminmax.max - xminmax.min) * 0.1, obs.y), obs);
			var segtestY = new Segment(new Point(obs.x, yminmax.min - (yminmax.max - yminmax.min) * 0.1), obs);
			var intx = segments.Select(s => s.IsIntersect2(segtestX)).Where(s => s.ints).ToList();
			int cnt;
			if (intx.Count > 0)
			{
				cnt = intx.Count - intx.Where(x => x.invtx).Count();
				return cnt % 2 == 0;
			}
			var inty = segments.Select(s => s.IsIntersect2(segtestY)).Where(s => s.ints).ToList();
			if (inty.Count > 0)
			{
				cnt = inty.Count - inty.Where(x => x.invtx).Count();
				return cnt % 2 == 0;
			}
			return false;
		}
	}


	#endregion //Shape List

}

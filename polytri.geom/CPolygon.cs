using cutear;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace PolygonCuttingEar
{
	/// <summary>
	/// Modified by JJ on 2020-01-22
	/// using LinkedList&lt;T&gt;
	/// </summary>
	public class CPolygon
	{
		LinkedList<CPoint2D> Vertices = new LinkedList<CPoint2D>();

		List<CPoint2D> AsList => ((IEnumerable<CPoint2D>)Vertices).ToList();
		public CPoint2D this[int index]
		{
			set
			{
				var ls = AsList;
				ls[index] = value;
				Vertices = new LinkedList<CPoint2D>(ls);
			}
			get
			{
				return AsList[index];
			}
		}

		public CPolygon()
		{

		}

		public CPolygon(IEnumerable<CPoint2D> points)
		{
			try
			{
				if (points.Count() < 3)
				{
					throw new InvalidInputGeometryDataException();
				}
				else
				{
					Vertices = new LinkedList<CPoint2D>(points);
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine(
					e.Message + e.StackTrace);
			}
		}

		/***********************************
		 From a given point, get its vertex index.
		 If the given point is not a polygon vertex, 
		 it will return -1 
		 ***********************************/
		public int VertexIndex(CPoint2D vertex) => AsList.IndexOf(vertex);

		protected static LinkedListNode<CPoint2D> PreviousNode(LinkedListNode<CPoint2D> curr) => curr?.Previous ?? curr?.List?.Last;
		protected static LinkedListNode<CPoint2D> NextNode(LinkedListNode<CPoint2D> curr) => curr?.Next ?? curr?.List.First;

		/***********************************
		 From a given vertex, get its previous vertex point.
		 If the given point is the first one, 
		 it will return  the last vertex;
		 If the given point is not a polygon vertex, 
		 it will return null; 
		 ***********************************/
		public CPoint2D PreviousPoint(CPoint2D vertex) => PreviousNode(Vertices.Find(vertex))?.Value;
		/***************************************
			 From a given vertex, get its next vertex point.
			 If the given point is the last one, 
			 it will return  the first vertex;
			 If the given point is not a polygon vertex, 
			 it will return null; 
		***************************************/
		public CPoint2D NextPoint(CPoint2D vertex) => NextNode(Vertices.Find(vertex))?.Value;


		/******************************************
		To calculate the polygon's area

		Good for polygon with holes, but the vertices make the 
		hole  should be in different direction with bounding 
		polygon.
		
		Restriction: the polygon is not self intersecting
		ref: www.swin.edu.au/astronomy/pbourke/
			geometry/polyarea/
		*******************************************/
		public double PolygonArea()
		{
			var ls = AsList;

			var ls2 = ls.Skip(1).ToList();
			ls2.Add(ls[0]);

			return ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
													(A, B) => A.p.X * B.p.Y - A.p.Y * B.p.X).Sum() / 2d;
			//return Math.Abs(area);
		}

		/******************************************
		To calculate the area of polygon made by given points 

		Good for polygon with holes, but the vertices make the 
		hole  should be in different direction with bounding 
		polygon.
		
		Restriction: the polygon is not self intersecting
		ref: www.swin.edu.au/astronomy/pbourke/
			geometry/polyarea/

		As polygon in different direction, the result coulb be
		in different sign:
		If dblArea>0 : polygon in clock wise to the user 
		If dblArea<0: polygon in count clock wise to the user 		
		*******************************************/

		/***********************************************
			To check a vertex concave point or a convex point
			-----------------------------------------------------------
			The out polygon is in count clock-wise direction
		************************************************/
		public VertexType GetVertexType(CPoint2D vertex)
		{
			var curr = Vertices.Find(vertex);
			if (curr == null)
				return VertexType.ErrorPoint;
			var prev = PreviousNode(curr);
			var next = NextNode(curr);
			if (IsOwned(vertex))
			{
				var backSeg = vertex.MakeRevSegment(prev.Value);
				var frontSeg = vertex.MakeSegment(next.Value);
				var sign = backSeg.Sign(frontSeg);
				return sign > 0 ? VertexType.ConvexPoint : sign < 0 ? VertexType.ConcavePoint : VertexType.ErrorPoint;
			}
			return VertexType.ErrorPoint;
		}

		public List<VertexType> GetVertexTypes()
		{
			var ls = AsList;
			var ls2 = ls.Skip(1).ToList();
			ls2.Add(ls.First());
			var ls0 = ls.ToArray().ToList();
			ls2.Insert(0, ls.Last());

			var fronts = ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
																				(A, B) => (s: A.p.MakeSegment(B.p), A.i));
			var backs = ls.Select((p, i) => (p, i)).Join(ls0.Select((p, i) => (p, i)), A => A.i, B => B.i,
																				(A, B) => (s: A.p.MakeRevSegment(B.p), A.i));
			var signs = backs.Join(fronts, back => back.i, front => front.i, (back, front) => back.s.Sign(front.s));
			return signs.Select(s => s > 0 ? VertexType.ConvexPoint : s < 0 ? VertexType.ConcavePoint : VertexType.ErrorPoint).ToList();
		}


		/*********************************************
		To check the Line of vertex1, vertex2 is a Diagonal or not
  
		To be a diagonal, Line vertex1-vertex2 has no intersection 
		with polygon lines.
		
		If it is a diagonal, return true;
		If it is not a diagonal, return false;
		reference: www.swin.edu.au/astronomy/pbourke
		/geometry/lineline2d
		*********************************************/
		public bool Diagonal(CPoint2D vertex1, CPoint2D vertex2)
		{
			var n1 = Vertices.Find(vertex1);
			var n2 = Vertices.Find(vertex2);
			if (n2 == NextNode(n1) || n2 == PreviousNode(n1))
				return false;

			var chk = vertex1.MakeSegment(vertex2);
			var mid = chk.Center;

			var ptpos = this.GetPointPosition(mid);
			if (ptpos != PointPosition.Inside)
				return false;

			//var ls = AsList;
			//var ls2 = ls.Skip(1).ToList();
			//ls2.Add(ls.First());
			var segs = MakeSegments();
			//ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
			//						(A, B) => A.p.MakeSegment(B.p));
			//var segs = segsn.Where(s => s.Pt1 != vertex1 && s.Pt1 != vertex2 && s.Pt2 != vertex1 && s.Pt2 != vertex2);
			var intx = segs.Select(s => s.Intersection(chk))
				.Where(x => !(x.x == vertex1.X && x.y == vertex1.Y) && !(x.x == vertex2.X && x.y == vertex2.Y));
			return !intx.Any();

			//double x1 = vertex1.X;
			//double y1 = vertex1.Y;
			//double x2 = vertex2.X;
			//double y2 = vertex2.Y;

			//var ls2 = ls.Skip(1).ToList();
			//ls2.Add(ls.First());

			//var directs = ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
			//					(A, B) => IsDirect(A.p, B.p)).ToList();
			//return directs.All(d => d);

			//for (int i= 0; i<count; i++) //each point
			//{
			//	var j= (i+1) % count;  //next point of i

			//	//Diagonal line:

			//	//CPolygon line:
			//	double x3=ls[i].X;
			//	double y3=ls[i].Y;
			//	double x4=ls[j].X;
			//	double y4=ls[j].Y;

			//	double de=(y4-y3)*(x2-x1)-(x4-x3)*(y2-y1);
			//	double ub=-1;

			//	if (de > ConstantValue.SmallValue || de < -ConstantValue.SmallValue)  //lines are not parallel
			//		ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / de;

			//	if ((ub> 0) && (ub<1))
			//	{
			//		bDiagonal=false;
			//		break;
			//	}
			//}
			//return bDiagonal;

			// checks whether this segment far apart from target segment or not
			//bool IsDirect(CPoint2D p1, CPoint2D p2)
			//{
			//	var de = (p2.Y - p1.Y) * (x2 - x1) - (p2.X - p1.X) * (y2 - y1);
			//	if (ConstantValue.NearZero(de))  //lines are parallel
			//		return true;

			//	var ub = ((x2 - x1) * (y1 - p1.Y) - (y2 - y1) * (x1 - p1.X)) / de;
			//	return (ub < 0) || (ub > 1);
			//}
		}


		/*************************************************
		To check FaVertices make a convex polygon or 
		concave polygon

		Restriction: the polygon is not self intersecting
		Ref: www.swin.edu.au/astronomy/pbourke
		/geometry/clockwise/index.html
		********************************************/
		public PolygonType GetPolygonType()
						=> GetVertexTypes().Any(t => t == VertexType.ConcavePoint) ? PolygonType.Concave : PolygonType.Convex;
		//{
		//int nNumOfVertices=Vertices.Length;
		//bool bSignChanged=false;
		//int nCount=0;
		//int j=0, k=0;

		//for (int i=0; i<nNumOfVertices; i++)
		//{
		//	j=(i+1) % nNumOfVertices; //j:=i+1;
		//	k=(i+2) % nNumOfVertices; //k:=i+2;

		//	double crossProduct=(Vertices[j].X- Vertices[i].X)
		//		*(Vertices[k].Y- Vertices[j].Y);
		//	crossProduct=crossProduct-(
		//		(Vertices[j].Y- Vertices[i].Y)
		//		*(Vertices[k].X- Vertices[j].X)
		//		);

		//	//change the value of nCount
		//	if ((crossProduct>0) && (nCount==0) )
		//		nCount=1;
		//	else if ((crossProduct<0) && (nCount==0))
		//		nCount=-1;

		//	if (((nCount==1) && (crossProduct<0))
		//		||( (nCount==-1) && (crossProduct>0)) )
		//		bSignChanged=true;
		//}

		//if (bSignChanged)
		//	return PolygonType.Concave;
		//else
		//	return PolygonType.Convex;
		//}

		/***************************************************
		Check a Vertex is a principal vertex or not
		ref. www-cgrl.cs.mcgill.ca/~godfried/teaching/
		cg-projects/97/Ian/glossay.html
  
		PrincipalVertex: a vertex pi of polygon P is a principal vertex if the
		diagonal pi-1, pi+1 intersects the boundary of P only at pi-1 and pi+1.
		*********************************************************/
		public bool PrincipalVertex(CPoint2D vertex)
		{
			bool bPrincipal = false;
			if (IsOwned(vertex)) //valid vertex
			{
				CPoint2D pt1 = PreviousPoint(vertex);
				CPoint2D pt2 = NextPoint(vertex);

				if (Diagonal(pt1, pt2))
					bPrincipal = true;
			}
			return bPrincipal;
		}

		/*********************************************
        To check whether a given point is a CPolygon Vertex
		**********************************************/
		public bool IsOwned(CPoint2D point) => Vertices.Find(point) != null;
		//{
		//	bool bVertex=false;
		//	int nIndex=VertexIndex(point);

		//	if ((nIndex>=0) && (nIndex<=Vertices.Length-1))
		//					   bVertex=true;

		//	return bVertex;
		//}

		/*****************************************************
		To reverse polygon vertices to different direction:
		clock-wise <------->count-clock-wise
		******************************************************/
		public void ReverseWise()
		{
			var ls = AsList;
			ls.Reverse();
			Vertices = new LinkedList<CPoint2D>(ls);
		}

		/*****************************************
		To check vertices make a clock-wise polygon or
		count clockwise polygon

		Restriction: the polygon is not self intersecting
		Ref: www.swin.edu.au/astronomy/pbourke/
		geometry/clockwise/index.html
		*****************************************/
		public PolygonWise Wise
		{
			get
			{
				var points = AsList;
				if (points.Count < 3)
					return PolygonWise.Unknown;

				var segments = points.Select((p, i) => (p, i)).Join(points.Skip(1).Select((p, i) => (p, i)), A => A.i, B => B.i,
											(A, B) => A.p.MakeSegment(B.p)).ToList();
				segments.Add(segments.First());
				var totsign = segments.Select((s, i) => (s, i)).Join(segments.Skip(1).Select((s, i) => (s, i)), A => A.i, B => B.i,
									(A, B) => A.s.Sign(B.s)).Sum();

				return totsign < 0 ? PolygonWise.Clockwise : totsign > 0 ? PolygonWise.AntiClockwise : PolygonWise.Unknown;

				//int nCount=0, j=0, k=0;
				//int nVertices=ls.Count;

				//for (int i=0; i<nVertices; i++)
				//{
				//	j=(i+1) % nVertices; //j:=i+1;
				//	k=(i+2) % nVertices; //k:=i+2;

				//	double crossProduct=(ls[j].X - ls[i].X)
				//		*(ls[k].Y- ls[j].Y);
				//	crossProduct=crossProduct-(
				//		(ls[j].Y- ls[i].Y)
				//		*(ls[k].X- ls[j].X)
				//		);

				//	if (crossProduct>0)
				//		nCount++;
				//	else
				//		nCount--;
				//}

				//if( nCount<0) 
				//	return PolygonDirection.Count_Clockwise;
				//else if (nCount> 0)
				//	return PolygonDirection.Clockwise;
				//else
				//	return PolygonDirection.Unknown;
			}
		}


		/*****************************************
		To check given points make a clock-wise polygon or
		count clockwise polygon

		Restriction: the polygon is not self intersecting
		*****************************************/
		public static PolygonWise getWise(List<CPoint2D> points)
		{
			if (points.Count < 3)
				return PolygonWise.Unknown;

			var segments = points.Select((p, i) => (p, i)).Join(points.Skip(1).Select((p, i) => (p, i)), A => A.i, B => B.i,
										(A, B) => A.p.MakeSegment(B.p)).ToList();
			segments.Add(segments.First());
			var totsign = segments.Select((s, i) => (s, i)).Join(segments.Skip(1).Select((s, i) => (s, i)), A => A.i, B => B.i,
								(A, B) => A.s.Sign(B.s)).Sum();

			return totsign < 0 ? PolygonWise.Clockwise : totsign > 0 ? PolygonWise.AntiClockwise : PolygonWise.Unknown;

			//int nCount=0, j=0, k=0;
			//int nPoints=points.Count;

			//if (nPoints<3)
			//	return PolygonDirection.Unknown;

			//var cpart1 = points.Skip(1).ToList();
			//cpart1.AddRange(points.Take(1));
			//var cpart2 = points.Skip(2).ToList();
			//cpart2.AddRange(points.Take(2));

			//var combo1 = cpart1.Select((p, i) => (p, i)).ToList();
			//var seg1 = combo1.Join(points.Select((p, i) => (p, i)), p1 => p1.i, p2 => p2.i,
			//					(p1, p2) => (dX:(p2.p.X - p1.p.X), dY:(p2.p.Y - p1.p.Y), p1.i)).ToList();
			//var seg2 = cpart2.Select((p, i) => (p, i)).Join(combo1, p1 => p1.i, p2 => p2.i,
			//					(p1, p2) => (dX:(p2.p.X - p1.p.X), dY:(p2.p.Y - p1.p.Y), p1.i)).ToList();
			//var cross = seg1.Join(seg2, S1 => S1.i, S2 => S2.i, (S1, S2) => S1.dX * S2.dY - S1.dY * S2.dX)
			//						.Select(x => x>0?1:-1).Sum();

			//for (int i=0; i<nPoints; i++)
			//{
			//	j=(i+1) % nPoints; //j:=i+1;
			//	k=(i+2) % nPoints; //k:=i+2;

			//	var crossProduct = (points[j].X - points[i].X) * (points[k].Y - points[j].Y) -
			//										 (points[j].Y - points[i].Y) * (points[k].X - points[j].X);

			//	if (crossProduct>0)
			//		nCount++;
			//	else
			//		nCount--;
			//}

			//return (cross < 0) ? PolygonDirection.Count_Clockwise : (cross > 0) ? PolygonDirection.Clockwise : PolygonDirection.Unknown;
		}

		/*****************************************************
		To reverse points to different direction (order) :
		******************************************************/
		public static void ReverseWise(List<CPoint2D> points)
		{
			points.Reverse();
		}

		(double min, double max) xminmax = default;
		(double min, double max) yminmax = default;

		public void Simplified(bool backup = true)
		{
			if (backup)
				Backup();
			var ls = AsList;
			ls = ls.Select(p => p.Simplified()).ToList();
			Vertices = new LinkedList<CPoint2D>(ls);
		}

		List<CPoint2D> reserced = null;
		public void Backup()
		{
			reserced = AsList;
		}

		public void Restore()
		{
			if (reserced != null)
			{
				Vertices = new LinkedList<CPoint2D>(reserced);
				reserced = null;
			}
		}

		void SetMinMax()
		{
			var ls = AsList;
			xminmax = (min: ls.Min(p => p.X), max: ls.Max(p => p.X));
			yminmax = (min: ls.Min(p => p.Y), max: ls.Max(p => p.Y));
		}

		Random rnd = new Random(DateTime.Now.GetHashCode());
		public PointPosition GetPointPosition(CPoint2D point)
		{
			var ls = AsList;
			if (ls.Count < 3)
				return PointPosition.Outside;
			if (xminmax == default)
				SetMinMax();

			//var randoms = Enumerable.Range(1, 10).Select(i => rnd.Next(30, 90)).ToList();
			//var observeds = new[] {
			//		point.MakeRevSegment(new CPoint2D(xminmax.min - 100, point.Y)),
			//		point.MakeRevSegment(new CPoint2D(point.X,yminmax.min-100)),
			//		point.MakeRevSegment(new CPoint2D(xminmax.min - 100, yminmax.min - 100)),
			//		point.MakeSegment(new CPoint2D(xminmax.max + 100, point.Y)),
			//		point.MakeSegment(new CPoint2D(point.X, yminmax.max + 100)),
			//		point.MakeSegment(new CPoint2D(xminmax.max + 100, yminmax.max + 100)),
			//}.ToList();
			//observeds.AddRange(
			//	randoms.Select(r=> point.MakeRevSegment(new CPoint2D(xminmax.min - r, yminmax.min - 20)))
			//	);
			//observeds.AddRange(
			//	randoms.Select(r => point.MakeRevSegment(new CPoint2D(xminmax.min - 20, yminmax.min - r)))
			//	);
			//observeds.AddRange(
			//	randoms.Select(r => point.MakeSegment(new CPoint2D(xminmax.max + r, yminmax.max + 20)))
			//	);
			//observeds.AddRange(
			//	randoms.Select(r => point.MakeSegment(new CPoint2D(xminmax.max + 20, yminmax.max + r)))
			//	);

			// make polygon segments
			var ls2 = ls.Skip(1).ToList();
			ls2.Add(ls[0]);
			var segs = ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
																	(A, B) => A.p.MakeSegment(B.p)).ToList();

			foreach (CSegment2D seg in segs)
				if (seg.InlLine(point))
					return PointPosition.OnEdge;
			var mids = segs.Select(s => s.Center).ToList();
			var dists = mids.Select(m => (m, dx: m.X - point.X, dy: m.Y - point.Y))
									.Select(d => (d.m, dist: d.dx * d.dx + d.dy * d.dy))
									.OrderBy(x => x.dist).ToList();

			foreach (var dist in dists)
			{
				var mid = dist.m;
				var obs = point.MakeSegment(mid);
				(double x, double y) ext;
				var xdists = (min: mid.X - xminmax.min, max: xminmax.max - mid.X);
				var ydists = (min: mid.Y - yminmax.min, max: yminmax.max - mid.Y);
				xdists = (xdists.min * xdists.min, xdists.max * xdists.max);
				ydists = (ydists.min * ydists.min, ydists.max * ydists.max);
				var xdx = xdists.min < xdists.max ? (d: xdists.min, p: xminmax.min) : (d: xdists.max, p: xminmax.max);
				var ydy = ydists.min < ydists.max ? (d: ydists.min, p: yminmax.min) : (d: ydists.max, p: yminmax.max);

				var obd = obs.Delta;
				if (obd.dy == 0)
					ext = (xdx.p, double.NaN);
				else if (obd.dx == 0)
					ext = (xdx.p, double.NaN);
				else if (xdx.d < ydy.d)
					ext = (xdx.p, double.NaN);
				else
					ext = (double.NaN, ydy.p);
				obs.Xtend(ext);

				var res = segs.Select(s => s.Intersecting(obs)).ToList();
				var onvertex = res.Any(p => p.iv);
				if (onvertex)
					continue;

				var count = res.Sum(p => p.ix ? 1 : 0);
				return count % 2 == 1 ? PointPosition.Inside : PointPosition.Outside;
			}
			throw new InvalidOperationException("Cannot deterimine is inside or not");
		}

		public static bool LogPoints = false;

		public PolygonRelation GetRelation(CPolygon other)
		{
			var fn = $@"C:\landrope\logpoints\{DateTime.Now:yyMMdd.HHmmss.fff}.csv";

			SetMinMax();

			Simplified();
			other.Simplified();
			try
			{
				var ls = AsList;
				var lso = other.AsList;

				if (LogPoints)
				{
					ls.SaveTo(fn);
					lso.AppendTo(fn);
					lso.RemoveAt(lso.Count - 1);
					Thread.Sleep(1);
				}

				var ins = lso.Select(p => GetPointPosition(p)).ToList();
				if (LogPoints)
				{
					var strm = new FileStream(fn, FileMode.Open);
					var wrt = new StreamWriter(strm);
					strm.Seek(0, SeekOrigin.End);
					wrt.WriteLine($"Result : {string.Join(",", ins.Select(i => i.ToString()))}");
					//wrt.WriteLine(ins);
					wrt.Flush();
					strm.Flush();
					strm.Close();
				}
				if (!ins.Any(b => b == PointPosition.Outside))
					return PolygonRelation.Outer; // poly 2 inside poly 1
				if (ins.Any(b => b == PointPosition.Inside) && ins.Any(b => b == PointPosition.Outside))
					return PolygonRelation.Intersecting; // poly 1 and poly 2 are intersecting

				var oins = ls.Select(p => other.GetPointPosition(p)).ToList();
				if (!ins.Any(b => b == PointPosition.Outside))
					return PolygonRelation.Inner; // poly 2 outside poly 1
				if (!ins.Any(b => b == PointPosition.Inside))
					return PolygonRelation.Separated; // poly 2 and poly 1 are separated
				return PolygonRelation.Undefined;
			}
			finally
			{
				other.Restore();
				Restore();
			}
		}

		List<CPoint2D[]> Ears = new List<CPoint2D[]>();
		LinkedListNode<CPoint2D> FindEar(LinkedListNode<CPoint2D> current)
		{
			if (Vertices.Count() == 3)
				return current;
			if (current == null)
				return null;
			if (Wise == PolygonWise.Clockwise)
			{
				var pt = current.Value;
				ReverseWise();
				current = Vertices.Find(pt);
			}
			var curr = current;
			while (true)
			{
				bool convex = GetVertexType(curr.Value) == VertexType.ConvexPoint;
				if (!convex)
				{
					curr = NextNode(curr);
					if (curr == current)
						return null;
					continue;
				}
				if (IsEar())
					return curr;
				curr = NextNode(curr);
				if (curr == current)
					return null;
			}

			bool IsEar()
			{
				CPoint2D pi = curr.Value;
				CPoint2D pj = PreviousNode(curr).Value; //previous vertex
				CPoint2D pk = NextNode(curr).Value;//next vertex

				var tri = Triangle.Create(pj, pi, pk);
				var ls = AsList;
				foreach (var pt in ls)
				{
					if (!tri.IsVertex(pt) && tri.Contains(pt))
						return false;
				}
				return true;
			}
		}

		LinkedListNode<CPoint2D> CutEar(LinkedListNode<CPoint2D> node)
		{
			var prev = PreviousNode(node);
			var next = NextNode(node);
			var Ear = new[] { prev.Value, node.Value, next.Value };
			Ears.Add(Ear);
			Vertices.Remove(node);
			return prev;
		}

		public void CutEars()
		{
			var fn = $@"C:\landrope\logpoints\cutears-{DateTime.Now:yyMMdd.HHmmss.fff}.csv";
			FileStream strm = null;
			StreamWriter wrt = null;
			Ears.Clear();
			if (Vertices.Count() < 3)
				return;
			if (LogPoints)
			{
				strm = new FileStream(fn, FileMode.Create);
				wrt = new StreamWriter(strm);
				wrt.WriteLine("Initial===");
				AsList.ForEach(p => wrt.WriteLine($"{p.X},{p.Y}"));
			}
			var current = Vertices.First;
			while (true)
			{
				if (Vertices.Count() == 3)
				{
					if (LogPoints)
						wrt.WriteLine("Ending===");
					CutEar(current);
					break;
				}
				var found = FindEar(current);
				if (found == null)
				{
					ReverseWise();
					found = FindEar(Vertices.First);
				}
				if (found == null)
				{
					found = Vertices.First;
					if(LogPoints)
						wrt.WriteLine("None Found, use first point");
				}
				if (LogPoints)
				{
					wrt.WriteLine("Found===");
					wrt.WriteLine($"{found.Value.X},{found.Value.Y}");
				}
				current = CutEar(found);
				if (LogPoints)
				{
					wrt.WriteLine("After Cut===");
					AsList.ForEach(p => wrt.WriteLine($"{p.X},{p.Y}"));
					wrt.WriteLine("current===");
					wrt.WriteLine($"{current.Value.X},{current.Value.Y}");
				}
			}
			if (LogPoints)
			{
				wrt.Flush();
				strm.Flush();
				strm.Close();
				strm.Dispose();
			}
		}

		static List<CPoint2D> RollRight(List<CPoint2D> ls)
		{
			var lsr = ls.Skip(1).ToList();
			lsr.Add(ls.First());
			return lsr;
		}

		public List<CSegment2D> MakeSegments()
		{
			var ls = AsList;
			var ls2 = RollRight(ls);
			return ls.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
							(A, B) => A.p.MakeSegment(B.p)).ToList();
		}

		public double _area = double.NaN;
		public CPoint2D _center = new CPoint2D(double.NaN, double.NaN);

		public double Area
		{
			get
			{
				if (double.IsNaN(_area))
					CalcAreaNCenter();
				return _area;
			}
		}

		public CPoint2D Center
		{
			get
			{
				if (double.IsNaN(_center.X) || double.IsNaN(_center.Y))
					CalcAreaNCenter();
				return _center;
			}
		}

		public void CalcAreaNCenter()
		{
			var backup = Vertices.ToArray().ToList();
			try
			{
				if (Wise == PolygonWise.Clockwise)
					ReverseWise();
				CutEars();

				var tries = Ears.Select(e => Triangle.Create(e)).ToList();
				var centero = tries.Select(t => (t.Area, t.Center));
				_area = centero.Sum(t => t.Area);
				var xs = centero.Sum(t => t.Center.X * t.Area);
				var ys = centero.Sum(t => t.Center.Y * t.Area);
				_center = new CPoint2D(xs / _area, ys / _area);
			}
			finally
			{
				Vertices = new LinkedList<CPoint2D>(backup);
			}
		}
	}

	public static class PointHelper
	{
		public static void SaveTo(this List<CPoint2D> ls, string filename)
		{
			var strm = new FileStream(filename, FileMode.Create);
			var wrt = new StreamWriter(strm);
			ls.ForEach(p => wrt.WriteLine($"{p.X:0.000},{p.Y:0.000}"));
			wrt.Flush();
			strm.Flush();
			strm.Close();
		}

		public static void AppendTo(this List<CPoint2D> ls, string filename)
		{
			var strm = new FileStream(filename, FileMode.OpenOrCreate);
			var wrt = new StreamWriter(strm);
			strm.Seek(0, SeekOrigin.End);
			wrt.WriteLine("Appended ---");
			ls.ForEach(p => wrt.WriteLine($"{p.X:0.000},{p.Y:0.000}"));
			wrt.Flush();
			strm.Flush();
			strm.Close();
		}
	}
}

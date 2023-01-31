using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Media.Media3D;

namespace GeomHelper
{
	public class I64ConcaveHull
	{
		private List<I64Point> nearestNeighbors(List<I64Point> l, I64Point q)
		{
			return l.OrderBy(p => p.distance2(q)).ToList();
		}

		private I64Point findMinYI64Point(List<I64Point> l)
		{
			//return l.OrderBy(p => p.y).FirstOrDefault();
			I64Point winner = l[0];
			l.ForEach(p => { if (p.y < winner.y) winner = p; });
			return winner;
		}

		static double Abs(double d) => d < 0 ? -d : d;
		static decimal Abs(decimal d) => d < 0 ? -d : d;
		static long Abs(long d) => d < 0 ? -d : d;

		public static double angleDifference(double a1, double a2)
		{
			// calculate angle difference in clockwise directions as radians
			if ((a1 > 0 && a2 >= 0) && a1 > a2) // quadrant 1 or 2 and a1 > a2
			{
				return Abs(a1 - a2);
			}
			if ((a1 >= 0 && a2 > 0) && a1 < a2) // quadrant 1 or 2 and a1 < a2
			{
				return 360 + a1 - a2;
			}
			if ((a1 < 0 && a2 <= 0) && a1 < a2) // quadrant 3 or 4 and a1 < a2
			{
				return 360 + a1 + Abs(a2);
			}
			if ((a1 <= 0 && a2 < 0) && a1 > a2)  // quadrant 3 or 4 and a1 > a2
			{
				return Abs(a1 - a2);
			}
			if (a1 <= 0 && 0 < a2)
			{
				return 360 + a1 - a2;
			}
			if (a1 >= 0 && 0 >= a2)
			{
				return a1 + Abs(a2);
			}
			return 0;
		}

		public double CosineC(I64Point A, I64Point B, I64Point C)
		{
			double a2 = B.distance2(C);
			double b2 = A.distance2(C);
			double c2 = A.distance2(B);
			return a2 + b2 - c2 / 2 / Math.Sqrt(a2 * b2);
		}


		private List<I64Point> sortByAngle(List<I64Point> l, I64Point q, double a)
		{
			return l.OrderBy(p => angleDifference(a, q.angleTo(p))).ToList();
		}

		private bool intersect(I64Point l1p1, I64Point l1p2, I64Point l2p1, I64Point l2p2)
		{
			// calculate part equations for line-line intersection
			double a1 = l1p2.y - l1p1.y;
			double b1 = l1p1.x - l1p2.x;
			double c1 = a1 * l1p1.x + b1 * l1p1.y;
			double a2 = l2p2.y - l2p1.y;
			double b2 = l2p1.x - l2p2.x;
			double c2 = a2 * l2p1.x + b2 * l2p1.y;
			// calculate the divisor
			double tmp = (a1 * b2 - a2 * b1);
			if (tmp == 0)
				return false;

			// calculate intersection I64Point x coordinate
			double pX = (c1 * b2 - c2 * b1) / tmp;

			// check if intersection x coordinate lies in line line I64Segment
			if ((pX > l1p1.x && pX > l1p2.x) || (pX > l2p1.x && pX > l2p2.x)
							|| (pX < l1p1.x && pX < l1p2.x) || (pX < l2p1.x && pX < l2p2.x))
			{
				return false;
			}

			// calculate intersection I64Point y coordinate
			double pY = (a1 * c2 - a2 * c1) / tmp;

			// check if intersection y coordinate lies in line line I64Segment
			if ((pY > l1p1.y && pY > l1p2.y) || (pY > l2p1.y && pY > l2p2.y)
							|| (pY < l1p1.y && pY < l1p2.y) || (pY < l2p1.y && pY < l2p2.y))
			{
				return false;
			}

			return true;
		}

		private bool PointInPolygon(I64Point p, List<I64Point> pp)
		{
			bool result = false;

			for (int i = 0, j = pp.Count - 1; i < pp.Count; j = i++)
			{
				if ((pp[i].y > p.y) != (pp[j].y > p.y) &&
						//								(p.x < (pp[j].x - pp[i].x) * (p.y - pp[i].y) / (pp[j].y - pp[i].y) + pp[i].x))
						((p.x - pp[i].x) * (pp[j].y - pp[i].y) < (pp[j].x - pp[i].x) * (p.y - pp[i].y)))
					result = !result;
			}
			return result;
		}

		public bool allPointsInPolygon(List<I64Point> I64Points, List<I64Point> poly)
		{
			List<bool> insides = I64Points.Select(pt => PointInPolygon(pt, poly)).ToList();
			return insides.All(i => i);
		}

		public bool anyPointsInPolygon(List<I64Point> I64Points, List<I64Point> poly)
		{
			List<bool> insides = I64Points.Select(pt => PointInPolygon(pt, poly)).ToList();
			return insides.Any(i => i);
		}

		public I64ConcaveHull()
		{

		}

		private void calcConvex(List<I64Point> I64PointArrayList)
		{

			// return I64Points if already Concave Hull
			if (I64PointArrayList.Count < 3)
				return;

			// the resulting concave hull
			List<I64Point> concaveHull = new List<I64Point>();

			// optional remove duplicates
			// -- JJ -- not required since the I64Point was already distincted before passed to this method
			//HashSet<I64Point> set = new HashSet<I64Point>(I64PointArrayList);
			//List<I64Point> I64PointArraySet = new List<I64Point>(set);

			// just copy the I64Points to another List
			List<I64Point> I64PointArraySet = I64PointArrayList.ToArray().ToList();

			// find first I64Point and remove from I64Point list
			I64Point firstI64Point = findMinYI64Point(I64PointArraySet);
			concaveHull.Add(firstI64Point);
			I64Point currentI64Point = firstI64Point;
			I64PointArraySet.Remove(firstI64Point);

			double previousAngle = 0;
			int step = 2;

			while ((currentI64Point != firstI64Point || step == 2) && I64PointArraySet.Count > 0)
			{

				// after 3 steps add first I64Point to dataset, otherwise hull cannot be closed
				if (step == 5)
					I64PointArraySet.Add(firstI64Point);

				// get k nearest neighbors of current I64Point
				List<I64Point> nearestI64Points = nearestNeighbors(I64PointArraySet, currentI64Point);

				// sort I64Points by angle clockwise
				List<I64Point> clockwiseI64Points = sortByAngle(nearestI64Points, currentI64Point, previousAngle);

				// check if clockwise angle nearest neighbors are candidates for concave hull
				I64Point found = null;
				for (int i = 0; i < clockwiseI64Points.Count; i++)
				{
					int lastI64Point = (clockwiseI64Points[i] == firstI64Point) ? 1 : 0;

					// check if possible new concave hull I64Point intersects with others

					bool its = false;
					for (int j = 2; j < (concaveHull.Count - lastI64Point); j++)
					{
						its = intersect(concaveHull[step - 2], clockwiseI64Points[i], concaveHull[step - 2 - j], concaveHull[step - 1 - j]);
						if (its)
							break;
					}
					if (!its)
					{
						found = clockwiseI64Points[i];
						break;
					}
				}

				if (found == null)
					return;

				// add candidate to concave hull and remove from dataset
				currentI64Point = found;
				concaveHull.Add(currentI64Point);
				I64PointArraySet.Remove(currentI64Point);

				// calculate last angle of the concave hull line
				previousAngle = concaveHull[step - 1].angleTo(concaveHull[step - 2]);

				step++;
			}

			convexHull = concaveHull.Distinct().ToList();
		}

		private List<I64Point> convexHull = null;

		private List<I64Point> objects = null;

#if (_LABEL_)
		private List<I64SegmentLab> segments = new List<I64SegmentLab>();
		public void SetSegment(IEnumerable<I64SegmentLab> segments)
		{
			this.segments.AddRange(segments);
		}
#else
		private List<I64Segment> segments = new List<I64Segment>();

		public void SetSegment(IEnumerable<I64Segment> segments)
		{
			this.segments.AddRange(segments);
		}
#endif

		public void SetConvexHull(IEnumerable<I64Point> I64Points) => convexHull = I64Points.ToList();

		private List<I64Point> concaveHull = null;
#if (_TRACED_)
		private List<TracedList<I64Point>> concaveHulls = null;
#endif

		const int Knearest = 150;

#if (_TRACED_)
#if (_LABEL_)
		public TracedList<Point>[] calculate(IEnumerable<SegmentLab> segments, int concavity)
		{
			TracedList<I64Point>[] result = _calculate(segments, concavity);
			return result.Select(l => new TracedList<Point>(l.tag,l.Select(p => (Point)p).ToList())).ToArray();
		}
#else
		public TracedList<Point>[] calculate(IEnumerable<Segment> segments, int concavity)
		{
			TracedList<I64Point>[] result = _calculate(segments, concavity);
			return result.Select(l => new TracedList<Point>(l.tag,l.Select(p => (Point)p).ToList())).ToArray();
		}
#endif
#else
#if (_LABEL_)
		public List<Point> calculate(IEnumerable<SegmentLab> segments, int concavity)
		{
			List<I64Point> result = _calculate(segments, concavity);
			//string csv = "";
			//result.ForEach(p => csv += $"\"{p.x}\",\"{p.y}\"\n");
			//Clipboard.SetText(csv);
			return result.Select(p=>(Point)p).ToList();
		}
#else
		public List<Point> calculate(IEnumerable<Segment> segments, int concavity)
		{
			List<I64Point> result = _calculate(segments, concavity);
			return result.Select(p => (Point)p).ToList();
		}
#endif
#endif


#if (_TRACED_)
#if (_LABEL_)
		private TracedList<I64Point>[] _calculate(IEnumerable<SegmentLab> segments, int concavity)
#else
		private TracedList<I64Point>[] _calculate(IEnumerable<Segment> segments, int concavity)
#endif
#else
#if (_LABEL_)
		private List<I64Point> _calculate(IEnumerable<SegmentLab> segments, int concavity)
#else
		private List<I64Point> _calculate(IEnumerable<Segment> segments, int concavity)
#endif
#endif
		{
#if (_LABEL_)
			SetSegment(segments.Select(s=>new I64SegmentLab(s)));
#else
			SetSegment(segments.Select(s => new I64Segment(s)));
#endif
			objects = this.segments.SelectMany(s => (new I64Point[] { s.Pt1, s.Pt2 })).Distinct().ToList();
			if (objects.Count < 3)
#if (_TRACED_)
				return new TracedList<I64Point>[] { new TracedList<I64Point>(0, objects) };
#else
				return objects;
#endif
			if (convexHull == null)
				calcConvex(this.objects);
			convexHull = convexHull.Distinct().ToList();
			if (convexHull == null || convexHull.Count < 3)
#if (_TRACED_)
				return new TracedList<I64Point>[] { new TracedList<I64Point>(0, objects) };
#else
				return objects;
#endif

			if (convexHull.Last() != convexHull.First())
				convexHull.Add(convexHull.First());

#if (_TRACED_)
			List<TracedList<I64Point>> finalList = new List<TracedList<I64Point>>();
			finalList.Add(new TracedList<I64Point>(0));
			finalList[0].AddRange(convexHull);
#endif
			objects = objects.Except(convexHull).ToList();

#if (_TRACED_)
			concaveHulls = new List<TracedList<I64Point>>();
#else
			concaveHull = new List<I64Point>();
#endif
			// find first I64Point
			I64Point rear = convexHull[0];

#if (!_TRACED_)
			concaveHull.Add(rear);
#endif

			for (int i = 1; i < convexHull.Count; i++)
			{
				I64Point front = convexHull[i];
#if (_TRACED_)
				List<TracedList<I64Point>> vertices = sliceSegment(rear, front, true, concavity, i == 1);
				concaveHulls.AddRange(vertices);
#else
				List<I64Point> vertices = sliceSegment(rear, front, true, concavity);
				concaveHull.AddRange(vertices);
#endif
				rear = front;
			}
#if (_TRACED_)
			int[] tags = concaveHulls.Select(c => c.tag).Distinct().OrderByDescending(i => i).ToArray();
			foreach (int tag in tags)
			{
				TracedList<I64Point> nLayer = new TracedList<I64Point>(concavity - tag + 1);
				foreach (TracedList<I64Point> tlist in concaveHulls)
					if (tlist.tag == tag)
						nLayer.AddRange(tlist);
				finalList.Add(nLayer);
			}
				return finalList.ToArray();
#else
			return concaveHull;
#endif
		}

#if (_TRACED_)
		private List<TracedList<I64Point>> sliceSegment(I64Point rear, I64Point front, bool LTR, int maxdepth, bool addRear= false)
#else
		private List<I64Point> sliceSegment(I64Point rear, I64Point front, bool LTR, int maxdepth, bool dummy = false)
#endif
		{
			if (maxdepth == 0)
#if (_TRACED_)
				return addRear ? new  List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { rear,front } } : 
					new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { front } };
#else
				return new List<I64Point> { front };
#endif
			int depth = maxdepth - 1;
			I64Segment seg = new I64Segment(rear, front);
#if (_LABEL_)
			I64SegmentLab seglab = segments.FirstOrDefault(s => (s.Pt1 == rear && s.Pt2 == front) || (s.Pt1 == front && s.Pt2 == rear));
			if(!I64Segment.Equals(seglab,null))
#else
			I64Segment segfo = segments.FirstOrDefault(s => (s.Pt1 == rear && s.Pt2 == front) || (s.Pt1 == front && s.Pt2 == rear));
			if (!I64Segment.Equals(segfo, null))
#endif
#if (_TRACED_)
				return addRear ? new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { rear, front } } :
					new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { front } };
#else
				return new List<I64Point> { front };
#endif

			double len2 = seg.Length2;

#if (_LABEL_)
			IEnumerable<I64PointDir> objs = objects.Select(p => new I64PointDir(p, null, seg));
#else
			IEnumerable<I64PointDir> objs = objects.Select(p => new I64PointDir(p, seg));
#endif
			IEnumerable<I64PointDir> rnearest = objs.Where(a => a.point != null && a.dist2R < len2);
			IEnumerable<I64PointDir> fnearest = objs.Where(a => a.point != null && a.dist2F < len2);
			IEnumerable<I64PointDir> xnearest = rnearest.Union(fnearest).Distinct();

			I64PointDir pdnearest = null;
			//bool intersecting = AnyIntersection(seg);

			//if (!intersecting)
			//{
#if (_LABEL_)
				IEnumerable<I64SegmentLab> rrnear = segments.Where(s => s.Pt1 == rear || s.Pt2 == rear);
				IEnumerable<I64SegmentLab> ftnear = segments.Where(s => s.Pt1 == front || s.Pt2 == front);
#else
			IEnumerable<I64Segment> rrnear = segments.Where(s => s.Pt1 == rear || s.Pt2 == rear);
			IEnumerable<I64Segment> ftnear = segments.Where(s => s.Pt1 == front || s.Pt2 == front);
#endif
			IEnumerable<I64PointDir> ropposites = rrnear//.Select(s => s.Pt1 == rear ? s.Pt2 : s.Pt1).Distinct()
				.Select(s => new I64PointDir(s.Pt1 == rear ? s.Pt2 : s.Pt1,
#if (_LABEL_)
					s.label, 
#endif
					seg))
					.Where(p => p.point != null);
			//.Where(pd=>pd.dist2R <= len2)
			//.ToList();

			IEnumerable<I64PointDir> fopposites = ftnear//.Select(s => s.Pt1 == front ? s.Pt2 : s.Pt1).Distinct()
				.Select(s => new I64PointDir(s.Pt1 == front ? s.Pt2 : s.Pt1,
#if (_LABEL_)
					s.label,
#endif
					seg))
					//.Where(pd => pd.dist2F <= len2)
					.Where(p => p.point != null);
			//.ToList();

			IEnumerable<I64PointDir> opposites = LTR ? ropposites : fopposites;
			IEnumerable<I64PointDir> segvictims = LTR ? fopposites : ropposites;
			I64Point partner = LTR ? front : rear;

			if (opposites.Any(pd => pd.cos == 1 && pd.angleR == 0)) // the I64Point inline with rear and front, but beyond the front
#if (_TRACED_)
				return addRear ? new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { rear, front } } :
						new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { front } };
#else
				return new List<I64Point> { front };
#endif
			I64PointDir mid = opposites.Where(pd => pd.cos == -1).OrderBy(p => LTR ? p.dist2R : p.dist2F).FirstOrDefault(); // the I64Point inline with rear and front, in the middle of those
			if (mid != null)
#if (_TRACED_)
				return addRear ? new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { rear, mid.point } } :
						new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { mid.point } };
#else
				return new List<I64Point> { mid.point };
#endif

			pdnearest = FindNearestSeg(opposites, segvictims.Union(xnearest), seg);
			if (pdnearest == null)
				pdnearest = FindNearestSeg(segvictims, opposites.Union(xnearest), seg);
			if (pdnearest == null)
				pdnearest = FindNearestSeg(xnearest, opposites.Union(segvictims), seg);

			I64Point nearest = pdnearest?.point;
			//}
			//else
			//{
			//	I64PointDir other = FindNearestSeg(xnearest, seg, false);
			//	nearest = other?.point;
			//}

			if (!(nearest is I64Point))
#if (_TRACED_)
				return addRear ? new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { rear, front } } :
					new List<TracedList<I64Point>> { new TracedList<I64Point>(maxdepth) { front } };
#else
				return new List<I64Point> { front };
#endif
			objects.Remove(nearest);

#if (_TRACED_)
			TracedList<I64Point> thistraced =  new TracedList<I64Point>(maxdepth) { nearest, front } ; ;
			if (addRear)
				thistraced.Insert(0, rear);

			List<TracedList<I64Point>> verts1 = sliceSegment(rear, nearest, true, depth,addRear);
			List<TracedList<I64Point>> verts2 = sliceSegment(nearest, front, false, depth,false);

			List<TracedList<I64Point>> vertices = new List<TracedList<I64Point>>() { thistraced };

			vertices.AddRange(verts1);
			vertices.AddRange(verts2);
			return vertices;
#else
			if (!LTR)
				return new List<I64Point> { nearest, front };

			List<I64Point> vertsF = sliceSegment(nearest, front, false, maxdepth); // slice by the front side with as a same depth
			vertsF.Insert(0, nearest);
			if (depth > 0)
			{
				List<I64Point> vertices = sliceSegment(rear, nearest, true, depth);
				for (int v = 1; v < vertsF.Count; v++)
					vertices.AddRange(sliceSegment(vertsF[v - 1], vertsF[v], true, depth));
				return vertices;
			}
			return vertsF;
			//List<I64Point> verts1 = sliceSegment(rear, nearest, true, depth);
			//List<I64Point> verts2 = sliceSegment(nearest, front, false, depth);
			//List<I64Point> vertices = verts1.Union(verts2).ToList();
			//return vertices;
#endif
		}



		const double HalfPI = 90;
		const double TriQuartPI = 45 * 3;
		const double ThirdPI = 60;


		private I64PointDir FindNearestSeg(IEnumerable<I64PointDir> Pointdirs, IEnumerable<I64PointDir> victims, I64Segment seg)
		{
			if (!Pointdirs.Any())
				return null;
			Pointdirs = Pointdirs.Where(pd => pd.sign >= 0)
				.Where(pd => (pd.angleR <= HalfPI) && (pd.angleF <= HalfPI))
				.OrderBy(pd => pd, I64PointDir.comparer);

			IEnumerable<I64Point> vpoints = victims.Where(pd => (pd.angleR <= HalfPI) && (pd.angleF <= HalfPI)).Select(pd => pd.point); ;
			IEnumerable<I64Point> vics = vpoints.Union(Pointdirs.Select(pd => pd.point));

			I64PointDir winner = null;

			foreach (I64PointDir pd in Pointdirs)
			{
				//I64Segment seg2 = new I64Segment(seg.Pt1, pd.point);
				//if (AnyIntersection(seg, vpoints))
				//	continue;
				//List<I64Point> poly = new List<I64Point> { seg.Pt1, seg.Pt2, pd.point, seg.Pt1 };
				//List<I64Point> pts = vics.Except(new I64Point[] { pd.point }).ToList();
				bool avoid = false;
				foreach (I64PointDir px in victims)
				{
					bool inside = pd.angleR > px.angleR && pd.angleF > px.angleF;
					avoid = pd.sign == -1 ? !inside : inside;
					//inside = PointInPolygon(pt, poly);
					if (avoid)
						break;
				}
				if (!avoid)
				{
					winner = pd;
					break;
				}
			}
			return winner;
		}

		private I64PointDir FindNearestSeg2(IEnumerable<I64PointDir> Pointdirs, I64Segment seg, bool insideOnly)
		{
			if (!Pointdirs.Any())
				return null;

			if (insideOnly)
				Pointdirs = Pointdirs.Where(pd => pd.sign >= 0);
			Pointdirs = Pointdirs
				.Where(pd => (pd.angleR <= HalfPI) && (pd.angleF <= HalfPI))
				.OrderBy(pd => pd, I64PointDir.comparer);
			return Pointdirs.FirstOrDefault();
		}

		private bool AnyIntersection(I64Segment seg, IEnumerable<I64Point> vpoints)
		{
			List<I64Segment> segs = segments.Where(s => vpoints.Contains(s.Pt1) || vpoints.Contains(s.Pt2)).ToList();
			foreach (I64Segment segx in segs)
			{
				if (segx.IsIntersect(seg))
					return true;
			}
			return false;
		}

	}

	public class I64SegmentLab : I64Segment
	{
		public string label;

		public I64SegmentLab(SegmentLab other)
			: base(other.Pt1, other.Pt2)
		{
			this.label = other.label;
		}

		public I64SegmentLab(Point pt1, Point pt2, string label)
			: base(pt1, pt2)
		{
			this.label = label;
		}
		public I64SegmentLab(Point3D pt1, Point3D pt2, string label)
			: base(pt1, pt2)
		{
			this.label = label;
		}
	}

	public class I64PointDir
	{
		public I64Point point = null;
		public double angleR = 180;
		public double angleF = 180;
		public int sign;
		public double cos = 1;
		public double dist2R;
		public double dist2F;
		public string label;
#if (_LABEL_)
		public I64PointDir(I64Point point, string label, I64Segment seg)
		{
			this.label = label;
#else
		public I64PointDir(I64Point point, I64Segment seg)
		{
#endif
			dist2R = seg.Pt1.distance2(point);
			dist2F = seg.Pt2.distance2(point);
			double len2 = seg.Length2;
			if (dist2R > len2 && dist2F > len2)
				return; //avoid the rest parameters

			this.point = point;
			angleR = seg.AngleMath(point);
			sign = 0;
			if (angleR < 0)
			{
				sign = -1;
				angleR = -angleR;
			}
			else if (angleR > 0)
				sign = 1;
			cos = seg.CosineCenter(point);
			double angleC = Math.Acos((double)cos) * 180 / Math.PI;
			angleF = 180 - (angleR + angleC);
		}

		public class Comparer : IComparer<I64PointDir>
		{
			public int Compare(I64PointDir x, I64PointDir y)
			{
				if (x.sign != y.sign)
					return x.sign - y.sign;
				return (x.cos < y.cos ? -x.sign : x.cos > y.cos ? x.sign : 0);
			}
		}
		public static Comparer comparer = new Comparer();
	}


	public class TracedList<T> : List<T> where T : class
	{
		public int tag;

		public TracedList()
			: base()
		{
			tag = 0;
		}
		public TracedList(int tag)
			: base()
		{
			this.tag = tag;
		}
		public TracedList(int tag, List<T> original)
			: base(original)
		{
			this.tag = tag;
		}
	}
}

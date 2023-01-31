using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Media.Media3D;

namespace GeomHelper
{
	public class ConcaveHull
	{
		private List<Point> nearestNeighbors(List<Point> l, Point q)
		{
			return l.OrderBy(p => p.distance2(q)).ToList();
		}

		private Point findMinYPoint(List<Point> l)
		{
			//return l.OrderBy(p => p.y).FirstOrDefault();
			Point winner = l[0];
			l.ForEach(p => { if (p.y < winner.y) winner = p; });
			return winner;
		}

		static double Abs(double d) => d < 0 ? -d : d;

		public static double angleDifference(double a1, double a2)
		{
			// calculate angle difference in clockwise directions as radians
			if ((a1 > 0 && a2 >= 0) && a1 > a2) // quadrant 1 or 2 and a1 > a2
			{
				return Abs(a1 - a2);
			}
			if ((a1 >= 0 && a2 > 0) && a1 < a2) // quadrant 1 or 2 and a1 < a2
			{
				return 2 * Math.PI + a1 - a2;
			}
			if ((a1 < 0 && a2 <= 0) && a1 < a2) // quadrant 3 or 4 and a1 < a2
			{
				return 2 * Math.PI + a1 + Abs(a2);
			}
			if ((a1 <= 0 && a2 < 0) && a1 > a2)  // quadrant 3 or 4 and a1 > a2
			{
				return Abs(a1 - a2);
			}
			if (a1 <= 0 && 0 < a2)
			{
				return 2 * Math.PI + a1 - a2;
			}
			if (a1 >= 0 && 0 >= a2)
			{
				return a1 + Abs(a2);
			}
			return 0.0;
		}

		public double CosineC(Point A, Point B, Point C)
		{
			double a2 = B.distance2(C);
			double b2 = A.distance2(C);
			double c2 = A.distance2(B);
			return a2 + b2 - c2 / 2 / Math.Sqrt(a2 * b2);
		}


		private List<Point> sortByVector2(List<Point> l, Point q, double theta)
		{
			Matrix trx = Matrix.Rotation2D(-theta, q);
			var rotpoints = l.Select(p => new { pt = p, rotv = trx.Transform2(p) }).ToList();
			return rotpoints.OrderBy(p => p.rotv, Vector.comparer).Select(p => p.pt).ToList();
		}
		private List<Point> sortByVector(List<Point> l, Point q, Vector front)
		{
			Vector land = front.Identity;
			Vector normal = land.Perpendic;
			if (double.IsNaN(land.dx) || double.IsNaN(land.dy))
			{
				return null;
			}
			var tagged = l.Select(p => new { pt = p, ve = new Vector(q, p) }).ToList();
			return tagged.OrderBy(p => new Vector(land.dotProduct(p.ve), normal.dotProduct(p.ve)), Vector.comparer).Select(p => p.pt).ToList();
		}

		private List<Point> sortByAngle(List<Point> l, Point q, double a)
		{
			return l.OrderBy(p => angleDifference(a, q.angleTo(p))).ToList();
		}

		private bool intersect(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
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

			// calculate intersection point x coordinate
			double pX = (c1 * b2 - c2 * b1) / tmp;

			// check if intersection x coordinate lies in line line segment
			if ((pX > l1p1.x && pX > l1p2.x) || (pX > l2p1.x && pX > l2p2.x)
							|| (pX < l1p1.x && pX < l1p2.x) || (pX < l2p1.x && pX < l2p2.x))
			{
				return false;
			}

			// calculate intersection point y coordinate
			double pY = (a1 * c2 - a2 * c1) / tmp;

			// check if intersection y coordinate lies in line line segment
			if ((pY > l1p1.y && pY > l1p2.y) || (pY > l2p1.y && pY > l2p2.y)
							|| (pY < l1p1.y && pY < l1p2.y) || (pY < l2p1.y && pY < l2p2.y))
			{
				return false;
			}

			return true;
		}

		private bool pointInPolygon(Point p, List<Point> pp)
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

		public bool allPointsInPolygon(List<Point> points, List<Point> poly)
		{
			List<bool> insides = points.Select(pt => pointInPolygon(pt, poly)).ToList();
			return insides.All(i => i);
		}

		public bool anyPointsInPolygon(List<Point> points, List<Point> poly)
		{
			List<bool> insides = points.Select(pt => pointInPolygon(pt, poly)).ToList();
			return insides.Any(i => i);
		}

		public ConcaveHull()
		{

		}

		private void calcConvex(List<Point> pointArrayList)
		{

			// return Points if already Concave Hull
			if (pointArrayList.Count < 3)
				return;

			// the resulting concave hull
			List<Point> concaveHull = new List<Point>();

			// optional remove duplicates
			// -- JJ -- not required since the point was already distincted before passed to this method
			//HashSet<Point> set = new HashSet<Point>(pointArrayList);
			//List<Point> pointArraySet = new List<Point>(set);

			// just copy the points to another List
			List<Point> pointArraySet = pointArrayList.ToArray().ToList();

			// find first point and remove from point list
			Point firstPoint = findMinYPoint(pointArraySet);
			concaveHull.Add(firstPoint);
			Point currentPoint = firstPoint;
			pointArraySet.Remove(firstPoint);

			double previousAngle = 0.0;
			int step = 2;

			while ((currentPoint != firstPoint || step == 2) && pointArraySet.Count > 0)
			{

				// after 3 steps add first point to dataset, otherwise hull cannot be closed
				if (step == 5)
					pointArraySet.Add(firstPoint);

				// get k nearest neighbors of current point
				List<Point> nearestPoints = nearestNeighbors(pointArraySet, currentPoint);

				// sort points by angle clockwise
				List<Point> clockwisePoints = sortByAngle(nearestPoints, currentPoint, previousAngle);

				// check if clockwise angle nearest neighbors are candidates for concave hull
				Point found = null;
				for (int i = 0; i < clockwisePoints.Count; i++)
				{
					int lastPoint = (clockwisePoints[i] == firstPoint) ? 1 : 0;

					// check if possible new concave hull point intersects with others

					bool its = false;
					for (int j = 2; j < (concaveHull.Count - lastPoint); j++)
					{
						its = intersect(concaveHull[step - 2], clockwisePoints[i], concaveHull[step - 2 - j], concaveHull[step - 1 - j]);
						if (its)
							break;
					}
					if (!its)
					{
						found = clockwisePoints[i];
						break;
					}
				}

				if (found == null)
					return;

				// add candidate to concave hull and remove from dataset
				currentPoint = found;
				concaveHull.Add(currentPoint);
				pointArraySet.Remove(currentPoint);

				// calculate last angle of the concave hull line
				previousAngle = concaveHull[step - 1].angleTo(concaveHull[step - 2]);

				step++;
			}

			//// Check if all points are contained in the concave hull
			//bool insideCheck = allPointsInPolygon(pointArraySet, concaveHull);

			////for (int i = pointArraySet.Count - 1; insideCheck && i > 0; i--)
			////	insideCheck = pointInPolygon(pointArraySet[i], concaveHull);

			//// if not all points inside -  try again
			//if (!insideCheck)
			//	return;

			convexHull = concaveHull.Distinct().ToList();
		}

		private List<Point> convexHull = null;

		private List<Point> objects = null;

		private List<SegmentLab> segments = new List<SegmentLab>();

		public void SetSegment(IEnumerable<SegmentLab> segments)
		{
			this.segments.AddRange(segments);
		}

		public void SetConvexHull(IEnumerable<Point> points) => convexHull = points.ToList();

		private List<Point> concaveHull = null;
		private List<TracedList<Point>> concaveHulls = null;

		const int Knearest = 150;

		public List<Point> calculate(List<Point> points, int concavity)
		{
			if (points.Count < 3)
				return points;

			if (convexHull == null)
				calcConvex(points);
			convexHull = convexHull.Distinct().ToList();
			if (convexHull == null || convexHull.Count < 3)
				return points;

			if (convexHull.Last() != convexHull.First())
				convexHull.Add(convexHull.First());

			objects = points.Except(convexHull).ToList();

			concaveHull = new List<Point>();

			List<Point> collections = new List<Point>();
			// find first point
			Point rear = convexHull[0];
			concaveHull.Add(rear);
			for (int i = 1; i < convexHull.Count; i++)
			{
				Point front = convexHull[i];
				List<Point> vertices = sliceSegment(rear, front, true, concavity);
				concaveHull.AddRange(vertices);
				rear = front;
			}

			return concaveHull;
		}

		public TracedList<Point>[] calculateTraced(IEnumerable<SegmentLab> segments, int concavity)
		{
			SetSegment(segments);
			objects = segments.SelectMany(s => new Point[] { s.Pt1, s.Pt2 }).Distinct().ToList();
			if (objects.Count < 3)
				return new TracedList<Point>[] { new TracedList<Point>(0, objects) };

			if (convexHull == null)
				calcConvex(this.objects);
			convexHull = convexHull.Distinct().ToList();
			if (convexHull == null || convexHull.Count < 3)
				return new TracedList<Point>[] { new TracedList<Point>(0, objects) };

			if (convexHull.Last() != convexHull.First())
				convexHull.Add(convexHull.First());

			List<TracedList<Point>> finalList = new List<TracedList<Point>>();
			finalList.Add(new TracedList<Point>(0));
			finalList[0].AddRange(convexHull);

			objects = objects.Except(convexHull).ToList();

			concaveHulls = new List<TracedList<Point>>();

			// find first point
			Point rear = convexHull[0];
			for (int i = 1; i < convexHull.Count; i++)
			{
				Point front = convexHull[i];
				List<TracedList<Point>> vertices = sliceSegmentTraced(rear, front, true, concavity, i == 1);
				concaveHulls.AddRange(vertices);
				rear = front;
			}
			int[] tags = concaveHulls.Select(c => c.tag).Distinct().OrderByDescending(i => i).ToArray();
			foreach (int tag in tags)
			{
				TracedList<Point> nLayer = new TracedList<Point>(concavity - tag + 1);
				foreach (TracedList<Point> tlist in concaveHulls)
					if (tlist.tag == tag)
						nLayer.AddRange(tlist);
				finalList.Add(nLayer);
			}


			return finalList.ToArray();
		}


		private List<Point> sliceSegment(Point rear, Point front, bool LTR, int maxdepth)
		{
			if (maxdepth == 0)
				return new List<Point> { front };

			int depth = maxdepth - 1;
			Segment seg = new Segment(rear, front);
			double len2 = seg.Length2;

			//var objs = objects.Select(p => new { pt = p, rd2 = p.distance2(rear), fd2 = p.distance2(front) });
			IEnumerable<PointDir> objs = objects.Select(p => new PointDir(p, null, seg));
			IEnumerable<PointDir> rnearest = objs.Where(a => a.dist2R < len2);
			IEnumerable<PointDir> fnearest = objs.Where(a => a.dist2F < len2);
			IEnumerable<PointDir> xnearest = rnearest.Union(fnearest).Distinct();

			Point nearest = null;
			bool intersecting = AnyIntersection(seg);

			if (!intersecting)
			{
				IEnumerable<SegmentLab> rrnear = segments.Where(s => s.Pt1 == rear || s.Pt2 == rear);
				IEnumerable<SegmentLab> ftnear = segments.Where(s => s.Pt1 == front || s.Pt2 == front);
				List<PointDir> ropposites = LTR ? rrnear//.Select(s => s.Pt1 == rear ? s.Pt2 : s.Pt1).Distinct()
					.Select(s => new PointDir(s.Pt1 == rear ? s.Pt2 : s.Pt1, s.label, seg))
					//.Where(pd=>pd.dist2R <= len2)
					.ToList()
					:
					ftnear//.Select(s => s.Pt1 == front ? s.Pt2 : s.Pt1).Distinct()
					.Select(s => new PointDir(s.Pt1 == front ? s.Pt2 : s.Pt1, s.label, seg))
					//.Where(pd => pd.dist2F <= len2)
					.ToList();

				Point partner = LTR ? front : rear;
				if (ropposites.Any(pd => pd.cos == 1 && pd.angleR == 0))
					return new List<Point> { front };
				PointDir mid = ropposites.FirstOrDefault(pd => pd.cos == -1);
				if (mid != null)
					return new List<Point> { mid.point };

				PointDir nearestR = FindNearestSeg(ropposites, seg, true);
				if (nearestR == null)
				{
					PointDir other = FindNearestSeg(xnearest, seg, true);
					nearest = other?.point;
				}
				else
					nearest = nearestR?.point;
			}
			else
			{
				PointDir other = FindNearestSeg(xnearest, seg, false);
				nearest = other?.point;
			}

			if (!(nearest is Point))
				return new List<Point> { front };

			objects.Remove(nearest);

			List<Point> verts1 = sliceSegment(rear, nearest, true, depth);
			List<Point> verts2 = sliceSegment(nearest, front, false, depth);

			List<Point> vertices = new List<Point>();// { rear, nearest, front };
			vertices.AddRange(verts1);
			vertices.AddRange(verts2);

			return vertices;
		}
		private List<TracedList<Point>> sliceSegmentTraced(Point rear, Point front, bool LTR, int maxdepth, bool addRear= false)
		{
			//Point ptcmp = new Point(106.6404125961, -6.00479599779);// 106.64041259610, -6.00479599779);
			//System.Diagnostics.Debug.WriteLine($"depth:{maxdepth}; rear={rear.x},{rear.y}; front={front.x},{front.y}");
			if (maxdepth == 0)
				return addRear? new  List<TracedList<Point>> { new TracedList<Point>(maxdepth) { rear,front } } : 
					new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { front } };

			int depth = maxdepth - 1;
			Segment seg = new Segment(rear, front);
			SegmentLab seglab = segments.FirstOrDefault(s => (s.Pt1 == rear && s.Pt2 == front) || (s.Pt1 == front && s.Pt2 == rear));
			if(!Segment.Equals(seglab,null))
				return addRear ? new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { rear, front } } :
					new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { front } };
			double len2 = seg.Length2;

			//var objs = objects.Select(p => new { pt = p, rd2 = p.distance2(rear), fd2 = p.distance2(front) });
			IEnumerable<PointDir> objs = objects.Select(p => new PointDir(p, null, seg));
			IEnumerable<PointDir> rnearest = objs.Where(a => a.dist2R < len2);
			IEnumerable<PointDir> fnearest = objs.Where(a => a.dist2F < len2);
			IEnumerable<PointDir> xnearest = rnearest.Union(fnearest).Distinct();

			Point nearest = null;
			bool intersecting = AnyIntersection(seg);

			if (!intersecting)
			{
				IEnumerable<SegmentLab> rrnear = segments.Where(s => s.Pt1 == rear || s.Pt2 == rear);
				IEnumerable<SegmentLab> ftnear = segments.Where(s => s.Pt1 == front || s.Pt2 == front);
				List<PointDir> ropposites = LTR ? rrnear//.Select(s => s.Pt1 == rear ? s.Pt2 : s.Pt1).Distinct()
					.Select(s => new PointDir(s.Pt1 == rear ? s.Pt2 : s.Pt1, s.label, seg))
					//.Where(pd=>pd.dist2R <= len2)
					.ToList()
					:
					ftnear//.Select(s => s.Pt1 == front ? s.Pt2 : s.Pt1).Distinct()
					.Select(s => new PointDir(s.Pt1 == front ? s.Pt2 : s.Pt1,s.label, seg))
					//.Where(pd => pd.dist2F <= len2)
					.ToList();

				Point partner = LTR ? front : rear;
				if (ropposites.Any(pd => pd.cos == 1 && pd.angleR == 0)) // the point inline with rear and front, but beyond the front
					return addRear ? new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { rear, front } } :
						new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { front } };
				PointDir mid = ropposites.Where(pd => pd.cos == -1).OrderBy(p=> LTR ? p.dist2R:p.dist2F).FirstOrDefault(); // the point inline with rear and front, in the middle of those
				if (mid != null)
					return addRear ? new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { rear, mid.point } } :
						new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { mid.point } };

				PointDir nearestR = FindNearestSeg(ropposites, seg, true);
				if (nearestR == null)
				{
					PointDir other = FindNearestSeg(xnearest, seg, true);
					nearest = other?.point;
				}
				else
					nearest = nearestR?.point;
			}
			else
			{
				PointDir other = FindNearestSeg(xnearest, seg, false);
				nearest = other?.point;
			}

			if (!(nearest is Point))
				return addRear ? new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { rear, front } } :
					new List<TracedList<Point>> { new TracedList<Point>(maxdepth) { front } };

			objects.Remove(nearest);

			TracedList<Point> thistraced =  new TracedList<Point>(maxdepth) { nearest, front } ; ;
			if (addRear)
				thistraced.Insert(0, rear);

			List<TracedList<Point>> verts1 = sliceSegmentTraced(rear, nearest, true, depth,addRear);
			List<TracedList<Point>> verts2 = sliceSegmentTraced(nearest, front, false, depth,false);

			List<TracedList<Point>> vertices = new List<TracedList<Point>>() { thistraced };
			vertices.AddRange(verts1);
			vertices.AddRange(verts2);

			return vertices;
		}


		const double HalfPI = Math.PI / 2;
		const double TriQuartPI = Math.PI * 3 / 4;
		const double ThirdPI = Math.PI / 3;


		private PointDir FindNearestSeg(IEnumerable<PointDir> pointdirs, Segment seg, bool insideOnly)
		{
			if (!pointdirs.Any())
				return null;

			if (insideOnly)
				pointdirs = pointdirs.Where(pd => pd.sign >= 0);
			pointdirs = pointdirs
				.Where(pd => (pd.angleR <= HalfPI) && (pd.angleF <= HalfPI))
				.OrderBy(pd => pd, PointDir.comparer);
			return pointdirs.FirstOrDefault();
		}

		private bool AnyIntersection(Segment seg)
		{
			foreach (Segment segx in segments)
			{
				if (segx.IsIntersect(seg))
					return true;
			}
			return false;
		}

	}

	public class SegmentLab : Segment
	{
		public string label;

		public SegmentLab(Point pt1, Point pt2, string label)
			: base(pt1, pt2)
		{
			this.label = label;
		}
		public SegmentLab(Point3D pt1, Point3D pt2, string label)
			: base(pt1, pt2)
		{
			this.label = label;
		}
	}

	public class PointDir
	{
		public Point point;
		public double angleR;
		public double angleF;
		public int sign;
		public double cos;
		public double dist2R;
		public double dist2F;
		public string label;
		public PointDir(Point point, string label, Segment seg)
		{
			this.label = label;
			this.point = point;
			dist2R = seg.Pt1.distance2(point);
			dist2F = seg.Pt2.distance2(point);
			double len2 = seg.Length2;
			if (dist2R > len2 && dist2F > len2)
				return; //avoid the rest parameters

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
			double angleC = Math.Acos(cos);
			angleF = Math.PI - (angleR + angleC);
		}

		public class Comparer : IComparer<PointDir>
		{
			public int Compare(PointDir x, PointDir y)
			{
				if (x.sign != y.sign)
					return x.sign - y.sign;
				return (x.cos < y.cos ? -x.sign : x.cos > y.cos ? x.sign : 0);
			}
		}
		public static Comparer comparer = new Comparer();
	}
}

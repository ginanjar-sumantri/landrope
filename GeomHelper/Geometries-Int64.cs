using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media.Media3D;

namespace GeomHelper
{
	public class I64Point
	{
		public long x;
		public long y;

		public static long Round(double x) => (long)Math.Truncate(x + 0.5);
		public static long Round(decimal x) => (long)Math.Truncate(x + 0.5m);
		public static long RoundUp(double x) => (long)Math.Truncate(x * 1e11 + 0.5);
		public static double FloatDown(long x) => x * 1e-11;

		public I64Point()
		{ }

		public I64Point(double x, double y)
		{
			this.x = RoundUp(x);
			this.y = RoundUp(y);
		}
		public I64Point(long x, long y)
		{
			this.x = x;
			this.y = y;
		}

		public I64Point(Point original)
			:this(original.x,original.y)
		{

		}

		public override bool Equals(object obj)
		{
			return (obj is I64Point) && (x == ((I64Point)obj).x) && (y == ((I64Point)obj).y);
		}
		public bool Equals(I64Point obj)
		{
			return !object.Equals(obj,null) && (x == obj.x) && (y == obj.y);
		}

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ ~y.GetHashCode();
		}

		private double Sqr(long d) => d * d;
		public long dX(I64Point other) => (other.x - x);
		public long dY(I64Point other) => (other.y - y);
		public double distance2(I64Point other) => Sqr(other.x - x) + Sqr(other.y - y);

		const double Rad2Deg = 180 / Math.PI;
		public double angleTo(I64Point other) => (Math.Atan2(other.y - y, other.x - x) * Rad2Deg);

		public I64Point(Point3D pt)
			:this(pt.x,pt.y)
		{
		}

		public static explicit operator Point(I64Point pt) => new Point(I64Point.FloatDown(pt.x), I64Point.FloatDown(pt.y));
		public static explicit operator Point3D(I64Point pt) => new Point3D(I64Point.FloatDown(pt.x), I64Point.FloatDown(pt.y), 0);
		public static explicit operator I64Vector(I64Point pt) => new I64Vector(pt.x, pt.y);
		public static explicit operator Vector(I64Point pt) => new Vector(FloatDown(pt.x), FloatDown(pt.y));
		public static explicit operator System.Drawing.PointF(I64Point pt) => new System.Drawing.PointF((float)FloatDown(pt.x), (float)FloatDown(pt.y));
		public static I64Point operator *(I64Point pt, long scalar) => new I64Point(pt.x * scalar,pt.y * scalar);
		public static I64Point operator *(I64Point pt, double scalar) => new I64Point(Round(pt.x*scalar),Round(pt.y * scalar));
		public static I64Point operator /(I64Point pt, long scalar) => new I64Point(pt.x / scalar, pt.y / scalar);
		public static I64Point operator /(I64Point pt, double scalar) => new I64Point(Round(pt.x / scalar), Round(pt.y / scalar));

		public static I64Point operator +(I64Point pt, I64Vector ve) => new I64Point(pt.x + ve.dx, pt.y + ve.dy);
		public static I64Point operator -(I64Point pt, I64Vector ve) => new I64Point(pt.x - ve.dx, pt.y - ve.dy);
		public static I64Vector operator -(I64Point left, I64Point right) => new I64Vector(left.x - right.x, left.y - right.y);

		public static bool operator ==(I64Point left, I64Point right)
		{
			return !object.Equals(right,null) && left.x == right.x && left.y == right.y;
		}
		public static bool operator !=(I64Point left, I64Point right)
		{
			return object.Equals(right, null) || left.x != right.x || left.y != right.y;
		}
	}

	public class I64Vector
	{
		public long dx;
		public long dy;

		public I64Vector(long dx, long dy)
		{
			this.dx = dx;
			this.dy = dy;
		}
		public I64Vector(double dx, double dy)
		{
			this.dx = (long)Math.Truncate(dx + 0.5);
			this.dy = (long)Math.Truncate(dy + 0.5);
		}

		public I64Vector(I64Point pt)
		{
			dx = pt.x;
			dy = pt.y;
		}

		public I64Vector(I64Point frompt, I64Point topt)
		{
			dx = topt.x - frompt.x;
			dy = topt.y - frompt.y;
		}

		private double Sqr(double x) => x * x;
		public double length2 => Sqr(dx) + Sqr(dy);
		public double length => Math.Sqrt(Sqr(dx) + Sqr(dy));

		public void Unitize()
		{
			double len = length;
			dx = I64Point.Round(dx / len);
			dy = I64Point.Round(dy / len);
		}

		public I64Vector Identity
		{
			get
			{
				double len = length;
				return new I64Vector(dx / length, dy / length);
			}
		}

		public static I64Vector operator +(I64Vector v1, I64Vector v2)
		{
			return new I64Vector(v1.dx + v2.dx, v1.dy + v2.dy);
		}

		public static I64Vector operator -(I64Vector v1, I64Vector v2)
		{
			return new I64Vector(v1.dx - v2.dx, v1.dy - v2.dy);
		}

		public static bool operator ==(I64Vector v1, I64Vector v2)
		{
			return v1.dx * v2.dy == v1.dy * v2.dx;
		}
		public static bool operator !=(I64Vector v1, I64Vector v2)
		{
			return v1.dx * v2.dy != v1.dy * v2.dx;
		}

		public static bool operator >(I64Vector v1, I64Vector v2)
		{
			return v1.dy * v2.dx > v1.dx * v2.dy;
		}
		public static bool operator <(I64Vector v1, I64Vector v2)
		{
			return v1.dy * v2.dx < v1.dx * v2.dy;
		}
		public static bool operator >=(I64Vector v1, I64Vector v2)
		{
			return v1.dy * v2.dx >= v1.dx * v2.dy;
		}
		public static bool operator <=(I64Vector v1, I64Vector v2)
		{
			return v1.dy * v2.dx <= v1.dx * v2.dy;
		}
		public static I64Vector operator *(I64Vector v, long scalar) => new I64Vector(v.dx * scalar, v.dy * scalar);
		public static I64Vector operator *(I64Vector v, double scalar) => new I64Vector(I64Point.Round(v.dx * scalar), I64Point.Round(v.dy * scalar));
		public static I64Vector operator *(I64Vector v, decimal scalar) => new I64Vector(I64Point.Round(v.dx * scalar), I64Point.Round(v.dy * scalar));
		public static I64Vector operator /(I64Vector v, long scalar) =>new I64Vector(v.dx / scalar, v.dy / scalar);
		public static I64Vector operator /(I64Vector v, double scalar) => new I64Vector(I64Point.Round(v.dx / scalar), I64Point.Round(v.dy / scalar));
		public static I64Vector operator /(I64Vector v, decimal scalar) => new I64Vector(I64Point.Round(v.dx / scalar), I64Point.Round(v.dy / scalar));

		public static explicit operator I64Point(I64Vector v) => new I64Point(v.dx, v.dy);

		public override bool Equals(object obj)
		{
			return obj is I64Vector && ((I64Vector)obj).dx == dx && ((I64Vector)obj).dy == dy;
		}

		public override int GetHashCode()
		{
			return dx.GetHashCode() ^ ~dy.GetHashCode();
		}

		public I64Vector Perpendic => new I64Vector(-dy, dx);

		public long dotProduct(I64Vector other) => dx * other.dx + dy * other.dy;

		public enum quadrant
		{
			q11 = 7,
			q12 = 6,
			q22 = 5,
			q23 = 4,
			q33 = 3,
			q34 = 2,
			q44 = 1,
			q41 = 0
		}

		quadrant Quadrant =>
				dx > 0 ? (dy < 0 ? quadrant.q44 : dy == 0 ? quadrant.q41 : quadrant.q11) :
								dx == 0 ? (dy < 0 ? quadrant.q34 : quadrant.q12) :
												(dy < 0 ? quadrant.q33 : dy == 0 ? quadrant.q23 : quadrant.q22);

		public class Comparer : IComparer<I64Vector>
		{

			public int Compare(I64Vector x, I64Vector y)
			{
				quadrant qX = x.Quadrant;
				quadrant qY = y.Quadrant;
				if (qX != qY)
					return qX - qY;

				long detX = x.dy * y.dx;
				if (detX < 0) detX = -detX;
				long detY = y.dy * x.dx;
				if (detY < 0) detY = -detY;

				long xdy = x.dy < 0 ? -x.dy : x.dy;
				long ydy = y.dy < 0 ? -y.dy : y.dy;

				if (x.dx == 0 || x.dy == 0 || detX == detY)
					return 0;

				switch (qX)
				{
					case quadrant.q44: case quadrant.q22: return detX > detY ? 1 : -1;
					default: return detX > detY ? -1 : 1;
				}
			}
		}

		public static Comparer comparer = new Comparer();
	}

	public class I64Segment
	{
		public I64Point Pt1;
		public I64Point Pt2;

		public struct Intersection
		{
			public I64Point point;
			public bool in_this;
			public bool in_other;
		}

		public class Closed
		{
			public I64Segment S1;
			public I64Point node1;
			public I64Segment S2;
			public I64Point node2;
			public double dist;

			public Closed(double dist2)
			{
				this.dist = dist2;
			}
			public Closed(double dist2, I64Segment seg1, I64Point pt1, I64Segment seg2, I64Point pt2)
			{
				this.dist = dist2;
				S1 = seg1;
				node1 = pt1;
				S2 = seg2;
				node2 = pt2;
			}
			public void Set(double dist2, I64Segment seg1, I64Point pt1, I64Segment seg2, I64Point pt2)
			{
				this.dist = dist2;
				S1 = seg1;
				node1 = pt1;
				S2 = seg2;
				node2 = pt2;
			}
		}

		public I64Segment(Segment other)
			:this(other.Pt1,other.Pt2)
		{
		}
		public I64Segment(I64Point pt1, I64Point pt2)
		{
			this.Pt1 = pt1;
			this.Pt2 = pt2;
		}
		public I64Segment(Point pt1, Point pt2)
		{
			this.Pt1 = new I64Point(pt1);
			this.Pt2 = new I64Point(pt2);
		}
		public I64Segment(Point3D pt1, Point3D pt2)
		{
			this.Pt1 = new I64Point(pt1);
			this.Pt2 = new I64Point(pt2);
		}

		public double Length2 => (Pt2 - Pt1).length2;
		public double Length => (Pt2 - Pt1).length;
		public bool IsInside(I64Point pt) => (pt == Pt1) || (pt == Pt2) || ((pt - Pt1).length + (Pt2 - pt).length) == (Pt2 - Pt1).length;

		public I64Point Middle => new I64Point((Pt1.x + Pt2.x) / 2, (Pt1.y + Pt2.y) / 2);

		public I64Vector vector => Pt2 - Pt1;

		public Intersection Intersect(I64Segment other)
		{
			I64Vector thisvect = vector;
			I64Vector othervect = other.vector;

			Intersection inter = new Intersection();
			long divisor = thisvect.dy * othervect.dx - thisvect.dx * othervect.dy;
			if (divisor == 0)
				return inter;

			long D1_Y = thisvect.dy;
			long D1_X = -thisvect.dx;
			long C1 = D1_Y * this.Pt1.x + D1_X * this.Pt1.y;

			//Line2
			long D2_Y = othervect.dy;
			long D2_X = -othervect.dx;
			long C2 = D2_Y * other.Pt1.x + D2_X * other.Pt1.y;

			long nx = (D1_X * C2 - D2_X * C1) / divisor;
			long ny = (D2_Y * C1 - D1_Y * C2) / divisor;

			I64Point ptres = new I64Point(nx, ny);
			inter.point = ptres;

			inter.in_this = nx.between(this.Pt1.x, this.Pt2.x) && ny.between(this.Pt1.y, this.Pt2.y);
			inter.in_other = nx.between(other.Pt1.x, other.Pt2.x) && ny.between(other.Pt1.y, other.Pt2.y);

			return inter;
		}

		public bool IsIntersect(I64Segment other)
		{
			if (this.Pt1 == other.Pt1 || this.Pt2 == other.Pt2 || this.Pt1 == other.Pt2 || this.Pt2 == other.Pt1)
				return false;

			// calculate part equations for line-line intersection
			long a1 = this.Pt2.y - this.Pt1.y;
			long b1 = this.Pt1.x - this.Pt2.x;
			long a2 = other.Pt2.y - other.Pt1.y;
			long b2 = other.Pt1.x - other.Pt2.x;

			if ((a1 * b2) == (a2 * b1) || (a1 * b2) == (-a2 * b1))
				return false;

			long c1 = a1 * this.Pt1.x + b1 * this.Pt1.y;
			long c2 = a2 * other.Pt1.x + b2 * other.Pt1.y;
			// calculate the divisor
			long tmp = (a1 * b2 - a2 * b1);
			if (tmp == 0)
				return false;

			// calculate intersection point x coordinate
			long pX = (c1 * b2 - c2 * b1) / tmp;

			// check if intersection x coordinate lies in line line segment
			if ((pX > this.Pt1.x && pX > this.Pt2.x) || (pX > other.Pt1.x && pX > other.Pt2.x)
							|| (pX < this.Pt1.x && pX < this.Pt2.x) || (pX < other.Pt1.x && pX < other.Pt2.x))
			{
				return false;
			}

			// calculate intersection point y coordinate
			long pY = (a1 * c2 - a2 * c1) / tmp;

			// check if intersection y coordinate lies in line line segment
			if ((pY > this.Pt1.y && pY > this.Pt2.y) || (pY > other.Pt1.y && pY > other.Pt2.y)
							|| (pY < this.Pt1.y && pY < this.Pt2.y) || (pY < other.Pt1.y && pY < other.Pt2.y))
			{
				return false;
			}

			return true;
		}

		public double DistanceTo(I64Point point)
		{
			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return Math.Sqrt(b2);
			if (b2 - c2 == a2)
				return Math.Sqrt(a2);

			double m = b2 - a2 - c2;
			double C12 = m * m / (4 * c2);
			return Math.Sqrt(a2 - C12);
		}

		public double Distance2To(I64Point point)
		{
			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return b2;
			if (b2 - c2 == a2)
				return a2;

			double m = b2 - a2 - c2;
			double C12 = m * m / (4 * c2);
			return a2 - C12;
		}

		public double Distance2Inside(I64Point point)
		{
			Intersection inter = ProjectionOf(point);
			if (!inter.in_this)
				return double.NaN;

			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return b2;
			if (b2 - c2 == a2)
				return a2;

			double m = b2 - a2 - c2;
			double C12 = m * m / (4 * c2);
			return a2 - C12;
		}

		public Intersection ProjectionOf(I64Point pt)
		{
			Intersection inter = new Intersection();

			if (Pt1.x == Pt2.x) // vertical line
			{
				inter.point = new I64Point(Pt1.x, pt.y);
				inter.in_this = pt.y.between(Pt1.y, Pt2.y);
				return inter;
			}
			if (Pt1.y == Pt2.y) // horizontal line
			{
				inter.point = new I64Point(pt.x, Pt1.y);
				inter.in_this = pt.x.between(Pt1.x, Pt2.x);
				return inter;
			}

			fraction m = new fraction(Pt2.y - Pt1.y, Pt2.x - Pt1.x);
			fraction b = Pt1.y - (m * Pt1.x);

			long x = (long)((m * pt.y + pt.x - m * b) / (m * m + 1));
			long y = (long)((m * m * pt.y + m * pt.x + b) / (m * m + 1));

			I64Point ptres = new I64Point(x, y);
			inter.point = ptres;
			inter.in_this = x.between(Pt1.x, Pt2.x) && y.between(Pt1.y, Pt2.y);

			return inter;
		}

		public bool IsInline(I64Segment other)
		{
			return IsInside(other.Pt1) && IsInside(other.Pt2);
		}

		public double AngleMath(I64Point pt)
		{
			long CAy = pt.y - Pt1.y;
			long CAx = pt.x - Pt1.x;
			long BAy = Pt2.y - Pt1.y;
			long BAx = Pt2.x - Pt1.x;
			double rad = -Math.Atan2(CAy * BAx - CAx * BAy, CAx * BAx + CAy * BAy);
			return rad;// < 0 ? -1 : rad > 0 ? 1 : 0;
		}

		public decimal AngleCW(I64Point pt)
		{
			long CAy = pt.y - Pt1.y;
			long CAx = pt.x - Pt1.x;
			long BAy = Pt2.y - Pt1.y;
			long BAx = Pt2.x - Pt1.x;
			double rad = -Math.Atan2(CAy * BAx - CAx * BAy, CAx * BAx + CAy * BAy);
			return (decimal)(rad >= 0 ? rad : Math.PI * 2 + rad);

			//long rad2 = Math.Atan2(Pt2.y - Pt1.y, Pt2.x - Pt1.x);
			//long rad1 = Math.Atan2(pt.y - Pt1.y, pt.x - Pt1.x);
			//return Math.rad1 - rad2;
		}

		public bool IsParalelTo(I64Segment other) => (this.Pt2.y - this.Pt1.y) * (other.Pt2.x - other.Pt1.x) == (this.Pt2.x - this.Pt1.x) * (other.Pt2.y - other.Pt1.y);

		public Closed Distance2To(I64Segment other)
		{
			Intersection inter = Intersect(other);
			if (inter.point!=null && inter.in_this && inter.in_other)
				return new Closed(-1);

			I64Segment[][] projections = new I64Segment[][] { new I64Segment[2], new I64Segment[2] };

			inter = other.ProjectionOf(this.Pt1);
			if (inter.point!=null && inter.in_this)
				projections[0][0] = new I64Segment(this.Pt1, inter.point);
			inter = other.ProjectionOf(this.Pt2);
			if (inter.point != null && inter.in_this)
				projections[0][1] = new I64Segment(this.Pt2, inter.point);

			inter = this.ProjectionOf(other.Pt1);
			if (inter.point != null && inter.in_this)
				projections[1][0] = new I64Segment(other.Pt1, inter.point);
			inter = this.ProjectionOf(other.Pt2);
			if (inter.point != null && inter.in_this)
				projections[1][1] = new I64Segment(other.Pt2, inter.point);

			Closed res = new Closed(double.NaN);

			for (int i = 0; i < 2; i++)
				for (int j = 0; j < 2; j++)
				{
					I64Segment segX = projections[i][j];
					if (segX != null)
					{
						double dist2 = segX.Length2;
						if (dist2 < res.dist)
							res.Set(dist2, this, segX.Pt1, other, segX.Pt2);
					}
				}
			double dd = (this.Pt1 - other.Pt1).length2;
			if (dd < res.dist)
				res.Set(dd, this, this.Pt1, other, other.Pt1);
			dd = (this.Pt1 - other.Pt2).length2;
			if (dd < res.dist)
				res.Set(dd, this, this.Pt1, other, other.Pt2);
			dd = (this.Pt2 - other.Pt2).length2;
			if (dd < res.dist)
				res.Set(dd, this, this.Pt2, other, other.Pt2);
			dd = (this.Pt2 - other.Pt1).length2;
			if (dd < res.dist)
				res.Set(dd, this, this.Pt2, other, other.Pt1);

			return res;
		}

		public double CosineTo(I64Point pt)
		{
			double a2 = Length2;
			double b2 = Distance2To(pt);
			double c2 = (Pt2 - pt).length2;
			return (a2 + b2 - c2) / (2 * Math.Sqrt(a2 * b2));
		}
		public double CosineCenter(I64Point pt)
		{
			double a2 = (pt-Pt1).length2;
			double b2 = (pt-Pt2).length2;
			double c2 = Length2;
			return (a2 + b2 - c2) / (2 * Math.Sqrt(a2*b2));
		}

		public static bool operator == (I64Segment seg, I64Segment other) =>
			seg is I64Segment && other is I64Segment && 
			((seg.Pt1 == other.Pt1 && seg.Pt2 == other.Pt2) || (seg.Pt1 == other.Pt2 && seg.Pt2 == other.Pt1));

		public static bool operator !=(I64Segment seg, I64Segment other) =>
			!(seg is I64Segment) != !(other is I64Segment) || 
			(seg.Pt1 != other.Pt1 && seg.Pt1 != other.Pt2 )|| (seg.Pt2 != other.Pt2 && seg.Pt2 != other.Pt1);

		public override bool Equals(object obj)
		{
			return obj is I64Segment && ((I64Segment)obj) == this;
		}

		public override int GetHashCode()
		{
			return Pt1.GetHashCode() ^ ~Pt2.GetHashCode();
		}
	}

	public partial class I64Polygon
	{
		public I64Point[] pts;
		public I64Polygon(bool open = false, params I64Point[] points)
		{
			List<I64Point> lst = points.ToList();
			if (open)
			{
				lst.Add(points[0]);
			}
			pts = lst.ToArray();
		}
		protected virtual double SignedArea
		{
			get
			{
				// Add the first point to the end.
				int num_points = pts.Length - 1;

				// Get the areas.
				long area = 0;
				for (int i = 0; i < num_points; i++)
				{
					area +=
							(pts[i + 1].x - pts[i].x) *
							(pts[i + 1].y + pts[i].y) / 2;
				}
				return area;
			}
		}

		public virtual double Area => Math.Abs(SignedArea);

		public virtual I64Point Centroid
		{
			get
			{
				// Add the first point at the end of the array.
				int num_points = pts.Length - 1;

				// Find the centroid.
				long X = 0;
				long Y = 0;
				long second_factor;
				for (int i = 0; i < num_points; i++)
				{
					second_factor =
							pts[i].x * pts[i + 1].y -
							pts[i + 1].x * pts[i].y;
					X += (pts[i].x + pts[i + 1].x) * second_factor;
					Y += (pts[i].y + pts[i + 1].y) * second_factor;
				}

				// Divide by 6 times the I64Polygon's area.
				long I64Polygon_area = (long)Area;
				X /= (6 * I64Polygon_area);
				Y /= (6 * I64Polygon_area);

				// If the values are negative, the I64Polygon is
				// oriented counterclockwise so reverse the signs.
				if (X < 0)
				{
					X = -X;
					Y = -Y;
				}

				return new I64Point(X, Y);
			}
		}

		public dynamic Tag { get; set; }

		public I64Point GravityCenter
		{
			get
			{
				if (!pts.Any())
					return new I64Point(0,0);
				List<Triangulator.Geometry.Point> tpoints = pts.Select(p => new Triangulator.Geometry.Point(p.x, p.y)).ToList();
				List<Triangulator.Geometry.Triangle> tri = Triangulator.Delauney.Triangulate(tpoints);
				List<I64Triangle> I64Triangles = tri.Select(t => new I64Triangle(pts[t.p1], pts[t.p2], pts[t.p3]))
					.Where(t => t.pts[0] != t.pts[1] && t.pts[1] != t.pts[2] && t.pts[2] != t.pts[0])
					.ToList();
				if (I64Triangles.Count == 0)
					return pts[0];
				I64Triangles.ForEach(t =>
				{
					t.Tag = new ExpandoObject();
					t.Tag.ctr = t.Centroid;
					t.Tag.wei = t.Area;
				});
				long totwei = I64Triangles.Sum(t => (long)t.Tag.wei);
				long sumX = I64Triangles.Sum(t => ((I64Point)t.Tag.ctr).x);
				long sumY = I64Triangles.Sum(t => ((I64Point)t.Tag.ctr).y);
				long wesumX = I64Triangles.Sum(t => ((I64Point)t.Tag.ctr).x * (long)t.Tag.wei);
				long wesumY = I64Triangles.Sum(t => ((I64Point)t.Tag.ctr).y * (long)t.Tag.wei);
				long xcent = wesumX / totwei;
				long ycent = wesumY / totwei;
				I64Point newcentro = new I64Point(xcent, ycent);
				if (IsInside(newcentro))
					return newcentro;

				I64Point[] ctrs = I64Triangles.Select(t => (I64Point)t.Tag.ctr).Where(c => IsInside(c)).ToArray();
				var vdists = ctrs.Select(c => new { p = c, d = (newcentro - c).length2 }).ToArray();
				I64Point pt = vdists.FirstOrDefault(v => v.d == vdists.Min(vd => vd.d))?.p;
				return pt == null ? newcentro : pt;
			}
		}

		public bool IsInside(I64Point pt)
		{
			long minX = pts.Min(p => p.x);
			I64Segment seg = new I64Segment(new I64Point(minX, pt.y), pt);
			int wind = 0;
			for (int i = 0; i < pts.Length - 1; i++)
			{
				I64Segment segn = new I64Segment(pts[i], pts[i + 1]);
				I64Segment.Intersection inter = segn.Intersect(seg);
				if (inter.point!=null && inter.in_this && inter.in_other)
					wind++;
			}
			return wind % 2 == 1;
		}

		public I64Segment[] Segments
		{
			get
			{
				I64Segment[] segs = new I64Segment[pts.Length - 1];
				for (int i = 0; i < pts.Length - 1; i++)
					segs[i] = new I64Segment(pts[i], pts[i + 1]);
				return segs;
			}
		}
		public bool IsIntersecting(I64Polygon other)
		{
			I64Segment[] thissegs = Segments;
			I64Segment[] othersegs = other.Segments;
			foreach (I64Segment ts in thissegs)
				foreach (I64Segment os in othersegs)
				{
					I64Segment.Intersection inter = ts.Intersect(os);
					if (inter.point !=null && inter.in_this && inter.in_other)
						return true;
				}
			return false;
		}

		public I64Segment.Closed DistanceTo(I64Polygon other)
		{
			I64Segment.Closed res = new I64Segment.Closed(double.NaN);

			I64Segment[] thissegs = Segments;
			I64Segment[] othersegs = other.Segments;
			foreach (I64Segment ts in thissegs)
				foreach (I64Segment os in othersegs)
				{
					I64Segment.Closed clsd = ts.Distance2To(os);
					if (clsd.dist < res.dist)
						res = clsd;
				}
			if (res.dist > 0)
				res.dist = (long)Math.Sqrt(res.dist);
			return res;
		}
	}

	public class I64Triangle : I64Polygon
	{
		public I64Triangle(params I64Point[] points)
			: base(true, points)
		{
			if (points.Length != 3)
				throw new InvalidOperationException("Number of vertices of a I64Triangle is always 3!");
		}

		public override I64Point Centroid => (I64Point)(((I64Vector)pts[0] + (I64Vector)pts[1] + (I64Vector)pts[2]) / 3d);

		public double Perimeter => (pts[1] - pts[0]).length + (pts[2] - pts[1]).length + (pts[0] - pts[2]).length;

		public override double Area
		{
			get
			{
				double A = (pts[1] - pts[0]).length;
				double B = (pts[2] - pts[1]).length;
				double C = (pts[0] - pts[2]).length;
				double s = (A + B + C) / 2;
				double area = Math.Sqrt(s * (s - A) * (s - B) * (s - C));
				return area;
			}
		}
	}

}

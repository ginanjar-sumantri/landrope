using geo.shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media.Media3D;

namespace GeomHelper
{
	public class Matrix
	{
		double[][] rows;

		public double[] this[int index] => rows[index];
		public int rownum => rows.Length;
		public int colnum => rows[0].Length;

		public Matrix(params double[][] elems)
		{
			int[] lens = elems.Select(e => e.Length).ToArray();
			if (elems.Length == 1 && elems[0].Length == 1)
				throw new Exception("Matrix can not contains just 1x1 element");
			if (lens.Max() != lens.Min())
				throw new Exception("All Rows in a matrix must have equal number of columns");
			rows = elems;
		}

		public Matrix(int rownum, int colnum)
		{
			rows = new double[rownum][];
			for (int i = 0; i < rownum; i++)
			{
				rows[i] = new double[colnum];
			}
		}

		public static Matrix Identity(int dimension)
		{
			Matrix mat = new Matrix(dimension, dimension);
			for (int i = 0; i < dimension; i++)
			{
				mat.rows[i][i] = 1;
			}
			return mat;
		}

		public Matrix Multiply(Matrix right)
		{
			if (colnum != right.rownum)
				throw new Exception("For multiply matrixes, Left matrix's number of columns must be equal with right matrix's number of rows");

			Matrix mat = new Matrix(rownum, right.colnum);
			for (int lrow = 0; lrow < rownum; lrow++)
			{
				for (int rcol = 0; rcol < right.colnum; rcol++)
				{
					double val = 0;
					for (int i = 0; i < colnum; i++)
						val += rows[lrow][i] * right.rows[i][rcol];
					mat.rows[lrow][rcol] = val;
				}
			}
			return mat;
		}

		public Matrix Add(Matrix right)
		{
			if (colnum != right.colnum || rownum != right.rownum)
				throw new Exception("For Add matrixes, both matrixes has to be identical");

			Matrix mat = new Matrix(rownum, colnum);
			for (int r = 0; r < rownum; r++)
				for (int c = 0; c < colnum; c++)
					mat.rows[r][c] = rows[r][c] + right.rows[r][c];
			return mat;
		}

		public Matrix Subtract(Matrix right)
		{
			if (colnum != right.colnum || rownum != right.rownum)
				throw new Exception("For Subtract matrixes, both matrixes has to be identical");

			Matrix mat = new Matrix(rownum, colnum);
			for (int r = 0; r < rownum; r++)
				for (int c = 0; c < colnum; c++)
					mat.rows[r][c] = rows[r][c] - right.rows[r][c];
			return mat;
		}

		public static Matrix Rotation2D(double theta)
		{
			Matrix mat = new Matrix(3, 3);
			mat.rows[2][2] = 1;
			double cos = Math.Cos(theta);
			double sin = Math.Sin(theta);

			mat.rows[0][0] = mat.rows[1][1] = cos;
			mat.rows[0][1] = -sin;
			mat.rows[1][0] = sin;
			return mat;
		}

		public static Matrix Rotation2D(double theta, Point center)
		{
			Matrix matR = new Matrix(3, 3);
			matR.rows[2][2] = 1;
			double cos = Math.Cos(theta);
			double sin = Math.Sin(theta);

			matR.rows[0][0] = matR.rows[1][1] = cos;
			matR.rows[0][1] = -sin;
			matR.rows[1][0] = sin;

			Matrix matT = Identity(3);
			matT.rows[0][2] = -center.x;
			matT.rows[1][2] = -center.y;

			Matrix mat = matR * matT;
			return mat;
		}

		public Point Transform(Point pt)
		{
			Matrix mpt = new Matrix(new double[] { pt.x }, new double[] { pt.y }, new double[] { 1 });
			Matrix rpt = this * mpt;
			return new Point(rpt.rows[0][0], rpt.rows[1][0]);
		}
		public Vector Transform2(Point pt)
		{
			Matrix mpt = new Matrix(new double[] { pt.x }, new double[] { pt.y }, new double[] { 1 });
			Matrix rpt = this * mpt;
			return new Vector(rpt.rows[0][0], rpt.rows[1][0]);
		}

		public static Matrix operator *(Matrix left, Matrix right) => left.Multiply(right);
		public static Matrix operator +(Matrix left, Matrix right) => left.Add(right);
		public static Matrix operator -(Matrix left, Matrix right) => left.Subtract(right);
	}

	public class Segment
	{
		public geo.shared.Point Pt1;
		public geo.shared.Point Pt2;

		public struct Intersection
		{
			public Point point;
			public bool in_this;
			public bool in_other;
		}

		public class Closed
		{
			public Segment S1;
			public geo.shared.Point node1;
			public Segment S2;
			public geo.shared.Point node2;
			public double dist;

			public Closed(double dist2)
			{
				this.dist = dist2;
			}
			public Closed(double dist2, Segment seg1, geo.shared.Point pt1, Segment seg2, geo.shared.Point pt2)
			{
				this.dist = dist2;
				S1 = seg1;
				node1 = pt1;
				S2 = seg2;
				node2 = pt2;
			}
			public void Set(double dist2, Segment seg1, geo.shared.Point pt1, Segment seg2, geo.shared.Point pt2)
			{
				this.dist = dist2;
				S1 = seg1;
				node1 = pt1;
				S2 = seg2;
				node2 = pt2;
			}
		}

		public Segment(double x1, double y1, double x2, double y2)
		{
			this.Pt1 = new Point(x1, y1);
			this.Pt2 = new Point(x2, y2);
		}

		public Segment(geo.shared.Point pt1, geo.shared.Point pt2)
		{
			this.Pt1 = pt1;
			this.Pt2 = pt2;
		}
		public Segment(geo.shared.Point3D pt1, geo.shared.Point3D pt2)
		{
			this.Pt1 = new Point(pt1);
			this.Pt2 = new Point(pt2);
		}

		public double Length2 => (Pt2 - Pt1).length2;
		public double Length => (Pt2 - Pt1).length;
		public bool IsInside(Point pt) => (pt == Pt1) || (pt == Pt2) || ((pt - Pt1).length + (Pt2 - pt).length) == (Pt2 - Pt1).length;

		public Point Middle => new Point((Pt1.x + Pt2.x) / 2, (Pt1.y + Pt2.y) / 2);

		public Vector vector => Pt2 - Pt1;

		public Intersection Intersect(Segment other)
		{
			Vector thisvect = vector;
			Vector othervect = other.vector;

			Intersection inter = new Intersection();
			double divisor = thisvect.dy * othervect.dx - thisvect.dx * othervect.dy;
			if (divisor == 0)
				return inter;

			double D1_Y = thisvect.dy;
			double D1_X = -thisvect.dx;
			double C1 = D1_Y * this.Pt1.x + D1_X * this.Pt1.y;

			//Line2
			double D2_Y = othervect.dy;
			double D2_X = -othervect.dx;
			double C2 = D2_Y * other.Pt1.x + D2_X * other.Pt1.y;

			double nx = (D1_X * C2 - D2_X * C1) / divisor;
			double ny = (D2_Y * C1 - D1_Y * C2) / divisor;

			Point ptres = new Point(nx, ny);
			inter.point = ptres;

			inter.in_this = nx.between(this.Pt1.x, this.Pt2.x) && ny.between(this.Pt1.y, this.Pt2.y);
			inter.in_other = nx.between(other.Pt1.x, other.Pt2.x) && ny.between(other.Pt1.y, other.Pt2.y);

			return inter;
		}

		public bool IsIntersect(Segment other)
		{
			if (this.Pt1 == other.Pt1 || this.Pt2 == other.Pt2 || this.Pt1 == other.Pt2 || this.Pt2 == other.Pt1)
				return false;

			// calculate part equations for line-line intersection
			double a1 = this.Pt2.y - this.Pt1.y;
			double b1 = this.Pt1.x - this.Pt2.x;
			double a2 = other.Pt2.y - other.Pt1.y;
			double b2 = other.Pt1.x - other.Pt2.x;

			if ((a1 * b2) == (a2 * b1) || (a1 * b2) == (-a2 * b1))
				return false;

			double c1 = a1 * this.Pt1.x + b1 * this.Pt1.y;
			double c2 = a2 * other.Pt1.x + b2 * other.Pt1.y;
			// calculate the divisor
			double tmp = (a1 * b2 - a2 * b1);
			if (tmp == 0)
				return false;

			// calculate intersection point x coordinate
			double pX = (c1 * b2 - c2 * b1) / tmp;

			// check if intersection x coordinate lies in line line segment
			if ((pX > this.Pt1.x && pX > this.Pt2.x) || (pX > other.Pt1.x && pX > other.Pt2.x)
							|| (pX < this.Pt1.x && pX < this.Pt2.x) || (pX < other.Pt1.x && pX < other.Pt2.x))
			{
				return false;
			}

			// calculate intersection point y coordinate
			double pY = (a1 * c2 - a2 * c1) / tmp;

			// check if intersection y coordinate lies in line line segment
			if ((pY > this.Pt1.y && pY > this.Pt2.y) || (pY > other.Pt1.y && pY > other.Pt2.y)
							|| (pY < this.Pt1.y && pY < this.Pt2.y) || (pY < other.Pt1.y && pY < other.Pt2.y))
			{
				return false;
			}

			return true;
		}

		//public (bool ints, bool invtx) IsIntersect2(Segment other)
		//{
		//	if (this.Pt1 == other.Pt1 || this.Pt2 == other.Pt2 || this.Pt1 == other.Pt2 || this.Pt2 == other.Pt1)
		//		return (true,true);

		//	// calculate part equations for line-line intersection
		//	double a1 = this.Pt2.y - this.Pt1.y;
		//	double b1 = this.Pt1.x - this.Pt2.x;
		//	double a2 = other.Pt2.y - other.Pt1.y;
		//	double b2 = other.Pt1.x - other.Pt2.x;

		//	if ((a1 * b2) == (a2 * b1) || (a1 * b2) == (-a2 * b1))
		//		return (false,false);

		//	double c1 = a1 * this.Pt1.x + b1 * this.Pt1.y;
		//	double c2 = a2 * other.Pt1.x + b2 * other.Pt1.y;
		//	// calculate the divisor
		//	double tmp = (a1 * b2 - a2 * b1);
		//	if (tmp == 0)
		//		return (false,false);

		//	// calculate intersection point x coordinate
		//	double pX = (c1 * b2 - c2 * b1) / tmp;

		//	// check if intersection x coordinate lies in line line segment
		//	if ((pX > this.Pt1.x && pX > this.Pt2.x) || (pX > other.Pt1.x && pX > other.Pt2.x)
		//					|| (pX < this.Pt1.x && pX < this.Pt2.x) || (pX < other.Pt1.x && pX < other.Pt2.x))
		//	{
		//		return (false,false);
		//	}

		//	// calculate intersection point y coordinate
		//	double pY = (a1 * c2 - a2 * c1) / tmp;

		//	// check if intersection y coordinate lies in line line segment
		//	if ((pY > this.Pt1.y && pY > this.Pt2.y) || (pY > other.Pt1.y && pY > other.Pt2.y)
		//					|| (pY < this.Pt1.y && pY < this.Pt2.y) || (pY < other.Pt1.y && pY < other.Pt2.y))
		//	{
		//		return (false,false);
		//	}

		//	var ptxy = new Point(pX, pY);
		//	var invtx = (ptxy == Pt1 || ptxy == Pt2);
		//	return (true,invtx);
		//}

		(double dy, double dx) gradient => (Pt2.y - Pt1.y, Pt2.x - Pt1.x);

		public (bool ints, bool invtx) IsIntersect2(Segment other)
		{
			if (this.Pt1 == other.Pt1 || this.Pt2 == other.Pt2 || this.Pt1 == other.Pt2 || this.Pt2 == other.Pt1)
				return (true,true);

			var m1 = this.gradient;
			var m2 = other.gradient;

			if ((m1.dy * m2.dx) == (m2.dy * m1.dx) || (m1.dy * m2.dx) == (-m2.dy * m1.dx))
				return (false,false);

			double c1 = m1.dy * this.Pt1.x + m1.dx * this.Pt1.y;
			double c2 = m2.dy * other.Pt1.x + m2.dx * other.Pt1.y;
			// calculate the divisor
			double tmp = (m1.dy * m2.dx - m2.dy * m1.dx);
			if (tmp == 0)
				return (false,false);

			// calculate intersection point x coordinate
			double pX = (c1 * m2.dx - c2 * m1.dx) / tmp;

			// check if intersection x coordinate lies in line line segment
			if ((pX > this.Pt1.x && pX > this.Pt2.x) || (pX > other.Pt1.x && pX > other.Pt2.x)
							|| (pX < this.Pt1.x && pX < this.Pt2.x) || (pX < other.Pt1.x && pX < other.Pt2.x))
				return (false,false);

			// calculate intersection point y coordinate
			double pY = (m1.dy * c2 - m1.dy * c1) / tmp;

			// check if intersection y coordinate lies in line line segment
			if ((pY > this.Pt1.y && pY > this.Pt2.y) || (pY > other.Pt1.y && pY > other.Pt2.y)
							|| (pY < this.Pt1.y && pY < this.Pt2.y) || (pY < other.Pt1.y && pY < other.Pt2.y))
				return (false,false);

			var invtx = new[] { this.Pt1.x, this.Pt2.x }.Contains(pX) ||
										new[] { this.Pt1.y, this.Pt2.y }.Contains(pY);
			return (true,invtx);
		}

		public double DistanceTo(Point point)
		{
			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return Math.Sqrt(b2);
			if (b2 - c2 == a2)
				return Math.Sqrt(a2);

			double m = b2 - a2 - c2;
			double C12 = m * m / 4 / c2;
			return Math.Sqrt(a2 - C12);
		}

		public double Distance2To(Point point)
		{
			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return b2;
			if (b2 - c2 == a2)
				return a2;

			double m = b2 - a2 - c2;
			double C12 = m * m / 4 / c2;
			return a2 - C12;
		}

		public double Distance2Inside(Point point)
		{
			Intersection inter = ProjectionOf(point);
			if (!inter.in_this)
				return 1e100;

			double a2 = (point - Pt1).length2;
			double b2 = (point - Pt2).length2;
			double c2 = Length2;
			if (a2 - c2 == b2)
				return b2;
			if (b2 - c2 == a2)
				return a2;

			double m = b2 - a2 - c2;
			double C12 = m * m / 4 / c2;
			return a2 - C12;
		}

		public Intersection ProjectionOf(Point pt)
		{
			Intersection inter = new Intersection();

			if (Pt1.x == Pt2.x) // vertical line
			{
				inter.point = new Point(Pt1.x, pt.y);
				inter.in_this = pt.y.between(Pt1.y, Pt2.y);
				return inter;
			}
			if (Pt1.y == Pt2.y) // horizontal line
			{
				inter.point = new Point(pt.x, Pt1.y);
				inter.in_this = pt.x.between(Pt1.x, Pt2.x);
				return inter;
			}

			fraction m = new fraction(Pt2.y - Pt1.y, Pt2.x - Pt1.x);
			fraction b = Pt1.y - (m * Pt1.x);

			double x = (double)((m * pt.y + pt.x - m * b) / (m * m + 1));
			double y = (double)((m * m * pt.y + m * pt.x + b) / (m * m + 1));

			Point ptres = new Point(x, y);
			inter.point = ptres;
			inter.in_this = x.between(Pt1.x, Pt2.x) && y.between(Pt1.y, Pt2.y);

			return inter;
		}

		public bool IsInline(Segment other)
		{
			return IsInside(other.Pt1) && IsInside(other.Pt2);
		}

		public double AngleMath(Point pt)
		{
			double CAy = pt.y - Pt1.y;
			double CAx = pt.x - Pt1.x;
			double BAy = Pt2.y - Pt1.y;
			double BAx = Pt2.x - Pt1.x;
			double rad = -Math.Atan2(CAy * BAx - CAx * BAy, CAx * BAx + CAy * BAy);
			return rad;// < 0 ? -1 : rad > 0 ? 1 : 0;
		}

		public double AngleCW(Point pt)
		{
			double CAy = pt.y - Pt1.y;
			double CAx = pt.x - Pt1.x;
			double BAy = Pt2.y - Pt1.y;
			double BAx = Pt2.x - Pt1.x;
			double rad = -Math.Atan2(CAy * BAx - CAx * BAy, CAx * BAx + CAy * BAy);
			return rad >= 0 ? rad : Math.PI * 2 + rad;

			//double rad2 = Math.Atan2(Pt2.y - Pt1.y, Pt2.x - Pt1.x);
			//double rad1 = Math.Atan2(pt.y - Pt1.y, pt.x - Pt1.x);
			//return Math.rad1 - rad2;
		}

		public bool IsParalelTo(Segment other) => (this.Pt2.y - this.Pt1.y) * (other.Pt2.x - other.Pt1.x) == (this.Pt2.x - this.Pt1.x) * (other.Pt2.y - other.Pt1.y);

		public Closed Distance2To(Segment other)
		{
			Intersection inter = Intersect(other);
			if (inter.point != null && inter.in_this && inter.in_other)
				return new Closed(-1);

			Segment[][] projections = new Segment[][] { new Segment[2], new Segment[2] };

			inter = other.ProjectionOf(this.Pt1);
			if (inter.point != null && inter.in_this)
				projections[0][0] = new Segment(this.Pt1, inter.point);
			inter = other.ProjectionOf(this.Pt2);
			if (inter.point != null && inter.in_this)
				projections[0][1] = new Segment(this.Pt2, inter.point);

			inter = this.ProjectionOf(other.Pt1);
			if (inter.point != null && inter.in_this)
				projections[1][0] = new Segment(other.Pt1, inter.point);
			inter = this.ProjectionOf(other.Pt2);
			if (inter.point != null && inter.in_this)
				projections[1][1] = new Segment(other.Pt2, inter.point);

			Closed res = new Closed(1e7);

			for (int i = 0; i < 2; i++)
				for (int j = 0; j < 2; j++)
				{
					Segment segX = projections[i][j];
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

		public double CosineTo(Point pt)
		{
			double a2 = Length2;
			double b2 = Distance2To(pt);
			double c2 = (Pt2 - pt).length2;
			return (a2 + b2 - c2) / 2 / Math.Sqrt(a2 * b2);
		}
		public double CosineCenter(Point pt)
		{
			double a2 = (pt - Pt1).length2;
			double b2 = (pt - Pt2).length2;
			double c2 = Length2;
			return (a2 + b2 - c2) / 2 / Math.Sqrt(a2 * b2);
		}

		public static bool operator ==(Segment seg, Segment other) =>
			seg is Segment && other is Segment &&
			((seg.Pt1 == other.Pt1 && seg.Pt2 == other.Pt2) || (seg.Pt1 == other.Pt2 && seg.Pt2 == other.Pt1));

		public static bool operator !=(Segment seg, Segment other) =>
			!(seg is Segment) != !(other is Segment) ||
			(seg.Pt1 != other.Pt1 && seg.Pt1 != other.Pt2) || (seg.Pt2 != other.Pt2 && seg.Pt2 != other.Pt1);

		public override bool Equals(object obj)
		{
			return obj is Segment && ((Segment)obj) == this;
		}

		public override int GetHashCode()
		{
			return Pt1.GetHashCode() ^ ~Pt2.GetHashCode();
		}
	}

	public partial class Polygon
	{
		public Point[] pts;
		public Polygon(bool open = false, params Point[] points)
		{
			List<Point> lst = points.ToList();
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
				double area = 0;
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

		public virtual Point Centroid
		{
			get
			{
				// Add the first point at the end of the array.
				int num_points = pts.Length - 1;

				// Find the centroid.
				double X = 0;
				double Y = 0;
				double second_factor;
				for (int i = 0; i < num_points; i++)
				{
					second_factor =
							pts[i].x * pts[i + 1].y -
							pts[i + 1].x * pts[i].y;
					X += (pts[i].x + pts[i + 1].x) * second_factor;
					Y += (pts[i].y + pts[i + 1].y) * second_factor;
				}

				// Divide by 6 times the polygon's area.
				double polygon_area = Area;
				X /= (6 * polygon_area);
				Y /= (6 * polygon_area);

				// If the values are negative, the polygon is
				// oriented counterclockwise so reverse the signs.
				if (X < 0)
				{
					X = -X;
					Y = -Y;
				}

				return new Point(X, Y);
			}
		}

		public dynamic Tag { get; set; }

		//public Point GravityCenter
		//{
		//	get
		//	{
		//		if (!pts.Any())
		//			return new Point(double.NaN, double.NaN);
		//		List<Triangulator.Geometry.Point> tpoints = pts.Select(p => new Triangulator.Geometry.Point(p.x, p.y)).ToList();
		//		List<Triangulator.Geometry.Triangle> tri = Triangulator.Delauney.Triangulate(tpoints);
		//		List<Triangle> triangles = tri.Select(t => new Triangle(pts[t.p1], pts[t.p2], pts[t.p3]))
		//			.Where(t => t.pts[0] != t.pts[1] && t.pts[1] != t.pts[2] && t.pts[2] != t.pts[0])
		//			.ToList();
		//		if (triangles.Count == 0)
		//			return pts[0];
		//		triangles.ForEach(t =>
		//		{
		//			t.Tag = new ExpandoObject();
		//			t.Tag.ctr = t.Centroid;
		//			t.Tag.wei = t.Area;
		//		});
		//		double totwei = triangles.Sum(t => (double)t.Tag.wei);
		//		double sumX = triangles.Sum(t => ((Point)t.Tag.ctr).x);
		//		double sumY = triangles.Sum(t => ((Point)t.Tag.ctr).y);
		//		double wesumX = triangles.Sum(t => ((Point)t.Tag.ctr).x * (double)t.Tag.wei);
		//		double wesumY = triangles.Sum(t => ((Point)t.Tag.ctr).y * (double)t.Tag.wei);
		//		double xcent = wesumX / totwei;
		//		double ycent = wesumY / totwei;
		//		Point newcentro = new Point(xcent, ycent);
		//		if (IsInside(newcentro))
		//			return newcentro;

		//		Point[] ctrs = triangles.Select(t => (Point)t.Tag.ctr).Where(c => IsInside(c)).ToArray();
		//		var vdists = ctrs.Select(c => new { p = c, d = (newcentro - c).length2 }).ToArray();
		//		Point pt = vdists.FirstOrDefault(v => v.d == vdists.Min(vd => vd.d))?.p;
		//		return pt == null ? newcentro : pt;
		//	}
		//}

		public bool IsInside(Point pt)
		{
			double minX = pts.Min(p => p.x);
			Segment seg = new Segment(new Point(minX, pt.y), pt);
			int wind = 0;
			for (int i = 0; i < pts.Length - 1; i++)
			{
				Segment segn = new Segment(pts[i], pts[i + 1]);
				Segment.Intersection inter = segn.Intersect(seg);
				if (inter.point != null && inter.in_this && inter.in_other)
					wind++;
			}
			return wind % 2 == 1;
		}

		public Segment[] Segments
		{
			get
			{
				Segment[] segs = new Segment[pts.Length - 1];
				for (int i = 0; i < pts.Length - 1; i++)
					segs[i] = new Segment(pts[i], pts[i + 1]);
				return segs;
			}
		}
		public bool IsIntersecting(Polygon other)
		{
			Segment[] thissegs = Segments;
			Segment[] othersegs = other.Segments;
			foreach (Segment ts in thissegs)
				foreach (Segment os in othersegs)
				{
					Segment.Intersection inter = ts.Intersect(os);
					if (inter.point != null && inter.in_this && inter.in_other)
						return true;
				}
			return false;
		}

		public Segment.Closed DistanceTo(Polygon other)
		{
			Segment.Closed res = new Segment.Closed(1e7);

			Segment[] thissegs = Segments;
			Segment[] othersegs = other.Segments;
			foreach (Segment ts in thissegs)
				foreach (Segment os in othersegs)
				{
					Segment.Closed clsd = ts.Distance2To(os);
					if (clsd.dist < res.dist)
						res = clsd;
				}
			if (res.dist > 0)
				res.dist = Math.Sqrt(res.dist);
			return res;
		}
	}

	public class Triangle : Polygon
	{
		public Triangle(params Point[] points)
			: base(true, points)
		{
			if (points.Length != 3)
				throw new InvalidOperationException("Number of vertices of a triangle is always 3!");
		}

		public override Point Centroid => (Point)(((Vector)pts[0] + (Vector)pts[1] + (Vector)pts[2]) / 3d);

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

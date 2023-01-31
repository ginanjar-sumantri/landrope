using System;
using System.Collections.Generic;
using System.Text;

namespace geo.shared
{
	public class Point
	{

		public double x;
		public double y;

		public Point()
		{ }

		public Point(double x, double y)
		{
			this.x = Math.Truncate(x * 1e11 + 0.5) * 1e-11;
			this.y = Math.Truncate(y * 1e11 + 0.5) * 1e-11;
		}
		public Point(Point3D pt)
			:this(pt.x, pt.y)
		{
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Point))
				return false;
			var dx = x - ((Point)obj).x;
			var dy = y - ((Point)obj).y;
			return (dx * dx + dy * dy) < 1e-2;
		}

		public override int GetHashCode()
		{
			// http://stackoverflow.com/questions/22826326/good-hashcode-function-for-2d-coordinates
			// http://www.cs.upc.edu/~alvarez/calculabilitat/enumerabilitat.pdf
			//int tmp = (int)(y + ((x + 1) / 2));
			//return Math.Abs((int)(x + (tmp * tmp)));
			return x.GetHashCode() ^ ~y.GetHashCode();
		}

		private double Sqr(double d) => d * d;
		public double dX(Point other) => (other.x - x);
		public double dY(Point other) => (other.y - y);
		public double distance2(Point other) => Sqr(other.x - x) + Sqr(other.y - y);

		public double angleTo(Point other) => Math.Atan2(other.y - y, other.x - x);

		public class Comparer : IEqualityComparer<Point>
		{
			public bool Equals(Point x, Point y)
			{
				var dx = x.x - y.x;
				var dy = x.y - y.y;
				return (dx * dx) < 1e-6 && (dy * dy) < 1e-6;
			}

			public int GetHashCode(Point obj) => obj.GetHashCode();
		}

		public static Comparer comparer = new Comparer();

		public static explicit operator Vector(Point pt) => new Vector(pt.x, pt.y);
		public static explicit operator System.Drawing.PointF(Point pt) => new System.Drawing.PointF((float)pt.x, (float)pt.y);
		public static Point operator *(Point pt, double scalar)
		{
			pt.x *= scalar;
			pt.y *= scalar;
			return pt;
		}
		public static Point operator /(Point pt, double scalar)
		{
			pt.x /= scalar;
			pt.y /= scalar;
			return pt;
		}

		public static Point operator +(Point pt, Vector ve) => new Point(pt.x + ve.dx, pt.y + ve.dy);
		public static Point operator -(Point pt, Vector ve) => new Point(pt.x - ve.dx, pt.y - ve.dy);
		public static Vector operator -(Point left, Point right) => new Vector(left.x - right.x, left.y - right.y);

		public static bool operator ==(Point left, Point right)
		{
			return right is Point && left.x == right.x && left.y == right.y;
		}
		public static bool operator !=(Point left, Point right)
		{
			return !(right is Point) || left.x != right.x || left.y != right.y;
		}

	}

	public class Point3D : Point
	{
		public double z;

		public Point3D()
			:base()
		{ }

		public Point3D(double x, double y, double z)
			:base(x,y)
		{
			this.z = Math.Truncate(z * 1e11 + 0.5) * 1e-11;
		}
		public Point3D(Point pt)
			:this(pt.x, pt.y, 0)
		{
		}

		public override bool Equals(object obj)
		{
			return (obj is Point3D) && (x == ((Point3D)obj).x) && (y == ((Point3D)obj).y) && (z == ((Point3D)obj).z);
		}

		public override int GetHashCode()
		{
			// http://stackoverflow.com/questions/22826326/good-hashcode-function-for-2d-coordinates
			// http://www.cs.upc.edu/~alvarez/calculabilitat/enumerabilitat.pdf
			//int tmp = (int)(y + ((x + 1) / 2));
			//return Math.Abs((int)(x + (tmp * tmp)));
			return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
		}

		private double Sqr(double d) => d * d;
		public double dX(Point3D other) => (other.x - x);
		public double dY(Point3D other) => (other.y - y);
		public double dZ(Point3D other) => (other.z - z);
		public double distance2(Point3D other) => Sqr(other.x - x) + Sqr(other.y - y) + Sqr(other.z - z);

		public double angleTo(Point3D other) => Math.Atan2(other.y - y, other.x - x);

		public static Point3D operator *(Point3D pt, double scalar)
		{
			pt.x *= scalar;
			pt.y *= scalar;
			pt.z *= scalar;
			return pt;
		}
		public static Point3D operator /(Point3D pt, double scalar)
		{
			pt.x /= scalar;
			pt.y /= scalar;
			pt.z /= scalar;
			return pt;
		}

		public static Point3D operator +(Point3D pt, Vector ve) => new Point3D(pt.x + ve.dx, pt.y + ve.dy, pt.z);
		public static Point3D operator -(Point3D pt, Vector ve) => new Point3D(pt.x - ve.dx, pt.y - ve.dy, pt.z);
		public static Vector operator -(Point3D left, Point3D right) => new Vector(left.x - right.x, left.y - right.y);

		public static bool operator ==(Point3D left, Point3D right)
		{
			return right is Point3D && left.x == right.x && left.y == right.y && left.z == right.z;
		}
		public static bool operator !=(Point3D left, Point3D right)
		{
			return !(right is Point3D) || left.x != right.x || left.y != right.y || left.z != right.z;
		}
	}

	public class Vector
	{
		public double dx;
		public double dy;

		public Vector() { }
		public Vector(double dx, double dy)
		{
			this.dx = dx;
			this.dy = dy;
		}

		public Vector(Point pt)
		{
			dx = pt.x;
			dy = pt.y;
		}

		public Vector(Point frompt, Point topt)
		{
			dx = topt.x - frompt.x;
			dy = topt.y - frompt.y;
		}

		public double length2 => dx * dx + dy * dy;
		public double length => Math.Sqrt(dx * dx + dy * dy);

		public void Unitize()
		{
			double len = length;
			dx /= len;
			dy /= len;
		}

		public Vector Identity
		{
			get
			{
				double len = length;
				return new Vector(dx / length, dy / length);
			}
		}

		public static Vector operator +(Vector v1, Vector v2)
		{
			return new Vector(v1.dx + v2.dx, v1.dy + v2.dy);
		}

		public static Vector operator -(Vector v1, Vector v2)
		{
			return new Vector(v1.dx - v2.dx, v1.dy - v2.dy);
		}

		public static bool operator ==(Vector v1, Vector v2)
		{
			return v1.dx * v2.dy == v1.dy * v2.dx;
		}
		public static bool operator !=(Vector v1, Vector v2)
		{
			return v1.dx * v2.dy != v1.dy * v2.dx;
		}

		public static bool operator >(Vector v1, Vector v2)
		{
			return v1.dy * v2.dx > v1.dx * v2.dy;
		}
		public static bool operator <(Vector v1, Vector v2)
		{
			return v1.dy * v2.dx < v1.dx * v2.dy;
		}
		public static bool operator >=(Vector v1, Vector v2)
		{
			return v1.dy * v2.dx >= v1.dx * v2.dy;
		}
		public static bool operator <=(Vector v1, Vector v2)
		{
			return v1.dy * v2.dx <= v1.dx * v2.dy;
		}
		public static Vector operator *(Vector v, double scalar)
		{
			return new Vector(v.dx * scalar, v.dy * scalar);
		}
		public static Vector operator /(Vector v, double scalar)
		{
			return new Vector(v.dx / scalar, v.dy / scalar);
		}

		public static explicit operator Point(Vector v) => new Point(v.dx, v.dy);

		public override bool Equals(object obj)
		{
			return obj is Vector && ((Vector)obj).dx == dx && ((Vector)obj).dy == dy;
		}

		public override int GetHashCode()
		{
			return dx.GetHashCode() ^ ~dy.GetHashCode();
		}

		public Vector Perpendic => new Vector(-dy, dx);

		public double dotProduct(Vector other) => dx * other.dx + dy * other.dy;

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

		public class Comparer : IComparer<Vector>
		{

			public int Compare(Vector x, Vector y)
			{
				if (double.IsNaN(x.dx) || double.IsNaN(x.dy))
				{
					return 0;
				}
				quadrant qX = x.Quadrant;
				quadrant qY = y.Quadrant;
				if (qX != qY)
					return qX - qY;

				double detX = x.dy * y.dx;
				if (detX < 0) detX = -detX;
				double detY = y.dy * x.dx;
				if (detY < 0) detY = -detY;

				double xdy = x.dy < 0 ? -x.dy : x.dy;
				double ydy = y.dy < 0 ? -y.dy : y.dy;

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

}

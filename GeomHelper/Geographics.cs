using geo.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media.Media3D;

namespace GeomHelper
{
	public static class SphericalMercator
	{
		public struct Boundary
		{
			double Top, Left, Right, Bottom;
			public Boundary(double top, double left, double right, double bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}
		}

		const double R = 6378137;
		const double MAX_LATITUDE = 85.0511287798;

		public static Point3D project(this Point3D asLatLng)
		{
			double d = Math.PI / 180;
			double max = MAX_LATITUDE;
			double lat = Math.Max(Math.Min(max, asLatLng.y), -max);
			double sin = Math.Sin(lat * d);

			return new Point3D(R * asLatLng.x * d, R * Math.Log((1 + sin) / (1 - sin)) / 2, 0);
		}

		public static Point3D unproject(this Point3D asPoint)
		{
			double d = 180 / Math.PI;
			return new Point3D(asPoint.x * d / R, (2 * Math.Atan(Math.Exp(asPoint.y / R)) - (Math.PI / 2)) * d,0);
		}

		public static Boundary bounds()
		{
			var d = R * Math.PI;
			return new Boundary(-d, -d, d, d);
		}
	}

	//#region GMap Object

	//public abstract class gmapObject
	//{
	//	public string type { get; private set; }
	//	public gmapObject()
	//	{
	//		type = this.GetType().Name.Substring(3); //geoXxxx => Xxxx

	//	}
	//}
	//public class geoFeature : gmapObject
	//{
	//	public geoFeature()
	//		: base()
	//	{
	//	}
	//	public Dictionary<string, object> properties = new Dictionary<string, object>();
	//	public geoPolygon geometry = new geoPolygon();
	//}
	//public class geoPolygon : gmapObject
	//{
	//	public geoPolygon()
	//		: base()
	//	{
	//	}
	//	public double[][][] coordinates = new double[0][][];
	//}
	//public class geoFeatureCollection : gmapObject
	//{
	//	public geoFeatureCollection()
	//		: base()
	//	{ }

	//	public geoFeature[] features = new geoFeature[0];
	//	public Point center;
	//}
	//#endregion
}

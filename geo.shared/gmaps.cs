using System;
using System.Collections.Generic;
using System.Drawing;

namespace geo.shared
{
	public abstract class gmapObject
	{
		public string type { get; private set; }
		public gmapObject()
		{
			type = this.GetType().Name.Substring(3); //geoXxxx => Xxxx

		}
	}
	public class geoFeature : gmapObject
	{
		public geoFeature()
			: base()
		{
		}
		public Dictionary<string, object> properties = new Dictionary<string, object>();
		public geoPolygon geometry = new geoPolygon();
	}
	public class geoPolygon : gmapObject
	{
		public geoPolygon()
			: base()
		{
		}
		public double[][][] coordinates = new double[0][][];
	}
	public class geoFeatureCollection : gmapObject
	{
		public geoFeatureCollection()
			: base()
		{ }

		public geoFeature[] features = new geoFeature[0];
		public Point center;
	}

	public class geoPoint
	{
		public double Longitude { get; set; }
		public double Latitude { get; set; }

		public geoPoint()
		{
			Longitude = 0;
			Latitude = 0;
		}

		public geoPoint(double lat, double lon)
		{
			this.Latitude = lat;
			this.Longitude = lon;
		}

		public static implicit operator XPointF(geoPoint p) => new XPointF(p.Latitude,p.Longitude);
		public static implicit operator geoPoint(XPointF pt) => new geoPoint(pt.X,pt.Y);
	}

	public class XPointF : Point
	{
		public double X { 
			get => x; 
			set => x=value; 
		}
		public double Y 
		{ get=>y; 
			set=>y=value; 
		}

		public XPointF() { }
		public XPointF(double X, double Y)
		{
			(this.x, this.y) = (X, Y);
		}

		public XPointF(geoPoint gp)
		{
			(x, y) = (gp.Latitude, gp.Longitude);
		}

		public static implicit operator PointF(XPointF xpt) => new PointF((float)xpt.x, (float)xpt.y);
		public static implicit operator XPointF(PointF pt) => new XPointF(pt.X, pt.Y);
	}


}

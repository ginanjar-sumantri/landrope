using System;

namespace PolygonCuttingEar
{
	/// <summary>
	/// Summary description for CPoint2D.
	/// </summary>

	//A point in Coordinate System
	public class CPoint2D
	{
		public double X;
		public double Y;

		public CPoint2D()
		{

		}

		public CPoint2D(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public CSegment2D MakeSegment(CPoint2D next) => new CSegment2D { Pt1 = this, Pt2 = next };
		public CSegment2D MakeRevSegment(CPoint2D prev) => new CSegment2D { Pt1 = prev, Pt2 = this };

		public static bool Equals(CPoint2D Point1, CPoint2D Point2)
			=> ConstantValue.NearZero(Point1.X - Point2.X) &&
					ConstantValue.NearZero(Point1.Y - Point2.Y);

		public bool Equals(CPoint2D newPoint)
			=> Equals(this, newPoint);

		/***To check whether the point is in a line segment***/
		public bool InLine(CSegment seg)
		{
			var Bx = seg.End.X;
			var By = seg.End.Y;
			var Ax = seg.Start.X;
			var Ay = seg.Start.Y;
			var Cx = this.X;
			var Cy = this.Y;

			var xminmax = seg.Xminmax;
			var yminmax = seg.Yminmax;

			return Equals(seg.Start) || Equals(seg.End) || 
								(Cx >= xminmax.min && Cx <= xminmax.max && Cy >= yminmax.min && Cy <= yminmax.max &&
								(Ax - Cx) * (By - Cy) == (Bx - Cx) * (Ay - Cy));
		}

		/*** Distance between two points***/
		public double DistanceTo(CPoint2D point)
		{
			return Math.Sqrt((point.X-this.X)*(point.X-this.X) 
				+ (point.Y-this.Y)*(point.Y-this.Y));

		}

		public (bool inters, bool invtx) IsIntersection((CPoint2D p1, CPoint2D p2) Seg1, (CPoint2D p1, CPoint2D p2) Seg2)
		{
			var de = (Seg2.p2.Y - Seg2.p1.Y) * (Seg1.p2.X - Seg1.p1.X) - (Seg2.p2.X - Seg2.p1.X) * (Seg1.p2.Y - Seg1.p1.Y);
			if (ConstantValue.NearZero(de))  //lines are parallel
				return (false,false);

			var ub = ((Seg1.p2.X - Seg1.p1.X) * (Seg1.p1.Y - Seg2.p1.Y) - (Seg1.p2.Y - Seg1.p1.Y) * (Seg1.p1.X - Seg2.p1.X)) / de;
			bool itx = (ub >= 0) && (ub <= 1);
			bool invx = ub == 0 | ub == 1;
			return (itx, invx);
		}

		public bool PointInsidePolygon(CPoint2D[] polygonVertices)
		{
			if (polygonVertices.Length<3) //not a valid polygon
				return false;
			
			int  nCounter= 0;
			int nPoints = polygonVertices.Length;
			
			CPoint2D s1, p1, p2;
			s1 = this;
			p1= polygonVertices[0];
			
			for (int i= 1; i<nPoints; i++)
			{
				p2= polygonVertices[i % nPoints];
				if (s1.Y > Math.Min(p1.Y, p2.Y))
				{
					if (s1.Y <= Math.Max(p1.Y, p2.Y) )
					{
						if (s1.X <= Math.Max(p1.X, p2.X) )
						{
							if (p1.Y != p2.Y)
							{
								double xInters = (s1.Y - p1.Y) * (p2.X - p1.X) /
									(p2.Y - p1.Y) + p1.X;
								if ((p1.X== p2.X) || (s1.X <= xInters) )
								{
									nCounter ++;
								}
							}  //p1.y != p2.y
						}
					}
				}
				p1 = p2;
			} //for loop
  
			if ((nCounter % 2) == 0) 
				return false;
			else
				return true;
		}

		/*********** Sort points from Xmin->Xmax ******/
		public static void SortPointsByX(CPoint2D[] points)
		{
			if (points.Length>1)
			{
				CPoint2D tempPt;
				for (int i=0; i< points.Length-2; i++)
				{
					for (int j = i+1; j < points.Length -1; j++)
					{
						if (points[i].X > points[j].X)
						{
							tempPt= points[j];
							points[j]=points[i];
							points[i]=tempPt;
						}
					}
				}
			}
		}

		/*********** Sort points from Ymin->Ymax ******/
		public static void SortPointsByY(CPoint2D[] points)
		{
			if (points.Length>1)
			{
				CPoint2D tempPt;
				for (int i=0; i< points.Length-2; i++)
				{
					for (int j = i+1; j < points.Length -1; j++)
					{
						if (points[i].Y > points[j].Y)
						{
							tempPt= points[j];
							points[j]=points[i];
							points[i]=tempPt;
						}
					}
				}
			}
		}

		public CPoint2D Simplified()
		{
			X = Math.Floor(X * 1000d + 0.5d)/1000d;
			Y = Math.Floor(Y * 1000d + 0.5d)/1000d;
			return this;
		}
	}
}

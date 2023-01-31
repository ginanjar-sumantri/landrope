/**************************************************
This unit is used to collect Analytic Geometry formulars
It includes Line, Line segment and CPolygon				
																				
Development by: Frank Shen                                    
Date: 08, 2004                                                         
Modification History:													
* *** **********************************************/

using System;
namespace PolygonCuttingEar
{
	/// <summary>
	///To define a line in the given coordinate system
	///and related calculations
	///Line Equation:ax+by+c=0
	///</summary>
	
	//a Line in 2D coordinate system: ax+by+c=0
	public class CLine
	{
		//line: ax+by+c=0;
		protected double a; 
		protected double b;
		protected double c;
				
		private void Initialize(Double angleInRad, CPoint2D point)
		{
			//angleInRad should be between 0-Pi
			
			try
			{
				//if ((angleInRad<0) ||(angleInRad>Math.PI))
				if (angleInRad>2*Math.PI)
				{
					string errMsg=string.Format(
						"The input line angle" +
						" {0} is wrong. It should be between 0-2*PI.", angleInRad);
				
					InvalidInputGeometryDataException ex=new 
						InvalidInputGeometryDataException(errMsg);

					throw ex;
				}
			
				if (Math.Abs(angleInRad-Math.PI/2)<
					ConstantValue.SmallValue) //vertical line
				{
					a=1;
					b=0;
					c=-point.X;
				}
				else //not vertical line
				{				
					a=-Math.Tan(angleInRad);
					b=1;
					c=-a*point.X-b*point.Y;
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
			}
		}
	
		
		public CLine(Double angleInRad, CPoint2D point)
		{
			Initialize(angleInRad, point);
		}
		
		public CLine(CPoint2D point1, CPoint2D point2)
		{			
			try
			{
				if (CPoint2D.Equals(point1, point2))
				{
					string errMsg="The input points are the same";
					InvalidInputGeometryDataException ex=new 
						InvalidInputGeometryDataException(errMsg);
					throw ex;	
				}			

				//Point1 and Point2 are different points:
				if (Math.Abs(point1.X-point2.X)
					<ConstantValue.SmallValue) //vertical line
				{
					Initialize(Math.PI/2, point1);
				}
				else if (Math.Abs(point1.Y-point2.Y)
					<ConstantValue.SmallValue) //Horizontal line
				{
					Initialize(0, point1);
				}
				else //normal line
				{
					double m=(point2.Y-point1.Y)/(point2.X-point1.X);
					double alphaInRad=Math.Atan(m);
					Initialize(alphaInRad, point1);
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
			}
		}

		public CLine(CLine copiedLine)
		{
			this.a=copiedLine.a; 
			this.b=copiedLine.b;
			this.c=copiedLine.c;
		}

		/*** calculate the distance from a given point to the line ***/ 
		public double GetDistance(CPoint2D point)
		{
			double x0=point.X;
			double y0=point.Y;

			double d=Math.Abs(a*x0+b*y0+c);
			d=d/(Math.Sqrt(a*a+b*b));
			
			return d;			
		}

		/*** point(x, y) in the line, based on y, calculate x ***/ 
		public double GetX(double y)
		{
			//if the line is a horizontal line (a=0), it will return a NaN:
			double x;
			try
			{
				if (Math.Abs(a)<ConstantValue.SmallValue) //a=0;
				{
					throw new NonValidReturnException();
				}
				
				x=-(b*y+c)/a;
			}
			catch (Exception e)  //Horizontal line a=0;
			{
				x=System.Double.NaN;
				System.Diagnostics.Trace.
					WriteLine(e.Message+e.StackTrace);
			}
				
			return x;
		}
		
		/*** point(x, y) in the line, based on x, calculate y ***/ 
		public double GetY(double x)
		{
			//if the line is a vertical line, it will return a NaN:
			double y;
			try
			{
				if (Math.Abs(b)<ConstantValue.SmallValue)
				{
					throw new NonValidReturnException();
				}
				y=-(a*x+c)/b;
			}
			catch (Exception e)
			{
				y=System.Double.NaN;
				System.Diagnostics.Trace.
					WriteLine(e.Message+e.StackTrace);
			}
			return y;
		}
		
		/*** is it a vertical line:***/
		public bool VerticalLine()
		{
			if (Math.Abs(b-0)<ConstantValue.SmallValue)
				return true;
			else
				return false;
		}
		
		/*** is it a horizontal line:***/
		public bool HorizontalLine()
		{
			if (Math.Abs(a-0)<ConstantValue.SmallValue)
				return true;
			else
				return false;
		}

		/*** calculate line angle in radian: ***/
		public double GetLineAngle()
		{
			if (b==0)
			{
				return Math.PI/2;
			}
			else //b!=0
			{
				double tanA=-a/b;
				return Math.Atan(tanA);
			}			
		}

		public bool Parallel(CLine line)
		{
			bool bParallel=false;
			if (this.a/this.b==line.a/line.b)
				bParallel=true;

			return bParallel;
		}

		/**************************************
		 Calculate intersection point of two lines
		 if two lines are parallel, return null
		 * ************************************/
		public CPoint2D IntersecctionWith(CLine line)
		{
			CPoint2D point=new CPoint2D();
			double a1=this.a;
			double b1=this.b;
			double c1=this.c;

			double a2=line.a;
			double b2=line.b;
			double c2=line.c;

			if (!(this.Parallel(line))) //not parallen
			{
				point.X=(c2*b1-c1*b2)/(a1*b2-a2*b1);
				point.Y=(a1*c2-c1*a2)/(a2*b2-a1*b2);
			}
			return point;
  		}
	}

	public class CSegment : CLine
	{
		//line: ax+by+c=0, with start point and end point
		//direction from start point ->end point
		public CPoint2D Start;
		public CPoint2D End;

		public CSegment(CPoint2D startPoint, CPoint2D endPoint)
			: base(startPoint,endPoint)
		{
			this.Start=startPoint;
			this.End= endPoint;
		}

		/*** chagne the line's direction ***/
		public void ChangeLineDirection()
		{
			CPoint2D tempPt;
			tempPt=this.Start;
			this.Start=this.End;
			this.End=tempPt;
		}

		/*** To calculate the line segment length:   ***/
		public double Length
		{
			get
			{
				var a = End.X - Start.X;
				var b = End.Y - Start.Y;
				return Math.Sqrt(a * a + b * b);
			}
		}

		/********************************************************** 
			Get point location, using windows coordinate system: 
			y-axes points down.
			Return Value:
			-1:point at the left of the line (or above the line if the line is horizontal)
			 0: point in the line segment or in the line segment 's extension
			 1: point at right of the line (or below the line if the line is horizontal)    
		 ***********************************************************/
		public int GetPointLocation(CPoint2D point)
		{
			double Ax, Ay, Bx, By, Cx, Cy;
			Bx=End.X;
			By=End.Y;
			  
			Ax=Start.X;
			Ay=Start.Y;
			  
			Cx=point.X;
			Cy=point.Y;
			
			if (this.HorizontalLine())
			{
				if (Math.Abs(Ay-Cy)<ConstantValue.SmallValue) //equal
					return 0;
				else if (Ay > Cy)
					return -1;   //Y Axis points down, point is above the line
				else //Ay<Cy
					return 1;    //Y Axis points down, point is below the line
			}
			else //Not a horizontal line
			{
				//make the line direction bottom->up
				if (End.Y>Start.Y)
					this.ChangeLineDirection();

				double L=this.Length;
				double s=((Ay-Cy)*(Bx-Ax)-(Ax-Cx)*(By-Ay))/(L*L);
				 
				//Note: the Y axis is pointing down:
				if (Math.Abs(s-0)<ConstantValue.SmallValue) //s=0
					return 0; //point is in the line or line extension
				else if (s>0) 
					return -1; //point is left of line or above the horizontal line
				else //s<0
					return 1;
			}
		}

		public (double min, double max) Xminmax => Start.X < End.X ? (Start.X, End.X) : (End.X, Start.X);
		public (double min, double max) Yminmax => Start.Y < End.Y ? (Start.Y, End.Y) : (End.Y, Start.Y);


		/***Check whether this line is in a longer line***/
		public bool InLine(CSegment longerLineSegment) =>
			Start.InLine(longerLineSegment) && End.InLine(longerLineSegment);

		/************************************************
		 * Offset the line segment to generate a new line segment
		 * If the offset direction is along the x-axis or y-axis, 
		 * Parameter is true, other wise it is false
		 * ***********************************************/
		public CSegment OffsetLine(double distance, bool rightOrDown)
		{
			//offset a line with a given distance, generate a new line
			//rightOrDown=true means offset to x incress direction,
			// if the line is horizontal, offset to y incress direction
  
			CSegment line;
			CPoint2D newStartPoint=new CPoint2D();
			CPoint2D newEndPoint=new CPoint2D();
			
			double alphaInRad= this.GetLineAngle(); // 0-PI
			if (rightOrDown)
			{
				if (this.HorizontalLine()) //offset to y+ direction
				{
					newStartPoint.X =this.Start.X;
					newStartPoint.Y=this.Start.Y + distance;

					newEndPoint.X =this.End.X;
					newEndPoint.Y=this.End.Y + distance;
					line=new CSegment(newStartPoint,newEndPoint);
				}
				else //offset to x+ direction
				{
					if (Math.Sin(alphaInRad)>0)  
					{
						newStartPoint.X=Start.X + Math.Abs(distance*Math.Sin(alphaInRad));
						newStartPoint.Y=Start.Y - Math.Abs(distance* Math.Cos(alphaInRad)) ;
						
						newEndPoint.X=End.X + Math.Abs(distance*Math.Sin(alphaInRad));
						newEndPoint.Y=End.Y - Math.Abs(distance* Math.Cos(alphaInRad)) ;
					
						line= new CSegment(
									   newStartPoint, newEndPoint);
					}
					else //sin(FalphaInRad)<0
					{
						newStartPoint.X=Start.X + Math.Abs(distance*Math.Sin(alphaInRad));
						newStartPoint.Y=Start.Y + Math.Abs(distance* Math.Cos(alphaInRad)) ;
						newEndPoint.X=End.X + Math.Abs(distance*Math.Sin(alphaInRad));
						newEndPoint.Y=End.Y + Math.Abs(distance* Math.Cos(alphaInRad)) ;

						line=new CSegment(
							newStartPoint, newEndPoint);
					}
				} 
			}//{rightOrDown}
			else //leftOrUp
			{
				if (this.HorizontalLine()) //offset to y directin
				{
					newStartPoint.X=Start.X;
					newStartPoint.Y=Start.Y - distance;

					newEndPoint.X=End.X;
					newEndPoint.Y=End.Y - distance;
					line=new CSegment(
						newStartPoint, newEndPoint);
				}
				else //offset to x directin
				{
					if (Math.Sin(alphaInRad)>=0)
					{
						newStartPoint.X=Start.X - Math.Abs(distance*Math.Sin(alphaInRad));
						newStartPoint.Y=Start.Y + Math.Abs(distance* Math.Cos(alphaInRad)) ;
						newEndPoint.X=End.X - Math.Abs(distance*Math.Sin(alphaInRad));
						newEndPoint.Y=End.Y + Math.Abs(distance* Math.Cos(alphaInRad)) ;
                        
						line=new CSegment(
							newStartPoint, newEndPoint);
					}
					else //sin(FalphaInRad)<0
					{
						newStartPoint.X=Start.X - Math.Abs(distance*Math.Sin(alphaInRad));
						newStartPoint.Y=Start.Y - Math.Abs(distance* Math.Cos(alphaInRad)) ;
						newEndPoint.X=End.X - Math.Abs(distance*Math.Sin(alphaInRad));
						newEndPoint.Y=End.Y - Math.Abs(distance* Math.Cos(alphaInRad)) ;
                            
						line=new CSegment(
							newStartPoint, newEndPoint);
					}
				}				
			}
			return line;	
		}

		/********************************************************
		To check whether 2 lines segments have an intersection
		*********************************************************/
		public  bool IntersectedWith(CSegment line)
		{
			double x1=this.Start.X;
			double y1=this.Start.Y;
			double x2=this.End.X;
			double y2=this.End.Y;
			double x3=line.Start.X;
			double y3=line.Start.Y;
			double x4=line.End.X;
			double y4=line.End.Y;

			double de=(y4-y3)*(x2-x1)-(x4-x3)*(y2-y1);
			double ua=((x4-x3)*(y1-y3)-(y4-y3)*(x1-x3))/de;
			double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3))/de;

			return (Math.Abs(de-0)<ConstantValue.SmallValue) && (ub > 0) && (ub < 1);
		}
		
	}
}

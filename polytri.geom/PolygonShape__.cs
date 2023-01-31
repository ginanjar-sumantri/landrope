using System;
using System.Collections.Generic;
using System.Linq;

namespace PolygonCuttingEar
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class PolygonShape
	{
		private CPoint2D[] InputVertices;
		private CPoint2D[] ManagedVertices;

		public List<Triangle> Triangles = new List<Triangle>();
		//public CPoint2D[][] Polygons;

		//public int TriangleLen
		//{
		//	get
		//	{
		//		return Polygons.Length;
		//	}
		//}

		//public CPoint2D[] Polygons(int index)
		//{
		//	if (index< m_aPolygons.Length)
		//		return m_aPolygons[index];
		//	else
		//		return null;
		//}

		public PolygonShape(IEnumerable<CPoint2D> vertices)
		{
			int nVertices=vertices.Count();
			if (nVertices<3)
			{
				System.Diagnostics.Trace.WriteLine("To make a polygon, "
					+ " at least 3 points are required!");
				return;
			}

			//initalize the 2D points
			InputVertices= vertices.ToArray();
          
			//make a working copy,  m_aUpdatedPolygonVertices are
			//in count clock direction from user view
			UpdateVertices();
		}

		/****************************************************
		To fill m_aUpdatedPolygonVertices array with input array.
		
		m_aUpdatedPolygonVertices is a working array that will 
		be updated when an ear is cut till m_aUpdatedPolygonVertices
		makes triangle (a convex polygon).
	   ******************************************************/
		private void UpdateVertices()
		{
			ManagedVertices = InputVertices.ToList().ToArray();

			//m_aUpdatedPolygonVertices should be in count clock wise
			if (CPolygon.PointsDirection(ManagedVertices)==PolygonDirection.Clockwise)
				CPolygon.ReversePointsDirection(ManagedVertices);
		}

		/**********************************************************
		To check the Pt is in the Triangle or not.
		If the Pt is in the line or is a vertex, then return true.
		If the Pt is out of the Triangle, then return false.

		This method is used for triangle only.
		***********************************************************/
		private bool TriangleContainsPoint(CPoint2D[] trianglePts, CPoint2D pt)
		{
			if (trianglePts.Length!=3)
				return false;
 
			for (int i=trianglePts.GetLowerBound(0); 
				i<trianglePts.GetUpperBound(0); i++)
			{
				if (pt.EqualsPoint(trianglePts[i]))
					return true;
			}
			
			bool bIn=false;

			CSegment line0=new CSegment(trianglePts[0],trianglePts[1]);
			CSegment line1=new CSegment(trianglePts[1],trianglePts[2]);
			CSegment line2=new CSegment(trianglePts[2],trianglePts[0]);

			if (pt.InLine(line0)||pt.InLine(line1)
				||pt.InLine(line2))
				bIn=true;
			else //point is not in the lines
			{
				double dblArea0=CPolygon.PolygonArea(new CPoint2D[]
			{trianglePts[0],trianglePts[1], pt});
				double dblArea1=CPolygon.PolygonArea(new CPoint2D[]
			{trianglePts[1],trianglePts[2], pt});
				double dblArea2=CPolygon.PolygonArea(new CPoint2D[]
			{trianglePts[2],trianglePts[0], pt});

				if (dblArea0>0)
				{
					if ((dblArea1 >0) &&(dblArea2>0))
						bIn=true;
				}
				else if (dblArea0<0)
				{
					if ((dblArea1 < 0) && (dblArea2< 0))
						bIn=true;
				}
			}				
			return bIn;			
		}

		
		/****************************************************************
		To check whether the Vertex is an ear or not based updated Polygon vertices

		ref. www-cgrl.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian
		/algorithm1.html

		If it is an ear, return true,
		If it is not an ear, return false;
		*****************************************************************/
		private bool IsEarOfUpdatedPolygon(CPoint2D vertex )		
		{
			CPolygon polygon=new CPolygon(ManagedVertices);

			if (polygon.PolygonVertex(vertex))
			{
				bool bEar=true;
				if (polygon.PolygonVertexType(vertex)==VertexType.ConvexPoint)
				{
					CPoint2D pi=vertex;
					CPoint2D pj=polygon.PreviousPoint(vertex); //previous vertex
					CPoint2D pk=polygon.NextPoint(vertex);//next vertex

					for (int i=ManagedVertices.GetLowerBound(0);
						i<ManagedVertices.GetUpperBound(0); i++)
					{
						CPoint2D pt = ManagedVertices[i];
						if ( !(pt.EqualsPoint(pi)|| pt.EqualsPoint(pj)||pt.EqualsPoint(pk)))
						{
							bEar = !(TriangleContainsPoint(new CPoint2D[] { pj, pi, pk }, pt));
						}
					}
				} //ThePolygon.getVertexType(Vertex)=ConvexPt
				else  //concave point
					bEar=false; //not an ear/
				return bEar;
			}
			else //not a polygon vertex;
			{
				System.Diagnostics.Trace.WriteLine("IsEarOfUpdatedPolygon: "+
					"Not a polygon vertex");
				return false;
			}
		}

		/****************************************************
		Set up m_aPolygons:
		add ears and been cut Polygon togather
		****************************************************/
		//private void SetPolygons()
		//{
		//	Polygons = Triangles.ToArray();
		//}

		/********************************************************
		To update m_aUpdatedPolygonVertices:
		Take out Vertex from m_aUpdatedPolygonVertices array, add 3 points
		to the m_aEars
		**********************************************************/
		private void UpdateTriangleVertices(CPoint2D vertex)
		{
			System.Collections.ArrayList alTempPts=new System.Collections.ArrayList(); 

			for (int i=0; i< ManagedVertices.Length; i++)
			{				
				if (vertex.EqualsPoint(
					ManagedVertices[i])) //add 3 pts to FEars
				{ 
					CPolygon polygon=new CPolygon(ManagedVertices);
					var aEar = new[] { polygon.PreviousPoint(vertex), vertex, polygon.NextPoint(vertex)};
					Triangles.Add(Triangle.Create(aEar));
				}
				else	
				{
					alTempPts.Add(ManagedVertices[i]);
				} //not equal points
			}
			
			if  (ManagedVertices.Length 
				- alTempPts.Count==1)
			{
				int nLength=ManagedVertices.Length;
				ManagedVertices=new CPoint2D[nLength-1];
        
				for (int  i=0; i<alTempPts.Count; i++)
					ManagedVertices[i]=(CPoint2D)alTempPts[i];
			}
		}
        

		/*******************************************************
		To cut an ear from polygon to make ears and an updated polygon:
		*******************************************************/
		public void CutEar()
		{
			Triangles.Clear();

			CPolygon polygon=new CPolygon(ManagedVertices);
			bool bFinish=false;

			//if (polygon.GetPolygonType()==PolygonType.Convex) //don't have to cut ear
			//	bFinish=true;

			if (ManagedVertices.Length==3) //triangle, don't have to cut ear
				bFinish=true;
			
			CPoint2D pt=new CPoint2D();
			while (bFinish==false) //UpdatedPolygon
			{
				int i=0;
				bool bNotFound=true;
				while (bNotFound 
					&& (i<ManagedVertices.Length)) //loop till find an ear
				{
					pt=ManagedVertices[i];
					if (IsEarOfUpdatedPolygon(pt))
						bNotFound=false; //got one, pt is an ear
					else
						i++;
				} //bNotFount
				//An ear found:}
				if (pt !=null)
					UpdateTriangleVertices(pt);
       
				//polygon=new CPolygon(UpdatedTriVerts);
				//if ((polygon.GetPolygonType()==PolygonType.Convex)
				//	&& (m_aUpdatedPolygonVertices.Length==3))
				if (ManagedVertices.Length==3)
					bFinish=true;
			} //bFinish=false
			//SetPolygons();
		}		
	}

	public class Triangle : List<CPoint2D>
	{
		public static Triangle Create(IEnumerable<CPoint2D> vertices)
			=> vertices.Count() != 3 ? null : new Triangle(vertices);
		protected Triangle(IEnumerable<CPoint2D> vertices)
		{
			if (vertices.Count() != 3)
				return;
			AddRange(vertices);
		}

		public double Area => Math.Abs((this[0].X * this[1].Y - this[1].X * this[0].Y +
																		this[1].X * this[2].Y - this[2].X * this[1].Y +
																		this[2].X * this[0].Y - this[0].X * this[2].Y) / 2d);

		public CPoint2D Center => new CPoint2D((this[0].X + this[1].X + 2 * this[2].X) / 4d, (this[0].Y + this[1].Y + 2 * this[2].Y) / 4d);
	}
}

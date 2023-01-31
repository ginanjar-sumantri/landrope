using cutear;
using System;
using System.Collections.Generic;
using System.Linq;
using Tracer;

namespace PolygonCuttingEar
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class PolygonShape
	{
		private CPoint2D[] InputVertices;
		private List<CPoint2D> OperationVertices;
		private List<CPoint2D> OriginalVertices;

		private List<CPoint2D[]> Ears = new List<CPoint2D[]>();
		public  List<Triangle> Triangles = new List<Triangle>();

		//public int NumberOfPolygons
		//{
		//	get
		//	{
		//		return Triangles.Length;
		//	}
		//}

		//public CPoint2D[] Polygons(int index)
		//{
		//	if (index< Triangles.Length)
		//		return Triangles[index];
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
			InputVertices=vertices.ToArray();
			//make a working copy,  m_aUpdatedPolygonVertices are
			//in count clock direction from user view
			SetUpdatedPolygonVertices();
		}

		/****************************************************
		To fill m_aUpdatedPolygonVertices array with input array.
		
		m_aUpdatedPolygonVertices is a working array that will 
		be updated when an ear is cut till m_aUpdatedPolygonVertices
		makes triangle (a convex polygon).
	   ******************************************************/
		private void SetUpdatedPolygonVertices()
		{
			OperationVertices = InputVertices.ToList();
			
			//m_aUpdatedPolygonVertices should be in count clock wise
			if (CPolygon.getWise(OperationVertices)==PolygonWise.Clockwise)
				OperationVertices.Reverse();
			OriginalVertices = OperationVertices.ToArray().ToList();
		}

		/**********************************************************
		To check the Pt is in the Triangle or not.
		If the Pt is in the line or is a vertex, then return true.
		If the Pt is out of the Triangle, then return false.

		This method is used for triangle only.
		***********************************************************/
		//private bool TriangleContainsPoint(CPoint2D[] trianglePts, CPoint2D pt)
		//{
		//	if (trianglePts.Length!=3)
		//		return false;
 
		//	for (int i=trianglePts.GetLowerBound(0); 
		//		i<trianglePts.GetUpperBound(0); i++)
		//	{
		//		if (pt.Equals(trianglePts[i]))
		//			return true;
		//	}
			
		//	bool bIn=false;

		//	CSegment line0=new CSegment(trianglePts[0],trianglePts[1]);
		//	CSegment line1=new CSegment(trianglePts[1],trianglePts[2]);
		//	CSegment line2=new CSegment(trianglePts[2],trianglePts[0]);

		//	if (pt.InLine(line0)||pt.InLine(line1)
		//		||pt.InLine(line2))
		//		bIn=true;
		//	else //point is not in the lines
		//	{
		//		double dblArea0=CPolygon.PolygonArea(new CPoint2D[] {trianglePts[0],trianglePts[1], pt});
		//		double dblArea1=CPolygon.PolygonArea(new CPoint2D[] {trianglePts[1],trianglePts[2], pt});
		//		double dblArea2=CPolygon.PolygonArea(new CPoint2D[] {trianglePts[2],trianglePts[0], pt});

		//		if (dblArea0>0)
		//		{
		//			if ((dblArea1 >0) &&(dblArea2>0))
		//				bIn=true;
		//		}
		//		else if (dblArea0<0)
		//		{
		//			if ((dblArea1 < 0) && (dblArea2< 0))
		//				bIn=true;
		//		}
		//	}				
		//	return bIn;			
		//}

		
		/****************************************************************
		To check whether the Vertex is an ear or not based updated Polygon vertices

		ref. www-cgrl.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian
		/algorithm1.html

		If it is an ear, return true,
		If it is not an ear, return false;
		*****************************************************************/
		private bool IsEar(CPoint2D vertex )		
		{
			CPolygon polygon=new CPolygon(OperationVertices);

			if (polygon.IsOwned(vertex))
			{
				bool bEar=true;
				if (polygon.GetVertexType(vertex)==VertexType.ConvexPoint)
				{
					CPoint2D pi=vertex;
					CPoint2D pj=polygon.PreviousPoint(vertex); //previous vertex
					CPoint2D pk=polygon.NextPoint(vertex);//next vertex

					for (int i=0;i<OperationVertices.Count; i++)
					{
						CPoint2D pt = OperationVertices[i];
						if ( !(pt.Equals(pi)|| pt.Equals(pj)||pt.Equals(pk)))
						{
							if (Triangle.Contains(new CPoint2D[] {pj, pi, pk}, pt))
								bEar=false;
						}
					}
				} //ThePolygon.getVertexType(Vertex)=ConvexPt
				else  //concave point
					bEar=false; //not an ear/
				return bEar;
			}
			else //not a polygon vertex;
			{
				MyTracer.TraceWarning2("Not a polygon vertex");
				return false;
			}
		}

		/****************************************************
		Set up m_aPolygons:
		add ears and been cut Polygon togather
		****************************************************/
		private void SetTriangles()
		{
			//int nPolygon=Ears.Count; //ears plus updated polygon
			//var tri=new CPoint2D[nPolygon][];

			//for (int i=0; i<nPolygon; i++) //add ears
			//{
			//	CPoint2D[] points=(CPoint2D[])Ears[i];
			//	tri[i]=points;
			//}
				
			//add UpdatedPolygon:
			//tri[nPolygon-1]=new 
			//	CPoint2D[OperationVertices.Count];

			//for (int i=0; i<OperationVertices.Count;i++)
			//{
			//	tri[nPolygon-1][i] = OperationVertices[i];
			//}

			Triangles = Ears.Select(t => Triangle.Create(t)).ToList();
		}

		/********************************************************
		To update m_aUpdatedPolygonVertices:
		Take out Vertex from m_aUpdatedPolygonVertices array, add 3 points
		to the m_aEars
		**********************************************************/
		private void UpdateVertices(CPoint2D vertex)
		{
			var llist = new LinkedList<CPoint2D>(OperationVertices);
			var node = llist.Find(vertex);
			AddEar((node.Previous ?? llist.Last).Value, vertex, (node.Next ?? llist.First).Value);
			//Ears.Add(new[] { (node.Previous??llist.Last).Value, vertex, (node.Next??llist.First).Value });
			//CPolygon polygon = new CPolygon(OperationVertices);
			//var aEar = new[] { polygon.PreviousPoint(vertex), vertex, polygon.NextPoint(vertex) };
			//Ears.Add(aEar);
			OperationVertices.Remove(vertex);
		}

		void AddEar(params CPoint2D[] points)
		{
			Ears.Add(points);
		}
        

		/*******************************************************
		To cut an ear from polygon to make ears and an updated polygon:
		*******************************************************/
		public void CutEar()
		{
			try
			{
				Ears.Clear();
				CPolygon polygon = new CPolygon(OperationVertices);

				//if (polygon.GetPolygonType()==PolygonType.Convex) //don't have to cut ear
				//	bFinish=true;

				while (true) //UpdatedPolygon
				{
					if (OperationVertices.Count == 3) //triangle, don't have to cut ear
					{
						AddEar(OperationVertices.ToArray());
						break;
					}
					CPoint2D pt;
					pt = OperationVertices.FirstOrDefault(p => IsEar(p));
					if (pt == null)
					{
						OperationVertices.Reverse();
						pt = OperationVertices.FirstOrDefault(p => IsEar(p));
					}
					if (pt == null)
						pt = OperationVertices.First();
					UpdateVertices(pt);
				}
			}
			finally
			{
				SetTriangles();
			}
		}		
	}
}

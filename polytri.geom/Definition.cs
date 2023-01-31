using System;

namespace PolygonCuttingEar
{
	/// <summary>
	///To define the common types used in 
	///Analytical Geometry calculations.
	/// </summary>

	//To define some constant Values 
	//used for local judgment 
	public struct ConstantValue
	{
		internal const double SmallValue = 1e-5;
		internal const double BigValue = 9999999999;

		internal static bool NearZero(double x) => x >= -SmallValue && x <= SmallValue;
		internal static bool NearZeroPlus(double x) => x <= SmallValue;
	}

	public enum VertexType
	{
		ErrorPoint,
		ConvexPoint,
		ConcavePoint
	}

	public enum PolygonType
	{
		Unknown,
		Convex,
		Concave
	}

	public enum PolygonWise
	{
		Unknown,
		Clockwise,
		AntiClockwise
	}


	public enum PolygonRelation
	{
		Separated = 0,
		Outer = 1,
		Inner = 2,
		Intersecting = 3,
		Undefined = 4
	}

	public enum PointPosition
	{
		Outside = 0,
		OnEdge = 1,
		Inside = 2
	}
}
using PolygonCuttingEar;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace cutear
{
	public class Triangle : List<CPoint2D>
	{
		public static Triangle Create(IEnumerable<CPoint2D> vertices)
			=> vertices.Count() != 3 ? null : new Triangle(vertices);
		public static Triangle Create(params CPoint2D[] vertices)
			=> vertices.Length != 3 ? null : new Triangle(vertices);
		protected Triangle(IEnumerable<CPoint2D> vertices)
		{
			if (vertices.Count() != 3)
				return;
			AddRange(vertices);
		}

		public double Area => Math.Abs((this[0].X * this[1].Y - this[1].X * this[0].Y +
																		this[1].X * this[2].Y - this[2].X * this[1].Y +
																		this[2].X * this[0].Y - this[0].X * this[2].Y) / 2d);

		public CPoint2D Center => new CPoint2D((this[0].X + this[1].X + 2 * this[2].X) / 4, (this[0].Y + this[1].Y + 2 * this[2].Y) / 4);

		public static double GetSignedArea(IEnumerable<CPoint2D> points)
		{
			if (points.Count() < 3)
				return double.NaN;
			var ls2 = points.Skip(1).ToList();
			ls2.Add(points.First());

			return points.Select((p, i) => (p, i)).Join(ls2.Select((p, i) => (p, i)), A => A.i, B => B.i,
													(A, B) => A.p.X * B.p.Y - A.p.Y * B.p.X).Sum() / 2d;
		}

		public static bool Contains(CPoint2D[] vertices, CPoint2D pt)
		{
			if (vertices.Length != 3)
				return false;

			if (vertices.Any(p => p.Equals(pt)))
				return true;

			CSegment line0 = new CSegment(vertices[0], vertices[1]);
			CSegment line1 = new CSegment(vertices[1], vertices[2]);
			CSegment line2 = new CSegment(vertices[2], vertices[0]);

			if (pt.InLine(line0) || pt.InLine(line1) || pt.InLine(line2)) 
				return true;

			double dblArea0 = GetSignedArea(new CPoint2D[] { vertices[0], vertices[1], pt });
			double dblArea1 = GetSignedArea(new CPoint2D[] { vertices[1], vertices[2], pt });
			double dblArea2 = GetSignedArea(new CPoint2D[] { vertices[2], vertices[0], pt });

			if (dblArea0 > 0)
			{
				if ((dblArea1 > 0) && (dblArea2 > 0)) return true;
			}
			else if (dblArea0 < 0)
				if ((dblArea1 < 0) && (dblArea2 < 0)) return true;

			return false;
		}

		List<CSegment2D> _segments = null;
		
		void MakeSegments()
		{
			_segments = new List<CSegment2D>();
			_segments.Add(this[0].MakeSegment(this[1]));
			_segments.Add(this[1].MakeSegment(this[2]));
			_segments.Add(this[2].MakeSegment(this[0]));
		}

		List<CSegment2D> Segments
		{
			get
			{
				if (_segments == null)
					MakeSegments();
				return _segments;
			}
		}

		new public bool Contains(CPoint2D pt)
		{
			if (this.Any(p => p.Equals(pt)))
				return true;

			var inlines = Segments.Select(s => s.InlLine(pt)).Where(b=>b).ToArray();
			if (inlines.Any())
				return true;

			double area0 = GetSignedArea(new CPoint2D[] { this[0], this[1], pt });
			double area1 = GetSignedArea(new CPoint2D[] { this[1], this[2], pt });
			double area2 = GetSignedArea(new CPoint2D[] { this[2], this[0], pt });

			if (area0 > 0)
			{
				if ((area1 > 0) && (area2 > 0)) return true;
			}
			else if (area0 < 0)
				if ((area1 < 0) && (area2 < 0)) return true;

			return false;
		}

		public bool IsVertex(CPoint2D pt) => this.Select(p=>p.Equals(pt)).Any(b=>b);
	}
}

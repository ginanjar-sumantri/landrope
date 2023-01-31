using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

namespace PolygonCuttingEar
{
	public class CSegment2D
	{
		public CPoint2D Pt1;
		public CPoint2D Pt2;

		(double dx, double dy) _delta = default;
		(double a, double b, double c) _coeff = default;
		(double min, double max) _xminmax = default;
		(double min, double max) _yminmax = default;

		void CalcCoeffs()
		{
			_delta = (Pt2.X - Pt1.X, Pt2.Y - Pt1.Y);

			_coeff = (_delta.dy, -_delta.dx, _delta.dy * Pt1.X - _delta.dx * Pt1.Y  );
			_xminmax = (Pt1.X <= Pt2.X ? Pt1.X : Pt2.X, Pt1.X >= Pt2.X ? Pt1.X : Pt2.X);
			_yminmax = (Pt1.Y <= Pt2.Y ? Pt1.Y : Pt2.Y, Pt1.Y >= Pt2.Y ? Pt1.Y : Pt2.Y);
		}

		public (double dx, double dy) Delta 
		{
			get {
				if (_delta == default)
					CalcCoeffs();
				return _delta;
			}
		}

		public (double a, double b, double c) Coeff
		{
			get
			{
				if (_coeff== default)
					CalcCoeffs();
				return _coeff;
			}
		}

		public (double min, double max) Xminmax
		{
			get {
				if (_xminmax == default)
					CalcCoeffs();
				return _xminmax;
			}
		}

		public (double min, double max) Yminmax
		{
			get
			{
				if (_yminmax == default)
					CalcCoeffs();
				return _yminmax;
			}
		}

		public CPoint2D Center => new CPoint2D((Pt1.X + Pt2.X) / 2d, (Pt1.Y + Pt2.Y) / 2d);

		public int Sign(CSegment2D next)
		{
			var thisdelta = Delta;
			var nextdelta = next.Delta;
			var diff = thisdelta.dx * nextdelta.dy - nextdelta.dx * thisdelta.dy;
			return diff > 0 ? 1 : diff < 0 ? -1 : 0;
		}
		public bool IsClockwise(CSegment2D next) => Sign(next) == -1;

		public (bool ix, bool iv) Intersecting(CSegment2D other)
		{
			var coeff = Coeff;
			var ocoeff = other.Coeff;
			var disc = coeff.a * ocoeff.b - ocoeff.a * coeff.b;
			if (ConstantValue.NearZero(disc))
				return (false,false);
			var x = (ocoeff.b * coeff.c - ocoeff.c * coeff.b) / disc;
			var y = (coeff.a * ocoeff.c - ocoeff.a * coeff.c) / disc;

			var oxminmax = other.Xminmax;
			var oyminmax = other.Yminmax;

			if (x >= _xminmax.min && x <= _xminmax.max && y >= _yminmax.min && y <= _yminmax.max  &&
							x >= oxminmax.min && x <= oxminmax.max && y >= oyminmax.min && y <= oyminmax.max)
			{
				var ix = (x == Pt1.X && y == Pt1.Y) || (x == Pt2.X && y == Pt2.Y);
				return (true, ix);
			}
			return (false, false);
		}

		public (double x,double y) Intersection(CSegment2D other)
		{
			var coeff = Coeff;
			var ocoeff = other.Coeff;
			var disc = coeff.a * ocoeff.b - ocoeff.a * coeff.b;
			if (ConstantValue.NearZero(disc))
				return (double.NaN,double.NaN);
			var x = (ocoeff.b * coeff.c - ocoeff.c * coeff.b) / disc;
			var y = (coeff.a * ocoeff.c - ocoeff.a * coeff.c) / disc;

			var oxminmax = other.Xminmax;
			var oyminmax = other.Yminmax;

			return (x >= _xminmax.min && x <= _xminmax.max && y >= _yminmax.min && y <= _yminmax.max) &&
							(x >= oxminmax.min && x <= oxminmax.max && y >= oyminmax.min && y <= oyminmax.max) ?
									(x,y) : (double.NaN,double.NaN);
		}

		internal void Xtend((double x, double y) ext)
		{
			var delta = Delta;
			if (!double.IsNaN(ext.x))
			{
				var y = (ext.x - Pt1.X) * delta.dy / delta.dx + Pt1.Y;
				Pt2 = new CPoint2D(ext.x, y);
			}
			else
			{
				var x = (ext.y - Pt1.Y) * delta.dx / delta.dy + Pt1.X;
				Pt2 = new CPoint2D(x, ext.y);
			}
			CalcCoeffs();
		}

		internal bool InlLine(CPoint2D point)
		{
			var dy1 = point.Y - Pt1.Y;
			var dy2 = Pt2.Y - point.Y;
			var dx1 = point.X - Pt1.X;
			var dx2 = Pt2.X - point.X;

			if (ConstantValue.NearZero(dx1) && ConstantValue.NearZero(dx2))
				return (dy1 >= 0 && dy2 >= 0) || (dy1 < 0 || dy2 < 0);
			if (ConstantValue.NearZero(dy1) && ConstantValue.NearZero(dy2))
				return (dx1 >= 0 && dx2 >= 0) || (dx1 < 0 || dx2 < 0);
			if (dx1 < 0 && dx2 > 0 || dx1 > 0 && dx2 < 0 || dy1 < 0 && dy2 > 0 || dy1 > 0 && dy2 < 0)
				return false;
			return dy1 * dx2 == dy2 * dx1;
		}
	}
}

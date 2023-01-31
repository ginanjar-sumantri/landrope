using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeomHelper
{
	public class fraction
	{
		public double mul;
		public double div;

		public fraction(double mul, double div)
		{
			this.mul = mul;
			this.div = div;
		}
		public fraction(double scalar)
		{
			this.mul = scalar;
			this.div = 1;
		}

		public override bool Equals(object obj)
		{
			return this == (fraction)obj;
		}

		public override int GetHashCode()
		{
			return mul.GetHashCode() ^ div.GetHashCode();
		}

		public static fraction operator *(fraction muldiv, fraction other)
		{
			return new fraction(muldiv.mul * other.mul, muldiv.div * other.div);
		}
		public static fraction operator *(fraction muldiv, double scalar)
		{
			return new fraction(muldiv.mul * scalar, muldiv.div);
		}
		public static fraction operator *(double scalar, fraction muldiv)
		{
			return new fraction(muldiv.mul * scalar, muldiv.div);
		}


		public static fraction operator /(fraction muldiv, fraction other)
		{
			return new fraction(muldiv.mul * other.div, muldiv.div * other.mul);
		}

		public static fraction operator /(fraction muldiv, double scalar)
		{
			return new fraction(muldiv.mul, muldiv.div * scalar);
		}
		public static fraction operator /(double scalar, fraction muldiv)
		{
			return new fraction(muldiv.div * scalar, muldiv.mul);
		}

		public static fraction operator +(fraction muldiv, fraction other)
		{
			return new fraction(muldiv.mul * other.div + muldiv.div * other.mul, muldiv.div * other.div);
		}
		public static fraction operator +(fraction muldiv, double scalar)
		{
			return new fraction(muldiv.mul + scalar * muldiv.div, muldiv.div);
		}
		public static fraction operator +(double scalar, fraction muldiv)
		{
			return new fraction(muldiv.mul + scalar * muldiv.div, muldiv.div);
		}

		public static fraction operator -(fraction muldiv, fraction other)
		{
			return new fraction(muldiv.mul * other.div - muldiv.div * other.mul, muldiv.div * other.div);
		}
		public static fraction operator -(fraction muldiv, double scalar)
		{
			return new fraction(muldiv.mul - scalar * muldiv.div, muldiv.div);
		}
		public static fraction operator -(double scalar, fraction muldiv)
		{
			return new fraction(scalar * muldiv.div - muldiv.mul, muldiv.div);
		}

		public static fraction operator - (fraction muldiv)
		{
			return new fraction(-muldiv.mul, muldiv.div);
		}

		public static bool operator == (fraction muldiv, fraction other)
		{
			return muldiv.mul * other.div == muldiv.div * other.mul;
		}

		public static bool operator !=(fraction muldiv, fraction other)
		{
			return muldiv.mul * other.div != muldiv.div * other.mul;
		}

		public static explicit operator double(fraction muldiv)
		{
			return muldiv.div == 0 ? (muldiv.mul == 0? 1 : 0 > 0? double.PositiveInfinity : double.NegativeInfinity) : muldiv.mul/muldiv.div;
		}

		public static explicit operator fraction(double scalar)
		{
			return new fraction(scalar);
		}
	}


}

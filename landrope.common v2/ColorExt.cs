using System;
using System.Drawing;

namespace landrope.common
{
	public static class ColorExt
	{
		public static Color FromHsl(float H, float S, float L)
		{
			var h = H / 255f;
			var sl = S / 255f;
			var l = L / 255f;

			// default to gray
			float r = l, g = l, b = l;

			float v = (l <= 0.5f) ? (l * (1 + sl)) : (l + sl - l * sl);
			if (v > 0)
			{
				float m = l + l - v;
				float sv = (v - m) / v;
				h *= 6.0f;
				int sextant = (int)h;
				float fract = h - sextant;
				float vsf = v * sv * fract;
				float mid1 = m + vsf;
				float mid2 = v - vsf;

				//(r, g, b) = sextant switch
				//{
				//	0 => (v, mid1, m),
				//	1 => (mid2, v, m),
				//	2 => (m, v, mid1),
				//	3 => (m, mid2, v),
				//	4 => (mid1, m, v),
				//	_ => (v, m, mid2)
				//};
				switch (sextant)
				{
					case	0 : r=v;		g=mid1;		b = m;		break;
					case	1 : r=mid2;	g = v;		b = m;		break;
					case	2 : r=m;		g = v;		b = mid1; break;
					case	3 : r=m;		g = mid2;	b = v;		break;
					case	4 : r=mid1;	g = m;		b = v;		break;
					case	5 : r=v;		g = m;		b = mid2;	break;
				}
			}

			return Color.FromArgb(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
		}

		//public static string ToCss(this Color clr) =>
		//	clr.A == 255 ? $"#{clr.R:x2}{clr.G:x2}{clr.B:x2}" : $"rgba({clr.R},{clr.G},{clr.B},{(decimal)clr.A / 255m})";
	}
}

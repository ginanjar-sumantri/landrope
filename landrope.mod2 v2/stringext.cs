using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
	public static class StringExt
	{
		public static string ToTitle(this string st)
		{
			if (st.Length < 2)
				return st.ToUpper();
			st = st.ToLower();
			return st.Substring(0, 1).ToUpper() + st.Substring(1);
		}
	}
}

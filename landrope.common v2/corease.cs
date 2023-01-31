using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace landrope.common
{
	public class CoreBase
	{
		public CoreBase FromDict(IDictionary<string, object> dict)
		{
			var props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var key in dict.Keys)
			{
				var prop = props.FirstOrDefault(p => p.Name == key);
				if (prop != null)
					prop.SetValue(this, dict[key]);
			}
			return this;
		}

	}
}

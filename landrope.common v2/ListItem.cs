using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class ListItem
	{
		public string value { get; set; }
		public string text { get; set; }
		public ListItemF ToField() => new ListItemF { value = value, text = text };
	}

	public class ListItemF
	{
		public string value;
		public string text;
	}

	public class ListItemEx<T> where T:Enum
	{
		public T value { get; set; }
		public string text { get; set; }
	}
}

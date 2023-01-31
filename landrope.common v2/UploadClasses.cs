using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.common
{
	public class cmnItem
	{
		public string key { get; set; }

		public string name { get; set; }

		public static implicit operator ListItem(cmnItem citem) 
			=> new ListItem { value=citem?.key, text=citem?.name };

		public static IEnumerable<ListItem> ToListItem(IEnumerable<cmnItem> citems)
			=> citems?.Select(c => (ListItem)c).ToList();
	}

	public class NewDataSet
	{
		public IEnumerable<cmnItem> Projects { get; set; }
		public IEnumerable<cmnItem> Desas { get; set; }
		public IEnumerable<cmnItem> Processes { get; set; }
		public IEnumerable<cmnItem> Types { get; set; }
		public string[] DisabledMap { get; set; }
		public string Pemilik { get; set; }
		public string Alashak { get; set; }
		public string NoPeta { get; set; }
		public double? Luas { get; set; }
		public string Group { get; set; }
		public string Alias { get; set; }

		public string StLuas
		{
			get => $"{Luas:0}";
			set
			{
				if(double.TryParse(value, out double dv)) Luas = dv;
			}
		}
	}

	public class UpdateDataSet : NewDataSet
	{
		public string key { get; set; }
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public int en_proses { get; set; }
		public int en_jenis { get; set; }
	}

	public class InfoDataSet
	{
		public string key { get; set; }
		public Dictionary<string, string> values { get; set; }
	}

	public class Mapval
	{
		public string key { get; set; }

		public DateTime uploaded { get; set; }

		public string fileName { get; set; }

		public int filesize { get; set; }

		public DateTime filecreated { get; set; }

		public DateTime fileupdated { get; set; }

		public string fileupdater { get; set; }

		public string uploader { get; set; }
	}
}

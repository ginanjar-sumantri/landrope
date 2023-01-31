using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace landrope.common
{
	public class GridSettings
	{
		public string pg { get; set; }
		public string rpp { get; set; }
		public string sorts { get; set; } // json
		public string oper { get; set; } // add - edit - del
		public string filter { get; set; } // json

		public GridSettings() { }
		public GridSettings(object option)
		{
			var json = JsonConvert.SerializeObject(option);
			var asoption = JsonConvert.DeserializeObject<DataSourceLoadOptions>(json);
			pg = $"{(asoption.Skip / asoption.Take) + 1}";
			rpp = $"{asoption.Take}";
			json = JsonConvert.SerializeObject(asoption.Filter);
			sorts = asoption.Sort == null ? null : JsonConvert.SerializeObject(asoption.Sort.Select(o => new
			{
				colid = o.Selector,
				sort = o.Desc ? "desc" : "asc"
			})).Replace(@"\""","'").Replace("\"","'");
			if (asoption.Filter == null || asoption.Filter.Count == 0)
				return;
			var newFilter = new List<object>();
			foreach (var obj in asoption.Filter)
			{
				if (obj is JArray)
				{
					var arr = ((JArray)obj).ToObject<object[]>();
					var field = arr[0].ToString();
					var xoper = arr[1].ToString();
					var filter = arr[2].ToString();
					var oper = xoper;
					switch (xoper)
					{
						case "=": oper = "equals"; break;
						case "<>":
						case "!=": oper = "notEqual"; break;
						case "<": oper = "lessThan"; break;
						case "<=": oper = "lessThanOrEqual"; break;
						case ">": oper = "greaterThan"; break;
						case ">=": oper = "greaterThanOrEqual"; break;
					}
					var fil = new { field, oper, filter };
					newFilter.Add(fil);
				}
			}
			filter = JsonConvert.SerializeObject(newFilter).Replace(@"\""", "'").Replace("\"", "'");
		}

		public string ToQueryString() => $"pg={pg}&rpp={rpp}&sorts={sorts}&filter={filter}&oper={oper}";
	}

	internal class DataSourceLoadOptions
	{
		public static bool? StringToLowerDefault { get; set; }
		public string DefaultSort { get; set; }
		public bool? RemoteGrouping { get; set; }
		public bool? RemoteSelect { get; set; }
		public string[] PreSelect { get; set; }
		public string[] Select { get; set; }
		public SummaryInfo[] GroupSummary { get; set; }
		public SummaryInfo[] TotalSummary { get; set; }
		public IList Filter { get; set; }
		public GroupingInfo[] Group { get; set; }
		public SortingInfo[] Sort { get; set; }
		public int Take { get; set; }
		public int Skip { get; set; }
		public bool IsCountQuery { get; set; }
		public bool RequireGroupCount { get; set; }
		public bool RequireTotalCount { get; set; }
		public bool? StringToLower { get; set; }
		public bool? PaginateViaPrimaryKey { get; set; }
	}

	//
	// Summary:
	//     Represents a group or total summary definition.
	internal class SummaryInfo
	{
		public string Selector { get; set; }
		public string SummaryType { get; set; }
	}

	//
	// Summary:
	//     Represents a grouping level to be applied to data.
	internal class GroupingInfo : SortingInfo
	{
		public string GroupInterval { get; set; }
		public bool? IsExpanded { get; set; }
	}

	internal class SortingInfo
	{
		public string Selector { get; set; }
		public bool Desc { get; set; }
	}

}

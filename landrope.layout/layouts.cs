using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynForm.shared;
using System.ComponentModel.Design;
using System.ComponentModel.DataAnnotations;
using landrope.common;
using System;
using MongoDB.Bson;

namespace landrope.layout
{
	public static class LayoutMaster
	{
		public static DynElement[] LoadLayout(string name,string serverpath, string[] rights)
		{
			if (name.EndsWith("`1"))
				name = name.Substring(0, name.Length - 2);
			string[] booleans = new[] { "true", "false" };
			string fname = $"{name}.dfl";
			string path = Path.Combine(Path.Combine(serverpath, "layouts"),fname);
			var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			var fr = new StreamReader(fs);
			var json = fr.ReadToEnd();
			fr.Close();
			fs.Close();
			if (string.IsNullOrWhiteSpace(json))
				return new DynElement[0];
			var layout= JsonConvert.DeserializeObject<List<DynElement>>(json);
			layout.ForEach(
				el => {
					if (!booleans.Contains(el.visible?.ToLower()??""))
					{
						if (rights==null || !rights.Any())
							el.visible = "false";
						else
						{
							var rgs = (el.visible?.ToUpper() ?? "").Split(',', '|', ';');
							var itsx = rights.Intersect(rgs);
							el.visible = itsx.Any() ? "true" : "false";
						}
					}
					if (!booleans.Contains(el.editable?.ToLower() ?? ""))
					{
						if (rights == null || !rights.Any())
							el.editable = "false";
						else
						{
							var rgs = (el.editable?.ToUpper() ?? "").Split(',', '|', ';');
							var itsx = rights.Intersect(rgs);
							el.editable = itsx.Any() ? "true" : "false";
						}
					}
				});
			return layout.ToArray();
		}
	
	}

}

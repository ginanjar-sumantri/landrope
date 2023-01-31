using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class DocRequestParam
	{
		public string key { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string step
		{
			get => _step.ToString("g");
			set { _step = Enum.TryParse<DocProcessStep>(value, out DocProcessStep stp) ? stp : DocProcessStep.Belum_Bebas; }
		}
		public DocProcessStep _step { get; set; }
		public string keyDocType { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string propkey
		{
			get => metakey.ToString("g");
			set { metakey = Enum.TryParse<MetadataKey>(value, out MetadataKey mk) ? mk : MetadataKey.Lainnya; }
		}

		public MetadataKey metakey { get; set; }

		public string value { get; set; }
		public Existence exis { get; set; }
		public Existence sec_exis { get; set; }
	}
}

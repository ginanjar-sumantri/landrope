using landrope.documents;
using landrope.common;
using landrope.mod3;
using Microsoft.OpenApi.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace landrope.api2.Controllers
{
	internal class DocWithProps
	{
		public string keyDT { get; set; }
		public string identifier { get; set; }
		public ResultMeta[] props { get; set; }

		public DocWithProps(string keyDT, string identifier, ResultMeta[] props)
		{
			this.keyDT = keyDT;
			this.identifier = identifier;
			this.props = props;
		}

		public static IEnumerable<DocWithProps> Templates(DocProcessStep step, string disc)
		{
			var allprops = StepDocType.GetItem(step, disc)?.receive.Join(DocType.List.Where(d => d.invalid != true), t => t.keyDocType, d => d.key,
						(t, d) => (t.keyDocType, d.identifier, props: d.metadata));
			return allprops.Select(d => new DocWithProps(d.keyDocType, d.identifier,
							d.props.Select(p => new ResultMeta { mkey = p.key, val = null }).ToArray())).ToArray();
		}
	}
}
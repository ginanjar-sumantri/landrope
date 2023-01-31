using landrope.mod3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.bundhost
{
	public class MainBundleMem 
	{
		public MainBundle source { get; set; }
		public List<BundledDocMem> docs { get; set; } = new List<BundledDocMem>();

		public MainBundleMem(MainBundle source)
		{
			this.source = source;
			docs = source.doclist.Select(d=>new BundledDocMem(d)).ToList();
		}
	}
}

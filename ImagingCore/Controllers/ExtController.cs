using iText.Kernel.Pdf;
using landrope.common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImagingCore.Controllers
{
	[ApiController]
	[Route("/ext")]

	//[EnableCors(nameof(landrope))]
	public class ExtController : ControllerBase
	{
		[HttpGet("scan")]
		public IActionResult Scan(string body)
		{
/*			var obj = Secured.decrypt<DmsExtScan>(body);
			if (obj == null)
				return UnprocessableEntity();
*/			var doc = Program.mainform.ShowAndScan();
			if (doc==null)
				return NoContent();
			//doc.GetWriter().get;
			var pstrm = new PdfStream(doc,);

			var strm = doc.GetReader().ReadStream(;
			var bin = new byte[strm.Length];
			strm.Read(bin, 0, bin.Length);
			var ret = new DmsExtScanResult { document64 = Convert.ToBase64String(bin) };
			return Ok(ret.encrypt());
		}
	}
}

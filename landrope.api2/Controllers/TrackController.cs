using landrope.api2.Models;
using landrope.hosts;
using landrope.mod2;
using landrope.mod3;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;

namespace landrope.api2.Controllers
{
	[Route("/api/track")]
	[NeedToken("REP_VIEW")]
	public class TrackController : Controller
	{
		ExtLandropeContext contextex = Contextual.GetContextExt();
		mod3.LandropePlusContext contextplus = Contextual.GetContextPlus();
		[HttpGet("list")]
		public IActionResult List(string token, string keyProject, string keyDesa)
		{
			//try
			//{
			//	var user = contextex.FindUser(token);
			//	var bhost = HostServicesHelper.GetBundleHost(ControllerContext.HttpContext.RequestServices);
			//	var last = bhost.LastPositionDiscrete();
			//	var qry = last.AsQueryable();
			//	if (keyDesa != null)
			//		qry = qry.Where(l => l.keyDesa == keyDesa);
			//	else if (keyProject!=null)
			//		qry = qry.Where(l => l.keyProject == keyProject);
			//	return Ok(qry.ToArray());
			//}
			//catch (UnauthorizedAccessException exa)
			//{
			//	return new ContentResult { StatusCode = int.Parse(exa.Message) };
			//}
			//catch (Exception ex)
			//{
			//	MyTracer.TraceError2(ex);
			//	return new UnprocessableEntityObjectResult($"Gagal mendata status bidang, harap beritahu support");
			//}
			throw new NotImplementedException();
		}
	}
}

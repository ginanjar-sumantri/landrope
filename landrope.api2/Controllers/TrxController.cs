using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using landrope.api2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using landrope.mod3;
using landrope.mod2;
using auth.mod;
using landrope.hosts.old;
using landrope.mod3.classes;
using GenWorkflow;
using landrope.common;
using flow.common;
using APIGrid;

namespace landrope.api2.Controllers
{
	[Route("api/trx-doc")]
	[ApiController]
	[EnableCors(nameof(landrope))]
	public class TrxController : ControllerBase
	{
		mod3.LandropePlusContext contextplus = Contextual.GetContextPlus();
		ExtLandropeContext contextex = Contextual.GetContextExt();
		authEntities authcontext = Contextual.GetAuthContext();

		[NeedToken("ADMIN_LAND1, ADMIN_LAND2, ARCHIVE_FULL, ARCHIVE_REVIEW")]
		[HttpGet("list")]
		public IActionResult GetList(string token, [FromQuery] AgGridSettings gs)
		{
			try
			{
				var user = contextex.FindUser(token);
				var privs = user.privileges.Select(a => a.identifier).Intersect(new[] { "ARCHIVE_FULL", "ARCHIVE_REVIEW" }).ToList();
				var users = authcontext.users.Query(u => u.invalid != null).ToArray();
				var host = HostServicesHelper.GetBatchHost(ControllerContext.HttpContext.RequestServices);
				var batches = host.ListActive().Cast<trxBatch>().Select(b => (b, b.Instance));
				var bloops = batches.Select(x => (x.b, ix:x.Instance.DoFindJobX(privs,null)));
				var blkeys = bloops.Select(x => x.b.key);
				var bowns = batches.Where(x => x.b.keyCreator == user.key).Select(x=>x.b);
				bowns = bowns.Where(x => !blkeys.Contains(x.key));
				var bres = bloops.Union(bowns.Select(b => (b, new (GraphNode, GraphRoute)[0])))
										.SelectMany(x=>x.Item2.Select(r=>(x.b,r)));
				var returns = bres.Select(x => new trxView{ 
					key=x.b.key,
					created=x.b.created,
					creator = users.FirstOrDefault(u=>u.key==x.b.keyCreator)?.identifier,
					durasi = x.b is lendBatch lb ? lb.duration : (int?)null,
					trxref = x.b is returnBatch rb ? host.ListByTrxType(TrxType.Peminjaman).Cast<trxBatch>().FirstOrDefault(b=>b.key == rb.keyReff)?.identifier : (string)null,
					number=x.b.identifier,
					trxtipe=x.b.type.ToString("g"),
					state = x.b.Instance?.lastState?.state??ToDoState.unknown_,
					status = x.b.Instance?.lastState?.state.AsStatus(),
					statustime = x.b.Instance?.lastState?.time,
					keyRoute = x.r.Item2?.key,
					verb = x.r.Item2?._verb??ToDoVerb.unknown_,
					ToDo = x.r.Item2?._verb.Title(),
					cmds = x.r.Item2?.branches?.Select(bb=>bb._control).ToArray()
				});
				return Ok(returns.GridFeed(gs));
			}
			catch (UnauthorizedAccessException exa)
			{
				return exa.Message switch {
					"401"=> new UnauthorizedResult(),
					"501" => new ContentResult { StatusCode = 501 },
					_ => new ContentResult{ StatusCode = 500}
				};
			}
			catch(Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}
	}
}

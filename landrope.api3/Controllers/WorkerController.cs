using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIGrid;
using landrope.common;
using landrope.mod2;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Tracer;

namespace landrope.api3.Controllers
{
    [ApiController]
    [Route("api/worker")]
    public class WorkerController : ControllerBase
    {
        ExtLandropeContext context = Contextual.GetContextExt();

        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token,[FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = context.FindUser(token);
                var workers = context.GetCollections(new WorkerBase(), "workers", "{}", "{_id:0}").ToList();

                var xlst = ExpressionFilter.Evaluate(workers, typeof(List<WorkerBase>), typeof(WorkerBase), gs);
                var data = xlst.result.Cast<WorkerBase>().ToList();

                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("add")]
        public IActionResult AddWorker([FromQuery] string token, [FromBody] WorkerBase Core)
        {
            try
            {
                var user = context.FindUser(token);

                var worker = new Worker();
                worker.FromCore(Core);
                
                context.workers.Insert(worker);
                context.SaveChanges();

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}

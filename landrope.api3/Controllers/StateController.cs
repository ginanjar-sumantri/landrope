using APIGrid;
using GraphConsumer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;
using Microsoft.Extensions.DependencyInjection;
using mongospace;
using landrope.consumers;
using landrope.mod2;
using landrope.mod4;
using landrope.mod3;
using landrope.hosts;
using landrope.common;
using landrope.mod;
using auth.mod;

namespace landrope.api3.Controllers
{
    [Route("api/state")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class StateController : ControllerBase
    {
        IServiceProvider services;
        //GraphContext gcontext;
        GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextes = Contextual.GetContextExt();
        LandropePayContext contextpay = Contextual.GetContextPay();
        LandropePlusContext contextplus = Contextual.GetContextPlus();

        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetReqPersilStateHost(services);
                var states = host.OpenedReqPersilState().Cast<ReqPersilState>().ToArray().AsParallel();

                var view = states.Select(x => x.toView(contextpay));

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<ReqPersilStateView>), typeof(ReqPersilStateView), gs);
                var data = xlst.result.Cast<ReqPersilStateView>().ToArray();

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

        [HttpGet("list/detail")]
        public IActionResult GetListDetail([FromQuery] string token, string key, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextpay.FindUser(token);
                var host = HostServicesHelper.GetReqPersilStateHost(services);
                var states = host.GetReqPersilState(key) as ReqPersilState;

                if (states == null)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (states.details == null)
                    states.details = new ReqPersilStateDtl[0];

                var locations = contextpay.GetVillages().ToArray();

                var view = states.details.Select(x => x.toView(contextpay, locations)).ToArray();

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<ReqPersilStateDtlView>), typeof(ReqPersilStateDtlView), gs);
                var data = xlst.result.Cast<ReqPersilStateDtlView>().ToArray();

                return Ok(data.GridFeed(gs));
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("save")]
        public IActionResult Add([FromQuery] string token, [FromBody] ReqPersilStateCore core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var priv = user.getPrivileges(null).Select(p => p.identifier).ToArray();

                var sudahBebas = priv.Contains("CREATE_EDIT_SB");
                var belumBebas = priv.Contains("CREATE_EDIT_BB");

                var discriminator = sudahBebas == true ? "EDIT_SB" : belumBebas == true ? "EDIT_BB" : null;

                var host = HostServicesHelper.GetReqPersilStateHost(services);

                var ent = new ReqPersilState(user);

                ent.fromCore(core);
                ent.CreateGraphInstance(user, discriminator);

                host.Add(ent);

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("save/detail")]
        public IActionResult AddBidang([FromQuery] string token, string key, [FromBody] string[] pkeys)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetReqPersilStateHost(services);
                var state = host.GetReqPersilState(key) as ReqPersilState;

                if (state == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                foreach (var k in pkeys)
                {
                    if (state.details != null)
                    {
                        if (state.details.Any(d => d.keyPersil == k))
                            continue;
                    }
                    var detail = new ReqPersilStateDtl() { key = mongospace.MongoEntity.MakeKey, keyPersil = k };

                    state.AddDetail(detail);
                }

                host.Update(state);

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        [HttpPost("delete/detail")]
        public IActionResult DeleteBidang([FromQuery] string token, string key, [FromBody] string[] keys)
        {
            try
            {
                var user = contextes.FindUser(token);
                var host = HostServicesHelper.GetReqPersilStateHost(services);
                var state = host.GetReqPersilState(key) as ReqPersilState;

                if (state == null)
                    return new UnprocessableEntityObjectResult("Request tidak ada");

                state.details.ToList().RemoveAll(x => keys.Contains(x.key));

                host.Update(state);

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult($"Gagal: {ex.Message}, harap beritahu support");
            }
        }

        //[HttpGet("persils/list")]
        //public IActionResult GetListPersilForCategory([FromQuery] string token, RequestState opr, [FromQuery] AgGridSettings gs)
        //{
        //    try
        //    {
        //        var user = contextpay.FindUser(token);
        //        var tm0 = DateTime.Now;

        //        var keys = opr switch
        //        {
        //            "kategori" => new object[] { 0, 1, 2, 3, 4, 5, null },
        //            "masuk" => new object[] { 5 },
        //            "batal" => new object[] { 0, 1, 3, 4, 5, null },
        //            "lanjut" => new object[] { 2 },
        //            _ => new object[] { 0, 1, 3, 4, null }
        //        };

        //        //object[] keys = { 0, 1, 3, null };
        //        var prokeys = string.Join(',', keys.Select(k => $"{k}"));
        //        var persils = contextex.GetDocuments(new PersilCore4(), "persils_v2",
        //            $@"<$match:<$and: [<en_state:<$in:[{prokeys}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".Replace("<", "{").Replace(">", "}"),
        //            $@"<$lookup:<from:'maps',let:< key: '$basic.current.keyDesa'>,pipeline:[<$unwind: '$villages'>,<$match: <$expr: <$eq:['$villages.key','$$key']>>>,
        //            <$project: <key: '$villages.key', identity: '$villages.identity'>>], as:'desas'>>".Replace("<", "{").Replace(">", "}"),
        //            $@"<$lookup:<from:'maps', localField:'basic.current.keyProject',foreignField:'key',as:'projects'>>".Replace("<", "{").Replace(">", "}"),
        //            $@"<$project:<key:'$key',IdBidang: '$IdBidang',en_state: '$en_state',
        //            alasHak : '$basic.current.surat.nomor',
        //            noPeta : '$basic.current.noPeta',
        //            states:
        //                <$switch :<
        //                    branches:
        //                        [<case: <$eq:['$en_state',0]>,then: 'BEBAS'>,
        //                            <case: <$eq:['$en_state',1]>,then: 'BELUM BEBAS'>,
        //                            <case: <$eq:['$en_state',2]>,then: 'BATAL'>,
        //                            <case: <$eq:['$en_state',3]>,then: 'KAMPUNG'>,
        //                            <case: <$eq:['$en_state',4]>,then: 'OVERLAP'>,
        //                            <case: <$eq:['$en_state',5]>,then: 'KELUAR'>], default: 'BEBAS'>>,
        //            desa : <$arrayElemAt:['$desas.identity', -1]>,
        //            project: <$arrayElemAt:['$projects.identity',-1]>,
        //            luasSurat : '$basic.current.luasSurat',
        //            group: '$basic.current.group',
        //            pemilik: '$basic.current.pemilik',
        //            _id: 0>>".Replace("<", "{").Replace(">", "}")).ToList();

        //        var xlst = ExpressionFilter.Evaluate(persils, typeof(List<PersilCore4>), typeof(PersilCore4), gs);
        //        var data = xlst.result.Cast<PersilCore4>().ToArray();

        //        return Ok(data.GridFeed(gs));
        //    }
        //    catch (UnauthorizedAccessException exa)
        //    {
        //        return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //    }
        //    catch (Exception ex)
        //    {

        //        MyTracer.TraceError2(ex);
        //        return new UnprocessableEntityObjectResult(ex.Message); ;
        //    }
        //}
    }
}

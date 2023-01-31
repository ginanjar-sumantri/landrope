using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using GenWorkflow;
using GraphHost;
using landrope.api2.Models;
using landrope.mod2;
using landrope.mod3;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mongospace;
using Microsoft.Extensions.DependencyInjection;
using landrope.common;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using auth.mod;
using APIGrid;
using landrope.mod3.shared;
using MongoDB.Driver;
using MongoDB.Bson;
using DynForm.shared;
using Tracer;
using DocumentFormat.OpenXml.Wordprocessing;
using landrope.documents;
using flow.common;
using landrope.hosts;
using System.Transactions;
using GraphConsumer;
using AssignerConsumer;
using BundlerConsumer;
using landrope.consumers;
using Newtonsoft.Json;

namespace landrope.api2.Controllers
{
    [Route("api/mobile")]
    [ApiController]
    public class MobileController : ControllerBase
    {
        IServiceProvider services;
        LandropePlusContext context;
        GraphHostConsumer ghost;
        AssignerHostConsumer ahost;
        BundlerHostConsumer bhost;

        public MobileController(IServiceProvider services)
        {
            this.services = services;
            context = services.GetService<mod3.LandropePlusContext>();
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
            bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/coords")]
        [Consumes("application/json")]
        public IActionResult List([FromQuery] string token, [FromBody] AssignQry qry)
        {
            var user = context.FindUser(token);

            //var active = (qry.incl & QueryMode.Active) == QueryMode.Active;
            var deleg = (qry.incl & QueryMode.Delegated) == QueryMode.Delegated;
            var wip = (qry.incl & QueryMode.Accepted) == QueryMode.Accepted;
            var complete = (qry.incl & QueryMode.Complished) == QueryMode.Complished;
            var over = (qry.incl & QueryMode.Overdue) == QueryMode.Overdue;

            //var noactive = (qry.excl & QueryMode.Active) == QueryMode.Active;
            var nodeleg = (qry.excl & QueryMode.Delegated) == QueryMode.Delegated;
            var nowip = (qry.excl & QueryMode.Accepted) == QueryMode.Accepted;
            var nocomplete = (qry.excl & QueryMode.Complished) == QueryMode.Complished;
            var noover = (qry.excl & QueryMode.Overdue) == QueryMode.Overdue;

            //var vactive = (active && noactive) || (!active && !noactive) ? 0 : active ? 1 : -1;
            var vdeleg = (deleg && nodeleg) || (!deleg && !nodeleg) ? 0 : deleg ? 1 : -1;
            var vwip = (wip && nowip) || (!wip && !nowip) ? 0 : wip ? 1 : -1;
            var vcomplete = (complete && nocomplete) || (!complete && !nocomplete) ? 0 : complete ? 1 : -1;
            var vover = (over && noover) || (!over && !noover) ? 0 : over ? 1 : -1;

            var today = $"new ISODate('{DateTime.Today:yyyy-MM-dd}T00:00:00')";
            var xfilters = new[] {
				//(v:vactive,tr:"{$ne:['$issued',null]}",fa:"{$eq:['$issued',null]}"),
				(v:vdeleg,tr:"{$ne:['$delegated',null]}",fa:"{$eq:['$delegated',null]}"),
                (v:vwip,tr:"{$ne:['$accepted',null]}",fa:"{$eq:['$accepted',null]}"),
                (v:vcomplete,tr:"{$ne:['$closed',null]}",fa:"{$eq:['$closed',null]}"),
                (v:vover,tr:"{$gte:[{$ifNull:['$overdue',-1000000]},0]}" ,fa:"{$lt:[{$ifNull:['$overdue',-1000000]},0]}"),
            };

            var sorts = new List<string>();
            var SortCreated = (qry.sort & SortMode.CreatedDesc);
            var SortIssued = (qry.sort & SortMode.IssuedDesc);
            var SortDelegated = (qry.sort & SortMode.DelegatedDesc);
            var SortAccepted = (qry.sort & SortMode.AcceptedDesc);
            var SortCompleted = (qry.sort & SortMode.ComplishedDesc);
            var SortOverdue = (qry.sort & SortMode.OverdueDesc);

            //if (SortCreated == SortMode.CreatedDesc) sorts.Add("created:-1");
            //else if (SortCreated == SortMode.Created) sorts.Add("age_created:-1");
            if (SortIssued == SortMode.IssuedDesc) sorts.Add("issued:-1");
            else if (SortIssued == SortMode.Issued) sorts.Add("age_issued:-1");
            if (SortDelegated == SortMode.DelegatedDesc) sorts.Add("delegated:-1");
            else if (SortDelegated == SortMode.Delegated) sorts.Add("age_delegated:-1");
            if (SortAccepted == SortMode.AcceptedDesc) sorts.Add("accepted:-1");
            else if (SortAccepted == SortMode.Accepted) sorts.Add("age_accepted:-1");
            if (SortCompleted == SortMode.ComplishedDesc) sorts.Add("closed:-1");
            else if (SortCompleted == SortMode.Complished) sorts.Add("age_closed:-1");
            if (SortOverdue == SortMode.OverdueDesc) sorts.Add("overdue:-1");
            else if (SortOverdue == SortMode.Overdue) sorts.Add("duedate:-1");

            int skip = 0;
            int limit = 0;
            var limited = !(qry.pg == 0 && qry.rpp == 0);
            if (limited)
            {
                if (qry.pg < 1)
                    qry.pg = 1;
                if (qry.rpp == 0)
                    qry.rpp = 10;
                skip = (qry.pg - 1) * qry.rpp;
                limit = qry.rpp;
            }

            var filters = new List<string> { $"{{$eq:['$CoIC','{user.key}']}}" };
            filters.AddRange(xfilters.Select(x => x.v == 1 ? x.tr : x.v == -1 ? x.fa : null));
            var stages = new List<string>{
                $"{{$match:{{$expr:{{$and:[{string.Join(",", filters)}]}}}}}}",
                $"{{$sort:{{{string.Join(",",sorts.ToArray())}}}}}",
                "{$project:{_id:0,age_issued:0,age_delegated:0,age_accepted:0,age_closed:0}}",
            };
            if (limited)
            {
                stages.Add($"{{$skip:{skip}}}");
                stages.Add($"{{$limit:{limit}}}");
            }

            var data = context.GetDocuments(new AssignmentViewM(), "material_assignment_mobile", stages.ToArray()).ToList();
            return Ok(data);
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("COIC_VIEW")]
        [HttpGet("tugas/coord/items/{key}")]
        public IActionResult GetDtl([FromQuery] string token, [FromRoute] string key)
        {
            try
            {
                var user = context.FindUser(token);
                var assign = ahost.GetAssignment(key).GetAwaiter().GetResult() as Assignment;
                if (assign == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                var data = assign.details.Select(d => d.ToView(context)).OrderBy(v => v.IdBidang).ToArray();
                return Ok(data);
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult { StatusCode = code };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }


        [EnableCors(nameof(landrope))]
        [NeedToken("TASK_VIEW,TASK_FULL,TASK_STEP")]
        [HttpGet("get")]
        public IActionResult Get([FromQuery] string token, [FromQuery] string key)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                var user = this.context.FindUser(token);
                var data = ahost.GetAssignment(key).GetAwaiter().GetResult() as Assignment;
                if (data == null)
                    return new NoContentResult();

                AssignmentViewM fdata = data.ToViewM(context);
                return Ok(fdata);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }

        option[] GetSteps(string[] privs)
        {
            var dsteps = new List<DocProcessStep>();
            if (privs.Contains("GPS_CREATOR"))
                dsteps.Add(DocProcessStep.Riwayat_Tanah);
            //dsteps.Add(DocProcessStep.GPS_Dan_Ukur);
            if (privs.Contains("NOT_CREATOR"))
                dsteps.AddRange(new[]{DocProcessStep.AJB,DocProcessStep.AJB_Hibah,DocProcessStep.Akta_Notaris,DocProcessStep.SPH,
									//DocProcessStep.Riwayat_Tanah
				});
            if (privs.Contains("BPN_CREATOR"))
                dsteps.AddRange(new[]{DocProcessStep.Balik_Nama,DocProcessStep.Cetak_Buku,DocProcessStep.PBT_Perorangan,
                                    DocProcessStep.PBT_PT,DocProcessStep.Peningkatan_Hak,
                                    DocProcessStep.Penurunan_Hak, DocProcessStep.SK_BPN});
            var isteps = dsteps.Select(s => (int)s);

            var steps = context.GetAllSteps();
            var res = steps.SelectMany(s => s.steps.Join(isteps, x => x, i => i, (x, i) => x)
            .Select(st => new option
            {
                keyparent = ContextExtensios.StrCats[s.disc],
                key = $"{(DocProcessStep)st:g}",
                identity = ((DocProcessStep)st).GetName()
            })).ToArray();
            return res;
        }


        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/delegate")]
        [Consumes("application/json")]
        public IActionResult Delegate([FromQuery] string token, [FromBody] AssignDelegate data)
        {
            try
            {
                var user = context.FindUser(token);
                var asgn = ahost.GetAssignment(data.akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                var instance = asgn.instance;
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
                //if (instance.lastState?.state != ToDoState.issued_)
                //	return new UnprocessableEntityObjectResult("Status Penugasan tidak sesuai");

                var PIC = context.users.FirstOrDefault(u => u.key == data.keyUser);
                if (PIC.invalid == true)
                    return new UnprocessableEntityObjectResult("PIC yang dipilih sudah tidak aktif");
                var summd = new SummData("ACTOR", "delegated_", data.keyUser);

                (var ok, var reason) = ghost.Take(user, asgn.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, asgn.instkey, data.rkey, ToDoControl._.ToString("g"), summd).GetAwaiter().GetResult();
                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(reason);
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/step")]
        public IActionResult Step([FromQuery] string token, [FromBody] AssignCommand data)
        {
            try
            {
                var user = context.FindUser(token);
                var asgn = ahost.GetAssignment(data.akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan tidak aktif");

                var instance = ghost.Get(asgn.instkey).GetAwaiter().GetResult();
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
                if (asgn.closed != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Penugasan telah selesai");

                var node = instance.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == data.rkey));
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow penugasan tidak jelas");
                var route = node.routes.First(r => r.key == data.rkey);

                var issuing = node._state == ToDoState.created_ && route._verb == ToDoVerb.issue_;
                if (issuing && !asgn.details.Any())
                    return new UnprocessableEntityObjectResult("Penugasan hanya bisa diterbitkan jika sudah ditambahkan bidang yang akan diproses");
                if (issuing)
                {
                    var results = asgn.details.Select(d => bhost.DoMakeTaskBundle(token, asgn, d, null, user, false).GetAwaiter().GetResult()).ToArray();
                    if (results.Any(r => r.bundle == null))
                        throw new Exception(string.Join("|", results.Where(r => r.bundle == null).Select(r => r.reason)));
                }

                (var ok, var reason) = ghost.Take(user, asgn.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, asgn.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);
                if (issuing)
                {
                    ghost.RegisterDocs(instance.key, asgn.details.Select(d => d.key).ToArray(), true).Wait();
                    var results = asgn.details.Select(d => bhost.DoMakeTaskBundle(token, asgn, d, null, user, false).GetAwaiter().GetResult())
                        .Where(r => !string.IsNullOrEmpty(r.reason))
                        .ToArray();
                    if (results.Any())
                        return new OkObjectResult(string.Join("|", results.Select(r => r.reason)));
                }
                return Ok();
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/dtl/submit")]
        public IActionResult Submit(string token, [FromBody] SubmitDoc data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.dkey) || data.info == null)
                return new UnprocessableEntityObjectResult("Penginputan hasil penugasan tidak benar");

            try
            {
                var user = context.FindUser(token);
                var asgn = ahost.GetAssignment(data.akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan tidak aktif");
                var detail = asgn.details.FirstOrDefault(d => d.key == data.dkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak valid");

                var instance = asgn.instance;
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
                if (asgn.closed != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Penugasan telah selesai diproses");

                var subinst = ghost.GetSub(instance.key, data.dkey).GetAwaiter().GetResult();
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Detail Penugasan belum lengkap");
                if (subinst.closed)
                    return new UnprocessableEntityObjectResult("Detail Penugasan telah selesai diproses");

                if (subinst.lastState?.state != ToDoState.accepted_)
                    return new UnprocessableEntityObjectResult("Dokumen hasil telah dikirim");

                detail.preresult = new AssignmentPreResult
                {
                    info = data.info
                };

                (var ok, var reason) = ghost.Take(user, asgn.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, asgn.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();

                if (ok)
                {
                    ahost.Update(asgn);
                    return Ok();
                }
                return new UnprocessableEntityObjectResult(reason);
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/dtl/submit-sps")]
        public IActionResult SubmitSPS(string token, [FromBody] SubmitSpsDoc data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.dkey) || data.nomor == null)
                return new UnprocessableEntityObjectResult("Penginputan SPS tidak benar");

            try
            {
                var user = context.FindUser(token);
                var asgn = ahost.GetAssignment(data.akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan tidak aktif");
                var detail = asgn.details.FirstOrDefault(d => d.key == data.dkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak valid");

                var instance = asgn.instance;
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
                if (asgn.closed != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Penugasan telah selesai diproses");

                var subinst = ghost.GetSub(instance.key, data.dkey).GetAwaiter().GetResult();
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Detail Penugasan belum lengkap");
                if (subinst.closed)
                    return new UnprocessableEntityObjectResult("Detail Penugasan telah selesai diproses");

                if (subinst.lastState?.state != ToDoState.accepted_)
                    return new UnprocessableEntityObjectResult("SPS telah diinput");

                if (context.AllSPS().FirstOrDefault(s => s.keyPersil == detail.keyPersil && s.step == asgn.step.Value) != null)
                    return new UnprocessableEntityObjectResult("SPS untuk bidang dan proses dimaksud sudah ada");
                var sps = new landrope.mod3.SPS
                {
                    keyPersil = detail.keyPersil,
                    step = asgn.step.Value,
                    date = DateTime.Now,
                    nomor = data.nomor
                };
                var ses = context.db.Client.StartSession();
                ses.StartTransaction();
                try
                {
                    context.AddSPS(sps);

                    (var ok, var reason) = ghost.Take(user, asgn.instkey, data.rkey).GetAwaiter().GetResult();
                    if (ok)
                        (ok, reason) = ghost.Summary(user, asgn.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();

                    if (!ok)
                        throw new Exception(reason);

                    ahost.Update(asgn).Wait();
                    ses.CommitTransaction();
                    return Ok();
                }
                catch (Exception)
                {
                    ses.AbortTransaction();
                    throw;
                }
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("tugas/keydt/{cat:int}/{step:int}")]
        public IActionResult GetKeyDT([FromRoute] int cat, [FromRoute] int step)
        {
            var _step = (DocProcessStep)step;
            var disc = ((AssignmentCat)cat).Discriminator();

            var sdt = StepDocType.GetItem(_step, disc);
            var reqdt = sdt.rec_primary ?? sdt.receive.FirstOrDefault();
            if (reqdt == null)
                return new UnprocessableEntityObjectResult("Pengaturan dokumen untuk proses dan kategori ini belum benar");
            var dtype = DocType.List.FirstOrDefault(d => d.key == reqdt.keyDocType);
            if (dtype == null)
                return new UnprocessableEntityObjectResult("Pengaturan dokumen untuk proses dan kategori ini belum benar");
            var rec = dtype.metadata.FirstOrDefault(r => r.primary == true) ?? dtype.metadata.FirstOrDefault();
            if (rec == null)
                return new UnprocessableEntityObjectResult("Pengaturan dokumen untuk proses dan kategori ini belum benar");

            return Ok(new { doctype = dtype.identifier, keydt = dtype.key, prop = rec.key.ToString("g"), propkey = (int)rec.key });
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("tugas/dtl/step")]
        public IActionResult StepDtl([FromQuery] string token, [FromBody] AssignDtlCommand data)
        {
            try
            {
                var user = context.FindUser(token);
                var asgn = ahost.GetAssignment(data.akey).GetAwaiter().GetResult() as Assignment;
                if (asgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak ada");
                if (asgn.invalid == true)
                    return new UnprocessableEntityObjectResult("Penugasan tidak aktif");
                var detail = asgn.details.FirstOrDefault(d => d.key == data.dkey);
                if (detail == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan tidak valid");

                var instance = asgn.instance;
                if (instance == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Penugasan belum lengkap");
                if (asgn.closed != null || instance.closed)
                    return new UnprocessableEntityObjectResult("Penugasan telah selesai diproses");

                var subinst = ghost.GetSub(instance.key, data.dkey).GetAwaiter().GetResult();
                if (subinst == null)
                    return new UnprocessableEntityObjectResult("Konfigurasi Detail Penugasan belum lengkap");
                if (subinst.closed)
                    return new UnprocessableEntityObjectResult("Detail Penugasan telah selesai diproses");

                var node = subinst.Core.nodes.OfType<GraphNode>().FirstOrDefault(n => n.routes.Any(r => r.key == data.rkey));
                if (node == null)
                    return new UnprocessableEntityObjectResult("Posisi Flow penugasan tidak jelas");

                var route = subinst.Core.nodes.OfType<GraphNode>().SelectMany(n => n.routes).FirstOrDefault(r => r.key == data.rkey);

                (var ok, var reason) = ghost.Take(user, asgn.instkey, data.rkey).GetAwaiter().GetResult();
                if (ok)
                    (ok, reason) = ghost.Summary(user, asgn.instkey, data.rkey, data.control.ToString("g"), null).GetAwaiter().GetResult();
                if (!ok)
                    return new UnprocessableEntityObjectResult(reason);

                string error = null;

                if (data.control == ToDoControl.yes_)
                    (ok, error) = route?._verb switch
                    {
                        ToDoVerb.confirm_ => (Bebaskan(detail.persil(context) ?? detail.persilHibah(context), user), null),
                        ToDoVerb.cancelConfirm_ => (Batalkan(detail.persil(context) ?? detail.persilHibah(context), user), null),
                        ToDoVerb.archiveValidate_ => InsertBundle(asgn, detail, user, ControllerContext.HttpContext.RequestServices),
                        _ => (true, null)
                    };
                if (ok)
                    return Ok();
                return new UnprocessableEntityObjectResult(string.IsNullOrEmpty(error) ? "Gagal mengubah status bidang" : error);
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        (bool OK, string err) InsertBundle(Assignment assg, AssignmentDtl detail, user user, IServiceProvider services)
        {
            MyTracer.TraceInfo2($"key persil: {detail.keyPersil}");

            if (detail.result != null)
            {
                MyTracer.TraceInfo2($"detail infoes: {detail.result?.infoes.Select(i => (i.keyDT, props: i.props.Select(x => (x.mkey, x.val)).ToArray())).ToArray()}");

                var bundle = bhost.MainGet(detail.keyPersil).GetAwaiter().GetResult() as MainBundle;
                if (bundle == null)
                    return (false, "Bundle dimaksud tidak ada");

                var infoes = detail.result.infoes.Select(i => (i.keyDT,
                        part: new ParticleDoc
                        {
                            props = i.props.Select(j => j.AsKV()).ToList()
                                                    .ToDictionary(x => x.Key, x => x.Value),
                            exists = i.exs
                        })).ToArray();

                infoes = infoes.Where(x => x.part.props.Values.Any(x => x.Value != null)).ToArray();

                if (infoes.Count() > 0)
                {

                    var insta = assg.instance;
                    var subinst = insta.FindDoc(detail.key);
                    if (subinst == null)
                        return (false, "Detail penugasan tidak mempunyai flow");
                    var arc_claim = subinst.claims.LastOrDefault(s => s.state == ToDoState.resultArchived_);
                    if (arc_claim == null)
                        return (false, "Status flow tidak valid");
                    (var crtime, var cruserkey) = (arc_claim.time, arc_claim.user1key);

                    infoes.Select(i => i.keyDT).ToList().ForEach(i =>
                    {
                        if (!bundle.doclist.Any(d => d.keyDocType == i))
                            bundle.doclist.Add(new BundledDoc(i));
                    });

                    var bdocs = infoes.Join(bundle.doclist, i => i.keyDT, d => d.keyDocType, (i, d) => (d, i.part));
                    MyTracer.TraceInfo2($"bdocs count: {bdocs.Count()}");
                    foreach (var bd in bdocs)
                    {
                        var pd = new ParticleDocChain(new[] { new KeyValuePair<string, ParticleDoc>(MongoEntity.MakeKey, bd.part) });
                        MyTracer.TraceInfo2($"doc in bdocs: {pd.Select(x => (x.Key, value: x.Value.props.Select(p => (p.Key, p.Value.type, p.Value.val)).ToArray())).ToArray()}");
                        var vale = new ValDocList
                        {
                            approved = true,
                            created = crtime,
                            keyCreator = cruserkey,
                            keyReviewer = user.key,
                            kind = ChangeKind.Add,
                            reviewed = DateTime.Now,
                            sourceFile = $"Penugasan {assg.identifier}",
                            Item = pd
                        };
                        MyTracer.TraceInfo2($"vale = {JsonConvert.SerializeObject(vale)}");
                        bd.d.Add(vale);
                        bd.d.current = pd;

                    }
                    bool OK = true;
                    List<string> errors = new List<string>();
                    var dc = new DocController(services);
                    foreach (var info in detail.result.infoes.Where(i => i.scanned != null))
                    {
                        (var ok, var error) = dc.Rename(assg.key, detail.keyPersil, info.keyDT, user.key);
                        OK = OK && ok;
                        if (!ok)
                            errors.Add($"[{info.keyDT}]{error}");
                        else
                        {
                            var log = new LogBundle(user, info.keyDT, detail.keyPersil, DateTime.Now, LogActivityType.Scan, LogActivityModul.Assigment);
                            context.logBundle.Insert(log);
                            context.SaveChanges();
                        }
                    }

                    var metaDatas = detail.result.infoes.SelectMany(x => x.props, (y, z) => new { docInfo = y, val = z.val })
                        .Where(x => x.val != null).Select(x => x.docInfo).Distinct().ToList();

                    if (metaDatas != null)
                        foreach (var info in metaDatas)
                        {
                            var log = new LogBundle(user, info.keyDT, detail.keyPersil, DateTime.Now, LogActivityType.Metadata, LogActivityModul.Assigment);
                            context.logBundle.Insert(log);
                            context.SaveChanges();
                        }


                    MyTracer.TraceInfo2($"errors = {errors}");
                    bhost.MainUpdate(bundle, true).Wait();
                    return (OK, string.Join(",", errors));
                }
                else
                {
                    return (true, "");
                }
            }
            else
            {
                return (true, "");
            }
        }

        bool Batalkan(Persil persil, user user)
        {
            (persil.en_state, persil.statechanged, persil.statechanger) = (StatusBidang.batal, DateTime.Now, user.key);
            context.persils.Update(persil);
            context.SaveChanges();
            return true;
        }

        bool Bebaskan(Persil persil, user user)
        {
            (persil.en_state, persil.statechanged, persil.statechanger) = (StatusBidang.bebas, DateTime.Now, user.key);
            context.persils.Update(persil);
            context.SaveChanges();
            return true;
        }

        [EnableCors(nameof(landrope))]
        [HttpGet("allowed")]
        public IActionResult Allowed([FromQuery] string username)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                var user = this.context.users.FirstOrDefault(u => u.identifier == username);
                if (user == null)
                    return Ok(false);
                var privs = user.getPrivileges(null).Select(p => p.identifier);
                var ok = privs.Intersect(new[] { "BPN_PIC", "NOT_PIC", "GPS_PIC" }).Any();
                return Ok(ok);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }


        [EnableCors(nameof(landrope))]
        [HttpPost("c")]
        [Consumes("application/json")]
        public IActionResult Check([FromBody] DateTime dtm)
        {
            var tmss = DateTime.Now - dtm;
            if (tmss.TotalSeconds >= 10)
                return NoContent();

            var tms = dtm.Ticks;
            return Ok($"{tms}>{tmss.Ticks}");
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("ct")]
        public IActionResult CheckToken(string token)
        {
            try
            {
                var user = context.FindUser(token);
                return Ok();
            }
            catch (UnauthorizedAccessException exua)
            {
                if (int.TryParse(exua.Message, out int code))
                    return new ContentResult
                    {
                        StatusCode = code
                    };
                return new UnprocessableEntityObjectResult(exua.Message);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}


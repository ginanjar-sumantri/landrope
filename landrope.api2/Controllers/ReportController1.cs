#define _PIK2
using auth.mod;
using landrope.hosts;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using landrope.api2.Models;
using landrope.common;
using System.Text;
using System.Reflection;
using MongoDB.Driver;
using MongoDB.Bson;
using GraphConsumer;
using AssignerConsumer;
using BundlerConsumer;
using landrope.consumers;
using landrope.budgets;
using flow.common;
using System.Net;
using GenWorkflow;
using APIGrid;
using Tracer;
using mongospace;
using SelectPdf;
using Microsoft.Extensions.Configuration;
using landrope.documents;
using System.Data;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Newtonsoft.Json;
using HttpAccessor;
using MailSender;
using landrope.mod4;

namespace landrope.api2.Controllers
{
    [Route("api/reporting")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class ReportController : ControllerBase
    {
        authEntities acontext;
        ExtLandropeContext contextex;
        LandropePlusContext contextplus;
        IServiceProvider services;
        GraphHostConsumer ghost;
        AssignerHostConsumer ahost;
        BundlerHostConsumer bhost;
        LandropeContext context = Contextual.GetContext();
        private IConfiguration configuration;

        public ReportController(IServiceProvider services, IConfiguration iconfig)
        {
            this.services = services;
            acontext = services.GetService<authEntities>();
            contextex = services.GetService<ExtLandropeContext>();
            contextplus = services.GetService<mod3.LandropePlusContext>();
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
            bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
            configuration = iconfig;
        }

        public static (bool girik, bool hibah, DocProcessStep head, int order, DocProcessStep[] steps)[] stepHeads =
            new (bool girik, bool hibah, DocProcessStep head, int order, DocProcessStep[] steps)[]
            {
                (true,false,DocProcessStep.PBT_Perorangan, 0, new[]{DocProcessStep.Baru_Bebas,DocProcessStep.Akta_Notaris,DocProcessStep.PBT_Perorangan}),
                (true,false,DocProcessStep.PBT_PT,1,new[]{DocProcessStep.SPH,DocProcessStep.PBT_PT}),
                (true,false,DocProcessStep.SK_BPN,2,new[]{DocProcessStep.SK_BPN}),
                (true,false,DocProcessStep.Cetak_Buku,3,new[]{DocProcessStep.Cetak_Buku}),
                (false,true, DocProcessStep.Penurunan_Hak,0,new[]{DocProcessStep.Baru_Bebas,DocProcessStep.Riwayat_Tanah,DocProcessStep.SHM_Hibah,
                                        DocProcessStep.Akta_Notaris,DocProcessStep.Penurunan_Hak}),
                (false,true, DocProcessStep.Balik_Nama,1,new[]{DocProcessStep.AJB}),
                (false,false, DocProcessStep.Penurunan_Hak,0,new[]{DocProcessStep.Baru_Bebas,DocProcessStep.Riwayat_Tanah,DocProcessStep.Akta_Notaris,DocProcessStep.Penurunan_Hak}),
                (false,false, DocProcessStep.Balik_Nama,1,new[]{DocProcessStep.AJB,DocProcessStep.Balik_Nama}),
            };

        [HttpGet("budget")]
        [NeedToken("MAP_VIEW,VIEW_MAP,REP_VIEW")]
        public IActionResult GetBudget(string token, string keys, bool csv = false)
        {
            var readies = bhost.NextReadiesDiscrete(inclActive: true).GetAwaiter().GetResult()
                            .Where(p => p.LuasHGB == null && p.noHGB == null).ToArray();
            var excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
                            .ToList().Select(x => x.key).ToArray();

            readies = readies.Where(r => !excluded.Contains(r.IdBidang)).ToArray();
            var Keys = keys?.Split(',', ';', '|');
            if (Keys != null && Keys.Any())
            {
                readies = readies.Where(p => Keys.Contains(p.keyProject)).ToArray();
            }
            var data = readies.Where(r => r._step != DocProcessStep.Belum_Bebas)
                                    .Select(r => (pnd: r, curr: Assignment.GetPrev(r.cat.Discriminator(), r._step))).ToArray();

            var AllProjects = contextplus.GetProjects().Select(p => new Location { key = p.key, name = p.identity }).ToArray();
            var xdata = data.Select(r => new ReportWithBudget
            {
                key = r.pnd.key,
                IdBidang = r.pnd.IdBidang,
                category = (int)r.pnd.cat,
                status = (int)r.curr,
                next_step = (int)r.pnd._step,
                keyProject = r.pnd.keyProject,
                luas = Math.Round(r.pnd.LuasPropsPBT ?? r.pnd.LuasDibayar ?? r.pnd.LuasSurat ?? 0),
            }).ToList();

            var budg = services.GetService<Budgetter>();
            budg.CalcBudget(xdata);
            /*			var xdataord = xdata.Join(bsteps, d => (d.category, d.next_step),
											b => (b.cat, b.step), (d, b) => (data: d, ord: b.ord)).ToList();
						xdataord.ForEach(x => { 
							foreach(var budget in x.data.budgets)
							{
								budget
							}
						});
			*/
            //RptBudgetBase repdata=null;
            if (csv)
            {
                var ddata = xdata.Select(x => (x.key, x.IdBidang, x.category, x.status, x.next_step, x.keyProject, x.MasukBPN, x.luas,
                                                    budgets: x.budgets.Select(b => (headx: heading2((AssignmentCat)x.category, b.step), b.price, b.amount))
                                                    .OrderBy(d => d.headx.order).ToArray()
                                                    .Select(b => new KeyValuePair<(int ord, DocProcessStep head), (double price, double amount)>(b.headx, (b.price, b.amount)))
                                                                            .ToDictionary(bb => bb.Key, bb => bb.Value))).ToArray();
                /*				var cdiscrete = xdata.SelectMany(x => x.budgets.Select(xb => (x.IdBidang, x.category, x.MasukBPN, x.keyProject, x.luas, x.status, x.next_step,
												budget: new BudgetDtl
												{
													IdBidang = xb.IdBidang,
													step = heading((AssignmentCat)x.category, (DocProcessStep)xb.step),
													price = xb.price,
													amount = xb.amount,
													accumulative = xb.accumulative
												}))).ToArray();
				*/
                var outdata = ddata.Join(AllProjects, d => d.keyProject, p => p.key, (d, p) => new
                {
                    d.IdBidang,
                    project = p.name,
                    cat = ((AssignmentCat)d.category).ToString("g"),
                    d.luas,
                    status = ((DocProcessStep)d.status).ToString("g"),
                    next = ((DocProcessStep)d.next_step).ToString("g"),
                    sps = d.MasukBPN,
                    d.budgets
                }).ToArray();

                var sb = MakeCsv(outdata);
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }

            var endpoints = new[] { DocProcessStep.Balik_Nama, DocProcessStep.Cetak_Buku };

            var discrete = xdata.SelectMany(x => x.budgets.Select(xb => (x.IdBidang, x.category, x.MasukBPN, x.keyProject, x.luas, x.status, x.next_step,
                            budget: new BudgetDtl
                            {
                                IdBidang = xb.IdBidang,
                                step = heading((AssignmentCat)x.category, (DocProcessStep)xb.step),
                                price = xb.price,
                                amount = xb.amount,
                                accumulative = xb.accumulative
                            }))).ToArray();

            var rdata = discrete.GroupBy(d => (d.MasukBPN, girik: d.category == (int)AssignmentCat.Girik, d.keyProject, d.budget.step))
                .Join(AllProjects, g => g.Key.keyProject, p => p.key, (g, p) => new
                {
                    girik = g.Key.girik,
                    masukBpn = g.Key.MasukBPN,
                    project = p.name,
                    head = g.Key.step,
                    status = g.First().status,
                    price = g.First().budget.price,
                    amount = g.Sum(d => d.budget.amount),
                    bidang = g.Count(),
                    luas = g.Sum(d => d.luas),
                })
                .Select(d =>
                new RptBudgetCommon
                {
                    girik = d.girik,
                    masukBpn = d.masukBpn,
                    project = d.project,
                    head = d.head,
                    bidang = endpoints.Contains(d.head) ? d.bidang : 0,
                    luas = endpoints.Contains(d.head) ? d.luas : 0,
                    price = d.price,
                    amount = d.amount
                }).ToArray();
            return Ok(rdata);

            DocProcessStep heading(AssignmentCat cat, DocProcessStep next)
            {
                var sh = cat switch
                {
                    AssignmentCat.Girik => stepHeads.FirstOrDefault(x => x.girik && !x.hibah && x.steps.Contains(next)),
                    AssignmentCat.Hibah => stepHeads.FirstOrDefault(x => !x.girik && x.hibah && x.steps.Contains(next)),
                    _ => stepHeads.FirstOrDefault(x => !x.girik && !x.hibah && x.steps.Contains(next))
                };
                if ((int)sh.head == 0 || sh.steps == null)
                    return cat == AssignmentCat.Girik ? DocProcessStep.PBT_Perorangan : DocProcessStep.Penurunan_Hak;
                return sh.head;
            }

            (int order, DocProcessStep head) heading2(AssignmentCat cat, DocProcessStep next)
            {
                var sh = cat switch
                {
                    AssignmentCat.Girik => stepHeads.Select(s => (s, i: s.order))
                                .FirstOrDefault(x => x.s.girik && !x.s.hibah && x.s.steps.Contains(next)),
                    AssignmentCat.Hibah => stepHeads.Select(s => (s, i: s.order + 4))
                                .FirstOrDefault(x => !x.s.girik && x.s.hibah && x.s.steps.Contains(next)),
                    _ => stepHeads.Select(s => (s, i: s.order + 4))
                                    .FirstOrDefault(x => !x.s.girik && !x.s.hibah && x.s.steps.Contains(next))
                };
                if ((int)sh.s.head == 0 || sh.s.steps == null)
                    return cat == AssignmentCat.Girik ? (-1, DocProcessStep.PBT_Perorangan) : (-1, DocProcessStep.Penurunan_Hak);
                return (sh.i, sh.s.head);
            }
        }

        static StringBuilder MakeCsv<T>(T[] outdata, bool doublehead = true)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
            var sb = new StringBuilder();
            var propreg = props.Where(p => p.PropertyType.Name != "Dictionary`2" && p.SetMethod != null);
            var headers = propreg.Select(p => (px: true, p, p.Name, Label: p.GetCustomAttribute<CsvLabelAttribute>()))
                                        .Where(p => p.Label == null || !p.Label.Avoid)
                                        .ToList();
            var propdict = props.Where(p => p.PropertyType.Name == "Dictionary`2");
            var fixcols = headers.Count;

            var prheaders = headers.Select(p => "").ToList();

            var pkeys = new List<(bool px, PropertyInfo p, string Name, CsvLabelAttribute Label)>();
            var prkeys = new List<string>();
            foreach (var d in outdata)
            {
                var dicts = propdict.Select(r => (pi: r, d: r.GetValue(d) as Dictionary<(int ord, DocProcessStep head), (double p, double a)>)).ToArray();
                var dkeys = dicts.SelectMany(dd => dd.d.Keys.Select(xk => (dd.pi, h: $"{xk.head}", p: $"{dd.d[xk].p:#,##0}")));
                dkeys = dkeys.Where(dk => !pkeys.Select(p => p.Name).Contains(dk.h)).ToArray();
                pkeys.AddRange(dkeys.Select(d => (false, d.pi, d.h, (CsvLabelAttribute)null)));
                prkeys.AddRange(dkeys.Select(d => d.p));
            }
            headers.AddRange(pkeys);
            prheaders.AddRange(prkeys);
            var dictcols = headers.Count - fixcols;
            var range = Enumerable.Range(fixcols, dictcols).ToArray();

            sb.AppendJoin(",", headers.Select(h => h.Label?.Text == null ? h.Name : h.Label.Text));
            sb.AppendLine();
            if (doublehead)
            {
                sb.AppendJoin(",", prheaders);
                sb.AppendLine();
            }
            foreach (var d in outdata)
            {
                var datas = headers.Select(r => r.p.GetValue(d)).ToArray();
                var normdatas = datas.Take(fixcols).Select(dd => dd?.ToString()).ToList();
                var dictdatas = datas.Skip(fixcols).ToArray();
                var pvals = new List<string>();
                var dicts = propdict.Select(r => r.GetValue(d) as Dictionary<(int ord, DocProcessStep head), (double p, double a)>).ToArray();
                var dvals = dicts.SelectMany(dd => dd.Keys.Select(xk => (ord: xk.ord + fixcols, val: dd[xk])).Select(dv => (dv.ord, dv.val.a)));
                var adds = range.GroupJoin(dvals, r => r, d => d.ord, (r, sd) => sd.FirstOrDefault())
                                        .Select(x => x.a).ToArray();
                normdatas.AddRange(adds.Select(a => a.ToString()));

                sb.AppendJoin(",", normdatas);
                sb.AppendLine();
            }

            return sb;
        }

        [HttpGet("progress")]
        [NeedToken("MAP_VIEW,VIEW_MAP,REP_VIEW")]
        public IActionResult GetProgress(string token, string keys, int prevdays = 7, bool csv = false)
        {
            string ProgressiveCollName = "progressive";

            var Keys = keys?.Split(',', ';', '|');
            /*			if (Keys != null && Keys.Any())
						{
							readies = readies.Where(p => Keys.Contains(p.keyProject)).ToArray();
						}*/

            var lreadies = bhost.SavedPositionWithNext(Keys).GetAwaiter().GetResult().Where(p => p.step != DocProcessStep.Belum_Bebas);
            var excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
                            .ToList().Select(x => x.key).ToArray();

            var readies = lreadies.Where(r => !excluded.Contains(r.IdBidang)).ToArray();

            /*			var actives = ahost.AssignedPersils();
						var dataX = readies.Where(r => r.next != null && r.next.Value != landrope.mcommon.DocProcessStep.Belum_Bebas)
												.Select(r => (pnd: r, bpn: r.next.Value.StepType() == flow.common.ToDoType.Proc_BPN)).ToArray();*/

            var spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
                                "sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList();

            /*			var datawn = readies.GroupJoin(actives, d => (d.pnd.key, step:d.pnd.next.Value), a => (a.key, a.step),
													(d, sa) => (d.pnd, d.bpn,
													Next: sa.FirstOrDefault().key != null ? d.pnd.next : (DocProcessStep?)null)).ToArray();*/
            var data = readies.GroupJoin(spss, d => (d.key, d.next), s => (s.keyPersil, s.step),
                                        (d, ss) => (pnd: d, bpn: d.next?.StepType() == flow.common.ToDoType.Proc_BPN,
                                        Next: d.next, sps: ss.FirstOrDefault()))
                .Select(x => (x.pnd, x.bpn, sps: x.sps == null ? 0 : 1,
                todo: (!x.bpn || x.sps == null) ? x.Next : x.pnd.next)).ToArray();

            var AllProjects = contextplus.GetProjects().Select(p => new Location { key = p.key, name = p.identity }).ToArray();
            var currdata = data.Select(r => new ProgressReport
            {
                key = r.pnd.key,
                IdBidang = r.pnd.IdBidang,
                category = (int)r.pnd.category,
                status = (int)r.pnd.step,
                next_step = (int?)r.todo,
                keyProject = r.pnd.keyProject,
                keyDesa = r.pnd.keyDesa,
                pbt = r.pnd.noPBT,
                bpn = r.bpn,
                sps = r.sps,
                luas = (decimal)Math.Round(r.pnd.luasPropsPBT ?? r.pnd.luasDibayar ?? r.pnd.luasSurat ?? 0),
            }).ToList();

            var xlocations = contextplus.GetDocuments(new
            {
                key = "",
                identity = "",
                villages = new[] {
                new{ key="", identity=""}
            }
            }, "maps", "{$project:{_id:0,key:1,identity:1,'villages.key':1,'villages.identity':1}}")
                .Where(l => Keys.Contains(l.key)).ToList();
            var projects = xlocations.Select(l => (l.key, l.identity)).ToArray();
            var desas = xlocations.SelectMany(p => p.villages.Select(v => (v.key, v.identity)))
                                    .Union(new[] { (key: "", identity: "") }).ToArray();
            var previous = $"{DateTime.Today.AddDays(-prevdays + 1):yyyy -MM-dd}";
            var prevreports = contextplus.GetDocuments(new Progressive(),
                                    ProgressiveCollName,
                    $@"<$lookup: <
           from: 'progressive',
           let:<chrono:'$chrono'>,
           pipeline:[
  						 <$match:<chrono:<$lt:new Date('{previous}')>>>,
               <$group: < _id: null, last:<$max:'$chrono'>>>,
               <$project: <_id:0>>,
               <$match:<$expr:<$eq:['$$chrono','$last']>>>
               ],
           as: 'lasting'
    >>".Replace("<", "{").Replace(">", "}"),
        "{$unwind: '$lasting'}",
        "{$project: {lasting:0,_id:0}}").ToList();

            var chrono_now = DateTime.Now;

            var prdata = prevreports.SelectMany(d => d.data.Select(x =>
                                    ChronoProgress.FromReport(d.chrono, x))).ToList();
            prdata.ForEach(p => p.past = true);
            var crdata = currdata.Select(x => ChronoProgress.FromReport(chrono_now, x)).ToArray();
            var xdata = prdata.Union(crdata).ToList();

            // save progressive asynchronously
            var progressive = new Progressive { chrono = chrono_now, data = currdata.ToArray() };
            contextplus.db.GetCollection<Progressive>(ProgressiveCollName).InsertOneAsync(progressive);

            var gdata = xdata.GroupBy(x => (x.chrono, x.category, x.keyProject, x.keyDesa, x.past, x.status, x.next_step, x.bpn, x.pbt))
                                        .Select(g => (g.Key.chrono, category: (AssignmentCat)g.Key.category, g.Key.keyProject, g.Key.keyDesa,
                                                                g.Key.past, status: (DocProcessStep)g.Key.status, next_step: (DocProcessStep?)g.Key.next_step,
                                                                g.Key.bpn, g.Key.pbt,
                                                                count: (decimal)g.Count(),
                                                                luas: g.Sum(d => d.luas),
                                                                sps: g.Sum(d => d.sps),
                                                                lsps: g.Sum(d => d.sps * d.luas))).ToArray();

            var sertcats = new[] { AssignmentCat.SHM, AssignmentCat.HGB, AssignmentCat.SHP };
            var currdataG = gdata.Where(d => d.category == AssignmentCat.Girik)
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();
            var currdataH = gdata.Where(d => d.category == AssignmentCat.Hibah)
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();
            var currdataS = gdata.Where(d => sertcats.Contains(d.category))
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();

            var allstepsG = landrope.common.Helpers.AllSteps(true, false).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.Girik, s.step, s.next, s.bpn))
                                            .ToArray();
            var allstepsS = landrope.common.Helpers.AllSteps(false, false).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.SHM, s.step, s.next, s.bpn))
                                            .ToArray();
            var allstepsH = landrope.common.Helpers.AllSteps(false, true).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.Hibah, s.step, s.next, s.bpn))
                                            .ToArray();

            var matchesG = allstepsG.GroupJoin(currdataG, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();
            var matchesH = allstepsH.GroupJoin(currdataH, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();
            var matchesS = allstepsS.GroupJoin(currdataS, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();

            var vdata = gdata.Join(projects, x => x.keyProject, p => p.key, (x, p) => (x, p))
                                .Join(desas, x => x.x.keyDesa, d => d.key, (x, d) => (x.x, x.p, d))
                .Select(x => new PivotProgressView
                {
                    chrono = x.x.chrono,
                    category = x.x.category,
                    project = x.p.identity,
                    desa = x.d.identity,
                    count = x.x.count,
                    luas = x.x.luas,
                    past = x.x.past,
                    bpn = x.x.bpn,
                    sps = x.x.sps,
                    lsps = x.x.lsps,
                    pbt = x.x.pbt ?? "",
                    position = PivotProgressView.SetPosition((DocProcessStep)x.x.status, (DocProcessStep?)x.x.next_step, (AssignmentCat)x.x.category)
                }).ToArray();

            var xgdataG = matchesG.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                                     (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            var xgdataH = matchesH.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                        (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            var xgdataS = matchesS.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                        (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            vdata = vdata.Union(xgdataG).Union(xgdataH).Union(xgdataS).ToArray();

            if (csv)
            {
                var sb = MakeCsv(vdata, false);
                var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
                file.FileDownloadName = $"Progress-{DateTime.Now:yyyyMMddHHmm}.csv";
                return file;
            }
            return Ok(vdata);


        }

        [HttpGet("progress2")]
        public IActionResult GetProgressSvc(int prevdays = 7)
        {
            string ProgressiveCollName = "progressive";

            var keyprojects = contextex.GetCollections(new { key = "" }, "maps", "{}", "{_id : 0, key :1}").ToList();

            var Keys = new List<string>();
            foreach (var item in keyprojects)
            {
                Keys.Add(item.key);
            }

            var lreadies = bhost.SavedPositionWithNext(Keys.ToArray()).GetAwaiter().GetResult().Where(p => p.step != DocProcessStep.Belum_Bebas);
            var excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
                            .ToList().Select(x => x.key).ToArray();

            var readies = lreadies.Where(r => !excluded.Contains(r.IdBidang)).ToArray();

            /*			var actives = ahost.AssignedPersils();
						var dataX = readies.Where(r => r.next != null && r.next.Value != landrope.mcommon.DocProcessStep.Belum_Bebas)
												.Select(r => (pnd: r, bpn: r.next.Value.StepType() == flow.common.ToDoType.Proc_BPN)).ToArray();*/

            var spss = contextplus.GetCollections(new { keyPersil = "", step = DocProcessStep.Baru_Bebas, date = DateTime.Today },
                                "sps", "{date:{$ne:null}}", "{_id:0, keyPersil:1, step:1, date:1 }").ToList();

            /*			var datawn = readies.GroupJoin(actives, d => (d.pnd.key, step:d.pnd.next.Value), a => (a.key, a.step),
													(d, sa) => (d.pnd, d.bpn,
													Next: sa.FirstOrDefault().key != null ? d.pnd.next : (DocProcessStep?)null)).ToArray();*/
            var data = readies.GroupJoin(spss, d => (d.key, d.next), s => (s.keyPersil, s.step),
                                        (d, ss) => (pnd: d, bpn: d.next?.StepType() == flow.common.ToDoType.Proc_BPN,
                                        Next: d.next, sps: ss.FirstOrDefault()))
                .Select(x => (x.pnd, x.bpn, sps: x.sps == null ? 0 : 1,
                todo: (!x.bpn || x.sps == null) ? x.Next : x.pnd.next)).ToArray();

            var AllProjects = contextplus.GetProjects().Select(p => new Location { key = p.key, name = p.identity }).ToArray();
            var currdata = data.Select(r => new ProgressReport
            {
                key = r.pnd.key,
                IdBidang = r.pnd.IdBidang,
                category = (int)r.pnd.category,
                status = (int)r.pnd.step,
                next_step = (int?)r.todo,
                keyProject = r.pnd.keyProject,
                keyDesa = r.pnd.keyDesa,
                pbt = r.pnd.noPBT,
                bpn = r.bpn,
                sps = r.sps,
                luas = (decimal)Math.Round(r.pnd.luasPropsPBT ?? r.pnd.luasDibayar ?? r.pnd.luasSurat ?? 0),
            }).ToList();

            var xlocations = contextplus.GetDocuments(new
            {
                key = "",
                identity = "",
                villages = new[] {
                new{ key="", identity=""}
            }
            }, "maps", "{$project:{_id:0,key:1,identity:1,'villages.key':1,'villages.identity':1}}")
                .Where(l => Keys.Contains(l.key)).ToList();
            var projects = xlocations.Select(l => (l.key, l.identity)).ToArray();
            var desas = xlocations.SelectMany(p => p.villages.Select(v => (v.key, v.identity)))
                                    .Union(new[] { (key: "", identity: "") }).ToArray();
            var previous = $"{DateTime.Today.AddDays(-prevdays + 1):yyyy -MM-dd}";
            var prevreports = contextplus.GetDocuments(new Progressive(),
                                    ProgressiveCollName,
                    $@"<$lookup: <
           from: 'progressive',
           let:<chrono:'$chrono'>,
           pipeline:[
  						 <$match:<chrono:<$lt:new Date('{previous}')>>>,
               <$group: < _id: null, last:<$max:'$chrono'>>>,
               <$project: <_id:0>>,
               <$match:<$expr:<$eq:['$$chrono','$last']>>>
               ],
           as: 'lasting'
    >>".Replace("<", "{").Replace(">", "}"),
        "{$unwind: '$lasting'}",
        "{$project: {lasting:0,_id:0}}").ToList();

            var chrono_now = DateTime.Now;

            var prdata = prevreports.SelectMany(d => d.data.Select(x =>
                                    ChronoProgress.FromReport(d.chrono, x))).ToList();
            prdata.ForEach(p => p.past = true);
            var crdata = currdata.Select(x => ChronoProgress.FromReport(chrono_now, x)).ToArray();
            var xdata = prdata.Union(crdata).ToList();

            // save progressive asynchronously
            var progressive = new Progressive { chrono = chrono_now, data = currdata.ToArray() };
            contextplus.db.GetCollection<Progressive>(ProgressiveCollName).InsertOneAsync(progressive);

            var gdata = xdata.GroupBy(x => (x.chrono, x.category, x.keyProject, x.keyDesa, x.past, x.status, x.next_step, x.bpn, x.pbt))
                                        .Select(g => (g.Key.chrono, category: (AssignmentCat)g.Key.category, g.Key.keyProject, g.Key.keyDesa,
                                                                g.Key.past, status: (DocProcessStep)g.Key.status, next_step: (DocProcessStep?)g.Key.next_step,
                                                                g.Key.bpn, g.Key.pbt,
                                                                count: (decimal)g.Count(),
                                                                luas: g.Sum(d => d.luas),
                                                                sps: g.Sum(d => d.sps),
                                                                lsps: g.Sum(d => d.sps * d.luas))).ToArray();

            var sertcats = new[] { AssignmentCat.SHM, AssignmentCat.HGB, AssignmentCat.SHP };
            var currdataG = gdata.Where(d => d.category == AssignmentCat.Girik)
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();
            var currdataH = gdata.Where(d => d.category == AssignmentCat.Hibah)
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();
            var currdataS = gdata.Where(d => sertcats.Contains(d.category))
                            .Select(d => (d.category, d.keyProject, d.status, d.next_step))
                            .Distinct().ToArray();

            var allstepsG = landrope.common.Helpers.AllSteps(true, false).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.Girik, s.step, s.next, s.bpn))
                                            .ToArray();
            var allstepsS = landrope.common.Helpers.AllSteps(false, false).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.SHM, s.step, s.next, s.bpn))
                                            .ToArray();
            var allstepsH = landrope.common.Helpers.AllSteps(false, true).Join(projects, s => 1, k => 1, (s, k) => (keyp: k.key, cat: AssignmentCat.Hibah, s.step, s.next, s.bpn))
                                            .ToArray();

            var matchesG = allstepsG.GroupJoin(currdataG, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();
            var matchesH = allstepsH.GroupJoin(currdataH, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();
            var matchesS = allstepsS.GroupJoin(currdataS, a => (a.keyp, a.step, a.next),
                                c => (c.keyProject, c.status, c.next_step),
                                (a, sc) => (a, exists: sc.Any())).Where(x => !x.exists).ToArray();

            var vdata = gdata.Join(projects, x => x.keyProject, p => p.key, (x, p) => (x, p))
                                .Join(desas, x => x.x.keyDesa, d => d.key, (x, d) => (x.x, x.p, d))
                .Select(x => new PivotProgressView
                {
                    chrono = x.x.chrono,
                    category = x.x.category,
                    project = x.p.identity,
                    desa = x.d.identity,
                    count = x.x.count,
                    luas = x.x.luas,
                    past = x.x.past,
                    bpn = x.x.bpn,
                    sps = x.x.sps,
                    lsps = x.x.lsps,
                    pbt = x.x.pbt ?? "",
                    position = PivotProgressView.SetPosition((DocProcessStep)x.x.status, (DocProcessStep?)x.x.next_step, (AssignmentCat)x.x.category)
                }).ToArray();

            var xgdataG = matchesG.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                                     (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            var xgdataH = matchesH.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                        (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            var xgdataS = matchesS.Join(projects, x => x.a.keyp, p => p.key, (x, p) => new PivotProgressView(
                        (chrono_now, false, x.a.cat, p.identity, x.a.step, x.a.next, x.a.bpn)));
            vdata = vdata.Union(xgdataG).Union(xgdataH).Union(xgdataS).ToArray();

            //if (csv)
            //{
            //    var sb = MakeCsv(vdata, false);
            //    var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
            //    file.FileDownloadName = $"Progress-{DateTime.Now:yyyyMMddHHmm}.csv";
            //    return file;
            //}
            return Ok(vdata);


        }


        /*
		 *    {$match:{en_state:{$in:[null,0,"0"]}}},
    {$addFields: {deal:{$ne:[{$ifNull: [ "$deal", null ]},null]} }},
    {$project:{deal:1,
        keyProject:"$basic.current.keyProject",keyDesa:"$basic.current.keyDesa",luasSurat:"$basic.current.luasSurat"}}
]
		 */

        [HttpGet("acq-status")]
        [NeedToken("MAP_VIEW,VIEW_MAP,REP_VIEW")]
        public IActionResult GetLandStatus(string token, string keys, bool csv = false)
        {
            var Keys = keys?.Split(',', ';', '|');
            var stKeys = string.Join(',', Keys?.Select(k => $"'{k}'") ?? new string[0]);
            var projfilter = (Keys == null || Keys.Length == 0) ? null :
                                $"<$match:<'basic.current.keyProject':<$in:[{stKeys}]>>>".Replace("<", "{").Replace(">", "}");

            var prestages = new string[] {
                "{$merge:{into:'material_spec_bundles_summ',on:'key',whenMatched:'replace',whenNotMatched:'insert'}}"
            };

            var predata = contextplus.GetDocuments<BsonDocument>(null, "vSpecBundlesSumm", prestages);
            //contextplus.db.GetCollection<BsonDocument>("vSpecBundlesSumm").Aggregate()
            //	.AppendStage(PipelineDefinition<BsonDocument, BsonDocument>.Create(prestages)));

            var data = contextplus.GetCollections(new PersilStatus(), "vLandStatus", "{}").ToList();

            //var excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
            //				.ToList().Select(x => x.key).ToArray();
            //data = data.Where(d => !excluded.Contains(d.key)).ToList();

            var locations = contextplus.GetVillages();
            //if (Keys != null && Keys.Length > 0)
            //	locations = locations.Where(x => Keys.Contains(x.project.key)).ToList();

            data = data.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToList();
            if (csv)
            {
                var sb = MakeCsv(data.ToArray(), false);
                var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
                file.FileDownloadName = $"Land-Status-{DateTime.Now:yyyyMMddHHmm}.csv";
                return file;
            }
            return Ok(data);
        }

        [NeedToken("NOT_CREATOR")]
        [HttpGet("hibahdamai")]
        public IActionResult GetHibah([FromQuery] string token)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextplus.FindUser(token);

                var data = contextplus.GetCollections(new { bidang = 0, sisa = 0, project = "", desa = "", bintang = 0, damai = 0 }, "vHibahDamai", "{}"
                    , "{bidang:1, sisa:1, project:1, desa:1, bintang:1, damai:1}").ToList().ToArray();

                return new JsonResult(data);

            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        [HttpGet("land-status-all")]
        [NeedToken("MAP_VIEW,VIEW_MAP,REP_VIEW")]
        public IActionResult GetLandStatusAll(string token, bool csv = false)
        {
#if _PIK2_
			var includeds = "keyProject:{$in:['PROJECT001','PROJECT002']}";
#else
            var includeds = "keyProject:{$in:['PROJECT006T','5E76F53E20C4177BBC72251E']}";
#endif
            //var prestages = new string[] {
            //	"{$match:{"+includeds+"}}",
            //	"{$merge:{into:'material_spec_bundles_summ',on:'key',whenMatched:'replace',whenNotMatched:'insert'}}"
            //};


            var task1 = contextplus.GetDocumentsAsync(new PersilStatusExt2(), "vLandStatus2", "{$match:{" + includeds + "}}");
            //Task.Run(async () =>
            //{
            //await contextplus.GetDocumentsAsync<BsonDocument>(null, "vSpecBundlesSumm", prestages);
            //return 
            //await contextplus.GetDocumentsAsync(new PersilStatusExt2(), "vLandStatus2", "{$match:{" + includeds + "}}"));
            //});

            var task2 = Task.Run(() => contextplus.GetVillages());

            //var task2 = contextplus.GetCollectionsAsync(new PersilStatus(), "vHibahDamai2", "{" + excludeds + "}");

            Task.WaitAll(task1, task2);

            var data = task1.Result;
            var locations = task2.Result;


            //contextplus.db.GetCollection<BsonDocument>("vSpecBundlesSumm").Aggregate()
            //	.AppendStage(PipelineDefinition<BsonDocument, BsonDocument>.Create(prestages)));



            //var excluded = contextplus.GetCollections(new { key = "" }, "exclusion", "{}", "{_id:0}")
            //				.ToList().Select(x => x.key).ToArray();
            //data = data.Where(d => !excluded.Contains(d.key)).ToList();

            //if (Keys != null && Keys.Length > 0)
            //	locations = locations.Where(x => Keys.Contains(x.project.key)).ToList();

            data = data.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => (PersilStatusExt2)d.SetLocation(p.project.identity, p.desa.identity))
                            .ToList();
#if _PIK2_
			foreach (var d in data)
				if (d.status=="aSudah Bebas")
					d.luas = d.luasDibayar ?? d.luasSurat ?? 0;
#endif

            //var xtras = damai.Select(d => (d, bbs: d.luasOverlap, blm: d.Luas - (d.luasOverlap ?? 0)));
            //set as belum bebas coz overlaps didn't exists
            //var notbbs = damai.Where(x => x.luasOverlap == 0).ToArray();
            //var bbs = damai.Where(x => x.luasOverlap > 0).ToArray();
            //foreach (var p in damai)
            //	p.bebas = false;
            //set as belum bebas coz overlaps didn't exists
            //Console.WriteLine($"* belum bebas...{notbbs.Length} records");
            //Console.WriteLine($"* bebas...{bbs.Length} records");

            //var damai = data.Where(d => d.proses == "b" && d.luasOverlap > 0);
            //var excess = damai.Where(x => (x.sisaOverlap > 0)).ToArray();
            //foreach (var p in excess)
            //{
            //	p.luasDibayar = p.luas = p.luasOverlap;
            //	p.status = "a Sudah Bebas";
            //	p.desc = "bEx Bintang";
            //	p.kategori = "bDamai";
            //}
            //foreach (var p in data)
            //	p.luasOverlap = p.sisaOverlap = 0;


            //Console.WriteLine($"* bebas parsial...{excess.Length} records");
            //data.AddRange(excess.Select(x => x.Clone(x.sisaOverlap.Value)));

            if (csv)
            {
                var sb = MakeCsv(data.ToArray(), false);
                var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
                file.FileDownloadName = $"Land-Status-{DateTime.Now:yyyyMMddHHmm}.csv";
                return file;
            }
            return Ok(data);
        }

        [NeedToken("BUNDLE_VIEW,BUNDLE_FULL,BUNDLE_REVIEW,BPN_CREATOR,NOT_CREATOR,ARCHIVE_FULL")]
        [HttpGet("bundles")]
        public IActionResult GetBundles([FromQuery] string token)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextplus.FindUser(token);

                var bundle = contextplus.GetCollections(new Bundle(), "view_bundles_doc", "{}"
                    , "{_id:0}").ToList();

                //var persils = contextplus.GetCollections(new { key = "", IdBidang = "", keyDesa = "", keyPTSK = "" }, "persils_v2", "{invalid:{$ne:true},'basic.current':{$ne:null}}"
                //    , "{_id:0, key:1, IdBidang:1, keyDesa:'basic.current.keyDesa', keyPTSK:'basic.current.keyPTSK'}").ToList();

                var persils = contextex.GetDocuments(new { key = "", IdBidang = "", keyDesa = "", keyPTSK = "" }, "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    "{$project:{_id:0,key:1,IdBidang:1,keyDesa:'$basic.current.keyDesa',keyPTSK:'$basic.current.keyPTSK'}}").ToList();

                var ptsks = contextplus.GetCollections(new { key = "", identifier = "" }, "masterdatas", "{_t:'ptsk',invalid:{$ne:true}}"
                    , "{_id:0, key:1, identifier:1}").ToList();

                var villages = contextex.GetVillages();

                var bundlegroup = bundle.GroupBy(c => new { c.key, c.arrayIndex, c.IdBidang, c.keyDocType }).Select(x =>
                 new BundleViewExt
                 {
                     key = x.FirstOrDefault().key,
                     keyDocType = x.FirstOrDefault().keyDocType,
                     JenisDok = x.FirstOrDefault().JenisDok,
                     IdBidang = x.FirstOrDefault().IdBidang,
                     Nomor = "\"" + x.Where(r => r.NamaField == "Nomor").Select(r => r.value).FirstOrDefault() + "\"",
                     Tahun = "\"" + x.Where(r => r.NamaField == "Tahun").Select(r => r.value).FirstOrDefault() + "\"",
                     Nama = "\"" + x.Where(r => r.NamaField == "Nama").Select(r => r.value).FirstOrDefault() + "\"",
                     Luas = "\"" + x.Where(r => r.NamaField == "Luas").Select(r => r.value).FirstOrDefault() + "\"",
                     Nilai = "\"" + x.Where(r => r.NamaField == "Nilai").Select(r => r.value).FirstOrDefault() + "\"",
                     Lunas = "\"" + x.Where(r => r.NamaField == "Lunas").Select(r => r.value).FirstOrDefault() + "\"",
                     Jenis = "\"" + x.Where(r => r.NamaField == "Jenis").Select(r => r.value).FirstOrDefault() + "\"",
                     Due_Date = x.FirstOrDefault(r => r.NamaField == "Due_Date")?.value == null ? "" : "\"" + x.FirstOrDefault(r => r.NamaField == "Due_Date")?.value.Replace("00:00:00", "").Replace("T", "").Trim() + "\"",
                     NIK = x.Where(r => r.NamaField == "NIK").Select(r => r.value).FirstOrDefault() == null ? "" : "\"'" + x.Where(r => r.NamaField == "NIK").Select(r => r.value).FirstOrDefault() + "\"",
                     Nomor_KK = x.Where(r => r.NamaField == "Nomor_KK").Select(r => r.value).FirstOrDefault() == null ? "" : "\"'" + x.Where(r => r.NamaField == "Nomor_KK").Select(r => r.value).FirstOrDefault() + "\"",
                     Tanggal_Bayar = x.FirstOrDefault(r => r.NamaField == "Tanggal_Bayar")?.value == null ? "" : "\"" + x.FirstOrDefault(r => r.NamaField == "Tanggal_Bayar")?.value.Replace("00:00:00", "").Replace("T", "").Trim() + "\"",
                     Tanggal_Validasi = x.FirstOrDefault(r => r.NamaField == "Tanggal_Validasi")?.value == null ? "" : "\"" + x.FirstOrDefault(r => r.NamaField == "Tanggal_Validasi")?.value.Replace("00:00:00", "").Replace("T", "").Trim() + "\"",
                     Tanggal = x.FirstOrDefault(r => r.NamaField == "Tanggal")?.value == null ? "" : "\"" + x.FirstOrDefault(r => r.NamaField == "Tanggal")?.value.Replace("00:00:00", "").Replace("T", "").Trim() + "\"",
                     Nama_Lama = "\"" + x.Where(r => r.NamaField == "Nama_Lama").Select(r => r.value).FirstOrDefault() + "\"",
                     Nama_Baru = "\"" + x.Where(r => r.NamaField == "Nama_Baru").Select(r => r.value).FirstOrDefault() + "\"",
                     NOP = x.Where(r => r.NamaField == "NOP").Select(r => r.value).FirstOrDefault() == null ? "" : "\"'" + x.Where(r => r.NamaField == "NOP").Select(r => r.value).FirstOrDefault() + "\"",
                     Nomor_NIB = "\"" + x.Where(r => r.NamaField == "Nomor_NIB").Select(r => r.value).FirstOrDefault() + "\"",
                     Nomor_PBT = "\"" + x.Where(r => r.NamaField == "Nomor_PBT").Select(r => r.value).FirstOrDefault() + "\"",
                     NTPN = "\"" + x.Where(r => r.NamaField == "NTPN").Select(r => r.value).FirstOrDefault() + "\"",
                     Nama_Notaris = "\"" + x.Where(r => r.NamaField == "Nama_Notaris").Select(r => r.value).FirstOrDefault() + "\"",
                     Keterangan = "\"" + x.Where(r => r.NamaField == "Keterangan").Select(r => r.value).FirstOrDefault() + "\"",
                     Lainnya = "\"" + x.Where(r => r.NamaField == "Lainnya").Select(r => r.value).FirstOrDefault() + "\"",
                     A = x.FirstOrDefault().Asli,
                     C = x.FirstOrDefault().Copy,
                     L = x.FirstOrDefault().legalisir,
                     S = x.FirstOrDefault().salinan,
                     SC = x.FirstOrDefault().softcopy
                 }).ToArray();


                bundlegroup = bundlegroup.Join(persils, d => d.key, p => p.key, (a, b) => a.setKey(b.IdBidang, b.keyDesa, b.keyPTSK)).ToArray();
                bundlegroup = bundlegroup.Join(villages, d => d.keyDesa, v => v.desa.key, (a, b) => a.setVillage(b.project.identity, b.desa.identity)).ToArray();
                bundlegroup = bundlegroup.Join(ptsks, d => d.keyPTSK, p => p.key, (a, b) => a.setPtsk(b.identifier)).ToArray();
                var data = bundlegroup.Select(x => x.toView()).ToArray();

                var sb = MakeCsv(data);
                var file = new FileContentResult(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv");
                file.FileDownloadName = $"bundles-{DateTime.Now:yyyyMMddHHmm}.csv";
                return file;
                //return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };

            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Surat Tugas BPN
        /// </summary>
        [NeedToken("BPN_CREATOR,BPN_REVIEW")]
        [ProducesResponseType(typeof(RptSuratTugas), (int)HttpStatusCode.OK)]
        [HttpPost("surat/bpn")]
        public IActionResult GetSuratTugasBpn([FromQuery] string token, string assignKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var assignment = contextex.GetCollections(new Assignment(), "assignments", "{_t:'assignment', key: '" + assignKey + "' }", "{_id:0}").FirstOrDefault();
                if (assignment == null)
                    return new UnprocessableEntityObjectResult("Assignment tidak ada");
                if (assignment.type != ToDoType.Proc_BPN)
                    return new UnprocessableEntityObjectResult("Tipe assignment salah !");

                //var villages = contextex.GetVillage(assignment.keyDesa);
                //var bidangsAssigned = assignment.details.Select(bid => bid.keyPersil);
                //string keyPersils = string.Join(",", bidangsAssigned);
                //keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());
                //var persils = context.db.GetCollection<Persil>("persils_v2").Find("{key:{$in :[" + keyPersils + "]}}").ToList();
                //var staticinfo = contextex.GetCollections(new StaticInfo(), "static_collections", "{_t:'SuratTugasBPN'}", "{_id:0}").FirstOrDefault();

                //signSurat signs1 = new signSurat();
                //signs1.FillSigns(1, "Menerima Tugas,", staticinfo.penerima.Replace("Pak", "").Replace("Ibu", "").Trim());
                //signSurat signs2 = new signSurat();
                //signs2.FillSigns(0, "Hormat Kami,", staticinfo.hormatKami.Trim());
                //List<signSurat> signs = new List<signSurat>();
                //signs.Add(signs1);
                //signs.Add(signs2);

                //RptSuratTugas view = new RptSuratTugas();
                //(
                //    view.tanggalPenugasan, view.nomorPenugasan,
                //    view.penerima, view.tembusan,
                //    view.jenisPenugasan, view.project,
                //    view.desa, view.jumlahBidang,
                //    view.luasSurat,
                //    view.signs
                // )
                // =
                // (
                //    assignment.created, assignment.identifier,
                //    staticinfo.penerima, staticinfo.tembusan.Split("^_^"),
                //    assignment.step.ToString().Replace("_", " "), villages.project.identity,
                //    villages.desa.identity, bidangsAssigned.Distinct().Count(),
                //    persils.Select(p => (p.basic.current.luasSurat ?? p.basic.current.luasDibayar).GetValueOrDefault(0)).Sum(),
                //    signs.OrderBy(s => s.signOrder).ToArray()
                // );

                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Surat Tugaas Non BPN
        /// </summary>
        /// 
        [NeedToken("NOT_CREATOR,NOT_REVIEW")]
        [ProducesResponseType(typeof(RptOrderNotary), (int)HttpStatusCode.OK)]
        [HttpGet("surat/nonBpn")]
        public IActionResult GetSuratTugasNonBPN([FromQuery] string token, string assignKey)
        {
            try
            {
                //var user = contextplus.FindUser(token);
                //var assignment = contextex.GetCollections(new Assignment(), "assignments", "{_t:'assignment', key: '" + assignKey + "' }", "{_id:0}").FirstOrDefault();
                //if (assignment == null)
                //    return new UnprocessableEntityObjectResult("Assignment tidak ada");
                //if (assignment.type != ToDoType.Proc_Non_BPN)
                //    return new UnprocessableEntityObjectResult("Tipe assignment salah !");

                //string keyNotaris = assignment.keyNotaris ?? "";
                //string keyPtsk = assignment.keyPTSK ?? "";
                //string keyCreator = assignment.keyCreator ?? "";
                //var villages = contextex.GetVillage(assignment.keyDesa);
                //var staticinfo = contextex.GetCollections(new StaticInfo(), "static_collections", "{_t:'SuratTugasNonBPN'}", "{_id:0}").FirstOrDefault();
                //var notaris = contextex.GetCollections(new Notaris(), "masterdatas", "{_t:'notaris', key:'" + keyNotaris + "'}", "{_id:0}").FirstOrDefault();
                //var company = context.db.GetCollection<Company>("masterdatas").Find("{_t:'pt', key:'" + keyPtsk + "' invalid:{$ne:true}}").FirstOrDefault();
                //var creator = context.db.GetCollection<user>("securities").Find("{key:'" + keyCreator + "'}").FirstOrDefault();

                //var bidangsAssigned = assignment.details.Select(bid => bid.keyPersil);
                //string keyPersils = string.Join(",", bidangsAssigned);
                //keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());
                //var persils = context.db.GetCollection<Persil>("persils_v2").Find("{key:{$in :[" + keyPersils + "]}}").ToList();

                //List<lampiranSurat> listLampiran = new List<lampiranSurat>();

                //foreach (var persil in persils)
                //{
                //    lampiranSurat lampiran = new lampiranSurat();
                //    string nomorSHM = GetNomorSurat(persil, "shm");
                //    string nomorShgb = GetNomorSurat(persil, "shgb").ToLower().Replace("hgb", "").Replace($"/{villages.desa.identity.ToLower()}", "").Trim();
                //    lampiran.namaPemilik = persil.basic.current.pemilik ?? persil.basic.current.surat.nama;
                //    lampiran.exSHM = nomorSHM;
                //    lampiran.noShgb = nomorShgb;
                //    lampiran.luas = persil.basic.current.luasDibayar ?? persil.basic.current.luasSurat;
                //    lampiran.hargaJual = persil.basic.current.satuanAkte == null || persil.basic.current.satuanAkte == 0 ? persil.basic.current.satuan : persil.basic.current.satuanAkte;
                //    lampiran.nilaiTransaksi = lampiran.luas * lampiran.hargaJual;
                //    listLampiran.Add(lampiran);
                //}

                //signSurat sign1 = new signSurat();
                //sign1.FillSigns(0, "Dibuat Oleh,", creator.FullName);
                //signSurat sign2 = new signSurat();
                //var gMain = ghost.Get(assignment.instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
                //var knownBy = context.db.GetCollection<user>("securities").Find("{key:'" + gMain.creatorkey + "'}").FirstOrDefault();
                //sign2.FillSigns(1, "Diketahui Oleh,", knownBy?.FullName);
                //signSurat sign3 = new signSurat();
                //sign3.FillSigns(2, "Disetujui Oleh,", staticinfo.approvedBy);
                //signSurat sign4 = new signSurat();
                //sign4.FillSigns(3, "Diterima Oleh,", "Notaris/PPAT");

                //List<signSurat> listSignSurat = new List<signSurat>();
                //listSignSurat.Add(sign1);
                //listSignSurat.Add(sign2);
                //listSignSurat.Add(sign3);
                //listSignSurat.Add(sign4);

                //RptOrderNotary view = new RptOrderNotary();
                //(view.pengirim, view.nomorPenugasan,
                //    view.tanggalPenugasan, view.perihal,
                //    view.notaris, view.desa,
                //    view.lampiran,
                //    view.signs
                //)
                // =
                //($"{company.identifier ?? ""} ({villages.project.identity})", assignment.identifier,
                //    assignment.created, $"{assignment.step.Value.DocProcessStepDesc()} ke {company.identifier}" ?? "",
                //    notaris == null ? "" : notaris.identifier, villages.desa.identity,
                //    listLampiran.ToArray(),
                //    listSignSurat.OrderBy(s => s.signOrder).ToArray()
                //);

                //return new JsonResult(view);
                return Ok();
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Tanda Terima Input Bundle Paging
        /// </summary>
        [HttpGet("rpt/log-bundle/paging")]
        public IActionResult GetTandaTerimaPaging([FromQuery] string token, [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] string keyword, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = context.GetDocuments(new
                {
                    IdBidang = "",
                    Project = "",
                    Desa = "",
                    NoPeta = "",
                    AlasHak = "",
                    created = DateTime.MinValue,
                    JenisDokumen = "",
                    user = "",
                    activityTypeEnum = 0,
                    activityModulEnum = 0
                }, "logBundle",
                    "{$match: {$and: [ {'created': {$gte: new Date('" + startDate + "')}}, {'created': {$lt: new Date('" + endDate + "')}} ] }}",
                    "{$project : {keyDocType: 1, keyCreator : 1, created : 1, activityType : 1, keyPersil : 1, modul: 1}}",
                    "{$lookup: { from: 'persils_v2', localField: 'keyPersil', foreignField: 'key', as : 'persils'}}",
                    "{$unwind: { path: '$persils', preserveNullAndEmptyArrays: true}}",
                    @"{$project:
                                                 {
                                                    _id: 0,
                                                    IdBidang: '$persils.IdBidang',
                                                    keyProject: '$persils.basic.current.keyProject',
                                                    keyDesa: '$persils.basic.current.keyDesa',
                                                    NoPeta: '$persils.basic.current.noPeta',
                                                    AlasHak: '$persils.basic.current.surat.nomor',
                                                    keyDocType: 1,
                                                    keyCreator: 1,
                                                    created: 1,
                                                    activityType: 1,
                                                    modul: 1 
                                                },
                                     }",

                    "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",
                    "{$match : {'project': {$ne: []}}}",

                    @"{$project:
                                                {
                                                 _id: 0,
                                                 IdBidang: 1,
                                                 Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                                 Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                                 NoPeta : 1,
                                                 AlasHak : 1,
                                                 keyDocType: 1,
                                                 keyCreator: 1,
                                                 created: 1,
                                                 activityType: 1,
                                                 modul: 1
                                                }
                                     }",
                    "{$lookup: { from: 'securities', localField: 'keyCreator', foreignField: 'key', as: 'user' }}",
                    "{$unwind: { path: '$user', preserveNullAndEmptyArrays: true}}",
                    @"{$project:
                                                {
                                                 _id: 0,
                                                 IdBidang: 1,
                                                 Project: 1,
                                                 Desa: 1,
                                                 NoPeta : 1,
                                                 AlasHak : 1,
                                                 keyDocType: 1,
                                                 user: '$user.FullName',
                                                 created: 1,
                                                 activityType: 1,
                                                 modul: 1
                                        }
                                    }",
                    "{$lookup: { from: 'jnsDok', localField: 'keyDocType', foreignField: 'key', as: 'doc' }}",
                    "{$unwind: { path: '$doc', preserveNullAndEmptyArrays: true}}",
                    @"{$project:
                                               {
                                                 _id: 0,
                                                 IdBidang: 1,
                                                 Project: 1,
                                                 Desa: 1,
                                                 NoPeta : 1,
                                                 AlasHak : 1,
                                                 created: 1,
                                                 user: 1,
                                                 activityTypeEnum: '$activityType',
                                                 activityModulEnum: '$modul',
                                                 JenisDokumen: '$doc.identifier'
                                                }
                        }").ToList();
                var logBundle = datas.Select(x => new RptLogBundle()
                {
                    IdBidang = x.IdBidang ?? string.Empty,
                    Project = x.Project ?? string.Empty,
                    Desa = x.Desa ?? string.Empty,
                    NoPeta = x.NoPeta ?? string.Empty,
                    AlasHak = x.AlasHak ?? string.Empty,
                    created = Convert.ToDateTime(x.created).ToLocalTime(),
                    JenisDokumen = x.JenisDokumen ?? string.Empty,
                    user = x.user ?? string.Empty,
                    activityType = Enum.GetName(typeof(LogActivityType), x.activityTypeEnum) ?? string.Empty,
                    activityModul = Enum.GetName(typeof(LogActivityModul), x.activityModulEnum) ?? string.Empty,
                }).OrderByDescending(x => x.created).ToList();


                if (keyword != null)
                {
                    string lowKeyword = keyword.ToLower();
                    logBundle = logBundle.Where(x => x.IdBidang.ToLower().Contains(lowKeyword))
                                .Union(logBundle.Where(x => x.Desa.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.Project.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.NoPeta.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.AlasHak.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.JenisDokumen.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.user.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.activityType.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.activityModul.ToLower().Contains(lowKeyword)))
                                .ToList();
                }

                var xlst = ExpressionFilter.Evaluate(logBundle, typeof(List<RptLogBundle>), typeof(RptLogBundle), gs);
                var data = xlst.result.Cast<RptLogBundle>().ToArray();
                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Tanda Terima Input Bundle CSV
        /// </summary>
        [HttpGet("rpt/log-bundle")]
        public IActionResult GetTandaTerimaInputBundle([FromQuery] string token, [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] string keyword, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = context.GetDocuments(new
                {
                    IdBidang = "",
                    Project = "",
                    Desa = "",
                    NoPeta = "",
                    AlasHak = "",
                    created = DateTime.MinValue,
                    JenisDokumen = "",
                    user = "",
                    activityTypeEnum = 0,
                    activityModulEnum = 0
                }, "logBundle",
                        "{$match: {$and: [ {'created': {$gte: new Date('" + startDate + "')}}, {'created': {$lt: new Date('" + endDate + "')}} ] }}",
                        "{$project : {keyDocType: 1, keyCreator : 1, created : 1, activityType : 1, keyPersil : 1, modul:1}}",
                        "{$lookup: { from: 'persils_v2', localField: 'keyPersil', foreignField: 'key', as : 'persils'}}",
                        "{$unwind: { path: '$persils', preserveNullAndEmptyArrays: true}}",
                        @"{$project:
                                     {
                                        _id: 0,
                                        IdBidang: '$persils.IdBidang',
                                        keyProject: '$persils.basic.current.keyProject',
                                        keyDesa: '$persils.basic.current.keyDesa',
                                        NoPeta: '$persils.basic.current.noPeta',
                                        AlasHak: '$persils.basic.current.surat.nomor',
                                        keyDocType: 1,
                                        keyCreator: 1,
                                        created: 1,
                                        activityType: 1,
                                        modul: 1
                                    },
                         }",

                        "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                        "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",
                        "{$match : {'project': {$ne: []}}}",

                        @"{$project:
                                    {
                                     _id: 0,
                                     IdBidang: 1,
                                     Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                     Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                     NoPeta : 1,
                                     AlasHak : 1,
                                     keyDocType: 1,
                                     keyCreator: 1,
                                     created: 1,
                                     activityType: 1,
                                     modul: 1 
                                    }
                         }",
                        "{$lookup: { from: 'securities', localField: 'keyCreator', foreignField: 'key', as: 'user' }}",
                        "{$unwind: { path: '$user', preserveNullAndEmptyArrays: true}}",
                        @"{$project:
                                    {
                                     _id: 0,
                                     IdBidang: 1,
                                     Project: 1,
                                     Desa: 1,
                                     NoPeta : 1,
                                     AlasHak : 1,
                                     keyDocType: 1,
                                     user: '$user.FullName',
                                     created: 1,
                                     activityType: 1,
                                     modul: 1
                            }
                        }",
                        "{$lookup: { from: 'jnsDok', localField: 'keyDocType', foreignField: 'key', as: 'doc' }}",
                        "{$unwind: { path: '$doc', preserveNullAndEmptyArrays: true}}",
                        @"{$project:
                                   {
                                     _id: 0,
                                     IdBidang: 1,
                                     Project: 1,
                                     Desa: 1,
                                     NoPeta : 1,
                                     AlasHak : 1,
                                     created: 1,
                                     user: 1,
                                     activityTypeEnum: '$activityType',
                                     activityModulEnum: '$modul',
                                     JenisDokumen: '$doc.identifier'
                                    }
                        }").ToList();
                var logBundle = datas.Select(x => new RptLogBundleCsv()
                {
                    IdBidang = "\"" + x.IdBidang + "\"",
                    Project = "\"" + x.Project + "\"",
                    Desa = "\"" + x.Desa + "\"",
                    NoPeta = "\"" + x.NoPeta + "\"",
                    AlasHak = "\"" + x.AlasHak + "\"",
                    created = Convert.ToDateTime(x.created).ToLocalTime().ToString("yyyy/MM/dd hh:mm:ss tt"),
                    JenisDokumen = "\"" + x.JenisDokumen + "\"",
                    user = "\"" + x.user + "\"",
                    activityType = Enum.GetName(typeof(LogActivityType), x.activityTypeEnum),
                    activityModul = Enum.GetName(typeof(LogActivityModul), x.activityModulEnum),
                }).OrderByDescending(x => x.created).ToList();

                if (keyword != null)
                {
                    string lowKeyword = keyword.ToLower();
                    logBundle = logBundle.Where(x => x.IdBidang.ToLower().Contains(lowKeyword))
                                .Union(logBundle.Where(x => x.Desa.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.Project.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.NoPeta.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.AlasHak.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.JenisDokumen.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.user.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.activityType.ToLower().Contains(lowKeyword)))
                                .Union(logBundle.Where(x => x.activityModul.ToLower().Contains(lowKeyword)))
                                .ToList();
                }

                var xlst = ExpressionFilter.Evaluate(logBundle, typeof(List<RptLogBundleCsv>), typeof(RptLogBundleCsv), gs);
                var data = xlst.result.Cast<RptLogBundleCsv>().ToArray();

                var filteredData = data.Evaluate(gs.where, gs.where2, gs.sortColumn, gs.sortOrder);
                var result = filteredData.result.ToArray();
                var sb = MakeCsv(result);
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        [HttpGet("bidang/kategori")]
        public IActionResult GetBidangKategori([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = context.GetDocuments(new
                {
                    IdBidang = "",
                    AlasHak = "",
                    Pemilik = "",
                    Group = "",
                    LuasSurat = (double?)0,
                    LuasInternal = (double?)0,
                    Kategori = new string[0],
                    Bebas = (int?)0
                }, "persils_v2",
                "{$match: {$and : [{'en_state' : {$nin:[2]}} , {'basic.current' : {$ne : null}} ]}}",

                "{$lookup : {from : 'maps', localField: 'basic.current.keyProject', foreignField: 'key', as : 'project'}}",
                "{$match : {'project' : {$ne : []}}}",

                @"{$project : { 
                                _id: 0,
                                IdBidang: 1,
                                AlasHak: '$basic.current.surat.nomor',
                                Pemilik: '$basic.current.pemilik',
                                Group: '$basic.current.group',
                                LuasSurat: '$basic.current.luasSurat',
                                LuasInternal: '$basic.current.luasInternal',
                                Kategori: {$ifNull: [{$arrayElemAt:['$categories', -1]},[]]},
                                Bebas : '$en_state'
                             }
                 }",
                @"{$project : {
                                IdBidang : 1,
                                AlasHak : 1,
                                Pemilik : 1,
                                Group : 1,
                                LuasSurat : 1,
                                LuasInternal : 1,
                                Kategori : '$Kategori.keyCategory',
                                Bebas : 1
                            }
                }").ToList();

                var categories = context.GetDocuments(new { key = "", segment = 0, desc = "" }, "categories_new",
                "{$match:{bebas:0}}",
                @"{$project: {
                                _id:0
                                ,key:1
                                ,segment:1
                                ,desc:1
                             }
                 }");

                var data = datas.Select(x => new RptKategoriBidang()
                {
                    IdBidang = "\"" + x.IdBidang + "\"",
                    AlasHak = "\"" + x.AlasHak + "\"",
                    Pemilik = "\"" + x.Pemilik + "\"",
                    Group = "\"" + x.Group + "\"",
                    LuasSurat = x.LuasSurat,
                    LuasInternal = x.LuasInternal,
                    Kategori = "\"" + string.Join(" ", categories.Join(x.Kategori, cat => cat.key.Trim()
                                                                               , kat => kat ?? ""
                                                                               , (y, z) => new { y.desc, y.segment }
                                          ).OrderBy(y => y.segment)
                                           .Select(y => y.desc)).Replace("{ desc = ", "").Replace(" }", "")
                               + "\"",
                    statusBebas = "\"" + Enum.GetName(typeof(StatusBidang), x.Bebas ?? 0).ToUpper() + "\""
                }).ToList();
                var filteredData = data.Evaluate(gs.where, gs.where2, gs.sortColumn, gs.sortOrder);
                var sb = MakeCsv(filteredData.result.ToArray());

                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("bidang/kategori/paging")]
        public IActionResult GetBidangKategoriPaging([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = context.GetDocuments(new
                {
                    IdBidang = "",
                    AlasHak = "",
                    Pemilik = "",
                    Group = "",
                    LuasSurat = (double?)0,
                    LuasInternal = (double?)0,
                    Kategori = new string[0],
                    Bebas = (int?)0
                }, "persils_v2",
                "{$match: {$and : [{'en_state' : {$nin:[2]}} , {'basic.current' : {$ne : null}} ]}}",

                "{$lookup : {from : 'maps', localField: 'basic.current.keyProject', foreignField: 'key', as : 'project'}}",
                "{$match : {'project' : {$ne : []}}}",

                @"{$project : { 
                                _id: 0,
                                IdBidang: 1,
                                AlasHak: '$basic.current.surat.nomor',
                                Pemilik: '$basic.current.pemilik',
                                Group: '$basic.current.group',
                                LuasSurat: '$basic.current.luasSurat',
                                LuasInternal: '$basic.current.luasInternal',
                                Kategori: {$ifNull: [{$arrayElemAt:['$categories', -1]},[]]},
                                Bebas : '$en_state'
                             }
                 }",
                @"{$project : {
                                IdBidang : 1,
                                AlasHak : 1,
                                Pemilik : 1,
                                Group : 1,
                                LuasSurat : 1,
                                LuasInternal : 1,
                                Kategori : '$Kategori.keyCategory',
                                Bebas : 1
                            }
                }").ToList();

                var categories = context.GetDocuments(new { key = "", segment = 0, desc = "" }, "categories_new",
                    "{$match:{bebas:0}}",
                    @"{$project: {
                                    _id:0
                                   ,key:1
                                   ,segment:1
                                   ,desc:1
                                 }
                    }");

                var data = datas.Select(x => new RptKategoriBidang()
                {
                    IdBidang = x.IdBidang,
                    AlasHak = x.AlasHak,
                    Pemilik = x.Pemilik,
                    Group = x.Group,
                    LuasSurat = x.LuasSurat,
                    LuasInternal = x.LuasInternal,
                    Kategori = string.Join(" ", categories.Join(x.Kategori, cat => cat.key.Trim()
                                                                               , kat => kat ?? ""
                                                                               , (y, z) => new { y.desc, y.segment }
                                          ).OrderBy(y => y.segment)
                                           .Select(y => y.desc)).Replace("{ desc = ", "").Replace(" }", ""),
                    statusBebas = Enum.GetName(typeof(StatusBidang), x.Bebas ?? 0).ToUpper()
                }).ToList();
                var xlst = ExpressionFilter.Evaluate(data, typeof(List<RptKategoriBidang>), typeof(RptKategoriBidang), gs);
                var result = xlst.result.Cast<RptKategoriBidang>().ToArray();
                return Ok(result.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Get tanda terima notaris
        /// </summary>
        [NeedToken("TTDNOT_VIEW")]
        [HttpGet("notary/receipt")]
        public IActionResult GetRptTandaTerimaNotaris([FromQuery] string token, string bundleKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var bundles = contextplus.mainBundles.FirstOrDefault(b => b.key == bundleKey);
                var mappingDocs = context.GetDocuments(new MappingDocs(), "masterdatas",
                                                  "{$match  : {_t  : 'mappingDocs'}}",
                                                  "{$project : {_id : 0, key:1, identifier : 1, jenis : 1, documents : 1, urutan : 1}}"
                                                 );

                var jenisDokumen = context.GetDocuments(new ReportDetail("", ""), "jnsDok",
                       "{$project : { _id :0, Identity:'$key', value:'$identifier' }}"
                     ).ToList();

                var infoPersils = context.GetDocuments(new { IdBidang = "", Desa = "" }, "persils_v2",
                                      "{$match : {key : '" + bundleKey + "'}}",
                                     @"{$project:
                                                 {
                                                    _id: 0,
                                                    IdBidang: '$IdBidang',
                                                    keyProject: '$basic.current.keyProject',
                                                    keyDesa: '$basic.current.keyDesa',
                                                },
                                     }",
                                     @"{$lookup:{
                                        from: 'maps', 
                                               let: { keyProject: '$keyProject',keyDesa: '$keyDesa'}, 
                                               pipeline:[{$unwind: '$villages'},
                                               {$project: { keyProject: '$key',keyDesa: '$villages.key',project: '$identity',desa: '$villages.identity'} },
                                               {$match:
                                               {$expr:
                                                       {$and:[{$eq:['$keyProject','$$keyProject']},
                                                        {$eq:['$keyDesa','$$keyDesa']}]}
                                                }
                                                }], as:'maps' }
                                     }",
                                     @"{$project:
                                                {
                                                 _id: 0,
                                                 IdBidang: 1,
                                                 Desa: {$ifNull:[{$arrayElemAt:['$maps.desa',-1]},'']}
                                                }
                                     }").FirstOrDefault();

                List<RptTandaTerimaNotaris> ListData = new List<RptTandaTerimaNotaris>();
                RptTandaTerimaNotaris data1 = new RptTandaTerimaNotaris();
                List<ReportDetail> detail = new List<ReportDetail>();

                //Add ID Bidang
                detail.Add(new ReportDetail("ID BIDANG", infoPersils.IdBidang));
                data1.Identity = "ID BIDANG";
                data1.orderHeader = 1;
                data1.Details = detail.ToArray();
                ListData.Add(data1);

                //Add Other Docs
                foreach (var item in mappingDocs.OrderBy(m => m.urutan))
                {
                    var listDocument = item.documents.Select(x => x.jenisDocs).ToList();
                    bool isEntriesExist = bundles.doclist.Where(d => listDocument.Contains(d.keyDocType))
                                                    .Any(d => d.entries.Any());
                    int? maxDoc = isEntriesExist ?
                                  bundles.doclist.Where(d => listDocument.Contains(d.keyDocType))
                                                 .Max(d => d.entries.LastOrDefault()?.Item.Count())
                                  : 0;
                    for (int i = 0; i <= maxDoc - 1; i++)
                    {
                        RptTandaTerimaNotaris data = new RptTandaTerimaNotaris();
                        data.Identity = item.identifier;
                        data.orderHeader = item.urutan; ;
                        ReportDetail rptDetail = new ReportDetail();
                        rptDetail.Identity = (rptDetail.Identity == "Alas Hak" ? "AJB" : rptDetail.Identity);
                        rptDetail.Details = GeBundleDetail(item, bundles, jenisDokumen, infoPersils.Desa, i).ToArray();
                        data.Details = rptDetail.Details;
                        ListData.Add(data);
                    }
                }
                return new JsonResult(ListData.OrderBy(x => x.orderHeader));
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

        [HttpGet("bundle/detail")]
        public IActionResult GetRreportBundle([FromQuery] string token, string bundleKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                //var bundlesData = context.GetDocuments(new ReportBundleDetail(), "bundles",
                var bundlesData = context.GetDocuments(new
                {
                    keyBundle = "",
                    keyDocType = "",
                    idBidang = "",
                    docName = "",
                    doclist = new BundledDoc()
                }, "bundles",
                    $"<$match:<key: '{bundleKey}'>>".MongoJs(),
                    "{$unwind: '$doclist'}",
                    @"{$project:
                    {
                        _id: 0,
                        keyBundle: '$key',
                        keyDocType: '$doclist.keyDocType',
                        doclist: '$doclist'
                    }}",
                    "{$lookup: { from: 'persils_v2', localField: 'keyBundle', foreignField: 'key', as : 'persils'} }",
                    "{$unwind: { path: '$persils', preserveNullAndEmptyArrays: true} }",

                    "{$lookup: { from: 'jnsDok', localField: 'keyDocType', foreignField: 'key', as : 'dok'}}",
                    "{$unwind: { path: '$dok', preserveNullAndEmptyArrays: true}}",
                    @"{$project:{
                        _id:0,
                        keyBundle:'$keyBundle',
                        keyDocType:'$keyDocType',
                        idBidang: {$ifNull:['$persils.IdBidang', '']},
                        docName:'$dok.identifier',
                        doclist: '$doclist'
                    }}").ToList();

                List<RegisteredDocView> docList = new();
                foreach (var item in bundlesData)
                {
                    var bundle = item.doclist.entries.LastOrDefault();
                    if (bundle != null)
                    {
                        int itemSet = 0;
                        foreach (var p in bundle.Item)
                        {
                            RegisteredDocView doc = new();
                            doc.docType = item.docName;
                            doc.keyDocType = item.keyDocType;
                            doc.count = itemSet == 0 ? bundle.Item.Count() : 1;

                            var entriesItem = p.Value;
                            doc.SetExistence(entriesItem.exists.Select(s => new KeyValuePair<Existence, int>(s.ex, s.cnt)).ToDictionary(x => x.Key, x => x.Value));
                            doc.SetProperties(entriesItem.props);

                            docList.Add(doc);
                            itemSet++;
                        }
                    }
                    else
                    {
                        RegisteredDocView doc = new();
                        doc.docType = item.docName;
                        doc.keyDocType = item.keyDocType;
                        doc.count = 0;
                        docList.Add(doc);
                    }
                }

                return new JsonResult(docList.OrderBy(o => o.docType));

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

        [HttpGet("assign/sla/view")]
        public IActionResult GetSLAPenugasan([FromQuery] string token, string keyProject, string keyDesa, string keyPTSK)
        {
            try
            {
                var configDocDb = configuration.GetSection("host").GetSection("url").Value;
                var user = contextplus.FindUser(token);
                string url = $"localhost:7788/home/sla?token={token}&kp={keyProject}&kd={keyDesa}&sk={keyPTSK}";
                HtmlToPdf converter = new HtmlToPdf();
                var margin = (int)Math.Round(72d / 2.54);
                // set converter options
                converter.Options.PdfPageSize = PdfPageSize.A3;
                converter.Options.MarginLeft = margin;
                converter.Options.MarginRight = margin;
                converter.Options.MarginTop = margin;
                converter.Options.MarginBottom = margin;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Landscape;
                converter.Options.WebPageWidth = 1121;
                converter.Options.WebPageHeight = 1587;
                PdfDocument doc = converter.ConvertUrl(url);
                var docBase64 = Convert.ToBase64String(doc.Save());
                return Ok(docBase64);
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

        [HttpGet("assign/sla/json")]
        public IActionResult GetSLAPenugasanjson([FromQuery] string token, string kp, string kd, string sk)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var data = GetSLAPenugasan(kp, kd, sk).GetAwaiter().GetResult();
                return new JsonResult(data);
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

        [HttpPost("assign/sla/reminder")]
        public async Task<IActionResult> GetReminderSLAPenugasan()
        {
            try
            {
                var slaProject = await GetSLAPenugasan();
                var data =  new SLAPenugasanViewCsv().ConvertView(slaProject.ToList());

                byte[] resultByte = new byte[0];
                string fileName = @"SLAPenugasan_PENDING_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";

                DataSet sheetSum = data.ToList().ToDataSet(new string[0]);

                DataSExcelSheet[] excelDataSheet = new DataSExcelSheet[] { new DataSExcelSheet { dataS = sheetSum, sheetName = "Summary" } };

                resultByte = DataSetHelper.ExportMultipleDataSetToExcelByte(excelDataSheet);
                var base64 = Convert.ToBase64String(resultByte);
                var alertSettings = context.GetDocuments(new ReminderSiapPembayaran(), "static_collections",
                   "{ $match: { _t: 'ReminderSLAPenugasan' } }",
                   "{ $project: { _id: 0 } }").FirstOrDefault();

                EmailStructure email = new EmailStructure
                {
                    Receiver = alertSettings.receivers.Select(x => x?.email ?? "").ToArray(),
                    Subject = "Reminder SLA Penugasan",
                    Body = @"<h4>Dear All,</h4>
                                    <p>Berikut adalah detail SLA Penugasan yang masih pending. Harap untuk segera diselesaikan.</p>
                                    <p>&nbsp; <br></p>
                                    <p align='left'>Regards,</p>
                                    <p align='left'>Alert Notification SLA Penugasan</p>
                                    ",
                    ContentType = FileType.Excel,
                    Attachment = new List<EmailAttachment> { new EmailAttachment { FileBase64 = base64, FileName = fileName } }
                };

                string url;
                var appsets = Config.AppSettings;
                if (!appsets.TryGet("sendEmail:url", out url))
                    return new InternalErrorResult("Invalid url. Cek kembali file pengaturan anda");

                var myContent = JsonConvert.SerializeObject(email);
                var content = new StringContent(myContent, Encoding.UTF8, "application/json");
                content.Headers.ContentType.CharSet = "";

                HttpClient client = new HttpClient();
                var response = await client.PostAsync(url + "api/mailsender/sendemail", content);

                if (response.IsSuccessStatusCode)
                    return Ok();

                return new UnprocessableEntityObjectResult(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                MyTracer.TraceError2(e);
                return new UnprocessableEntityObjectResult(e.Message);
            }
        }

        private async Task<AssignmentSLAProject[]> GetSLAPenugasan(string keyProject = null, string keyDesa = null, string keyPTSK = null)
        {
            try
            {
                var view = new List<SLAPenugasanView>();
                var builder = Builders<Assignment>.Filter;
                var filter = builder.Ne(e => e.invalid, true);
                if (!string.IsNullOrEmpty(keyProject))
                    filter &= builder.Eq(e => e.keyProject, keyProject);
                if (!string.IsNullOrEmpty(keyDesa))
                    filter &= builder.Eq(e => e.keyDesa, keyDesa);
                if (!string.IsNullOrEmpty(keyPTSK))
                    filter &= builder.Eq(e => e.keyPTSK, keyPTSK);

                var assignments = context.GetCollections(new Assignment(), "assignments", filter, "{_id:0}").ToList().AsParallel();

                var keys = assignments.Select(x => x.instkey).Distinct().ToArray();
                var instkeys = string.Join(',', keys.Select(k => $"{k}"));
                var instaces = ghost.GetMany(instkeys, "").GetAwaiter().GetResult().Cast<GraphMainInstance>().ToArray().AsParallel();

                foreach (var assg in assignments)
                {
                    var details = assg.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
                    var main = instaces.Where(x => x.key == assg.instkey).FirstOrDefault();

                    if (main != null && main.Core != null)
                    {
                        assg.CalcDuration2();
                        (DateTime? duedate, uint? duration) = (assg.duedate, assg.duration);
                        var chain = main.SubChains;
                        var subs = main.children.ToArray();
                        var subchs = subs.Join(chain, s => s.Key, c => c.Value, (s, c) => (sub: s.Value, dk: c.Key)).ToArray();

                        var ndata = details.Join(subchs, d => d.key, s => s.dk, (d, s) =>
                        new SLAPenugasanView
                        {
                            key = d.key,
                            jenisPersil = assg.category.GetValueOrDefault(),
                            keyPersil = d.keyPersil,
                            keyProject = assg.keyProject,
                            keyDesa = assg.keyDesa,
                            keyPTSK = assg.keyPTSK,
                            step = assg.step,
                            mulai = main?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time,
                            selesai = s.sub.states.LastOrDefault(s => s.state == ToDoState.resultArchived_)?.time,
                            duration = duration,
                            target = duedate,
                            overdue = duedate == null ? (int?)null : DateTime.Today <= duedate.Value ? (int?)null : (DateTime.Today - duedate.Value).Days,
                            sisaHari = duedate == null ? (int?)null : (duedate.Value - DateTime.Today).Days

                        }).ToArray().AsParallel();

                        view.AddRange(ndata);
                    }
                }

                var persil = view.Select(x => x.keyPersil).Distinct().ToArray();
                var keyPersils = string.Join(',', persil.Select(k => $"'{k}'"));
                var persils = context.GetDocuments(new PersilSLA(), "persils_v2",
                    $@"<$match:<$and: [<key:<$in:[{keyPersils}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps',let:< key: '$basic.current.keyDesa'>,pipeline:[<$unwind: '$villages'>,<$match: <$expr: <$eq:['$villages.key','$$key']>>>,
                    <$project: <key: '$villages.key', identity: '$villages.identity'>>], as:'desas'>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps', localField:'basic.current.keyProject',foreignField:'key',as:'projects'>>".Replace("<", "{").Replace(">", "}"),
                    @"{$match: {'projects': {$ne : [] }}}",
                    $@"<$project:<
                    key:'$key',
                    IdBidang: '$IdBidang',
                    alasHak : '$basic.current.surat.nomor',
                    keyProject : '$basic.current.keyProject',
                    keyDesa : '$basic.current.keyDesa',
                    keyPTSK : '$basic.current.keyPTSK',
                    desa : <$arrayElemAt:['$desas.identity', -1]>,
                    project: <$arrayElemAt:['$projects.identity',-1]>,
                    luasSurat : '$basic.current.luasSurat',
                    group: '$basic.current.group',
                    pemilik: '$basic.current.pemilik',
                    nama : '$basic.current.surat.nama',
                    _id: 0>>".Replace("<", "{").Replace(">", "}")).ToArray().AsParallel();

                var companies = contextex.ptsk.Query(x => x.invalid != true && x.status == StatusPT.pembeli).ToArray()
                                            .Select(p => new ExtLandropeContext.entitas { key = p.key, identity = p.identifier }).ToArray().AsParallel();

                var datax = view.Join(persils, a => a.keyPersil, p => p.key, (a, p) => a.setBidang(p.IdBidang, p.alasHak,
                    p.pemilik, p.nama, p.luasSurat, p.group, p.project, p.desa, companies.FirstOrDefault(x => x.key == p.keyPTSK)?.identity)).ToArray().AsParallel();

                var nsdata = datax.GroupBy(x => x.project, (y, z) =>
                                            new AssignmentSLAProject
                                            {
                                                Project = y,
                                                Desas = z.GroupBy(x => x.desa, (y, z) =>
                             new AssignmentSLAVillage
                             {
                                 Desa = y,
                                 ptsks = z.GroupBy(x => x.ptsk, (y, z) =>
                                new AssignmentSLAPTSK
                                {
                                    ptsk = y,
                                    types = z.GroupBy(x => (x.step, x.duration), (y, z) =>
                                new AssignmentSLAField
                                {
                                    type = common.Helpers.DocProcessStepDesc2(y.step.GetValueOrDefault()),
                                    SLA = y.duration.ToString(),
                                    bidangs = z.Select(x => new AssigmentBidang
                                    {
                                        jenisPersil = x.jenisPersil.Discriminator(),
                                        IdBidang = x.IdBidang,
                                        AlasHak = x.alashak,
                                        Nama = x.pemilik ?? x.nama,
                                        Luas = x.luas,
                                        Group = x.group,
                                        Mulai = x.mulai,
                                        Target = x.target,
                                        Selesai = x.selesai,
                                        SisaHari = x.sisaHari,
                                        Overdue = x.overdue
                                    }).ToArray()
                                }).ToArray()
                                }).ToArray()
                             }).ToArray()
                                            }).ToArray();

                return nsdata.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("assign/sla")]
        public IActionResult GetSLAPenugasanNew(string token, string keyProject, string keyDesa, string keyPTSK)
        {
            try
            {
                var user = contextplus.FindUser(token);

                var view = new List<SLAPenugasanView>();
                var builder = Builders<Assignment>.Filter;
                var filter = builder.Ne(e => e.invalid, true);
                if (!string.IsNullOrEmpty(keyProject))
                    filter &= builder.Eq(e => e.keyProject, keyProject);
                if (!string.IsNullOrEmpty(keyDesa))
                    filter &= builder.Eq(e => e.keyDesa, keyDesa);
                if (!string.IsNullOrEmpty(keyPTSK))
                    filter &= builder.Eq(e => e.keyPTSK, keyPTSK);

                var assignments = context.GetCollections(new Assignment(), "assignments", filter, "{_id:0}").ToList().AsParallel();

                var keys = assignments.Select(x => x.instkey).Distinct().ToArray();
                var instkeys = string.Join(',', keys.Select(k => $"{k}"));
                var instaces = ghost.GetMany(instkeys, "").GetAwaiter().GetResult().Cast<GraphMainInstance>().ToArray().AsParallel();

                foreach (var assg in assignments)
                {
                    var details = assg.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
                    var main = instaces.Where(x => x.key == assg.instkey).FirstOrDefault();

                    if (main != null && main.Core != null)
                    {
                        assg.CalcDuration2();
                        (DateTime? duedate, uint? duration) = (assg.duedate, assg.duration);
                        var chain = main.SubChains;
                        var subs = main.children.ToArray();
                        var subchs = subs.Join(chain, s => s.Key, c => c.Value, (s, c) => (sub: s.Value, dk: c.Key)).ToArray();

                        var ndata = details.Join(subchs, d => d.key, s => s.dk, (d, s) =>
                        new SLAPenugasanView
                        {
                            key = d.key,
                            jenisPersil = assg.category.GetValueOrDefault(),
                            keyPersil = d.keyPersil,
                            keyProject = assg.keyProject,
                            keyDesa = assg.keyDesa,
                            keyPTSK = assg.keyPTSK,
                            step = assg.step,
                            mulai = main?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time,
                            selesai = s.sub.states.LastOrDefault(s => s.state == ToDoState.resultArchived_)?.time,
                            duration = duration,
                            target = duedate,
                            overdue = duedate == null ? (int?)null : DateTime.Today <= duedate.Value ? (int?)null : (DateTime.Today - duedate.Value).Days,
                            sisaHari = duedate == null ? (int?)null : (duedate.Value - DateTime.Today).Days

                        }).ToArray().AsParallel();

                        view.AddRange(ndata);
                    }
                }

                var persil = view.Select(x => x.keyPersil).Distinct().ToArray();
                var keyPersils = string.Join(',', persil.Select(k => $"'{k}'"));
                var persils = context.GetDocuments(new PersilSLA(), "persils_v2",
                    $@"<$match:<$and: [<key:<$in:[{keyPersils}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps',let:< key: '$basic.current.keyDesa'>,pipeline:[<$unwind: '$villages'>,<$match: <$expr: <$eq:['$villages.key','$$key']>>>,
                    <$project: <key: '$villages.key', identity: '$villages.identity'>>], as:'desas'>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$lookup:<from:'maps', localField:'basic.current.keyProject',foreignField:'key',as:'projects'>>".Replace("<", "{").Replace(">", "}"),
                    $@"<$project:<
                    key:'$key',
                    IdBidang: '$IdBidang',
                    alasHak : '$basic.current.surat.nomor',
                    keyProject : '$basic.current.keyProject',
                    keyDesa : '$basic.current.keyDesa',
                    keyPTSK : '$basic.current.keyPTSK',
                    desa : <$arrayElemAt:['$desas.identity', -1]>,
                    project: <$arrayElemAt:['$projects.identity',-1]>,
                    luasSurat : '$basic.current.luasSurat',
                    group: '$basic.current.group',
                    pemilik: '$basic.current.pemilik',
                    nama : '$basic.current.surat.nama',
                    _id: 0>>".Replace("<", "{").Replace(">", "}")).ToArray().AsParallel();

                var companies = contextex.ptsk.Query(x => x.invalid != true && x.status == StatusPT.pembeli).ToArray()
                                            .Select(p => new ExtLandropeContext.entitas { key = p.key, identity = p.identifier }).ToArray().AsParallel();

                var datax = view.Join(persils, a => a.keyPersil, p => p.key, (a, p) => a.setBidang(p.IdBidang, p.alasHak,
                    p.pemilik, p.nama, p.luasSurat, p.group, p.project, p.desa, companies.FirstOrDefault(x => x.key == p.keyPTSK)?.identity)).ToArray().AsParallel();

                var data = datax.Select(x =>
                                new SLAAssigmentBidang
                                {
                                    Project = x.project,
                                    Desa = x.desa,
                                    ptsk = x.ptsk,
                                    type = common.Helpers.DocProcessStepDesc2(x.step.GetValueOrDefault()),
                                    SLA = x.duration.ToString(),
                                    jenisPersil = x.jenisPersil.SimpleDesc(),
                                    IdBidang = x.IdBidang,
                                    AlasHak = x.alashak,
                                    Nama = x.pemilik ?? x.nama,
                                    Luas = x.luas ?? 0,
                                    Group = x.group,
                                    Mulai = x.mulai ?? DateTime.MinValue,
                                    Target = x.target ?? DateTime.MinValue,
                                    Selesai = x.selesai ?? DateTime.MinValue,
                                    SisaHari = x.sisaHari ?? 0,
                                    Overdue = x.overdue ?? 0
                                }).ToArray();

                return new JsonResult(data.ToArray());
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

        [HttpGet("ddl/project")]
        public IActionResult GetProjectDesaPTSK()
        {
            try
            {
                var maps = context.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identity }).OrderBy(p => p.name).ToList();

                var keys = context.GetCollections(new { keyProject = "" }, "assignments", "{}", "{_id:0,keyProject:1}").ToList().Distinct();

                var data = maps.Join(keys, m => m.key, k => k.keyProject, (m, k) => new cmnItem { key = m.key, name = m.name }).ToList();

                return new JsonResult(data.ToArray());
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet("ddl/desas")]
        public IActionResult GetDesaList(string prokey)

        {
            LandropeContext mcontext = Contextual.GetContext();
            try
            {
                var sample = new { key = "", proj = "", identity = "" };

                var desas = mcontext.GetCollections(sample, "villages", $"{{'project.key':'{prokey}',invalid:{{$ne:true}}}}",
                                                                                "{_id:0,key:'$village.key',proj:'$project.identity', identity:'$village.identity'}").ToList()
                                        .Select(p => new cmnItem { key = p.key, name = $"{p.identity}" }).ToList();

                var keys = context.GetCollections(new { keyDesa = "" }, "assignments", $"{{keyProject:'{prokey}'}}", "{_id:0,keyDesa:1}").ToList().Distinct();

                var data = desas.Join(keys, d => d.key, k => k.keyDesa, (d, k) => new cmnItem { key = d.key, name = d.name }).ToArray();

                return new JsonResult(data.OrderBy(d => d.name).ToArray());
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpGet("ddl/ptsk")]
        public IActionResult GetPTSKList(string prokey, string desa)

        {
            try
            {
                var ptsks = contextex.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var keys = context.GetCollections(new { keyPTSK = "" }, "assignments", $"{{keyProject:'{prokey}', keyDesa:'{desa}'}}", "{_id:0,keyPTSK:1}").ToList().Distinct();

                var data = ptsks.Join(keys, d => d.key, k => k.keyPTSK, (d, k) => new cmnItem { key = d.key, name = d.name }).ToArray();

                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpGet("assign/getmasterstatuspenugasan")]
        public IActionResult GetMasterStatusPenugasan([FromQuery] string token)
        {
            try
            {
                user user = context.FindUser(token);

                string[] templates = new string[] { "PROSESBPN", "SUB_PROSESBPN", "PROSESNOT", "SUB_PROSESNOT" };
                //List<GraphTemplate> gT = templates
                //                                    .Select(s => ghost.GetTemplate(s).GetAwaiter().GetResult())
                //                                    .ToList();

                List<string> stages = new();
                stages.Add($"<$match: <'Core.name': <$in: [{string.Join(",", templates.Select(s => $"'{s}'").ToArray())}] >> >".MongoJs());
                stages.Add(@"{$project : { _id : 0 }}");

                var graphTemplates = context.GetDocuments(new GraphMainInstance(), "graphables", stages.ToArray()).ToList();
                ToDoState[] toDoStates = graphTemplates
                    .SelectMany(sm => sm.Core.nodes, (template, nodes) => new
                    {
                        template = template,
                        nodes = nodes as GraphStatedNode
                    })
                    .Select(s => (ToDoState)s.nodes._state)
                    .ToArray();

                cmnItem[] cmnItems = Enum.GetValues(typeof(ToDoState))
                                    .Cast<ToDoState>()
                                    .Where(w => toDoStates.Contains(w))
                                    .Select(s => new cmnItem()
                                    {
                                        key = ((int)s).ToString(),
                                        name = s.AsStatus()
                                    })
                                    .OrderBy(s => s.name)
                                    .ToArray();
                return new JsonResult(cmnItems);

            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpPost("assign/processpenugasan")]
        public IActionResult ReportProcessPenugasan([FromQuery] string token, [FromQuery] AgGridSettings gs, [FromBody] ReportProcessPenugasanDto dto)
        {
            try
            {
                user user = context.FindUser(token);

                List<string> stages = new();
                if (!string.IsNullOrEmpty(dto.KeyProject))
                    stages.Add($"<$match: <'projectKey': '{dto.KeyProject}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyDesa))
                    stages.Add($"<$match: <'desaKey': '{dto.KeyDesa}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyPtsk))
                    stages.Add($"<$match: <'ptskKey': '{dto.KeyPtsk}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyPic))
                    stages.Add($"<$match: <'picKey': '{dto.KeyPic}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyJenisPenugasan))
                {
                    int.TryParse(dto.KeyJenisPenugasan, out int keyJenis);
                    stages.Add($"<$match: <'assignment.step': {(int)(DocProcessStep)keyJenis} >>".MongoJs());
                }

                if (!string.IsNullOrEmpty(dto.KeyStatus))
                {
                    int.TryParse(dto.KeyStatus, out int keyStatus);
                    stages.Add($"<$match: <'graph.lastState.state': {(int)(DocProcessStep)keyStatus} >>".MongoJs());
                }

                if (!dto.IsDetailMode) // Normal Mode 
                {
                    var result = GetReportPenugasan<List<ViewAssigmentProcess>>(stages.ToArray(), dto.KeyStatus, dto.IsDetailMode);

                    var xlst = ExpressionFilter.Evaluate(result.ToList(), typeof(List<ViewAssigmentProcess>), typeof(ViewAssigmentProcess), gs);
                    var data = xlst.result.Cast<ViewAssigmentProcess>().ToList();

                    return Ok(data.GridFeed(gs));
                }
                else // Detail Mode
                {
                    var result = GetReportPenugasan<List<ViewAssigmentProcessDetail>>(stages.ToArray(), dto.KeyStatus, dto.IsDetailMode);

                    var xlst = ExpressionFilter.Evaluate(result.ToList(), typeof(List<ViewAssigmentProcessDetail>), typeof(ViewAssigmentProcessDetail), gs);
                    var data = xlst.result.Cast<ViewAssigmentProcessDetail>().ToList();

                    return Ok(data.GridFeed(gs));
                }
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [HttpPost("assign/exportprocesspenugasan")]
        public IActionResult ExportReportProcessPenugasan([FromQuery] string token, [FromBody] ReportProcessPenugasanDto dto)
        {
            try
            {
                user user = context.FindUser(token);

                List<string> stages = new();
                if (!string.IsNullOrEmpty(dto.KeyProject))
                    stages.Add($"<$match: <'projectKey': '{dto.KeyProject}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyDesa))
                    stages.Add($"<$match: <'desaKey': '{dto.KeyDesa}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyPtsk))
                    stages.Add($"<$match: <'ptskKey': '{dto.KeyPtsk}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyPic))
                    stages.Add($"<$match: <'picKey': '{dto.KeyPic}'>>".MongoJs());

                if (!string.IsNullOrEmpty(dto.KeyJenisPenugasan))
                {
                    int.TryParse(dto.KeyJenisPenugasan, out int keyJenis);
                    stages.Add($"<$match: <'assignment.step': {(DocProcessStep)keyJenis} >>".MongoJs());
                }

                if (!string.IsNullOrEmpty(dto.KeyStatus))
                {
                    int.TryParse(dto.KeyStatus, out int keyStatus);
                    stages.Add($"<$match: <'graph.lastState.state': {(DocProcessStep)keyStatus} >>".MongoJs());
                }

                if (!dto.IsDetailMode) // Normal Mode 
                {
                    var result = GetReportPenugasan<List<ViewAssigmentProcess>>(stages.ToArray(), dto.KeyStatus, dto.IsDetailMode);
                    return Ok(result.ToArray());
                }
                else // Detail Mode
                {
                    var result = GetReportPenugasan<List<ViewAssigmentProcessDetail>>(stages.ToArray(), dto.KeyStatus, dto.IsDetailMode);
                    return Ok(result.ToArray());
                }
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }
        private IEnumerable<dynamic> GetReportPenugasan<T>(string[] stages, string keyStatus, bool detailMode)
        {
            var slaMaster = context.GetCollections(new SLA(), "timings", "{}", "{_id:0}").ToList();

            var tuntas = ToDoState.complished_;

            int keySts = default;
            if (!string.IsNullOrEmpty(keyStatus)) // filter Status
            {
                int.TryParse(keyStatus, out keySts);
            }

            var rawDatas = context.GetDocuments(new ViewAssignmentJoined(), "view_assignment_joined", stages)
                    .ToList();

            if (!detailMode)
            {
                if (!string.IsNullOrEmpty(keyStatus))
                {
                    if (keySts == -1)
                        rawDatas = rawDatas.Where(w => w.graph.lastState.state != tuntas).ToList();
                    else
                        rawDatas = rawDatas.Where(w => w.graph.lastState.state == (ToDoState)keySts).ToList();
                }

                var assigmentProcess = rawDatas.Select((s, i) =>
                {
                    ViewAssigmentProcess v = new();

                    v.No = i + 1;
                    v.NoSurat = s.assignment.identifier;
                    v.TanggalSurat = s.assignment.created;
                    v.PIC = s.picName;
                    v.Project = s.projectName;
                    v.Desa = s.desaName;
                    v.PT = s.ptskName;
                    v.JenisPenugasan = ((DocProcessStep)s.assignment.step).ToDescription();
                    v.JumlahBidang = s.assignment.details.Count();

                    double? luasSurat = 0d;
                    double? luasNib = 0d;

                    var details = s.assignment.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
                    var main = s.graph;

                    if (main != null && main.Core != null)
                    {
                        (DateTime? duedate, uint? duration) = CalcDuration(s.assignment, s.graph);

                        v.TargetPenugasan = duedate;

                        Persil[] persils = s.persils;
                        MainBundle[] bundles = s.bundles;

                        var persilJoinBundle = (
                            from p in persils
                            join b in bundles on p.key equals b.key into pb
                            from b in pb.DefaultIfEmpty()
                            select new { persil = p, bundle = b }
                        ).ToArray();

                        luasSurat = persils.Select(s => s.basic.current?.luasSurat).Sum();
                        v.LuasSurat = luasSurat;

                        luasNib = persilJoinBundle.Select(s => GetLuasNIB(s.persil, s.bundle)).Sum();
                        v.LuasNib = luasNib;

                        (int doneBidang, double? doneLuas, double? doneProgress) = (0, 0d, 0d);
                        (int outBidang, double? outLuas, double? outProgress) = (v.JumlahBidang, v.LuasSurat, 100d);

                        var nestedDyn = main.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

                        var detailHalf = details.Select(d =>
                        {
                            var instakey = nestedDyn.instakeys.Where(iw => iw.Key == d.key).FirstOrDefault();
                            bool isDone = main.children.Where(cw => cw.Key == instakey.Value).Select(sw => sw.Value.closed).FirstOrDefault();

                            return new
                            {
                                d.key,
                                d.keyPersil,
                                isDone
                            };
                        }).ToArray();

                        Persil[] persilsClosed = persils.Join(detailHalf, p => p.key, d => d.keyPersil, (p, d) => new { persils = p, detail = d })
                                                .Where(w => w.detail.isDone)
                                                .Select(s => s.persils)
                                                .ToArray();

                        bool isZeroClosed = persilsClosed.Count() == 0 ? true : false;

                        doneBidang = isZeroClosed ? 0 : persilsClosed.Count();
                        doneLuas = isZeroClosed ? 0 : persilsClosed.Select(s => s.basic?.current?.luasSurat).Sum();
                        doneProgress = outBidang == 0 ? 0 : ((double)doneBidang / (double)outBidang) * 100;

                        (v.DoneBidang, v.DoneLuas, v.DoneProgress) = (doneBidang, doneLuas, doneProgress);
                        (
                            v.OutBidang,
                            v.OutLuas,
                            v.OutProgress
                        ) = (
                            outBidang - doneBidang,
                            outLuas - doneLuas,
                            outProgress - doneProgress
                        );
                    }

                    return v;
                })
                .Where(w => !string.IsNullOrEmpty(w.Project));

                return assigmentProcess;
            }
            else
            {
                var masterSPS = contextplus.GetCollections(new mod3.SPS(), "sps", "{}", "{_id:0}").ToList();

                List<ViewAssigmentProcessDetail> asgDetail = new();
                int i = 1;
                foreach (ViewAssignmentJoined raw in rawDatas)
                {
                    var details = raw.assignment.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
                    var main = raw.graph;
                    var assg = raw.assignment;
                    var persils = raw.persils;
                    var bundles = raw.bundles;

                    if (main != null && main.Core != null)
                    {
                        (DateTime? duedate, uint? duration) = CalcDuration(assg, main);

                        var nestedDyn = main.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

                        var rawCustom = details.Select(d =>
                        {
                            var instakey = nestedDyn.instakeys.Where(iw => iw.Key == d.key).FirstOrDefault();
                            //ToDoState lastState = main.children.Where(w => w.Key == instakey.Value).Select(s => s.Value.lastState.state).FirstOrDefault();
                            var lastState = main.children.FirstOrDefault(f => f.Key == instakey.Value);

                            return new
                            {
                                d.key,
                                d.keyPersil,
                                lastState,
                                persil = persils.FirstOrDefault(f => f.key == d.keyPersil),
                                bundle = bundles.FirstOrDefault(f => f.key == d.keyPersil)
                            };
                        }).ToArray();

                        if (!string.IsNullOrEmpty(keyStatus)) // filter Status
                        {
                            if (keySts == -1)
                                rawCustom = rawCustom.Where(w => w.lastState.Value.lastState.state != tuntas).ToArray();
                            else
                                rawCustom = rawCustom.Where(w => w.lastState.Value.lastState.state == (ToDoState)keySts).ToArray();
                        }

                        foreach (var data in rawCustom)
                        {
                            Persil persil = data.persil;
                            MainBundle bundle = data.bundle;

                            ViewAssigmentProcessDetail v = new();

                            v.No = i;
                            v.NoSurat = assg.identifier;
                            v.TanggalSurat = assg.created;
                            v.PIC = raw.picName;
                            v.Project = raw.projectName;
                            v.Desa = raw.desaName;
                            v.PT = raw.ptskName;
                            v.JenisPenugasan = ((DocProcessStep)assg.step).ToDescription();

                            v.IdBidang = persil.IdBidang;
                            v.NamaPemilik = persil.basic.current?.pemilik ?? persil.basic.current?.surat?.nama;
                            v.NoAlasHak = persil.basic.current?.surat?.nomor;
                            v.LuasSurat = persil.basic.current?.luasSurat;
                            v.LuasNib = GetLuasNIB(persil, bundle);

                            var sps = masterSPS.Where(w => w.keyPersil == persil.key && w.step == assg.step).FirstOrDefault();

                            v.TanggalTarget = duedate;
                            v.TanggalSPS = sps?.date;// ?? DateTime.MinValue;
                            v.NomorSPS = sps?.nomor;
                            //v.TanggalSelesai = main?.states.LastOrDefault(s => s.state == ToDoState.resultArchived_)?.time;
                            v.TanggalSelesai = data.lastState.Value?.lastState?.state == ToDoState.resultValidated_ ? data.lastState.Value?.lastState?.time : null;
                            v.Overdue = duedate == null ? (int?)null : DateTime.Today <= duedate.Value ? (int?)null : (DateTime.Today - duedate.Value).Days;
                            v.NoProdukSebelumnya = "";
                            v.Kategori1 = "";
                            v.Kategori2 = "";

                            asgDetail.Add(v);
                            i++;
                        }
                    }
                }

                return asgDetail.Where(x => !string.IsNullOrEmpty(x.Project));
            }

            (DateTime? duedate, uint? duration) CalcDuration(ViewAssignment asg, GraphMainInstance graph)
            {
                var count = asg.details.Length;
                if (count < 1)
                    return (asg.duedate, asg.duration);

                uint? duration = null;
                var today = DateTime.Today;
                var projectKey = asg.keyProject;
                var desaKey = asg.keyDesa;
                var slas = slaMaster.Where(s =>
                    s._step == asg.step
                    && (today >= s.since)
                    && (today <= (s.until ?? DateTime.MaxValue))
                    && (s.keyProject == null || s.keyProject == projectKey)
                );
                if (slas.Any(s => s.keyProject == projectKey))
                    slas = slas.Where(s => s.keyProject == projectKey);
                slas = slas.Where(s => s.keyDesa == null || s.keyDesa == desaKey);
                if (slas.Any(s => s.keyDesa == desaKey))
                    slas = slas.Where(s => s.keyDesa == desaKey);

                var sla = slas.OrderByDescending(s => s.since).FirstOrDefault();
                if (sla == null)
                    throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
                var range = sla.ranges.FirstOrDefault(r => count >= (r.from ?? uint.MinValue) && count <= (r.upto ?? uint.MaxValue));
                if (range == null || (range.from == null && range.upto == null))
                    throw new InvalidOperationException("Tabel SLA belum dikonfigurasi dengan benar");
                duration = range.Duration;

                DateTime? accepted = graph?.states.LastOrDefault(s => s.state == ToDoState.accepted_)?.time;
                DateTime? duedate = accepted?.Date.AddDays(duration ?? 0);
                return (duedate, duration);
            }
        }

        //[HttpPost("assign/processpenugasan")]
        //public IActionResult ReportProcessPenugasan([FromQuery] string token, [FromBody] ReportProcessPenugasanDto dto)
        //{
        //    var masterProject = context.GetDocuments(new { key = "", identity = "" }, "projects", "{$project:{_id:0,key:1,identity:1}}").ToList();
        //    var masterDesa = context.GetDocuments(new { key = "", identity = "" }, "villages", "{$project: {_id:0,key:'$village.key',identity:'$village.identity'}}").ToList();
        //    var masterPtsk = context.GetDocuments(new { key = "", identity = "" }, "masterdatas", "{$match: {_t:'ptsk'}}", "{$project : {_id:0,key:1,identity:'$identifier'} }").ToList();
        //    var masterPic = context.GetDocuments(new { key = "", identity = "" }, "masterdatas", "{$match: {_t:'notaris'}}", "{$project : {_id:0,key:1,identity:'$identifier'} }").ToList();
        //    var masterPersil = contextex.GetDocuments(new Persil(), "persils_v2",
        //            "{$project: {_id:0}}").ToList();

        //    var builder = Builders<Assignment>.Filter;
        //    var filter = builder.Ne(e => e.invalid, true);
        //    if (!string.IsNullOrEmpty(dto.KeyProject))
        //        filter &= builder.Eq(e => e.keyProject, dto.KeyProject);
        //    if (!string.IsNullOrEmpty(dto.KeyDesa))
        //        filter &= builder.Eq(e => e.keyDesa, dto.KeyDesa);
        //    if (!string.IsNullOrEmpty(dto.KeyPtsk))
        //        filter &= builder.Eq(e => e.keyPTSK, dto.KeyPtsk);
        //    if (!string.IsNullOrEmpty(dto.KeyJenisPenugasan))
        //    {
        //        int.TryParse(dto.KeyJenisPenugasan, out int keyJenis);
        //        filter &= builder.Eq(e => e.step, (DocProcessStep)keyJenis);
        //    }
        //    if (!string.IsNullOrEmpty(dto.KeyPic))
        //        filter &= builder.Eq( e => e.keyPIC, dto.KeyPic);

        //    var assignments = context.GetCollections(new Assignment(), "assignments", filter, "{_id:0}")
        //                            .ToList()
        //                            .AsParallel()
        //                            ; //.Take(take);
        //    //assignments = assignments.Where( w => w.identifier == "004/BPN/NIBO-GEM/I/2022");
        //    var keys = assignments.Select(x => x.instkey).Distinct().ToArray();
        //    var instkeys = string.Join(',', keys.Select(k => $"{k}"));
        //    var instaces = ghost.GetMany(instkeys, "")
        //                        .GetAwaiter()
        //                        .GetResult()
        //                        .Cast<GraphMainInstance>()
        //                        .ToArray()
        //                        .AsParallel();

        //    try
        //    {
        //        user user = context.FindUser(token);

        //        if (!dto.IsDetailMode) // Normal Mode 
        //        {
        //            if(!string.IsNullOrEmpty(dto.KeyStatus))
        //            {
        //                int.TryParse(dto.KeyStatus, out int keyStatus);
        //                assignments = assignments.Join(instaces, asg => asg.instkey, grp => grp.key, (asg, grp) => new { asgs = asg, graphs = grp })
        //                                        .Where(w => w.graphs.lastState.state == (ToDoState)keyStatus)
        //                                        .Select(s => s.asgs);
        //            }

        //            var assigmentProcess = assignments.Select((assg, i) => new { assg, i })
        //                .Select( s =>
        //                {
        //                    ViewAssigmentProcess v = new();

        //                    v.No = s.i + 1;
        //                    v.NoSurat = s.assg.identifier;
        //                    v.TanggalSurat = s.assg.created;
        //                    v.PIC = masterPic.Where(w => w.key == s.assg.keyNotaris).Select(s => s.identity).FirstOrDefault<string>();
        //                    v.Project = masterProject.Where(w => w.key == s.assg.keyProject).Select(s => s.identity).FirstOrDefault<string>();
        //                    v.Desa = masterDesa.Where(w => w.key == s.assg.keyDesa).Select(s => s.identity).FirstOrDefault<string>();
        //                    v.PT = masterPtsk.Where(w => w.key == s.assg.keyPTSK).Select(s => s.identity).FirstOrDefault<string>();
        //                    v.JenisPenugasan = ((DocProcessStep)s.assg.step).ToDescription();
        //                    v.JumlahBidang = s.assg.details.Count();

        //                    double? luasSurat = 0d;
        //                    double? luasNib = 0d;

        //                    var details = s.assg.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
        //                    var main = instaces.Where(x => x.key == s.assg.instkey).FirstOrDefault();

        //                    if (main != null && main.Core != null)  
        //                    {
        //                        s.assg.CalcDuration2();
        //                        //s.assg.CalcDuration();
        //                        (DateTime? duedate, uint? duration) = (s.assg.duedate, s.assg.duration);
        //                        var chain = main.SubChains;
        //                        var subs = main.children.ToArray();
        //                        var subchs = subs.Join(chain, s => s.Key, c => c.Value, (s, c) => (sub: s.Value, dk: c.Key)).ToArray();

        //                        v.TargetPenugasan = duedate;

        //                        Persil[] persils = details.Select(s => contextex.persils.FirstOrDefault(f => f.key == s.keyPersil)).ToArray();

        //                        luasSurat = persils.Select(s => s.basic.current?.luasSurat).Sum();
        //                        v.LuasSurat = luasSurat;

        //                        luasNib = persils.Select(s => GetLuasNIB(s)).Sum();
        //                        v.LuasNib = luasNib;

        //                        //var nodes = main.Core.nodes.Select(s => (GraphNestedDyn)s).ToArray();

        //                        (int doneBidang, double? doneLuas, double? doneProgress) = (0, 0d, 0d);
        //                        (int outBidang, double? outLuas, double? outProgress) = (v.JumlahBidang, v.LuasSurat, 100d);

        //                        var nestedDyn = main.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

        //                        var detailHalf = details.Select( d =>
        //                        {
        //                            var instakey = nestedDyn.instakeys.Where(iw => iw.Key == d.key).FirstOrDefault();
        //                            //bool isDone = nestedDyn.closeds.Where(cw => cw.Key == instakey.Value).FirstOrDefault().Value;
        //                            bool isDone = main.children.Where(cw => cw.Key == instakey.Value).Select(sw => sw.Value.closed).FirstOrDefault();

        //                            return new
        //                            {
        //                                key = d.key,
        //                                keyPersil = d.keyPersil,
        //                                isDone = isDone
        //                            };
        //                        }).ToArray();

        //                        Persil[] persilsClosed = persils.Join(detailHalf, p => p.key, d => d.keyPersil, (p, d) => new { persils = p, detail = d})
        //                                                .Where( w => w.detail.isDone)
        //                                                .Select( s => s.persils)
        //                                                .ToArray();

        //                        bool isZeroClosed = persilsClosed.Count() == 0 ? true : false;

        //                        doneBidang = isZeroClosed ? 0 : persilsClosed.Count();
        //                        doneLuas = isZeroClosed ? 0 : persilsClosed.Select(s => s.basic?.current?.luasSurat).Sum();
        //                        doneProgress = outBidang == 0 ? 0 : ((double)doneBidang / (double)outBidang) * 100;

        //                        (v.DoneBidang, v.DoneLuas, v.DoneProgress) = (doneBidang, doneLuas, doneProgress);
        //                        (
        //                            v.OutBidang, 
        //                            v.OutLuas, 
        //                            v.OutProgress
        //                        ) = (
        //                            outBidang - doneBidang, 
        //                            outLuas - doneLuas, 
        //                            outProgress - doneProgress
        //                        );
        //                    }
        //                    return v;
        //                })
        //                .Where(v => !string.IsNullOrEmpty(v.Project))
        //                .ToList();
        //            return new JsonResult(assigmentProcess);
        //        }
        //        else // Detail Mode 
        //        {
        //            var masterSPS = contextplus.GetCollections(new mod3.SPS(), "sps", "{}", "{_id:0}").ToList();

        //            List<ViewAssigmentProcessDetail> asgDetail = new();
        //            int i = 1;
        //            foreach (Assignment assg in assignments)
        //            {
        //                var details = assg.details.Select(x => new { key = x.key, keyPersil = x.keyPersil }).ToArray();
        //                var main = instaces.Where(x => x.key == assg.instkey).FirstOrDefault();

        //                if (main != null && main.Core != null)
        //                {
        //                    assg.CalcDuration2();
        //                    //assg.CalcDuration();
        //                    (DateTime? duedate, uint? duration) = (assg.duedate, assg.duration);
        //                    var chain = main.SubChains;
        //                    var subs = main.children.ToArray();
        //                    var subchs = subs.Join(chain, s => s.Key, c => c.Value, (s, c) => (sub: s.Value, dk: c.Key)).ToArray();

        //                    var nestedDyn = main.Core.nodes.OfType<GraphNestedDyn>().FirstOrDefault();

        //                    var rawDatas = details.Select(d =>
        //                    {
        //                        var instakey = nestedDyn.instakeys.Where(iw => iw.Key == d.key).FirstOrDefault();
        //                        //ToDoState lastState = main.children.Where(w => w.Key == instakey.Value).Select(s => s.Value.lastState.state).FirstOrDefault();
        //                        var lastState = main.children.FirstOrDefault( f => f.Key == instakey.Value);

        //                        return new
        //                        {
        //                            d.key,
        //                            d.keyPersil,
        //                            lastState,
        //                            persil = contextex.persils.FirstOrDefault(f => f.key == d.keyPersil)
        //                        };
        //                    }).ToArray();

        //                    if (!string.IsNullOrEmpty(dto.KeyStatus)) // filter Status
        //                    {
        //                        int.TryParse(dto.KeyStatus, out int keyStatus);
        //                        rawDatas = rawDatas.Where(w => w.lastState.Value.lastState.state == (ToDoState)keyStatus).ToArray();
        //                    }

        //                    foreach(var data in rawDatas)
        //                    {
        //                        Persil persil = data.persil;

        //                        ViewAssigmentProcessDetail v = new();

        //                        v.No = i;
        //                        v.NoSurat = assg.identifier;
        //                        v.TanggalSurat = assg.created;
        //                        v.PIC = masterPic.Where(w => w.key == assg.keyNotaris).Select(s => s.identity).FirstOrDefault<string>();
        //                        v.Project = masterProject.Where(w => w.key == assg.keyProject).Select(s => s.identity).FirstOrDefault<string>();
        //                        v.Desa = masterDesa.Where(w => w.key == assg.keyDesa).Select(s => s.identity).FirstOrDefault<string>();
        //                        v.PT = masterPtsk.Where(w => w.key == assg.keyPTSK).Select(s => s.identity).FirstOrDefault<string>();
        //                        v.JenisPenugasan = ((DocProcessStep)assg.step).ToDescription();

        //                        v.IdBidang = persil.IdBidang;
        //                        v.NamaPemilik = persil.basic.current?.pemilik ?? persil.basic.current?.surat?.nama;
        //                        v.NoAlasHak = persil.basic.current?.surat?.nomor;
        //                        v.LuasSurat = persil.basic.current?.luasSurat;
        //                        v.LuasNib = GetLuasNIB(persil);

        //                        var sps = masterSPS.Where(w => w.keyPersil == persil.key && w.step == assg.step).FirstOrDefault();

        //                        v.TanggalTarget = duedate;
        //                        v.TanggalSPS = sps?.date;// ?? DateTime.MinValue;
        //                        v.NomorSPS = sps?.nomor;
        //                        //v.TanggalSelesai = main?.states.LastOrDefault(s => s.state == ToDoState.resultArchived_)?.time;
        //                        v.TanggalSelesai = data.lastState.Value?.lastState?.state == ToDoState.resultValidated_ ? data.lastState.Value?.lastState?.time : null;
        //                        v.Overdue = duedate == null ? (int?)null : DateTime.Today <= duedate.Value ? (int?)null : (DateTime.Today - duedate.Value).Days;
        //                        v.NoProdukSebelumnya = "";
        //                        v.Kategori1 = "";
        //                        v.Kategori2 = "";

        //                        asgDetail.Add(v);
        //                        i++;
        //                    }
        //                }
        //            }
        //            return new JsonResult(asgDetail.Where(x => !string.IsNullOrEmpty(x.Project)).ToList());
        //        }
        //    }
        //    catch (UnauthorizedAccessException exa)
        //    {
        //        return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new InternalErrorResult(ex.Message);
        //    }
        //}

        private double GetLuasNIB(Persil persil, MainBundle bundle)
        {
            //var bundle = contextplus.mainBundles.FirstOrDefault(b => b.key == persil.key);

            if (bundle == null)
                return 0;

            //var Doc = bundle.Current().FirstOrDefault(b => b.keyDocType == "JDOK032");

            //if (Doc == null)
            //	return null;

            var doclist = bundle.doclist.FirstOrDefault(b => b.keyDocType == "JDOK032");
            if (doclist == null)
                return 0;

            var entry = doclist.entries.LastOrDefault();
            if (entry == null)
                return 0;

            //var part = Doc.docs.ToArray().FirstOrDefault().Value;
            var part = entry.Item.FirstOrDefault().Value;
            if (part == null)
                return 0;

            var typename = MetadataKey.Luas.ToString("g");
            var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
            var castvalue = dyn?.Value;

            double result = 0;
            if (castvalue != null)
                result = Convert.ToDouble(castvalue);

            return result;
        }

        /// <summary>
        /// Get Data for New Surat tugas
        /// </summary>
        [HttpGet("surat/tugas/new")]
        public IActionResult GetSuratTugasNew([FromQuery] string token, string assignKey)
        {
            try
            {
                user user = new();
                if (!string.IsNullOrEmpty(token))
                    user = context.FindUser(token);
                var assignment = contextplus.assignments.FirstOrDefault(a => a.key == assignKey);
                if (assignment == null)
                    return new UnprocessableEntityObjectResult("Penugasan tidak tersedia !");

                var village = contextplus.GetVillage(assignment.keyDesa);
                var villages = contextplus.GetVillages();
                var notaris = context.GetCollections(new Notaris(), "masterdatas", "{_t: 'notaris', invalid:{$ne:true}}", "{_id:0}").ToList();
                var internals = context.GetCollections(new mod2.Internal(), "masterdatas", "{_t: 'internal', invalid:{$ne:true}}", "{_id:0}").ToList();
                var ptsk = context.GetCollections(new PTSK(), "masterdatas", "{_t: 'ptsk', invalid:{$ne:true}}", "{_id:0}").ToList();

                var bidangsAssigned = assignment.details.Select(bid => bid.keyPersil);
                string keyPersils = string.Join(",", bidangsAssigned);
                keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                var bayars = context.GetDocuments(new { keyPersil = "", tahap = 0 }, "bayars",
                                                "{$match: {$and: [{'bidangs': {$ne:[]}}, {'invalid': {$ne:true}} ]} }",
                                                "{$unwind: '$bidangs'}",
                                                "{$match: {'bidangs.keyPersil': {$in: [" + keyPersils + "]}}}",
                                                "{$project: { _id:0, tahap:'$nomorTahap', keyPersil:'$bidangs.keyPersil' }}"
                                                ).ToList();

                var persils = context.db.GetCollection<Persil>("persils_v2").Find("{key:{$in :[" + keyPersils + "]}}").ToList();

                if (persils.Count() == 0)
                    return new UnprocessableEntityObjectResult($"Gagal Memuat Report: Data Bidang tidak ditemukan");

                List<(string key, dynamic value)> bundleShm = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK036", keyPersils);
                List<(string key, dynamic value)> bundleShgb = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK037", keyPersils);

                List<(string key, dynamic value)> bundleShgbFinal = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK054", keyPersils);
                List<(string key, dynamic value)> bundleShp = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK038", keyPersils);

                List<(string key, dynamic value)> bundleSK = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK047", keyPersils);
                List<(string key, dynamic value)> bundlePengantarSK = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK057", keyPersils);

                List<(string key, dynamic value)> bundleNoPBTPT = GetDataFromBundle(MetadataKey.Nomor_PBT.ToString("g"), "JDOK050", keyPersils);
                List<(string key, dynamic value)> bundleLuasPBTPT = GetDataFromBundle(MetadataKey.Luas.ToString("g"), "JDOK050", keyPersils);

                List<(string key, dynamic value)> bundleLuasPBTPerorang = GetDataFromBundle(MetadataKey.Luas.ToString("g"), "JDOK032", keyPersils);
                List<(string key, dynamic value)> bundleBPHTBPT = GetDataFromBundle(MetadataKey.Nilai.ToString("g"), "JDOK031", keyPersils);

                List<(string key, dynamic value)> bundleBPHTBWaris = GetDataFromBundle(MetadataKey.Nilai.ToString("g"), "JDOK041", keyPersils);

                List<(string key, dynamic value)> bundlePPJB = GetDataFromBundle(MetadataKey.Nilai.ToString("g"), "JDOK019", keyPersils);

                List<(string key, dynamic value)> nomorSPHs = GetDataFromBundle(MetadataKey.Nomor.ToString("g"), "JDOK022", keyPersils);

                var summaries = persils.Select(p => new SuratTugasBidangSummary()
                {
                    IdBidang = p.IdBidang,
                    Project = villages.FirstOrDefault(v => v.desa.key == p?.basic?.current?.keyDesa).project.identity,
                    Desa = villages.FirstOrDefault(v => v.desa.key == p?.basic?.current?.keyDesa).desa.identity,
                    Seller = p?.basic?.current?.pemilik ?? p?.basic?.current?.surat?.nama,
                    Tahap = bayars.FirstOrDefault(b => b.keyPersil == p.key)?.tahap ?? 0,
                    NomorPBTPT = Convert.ToString(bundleNoPBTPT.FirstOrDefault(pbt => pbt.key == p.key).value ?? null),
                    NomorPengantarSK = Convert.ToString(bundlePengantarSK.FirstOrDefault(sk => sk.key == p.key).value ?? null),
                    NomorSK = Convert.ToString(bundleSK.FirstOrDefault(sk => sk.key == p.key).value ?? null),
                    NilaiBPHTB = Convert.ToDouble(bundleBPHTBPT.FirstOrDefault(bphtb => bphtb.key == p.key).value ?? bundleBPHTBWaris.FirstOrDefault(bphtb => bphtb.key == p.key).value ?? 0),
                    AlasHak = p.basic.current.surat.nomor,
                    Shm = Convert.ToString(bundleShm.FirstOrDefault(s => s.key == p.key).value ?? null),
                    Shgb = Convert.ToString(bundleShgbFinal.FirstOrDefault(s => s.key == p.key).value ?? bundleShgb.FirstOrDefault(s => s.key == p.key).value),
                    LuasPBTPT = Convert.ToDouble(bundleLuasPBTPT.FirstOrDefault(pbt => pbt.key == p.key).value ?? 0),
                    LuasPBTPerorang = Convert.ToDouble(bundleLuasPBTPerorang.FirstOrDefault(pbt => pbt.key == p.key).value ?? 0),
                    LuasSurat = p.basic.current.luasSurat.GetValueOrDefault(0),
                    Satuan = (Convert.ToDouble(bundlePPJB.FirstOrDefault(ppjb => ppjb.key == p.key).value ?? 0) / p.basic.current.luasSurat.GetValueOrDefault(1) == 0) ? p?.basic?.current?.satuan.GetValueOrDefault(0) : (Convert.ToDouble(bundlePPJB.FirstOrDefault(ppjb => ppjb.key == p.key).value ?? 0) / p.basic.current.luasSurat.GetValueOrDefault(1)),
                    NilaiTransaksi = Convert.ToDouble(bundlePPJB.FirstOrDefault(ppjb => ppjb.key == p.key).value) == 0 ? (p.basic.current.luasSurat.GetValueOrDefault(0) * p?.basic?.current?.satuan.GetValueOrDefault(0)) : Convert.ToDouble(bundlePPJB.FirstOrDefault(ppjb => ppjb.key == p.key).value ?? 0),
                    PPJBTo = ptsk.FirstOrDefault(pt => pt.key == assignment?.keyPenampung)?.identifier,
                    PTSK = ptsk.FirstOrDefault(pt => pt.key == (p?.basic?.current?.keyPTSK ?? ""))?.identifier,
                    NomorSPH = Convert.ToString(nomorSPHs.FirstOrDefault(sph => sph.key == p.key).value ?? null)
                }).ToArray();

                GraphMainInstance instance = ghost.Get(assignment.instkey).GetAwaiter().GetResult();

                var pics = notaris.Select(n => (n.key, n.identifier, "notaris", "")).Union(internals.Select(i => (i.key, i.identifier, "internal", i?.salutation ?? ""))).ToList();
                var pic = pics != null ? pics.FirstOrDefault(x => x.key == assignment.keyPic) : ("", "", "", "");

                string picName = (!instance.Core.name.Contains("BPN") || pic.Item3 == "notaris") ? "Notaris/PPAT\r\n" : "";
                picName += $"{pic.Item4} {pic.Item2}";

                string templateMain = instance.Core.name.Contains("BPN") ? "SuratTugas" : "OrderNotaris";
                double luasSurat = summaries.Sum(x => x.LuasSurat);

                string jenisSurat = assignment?.step == DocProcessStep.SK_BPN ? (luasSurat > 2000000) ? $"{templateMain}-SKKanwil" : $"{templateMain}-SKKantah" :
                                    $"{SetJenisSuratExt(assignment, templateMain)}";

                string title = instance.Core.name.Contains("BPN") ? "SURAT TUGAS" : "SURAT ORDER NOTARIS";

                var dataStatic = context.GetDocuments(new SuratTugasStatic(), "static_collections",
                   $"<$match: <_t: '{jenisSurat.Replace("Girik", "").Replace("Sertifikat", "")}'>>".MongoJs(),
                    "{$project: {_id: 0, kepada:0, _t:0}}");

                var listSignersStatic = dataStatic.FirstOrDefault()?.signs != null ? dataStatic.FirstOrDefault()?.signs.ToList() : new List<string>();
                string tembusan = "";
                var internal_pics = internals.FirstOrDefault(x => x.key == assignment.keyPic);
                if (internal_pics != null && internal_pics?.tembusan.Count() != 0)
                {
                    var listTembusan = internal_pics.tembusan.Join(internals, temb => temb.Trim(), intern => intern.key, (temb, intern) => new mod2.Internal()
                    {
                        key = temb,
                        identifier = $"{intern?.salutation + " " ?? ""}{intern.identifier}"
                    }).ToList();
                    tembusan += listTembusan.Count != 0 ? "\r\n Cc. " + listTembusan?.FirstOrDefault()?.identifier : "";
                    tembusan += string.Join("\r\n", listTembusan.Skip(1).Select(x => x?.identifier));
                    picName += tembusan;
                }

                var notes = dataStatic.FirstOrDefault()?.notes != null ?
                            dataStatic.FirstOrDefault()?.notes.Select(x => new SuratTugas2Dim()
                            {
                                text = x
                            }).ToArray() : new SuratTugas2Dim[0];

                string prevAssignNumber = GetPreviousAssignNumber(keyPersils, assignment.created ?? new DateTime());
                if (jenisSurat == "SuratTugas-PBTPT" && !string.IsNullOrEmpty(prevAssignNumber))
                {
                    var notesExt = notes.ToList();
                    SuratTugas2Dim newNotes = new()
                    {
                        text = $"sebelumnya sudah pernah dibuatkan surat tugas dengan nomor {prevAssignNumber}"
                    };
                    notesExt.Add(newNotes);
                    notes = notesExt.ToArray();
                }

                var perihals = dataStatic.FirstOrDefault()?.perihal != null ?
                              dataStatic.FirstOrDefault()?.perihal.Select(x => new SuratTugas2Dim()
                              {
                                  text = x
                              }).ToList() : new List<SuratTugas2Dim>();

                if (perihals.Count() == 0)
                {
                    SuratTugas2Dim perihal = new SuratTugas2Dim();
                    perihal.text = assignment.step?.PerihalSurat();
                    perihals.Add(perihal);
                }

                int totalGroup = 0;
                var totalByNomorPBT = summaries.Select(s => (s.NomorPBTPT, s.LuasPBTPT)).Distinct();
                var totalByPengantarSK = summaries.Select(s => (s.NomorPengantarSK, s.LuasPBTPT)).Distinct();
                var totalBySK = summaries.Select(s => (s.NomorSK, s.LuasPBTPT)).Distinct();
                double totalLuas = 0;

                if (jenisSurat.ToLower().Contains("surattugas-skkantah") || jenisSurat.ToLower().Contains("surattugas-pengantarsk"))
                {
                    summaries = summaries.Select(s => (s.Project, s.PTSK, s.Desa, s.NomorPBTPT, s.LuasPBTPT, s.IdBidang))
                                                .GroupBy(s => new { s.NomorPBTPT, s.LuasPBTPT, s.PTSK, s.Project, s.Desa })
                                                .Select(s => new SuratTugasBidangSummary
                                                {
                                                    Project = s.Key.Project,
                                                    Desa = s.Key.Desa,
                                                    PTSK = s.Key.PTSK,
                                                    NomorPBTPT = s.Key.NomorPBTPT,
                                                    LuasPBTPT = s.Key.LuasPBTPT,
                                                    JumlahBidang = s.Select(x => x.IdBidang).Count()
                                                }).ToArray();

                }
                else if (jenisSurat.ToLower().Contains("surattugas-skkanwil"))
                {
                    summaries = summaries.Select(s => (s.Project, s.PTSK, s.Desa, s.NomorPengantarSK, s.LuasPBTPT, s.IdBidang))
                                                .GroupBy(s => new { s.NomorPengantarSK, s.PTSK, s.Project, s.Desa })
                                                .Select(s => new SuratTugasBidangSummary
                                                {
                                                    Project = s.Key.Project,
                                                    Desa = s.Key.Desa,
                                                    PTSK = s.Key.PTSK,
                                                    NomorPengantarSK = s.Key.NomorPengantarSK,
                                                    LuasPBTPT = (double)summaries.Where(x => x.NomorPengantarSK == s.Key.NomorPengantarSK
                                                                                 && x.Project == s.Key.Project
                                                                                 && x.Desa == s.Key.Desa
                                                                                 && x.PTSK == s.Key.PTSK
                                                                               )
                                                                         .Select(x => (x.LuasPBTPT, x.NomorPBTPT))
                                                                         .GroupBy(x => new { x.NomorPBTPT, x.LuasPBTPT })
                                                                         .Select(x => x.First()).Sum(x => x.LuasPBTPT),
                                                    JumlahBidang = s.Select(x => x.IdBidang).Count()
                                                }).ToArray();
                }
                else if (jenisSurat.ToLower().Contains("surattugas-cetakbuku"))
                {
                    summaries = summaries.Select(s => (s.Project, s.PTSK, s.Desa, s.NomorSK, s.LuasPBTPT, s.IdBidang))
                                                .GroupBy(s => new { s.NomorSK, s.PTSK, s.Project, s.Desa })
                                                .Select(s => new SuratTugasBidangSummary
                                                {
                                                    Project = s.Key.Project,
                                                    Desa = s.Key.Desa,
                                                    PTSK = s.Key.PTSK,
                                                    NomorSK = s.Key.NomorSK,
                                                    LuasPBTPT = (double)summaries.Where(x => x.NomorSK == s.Key.NomorSK
                                                                                   && x.Project == s.Key.Project
                                                                                   && x.Desa == s.Key.Desa
                                                                                   && x.PTSK == s.Key.PTSK
                                                                               )
                                                                         .Select(x => (x.LuasPBTPT, x.NomorPBTPT))
                                                                         .GroupBy(x => new { x.NomorPBTPT, x.LuasPBTPT })
                                                                         .Select(x => x.First()).Sum(x => x.LuasPBTPT),
                                                    JumlahBidang = s.Select(x => x.IdBidang).Count()
                                                }).ToArray();
                }
                else if (jenisSurat.ToLower().Contains("kantah") || jenisSurat.ToLower().Contains("pengantarsk"))
                {
                    totalGroup = totalByNomorPBT.Count();
                    totalLuas = totalByNomorPBT.Sum(s => s.LuasPBTPT);
                }
                else if (jenisSurat.ToLower().Contains("kanwil"))
                {
                    totalGroup = totalByPengantarSK.Count();
                    totalLuas = totalByPengantarSK.Sum(s => s.LuasPBTPT);
                }
                else
                {
                    totalGroup = totalBySK.Count();
                    totalLuas = totalBySK.Sum(s => s.LuasPBTPT);
                }

                List<SuratTugas1Dim> listdata1Dim = new List<SuratTugas1Dim>();
                SuratTugas1Dim data1Dim = new SuratTugas1Dim
                {
                    IsPreview = (String.IsNullOrEmpty(token)),
                    TanggalSurat = DateTime.Now,
                    TitleSurat = title,
                    NomorSurat = assignment.identifier,
                    JenisSurat = jenisSurat,
                    Project = village.project?.identity,
                    Desa = village.desa?.identity,
                    PIC = picName,
                    PTSK = ptsk.FirstOrDefault(pt => pt.key == assignment?.keyPTSK)?.identifier,
                    Total = totalGroup,
                    TotalLuas = totalLuas,
                    Signers1 = user?.FullName ?? "",
                    Signers2 = dataStatic.FirstOrDefault()?.signs.Skip(0)?.FirstOrDefault(),
                    Signers3 = dataStatic.FirstOrDefault()?.signs.Skip(1)?.FirstOrDefault()
                };
                listdata1Dim.Add(data1Dim);

                SuratTugas data = new SuratTugas();
                (
                    data.Perihal,
                    data.Notes,
                    data.SummaryBidang,
                    data.Data
                )
                    =
                (
                    perihals.ToArray(),
                    notes,
                    summaries,
                    listdata1Dim.ToArray()
                );

                return Ok(data);
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


        [HttpGet("bundle/prabebas")]
        public IActionResult GetReportBundle([FromQuery] string token)
        {
            try
            {
                var user = context.FindUser(token);

                var bundlePraBebas = context.GetDocuments(new PraBebasDetail(), "praDeals",
                    "{$match: {'invalid': {$ne: true}}}",
                    "{$unwind: '$details'}",
                    "{$match: {'details.keyPersil': {$ne: null}}}",
                    "{$lookup: {from: 'persils_v2', localField: 'details.keyPersil', foreignField: 'key', as: 'persil'}}",
                    "{$unwind: { path: '$persil',preserveNullAndEmptyArrays: true}}",
                    "{$lookup: {from: 'maps',let: { key: '$persil.basic.current.keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$unwind: { path: '$desas',preserveNullAndEmptyArrays: true}}",
                    "{$lookup: {from: 'maps', localField: 'persil.basic.current.keyProject',foreignField: 'key',as:'projects'}}",
                    "{$unwind: { path: '$projects',preserveNullAndEmptyArrays: true}}",
                   @"{$project: {
                         _id: 0,
                         key: '$details.keyPersil',
                         Flag: 'bundle',
                         IdBidang: {$ifNull: ['$persil.IdBidang', '(Ada di Server lain)']},
                         Project: '$projects.identity',
                         Desa: '$desas.identity',
                         AlasHak: '$persil.basic.current.surat.nomor',
                         Pemilik: '$persil.basic.current.pemilik',
                         Group: '$persil.basic.current.group',
                         LuasSurat: '$persil.basic.current.luasSurat'
                     }}",
                  @"{$unionWith: {    
                         'coll': 'praDeals',
                         pipeline: [
                                 {$match: {'invalid': {$ne: true}}},
                                 {$unwind: '$details'},
                                 {$match: {'details.keyPersil': {$eq: null}}},
        
                                 {$project: {
                                     _id: 0,
                                     key: '$details.key',
                                     Flag: 'preBundle',
                                     IdBidang: '(Bidang belum ditentukan)',
                                     Project: '',
                                     Desa: '$details.desa',
                                     AlasHak: '$details.alasHak',
                                     Pemilik: '$details.pemilik',
                                     Group: '$details.group',
                                     LuasSurat: '$details.luasSurat'
                                 }}
                             ]
                     }}");

                string keyPersilsBundle = string.Join(",", bundlePraBebas.Where(x => x.Flag == "bundle").Select(x => $"'{x.key}'"));
                string keyPersilsPreBundle = string.Join(",", bundlePraBebas.Where(x => x.Flag == "preBundle").Select(x => $"'{x.key}'"));

                var bundles = context.GetDocuments(new MainBundle(), "bundles",
                    "{$match: {key: {$in: [ " + keyPersilsBundle + " ]}}}", "{$project:{_id: 0}}").ToList();

                var preBundles = context.GetDocuments(new PreBundle(), "bundles",
                    "{$match: {key: {$in: [ " + keyPersilsPreBundle + " ]}}}", "{$project:{_id: 0}}").ToList();

                var bundleReport = bundles.Join(bundlePraBebas, b => b.key,
                                                    bp => bp.key,
                                                    (b, bp) => new { X = b, Y = bp })
                    .SelectMany(x => x.X.doclist, (a, b) => new
                    {
                        docReport = b.ToReport(
                        a.Y.Project,
                        a.Y.Desa,
                        a.Y.Pemilik,
                        a.Y.IdBidang,
                        a.Y.LuasSurat,
                        a.Y.AlasHak,
                        a.Y.Group)
                    }).Select(x => x.docReport)
                        .Union(
                    preBundles.Join(bundlePraBebas, b => b.key,
                                                    bp => bp.key,
                                                    (b, bp) => new { X = b, Y = bp })
                            .SelectMany(x => x.X.doclist, (a, b) => new
                            {
                                docReport = b.ToReport(
                                a.Y.Project,
                                a.Y.Desa,
                                a.Y.Pemilik,
                                a.Y.IdBidang,
                                a.Y.LuasSurat,
                                a.Y.AlasHak,
                                a.Y.Group)
                            }).Select(x => x.docReport)
                    ).ToList();

                var sb = MakeCsv(bundleReport.ToArray());
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
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

        [NeedToken("RPT_MANDATORY_DOC")]
        [HttpGet("mandatory-document")]
        public async Task<IActionResult> GetMandatoryDocumentReport([FromQuery] string token, [FromQuery] AgGridSettings gs, bool isReport = false)
        {
            try
            {
                user user = context.FindUser(token);

                string[] stageQuery = new[] {
                    "{ $unwind: '$details' }",
                    "{ $lookup: { from: 'docSettings', localField: 'details.key', foreignField: 'keyDetail', as: 'docSetting' } }",
                    "{ $unwind: { path: '$docSetting', preserveNullAndEmptyArrays: true } }",
                    "{ $unwind: '$docSetting.docsSetting' }",
                    "{ $match: { $or: [{ 'docSetting.docsSetting.UTJ': true }, { 'docSetting.docsSetting.DP': true }, { 'docSetting.docsSetting.Lunas': true }] } }",

                    "{ $addFields: { sKey: { $ifNull : ['$details.keyPersil', '$details.key' ] } } }",
                    "{ $lookup: { from: 'bundles', localField: 'sKey', foreignField: 'key', as: 'bundle' } }",
                    "{ $unwind: { path: '$bundle', preserveNullAndEmptyArrays: true } }",
                    "{ $lookup: { from: 'jnsDok', localField: 'docSetting.docsSetting.keyDocType', foreignField: 'key', as: 'doc' } }",
                    "{ $unwind: '$doc' }",
                    "{ $lookup: { from: 'villages', localField: 'details.keyDesa', foreignField: 'village.key', as: 'md' } }",
                    "{ $unwind: '$md' }",
                    "{ $lookup: { from: 'bayars', localField: 'details.keyPersil', foreignField: 'bidangs.keyPersil', as: 'bayar' } }",
                    "{ $unwind: { path: '$bayar', preserveNullAndEmptyArrays: true } }",

                    "{ $addFields: { BundleDocList: { $first: { $filter: { input: '$bundle.doclist', as: 'x', cond: { $eq: ['$$x.keyDocType', '$docSetting.docsSetting.keyDocType'] } } } } } }",
                    "{ $addFields: { BundleLastEntries: { $last: '$BundleDocList.entries' } } }",
                    "{ $addFields: { TotalSet: { $cond: [ { $ifNull: ['$BundleLastEntries.Item', false ] }, { $size: { $objectToArray: '$BundleLastEntries.Item' } }, 0] } } }",
                    @"{ $project: { _id: 0,
                            KeyPersil: '$details.keyPersil',
                            DocType: '$doc.identifier',
                            IdBidang: '$bundle.IdBidang',
                            Project: '$md.project.identity',
                            Desa: '$md.village.identity',
                            Tahap: '$bayar.nomorTahap',
                            AlasHak: '$details.alasHak',
                            Group: '$details.group',
                            Pemilik: '$details.pemilik',
                            LuasSurat: '$details.luasSurat',
                            Bayar: '$bayar',
                            UTJ: { $cond: ['$docSetting.docsSetting.UTJ', { $cond: [{ $gte: [ '$TotalSet', '$docSetting.docsSetting.totalSetUTJ' ]}, 'Y/L', 'Y/B'] }, 'N'] },
                            DP: { $cond: ['$docSetting.docsSetting.DP', { $cond: [{ $gte: [ '$TotalSet', '$docSetting.docsSetting.totalSetDP' ]}, 'Y/L', 'Y/B'] }, 'N'] },
                            Lunas: { $cond: ['$docSetting.docsSetting.Lunas', { $cond: [{ $gte: [ '$TotalSet', '$docSetting.docsSetting.totalSetLunas' ]}, 'Y/L', 'Y/B'] }, 'N'] },
                            StatusDocSetting: '$docSetting.status',
                    } }"
                };
                //KeyDocType: '$docSetting.docsSetting.keyDocType',
                //KeyDocSetting: '$docSetting.keyDetail',


                IEnumerable<MandatoryDocumentReportModel> core = await context.GetDocumentsAsync(new MandatoryDocumentReportModel(), "praDeals", stageQuery);
                var data = core.Select(x => x.ToViewModel());

                if (isReport)
                    return Ok(data.ToList());

                var xlst = ExpressionFilter.Evaluate(data, typeof(List<MandatoryDocumentReportViewModel>), typeof(MandatoryDocumentReportViewModel), gs);
                var datas = xlst.result.Cast<MandatoryDocumentReportViewModel>().ToList();
                var result = datas.GridFeed(gs);

                return Ok(result);
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

        static StringBuilder MakeCsv<T>(T[] reportData)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
            var header = string.Join(",", props.ToList().Select(x => x.Name));
            lines.Add(header);

            var valueLines = reportData.Select(row => string.Join(",", header.Split(',').Select(a => row.GetType().GetProperty(a).GetValue(row, null))));
            lines.AddRange(valueLines);

            foreach (string item in lines)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        private class Bundle
        {
            public string key { get; set; }
            public int arrayIndex { get; set; }
            public string IdBidang { get; set; }
            public string storkey { get; set; }
            public string keyDocType { get; set; }
            public string JenisDok { get; set; }
            public string NamaField { get; set; }
            public string value { get; set; }
            public int Asli { get; set; }
            public int Copy { get; set; }
            public int legalisir { get; set; }
            public int salinan { get; set; }
            public int softcopy { get; set; }
        }

        private class BundleView
        {
            public string Project { get; set; }
            public string Desa { get; set; }
            public string PTSK { get; set; }
            public string IdBidang { get; set; }
            public string JenisDok { get; set; }
            public int A { get; set; }
            public int C { get; set; }
            public int L { get; set; }
            public int S { get; set; }
            public int SC { get; set; }
            public string Nomor { get; set; }
            public string Tahun { get; set; }
            public string Nama { get; set; }
            public string Luas { get; set; }
            public string Nilai { get; set; }
            public string Lunas { get; set; }
            public string Jenis { get; set; }
            public string Due_Date { get; set; }
            public string NIK { get; set; }
            public string Nomor_KK { get; set; }
            public string Tanggal_Bayar { get; set; }
            public string Tanggal_Validasi { get; set; }
            public string Tanggal { get; set; }
            public string Nama_Lama { get; set; }
            public string Nama_Baru { get; set; }
            public string NOP { get; set; }
            public string Nomor_NIB { get; set; }
            public string Nomor_PBT { get; set; }
            public string NTPN { get; set; }
            public string Nama_Notaris { get; set; }
            public string Keterangan { get; set; }
            public string Lainnya { get; set; }
        }

        private class BundleViewExt : BundleView
        {
            public string key { get; set; }
            public string keyDocType { get; set; }
            public string keyDesa { get; set; }
            public string keyPTSK { get; set; }

            public BundleView toView()
            {
                var view = new BundleView();
                (view.Project, view.Desa, view.PTSK, view.IdBidang, view.JenisDok, view.A, view.C, view.L, view.S, view.SC, view.Nomor, view.Tahun, view.Nama, view.Luas, view.Nilai, view.Lunas,
                    view.Jenis, view.Due_Date, view.NIK, view.Nomor_KK, view.Tanggal_Bayar, view.Tanggal_Validasi, view.Tanggal, view.Nama_Lama, view.Nama_Baru, view.NOP, view.Nomor_NIB, view.Nomor_PBT,
                    view.NTPN, view.Nama_Notaris, view.Keterangan, view.Lainnya) =
                (Project, Desa, PTSK, IdBidang, JenisDok, A, C, L, S, SC, Nomor, Tahun, Nama, Luas, Nilai, Lunas, Jenis, Due_Date, NIK, Nomor_KK, Tanggal_Bayar, Tanggal_Validasi, Tanggal,
                    Nama_Lama, Nama_Baru, NOP, Nomor_NIB, Nomor_PBT, NTPN, Nama_Notaris, Keterangan, Lainnya);

                return view;
            }
            public BundleViewExt setKey(string idBidang, string keyDesa, string keyPTSK)
            {
                if (idBidang != null)
                    this.IdBidang = idBidang;
                if (keyDesa != null)
                    this.keyDesa = keyDesa;
                if (keyPTSK != null)
                    this.keyPTSK = keyPTSK;

                return this;
            }

            public BundleViewExt setVillage(string project, string desa)
            {
                if (project != null)
                    this.Project = project;
                if (desa != null)
                    this.Desa = desa;

                return this;
            }

            public BundleViewExt setPtsk(string ptsk)
            {
                if (ptsk != null)
                    this.PTSK = ptsk;

                return this;
            }

        }

        private class PraBebasDetail
        {
            public string key { get; set; }
            public string Flag { get; set; }
            public string IdBidang { get; set; }
            public string Project { get; set; }
            public string Desa { get; set; }
            public string AlasHak { get; set; }
            public string Pemilik { get; set; }
            public string Group { get; set; }
            public double? LuasSurat { get; set; }
        }

        private string GetNomorSurat(Persil persil, string jenisDoc)
        {
            var bundle = contextplus.mainBundles.FirstOrDefault(b => b.key == persil.key);
            string nomor = string.Empty;
            string doc = "";
            if (jenisDoc.ToLower() == "shm")
                doc = "JDOK036";
            else if (jenisDoc.ToLower() == "shgb")
                doc = "JDOK037";

            if (bundle == null)
                return nomor;
            var doclist = bundle.doclist.FirstOrDefault(b => b.keyDocType == doc);

            if (doclist == null)
                return nomor;

            var entry = doclist.entries.LastOrDefault();
            if (entry == null)
                return nomor;

            var part = entry.Item.FirstOrDefault().Value;
            if (part == null)
                return nomor;

            var typename = MetadataKey.Nomor.ToString("g");
            var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
            var castvalue = dyn?.Value;

            if (castvalue != null)
                nomor = Convert.ToString(castvalue);

            return nomor;
        }

        private List<ReportDetail> GeBundleDetail(MappingDocs item, MainBundle bundles, List<ReportDetail> jenisDokumen, string desa, int iter)
        {
            List<ReportDetail> listrptDetails = new List<ReportDetail>();
            foreach (var doc in item.documents.OrderBy(x => x.urutan))
            {
                List<ReportDetail> listDetail = new List<ReportDetail>();
                string jenisDoc = jenisDokumen.Where(x => x.Identity == doc.jenisDocs).FirstOrDefault().value;
                var entries = bundles.doclist.Where(b => b.keyDocType == doc.jenisDocs)
                                             .Select(b => b.entries).ToList();

                if (entries.Select(x => x.Count()).FirstOrDefault() - 1 < iter)
                    continue;

                var part = bundles.doclist.Where(b => b.keyDocType == doc.jenisDocs)
                                          .Select(b => b.entries.LastOrDefault().Item.ElementAt(iter).Value)
                                          .FirstOrDefault();
                int i = 0;
                foreach (var metaValue in doc.metadata)
                {
                    var typename = Enum.GetName(typeof(MetadataKey), Convert.ToInt32(metaValue));
                    var dyn = part.props.TryGetValue(typename, out Dynamic val) ? val : null;
                    ReportDetail detailValue = new ReportDetail(typename.Replace("_", " "), Convert.ToString(dyn?.Value));
                    listDetail.Add(detailValue);
                    i++;
                    if (item.identifier == "Alas Hak" && i == 1)
                    {
                        detailValue = new ReportDetail("Desa", desa);
                        listDetail.Add(detailValue);
                    }
                }

                var copy = part.exists.Any(x => x.ex == Existence.Copy);
                var asli = part.exists.Any(x => x.ex == Existence.Asli);

                var countCopy = part.exists.Where(x => x.ex == Existence.Copy).Select(x => x.cnt).FirstOrDefault();
                var asliCopy = part.exists.Where(x => x.ex == Existence.Asli).Select(x => x.cnt).FirstOrDefault();

                ReportDetail detailAsli = new ReportDetail("Asli", (asli && asliCopy > 0) ? "TRUE" : "FALSE");
                listDetail.Add(detailAsli);
                ReportDetail detailCopy = new ReportDetail("Copy", (copy && countCopy > 0) ? "TRUE" : "FALSE");
                listDetail.Add(detailCopy);

                ReportDetail details = new ReportDetail();
                details.Identity = jenisDoc.Trim() == "AKTA NIKAH/SURAT KETERANGAN NIKAH/SURAT CERAI" ? "AKTA NIKAH/SURAT CERAI" : jenisDoc.Trim();
                details.orderHeader = doc.urutan ?? 0;
                details.Details = listDetail.ToArray();
                listrptDetails.Add(details);
            }
            return listrptDetails;
        }

        private List<(string key, dynamic value)> GetDataFromBundle(string dataType, string keyDocType, string keyPersils)
        {
            var bundles = context.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
             "{$match:  {$and : [{'_t' : 'mainBundle'}, {'key': {$in: [" + keyPersils + "]}}]}}",
             "{$unwind: '$doclist'}",
             "{$match : {$and : [{'doclist.keyDocType': '" + keyDocType + "'}, {'doclist.entries' : {$ne : []}}]}}",
             @"{$project : {
                                          _id: 0
                                        , key: 1
                                        , doclist: 1
                         }}").ToList();

            var bundleValue = bundles.Where(x => x.doclist.keyDocType == keyDocType)
                                        .Select(x => new
                                        {
                                            key = x.key,
                                            entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value,
                                        })
                .Select(x => new
                {
                    key = x.key,
                    dyn = x.entries != null ? x.entries.props.TryGetValue(dataType, out Dynamic val) ? val : null : null
                })
                .Select(x => new
                {
                    key = x.key,
                    value = (dynamic)x.dyn?.Value
                }).ToList();

            return bundleValue.Select(x => (x.key, x.value)).ToList();
        }

        private string SetJenisSuratExt(Assignment assignment, string templateMain)
        {
            string[] diffTemplate = { "PPJB", "KuasaMenjual" };
            if (diffTemplate.Contains(assignment.step?.LayoutSurat()) && templateMain == "OrderNotaris")
            {
                string cat = assignment.category != AssignmentCat.Girik ? "Sertifikat" : "Girik";
                return $"{templateMain}-{assignment.step?.LayoutSurat()}{cat}";
            }
            else
            {
                return $"{templateMain}-{assignment.step?.LayoutSurat()}";
            }
        }

        private string GetPreviousAssignNumber(string keyPersils, DateTime created)
        {
            string createdUTC = created.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            var prevAssignment = context.GetDocuments(new { nomorSurat = "" }, "assignment",
                "{$unwind: '$details'}",
                "{$match: { 'details.keyPersil': {$in : [" + keyPersils + " ]}}}",
                "{$match: { created: {$lt: '" + createdUTC + "' }}}",
                "{$sort: {created: -1}}",
               @"{$project: {
                        _id: 0,
                        nomorSurat: {$ifNull: ['$identifier', '']}
                  }}"
                ).FirstOrDefault();
            return prevAssignment?.nomorSurat;
        }
    }
}
using APIGrid;
using auth.mod;
using DynForm.shared;
using landrope.common;
using landrope.layout;
using landrope.mod;
using landrope.mod2;
using landrope.mod.shared;
using landrope.web.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tracer;
using MongoDB.Bson;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using landrope.material;
using landrope.mod3;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;

//using GridMvc.Server;

namespace landrope.web.Controllers
{
    [ApiController]
    [Route("api/admin")]

    public class AdminController : ControllerBase
    {
        ExtLandropeContext contextex = Contextual.GetContextExt();
        LandropeContext context = Contextual.GetContext();
        LandropePlusContext contextplus = Contextual.GetContextPlus();

        [EnableCors(nameof(landrope))]
        [NeedToken("DATA_VIEW,DATA_FULL,DATA_REVIEW,PAYMENT_VIEW,PAYMENT_FULL,MAP_VIEW,MAP_FULL,MAP_REVIEW,PASCA_VIEW,PASCA_FULL,PASCA_REVIEW,PRA_VIEW")]
        //[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
        [HttpGet("persil/list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs,
                                                                [FromQuery] bool validated = false,
                                                                [FromQuery] bool validation = false,
                                                                [FromQuery] bool rejected = false)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                var tm0 = DateTime.Now;
                ValidatableEntry<PersilBasic> noentry = null;
                if (validation)
                {
                    validated = false;
                    rejected = false;
                }
                if (validated)
                    rejected = false;

                var filters = new List<string>();// { "invalid:{$ne:true}}" };
                var stfilter = validation ? "{$in:['KOTOR','BARU']}" : rejected ? "'KOREKSI'" : validated ? "'VALID'" : null;
                if (stfilter != null)
                    filters.Add($"status:{stfilter}");

                var ureg = contextex.FindUserReg(token);
                if (!ureg.map)
                {
                    if (ureg.regular || ureg.pra)
                        filters.Add("regular:true");
                    else
                        filters.Add("regular:{$ne:true}");
                    if (ureg.pra)
                        filters.Add("en_state:1");
                    else
                        filters.Add("en_state:{$in:[null,0]}");
                }
                else if (validation)
                {
                    filters.Add("en_state:{$in:[1,3,5]}");
                    //filters.Add("en_state:1");
                }

                var prestages = new List<string> { $"{{$match:{{{string.Join(",", filters)}}}}}" };
                var poststages = new List<string>();
                var sortstages = new List<string>();

                if ((gs?.where?.rules?.Any() ?? false))
                {
                    var flt = gs.where.rules.Select(r => $"'{r.Field}':/{prepregx(r.Data)}/i").ToArray();

                    poststages.Add($"{{$match:{{{string.Join(",", flt)}}}}}");
                }

                if (!string.IsNullOrEmpty(gs?.sortColumn))
                {
                    var col = gs.sortColumn.Trim();
                    var dir = gs.sortOrder;
                    if (string.IsNullOrEmpty(dir))
                        dir = "asc";
                    if (col.Contains(" ") && gs.sortColumn.EndsWith("sc"))
                    {
                        var parts = col.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        col = parts[0];
                        if (parts.Length > 1)
                            dir = parts[1];
                    }
                    dir = dir.ToLower();
                    sortstages.Add($"{{$sort:{{{col}:{(dir == "desc" ? -1 : 1)}}}}}");
                }

                int skip = (gs.pageIndex - 1) * gs.pageSize;
                int limit = gs.pageSize > 0 ? gs.pageSize : 0;

                var result = CollectPersil(token, prestages, poststages, sortstages, skip, limit);

                int totalRecords = result.count;
                int totalPages = limit <= 0 ? 1 : (totalRecords - 1) / limit + 1;
                var lresult = result.data;
                var tms = DateTime.Now - tm0;

                return Ok(new
                {
                    total = totalPages,
                    page = gs.pageIndex,
                    records = totalRecords,
                    rows = lresult,
                    tm = tms
                });
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }

            string prepregx(string val)
            {
                //var st = val;//.Substring(1,val.Length-2); // remove the string marker
                var st = val.Replace(@"\", @"\\");
                st = new Regex(@"([\(\)\{\}\[\]\.\,\+\?\|\^\$\/])").Replace(st, @"\$1");
                st = new Regex(@"\s+").Replace(st, @"\s+");
                return st;
            }
        }

        (IEnumerable<PersilCore> data, int count) CollectPersil(string token,
                                IEnumerable<string> prestages,
                                IEnumerable<string> poststages,
                                IEnumerable<string> sortstages,
                                int skip, int limit)
        {
            //var aggs = new[] {
            //	//@"{$project:{state:1,persil:1,basic:{$arrayElemAt:['$persil.basic.entries.item',-1]},PBTPT:{$arrayElemAt:['$persil.PBTPT.entries.item',-1]}}}",
            //	@"{$lookup:{ from: 'maps',localField:'basic.keyProject',foreignField:'key',as: 'projects'}}",
            //	@"{$lookup: {
            //		from: 'maps',
            //		let:{key:'$basic.keyDesa'},
            //		pipeline:[
            //			{$unwind:'$villages'},
            //			{$match:{$expr:{$eq:['$villages.key','$$key']}}},
            //			{$project:{key:'$villages.key',identity:'$villages.identity'}}
            //		],
            //		as: 'desas'
            //	}}",
            //	@"{$lookup: {from: 'masterdatas',localField: 'basic.keyPenampung',foreignField: 'key',as: 'penampung'}}",
            //	@"{$lookup: {from: 'masterdatas',localField: 'basic.keyPTSK',foreignField: 'key',as: 'PTSK'}}",
            //	@"{$lookup: {from: 'persilMaps',localField: 'key',foreignField: 'key',as: 'map'}}",
            //	@"{$lookup: {from: 'persilMaps_dirty',localField: 'key',foreignField: 'key',as: 'map2'}}",
            //	@"{$unwind: {path: '$projects',preserveNullAndEmptyArrays: true}}",
            //	@"{$unwind: {path: '$desas',preserveNullAndEmptyArrays: true}}",
            //	@"{$unwind: {path: '$penampung',preserveNullAndEmptyArrays: true}}",
            //	@"{$unwind: {path: '$PTSK',preserveNullAndEmptyArrays: true}}",
            //	@"{$unwind: {path: '$map',preserveNullAndEmptyArrays: true}}",
            //	@"{$unwind: {path: '$map2',preserveNullAndEmptyArrays: true}}",
            //	@"{$project: {
            //		_id: 0,
            //		key: 1,
            //		status: '$state',
            //		bebas: {$switch:{
            //			branches: [
            //				{case: {$eq: [{'$ifNull': ['$en_state',0]},0]},then: 'BEBAS'},
            //				{case: {$eq: [{$ifNull: ['$en_state',0]},2]},then: '(BATAL))'}
            //			],
            //			default: 'YA'
            //		}},
            //		created: '$persil.created',
            //		proses: {$switch: {
            //			branches: [
            //				{case: {$eq: ['$basic.en_proses',1]},then: 'batal'},
            //				{case: {$eq: ['$basic.en_proses',2]},then: 'overlap'},
            //				{case: {$eq: ['$basic.en_proses',3]},then: 'lokal'},
            //				{case: {$eq: ['$basic.en_proses',4]},then: 'hibah'}
            //			],
            //			default: 'standar'
            //		}},
            //		jenis: {$switch: {
            //			branches: [
            //				{case: {$eq: ['$basic.en_jenis',1]},then: 'girik'},
            //				{case: {$eq: ['$basic.en_jenis',2]},then: 'shp'},
            //				{case: {$eq: ['$basic.en_jenis',3]},then: 'hgb'},
            //				{case: {$eq: ['$basic.en_jenis',4]},then: 'shm'},
            //				{case: {$eq: ['$basic.en_jenis',5]},then: 'khusus'}
            //			],
            //			default: 'unknown'
            //		}},
            //		alias: '$basic.alias',
            //		project: '$projects.identity',
            //		desa: '$desas.identity',
            //		group: '$basic.group',
            //		tahap: '$basic.tahap',
            //		luasSurat: '$basic.luasSurat',
            //		mediator: '$basic.mediator',
            //		noPeta: '$basic.noPeta',
            //		namaSurat: '$basic.surat.nama',
            //		nomorSurat: '$basic.surat.nomor',
            //		penampung: '$penampung.identifier',
            //		PTSK: '$PTSK.identifier',
            //		pemilik: '$basic.pemilik',
            //		noNIB: '$PBTPT.hasil.noNIB',
            //		map: {$switch: {
            //			branches: [
            //				{case: {$ne: [{'$ifNull': ['$map2.key','']},'']},then: 'VALIDASI'},
            //				{case: {$ne: [{'$ifNull': ['$map.key','']},'']},then: 'ADA'}
            //			],
            //			default: 'BELUM'
            //		}}
            //	}}"
            //};

            //var tmpname = $"tmp_{token}{DateTime.Now.Ticks}";

            //var poststages = addstages.Where(s => !s.StartsWith("!>"));
            //var prestages = addstages.Except(poststages).Select(s=>s.Substring(2));

            var allstages = prestages./*Union(aggs).*/Union(poststages).ToArray();

            //allstages.Add(outstage);

            //var pipeline = PipelineDefinition<BsonDocument, BsonString>.Create(allstages.ToArray());
            //var outres = context.db.GetCollection<BsonDocument>("persil_stage").Aggregate(
            //).FirstOrDefault();

            var countstage = "{$count:'count'}";
            var totstages = allstages.Union(new[] { countstage }).ToArray();

            var limstages = new List<string>();
            if (skip > 0)
                limstages.Add($"{{$skip:{skip}}}");
            if (limit > 0)
                limstages.Add($"{{$limit:{limit}}}");
            limstages.Add("{$project:{regular:0,en_state:0}}");

            allstages = allstages.Union(sortstages).Union(limstages)
                .Union(new[] { "{$project:{_id:0}}" })
                .ToArray();

            var countpipe = PipelineDefinition<BsonDocument, BsonDocument>.Create(totstages);
            var resultpipe = PipelineDefinition<BsonDocument, PersilCore>.Create(allstages);

            int count = 0;
            List<PersilCore> result = new List<PersilCore>();

            var coll = context.db.GetCollection<BsonDocument>("material_persil_core");
            //var coll2 = contextex.db.GetCollection<BsonDocument>("persil_core_2");
            var task1 = Task.Run(() =>
            {
                var xcounter = coll.Aggregate<BsonDocument>(countpipe);
                var counter = xcounter.FirstOrDefault();
                if (counter != null)
                    count = counter.Names.Any(s => s == "count") ? counter.GetValue("count").AsInt32 : 0;
            });
            var task2 = Task.Run(() =>
            {
                var res = coll.Aggregate<PersilCore>(resultpipe);
                result = res.ToList();
            });
            Task.WaitAll(task1, task2);
            return (result, count);// (int)Math.Truncate(count.count));
        }

        private (PropertyInfo pinfo, object shell) GetShell(string part, string key, bool? regular)
        {
            if (string.IsNullOrEmpty(part))
                return (null, null);
            var data = contextex.persils.FirstOrDefault(p => p.key == key);
            //if (data == null || (regular.HasValue && regular != (data.regular ?? false)))
            //    return (null, null);

            var type = data.GetType();
            var props = type.GetProperties();
            var propns = props.Select(p => (p, a: p.GetCustomAttribute(typeof(StepAttribute))))
                                    .Where(p => p.a != null)
                                    .Select(x => (x.p, an: ((StepAttribute)x.a).name))
                                    .ToList();
            var prop = propns.FirstOrDefault(p => p.an == part).p;
            if (prop == null)
                return (null, null);

            var shell = prop.GetValue(data);
            return (prop, shell);
        }

        private (PropertyInfo pinfo, object shell) GetShell(Persil data, string part)
        {
            if (data == null || string.IsNullOrEmpty(part))
                return (null, null);
            var type = data.GetType();
            var props = type.GetProperties();
            var propns = props.Select(p => (p, a: p.GetCustomAttribute(typeof(StepAttribute))))
                                    .Where(p => p.a != null)
                                    .Select(x => (x.p, an: ((StepAttribute)x.a).name))
                                    .ToList();
            var prop = propns.FirstOrDefault(p => p.an == part).p;
            if (prop == null)
                return (null, null);

            var shell = prop.GetValue(data);
            return (prop, shell);
        }



        [EnableCors(nameof(landrope))]
        [NeedToken("DATA_VIEW,DATA_FULL,PAYMENT_VIEW,PAYMENT_FULL,PASCA_VIEW,PASCA_FULL,MAP_FULL")]
        [HttpGet("persil/{part}")]
        public IActionResult GetPart(string token, string part, string key, bool validated = false)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var ureg = contextex.FindUserReg(token);
            try
            {
                if (ureg.map && part != "basic")
                    return NoContent();
                var xshell = GetShell(part, key, ureg.map ? null : (bool?)ureg.regular);
                if (xshell.pinfo == null || xshell.shell == null)
                    return NoContent();
                if (ureg.map)
                {
                    if (xshell.shell is ValidatableShell<PersilBasic>)
                        return ReturnContext<PersilBasic, cmnPersilBasic>((ValidatableShell<PersilBasic>)xshell.shell, ureg.user);
                    return null;
                }
                if (ureg.pra)
                {
                    if (xshell.shell is ValidatableShell<PersilBasic>)
                        return ReturnContext<PersilBasic, cmnPersilBasic>((ValidatableShell<PersilBasic>)xshell.shell, ureg.user);
                    else if (xshell.shell is ValidatableShell<GroupUTJ>)
                        return ReturnContext<GroupUTJ, cmnGroupUTJ>((ValidatableShell<GroupUTJ>)xshell.shell, ureg.user);
                    return null;
                }
                switch (xshell.shell)
                {
                    case ValidatableShell<PersilBasic> shell: return ReturnContext<PersilBasic, cmnPersilBasic>(shell, ureg.user);
                    case ValidatableShell<GroupUTJ> shell: return ReturnContext<GroupUTJ, cmnGroupUTJ>(shell, ureg.user);
                    case ValidatableShell<GroupDP> shell: return ReturnContext<GroupDP, cmnGroupDP>(shell, ureg.user);
                    case ValidatableShell<GroupPelunasan> shell: return ReturnContext<GroupPelunasan, cmnGroupPelunasan>(shell, ureg.user);
                    case ValidatableShell<ProsesPerjanjian> shell: return ReturnContext<ProsesPerjanjian, cmnProsesPerjanjian>(shell, ureg.user);
                    //case ValidatableShell<ProsesPerjanjianGirik> shell: return ReturnContext<ProsesPerjanjianGirik, cmnProsesPerjanjianGirik>(shell, ureg.user);
                    //case ValidatableShell<ProsesPerjanjianSertifikat> shell: return ReturnContext<ProsesPerjanjianSertifikat, cmnProsesPerjanjianSertifikat>(shell, ureg.user);
                    case ValidatableShell<ProsesPBT<NIB_PT>> shell: return ReturnContext<ProsesPBT<NIB_PT>, cmnProsesPBT>(shell, ureg.user);
                    case ValidatableShell<ProsesPBT<NIB_Perorangan>> shell: return ReturnContext<ProsesPBT<NIB_Perorangan>, cmnProsesPBT>(shell, ureg.user);
                    case ValidatableShell<ProsesSPH> shell: return ReturnContext<ProsesSPH, cmnProsesSPH>(shell, ureg.user);
                    case ValidatableShell<ProsesMohonSKKantah> shell: return ReturnContext<ProsesMohonSKKantah, cmnProsesMohonSKKantah>(shell, ureg.user);
                    case ValidatableShell<ProsesMohonSKKanwil> shell: return ReturnContext<ProsesMohonSKKanwil, cmnProsesMohonSKKanwil>(shell, ureg.user);
                    case ValidatableShell<ProsesCetakBukuHGB> shell: return ReturnContext<ProsesCetakBukuHGB, cmnProsesCetakBuku>(shell, ureg.user);
                    case ValidatableShell<ProsesCetakBukuSHM> shell: return ReturnContext<ProsesCetakBukuSHM, cmnProsesCetakBukuSHM>(shell, ureg.user);
                    case ValidatableShell<ProsesTurunHak> shell: return ReturnContext<ProsesTurunHak, cmnProsesTurunHak>(shell, ureg.user);
                    case ValidatableShell<ProsesNaikHak> shell: return ReturnContext<ProsesNaikHak, cmnProsesNaikHak>(shell, ureg.user);
                    case ValidatableShell<ProsesAJB> shell: return ReturnContext<ProsesAJB, cmnProsesAJB>(shell, ureg.user);
                    case ValidatableShell<ProsesBalikNama> shell: return ReturnContext<ProsesBalikNama, cmnProsesBalikNama>(shell, ureg.user);
                    case ValidatableShell<PembayaranPajak> shell: return ReturnContext<PembayaranPajak, cmnPembayaranPajak>(shell, ureg.user);
                    case ValidatableShell<MasukAJB> shell: return ReturnContext<MasukAJB, cmnMasukAJB>(shell, ureg.user);
                    default: return NoContent();
                }

                //Type gtype = xshell.shell.GetType();
                //var ptypes = gtype.GenericTypeArguments;
                //if (ptypes.Length == 1)
                //{
                //	switch (ptypes[0].Name)
                //	{
                //		case nameof(PersilBasic): return ReturnContext<PersilBasic, cmnPersilBasic>((ValidatableShell<PersilBasic>)xshell.shell,user);
                //		case nameof(GroupUTJ): return ReturnContext<GroupUTJ, cmnGroupUTJ>((ValidatableShell<GroupUTJ>)xshell.shell, user);
                //		case nameof(GroupDP): return ReturnContext<GroupDP, cmnGroupDP>((ValidatableShell<GroupDP>)xshell.shell, user);
                //		case nameof(GroupPelunasan): return ReturnContext<GroupPelunasan, cmnGroupPelunasan>((ValidatableShell<GroupPelunasan>)xshell.shell, user);
                //		case nameof(ProsesPerjanjian): return ReturnContext<ProsesPerjanjian, cmnProsesPerjanjian>((ValidatableShell<ProsesPerjanjian>)xshell.shell, user);
                //		//case nameof(ProsesPerjanjianGirik): return ReturnContext<ProsesPerjanjianGirik, cmnProsesPerjanjianGirik>((ValidatableShell<ProsesPerjanjianGirik>)xshell.shell, user);
                //		//case nameof(ProsesPerjanjianSertifikat): return ReturnContext<ProsesPerjanjianSertifikat, cmnProsesPerjanjianSertifikat>((ValidatableShell<ProsesPerjanjianSertifikat>)xshell.shell, user);
                //		case nameof(ProsesPBT): return ReturnContext<ProsesPBT, cmnProsesPBT>((ValidatableShell<ProsesPBT>)xshell.shell, user);
                //		case nameof(ProsesSPH): return ReturnContext<ProsesSPH, cmnProsesSPH>((ValidatableShell<ProsesSPH>)xshell.shell, user);
                //		case nameof(ProsesMohonSKKantah): return ReturnContext<ProsesMohonSKKantah, cmnProsesMohonSKKantah>((ValidatableShell<ProsesMohonSKKantah>)xshell.shell, user);
                //		case nameof(ProsesMohonSKKanwil): return ReturnContext<ProsesMohonSKKanwil, cmnProsesMohonSKKanwil>((ValidatableShell<ProsesMohonSKKanwil>)xshell.shell, user);
                //		case nameof(ProsesCetakBuku): return ReturnContext<ProsesCetakBuku, cmnProsesCetakBuku>((ValidatableShell<ProsesCetakBuku>)xshell.shell, user);
                //		case nameof(ProsesTurunHak): return ReturnContext<ProsesTurunHak, cmnProsesTurunHak>((ValidatableShell<ProsesTurunHak>)xshell.shell, user);
                //		case nameof(ProsesNaikHak): return ReturnContext<ProsesNaikHak, cmnProsesNaikHak>((ValidatableShell<ProsesNaikHak>)xshell.shell, user);
                //		case nameof(ProsesAJB): return ReturnContext<ProsesAJB, cmnProsesAJB>((ValidatableShell<ProsesAJB>)xshell.shell, user);
                //		case nameof(ProsesBalikNama): return ReturnContext<ProsesBalikNama, cmnProsesBalikNama>((ValidatableShell<ProsesBalikNama>)xshell.shell, user);
                //		case nameof(PembayaranPajak): return ReturnContext<PembayaranPajak, cmnPembayaranPajak>((ValidatableShell<PembayaranPajak>)xshell.shell, user);
                //		case nameof(MasukAJB): return ReturnContext<MasukAJB, cmnMasukAJB>((ValidatableShell<MasukAJB>)xshell.shell, user);
                //	}
                //}
                //return NoContent();
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }

            IActionResult ReturnContext<TIn, TOut>(ValidatableShell<TIn> shell, user User)
                            where TIn : ValidatableItem where TOut : class
            {
                var entry = shell.GetLast(true);
                var data = validated ?
                    new
                    {
                        part = shell.current,
                        valid = new Validation<TIn>()
                    } :
                new
                {
                    part = entry?.item,
                    valid = entry?.Validation
                };
                //if (data?.part == null & token == "SAMPLE")
                //	data = new { part = new PersilBasic(), valid = (object)new { reviewed = true, approved = true, note = "" } };

                if (data.part == null && !validated)
                    data = new { part = Activator.CreateInstance<TIn>(), valid = new Validation<TIn>() };


                //var odata = data.part;
                if (data.part == null)
                    return NoContent();

                var curdir = ControllerContext.GetContentRootPath();
                //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
                var rights = User?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
                var layout = LayoutMaster.LoadLayout(typeof(TIn).Name, curdir, rights).ToList();
                if (data.valid.corrections.Any())
                    layout.ForEach(l => l.correction = data.valid.corrections.Contains(l.value));

                data.part.extras = GetXtras(layout, data.valid.note);
                TOut fdata = Converter.Forward<TIn, TOut>(data.part);

                Expandable.TryExpand(fdata);
                //if (typeof(IExpandable).IsAssignableFrom(typeof(TOut)))
                //	((IExpandable)fdata).Expand();

                var context = new DynamicContext<TOut>(layout.ToArray(), fdata);
                var json = JsonConvert.SerializeObject(context, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include, DateTimeZoneHandling = DateTimeZoneHandling.Local });
                return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
            }
        }

        [EnableCors(nameof(landrope))]
        //[NeedToken("DATA_REVIEW,PAYMENT_REVIEW,NORMAL_REVIEW,SPESIAL_REVIEW,ARSIP_REVIEW,PASCA_REVIEW,MAP_REVIEW,APPR1_ADD_BB,APPR2_ADD_BB,APPR1_EDIT_BB,APPR2_EDIT_BB,APPR1_EDIT_SB,APPR2_EDIT_SB")]
        [HttpGet("persil/validation")]
        public IActionResult GetValidation(string token, string key)
        {
            var ureg = contextex.FindUserReg(token);
            try
            {
                var persil = contextex.persils.FirstOrDefault(p => p.key == key);
                if (persil == null)
                    return NoContent();
                if (ureg.map && persil.en_state != StatusBidang.belumbebas && persil.en_state != StatusBidang.kampung && persil.en_state != StatusBidang.keluar)
                    return NoContent();

                var steps = contextex.GetStepsX(key, null).ToArray();
                var scenes = steps.OrderBy(s => s.order).Select(s => GetValidationPart(s.name, key, ureg.regular || ureg.map))
                                                    .Where(s => s != null).ToArray();

                var json = JsonConvert.SerializeObject(scenes, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include, DateTimeZoneHandling = DateTimeZoneHandling.Local });
                return new ContentResult { ContentType = "application/json", Content = json };//Ok(context);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        private ValidationScene GetValidationPart(string part, string key, bool regular)
        {
            try
            {
                var xshell = GetShell(part, key, regular);
                if (xshell.pinfo == null || xshell.shell == null)
                    return null;

                switch (xshell.shell)
                {
                    case ValidatableShell<PersilBasic> shell: return ReturnScene(shell);
                    case ValidatableShell<GroupUTJ> shell: return ReturnScene(shell);
                    case ValidatableShell<GroupDP> shell: return ReturnScene(shell);
                    case ValidatableShell<GroupPelunasan> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesPerjanjian> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesPBT<NIB_PT>> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesPBT<NIB_Perorangan>> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesSPH> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesMohonSKKantah> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesMohonSKKanwil> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesCetakBukuHGB> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesCetakBukuSHM> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesTurunHak> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesNaikHak> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesAJB> shell: return ReturnScene(shell);
                    case ValidatableShell<ProsesBalikNama> shell: return ReturnScene(shell);
                    case ValidatableShell<PembayaranPajak> shell: return ReturnScene(shell);
                    case ValidatableShell<MasukAJB> shell: return ReturnScene(shell);
                    default: return null;
                }

                //Type gtype = xshell.shell.GetType();
                //var ptypes = gtype.GenericTypeArguments;
                //if (ptypes.Length == 1)
                //{
                //	switch (ptypes[0].Name)
                //	{
                //		case nameof(PersilBasic): return ReturnScene<PersilBasic>((ValidatableShell<PersilBasic>)xshell.shell);
                //		case nameof(GroupUTJ): return ReturnScene<GroupUTJ>((ValidatableShell<GroupUTJ>)xshell.shell);
                //		case nameof(GroupDP): return ReturnScene<GroupDP>((ValidatableShell<GroupDP>)xshell.shell);
                //		case nameof(GroupPelunasan): return ReturnScene<GroupPelunasan>((ValidatableShell<GroupPelunasan>)xshell.shell);
                //		case nameof(ProsesPerjanjian): return ReturnScene<ProsesPerjanjian>((ValidatableShell<ProsesPerjanjian>)xshell.shell);
                //		case nameof(ProsesPBT): return ReturnScene<ProsesPBT>((ValidatableShell<ProsesPBT>)xshell.shell);
                //		case nameof(ProsesSPH): return ReturnScene<ProsesSPH>((ValidatableShell<ProsesSPH>)xshell.shell);
                //		case nameof(ProsesMohonSKKantah): return ReturnScene<ProsesMohonSKKantah>((ValidatableShell<ProsesMohonSKKantah>)xshell.shell);
                //		case nameof(ProsesMohonSKKanwil): return ReturnScene<ProsesMohonSKKanwil>((ValidatableShell<ProsesMohonSKKanwil>)xshell.shell);
                //		case nameof(ProsesCetakBuku): return ReturnScene<ProsesCetakBuku>((ValidatableShell<ProsesCetakBuku>)xshell.shell);
                //		case nameof(ProsesTurunHak): return ReturnScene<ProsesTurunHak>((ValidatableShell<ProsesTurunHak>)xshell.shell);
                //		case nameof(ProsesNaikHak): return ReturnScene<ProsesNaikHak>((ValidatableShell<ProsesNaikHak>)xshell.shell);
                //		case nameof(ProsesAJB): return ReturnScene<ProsesAJB>((ValidatableShell<ProsesAJB>)xshell.shell);
                //		case nameof(ProsesBalikNama): return ReturnScene<ProsesBalikNama>((ValidatableShell<ProsesBalikNama>)xshell.shell);
                //		case nameof(PembayaranPajak): return ReturnScene<PembayaranPajak>((ValidatableShell<PembayaranPajak>)xshell.shell);
                //		case nameof(MasukAJB): return ReturnScene<MasukAJB>((ValidatableShell<MasukAJB>)xshell.shell);
                //	}
                //}
                //return null;
            }
            catch (Exception ex)
            {
                throw;
            }

            ValidationScene ReturnScene<TIn>(ValidatableShell<TIn> shell)
                            where TIn : ValidatableItem
            {
                var entry = shell.GetWaiting();
                if (entry == null)
                    return null;

                var current = shell.current;

                var curdir = ControllerContext.GetContentRootPath();
                var layout = LayoutMaster.LoadLayout(typeof(TIn).Name, curdir, null);

                var dyns = layout.Where(l => l.shown).Select(l => DynMapper.FromElement(l, entry.item, current, context, contextex));
                var scene = new ValidationScene
                {
                    step = part,
                    username = contextex.users.FirstOrDefault(u => u.key == entry.keyCreator)?.FullName,
                    changetime = entry.created
                };
                scene.items.AddRange(dyns);
                return scene;
            }
        }


        //private object GetXtras(string typename)
        //{
        //	switch (typename)
        //	{
        //		case nameof(PersilBasic):
        //			return new
        //			{
        //				lstProses = Enum.GetNames(typeof(JenisProses))
        //											.Select(v => new option { key = v, identity = v }),
        //				lstLahan = Enum.GetNames(typeof(JenisLahan))
        //									.Select(v => new option { key = v, identity = v }),
        //				lstJenis = Enum.GetNames(typeof(JenisAlasHak))
        //									.Select(v => new option { key = v, identity = v }),
        //				lstStatus = Enum.GetNames(typeof(StatusBerkas))
        //										.Select(v => new option { key = v, identity = v }),

        //				optProjects = this.context.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}")
        //										.ToList().Select(x => new option { key = x.key, identity = x.identity }).ToArray(),
        //				optDesas = this.context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
        //												"villages", "{}", "{_id:0}")
        //										.ToList().Select(x => new option { keyparent = x.project.key, key = x.village.key, identity = x.village.identity })
        //										.ToArray(),
        //				optPenampungs = this.contextex.companies.Query(c => c.status == StatusPT.penampung && c.invalid != true).ToArray(),
        //				optKSs = this.contextex.companies.Query(c => c.status == StatusPT.pembeli && c.invalid != true).ToArray(),
        //				optNotarists = this.contextex.companies.Query(n => n.invalid != null),
        //				optCompanies = this.contextex.companies.Query(c => c.invalid != true).ToArray(),
        //				optGroups = new option[0],
        //				optUsers = new option[0]
        //			};
        //		case nameof(ProsesPerjanjianGirik):
        //		case nameof(ProsesPerjanjianSertifikat):
        //			return new
        //		{
        //			optNotarists = this.contextex.companies.Query(n => n.invalid != null),
        //				optCompanies = this.contextex.companies.Query(c => c.invalid != true).ToArray(),
        //				optUsers = new option[0]
        //			};
        //	}
        //	return null;
        //}

        private object GetXtras(IEnumerable<DynElement> layout, string note)
        {
            var dict = new Dictionary<string, option[]>();
            layout.Where(e => !string.IsNullOrWhiteSpace(e.options)).Select(e => e.options).Distinct().ToList().ForEach(
                o =>
                {
                    var opt = o.Replace("extras.", "");

                    option[] opts = opt switch
                    {
                        "lstProses" => Enum.GetNames(typeof(JenisProses)).Select(v => new option { key = v, identity = v }).ToArray(),
                        "lstLahan" => Enum.GetNames(typeof(JenisLahan)).Select(v => new option { key = v, identity = v }).ToArray(),
                        "lstJenis" => Enum.GetNames(typeof(JenisAlasHak)).Select(v => new option { key = v, identity = v }).ToArray(),
                        "lstStatus" => Enum.GetNames(typeof(SifatBerkas)).Select(v => new option { key = v, identity = v }).ToArray(),
                        "optProjects" => this.context.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}")
                                                        .ToList().Select(x => new option { key = x.key, identity = x.identity }).ToArray(),
                        "optDesas" => this.context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
                                                                    "villages", "{}", "{_id:0}")
                                    .ToList().Select(x => new option { keyparent = x.project.key, key = x.village.key, identity = x.village.identity })
                                    .ToArray(),
                        "optPenampungs" => this.contextex.companies.Query(c => c.status == StatusPT.penampung && c.invalid != true)
                                    .ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                                    .Distinct().ToArray(),
                        //"optSKs" => this.contextex.companies.Query(c => c.status == StatusPT.pembeli && c.invalid != true)
                        //                                .ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                        //                                .Distinct().ToArray(),
                        "optSKs" => this.contextex.ptsk.Query(c => c.status == StatusPT.pembeli && c.invalid != true)
                        .ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                        .Distinct().ToArray(),
                        "optNotaris" => GetNotaris(),
                        "optNotarists" => GetNotaris(),
                        "optCompany" => GetCompany(),
                        "optCompanies" => GetCompany(),
                        "optUsers" => this.contextex.users.Query(u => u.invalid != true)
                                                        .ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                                                        .Distinct().ToArray(),

                        _ => new option[0]
                    };

                    //			option[] opts = new option[0];
                    //			switch (opt)
                    //			{
                    //				case "lstProses":
                    //					opts = Enum.GetNames(typeof(JenisProses))
                    //		 .Select(v => new option { key = v, identity = v }).ToArray(); break;
                    //				case "lstLahan":
                    //					opts = Enum.GetNames(typeof(JenisLahan))
                    //	.Select(v => new option { key = v, identity = v }).ToArray(); break;
                    //				case "lstJenis":
                    //					opts = Enum.GetNames(typeof(JenisAlasHak))
                    //	.Select(v => new option { key = v, identity = v }).ToArray(); break;
                    //				case "lstStatus":
                    //					opts = Enum.GetNames(typeof(SifatBerkas))
                    //	 .Select(v => new option { key = v, identity = v }).ToArray(); break;
                    //				case "optProjects":
                    //					opts = this.context.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}")
                    // .ToList().Select(x => new option { key = x.key, identity = x.identity }).ToArray(); break;
                    //				case "optDesas":
                    //					opts = this.context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
                    //				"villages", "{}", "{_id:0}")
                    //		.ToList().Select(x => new option { keyparent = x.project.key, key = x.village.key, identity = x.village.identity })
                    //		.ToArray(); break;
                    //				case "optPenampungs":
                    //					opts = this.contextex.companies.Query(c => c.status == StatusPT.penampung && c.invalid != true)
                    //.ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                    //.Distinct().ToArray(); break;
                    //				case "optSKs":
                    //					opts = this.contextex.companies.Query(c => c.status == StatusPT.pembeli && c.invalid != true)
                    //			 .ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                    //			 .Distinct().ToArray(); break;
                    //				case "optNotaris":
                    //				case "optNotarists":
                    //					opts = this.contextex.notarists.Query(n => n.invalid != true)
                    //.ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                    //.Distinct().ToArray(); break;
                    //				case "optCompany":
                    //				case "optCompanies":
                    //					opts = this.contextex.companies.Query(c => c.invalid != true)
                    //.ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                    //.Distinct().ToArray(); break;
                    //				case "optGroups":
                    //					opts = new option[0]; contextex.GetCollections(new { group = "" }, "persils_v2", "{}", "{_id:0,group:1}")
                    //	 .ToList().Select(x => new option { keyparent = null, key = x.group, identity = x.group })
                    //	 .Distinct().ToArray(); break;
                    //				case "optUsers":
                    //					opts = this.contextex.users.Query(u => u.invalid != true)
                    //		.ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                    //		.Distinct().ToArray(); break;
                    //			}
                    dict.Add(opt, opts);
                });
            if (!string.IsNullOrWhiteSpace(note))
                dict.Add("NOTE", new option[] { new option { key = "NOTE", identity = note } });
            return dict;

            option[] GetNotaris() => this.contextex.notarists.Query(n => n.invalid != true)
                        .ToArray().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                        .Distinct().ToArray();
            option[] GetCompany() => this.contextex.companies.Query(c => c.invalid != true)
                        .ToList().Select(x => new option { keyparent = null, key = x.key, identity = x.identifier })
                        .Distinct().ToArray();
        }

        private Persil GetPersil(string key)
        {
            return contextex.persils.FirstOrDefault(p => p.key == key);
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("PASCA_FULL")]
        [HttpGet("persil/new")]
        public IActionResult NewPersil(string token)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var user = contextex.FindUser(token);
            var rights = user?.getPrivileges(null)?.Select(a => a.identifier).ToArray();
            var odata = Activator.CreateInstance<PersilBasic>();
            var curdir = ControllerContext.GetContentRootPath();
            //var layout = LayoutMaster.dictionary.TryGetValue(typeof(TIn).Name, out DynElement[] lay) ? lay : new DynElement[0];
            var layout = LayoutMaster.LoadLayout(typeof(PersilBasic).Name, curdir, rights);
            odata.extras = GetXtras(layout, "");
            cmnPersilBasic fdata = Converter.Forward<PersilBasic, cmnPersilBasic>(odata);

            var context = new DynamicContext<cmnPersilBasic>(layout, fdata);
            return Ok(context);
        }


        private IActionResult Update<T>(string key, string propname, T item, string oper, user User) where T : ValidatableItem
        {
            try
            {
                var data = GetPersil(key);
                if (data == null)
                    return new NotModifiedResult("Persil not found");
                var pinfo = data.GetType().GetProperty(propname);
                var part = (ValidatableShell<T>)pinfo.GetValue(data);
                if (part == null)
                    part = Activator.CreateInstance<ValidatableShell<T>>();

                switch (oper)
                {
                    case "del": part.PutEntry(null, ChangeKind.Delete, User?.key); break;
                    case "edit": part.PutEntry(item, ChangeKind.Update, User?.key); break;
                    case "add": part.PutEntry(item, ChangeKind.Add, User?.key); break;
                }
                pinfo.SetValue(data, part);
                contextex.persils.Update(data);
                try
                {
                    contextex.SaveChanges();
                    return Ok();
                }
                catch (Exception eex)
                {
                    return new NotModifiedResult(eex.Message);
                }
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [PersilCSMaterializer(Auto = false)]
        private IActionResult Update2(string key, string part, object item, string oper, user User, bool regular, bool map)
        {
            (PropertyInfo pinfo, object shell) xshell;
            try
            {
                Persil data = null;
                bool makebundle = false;
                if (oper == "add")
                {
                    if (regular != true)
                        return new NotModifiedResult("Anda tidak punya hak untuk menambahkan bidang");
                    var json = System.Text.Json.JsonSerializer.Serialize(item);
                    var cbasic = System.Text.Json.JsonSerializer.Deserialize<cmnPersilBasic>(json);

                    cbasic.extras = null;
                    //if (typeof(IExpandable).IsAssignableFrom(typeof(cmnPersilBasic)))
                    //	((IExpandable)cbasic).CanRemove();

                    PersilBasic basic = Converter.Reverse<PersilBasic, cmnPersilBasic>(cbasic);
                    //if (basic.en_jenis == JenisAlasHak.unknown)
                    //    basic.en_jenis = JenisAlasHak.khusus;
                    switch (basic.en_proses)
                    {
                        case JenisProses.hibah: data = new PersilHibah(); break;
                        case JenisProses.lokal:
                        case JenisProses.standar:
                            switch (basic.en_jenis)
                            {
                                case JenisAlasHak.girik: data = new PersilGirik(); break;
                                case JenisAlasHak.shm: data = new PersilSHM(); break;
                                case JenisAlasHak.shp: data = new PersilSHP(); break;
                                case JenisAlasHak.hgb: data = new PersilHGB(); break;
                                case JenisAlasHak.unknown: data = null; break;
                            }; break;
                        case JenisProses.batal:
                        case JenisProses.overlap:
                            data = new PersilGirik
                            {
                                basic = new ValidatableShell<PersilBasic>
                                { current = new PersilBasic { en_proses = basic.en_proses } }
                            }; break;
                    }
                    data.en_state = new[] { JenisProses.batal, JenisProses.overlap }.Contains(basic.en_proses) ? StatusBidang.batal : StatusBidang.bebas;
                    data.regular = true;
                    data.statechanged = DateTime.Now;
                    data.statechanger = User.key;
                    data.basic.PutEntry(basic, ChangeKind.Add, User?.key);
                    data.MakeID();

                    contextex.persils.Insert(data);

                    makebundle = data.en_state == StatusBidang.bebas;
                }
                else
                {
                    data = GetPersil(key);
                    if (data.en_state == StatusBidang.batal)
                        return new NotModifiedResult("Bidang ini sudah dibatalkan dan tidak dapat dimodifikasi lagi.");

                    regular = regular || (map && (data.en_state == StatusBidang.belumbebas || data.en_state == StatusBidang.kampung || data.en_state == StatusBidang.keluar));

                    if (regular != (data.regular ?? false))
                        return new ContentResult
                        {
                            StatusCode = (int)HttpStatusCode.Forbidden,
                            Content = "Hak Akses anda tidak berlaku untuk bidang ini",
                            ContentType = "text"
                        };

                    if (map && data.en_state != StatusBidang.belumbebas && data.en_state != StatusBidang.kampung && data.en_state != StatusBidang.keluar)
                        return new ContentResult
                        {
                            StatusCode = (int)HttpStatusCode.Forbidden,
                            Content = "Anda tidak berhak mengubah data bidang yang sudah bebas",
                            ContentType = "text"
                        };

                    if (data.en_state == StatusBidang.belumbebas || data.en_state == StatusBidang.kampung)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(item);
                        var cbasic = System.Text.Json.JsonSerializer.Deserialize<cmnPersilBasic>(json);

                        var mustFromRequestWF = (cbasic.keyProject != data.basic?.current?.keyProject || cbasic.keyDesa != data.basic?.current?.keyDesa || cbasic.keyPTSK != data.basic?.current?.keyPTSK || cbasic.en_proses != (int)data.basic?.current?.en_proses) ? true : false;
                        if (mustFromRequestWF)
                            return new ContentResult
                            {
                                StatusCode = (int)HttpStatusCode.Forbidden,
                                Content = "Perubahan Data Project/Desa/PTSK/Jenis Proses Bidang Belum Bebas hanya bisa dalam modul Work Request",
                                ContentType = "text"
                            };
                    }

                    xshell = GetShell(part, key, map ? null : (bool?)regular);
                    if (xshell.pinfo == null || xshell.shell == null)
                        return new NotModifiedResult($"Part {part} is invalid");

                    switch (xshell.shell)
                    {
                        case ValidatableShell<PersilBasic> shell: Put<PersilBasic, cmnPersilBasic>(shell); break;
                        case ValidatableShell<GroupUTJ> shell: Put<GroupUTJ, cmnPersilBasic>(shell); break;
                        case ValidatableShell<GroupDP> shell: Put<GroupDP, cmnGroupDP>(shell); break;
                        case ValidatableShell<ProsesPerjanjian> shell: Put<ProsesPerjanjian, cmnProsesPerjanjian>(shell); break;
                        //case ValidatableShell<ProsesPerjanjianGirik> shell: Put<ProsesPerjanjianGirik, cmnProsesPerjanjianGirik>(shell); break;
                        //case ValidatableShell<ProsesPerjanjianSertifikat> shell: Put<ProsesPerjanjianSertifikat, cmnProsesPerjanjianSertifikat>(shell); break;
                        case ValidatableShell<GroupPelunasan> shell: Put<GroupPelunasan, cmnGroupPelunasan>(shell); break;
                        case ValidatableShell<ProsesPBT<NIB_Perorangan>> shell: Put<ProsesPBT<NIB_Perorangan>, cmnProsesPBT>(shell); break;
                        case ValidatableShell<ProsesPBT<NIB_PT>> shell: Put<ProsesPBT<NIB_PT>, cmnProsesPBT>(shell); break;
                        case ValidatableShell<ProsesSPH> shell: Put<ProsesSPH, cmnProsesSPH>(shell); break;
                        case ValidatableShell<ProsesMohonSKKantah> shell: Put<ProsesMohonSKKantah, cmnProsesMohonSKKantah>(shell); break;
                        case ValidatableShell<ProsesMohonSKKanwil> shell: Put<ProsesMohonSKKanwil, cmnProsesMohonSKKanwil>(shell); break;
                        case ValidatableShell<ProsesCetakBukuHGB> shell: Put<ProsesCetakBukuHGB, cmnProsesCetakBuku>(shell); break;
                        case ValidatableShell<ProsesCetakBukuSHM> shell: Put<ProsesCetakBukuSHM, cmnProsesCetakBukuSHM>(shell); break;
                        case ValidatableShell<ProsesTurunHak> shell: Put<ProsesTurunHak, cmnProsesTurunHak>(shell); break;
                        case ValidatableShell<ProsesNaikHak> shell: Put<ProsesNaikHak, cmnProsesNaikHak>(shell); break;
                        case ValidatableShell<ProsesAJB> shell: Put<ProsesAJB, cmnProsesAJB>(shell); break;
                        case ValidatableShell<ProsesBalikNama> shell: Put<ProsesBalikNama, cmnProsesBalikNama>(shell); break;
                        case ValidatableShell<PembayaranPajak> shell: Put<PembayaranPajak, cmnPembayaranPajak>(shell); break;
                        case ValidatableShell<MasukAJB> shell: Put<MasukAJB, cmnMasukAJB>(shell); break;
                    }



                    //var ptype = xshell.pinfo.PropertyType.GetGenericArguments().FirstOrDefault();

                    //switch (ptype?.Name)
                    //{
                    //	case nameof(PersilBasic): Put<PersilBasic,cmnPersilBasic>(xshell.shell); break;
                    //	case nameof(GroupUTJ):Put<GroupUTJ, cmnPersilBasic>(xshell.shell); break;
                    //	case nameof(GroupDP): Put<GroupDP, cmnGroupDP>(xshell.shell); break;
                    //	case nameof(ProsesPerjanjian): Put<ProsesPerjanjian, cmnProsesPerjanjian>(xshell.shell); break;
                    //	//case nameof(ProsesPerjanjianGirik): Put<ProsesPerjanjianGirik, cmnProsesPerjanjianGirik>(xshell.shell); break;
                    //	//case nameof(ProsesPerjanjianSertifikat): Put<ProsesPerjanjianSertifikat, cmnProsesPerjanjianSertifikat>(xshell.shell); break;
                    //	case nameof(GroupPelunasan): Put<GroupPelunasan, cmnGroupPelunasan>(xshell.shell); break;
                    //	case nameof(ProsesPBT): Put<ProsesPBT, cmnProsesPBT>(xshell.shell); break;
                    //	case nameof(ProsesSPH): Put<ProsesSPH, cmnProsesSPH>(xshell.shell); break;
                    //	case nameof(ProsesMohonSKKantah): Put<ProsesMohonSKKantah, cmnProsesMohonSKKantah>(xshell.shell); break;
                    //	case nameof(ProsesMohonSKKanwil): Put<ProsesMohonSKKanwil, cmnProsesMohonSKKanwil>(xshell.shell); break;
                    //	case nameof(ProsesCetakBuku): Put<ProsesCetakBuku, cmnProsesCetakBuku>(xshell.shell); break;
                    //	case nameof(ProsesTurunHak): Put<ProsesTurunHak, cmnProsesTurunHak>(xshell.shell); break;
                    //	case nameof(ProsesNaikHak): Put<ProsesNaikHak, cmnProsesNaikHak>(xshell.shell); break;
                    //	case nameof(ProsesAJB): Put<ProsesAJB, cmnProsesAJB>(xshell.shell); break;
                    //	case nameof(ProsesBalikNama): Put<ProsesBalikNama, cmnProsesBalikNama>(xshell.shell); break;
                    //	case nameof(PembayaranPajak): Put<PembayaranPajak, cmnPembayaranPajak>(xshell.shell); break;
                    //	case nameof(MasukAJB): Put<MasukAJB, cmnMasukAJB>(xshell.shell); break;
                    //}

                    xshell.pinfo.SetValue(data, xshell.shell);
                    if (string.IsNullOrEmpty(data.IdBidang))
                        data.MakeID();
                    contextex.persils.Update(data);
                }

                MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(data.key);

                try
                {
                    contextex.SaveChanges();
                    //MethodBase.GetCurrentMethod().GetCustomAttribute<PersilCSMaterializerAttribute>()
                    //	.ManualExecute(contextex, data.key);
                    if (makebundle)
                    {
                        var bundle = new MainBundle(contextplus, data.key, data.IdBidang);
                        contextplus.bundles.Insert(bundle);
                        contextplus.SaveChanges();
                    }
                    return Ok(data.key);
                }
                catch (Exception eex)
                {
                    MyTracer.TraceError2(eex);
                    return new NotModifiedResult(eex.Message);
                }
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }

            void Put<TIn, TOut>(ValidatableShell<TIn> shell) where TIn : ValidatableItem where TOut : class
            {
                //var dict = ((JObject)item).ToObject<Dictionary<string,object>>();
                var json = System.Text.Json.JsonSerializer.Serialize(item);
                var cmn = System.Text.Json.JsonSerializer.Deserialize<TOut>(json);
                //if (typeof(IExpandable).IsAssignableFrom(typeof(TOut)))
                //	((IExpandable)cmn).CanRemove();
                var itemx = Converter.Reverse<TIn, TOut>(cmn);
                itemx.extras = null;
                switch (oper)
                {
                    case "del": shell.PutEntry((TIn)(object)null, ChangeKind.Delete, User?.key); break;
                    case "edit": shell.PutEntry(itemx, ChangeKind.Update, User?.key); break;
                }
            }


            //IActionResult Returning<T>() where T : ValidatableItem
            //{
            //	string st = JsonConvert.SerializeObject(item);
            //	var vitem = JsonConvert.DeserializeObject<T>(st);
            //	return Update<T>(key, xshell.pinfo.Name, vitem, oper, User);
            //}
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("DATA_FULL,PAYMENT_FULL,PASCA_FULL,MAP_FULL")]
        [HttpPost("persil/{part}/save")]
        public IActionResult SaveItem([FromQuery] string token, [FromQuery] string key, [FromRoute] string part, [FromQuery] string oper,
                                                                [FromBody] object item)
        {
            //HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            var ureg = contextex.FindUserReg(token);
            try
            {
                if (oper == "add" && part != "basic")
                    return new NotModifiedResult("Bidang baru harus mulai dari pengisian data Riwayat Tanah");

                if (ureg.user == null)
                    return Unauthorized();

                return Update2(key, part, item, oper, ureg.user, ureg.regular, ureg.map);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        public class ApprovalItem
        {
            public bool approve { get; set; }
            public string rejectNote { get; set; }
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("DATA_REVIEW, PAYMENT_REVIEW,PASCA_REVIEW,MAP_REVIEW")]
        [HttpPost("persil/{part}/approval")]
        [PersilMaterializer(Async = true)]
        public IActionResult Validate(string token, string key, string part, [FromBody] ApprovalItem item)
        {
            user User;
            Persil data;
            try
            {
                var ureg = contextex.FindUserReg(token);
                User = ureg.user;
                if (User == null)
                    return Unauthorized();

                data = contextex.db.GetCollection<Persil>("persils_v2").Find($"{{key:'{key}'}}").FirstOrDefault();
                if (data == null || (ureg.map || ureg.regular) != (data.regular ?? false))
                    return new NotModifiedResult("Invalid persil key given");

                var xshell = GetShell(data, part);
                if (xshell.pinfo == null || xshell.shell == null)
                    return new NotModifiedResult($"part {part} is not valid");

                var res = Returning((ValidatableShell)xshell.shell);

                //if (res == Ok())
                //{
                //    //var projects = contextex.GetDocuments(new { keyProject = "" }, "static_collections",
                //    //    "{$match: {_t :'mapsDisable'}}",
                //    //    "{$unwind: '$keyProject'}",
                //    //    "{$project: {_id:0, 'keyProject' : 1}}").Select(x => x.keyProject).ToList();

                //    var persil = contextex.persils.FirstOrDefault(x => x.key == data.key);

                //    if (projects.Contains(persil.basic.current.keyProject))
                //    {
                //        if (string.IsNullOrEmpty(persil.basic.current.noPeta))
                //        {
                //            var gnId_noPeta = new NoPetaGenerator(contextex, persil.basic.current.keyDesa);
                //            var IdNoPeta = gnId_noPeta.Generate();
                //            persil.IdBidang = IdNoPeta;
                //            persil.basic.current.noPeta = IdNoPeta;
                //            contextex.persils.Update(persil);
                //        }
                //        else
                //        {
                //            persil.IdBidang = persil.basic.current.noPeta;
                //            contextex.persils.Update(persil);
                //        }
                //    }
                //    else
                //    {
                //        persil.MakeID();
                //        contextex.persils.Update(persil);
                //    }

                //    context.SaveChanges();
                //}

                return Ok();
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }

            IActionResult Returning(ValidatableShell shell)// where T : ValidatableItem
            {
                if (shell == null)
                    return new NotModifiedResult($"part {part} has no entry");
                shell.Validate(User?.key ?? "", item.approve, item.rejectNote);
                //Change How to Update so _t : persil not Removed
                //contextex.db.GetCollection<Persil>("persils_v2").ReplaceOne($"{{key:'{data.key}'}}", data);
                contextex.persils.Update(data);
                contextex.SaveChanges();

                return Ok();
            }
        }

        [EnableCors(nameof(landrope))]
        [NeedToken("DATA_VIEW,DATA_FULL,DATA_REVIEW,PAYMENT_VIEW,PAYMENT_FULL,MAP_VIEW,MAP_FULL,MAP_REVIEW,PASCA_VIEW,PASCA_FULL,PASCA_REVIEW")]
        //[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
        [HttpPost("persil/edit/luaspelunasan")]
        public IActionResult EditLuasPelunasan([FromQuery] string token, [FromBody] PersilCore3 core)
        {
            try
            {
                var user = contextex.FindUser(token);
                var persil = GetPersil(core.key);

                var pelunasan = new pelunasan();
                pelunasan.keyCreator = user.key;
                pelunasan.luasPelunasan = Convert.ToDouble(core.luasPelunasan);
                pelunasan.created = DateTime.Now;
                pelunasan.reason = core.reason;

                var lst = new List<pelunasan>();
                if (persil.paidHistories != null)
                    lst = persil.paidHistories.ToList();

                lst.Add(pelunasan);

                persil.paidHistories = lst.ToArray();
                persil.luasPelunasan = core.luasPelunasan;

                contextex.persils.Update(persil);
                contextex.SaveChanges();

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

        ////[NeedToken("PRABEBAS_VIEW,PASKABEBAS_VIEW")]
        //[HttpGet("persil/problem/{part}")]
        //public IActionResult GetPartProblems(string token, string part, string key)
        //{
        //	try
        //	{
        //		if (string.IsNullOrEmpty(part))
        //			return new NoContentResult();
        //		var data = contextex.db.GetCollection<Persil>("expersils").Find($"{{key:'{key}'}}").FirstOrDefault();
        //		if (data == null)
        //			return new NoContentResult();

        //		//ValidatableShell<ValidatableItem> shell = null;
        //		switch (part.ToLower())
        //		{
        //			case "basic": return Returning(data.basic);
        //			case "arsip": return Returning(data.arsip);
        //			case "utj": return Returning(data.gUTJ);
        //			case "dp": return Returning(data.gDP);
        //			case "pbtperorangan": return Returning(data.PBTPerorangan);
        //			case "pelunasan": return Returning(data.gpelunasan);
        //			case "ppjb": return Returning(data.PPJB);
        //			case "aktakuasa": return Returning(data.aktaKuasa);
        //			case "aktakesepakatan": return Returning(data.aktaKesepakatan);
        //			case "aktapengosongan": return Returning(data.aktaPengosongan);
        //			case "sph": return Returning(data.SPH);
        //			case "pbtpt": return Returning(data.PBTPT);
        //			case "mohonsk": return Returning(data.mohonSK);
        //			case "penurunanhak": return Returning(data.penurunanHak);
        //			case "baliknama": return Returning(data.balikNama);
        //		}
        //		return new NoContentResult();
        //	}
        //	catch (Exception ex)
        //	{
        //		return new InternalErrorResult(ex.Message);
        //	}

        //	IActionResult Returning<T>(ValidatableShell<T> shell) where T : ValidatableItem
        //	{
        //		var data = shell.problems;
        //		if ((data == null || data.Count == 0) && token == "SAMPLE")
        //			data = new problem[] { new problem() }.ToList();
        //		return Ok(data);
        //	}
        //}

        //[HttpPost("persil/problem/{part}/save")]
        //public IActionResult SavePartProblems([FromQuery]string token, [FromRoute]string part, [FromQuery]string key, [FromQuery]string oper,
        //																				[FromBody] problem item)
        //{
        //	Persil data;
        //	user User;
        //	try
        //	{
        //		User = AuthController.FindUser(contextex, token);
        //		if (User == null)
        //		{
        //			// not implemented yet
        //		}

        //		oper = oper?.ToLower();
        //		if (!new[] { "add", "edit", "del" }.Contains(oper))
        //			return new NotModifiedResult("Invalid operation parameter");
        //		if (string.IsNullOrEmpty(part))
        //			return new NoContentResult();
        //		data = contextex.db.GetCollection<Persil>("expersils").Find($"{{key:'{key}'}}").FirstOrDefault();
        //		if (data == null)
        //			return new NoContentResult();

        //		switch (part.ToLower())
        //		{
        //			case "basic": return Process(data.basic);
        //			case "arsip": return Process(data.arsip);
        //			case "utj": return Process(data.gUTJ);
        //			case "dp": return Process(data.gDP);
        //			case "pbtperorangan": return Process(data.PBTPerorangan);
        //			case "pelunasan": return Process(data.gpelunasan);
        //			case "ppjb": return Process(data.PPJB);
        //			case "aktakuasa": return Process(data.aktaKuasa);
        //			case "aktakesepakatan": return Process(data.aktaKesepakatan);
        //			case "aktapengosongan": return Process(data.aktaPengosongan);
        //			case "sph": return Process(data.SPH);
        //			case "pbtpt": return Process(data.PBTPT);
        //			case "mohonsk": return Process(data.mohonSK);
        //			case "penurunanhak": return Process(data.penurunanHak);
        //			case "baliknama": return Process(data.balikNama);
        //		}
        //		return new NoContentResult();
        //	}
        //	catch (InvalidDataException ex1)
        //	{
        //		return new UnprocessableEntityObjectResult(ex1.Message);
        //	}
        //	catch (KeyNotFoundException)
        //	{
        //		return new NotModifiedResult("The problem not found in the part");
        //	}
        //	catch (Exception ex3)
        //	{
        //		return new InternalErrorResult(ex3.Message);
        //	}

        //	IActionResult Process<T>(ValidatableShell<T> shell) where T : ValidatableItem
        //	{
        //		if (oper != "del")
        //		{
        //			if (string.IsNullOrWhiteSpace(item.subject))
        //				throw new InvalidDataException("Subject cannot empty");
        //		}
        //		switch (oper)
        //		{
        //			case "add": Add(shell); break;
        //			case "del": Del(shell); break;
        //			case "edit": Edit(shell); break;
        //		}
        //		contextex.db.GetCollection<Persil>("expersils").ReplaceOne($"{{key:'{key}'}}", data);
        //		return Ok();
        //	}

        //	void Add<T>(ValidatableShell<T> shell) where T : ValidatableItem
        //	{
        //		if (shell.problems == null)
        //			shell.problems = new List<problem>();
        //		item.issued = DateTime.Now;
        //		item.keyIssuer = User?.key ?? "";
        //		item.validated = null;
        //		item.keyValidator = null;
        //		item.updated = null;
        //		item.keyUpdater = null;
        //		shell.problems.Add(item);
        //	}

        //	void Edit<T>(ValidatableShell<T> shell) where T : ValidatableItem
        //	{
        //		if (shell.problems == null)
        //			throw new KeyNotFoundException();
        //		var idx = shell.problems.FindIndex(p => p.issued == item.issued);
        //		if (idx == -1)
        //			throw new KeyNotFoundException();
        //		var olditem = shell.problems[idx];

        //		if (olditem.validated == null && item.validated.HasValue)
        //		{
        //			item.keyValidator = User?.key ?? "";
        //		}
        //		else if (olditem.validated.HasValue && olditem.published
        //			&& olditem.subject != item.subject || olditem.descr != item.descr)
        //		{
        //			item.updated = DateTime.Now;
        //			item.published = false;
        //			item.keyValidator = null;
        //			item.keyUpdater = User?.key ?? "";
        //			item.validated = null;
        //		}

        //		shell.problems[idx] = item;
        //	}

        //	void Del<T>(ValidatableShell<T> shell) where T : ValidatableItem
        //	{
        //		if (shell.problems == null)
        //			throw new KeyNotFoundException();
        //		var idx = shell.problems.FindIndex(p => p.issued == item.issued);
        //		if (idx == -1)
        //			throw new KeyNotFoundException();
        //		shell.problems.RemoveAt(idx);
        //	}
        //}

        /// <summary>
        /// Perbaikan data notaris double
        /// </summary>
        [HttpPost("persil/fix-data-notaris")]
        public IActionResult FixDataNotaris()
        {
            try
            {
                var static_Col = context.GetDocuments(new { KeyLama = "", KeyBaru = "", Identifier = "" }, "static_collections",
                    "{'$match' : {_t : 'PerbaikanDataNotaris'}}",
                    "{'$unwind' : '$details'}",
                    @"{'$project' : {
                        'KeyLama' : '$details.keyLama',
                        'KeyBaru' : '$details.keyBaru',
                        'Identifier' : '$details.identifier',
                        _id : 0 }}"
                    ).ToArray();

                var listKey = static_Col.Select(a => a.KeyLama).Distinct().ToList();
                var persils = contextplus.persils.Query(p => listKey.Contains(p.PraNotaris) 
                                || p.postNotaris.Any(pp => listKey.Contains(pp.key))
                                || listKey.Contains(p.basic.current.order.keyNotaris)
                                || p.basic.entries.Any(e => listKey.Contains(e.item.order.keyNotaris))
                                ).ToList();

                if (persils?.Count > 0)
                {
                    persils.Where(p => listKey.Contains(p.PraNotaris)).ToList().ForEach(u =>
                    {
                        u.PraNotaris = static_Col.ToList().Find(s => u.PraNotaris == s.KeyLama).KeyBaru;
                    });

                    persils.Where(p => listKey.Contains(p.basic?.current?.order?.keyNotaris)).ToList().ForEach(u =>
                    {
                        u.basic.current.order.keyNotaris = static_Col.ToList().Find(s => u.basic.current.order.keyNotaris == s.KeyLama).KeyBaru;
                    });


                    if (persils.Exists(p => p.postNotaris?.Length > 0))
                    {
                        if (persils.Exists(p => p.postNotaris.Any(pp => static_Col.Any(s => pp.key == s.KeyLama))))
                        {
                            Parallel.ForEach(persils, i =>
                            {
                                i.postNotaris.Where(a => static_Col.Any(s => a.key == s.KeyLama)).ToList().ForEach(p =>
                                {
                                    p.key = static_Col.ToList().Find(s => p.key == s.KeyLama).KeyBaru;

                                });
                            });
                        }
                    }

                    if (persils.Exists(p => p.basic?.entries?.Count > 0))
                    {
                        if (persils.Exists(p => p.basic.entries.Any(pp => static_Col.Any(s => pp.item?.order?.keyNotaris == s.KeyLama))))
                        {
                            Parallel.ForEach(persils, i =>
                            {
                                i.basic.entries.Where(p => listKey.Contains(p.item?.order?.keyNotaris)).ToList().ForEach(u =>
                                {
                                    u.item.order.keyNotaris = static_Col.ToList().Find(s => u.item?.order?.keyNotaris == s.KeyLama).KeyBaru;
                                });
                            });
                        }
                    }

                    contextplus.persils.Update(persils);
                    contextplus.SaveChanges();
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

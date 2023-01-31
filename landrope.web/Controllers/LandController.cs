using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using landrope.mod;
using MongoDB.Driver;
using MongoDB.Bson;
using GeomHelper;
using System.Threading;
using System.Globalization;
using Tracer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Mvc.Rendering;
using Swashbuckle.AspNetCore.SwaggerGen;
//using Google.Rpc;
using landrope.web.Models;
using landrope.common;
//using System.Text.Json.Serialization;
//using System.Text.Json;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using HttpAccessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.IO;
using CadLoader;
using landrope.mod2;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc.Routing;
using geo.shared;
using Microsoft.AspNetCore.Hosting;
using landrope.material;
using System.Reflection.Metadata;
using System.Reflection;

namespace landrope.web.Controllers
{
#if (_USE_MONGODB_)
    using GeoPoint = geoPoint;
#endif


    [Route("api/entities")]
    [ApiController]
    public class LandController : ControllerBase
    {
        //public LandController(IMemoryContext memcontext)
        //{
        //	this.memcontext = memcontext;
        //}
        //IMemoryContext memcontext;

        //Connection conn = new Connection();
        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextex = Contextual.GetContextExt();

        //[EnableCors(nameof(landrope))]
        //[HttpPost("project/list")]
        //[NeedToken("VIEW_MAP", true)]
        //public IActionResult GetProjectList()
        //{
        //	var data = memcontext.Raw.Where(p => p.TheArea > 0).Select(p => new { p.key, p.identity, p.Area })
        //								.OrderBy(p => p.key);
        //	return new JsonResult(data);
        //}

        //[EnableCors(nameof(landrope))]
        //[HttpPost("village/list")]
        //[NeedToken("VIEW_MAP", true)]
        //public IActionResult GetVillageList(string projkey)
        //{
        //	string[] prjkeys = (projkey ?? "").Split(',', ';', '|');
        //	if (!prjkeys.Any())
        //		return new NoContentResult();

        //	var stprojects = $"['{string.Join("','", prjkeys)}']";
        //	var data = context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
        //											"villages", $"{{'project.key':{{$in{stprojects}}}}}", "{'village.areas':0}").ToList().Select(l => l.village)
        //											.OrderBy(v => v.identity);
        //	return new JsonResult(data);
        //}

        //[EnableCors(nameof(landrope))]
        //[HttpPost("land/list")]
        //[NeedToken("VIEW_MAP", true)]
        //public IActionResult GetLandList(string villakey, string projkey)
        //{
        //	string[] vilkeys = villakey == null ? new string[0] : villakey.Split(',', ';', '|');
        //	if (projkey != null)
        //	{
        //		string[] prjkeys = (projkey ?? "").Split(',', ';', '|');
        //		if (!prjkeys.Any())
        //			return new NoContentResult();

        //		var stprojects = $"['{string.Join("','", prjkeys)}']";

        //		var proj = context.GetCollections(new Project(), "maps", $"{{key:{{$in:{stprojects}}}}}").ToList();
        //		if (proj != null)
        //			vilkeys = proj.SelectMany(p => p.villages.Select(v => v.key)).ToArray();
        //	}
        //	if (!vilkeys.Any())
        //		return new NoContentResult();

        //	var stvillas = $"['{string.Join("','", vilkeys)}']";
        //	var data = context.GetCollections(new Land(),
        //																	"lands", $"{{'vilkey':{{$in:{stvillas}}}}}", "{_id:0,areas:0}").ToList().
        //																	OrderBy(l => l.identity);
        //	return new JsonResult(data);
        //}

        //public IActionResult map(string t, string key)
        //{
        //	Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        //	//object[] entities;
        //	t = t?.ToLower();
        //	IEnumerable<landbase> entities = null;
        //	var keys = string.Join(",",(key?.Split(',', ';', '|').Select(s => "'" + s + "'").ToArray() ?? new string[0]));
        //	switch (t)
        //	{
        //		case "p":
        //			entities = context.GetCollections(new Project(), "project_only", "{}", "{villages:0}").ToList();
        //			break;
        //		case "s":
        //			entities = context.GetCollections(new { project = new { key = "", identity = "" }, village = new Village() },
        //																	"villages", $"{{'project.key':{{$in:[{keys}]}}}}").ToList().Select(l => l.village);
        //			break;
        //		case "t":
        //			entities = context.GetCollections(new
        //			{
        //				project = new { key = "", identity = "" },
        //				village = new { key = "", identity = "" },
        //				land = new Land()
        //			},"land_full", $"{{'project.key':{{$in:[{keys}]}}}}", "{'land._id':0}").ToList().Select(l => l.land);
        //			break;
        //		case "g":
        //			entities = context.GetCollections(new Land(), "lands", $"{{'vilkey':{{$in:[{keys}]}}}}","{_id:0}").ToList();
        //			break;
        //	}

        //	var go = ToGmap(entities, new[] { "t","s" }.Contains(t));
        //	return new JsonResult(go);
        //}

        //[EnableCors(nameof(landrope))]
        //[HttpPost("/api/map")]
        //[NeedToken("VIEW_MAP", true)]
        //public IActionResult map([FromForm]string token, [FromForm]string key)
        //{
        //	Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        //	//object[] entities;
        //	var keys = key?.Split(',', ';', '|') ?? new string[0];
        //	var lands = memcontext.Raw.Where(p => keys.Contains(p.key))
        //							.Select(p => (pkey: p.key, lands: p.villages.SelectMany(v => v.lands).ToList())).ToList();
        //	var villages = memcontext.Raw.Where(p => keys.Contains(p.key))
        //							.SelectMany(p => p.villages.Select(v => (pkey: p.key, vill: v)));
        //	var Areas = GetArea(keys, null, null);

        //	var golands = lands.SelectMany(l => l.lands.Select(ll => (l.pkey, status: ll.ls_status, land: ll))
        //					.GroupBy(x => x.pkey)
        //					.Select(g => (key: g.Key, dict: g.Select(x => (x.status, x.land))
        //								.GroupBy(x => x.status).Select(gg => (status: gg.Key, lands: gg.Select(xx => xx.land.ToGmap()).ToArray()))
        //										.ToDictionary(x => (int)x.status, x => x.lands)))).ToList();
        //	var Areas2 = Areas.GroupBy(a => a.key).Select(g => (key: g.Key, dict: g.ToDictionary(x => (int)x.status, x => x.Area)));

        //	var govillages = villages.GroupBy(v => v.pkey)
        //					.Select(g => (key: g.Key, SKs: g.ToDictionary(x => x.vill.key, x => x.vill.ToGmap()))).ToList();

        //	var combo = golands.Join(govillages, l => l.key, v => v.key, (l, v) => (l.key, l.dict, v.SKs))
        //		.Join(Areas2, x => x.key, a => a.key, (x, a) => (x.key, lands: x.dict, x.SKs, Areas: a.dict))
        //		.Select(x => new MapDataValue { key = x.key, Areas = x.Areas, Lands = x.lands, SKs = x.SKs }).ToArray();
        //	var str = JsonConvert.SerializeObject(combo);
        //	return new ContentResult { Content = str, ContentType = "Application/json", StatusCode = 200 };
        //	//return Ok(combo);
        //}

        //gmapObject ToGmap(IEnumerable<landbase> entities, bool byident = false)
        //{
        //	geoFeatureCollection coll = new geoFeatureCollection();

        //	var aentities = entities.Where(e => e.areas.Any() && e.areas.First().coordinates.Any()).ToList();
        //	if (!aentities.Any())
        //		return coll;
        //	aentities.ForEach(e =>
        //	{
        //		if (e.Center.Latitude == double.NaN || e.Center.Longitude == double.NaN)
        //			e.Center = e.areas.First().coordinates.First();
        //	});
        //	var centers = aentities.Select(e => (e.key, pt: e.Center)).ToList();
        //	var feats1 = aentities.Select(e => (e.key, id: e.identity, gp: (geoFeature)e.ToGmap())).ToList();
        //	if (!byident)
        //	{
        //		var feats2 = feats1.GroupJoin(centers, x => x.key, y => y.key, (x, sy) => (x.gp, sy.DefaultIfEmpty().First().pt));
        //		var feats3 = feats2.OrderByDescending(x => x.pt?.Latitude ?? 0).ThenBy(x => x.pt?.Longitude ?? 0);
        //		var feats = feats3.Select((x, i) => (x.gp, i)).ToList();
        //		feats.ForEach(x => x.gp.properties.Add("label", $"{x.i}"));
        //		coll.features = feats.Select(x => x.gp).ToArray();
        //	}
        //	else
        //	{
        //		feats1.ForEach(x => x.gp.properties.Add("label", x.id));
        //		coll.features = feats1.Select(x => x.gp).ToArray();
        //	}
        //	var ccount = centers.Count;

        //	coll.center = new Point(centers.Sum(c => c.pt.Longitude) / ccount, centers.Sum(c => c.pt.Latitude) / ccount);
        //	return coll;
        //}

        //[HttpPost("/api/info/areas")]
        //internal List<RegArea> GetArea(string projkey = null, string vilkey = null, string landkey = null)
        //{
        //	string[] projkeys = projkey?.Split(',', ';', '|');
        //	string[] vilkeys = vilkey?.Split(',', ';', '|');
        //	string[] landkeys = landkey?.Split(',', ';', '|');
        //	return GetArea(projkeys, vilkeys, landkeys);
        //}

        //internal List<RegArea> GetArea(string[] projkeys = null, string[] vilkeys = null, string[] landkeys = null)
        //{
        //	List<RegArea> Areas = new List<RegArea>();
        //	try
        //	{
        //		if (projkeys != null)
        //		{
        //			var lands = memcontext.Raw.Where(p => projkeys.Contains(p.key))
        //							.SelectMany(p => p.villages.SelectMany(v => v.lands.Select(l => (pkey: p.key, land: l))));
        //			Areas = lands.GroupBy(l => (l.pkey, status: l.land.ls_status))
        //			.Select(g => new RegArea { key = g.Key.pkey, status = g.Key.status, Area = g.Sum(x => x.land.TheArea) }).ToList();
        //			//var summary = Areas.GroupBy(a => a.status).Select(g => new RegArea { key = "*", status = g.Key, Area = g.Sum(x => x.Area) });
        //			//Areas.AddRange(summary);
        //		}
        //		else if (vilkeys != null)
        //		{
        //			var lands = memcontext.Raw.SelectMany(p => p.villages).Where(v => vilkeys.Contains(v.key))
        //							.SelectMany(v => v.lands.Select(l => (vkey: v.key, land: l)));
        //			Areas = lands.GroupBy(l => (l.vkey, status: l.land.ls_status))
        //			.Select(g => new RegArea { key = g.Key.vkey, status = g.Key.status, Area = g.Sum(x => x.land.TheArea) }).ToList();
        //			//var summary = Areas.GroupBy(a => a.status).Select(g => new RegArea { key = "*", status = g.Key, Area = g.Sum(x => x.Area) });
        //			//Areas.AddRange(summary);
        //		}
        //		else if (landkeys != null)
        //		{
        //			var lands = memcontext.Raw.SelectMany(p => p.villages).SelectMany(v => v.lands)
        //							.Where(l => landkeys.Contains(l.key));
        //			Areas = lands.GroupBy(l => (l.key, status: l.ls_status))
        //			.Select(g => new RegArea { key = g.Key.key, status = g.Key.status, Area = g.Sum(x => x.TheArea) }).ToList();
        //			//var summary = Areas.GroupBy(a => a.status).Select(g => new RegArea { key = "*", status = g.Key, Area = g.Sum(x => x.Area) });
        //			//Areas.AddRange(summary);
        //		}
        //		Areas.ForEach(a => a.Area = Math.Truncate(a.Area));
        //		return Areas;
        //		//return new JsonResult(Areas.OrderBy(a=>a.key).ThenBy(a=>a.status));
        //	}
        //	catch (Exception ex)
        //	{
        //		MyTracer.TraceError2(ex);
        //		//return new StatusCodeResult(500);
        //		return new List<RegArea>();
        //	}
        //}

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/upload")]
        //[NeedToken("MAP_FULL", true)]
        //[Consumes("multipart/form-data")]
        [PersilCSMaterializer]
        public IActionResult UploadMap([FromQuery] string token, [FromQuery] string key, [FromQuery] string nopeta, IFormFile myFile)
        {
            //MyTracer.TraceInfo2($"Enter... File namw={myFile?.FileName}");
            var persil = contextex.persils.FirstOrDefault(p => p.key == key);
            if (persil == null)
                return new NotModifiedResult("Invalid persil key given");
            var user = contextex.FindUser(token);

            try
            {
                var basic = persil.basic;
                var lastentry = basic.entries.Where(e => e.item != null).Last();
                var item = lastentry.item;
                nopeta = nopeta?.Trim();
                if (!string.IsNullOrEmpty(nopeta) && item.noPeta != nopeta)
                {
                    var nitem = JsonConvert.DeserializeObject<PersilBasic>(JsonConvert.SerializeObject(item));
                    nitem.noPeta = nopeta;
                    persil.basic.PutEntry(nitem, ChangeKind.Update, user.key);
                    contextex.persils.Update(persil);
                }

                if (myFile == null || myFile.Length == 0)
                {
                    contextex.SaveChanges();
                    return Ok();
                }
                else
                {
                    //MyTracer.TraceInfo2("1000...");
                    //var curdir = HttpAccessor.Config.env.ContentRootPath;
                    var curdir = ControllerContext.GetContentRootPath();
                    //MyTracer.TraceInfo2("2000...");
                    var path = Path.Combine(Path.Combine(curdir, "uploads"), user.key);
                    //MyTracer.TraceInfo2("3000...");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    //MyTracer.TraceInfo2("5000...");
                    var filename = myFile?.FileName;
                    var filesize = (int)myFile?.Length;
                    //MyTracer.TraceInfo2("6000...");
                    var dstname = Path.Combine(path, filename);
                    //MyTracer.TraceInfo2("7000...");
                    var filestrm = System.IO.File.Create(dstname);
                    myFile.CopyTo(filestrm);
                    filestrm.Flush();
                    filestrm.Close();
                    //MyTracer.TraceInfo2("8000...");
                    try
                    {
                        //var memstrm = new MemoryStream();
                        //	file.CopyTo(memstrm);
                        //memstrm.Flush();
                        //memstrm.Seek(0, SeekOrigin.Begin);
                        //MyTracer.TraceInfo2("9000...");
                        var procs = new processor(context, contextex);
                        //var res = procs.ProcessSingle(memstrm, persil.key, user);
                        //MyTracer.TraceInfo2("10000...");
                        var res = procs.ProcessSingle(dstname, persil.key, user, filesize);
                        //MyTracer.TraceInfo2("11000...");
                        contextex.SaveChanges();
                        if (string.IsNullOrEmpty(res))
                            return Ok();
                        //MyTracer.TraceInfo2("12000...");
                        return new UnprocessableEntityObjectResult(res);
                        //MyTracer.TraceInfo2("quit...");
                    }
                    finally
                    {
                        //MyTracer.TraceInfo2("post quit...");
                        System.IO.File.Delete(dstname);
                        //context.MaterializeAsync("persilMapsProject", "material_persil_map");
                        //MyTracer.TraceInfo2("final quit...");
                    }
                }
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new InternalErrorResult(ex.Message);
            }
        }


        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/metadata")]
        [NeedToken("MAP_REVIEW", true)]
        [PersilMaterializer]
        //[Consumes("multipart/form-data")]
        public IActionResult UploadMapInfo([FromQuery] string token, [FromQuery] string key)
        {
            var persil = contextex.persils.FirstOrDefault(p => p.key == key);
            if (persil == null)
                return new UnprocessableEntityObjectResult("Invalid persil key given");

            try
            {
                var map = new
                {
                    key = "",
                    uploaded = new DateTime(),
                    fileName = "",
                    filesize = 0,
                    filecreated = new DateTime(),
                    fileupdated = new DateTime(),
                    fileupdater = "",
                    uploader = ""
                };

                map = context.GetDocuments(map, "persilMaps",
                            "{$project: { key: '$key',entcount: {$size: '$entries'},entries: '$entries'} }",
                            $"{{$match: {{ entcount: {{$gt: 0}},key: '{key}'}} }}",
                            @"{$project: { key: '$key',last: {$cond:[{$gt:['$entcount', 1]},
									{$arrayElemAt:['$entries', -1]},{$arrayElemAt:['$entries', 0]} ]} } }",
                            "{$match: { 'last.reviewed':null} }",
                            @"{$project: {
									key: '$key',uploaded: '$last.uploaded',
									keyuploader: '$last.keyUploader',
                  fileName: '$last.sourceFile',filesize: '$last.metadata.filesize',
                  filecreated: '$last.metadata.created',fileupdated: '$last.metadata.updated',
                  fileupdater: '$last.metadata.updater'}}",
                            "{$lookup: {from: 'securities',localField: 'keyuploader', foreignField: 'key',as:'uploader'}}",
                            "{$unwind: { path: '$uploader',preserveNullAndEmptyArrays: true} }",
                            @"{$project: {_id:0, key: 1,uploaded: 1,uploader: '$uploader.FullName',
                   fileName: 1,filesize: 1,filecreated: 1,fileupdated: 1,fileupdater: 1}}"
                    ).FirstOrDefault();
                if (map == null)
                    return Ok();
                return Ok(map);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/new-persil")]
        [NeedToken("MAP_FULL", true)]
        [PersilCSMaterializer]
        //[Consumes("multipart/form-data")]
        public IActionResult UploadMapNew([FromQuery] string token,
                                                [FromQuery] string keyProject, [FromQuery] string keyDesa,
                                                [FromQuery] string proses, [FromQuery] string jenis, [FromQuery] string nama,
                                                [FromQuery] string nomor, [FromQuery] string nopeta, [FromQuery] double? luas,
                                                [FromQuery] string group, [FromQuery] string alias, IFormFile myFile)
        {
            var user = contextex.FindUser(token);

            Project proj = context.GetCollections(new Project(), "maps", $"{{key:'{keyProject}'}}").FirstOrDefault();
            if (proj == null)
                return new UnprocessableEntityObjectResult("Invalid project key givem");
            Village desa = proj.villages.FirstOrDefault(v => v.key == keyDesa);
            if (desa == null)
                return new UnprocessableEntityObjectResult("Invalid desa key givem");

            JenisProses pros = Enum.TryParse<JenisProses>(proses.ToLower(), out JenisProses jp) ? jp : JenisProses.batal;
            //if (pros == JenisProses.batal)
            //	return new UnprocessableEntityObjectResult("Jenis proses tidak valid");
            JenisAlasHak jalas = Enum.TryParse<JenisAlasHak>(jenis.ToLower(), out JenisAlasHak jal) ? jal : JenisAlasHak.unknown;
            Persil persil = (pros, jalas) switch
            {
                (JenisProses.hibah, _) => new PersilHibah(),
                (JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.girik) => new PersilGirik(),
                (JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.shm) => new PersilSHM(),
                (JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.shp) => new PersilSHP(),
                (JenisProses.standar or JenisProses.overlap or JenisProses.batal, JenisAlasHak.hgb) => new PersilHGB(),
                _ => null
            };
            if (persil == null)
                return new UnprocessableEntityObjectResult("Kombinasi jenis proses + jenis alas hak tidak valid");
            persil.key = mongospace.MongoEntity.MakeKey;
            persil.en_state = pros switch
            {
                //JenisProses.overlap => StatusBidang.overlap,
                JenisProses.batal => StatusBidang.batal,
                _ => StatusBidang.belumbebas
            };
            persil.regular = true;

            MyTracer.TraceInfo2($"Insert New Persil... key:{persil.key}, en_state:{persil.en_state},project:{proj.identity},desa:{desa.identity}" +
                $"alashak:{nomor}");
            MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);

            ////For New Project
            //var projects = contextex.GetDocuments(new { keyProject = "" }, "static_collections",
            //            "{$match: {_t :'mapsDisable'}}",
            //            "{$unwind: '$keyProject'}",
            //            "{$project: {_id:0, 'keyProject' : 1}}").Select(x => x.keyProject).ToList();

            //if (projects.Contains(proj.key))
            //{
            //    var gnId_noPeta = new NoPetaGenerator(contextex, desa.key);
            //    var IdNoPeta = gnId_noPeta.Generate();
            //    nopeta = IdNoPeta;
            //}
            ////

            var entry = new PersilBasic
            {
                en_jenis = jalas,
                en_proses = pros,
                keyProject = proj.key,
                keyDesa = desa.key,
                luasSurat = luas,
                noPeta = nopeta,
                pemilik = nama,
                group = group,
                alias = alias,
                surat = new landrope.mod2.AlasHak { nomor = nomor }
            };
            //var curdir = HttpAccessor.Config.env.ContentRootPath;
            var curdir = ControllerContext.GetContentRootPath();
            var path = Path.Combine(Path.Combine(curdir, "uploads"), user.key);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var filename = myFile?.FileName;
            var filesize = (int)myFile?.Length;
            var dstname = Path.Combine(path, filename);
            var filestrm = System.IO.File.Create(dstname);
            myFile.CopyTo(filestrm);
            filestrm.Flush();
            filestrm.Close();
            var tag = "1000";
            try
            {
                try
                {
                    persil.basic.PutEntry(entry, ChangeKind.Add, user.key);
                    tag = "2000";
                    contextex.persils.Insert(persil);

                    tag = "3000";
                    var procs = new processor(context, contextex);
                    tag = "4000";
                    var res = procs.ProcessSingle(dstname, persil, user, filesize);
                    tag = "5000";
                    contextex.SaveChanges();

                    MyTracer.TraceInfo2("Finish inserting persil and map...");
                    if (string.IsNullOrEmpty(res))
                        return Ok();
                    return new UnprocessableEntityObjectResult(res);
                }
                finally
                {
                    tag = "6000";
                    System.IO.File.Delete(dstname);
                    //context.MaterializeAsync("persilMapsProject", "material_persil_map");
                    //MethodBase.GetCurrentMethod().GetCustomAttribute<PersilCSMaterializerAttribute>()
                    //	.ManualExecute(contextex, persil.key);
                }
            }
            catch (Exception ex)
            {
                contextex.DiscardChanges();
                MyTracer.TraceError2(new Exception($@"Error on tag {tag} with params: 
kP:{keyProject},kD:{keyDesa},p:{proses},j:{jenis},n:{nama},no:{nomor},nP:{nopeta},l:{luas},
fN:{myFile.FileName},fS:{myFile.Length}, destination file:{dstname}", ex));
                return new InternalErrorResult(ex.Message);
                //ses.AbortTransaction();
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/upd-persil")]
        [NeedToken("MAP_FULL", true)]
        [PersilMaterializer]
        //[Consumes("multipart/form-data")]
        public IActionResult PersilUpdate([FromQuery] string token,
                                                [FromQuery] string keyProject, [FromQuery] string keyDesa,
                                                [FromQuery] string proses, [FromQuery] string jenis, [FromQuery] string nama,
                                                [FromQuery] string nomor, [FromQuery] string nopeta, [FromQuery] double? luas,
                                                [FromQuery] string group, [FromQuery] string alias, [FromQuery] string key)
        {
            try
            {
                var user = contextex.FindUser(token);

                var persil = contextex.persils.FirstOrDefault(p => p.key == key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Invalid persil key givem");
                Project proj = context.GetCollections(new Project(), "maps", $"{{key:'{keyProject}'}}").FirstOrDefault();
                if (proj == null)
                    return new UnprocessableEntityObjectResult("Invalid project key givem");
                Village desa = proj.villages.FirstOrDefault(v => v.key == keyDesa);
                if (desa == null)
                    return new UnprocessableEntityObjectResult("Invalid desa key givem");

                JenisProses pros = Enum.TryParse<JenisProses>(proses.ToLower(), out JenisProses jp) ? jp : JenisProses.batal;
                JenisAlasHak jalas = Enum.TryParse<JenisAlasHak>(jenis.ToLower(), out JenisAlasHak jal) ? jal : JenisAlasHak.unknown;
                var item = new PersilBasic
                {
                    en_jenis = jalas,
                    en_proses = pros,
                    keyProject = proj.key,
                    keyDesa = desa.key,
                    luasSurat = luas,
                    noPeta = nopeta,
                    pemilik = nama,
                    group = group,
                    alias = alias,
                    surat = new landrope.mod2.AlasHak { nomor = nomor }
                };
                persil.basic.entries.Add(
                    new ValidatableEntry<PersilBasic>
                    {
                        created = DateTime.Now,
                        en_kind = ChangeKind.Update,
                        keyCreator = user.key,
                        keyReviewer = user.key,
                        reviewed = DateTime.Now,
                        approved = true,
                        item = item
                    }
                );
                persil.basic.current = item;
                contextex.persils.Update(persil);
                contextex.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(new Exception($@"Error updating key {key} with params: 
kP:{keyProject},kD:{keyDesa},p:{proses},j:{jenis},n:{nama},no:{nomor},nP:{nopeta},l:{luas}", ex));
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/allow-new")]
        //[Consumes("multipart/form-data")]
        public IActionResult UploadCanNew([FromQuery] string token)
        {
            var user = contextex.FindUser(token);
            var privs = user.getPrivileges(null).Select(a => a.identifier).ToArray();
            var allow = privs.Contains("MAP_FULL");
            return Ok(allow);
        }

        public class ValidationData
        {
            public string key { get; set; }
            public bool approved { get; set; }
            public string rejectnote { get; set; }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/validate")]
        [NeedToken("MAP_REVIEW", true)]
        [Consumes("application/json")]
        [PersilCS2Materializer]
        public IActionResult ValidatedMap([FromQuery] string token, [FromBody] ValidationData data)
        {
            //MyTracer.TraceInfo2($"Enter... File namw={myFile?.FileName}");
            var persil = contextex.persils.FirstOrDefault(p => p.key == data.key);
            if (persil == null)
                return new UnprocessableEntityObjectResult("Invalid persil key given");
            var user = contextex.FindUser(token);

            MethodBase.GetCurrentMethod().SetKeyValue<PersilCS2MaterializerAttribute>(data.key);

            var persilmap = contextex.persilmaps.FirstOrDefault(m => m.key == data.key);
            if (persilmap == null)
                return new UnprocessableEntityObjectResult("Belum ada upload map untu persil ini");
            try
            {
                var lastent = persilmap.entries.LastOrDefault(e => e.reviewed == null);
                if (lastent == null)
                    return new UnprocessableEntityObjectResult("Tidak ada yang perlu divalidasi");
                if (!data.approved && string.IsNullOrWhiteSpace(data.rejectnote))
                    return new UnprocessableEntityObjectResult("Penolakan harus disertai alasannya");
                persilmap.Validate(user, data.approved, data.rejectnote);
                contextex.persilmaps.Update(persilmap);
                //if (data.approved && persil.en_state == StatusBidang.belumbebas)
                //	persil.basic.Validate(user.key, true, null);
                //contextex.persils.Update(persil);

                contextex.SaveChanges();
                //MethodBase.GetCurrentMethod().GetCustomAttribute<PersilMaterializerAttribute>()
                //	.ManualExecute(contextex, data.key);
                MyTracer.TraceInfo2($"End validation on persil key {data.key}");
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                contextex.DiscardChanges();
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/get-info")]
        [NeedToken("MAP_REVIEW,MAP_FULL", true)]
        public IActionResult GetdMapInfo([FromQuery] string token, [FromQuery] string key)
        {
            var statuss = new StatusBidang[] { StatusBidang.belumbebas, StatusBidang.kampung };
            try
            {
                var persil = contextex.persils.FirstOrDefault(p => p.key == key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Invalid persil key given");
                if (!statuss.Contains(persil.en_state ?? StatusBidang.bebas))
                    return new UnprocessableEntityObjectResult("Hanya bidang belum bebas dan kampung yang bisa diupdate dari sini");

                var keyp = persil.basic.current?.keyProject;
                var projects = context.GetCollections(new { key = "", identity = "" }, "maps", "{}", "{_id:0,key:1,identity:1}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identity }).ToList();
                var desas = context.GetCollections(new { key = "", identity = "" }, "villages", $"{{'project.key':'{keyp}'}}", "{_id:0,key:'$village.key',identity:'$village.identity'}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identity }).ToList();
                var process = Enum.GetValues(typeof(JenisProses)).Cast<JenisProses>()
                                            .Select(v => new cmnItem { key = $"{(int)v}", name = v.ToString("g") }).ToList();
                var jenis = Enum.GetValues(typeof(JenisAlasHak)).Cast<JenisAlasHak>()
                                            .Select(v => new cmnItem { key = $"{(int)v}", name = v.ToString("g") }).ToList();
                var data = new UpdateDataSet
                {
                    key = key,
                    keyProject = keyp,
                    keyDesa = persil.basic.current?.keyDesa,
                    en_jenis = (int)persil.basic.current?.en_jenis,
                    en_proses = (int)persil.basic.current?.en_proses,
                    Alashak = persil.basic.current?.surat?.nomor,
                    Luas = persil.basic.current?.luasSurat,
                    NoPeta = persil.basic.current?.noPeta,
                    Pemilik = persil.basic.current?.pemilik,
                    Group = persil.basic.current?.group,
                    Alias = persil.basic.current?.alias,
                    Projects = projects,
                    Desas = desas,
                    Processes = process,
                    Types = jenis
                };
                return Ok(data);
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        [EnableCors(nameof(landrope))]
        [HttpPost("/api/map/remove")]
        [NeedToken("MAP_FULL", true)]
        public IActionResult Remove([FromQuery] string token, [FromQuery] string key)
        {
            var ses = context.db.Client.StartSession();
            ses.StartTransaction();
            try
            {
                var docs = context.db.GetCollection<BsonDocument>("persils_v2").Find($"{{key:'{key}'}}").ToList();
                if (!docs.Any())
                    return new UnprocessableEntityObjectResult("Invalid persil key given");
                var user = contextex.FindUser(token);
                var remdoc = new BsonDocument(new[] {
                    new BsonElement("time",DateTime.Now),
                    new BsonElement("user",user.key),
                    new BsonElement("doc",docs[0])
                }.AsEnumerable());

                context.db.GetCollection<BsonDocument>("persils_removed").InsertOne(remdoc);
                var dr = context.db.GetCollection<BsonDocument>("persils_v2").DeleteOne($"{{key:'{key}'}}");
                var OK = dr.DeletedCount == 1;
                if (!OK)
                {
                    ses.AbortTransaction();
                    return new UnprocessableEntityObjectResult("Gagal menghapus bidang (main collection)");
                }
                dr = context.db.GetCollection<BsonDocument>("material_persil_core").DeleteOne($"{{key:'{key}'}}");
                OK = dr.DeletedCount == 1;
                if (!OK)
                {
                    ses.AbortTransaction();
                    return new UnprocessableEntityObjectResult("Gagal menghapus bidang (cache collection)");
                }
                dr = context.db.GetCollection<BsonDocument>("material_persil_map").DeleteOne($"{{key:'{key}'}}");
                OK = dr.DeletedCount == 1;
                if (!OK)
                {
                    ses.AbortTransaction();
                    return new UnprocessableEntityObjectResult("Gagal menghapus bidang (map cache collection)");
                }
                ses.CommitTransaction();
                return Ok();
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }
        }

        //[EnableCors(nameof(landrope))]
        //[HttpGet("/api/new-map")]
        //[NeedToken("MAP_VIEW")]
        //public IActionResult newmap(string token, string key)
        //{
        //	Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        //	//object[] entities;
        //	var keys = key?.Split(',', ';', '|') ?? new string[0];
        //	var lands = memcontext.Raw.Where(p => keys.Contains(p.key))
        //							.Select(p => (pkey: p.key, lands: p.villages.SelectMany(v => v.lands).ToList())).ToList();
        //	var villages = memcontext.Raw.Where(p => keys.Contains(p.key))
        //							.SelectMany(p => p.villages.Select(v => (pkey: p.key, vill: v)));
        //	var Areas = GetArea(keys, null, null);

        //	var golands = lands.SelectMany(l => l.lands.Select(ll => (l.pkey, status: ll.ls_status, land: ll))
        //					.GroupBy(x => x.pkey)
        //					.Select(g => (key: g.Key, dict: g.Select(x => (x.status, x.land))
        //								.GroupBy(x => x.status).Select(gg => (status: gg.Key, lands: gg.Select(xx => xx.land.ToGmap()).ToArray()))
        //										.ToDictionary(x => (int)x.status, x => x.lands)))).ToList();
        //	var Areas2 = Areas.GroupBy(a => a.key).Select(g => (key: g.Key, dict: g.ToDictionary(x => (int)x.status, x => x.Area)));

        //	var govillages = villages.GroupBy(v => v.pkey)
        //					.Select(g => (key: g.Key, SKs: g.ToDictionary(x => x.vill.key, x => x.vill.ToGmap()))).ToList();

        //	var combo = golands.Join(govillages, l => l.key, v => v.key, (l, v) => (l.key, l.dict, v.SKs))
        //		.Join(Areas2, x => x.key, a => a.key, (x, a) => (x.key, lands: x.dict, x.SKs, Areas: a.dict))
        //		.Select(x => new MapDataValue { key = x.key, Areas = x.Areas, Lands = x.lands, SKs = x.SKs }).ToArray();
        //	var str = JsonConvert.SerializeObject(combo);
        //	return new ContentResult { Content = str, ContentType = "Application/json", StatusCode = 200 };
        //	//return Ok(combo);
        //}
    }

    internal class RegArea
    {
        public string key { get; set; }
        public LandStatus status { get; set; }
        public double Area { get; set; }
    }

    public class MapDataValue
    {
        public string key { get; set; }
        public Dictionary<int, double> Areas { get; set; }
        public Dictionary<string, gmapObject> SKs { get; set; }
        public Dictionary<int, gmapObject[]> Lands { get; set; }
    }

    //public class MapDataValueConverter : JsonConverter<MapDataValue>
    //{
    //	public override MapDataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //	{
    //		throw new NotImplementedException();
    //	}

    //	public override void Write(Utf8JsonWriter writer, MapDataValue value, JsonSerializerOptions options)
    //	{
    //		throw new NotImplementedException();
    //	}
    //}

}

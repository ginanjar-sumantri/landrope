using APIGrid;
using landrope.common;
using landrope.mod2;
using landrope.mod3;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;
using mongospace;
using auth.mod;

namespace landrope.api3.Controllers
{
    [Route("api/overlap")]
    [ApiController]
    public class OverlapController : ControllerBase
    {
        ExtLandropeContext contextex = Contextual.GetContextExt();

        //[HttpGet("list")]
        //public IActionResult GetList([FromQuery] string token, JenisProses pro, string find, [FromQuery] AgGridSettings gs)
        //{
        //    try
        //    {
        //        var user = contextex.FindUser(token);

        //        var qry = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().AsParallel();

        //        var all = new List<PersilOvp>().AsParallel();

        //        if (pro == JenisProses.hibah)
        //        {

        //            var persilV2 = contextex.GetDocuments(new PersilOvp(), "persils_v2",
        //                $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
        //                "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

        //            var keys = string.Join(',', persilV2.Select(k => $"'{k.key}'"));

        //            var persilV2Hibah = contextex.GetDocuments(new PersilOvp(), "persils_v2_hibah",
        //               $"<$match:<key:<$nin:[{keys}]>,invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
        //               "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

        //            all = persilV2.Union(persilV2Hibah);

        //        }
        //        else
        //        {
        //            all = contextex.GetDocuments(new PersilOvp(), "persils_v2",
        //                $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
        //                "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();
        //        }

        //        var locations = contextex.GetVillages().AsParallel();

        //        var persils = all.Join(qry, p => p.IdBidang, h => h.IdBidang, (p, h) => p.toView(h.overlap)).ToArray().AsParallel();
        //        var datax = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
        //                     .ToArray().AsParallel();

        //        var xlst = ExpressionFilter.Evaluate(datax, typeof(List<PersilHeaderView>), typeof(PersilHeaderView), gs);
        //        var data = xlst.result.Cast<PersilHeaderView>().ToArray();

        //        return Ok(data.GridFeed(gs));
        //    }
        //    catch (UnauthorizedAccessException exa)
        //    {
        //        return new ContentResult { StatusCode = int.Parse(exa.Message) };
        //    }
        //    catch (Exception ex)
        //    {
        //        MyTracer.TraceError2(ex);
        //        return new UnprocessableEntityObjectResult(ex.Message);
        //    }
        //}

        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, JenisProses pro, string find, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var hibahKeys = "";
                var user = contextex.FindUser(token);
                //find = find.ToEscape();
                find = string.IsNullOrEmpty(find) ? " " : find.ToEscape();
                var findPersil = contextex.GetDocuments(new PersilOvp(), "persils_v2",
                    $@"<$match: <'invalid' : <$ne:true>,
                                'basic.current' : <$ne: null>,
                                'basic.current.en_proses' : <$in: [2,{(int)pro}]>
                                >>".MongoJs(),
                    $@"<$match:
                                <$or: [<'IdBidang' : /{find}/i>, 
                                <'basic.current.surat.nama' : /{find}/i>, 
                                <'basic.current.surat.nomor' : /{find}/i>, 
                                <'basic.current.noPeta' : /{find}/i>, 
                                <'basic.current.group' : /{find}/i>]
                                >>".MongoJs(),
                    @"{$project: {
                                         _id : 0,
                                        'key' : 1,
                                        'IdBidang' : 1,
                                        'en_state' : {'$ifNull': [ '$en_state', 0 ]},
                                        'basic' : '$basic.current'
                                     }
                         }").ToList();

                if (pro == JenisProses.hibah)
                {
                    var keys = string.Join(',', findPersil.Select(k => $"'{k.key}'"));


                    //'key' : <$nin: [{ keys}]>

                    var findPersilHibah = contextex.GetDocuments(new PersilOvp(), "persils_v2_hibah",
                                $@"<$match: <'invalid' : <$ne:true>,
                                'basic.current' : <$ne: null>,
                                'basic.current.en_proses' : <$in: [2,{(int)pro}]>
                                >>".MongoJs(),
                                $@"<$match:
                                <$or: [<'IdBidang' : /{find}/i>, 
                                <'basic.current.surat.nama' : /{find}/i>, 
                                <'basic.current.surat.nomor' : /{find}/i>, 
                                <'basic.current.noPeta' : /{find}/i>, 
                                <'basic.current.group' : /{find}/i>]
                                >>".MongoJs(),
                                @"{$project: {
                                         _id : 0,
                                        'key' : 1,
                                        'IdBidang' : 1,
                                        'en_state' : {'$ifNull': [ '$en_state', 0 ]},
                                        'basic' : '$basic.current'
                                     }
                         }").ToList();

                    hibahKeys = string.Join(',', findPersilHibah.Select(k => $"'{k.key}'"));

                    findPersil = findPersil.Union(findPersilHibah).ToList();
                }

                var locations = contextex.GetVillages().AsParallel();
                var idBidangs = findPersil.Select(x => x.IdBidang).ToList();

                var bintang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().Where(x => idBidangs.Contains(x.IdBidang)).ToList();
                var bintangOverlap = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList()
                    .SelectMany(x => x.overlap, (bintang, overlap) => new { bintang = bintang, idOverlap = overlap.IdBidang })
                    .Where(x => idBidangs.Contains(x.idOverlap))
                    .Select(x => x.bintang).ToList();

                var union = bintang.Union(bintangOverlap).Select(x => new { IdBidang = x.IdBidang, totalOverlap = x.overlap.Sum(x => x.luas) ?? 0 }).Distinct()
                    .Select(x => new PersilOverlap { IdBidang = x.IdBidang, totalOverlap = x.totalOverlap }).ToArray();

                var idBintangs = string.Join(',', union.Select(k => $"'{k.IdBidang}'"));

                var persils = contextex.GetDocuments(new PersilOvp(), "persils_v2",
                      $"<$match:<IdBidang:<$in:[{idBintangs}]>,'basic.current.en_proses':{(int)pro},invalid:<$ne:true>,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                      "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToList();

                if (pro == JenisProses.hibah)
                {
                    var persilsHibah = contextex.GetDocuments(new PersilOvp(), "persils_v2_hibah",
                    $"<$match:<IdBidang:<$in:[{idBintangs}]>,invalid:<$ne:true>,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToList();

                    persils = persils.Union(persilsHibah).ToList();
                }

                var view = new List<PersilHeaderView>();

                foreach (var item in union)
                {
                    var prsl = persils.FirstOrDefault(p => p.IdBidang == item.IdBidang);
                    if (prsl != null)
                    {
                        view.Add(item.toView(prsl));
                    }
                }

                //var view = union.Select(po => po.toView(persils.FirstOrDefault(p => p.IdBidang == po.IdBidang))).ToArray();

                var datax = view.Join(locations, d => d.keyDesa, p => p.desa?.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                             .ToArray();

                var xlst = ExpressionFilter.Evaluate(datax, typeof(List<PersilHeaderView>), typeof(PersilHeaderView), gs);
                var data = xlst.result.Cast<PersilHeaderView>().Distinct().ToArray();

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

        [HttpGet("list-dtl")]
        public IActionResult GetListDetail([FromQuery] string token, string id, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var builder = Builders<PersilOverlap>.Filter;
                var filter = builder.Eq("IdBidang", id);

                var persil = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();

                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var locations = contextex.GetVillages().AsParallel();

                var dtl = persil.overlap;

                if (dtl == null)
                    dtl = new Overlap[0];

                var persils = dtl.Select(x => x.toView(contextex)).ToArray();
                var datax = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToArray().AsParallel();

                var xlst = ExpressionFilter.Evaluate(datax, typeof(List<PersilDetailView>), typeof(PersilDetailView), gs);
                var data = xlst.result.Cast<PersilDetailView>().ToArray();

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

        [HttpGet("list-bdg")]
        public IActionResult GetListAllPersil([FromQuery] string token, JenisProses pro, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);

                var qry = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().AsParallel();
                var fil = qry.Select(x => x.IdBidang).ToArray();
                var IdBidangs = string.Join(',', fil.Select(k => $"'{k}'"));

                var all = new List<PersilOvp>().AsParallel();

                if (pro == JenisProses.hibah)
                {
                    var persilV2 = contextex.GetDocuments(new PersilOvp(), "persils_v2",
                        $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                        "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

                    var keys = string.Join(',', persilV2.Select(k => $"'{k.key}'"));

                    //var persilV2Hibah = contextex.GetDocuments(new PersilOvp(), "persils_v2_hibah",
                    //  $"<$match:<key:<$nin:[{keys}]>,invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    //  "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

                    var persilV2Hibah = contextex.GetDocuments(new PersilOvp(), "persils_v2_hibah",
                     $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                     "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

                    all = persilV2.Union(persilV2Hibah);
                }
                else
                {
                    all = contextex.GetDocuments(new PersilOvp(), "persils_v2",
                        $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':{(int)pro},'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                        "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();
                }

                all = all.Where(x => !fil.Contains(x.IdBidang));

                var locations = contextex.GetVillages();

                var persils = all.Select(x => x.toViewHeader()).ToArray();
                var datax = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                             .ToArray().AsParallel();

                var xlst = ExpressionFilter.Evaluate(datax, typeof(List<PersilHeaderView>), typeof(PersilHeaderView), gs);
                var data = xlst.result.Cast<PersilHeaderView>().ToArray();

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

        [HttpGet("list-overlap")]
        public IActionResult GetListAllOverlap([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var fil = new List<string>();

                var qry = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().AsParallel();

                var overlaps = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                    .GroupBy(x => x.id, (x, y) => new OverlapCmd { IdBidang = x, luas = y.Sum(z => z.luas) }).ToArray().AsParallel();

                var all = contextex.GetDocuments(new PersilOvp(), "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':<$in:[0,2]>,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToArray().AsParallel();

                var locations = contextex.GetVillages();

                var bidangs = all.Join(overlaps, a => a.IdBidang, b => b.IdBidang, (a, b) => new { Idbidang = a.IdBidang, sisaLuas = a.basic.luasSurat - b.luas }).ToArray(); //Bidang Overlap yang sisa luasnya habis kaarena masuk di bidang2 bintang

                //var persilOverlaps = persils.Where(x => !bidangs.Where(x => x.sisaLuas <= 0).Select(x => x.Idbidang).Contains(x.IdBidang))
                //    .Select(x => x.toViewOv(contextex, bidangs.Select(x => (x.Idbidang, x.sisaLuas)), PersilHelper.GetNomorPBT(x))).ToList();

                var persils = all.Select(x => x.toViewOverlap(bidangs.Select(x => (x.Idbidang, x.sisaLuas)))).ToArray();
                var datax = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToArray().AsParallel();

                var xlst = ExpressionFilter.Evaluate(datax, typeof(List<PersilHeaderView>), typeof(PersilHeaderView), gs);
                var data = xlst.result.Cast<PersilHeaderView>().ToArray();

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
        public IActionResult Add([FromQuery] string token, [FromBody] PersilHeaderCore core)
        {
            try
            {
                var builder = Builders<PersilOverlap>.Filter;
                var user = contextex.FindUser(token);
                var persil = GetPersil(core.key);
                Persil persilhibah = new Persil();

                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                //var bidang = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").Find(filter).FirstOrDefault();

                var bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();

                if (bidang != null)
                    return new UnprocessableEntityObjectResult("Bidang sudah ada");

                if (bidang == null)
                {
                    persilhibah = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{core.key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                    filter = builder.Eq(e => e.IdBidang, persilhibah == null ? "" : persilhibah.IdBidang);
                    bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();

                    if (bidang != null)
                        return new UnprocessableEntityObjectResult("Bidang sudah ada");
                }

                var luasHD = persil.basic.current.luasSurat == null ? 0 : persil.basic.current.luasSurat;

                var header = new PersilOverlap
                {
                    IdBidang = persilhibah == null ? persil.IdBidang : persilhibah.IdBidang,
                    kind = 0,
                    overlap = new Overlap[0]
                };

                var details = new List<Overlap>();

                if (core.overlap.Count() > 0)
                {
                    //var qry = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").Find(builder.Empty).ToList();
                    //var overlaps = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                    //    .GroupBy(x => x.id, (x, y) => new OverlapCmd { IdBidang = x, luas = y.Sum(z => z.luas) }).ToList();

                    foreach (var item in core.overlap)
                    {
                        var persildtl = GetPersil(item.key);

                        if (persildtl == null)
                            return new UnprocessableEntityObjectResult("Bidang overlap tidak ada");

                        //var overlap = overlaps.Where(x => x.IdBidang == item.IdBidang).FirstOrDefault();
                        //if (overlap != null)
                        //{
                        //    var sisa = persil.basic.current.luasSurat - (overlap.luas + item.luas);
                        //    if (sisa < 0)
                        //        return new UnprocessableEntityObjectResult($"Luas Bidang {item.IdBidang} overlap melebihi LuasSuratnya!");
                        //}

                        var ov = new Overlap
                        {
                            IdBidang = item.IdBidang,
                            luas = item.luas
                        };

                        details.Add(ov);
                    }

                    //if (luasHD < details.Select(x => x.luas).Sum())
                    //{
                    //    return new UnprocessableEntityObjectResult("Total Luas bidang overlap, tidak boleh lebih dari bidang utama");
                    //}

                    header.overlap = details.ToArray();

                }

                var collection = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas");
                collection.InsertOne(header);

                (bool ok, string message) = (true, null);

                (ok, message) = AddHistoryHeaderOvelap(user, header, "Insert");
                if (header.overlap.Count() != 0)
                {
                    for (int i = 0; i < header.overlap.Count(); i++)
                    {
                        (ok, message) = AddHistoryDetailOvelap(user, header.overlap[i], "Insert", header.IdBidang);
                    }
                }
                if (!ok)
                {
                    MyTracer.TraceError3(message);
                    return new UnprocessableEntityObjectResult(message);
                }
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

        [HttpPost("add-detail")]
        public IActionResult AddDetail([FromQuery] string token, string key, [FromBody] PersilDetailCore core)
        {
            try
            {
                var user = contextex.FindUser(token);

                var builder = Builders<PersilOverlap>.Filter;

                var persil = GetPersil(key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var luasHD = persil.basic.current.luasSurat == null ? 0 : persil.basic.current.luasSurat;

                var filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                //var bidang = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").Find(filter).FirstOrDefault();
                var bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();

                if (bidang == null)
                {
                    persil = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                    filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                    bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                }

                if (bidang == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var exists = bidang.overlap.Where(x => x.IdBidang == core.IdBidang).FirstOrDefault();
                if (exists != null)
                    return new UnprocessableEntityObjectResult("Bidang overlap sudah ada");

                //var qry = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").Find(builder.Empty).ToList();
                //var overlap = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                //    .GroupBy(x => x.id, (x, y) => new OverlapCmd { IdBidang = x, luas = y.Sum(z => z.luas) })
                //    .Where(x => x.IdBidang == core.IdBidang).FirstOrDefault();

                //if (overlap != null)
                //{
                //    var detail = GetPersil(core.key);
                //    var sisa = detail.basic.current.luasSurat - (overlap.luas + core.luas);
                //    if (sisa < 0)
                //        return new UnprocessableEntityObjectResult($"Luas Bidang {core.IdBidang} overlap melebihi LuasSuratnya!");
                //}

                //if (luasHD < (bidang.overlap.Select(x => x.luas).Sum() + core.luas))
                //    return new UnprocessableEntityObjectResult("Total Luas bidang overlap, tidak boleh lebih dari bidang utama");

                var ov = new Overlap
                {
                    IdBidang = core.IdBidang,
                    luas = core.luas
                };

                var update = Builders<PersilOverlap>.Update
                        .Push<Overlap>(e => e.overlap, ov);

                contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").FindOneAndUpdate(filter, update);
                (bool ok, string messsage) = AddHistoryDetailOvelap(user, ov, "Insert", bidang.IdBidang);
                if (!ok)
                {
                    MyTracer.TraceError3(messsage);
                    return new UnprocessableEntityObjectResult(messsage);
                }

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

        [HttpPost("update-detail")]
        public IActionResult UpdateDetail([FromQuery] string token, string key, [FromBody] PersilDetailCore core)
        {
            try
            {
                var user = contextex.FindUser(token);

                var builder = Builders<PersilOverlap>.Filter;
                var builderUpdate = Builders<PersilOverlap>.Update;

                var persil = GetPersil(key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var luasHD = persil.basic.current.luasSurat == null ? 0 : persil.basic.current.luasSurat;

                var filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                var bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                if (bidang == null)
                {
                    persil = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                    filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                    bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                }

                if (bidang == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                //var ov = bidang.overlap.Where(x => x.IdBidang == core.IdBidang).FirstOrDefault();

                //var qry = contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").Find(builder.Empty).ToList();
                //var overlap = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                //    .GroupBy(x => x.id, (x, y) => new OverlapCmd { IdBidang = x, luas = y.Sum(z => z.luas) })
                //    .Where(x => x.IdBidang == core.IdBidang).FirstOrDefault();

                //if (overlap != null)
                //{
                //    var detail = GetPersil(core.key);
                //    var sisa = detail.basic.current.luasSurat - ((overlap.luas - ov.luas) + core.luas);
                //    if (sisa < 0)
                //        return new UnprocessableEntityObjectResult($"Luas Bidang {core.IdBidang} overlap melebihi LuasSuratnya!");
                //}

                //if (luasHD < ((bidang.overlap.Select(x => x.luas).Sum() - ov.luas) + core.luas))
                //    return new UnprocessableEntityObjectResult("Total Luas bidang overlap, tidak boleh lebih dari bidang utama");

                var filter2 = Builders<PersilOverlap>.Filter.And(Builders<PersilOverlap>.Filter.Eq(x => x.IdBidang, bidang.IdBidang),
                                                                    Builders<PersilOverlap>.Filter.ElemMatch(x => x.overlap, x => x.IdBidang == core.IdBidang));

                var update = builderUpdate.Combine(builderUpdate.Set(x => x.overlap[-1].IdBidang, core.IdBidang),
                                                    builderUpdate.Set(x => x.overlap[-1].luas, core.luas));

                contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").UpdateOne(filter2, update);
                Overlap ov = new()
                {
                    IdBidang = core.IdBidang,
                    luas = core.luas
                };

                (bool ok, string message) = AddHistoryDetailOvelap(user, ov, "Update", bidang.IdBidang);
                if (!ok)
                {
                    MyTracer.TraceError3(message);
                    return new UnprocessableEntityObjectResult(message);
                }

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

        [HttpPost("delete")]
        public IActionResult DelHd([FromQuery] string token, string key)
        {
            try
            {
                var user = contextex.FindUser(token);

                var builder = Builders<PersilOverlap>.Filter;
                var builderUpdate = Builders<PersilOverlap>.Update;

                var persil = GetPersil(key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var filter = builder.Eq(e => e.IdBidang, persil.IdBidang);

                var bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                if (bidang == null)
                {
                    persil = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                    filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                    bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                }

                if (bidang == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada di data overlap");

                if (bidang.overlap.Count() > 0)
                    return new UnprocessableEntityObjectResult("Gagal menghapus, bidang overlap masih ada!");

                (bool ok, string message) = AddHistoryHeaderOvelap(user, bidang, "Delete");
                if (!ok)
                {
                    MyTracer.TraceError3(message);
                    return new UnprocessableEntityObjectResult(message);
                }

                contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").DeleteOne(filter);

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

        [HttpPost("delete-dtl")]
        public IActionResult DelDtl([FromQuery] string token, string key, string idBidang)
        {
            try
            {
                var user = contextex.FindUser(token);

                var builder = Builders<PersilOverlap>.Filter;
                var builderUpdate = Builders<PersilOverlap>.Update;

                var persil = GetPersil(key);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var luasHD = persil.basic.current.luasSurat == null ? 0 : persil.basic.current.luasSurat;

                var filter = builder.Eq(e => e.IdBidang, persil.IdBidang);

                PersilOverlap bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                if (bidang == null)
                {
                    persil = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
                    filter = builder.Eq(e => e.IdBidang, persil.IdBidang);
                    bidang = contextex.GetCollections(new PersilOverlap(), "hibahOverlapLuas", filter, "{_id:0}").FirstOrDefault();
                }

                var ov = bidang.overlap.FirstOrDefault(x => x.IdBidang == idBidang);
                var lst = bidang.overlap.ToList();
                lst.Remove(ov);

                bidang.overlap = lst.ToArray();

                var filter2 = Builders<PersilOverlap>.Filter.Eq(e => e.IdBidang, bidang.IdBidang);
                var update = Builders<PersilOverlap>.Update.Set(e => e.overlap, lst.ToArray());

                (bool ok, string message) = AddHistoryDetailOvelap(user, ov, "Delete", bidang.IdBidang);
                if (!ok)
                {
                    MyTracer.TraceError3(message);
                    return new UnprocessableEntityObjectResult(message);
                }

                contextex.db.GetCollection<PersilOverlap>("hibahOverlapLuas").UpdateOne(filter2, update);

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

        private Persil GetPersil(string key)
        {
            var persil = contextex.persils.FirstOrDefault(p => p.key == key);

            if (persil == null)
            {
                persil = contextex.GetCollections(new Persil(), "persils_v2_hibah", $"<key: '{key}'>".MongoJs(), "{_id:0}").FirstOrDefault();
            }

            return persil;
        }

        private (bool, string) AddHistoryHeaderOvelap(user user, PersilOverlap headerOverlap, string action)
        {
            (bool Ok, string message) = (true, null);
            try
            {
                OverlapHeaderHistory ovHistory = new()
                {
                    _t = "hibahOverlapLuas",
                    created = DateTime.Now,
                    creator = user.key,
                    action = action,
                    IdBidang = headerOverlap.IdBidang,
                    kind = headerOverlap.kind,
                    group = headerOverlap.group,
                    totalOverlap = headerOverlap.totalOverlap,
                    distinctKey = "header"
                };
                var collection = contextex.db.GetCollection<OverlapHeaderHistory>("logHistoryModule");
                collection.InsertOne(ovHistory);
                return (Ok, message);
            }
            catch (Exception ex)
            {
                (Ok, message) = (false, ex.Message);
                return (Ok, message);
            }
        }

        private (bool, string) AddHistoryDetailOvelap(user user, Overlap bidangOverlap, string action, string IdBidangHeader)
        {
            (bool Ok, string message) = (true, null);
            try
            {
                OverlapDetailHistory ovDetailHistory = new()
                {
                    _t = "hibahOverlapLuas",
                    created = DateTime.Now,
                    creator = user.key,
                    action = action,
                    IdBidang = IdBidangHeader,
                    IdBidangOverlap = bidangOverlap.IdBidang,
                    luasOverlap = bidangOverlap.luas,
                    distinctKey = "detail"
                };
                var collection = contextex.db.GetCollection<OverlapDetailHistory>("logHistoryModule");
                collection.InsertOne(ovDetailHistory);
                return (Ok, message);
            }
            catch (Exception ex)
            {
                (Ok, message) = (false, ex.Message);
                return (Ok, message);
            }
        }
    }
}

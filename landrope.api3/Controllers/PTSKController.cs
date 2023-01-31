using APIGrid;
using landrope.mod;
using landrope.mod2;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tracer;
using MongoDB.Driver;
using landrope.hosts;
using landrope.common;
using auth.mod;
using Microsoft.AspNetCore.Http;
using System.IO;
using CadLoader;

namespace landrope.api3.Controllers
{
    [Route("api/ptsk")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class PTSKController : Controller
    {
        IServiceProvider services;
        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextex = Contextual.GetContextExt();

        public PTSKController(IServiceProvider services)
        {
            this.services = services;
        }

        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var qry = contextex.ptsk.Query(x => x.invalid != true);
                //var qry = contextex.db.getcollection<ptsk>("masterdatas").find("{_t:'ptsk',invalid:{$ne:true}}").tolist();

                var xlst = ExpressionFilter.Evaluate(qry, typeof(List<PTSK>), typeof(PTSK), gs);
                var data = xlst.result.Cast<PTSK>().Select(a => a.toView()).ToArray();

                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpPost("add")]
        public IActionResult AddPTSK([FromQuery] string token, [FromQuery] string identifier,
                                                                            [FromQuery] string code,
                                                                            [FromQuery] DateTime terbit,
                                                                            [FromQuery] string nomor, IFormFile myFile)
        {
            try
            {
                var user = contextex.FindUser(token);
                var host = HostServicesHelper.GetPTSKHost(services);

                var ent = new PTSK
                {
                    key = entity.MakeKey,
                    identifier = identifier,
                    code = code,
                    terbit = terbit,
                    nomor = nomor
                };
                
                if (myFile == null || myFile.Length == 0)
                {  
                    host.Add(ent);
                    return Ok();
                }
                else
                {
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

                    try
                    {
                        var procs = new processor(context, contextex);
                        var res = procs.ProcessSingle(dstname, ent);
                        contextex.SaveChanges();

                        if (string.IsNullOrEmpty(res))
                            return Ok();
                        return new UnprocessableEntityObjectResult(res);
                    }
                    finally
                    {
                        System.IO.File.Delete(dstname);
                    }
                }
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpPost("edit")]
        public IActionResult EditPTSK([FromQuery] string token, [FromQuery] string key,
                                                                            [FromQuery] string identifier,
                                                                            [FromQuery] string code,
                                                                            [FromQuery] DateTime terbit,
                                                                            [FromQuery] string nomor, IFormFile myFile)
        {
            try
            {
                var user = contextex.FindUser(token);
                var host = HostServicesHelper.GetPTSKHost(services);

                var old = host.GetPTSK(key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("PTSK dimaksud tidak ditemukan");

                old.FromCore(identifier, code, terbit, nomor);

                if (myFile == null || myFile.Length == 0)
                {
                    host.Update(old);
                    return Ok();
                }
                else
                {
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

                    try
                    {
                        var procs = new processor(context, contextex);
                        var res = procs.ProcessSingle(dstname, old);
                        contextex.SaveChanges();

                        if (string.IsNullOrEmpty(res))
                            return Ok();
                        return new UnprocessableEntityObjectResult(res);
                    }
                    finally
                    {
                        System.IO.File.Delete(dstname);
                    }
                }
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [HttpPost("add/histories")]
        public IActionResult AddHistories([FromQuery] string token, string keyPersil, string keyPTSK)
        {
            try
            {
                var user = contextex.FindUser(token);
                var persil = GetPersil(keyPersil);

                var sk = new skhistory
                {
                    keyPTSK = keyPTSK,
                    tanggal = DateTime.Now,
                    keyCreator = user.key
                    
                };

                var lst = new List<skhistory>();
                if (persil.histories != null)
                    lst = persil.histories.ToList();

                lst.Add(sk);

                persil.histories = lst.ToArray();
                persil.basic.current.keyPTSK = sk.keyPTSK;

                var last = persil.basic.entries.LastOrDefault();

                var item = new PersilBasic();
                item = persil.basic.current;
                item.keyPTSK = sk.keyPTSK;

                var newEntries1 =
                    new ValidatableEntry<PersilBasic>
                    {
                        created = DateTime.Now,
                        en_kind = ChangeKind.Update,
                        keyCreator = user.key,
                        keyReviewer = user.key,
                        reviewed = DateTime.Now,
                        approved = true,
                        item = item
                    };

                persil.basic.entries.Add(newEntries1);

                persil.basic.current = item;
                contextex.persils.Update(persil);
                contextex.SaveChanges();

                if (last != null && last.reviewed == null)
                {
                    var item2 = new PersilBasic();
                    item2 = last.item;
                    item2.keyPTSK = sk.keyPTSK;

                    var newEntries2 =
                        new ValidatableEntry<PersilBasic>
                        {
                            created = last.created,
                            en_kind = last.en_kind,
                            keyCreator = last.keyCreator,
                            keyReviewer = last.keyReviewer,
                            reviewed = last.reviewed,
                            approved = last.approved,
                            item = item2
                        };

                    persil.basic.entries.Add(newEntries2);

                    contextex.persils.Update(persil);
                    contextex.SaveChanges();
                }

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

        [HttpGet("list/histories")]
        public IActionResult GetListHistories([FromQuery] string token, string keyPersil, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var tm0 = DateTime.Now;
                var qry = GetPersil(keyPersil);

                var xlst = ExpressionFilter.Evaluate(qry.histories, typeof(List<skhistory>), typeof(skhistory), gs);
                var data = xlst.result.Cast<skhistory>().Select(a => a.toView(qry, contextex)).ToArray();

                return Ok(data.GridFeed(gs));

                
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {

                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        private Persil GetPersil(string key)
        {
            return contextex.persils.FirstOrDefault(p => p.key == key);
        }

        private PTSK GetPTSK(string key)
        {
            return contextex.ptsk.FirstOrDefault(p => p.key == key);
        }

    }
}

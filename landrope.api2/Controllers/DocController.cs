using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using landrope.common;
using landrope.mod3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Core.Misc;
using landrope.mod2;
using landrope.api2.Models;
using landrope.mod;
using MongoDB.Bson;
using Microsoft.AspNetCore.Cors;
using landrope.documents;
using FileRepos;
using landrope.material;
using System.Reflection;
using Tracer;
using MongoDB.Driver;
using BundlerConsumer;
using AssignerConsumer;
using GraphConsumer;
using landrope.consumers;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;
using landrope.mod4;
using iTextSharp.text.pdf;

namespace landrope.api2.Controllers
{

    [Route("api/doc")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class DocController : ControllerBase
    {
        ExtLandropeContext contextex;
        LandropeContext context;
        LandropePlusContext contextplus;
        FileGrid fgrid;
        BundlerHostConsumer bhost;
        AssignerHostConsumer ahost;
        GraphHostConsumer ghost;
        LandropePayContext contextpay = Contextual.GetContextPay();

        public DocController(IServiceProvider services)
        {
            context = services.GetService<LandropeContext>();
            contextplus = services.GetService<LandropePlusContext>();
            contextex = services.GetService<ExtLandropeContext>();
            contextpay = services.GetService<LandropePayContext>();
            fgrid = services.GetService<FileGrid>();

            var docdb = context.db.Client.GetDatabase("landocs");
            //bucket = new GridFSBucket(docdb, new GridFSBucketOptions { BucketName = "docsrepo", ChunkSizeBytes = 524288, });
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
            ahost = services.GetService<IAssignerHostConsumer>() as AssignerHostConsumer;
            bhost = services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
        }

        [HttpPost("save")]
        [AssigmmentMaterializer(Auto = false)]
        [MainBundleMaterializer(Auto = false)]
        public IActionResult Save([FromBody] string body)
        {
            try
            {

                var obj = Secured.decrypt<DmsSave>(body);
                MethodBase.GetCurrentMethod().SetKeyValue<MainBundleMaterializerAttribute>(obj.keyBundle);
                MethodBase.GetCurrentMethod().SetKeyValue<AssignmentSuppMaterializerAttribute>(obj.keyAssign);

                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "ARCHIVE_FULL", "DATA_FULL" });
                if (!ok)
                    return result;

                string fname = "";

                if (obj.assign)
                {
                    var assgn = ahost.GetAssignment(obj.keyAssign).GetAwaiter().GetResult() as Assignment;
                    if (assgn == null)
                        return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ditemukan");
                    var adtl = assgn.details.FirstOrDefault(d => d.keyPersil == obj.keyBundle);
                    if (adtl == null)
                        return new UnprocessableEntityObjectResult("Detail Penugasan dimaksud tidak ditemukan");
                    if (adtl.result == null || adtl.result.infoes == null || !adtl.result.infoes.Any())
                        return new UnprocessableEntityObjectResult("Jenis Dokumen hasil untuk Bidang Penugasan ini belum diinput");

                    var info = adtl.result.infoes.FirstOrDefault(i => i.keyDT == obj.keyDocType);
                    if (info == null)
                        return new UnprocessableEntityObjectResult("Jenis Dokumen hasil untuk Bidang Penugasan ini belum diinput");
                    info.scanned = DateTime.Now;
                    info.scannedBy = user.key;
                    var listExs = info.exs.Where(x => x.ex != Existence.Soft_Copy).ToList() ?? new List<Existency>();

                    // create new Existency
                    Existency exist = new Existency
                    {
                        ex = Existence.Soft_Copy,
                        cnt = 1
                    };

                    listExs.Add(exist);

                    info.exs = listExs.ToArray();

                    var infoOld = adtl.result.infoes.Where(i => i.keyDT != obj.keyDocType).ToList();
                    var detailOld = assgn.details.Where(d => d.keyPersil != obj.keyBundle).ToList();

                    infoOld.Add(info);
                    adtl.result.infoes = infoOld.ToArray();
                    detailOld.Add(adtl);
                    assgn.details = detailOld.ToArray();

                    ahost.Update(assgn).Wait();

                    fname = obj.docIdAsgn;

                }
                else if (obj.praDeals)
                {
                    var preBundle = bhost.PreBundleGet(obj.keyBundle).GetAwaiter().GetResult() as PreBundle;
                    if (preBundle == null)
                        return new UnprocessableEntityObjectResult("Pre Bundle not found");
                    var bdoc = preBundle.doclist.FirstOrDefault(d => d.keyDocType == obj.keyDocType);
                    if (bdoc == null)
                        return new UnprocessableEntityObjectResult("The doc type was not valid");

                    var lastitems = bdoc.entries.LastOrDefault()?.Item;

                    var newitems = new ParticleDocChain();

                    foreach (var key in lastitems.Keys)
                    {
                        var last = lastitems[key];
                        var dup = new ParticleDoc { exists = last.exists, props = last.props, reqs = last.reqs };
                        var exlist = dup.exists.ToList();
                        var ix = exlist.FindIndex(x => x.ex == (obj.keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli));
                        if (ix != -1)
                        {
                            dup.exists[ix].cnt = 1;
                            if (obj.keyDocType == "JDOK066")
                            {
                                Dynamic dyn = new Dynamic();
                                dyn.type = Dynamic.ValueType.Date;
                                dyn.val = DateTime.UtcNow.ToString("s");
                                dup.props["Tanggal"] = dyn;
                            }
                        }
                        else
                        {
                            exlist.Add(new Existency { ex = obj.keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli, cnt = 1 });
                            dup.exists = exlist.ToArray();
                            if (obj.keyDocType == "JDOK066")
                            {
                                Dynamic dyn = new Dynamic();
                                dyn.type = Dynamic.ValueType.Date;
                                dyn.val = DateTime.UtcNow.ToString("s");
                                dup.props.Add("Tanggal", dyn);
                            }
                        }
                        newitems.Add(key, dup);
                    }
                    if (lastitems.Keys.Count() == 0 && obj.keyDocType == "JDOK066")
                    {
                        var dup = new ParticleDoc
                        {
                            exists = new Existency[0],
                            props = new ParticleDocProp().props,
                            reqs = new ParticleDoc().reqs
                        };
                        var exlist = dup.exists.ToList();
                        Dynamic dyn = new Dynamic();
                        dyn.type = Dynamic.ValueType.Date;
                        dyn.val = DateTime.UtcNow.ToString("s");

                        dup.props.Add("Tanggal", dyn);
                        exlist.Add(new Existency { ex = (obj.keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli), cnt = 1 });
                        dup.exists = exlist.ToArray();

                        newitems.Add($"Tmp_{DateTime.Now.Ticks}", dup);
                    }

                    DateTime timestamp = DateTime.Now;
                    bdoc.UpdateExistence(user.key, newitems, "Scan Document", timestamp);

                    var logDeal = contextplus.GetDocuments(new LogDeal(), "praDeals",
                                                        "{$unwind: '$details'}",
                                                        "{$match: {'details.keyPersil' : '" + obj.keyBundle + "'}}",
                                                       @"{$project: {
                                                        key:'',
                                                        identifier:'',
                                                        keyPersil: '$details.keyPersil',
                                                        tanggalKesepakatan: '$tanggalProses',
                                                        _id:0
                                                    }}").ToList().FirstOrDefault();

                    if (logDeal?.keyPersil == obj?.keyBundle)
                    {
                        var oldLogDeal = contextplus.logDeal.FirstOrDefault(x => x.keyPersil == obj.keyBundle) ?? new LogDeal();
                        var oldDoc = oldLogDeal.jenisDokumen.ToList();
                        var isExist = oldDoc.Any(x => x.Trim() == obj.keyDocType);
                        var docEmpty = oldDoc.Count() == 0 ? true : false;
                        oldDoc.Add(obj.keyDocType);
                        oldLogDeal.jenisDokumen = oldDoc.ToArray();
                        if (!isExist)
                        {
                            contextplus.logDeal.Update(oldLogDeal);
                            contextplus.SaveChanges();
                        }
                        if (docEmpty)
                        {
                            oldLogDeal.tanggalKesepakatan = logDeal.tanggalKesepakatan;
                            oldLogDeal.keyPersil = logDeal.keyPersil;
                            contextplus.logDeal.Insert(oldLogDeal);
                            contextplus.SaveChanges();
                        }
                    }
                    else
                    {
                        bhost.PreUpdate(preBundle, true).Wait();
                    }

                    var log = new LogPreBundle(user, obj.keyDocType, obj.keyBundle, timestamp, LogActivityType.Scan, LogActivityModul.Pradeals);
                    contextplus.logPreBundle.Insert(log);
                    contextplus.SaveChanges();
                    fname = obj.docIdAsgn;
                }
                else
                {
                    fname = obj.docId;

                    var listbundle = bhost.ChildrenList(obj.keyBundle).GetAwaiter().GetResult().OfType<TaskBundle>().ToList();
                    if (listbundle != null)
                    {
                        foreach (var item in listbundle)
                        {
                            var tbdoc = item.doclist.FirstOrDefault(d => d.keyDocType == obj.keyDocType);
                            if (tbdoc != null)
                            {
                                foreach (var val in tbdoc.docs.Values)
                                {
                                    val[5] = 1;
                                }
                            }
                        }
                    }

                    var bundle = bhost.MainGet(obj.keyBundle).GetAwaiter().GetResult() as MainBundle;
                    if (bundle == null)
                        return new UnprocessableEntityObjectResult("Bundle not found");
                    var bdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == obj.keyDocType);
                    if (bdoc == null)
                        return new UnprocessableEntityObjectResult("The doc type was not valid");

                    var lastitems = bdoc.entries.LastOrDefault()?.Item;//.SelectMany(e => e.Item.Select(i => (e.created, i.Key, i.Value.exists, i.Value.props))).ToArray();
                                                                       //var lastcrt = allentries.GroupBy(x => x.Key).Select(g => (g.Key, created: g.Max(d => d.created))).ToArray();
                                                                       //var entries = allentries.Join(lastcrt, e => (e.Key, e.created), l => (l.Key, l.created), (a, l) => a).ToArray();

                    var newitems = new ParticleDocChain();
                    //foreach(var entry in entries)
                    //{
                    foreach (var key in lastitems.Keys)
                    {
                        var last = lastitems[key];
                        var dup = new ParticleDoc { exists = last.exists, props = last.props, reqs = last.reqs };
                        var exlist = dup.exists.ToList();
                        var ix = exlist.FindIndex(x => x.ex == Existence.Soft_Copy);
                        if (ix != -1)
                            dup.exists[ix].cnt = 1;
                        else
                        {
                            exlist.Add(new Existency { ex = Existence.Soft_Copy, cnt = 1 });
                            dup.exists = exlist.ToArray();
                        }
                        newitems.Add(key, dup);
                    }
                    //}
                    DateTime timestamp = DateTime.Now;
                    bdoc.UpdateExistence(user.key, newitems, "Scan Document", timestamp);

                    var logDeal = contextplus.GetDocuments(new LogDeal(), "praDeals",
                                                        "{$unwind: '$details'}",
                                                        "{$match: {'details.keyPersil' : '" + obj.keyBundle + "'}}",
                                                       @"{$project: {
                                                        key:'',
                                                        identifier:'',
                                                        keyPersil: '$details.keyPersil',
                                                        tanggalKesepakatan: '$tanggalProses',
                                                        _id:0
                                                    }}").ToList().FirstOrDefault();

                    if (logDeal?.keyPersil == obj?.keyBundle)
                    {
                        var oldLogDeal = contextplus.logDeal.FirstOrDefault(x => x.keyPersil == obj.keyBundle) ?? new LogDeal();
                        var oldDoc = oldLogDeal.jenisDokumen.ToList();
                        var isExist = oldDoc.Any(x => x.Trim() == obj.keyDocType);
                        var docEmpty = oldDoc.Count() == 0 ? true : false;
                        oldDoc.Add(obj.keyDocType);
                        oldLogDeal.jenisDokumen = oldDoc.ToArray();
                        if (!isExist)
                        {
                            contextplus.logDeal.Update(oldLogDeal);
                            contextplus.SaveChanges();
                        }
                        if (docEmpty)
                        {
                            oldLogDeal.tanggalKesepakatan = logDeal.tanggalKesepakatan;
                            oldLogDeal.keyPersil = logDeal.keyPersil;
                            contextplus.logDeal.Insert(oldLogDeal);
                            contextplus.SaveChanges();
                        }
                    }

                    bhost.MainUpdate(bundle, true).Wait();
                    if (listbundle.Count > 0)
                        bhost.TaskUpdateEx(bundle.key, true).Wait();

                    var log = new LogBundle(user, obj.keyDocType, obj.keyBundle, timestamp, LogActivityType.Scan, (logDeal?.keyPersil == obj?.keyBundle) ? LogActivityModul.Pradeals : LogActivityModul.Bundle);
                    contextplus.logBundle.Insert(log);
                    contextplus.SaveChanges();
                }

                var doc = Convert.FromBase64String(obj.GetDocument64());
                fgrid.GetBucket().UploadFromBytes(fname, doc,
                                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });

                //if (!obj.assign)
                //	MethodBase.GetCurrentMethod().GetCustomAttribute<MainBundleMaterializerAttribute>()
                //		.ManualExecute(contextplus, obj.keyBundle);
                //else
                //	MethodBase.GetCurrentMethod().GetCustomAttribute<AssigmmentMaterializerAttribute>()
                //		.ManualExecute(contextplus, obj.keyAssign);
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpGet("load")]
        public IActionResult Load(string body)
        {
            var obj = Secured.decrypt<DmsLoad>(body);
            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "ARCHIVE_FULL" });
            if (!ok)
                return result;
            //var doc = Convert.FromBase64String(obj.document64);
            //var fname = obj.docId;
            try
            {
                //var file = bucket.Find($"<filename:'{obj.docId}'>".Replace("<", "{").Replace(">", "}")).Current.FirstOrDefault();
                var lastfile = fgrid.GetBucket().Find($"{{filename: '{obj.docId}'}}").ToList().LastOrDefault();
                if (lastfile == null)
                    return new UnprocessableEntityObjectResult("document not found");

                var strm = fgrid.GetBucket().OpenDownloadStream(lastfile.Id);
                //bucket.DownloadToStream(file.Id, strm, new GridFSDownloadByNameOptions { Seekable = true });
                //strm.Seek(0, System.IO.SeekOrigin.Begin);
                var metadict = strm.FileInfo.Metadata.ToDictionary();
                var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMeta>(strmeta);
                var creator = meta.keyCreator == null ? null : context.users.FirstOrDefault(u => u.key == meta.keyCreator);
                var doc = new byte[strm.Length];
                strm.Read(doc, 0, doc.Length);
                var ret = new DmsLoadResult { creator = creator?.FullName, created = meta.created };
                ret.SetDocument64(Convert.ToBase64String(doc));
                return Ok(ret.encrypt());
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpGet("downloadfile-bundle")]
        public IActionResult DownloadFileBundle(string body)
        {
            var obj = Secured.decrypt<DmsLoad>(body);
            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "ARCHIVE_FULL" });
            if (!ok)
                return result;
            //var doc = Convert.FromBase64String(obj.document64);
            //var fname = obj.docId;
            try
            {
                //var file = bucket.Find($"<filename:'{obj.docId}'>".Replace("<", "{").Replace(">", "}")).Current.FirstOrDefault();
                var lastfile = fgrid.GetBucket().Find($"{{filename: '{obj.docId}'}}").ToList().LastOrDefault();
                if (lastfile == null)
                    return new UnprocessableEntityObjectResult("document not found");

                var strm = fgrid.GetBucket().OpenDownloadStream(lastfile.Id);
                //bucket.DownloadToStream(file.Id, strm, new GridFSDownloadByNameOptions { Seekable = true });
                //strm.Seek(0, System.IO.SeekOrigin.Begin);
                var metadict = strm.FileInfo.Metadata.ToDictionary();
                var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                var doc = new byte[strm.Length];
                strm.Read(doc, 0, doc.Length);

                return new FileContentResult(doc, "application/pdf");
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("save-req")]
        public IActionResult SaveReq([FromBody] string body)
        {
            try
            {
                var obj = Secured.decrypt<DmsSaveReq>(body);
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "" });
                if (!ok)
                    return result;

                var doc = Convert.FromBase64String(obj.GetDocument64());

                fgrid.GetBucket().UploadFromBytes(obj.key, doc,
                                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpPost("save-file-req")]
        public IActionResult SaveFileReq(string token, IFormFile file, string key)
        {
            var strm = file.OpenReadStream();
            var data = new byte[strm.Length];
            strm.Read(data, 0, data.Length);
            string fname = "";

            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(token, context, new[] { "" });
            if (!ok)
                return result;

            fname = key;

            fgrid.GetBucket().UploadFromBytes(fname, data,
                            new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });

            return Ok();
        }

        [HttpGet("downloadfile-request")]
        public IActionResult DownloadFileRequest(string body)
        {
            var obj = Secured.decrypt<DmsLoadReq>(body);
            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new string[0]);
            if (!ok)
                return result;

            try
            {
                var lastfiles = fgrid.GetBucket().Find($"{{filename: '{obj.key}'}}").ToList();
                if (!lastfiles.Any())
                    return new UnprocessableEntityObjectResult("document not found");

                var streams = new List<byte[]>();
                List<DmsLoadAttPart> parts = new List<DmsLoadAttPart>();
                foreach (var lastfile in lastfiles)
                {
                    var strm = fgrid.GetBucket().OpenDownloadStream(lastfile.Id);
                    var metadict = strm.FileInfo.Metadata.ToDictionary();
                    var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                    var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMeta>(strmeta);
                    var creator = meta.keyCreator == null ? null : context.users.FirstOrDefault(u => u.key == meta.keyCreator);
                    var doc = new byte[strm.Length];
                    strm.Read(doc, 0, doc.Length);
                    streams.Add(doc);
                }

                var mergefile = mergePdfs(streams.ToArray());

                return new FileContentResult(mergefile, "application/pdf");
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        internal static byte[] mergePdfs(byte[][] pdfs)
        {
            MemoryStream outStream = new MemoryStream();
            using (iTextSharp.text.Document document = new iTextSharp.text.Document())
            using (PdfCopy copy = new PdfCopy(document, outStream))
            {
                document.Open();
                foreach (var item in pdfs)
                {
                    copy.AddDocument(new PdfReader(item));
                }
            }

            return outStream.ToArray();
        }

        [HttpPost("rename")]
        public (bool OK, string err) Rename(string keyAssign, string keyBundle, string keyDocType, string userkey)
        {
            try
            {
                var bucket = fgrid.GetBucket();
                var oldname = $"A{keyAssign}-{keyBundle}{keyDocType}";
                var newname = $"{keyBundle}{keyDocType}";
                var lastfile = bucket.Find($"{{filename: '{oldname}'}}").ToList().LastOrDefault();
                if (lastfile == null)
                    return (false, "document not found");

                bucket.Rename(lastfile.Id, newname);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        [HttpPost("rename/pradeal")]
        public (bool OK, string err) RenamePraDeal(string keyAssign, string keyBundleOld, string keyBundleNew, string keyDocType, string userkey)
        {
            try
            {
                var bucket = fgrid.GetBucket();
                var oldname = $"{keyAssign}-{keyBundleOld}{keyDocType}";
                var newname = $"{keyBundleNew}{keyDocType}";
                var lastfile = bucket.Find($"{{filename: '{oldname}'}}").ToList().LastOrDefault();
                if (lastfile == null)
                    return (false, "document not found");

                bucket.Rename(lastfile.Id, newname);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        [HttpPost("save-test")]
        public IActionResult SaveTest(IFormFile file)
        {
            var name = file.FileName;
            var strm = file.OpenReadStream();
            var data = new byte[strm.Length];
            strm.Read(data, 0, data.Length);

            fgrid.GetBucket().UploadFromBytes(name, data,
                                new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = file.ContentType }.ToBsonDocument() });
            return Ok();
        }

        [HttpPost("save-file")]
        [MainBundleMaterializer(Auto = false)]
        [AssigmmentMaterializer(Auto = false)]
        public IActionResult SaveFile([FromQuery] string token, IFormFile file, string keyAssgn, string keyPersil, string keyDocType, UploadTaskType taskType = UploadTaskType._)
        {
            var strm = file.OpenReadStream();
            var data = new byte[strm.Length];
            strm.Read(data, 0, data.Length);
            string fname = "";

            if (taskType == UploadTaskType.PraDeal)
            {
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(token, context, new[] { "MAP_FULL", "MAP_REVIEW", "PAY_PRA_TRANS", "PAY_MKT" });
                if (!ok)
                    return result;

                var keys = keyPersil.Split("|"); // {keyReq}|{keyDetails}
                string reqKey = keys[0];
                string detKey = keys[1];

                var preBundle = bhost.PreBundleGet(detKey).GetAwaiter().GetResult() as PreBundle;
                if (preBundle == null)
                    return new UnprocessableEntityObjectResult("Bundle not found");
                var bdoc = preBundle.doclist.FirstOrDefault(d => d.keyDocType == keyDocType);
                if (bdoc == null)
                    return new UnprocessableEntityObjectResult("The doc type was not valid");


                var lastitems = bdoc.entries.LastOrDefault()?.Item;
                lastitems = lastitems ?? new ParticleDocChain();
                var newitems = new ParticleDocChain();
                foreach (var key in lastitems.Keys)
                {
                    var last = lastitems[key];
                    var dup = new ParticleDoc
                    {
                        exists = last.exists,
                        props = last.props,
                        reqs = last.reqs
                    };
                    var exlist = dup.exists.ToList();
                    var ix = exlist.FindIndex(x => x.ex == (keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli));
                    if (ix != -1)
                    {
                        dup.exists[ix].cnt = 1;
                        if (keyDocType == "JDOK066")
                        {
                            Dynamic dyn = new Dynamic();
                            dyn.type = Dynamic.ValueType.Date;
                            dyn.val = DateTime.UtcNow.ToString("s");
                            dup.props["Tanggal"] = dyn;
                        }
                    }
                    else
                    {
                        exlist.Add(new Existency { ex = (keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli), cnt = 1 });
                        dup.exists = exlist.ToArray();
                        if (keyDocType == "JDOK066")
                        {
                            Dynamic dyn = new Dynamic();
                            dyn.type = Dynamic.ValueType.Date;
                            dyn.val = DateTime.UtcNow.ToString("s");
                            dup.props.Add("Tanggal", dyn);
                        }
                    }

                    newitems.Add(key, dup);
                }
                if (lastitems.Keys.Count() == 0 && keyDocType == "JDOK066")
                {
                    var dup = new ParticleDoc
                    {
                        exists = new Existency[0],
                        props = new ParticleDocProp().props,
                        reqs = new ParticleDoc().reqs
                    };
                    var exlist = dup.exists.ToList();
                    Dynamic dyn = new Dynamic();
                    dyn.type = Dynamic.ValueType.Date;
                    dyn.val = DateTime.UtcNow.ToString("s");

                    dup.props.Add("Tanggal", dyn);
                    exlist.Add(new Existency { ex = (keyDocType != "JDOK066" ? Existence.Soft_Copy : Existence.Asli), cnt = 1 });
                    dup.exists = exlist.ToArray();

                    newitems.Add($"Tmp_{DateTime.Now.Ticks}", dup);
                }
                DateTime timestamp = DateTime.Now;
                bdoc.UpdateExistence(user.key, newitems, "Upload Document", timestamp);


                var logDeal = contextplus.GetDocuments(new LogDeal(), "praDeals",
                                                    "{$unwind: '$details'}",
                                                    "{$match: {'details.keyPersil' : '" + keyPersil + "'}}",
                                                   @"{$project: {
                                                       key:'',
                                                       identifier:'',
                                                       keyPersil: '$details.keyPersil',
                                                       tanggalKesepakatan: '$tanggalProses',
                                                       _id:0
                                                    }}").ToList().FirstOrDefault();

                if (logDeal?.keyPersil == keyPersil)
                {
                    var oldLogDeal = contextplus.logDeal.FirstOrDefault(x => x.keyPersil == keyPersil) ?? new LogDeal();
                    var oldDoc = oldLogDeal.jenisDokumen.ToList();
                    var docExist = oldDoc.Any(x => x.Trim() == keyDocType);
                    var docEmpty = oldDoc.Count() == 0 ? true : false;
                    oldDoc.Add(keyDocType);
                    oldLogDeal.jenisDokumen = oldDoc.ToArray();
                    if (!docExist)
                    {
                        contextplus.logDeal.Update(oldLogDeal);
                        contextplus.SaveChanges();
                    }
                    if (docEmpty)
                    {
                        oldLogDeal.identifier = "";
                        oldLogDeal.tanggalKesepakatan = logDeal.tanggalKesepakatan;
                        oldLogDeal.keyPersil = logDeal.keyPersil;
                        contextplus.logDeal.Insert(oldLogDeal);
                        contextplus.SaveChanges();
                    }
                }

                fname = $"{reqKey}-{detKey}{keyDocType}";

                bhost.PreUpdate(preBundle, true).Wait();

                var log = new LogPreBundle(user, keyDocType, detKey, timestamp, LogActivityType.Upload, LogActivityModul.Pradeals);
                contextplus.logPreBundle.Insert(log);
                contextplus.SaveChanges();

                fgrid.GetBucket().UploadFromBytes(fname, data,
                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
            }
            else if (taskType == UploadTaskType.Assignment)
            {
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(token, context, new[] { "ARCHIVE_FULL", "DATA_FULL" });
                if (!ok)
                    return result;

                MethodBase.GetCurrentMethod().SetKeyValue<AssignmentSuppMaterializerAttribute>(keyAssgn);

                var assgn = ahost.GetAssignment(keyAssgn).GetAwaiter().GetResult() as Assignment;
                if (assgn == null)
                    return new UnprocessableEntityObjectResult("Penugasan dimaksud tidak ditemukan");
                var adtl = assgn.details.FirstOrDefault(d => d.keyPersil == keyPersil);
                if (adtl == null)
                    return new UnprocessableEntityObjectResult("Detail Penugasan dimaksud tidak ditemukan");
                if (adtl.result == null || adtl.result.infoes == null || !adtl.result.infoes.Any())
                    return new UnprocessableEntityObjectResult("Jenis Dokumen hasil untuk Bidang Penugasan ini belum diinput");

                var info = adtl.result.infoes.FirstOrDefault(i => i.keyDT == keyDocType);
                if (info == null)
                    return new UnprocessableEntityObjectResult("Jenis Dokumen hasil untuk Bidang Penugasan ini belum diinput");
                info.scanned = DateTime.Now;
                info.scannedBy = user.key;
                var listExs = info.exs.Where(x => x.ex != Existence.Soft_Copy).ToList() ?? new List<Existency>();

                // create new Existency
                Existency exist = new Existency
                {
                    ex = Existence.Soft_Copy,
                    cnt = 1
                };

                listExs.Add(exist);

                info.exs = listExs.ToArray();

                var infoOld = adtl.result.infoes.Where(i => i.keyDT != keyDocType).ToList();
                var detailOld = assgn.details.Where(d => d.keyPersil != keyPersil).ToList();

                infoOld.Add(info);
                adtl.result.infoes = infoOld.ToArray();
                detailOld.Add(adtl);
                assgn.details = detailOld.ToArray();

                ahost.Update(assgn).Wait();

                fname = $"A{keyAssgn}-{keyPersil}{keyDocType}";

                fgrid.GetBucket().UploadFromBytes(fname, data,
                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
            }
            else
            {

                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(token, context, new[] { "ARCHIVE_FULL", "DATA_FULL" });
                if (!ok)
                    return result;


                var logDeal = contextplus.GetDocuments(new LogDeal(), "praDeals",
                                                    "{$unwind: '$details'}",
                                                    "{$match: {'details.keyPersil' : '" + keyPersil + "'}}",
                                                   @"{$project: {
                                                       key:'',
                                                       identifier:'',
                                                       keyPersil: '$details.keyPersil',
                                                       tanggalKesepakatan: '$tanggalProses',
                                                       _id:0
                                                    }}").ToList().FirstOrDefault();


                fname = keyPersil + keyDocType;

                MethodBase.GetCurrentMethod().SetKeyValue<MainBundleMaterializerAttribute>(keyPersil);
                var listbundle = bhost.ChildrenList(keyPersil).GetAwaiter().GetResult().OfType<TaskBundle>().ToList();
                if (listbundle != null)
                {
                    foreach (var item in listbundle)
                    {
                        var tbdoc = item.doclist.FirstOrDefault(d => d.keyDocType == keyDocType);
                        if (tbdoc != null)
                        {
                            foreach (var val in tbdoc.docs.Values)
                            {
                                val[5] = 1;
                            }
                        }
                    }
                }

                var bundle = bhost.MainGet(keyPersil).GetAwaiter().GetResult() as MainBundle;
                if (bundle == null)
                    return new UnprocessableEntityObjectResult("Bundle not found");
                var bdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == keyDocType);
                if (bdoc == null)
                    return new UnprocessableEntityObjectResult("The doc type was not valid");

                var lastitems = bdoc.entries.LastOrDefault()?.Item;
                lastitems = lastitems ?? new ParticleDocChain();
                var newitems = new ParticleDocChain();
                foreach (var key in lastitems.Keys)
                {
                    var last = lastitems[key];
                    var dup = new ParticleDoc { exists = last.exists, props = last.props, reqs = last.reqs };
                    var exlist = dup.exists.ToList();
                    var ix = exlist.FindIndex(x => x.ex == Existence.Soft_Copy);
                    if (ix != -1)
                        dup.exists[ix].cnt = 1;
                    else
                    {
                        exlist.Add(new Existency { ex = Existence.Soft_Copy, cnt = 1 });
                        dup.exists = exlist.ToArray();
                    }
                    newitems.Add(key, dup);
                }
                DateTime timestamp = DateTime.Now;
                bdoc.UpdateExistence(user.key, newitems, "Upload Document", timestamp);

                if (logDeal?.keyPersil == keyPersil)
                {
                    var oldLogDeal = contextplus.logDeal.FirstOrDefault(x => x.keyPersil == keyPersil) ?? new LogDeal();
                    var oldDoc = oldLogDeal.jenisDokumen.ToList();
                    var docExist = oldDoc.Any(x => x.Trim() == keyDocType);
                    var docEmpty = oldDoc.Count() == 0 ? true : false;
                    oldDoc.Add(keyDocType);
                    oldLogDeal.jenisDokumen = oldDoc.ToArray();
                    if (!docExist)
                    {
                        contextplus.logDeal.Update(oldLogDeal);
                        contextplus.SaveChanges();
                    }
                    if (docEmpty)
                    {
                        oldLogDeal.identifier = "";
                        oldLogDeal.tanggalKesepakatan = logDeal.tanggalKesepakatan;
                        oldLogDeal.keyPersil = logDeal.keyPersil;
                        contextplus.logDeal.Insert(oldLogDeal);
                        contextplus.SaveChanges();
                    }
                }

                bhost.MainUpdate(bundle, true).Wait();
                if (listbundle.Count > 0)
                    bhost.TaskUpdateEx(bundle.key, true).Wait();

                var log = new LogBundle(user, keyDocType, keyPersil, timestamp, LogActivityType.Upload, (logDeal?.keyPersil == keyPersil) ? LogActivityModul.Pradeals : LogActivityModul.Bundle);
                contextplus.logBundle.Insert(log);
                contextplus.SaveChanges();


                fgrid.GetBucket().UploadFromBytes(fname, data,
                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
            }
            return Ok();
        }

        [HttpPost("save-file-proses")]
        public IActionResult SaveFileProses([FromQuery] string token, IFormFile file, string keyProses, string keyDetail, string keySubtype, string proses)
        {
            var strm = file.OpenReadStream();
            var data = new byte[strm.Length];
            strm.Read(data, 0, data.Length);
            string fname = "";

            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(token, context, new[] { "" });
            if (!ok)
                return result;

            //fname = $"{proses}//{keyProses}-{keyDetail}{keySubtype}";
            fname = !string.IsNullOrEmpty(keyDetail) && !string.IsNullOrEmpty(keySubtype) ? $"{proses}//{keyProses}-{keyDetail}{keySubtype}" : $"{proses}//{keyProses}";

            fgrid.GetBucket().UploadFromBytes(fname, data,
                            new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });

            if (!string.IsNullOrEmpty(keyDetail) && !string.IsNullOrEmpty(keySubtype))
            {
                if (proses == "sertifikasi")
                {
                    var sertifikasi = contextpay.sertifikasis.FirstOrDefault(x => x.key == keyProses);
                    var detail = sertifikasi.details.FirstOrDefault(x => x.key == keyDetail);
                    var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == keySubtype);

                    sertifikasi.isAttachment = true;
                    detail.isAttachment = true;

                    contextpay.sertifikasis.Update(sertifikasi);
                    contextpay.SaveChanges();
                }
                else
                {
                    var pajak = contextpay.pajaks.FirstOrDefault(x => x.key == keyProses);
                    var detail = pajak.details.FirstOrDefault(x => x.key == keyDetail);
                    var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == keySubtype);

                    pajak.isAttachment = true;
                    detail.isAttachment = true;

                    contextpay.pajaks.Update(pajak);
                    contextpay.SaveChanges();
                }
            }

            return Ok();
        }

        [HttpPost("del-file-proses")]
        public IActionResult DeleteFileProses([FromQuery] string keyProses, string keyDetail, string keySubtype, string proses)
        {
            var fname = $"{proses}//{keyProses}-{keyDetail}{keySubtype}";

            var lastfiles = fgrid.GetBucket().Find($"{{filename: '{fname}'}}").ToList().OrderByDescending(x => x.UploadDateTime).FirstOrDefault();

            if (lastfiles != null)
            {
                fgrid.GetBucket().Delete(lastfiles.Id);

                if (proses == "sertifikasi")
                {
                    var sertifikasi = contextpay.sertifikasis.FirstOrDefault(x => x.key == keyProses);
                    var detail = sertifikasi.details.FirstOrDefault(x => x.key == keyDetail);
                    var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == keySubtype);

                    sertifikasi.isAttachment = false;
                    detail.isAttachment = false;
                    detailSub.isAttachment = false;

                    contextpay.sertifikasis.Update(sertifikasi);
                    contextpay.SaveChanges();
                }
                else
                {
                    var pajak = contextpay.pajaks.FirstOrDefault(x => x.key == keyProses);
                    var detail = pajak.details.FirstOrDefault(x => x.key == keyDetail);
                    var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == keySubtype);

                    pajak.isAttachment = false;
                    detail.isAttachment = false;
                    detailSub.isAttachment = false;

                    contextpay.pajaks.Update(pajak);
                    contextpay.SaveChanges();
                }
            }

            return Ok();
        }

        [HttpGet("load-test")]
        public IActionResult LoadTest(string docId)
        {
            try
            {
                var strm = fgrid.GetBucket().OpenDownloadStreamByName(docId);
                /*				var metadict = strm.FileInfo.Metadata.ToDictionary();
								var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
								var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMeta>(strmeta);
								var creator = meta.keyCreator == null ? null : context.users.FirstOrDefault(u => u.key == meta.keyCreator);
				*/
                var doc = new byte[strm.Length];
                strm.Read(doc, 0, doc.Length);
                return new FileContentResult(doc, "application/pdf");
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("save-pro")]
        public IActionResult SavePro([FromBody] string body)
        {
            try
            {
                var obj = Secured.decrypt<DmsSavePro>(body);
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "" });
                if (!ok)
                    return result;

                var doc = Convert.FromBase64String(obj.GetDocument64());

                fgrid.GetBucket().UploadFromBytes(obj.docId, doc,
                                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });

                if (!string.IsNullOrEmpty(obj.keyDetail) && !string.IsNullOrEmpty(obj.keySubtype))
                {

                    if (obj.prefix == "sertifikasi")
                    {

                        var sertifikasi = contextpay.sertifikasis.FirstOrDefault(x => x.key == obj.keyProses);
                        var detail = sertifikasi.details.FirstOrDefault(x => x.key == obj.keyDetail);
                        var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == obj.keySubtype);

                        sertifikasi.isAttachment = true;
                        detail.isAttachment = true;


                        contextpay.sertifikasis.Update(sertifikasi);
                        contextpay.SaveChanges();
                    }
                    else
                    {
                        var pajak = contextpay.pajaks.FirstOrDefault(x => x.key == obj.keyProses);
                        var detail = pajak.details.FirstOrDefault(x => x.key == obj.keyDetail);
                        var detailSub = detail.subTypes.FirstOrDefault(x => x.subType == obj.keySubtype);

                        pajak.isAttachment = true;
                        detail.isAttachment = true;

                        contextpay.pajaks.Update(pajak);
                        contextpay.SaveChanges();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpGet("load-pro")]
        public IActionResult LoadPro(string body)
        {
            var obj = Secured.decrypt<DmsLoadPro>(body);
            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new string[0]);
            if (!ok)
                return result;

            try
            {
                //var lastfiles = fgrid.GetBucket().Find($"{{filename: '{obj.docId}'}}").ToList();
                var lastfiles = new List<GridFSFileInfo>();

                if (!string.IsNullOrEmpty(obj.keyDetail) && !string.IsNullOrEmpty(obj.keySubtype))
                {
                    var headname = $"{obj.prefix}//{obj.keyProses}";
                    var head = fgrid.GetBucket().Find($"{{filename: '{headname}'}}").ToList();

                    var files = fgrid.GetBucket().Find($"{{filename: '{obj.docId}'}}").ToList();

                    if (files.Any())
                        lastfiles.AddRange(head);
                }
                else
                {
                    lastfiles = fgrid.GetBucket().Find($"{{filename: /{obj.keyProses}/i}}").ToList();
                }

                if (!lastfiles.Any())
                    return new UnprocessableEntityObjectResult("document not found");

                List<DmsLoadAttPart> parts = new List<DmsLoadAttPart>();
                foreach (var lastfile in lastfiles)
                {
                    var strm = fgrid.GetBucket().OpenDownloadStream(lastfile.Id);
                    //bucket.DownloadToStream(file.Id, strm, new GridFSDownloadByNameOptions { Seekable = true });
                    //strm.Seek(0, System.IO.SeekOrigin.Begin);
                    var metadict = strm.FileInfo.Metadata.ToDictionary();
                    var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                    var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMeta>(strmeta);
                    var creator = meta.keyCreator == null ? null : context.users.FirstOrDefault(u => u.key == meta.keyCreator);
                    var doc = new byte[strm.Length];
                    strm.Read(doc, 0, doc.Length);
                    var part = new DmsLoadAttPart { creator = creator?.FullName, created = meta.created };
                    part.SetDocument64(Convert.ToBase64String(doc));
                    parts.Add(part);
                }
                var ret = new DmsLoadAttResult { docs = parts.ToArray() };
                return Ok(ret.encrypt());
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("save-att")]
        public IActionResult SaveAtt([FromBody] string body)
        {
            try
            {
                var obj = Secured.decrypt<DmsSaveAtt>(body);
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "PAY_LAND_FULL" });
                if (!ok)
                    return result;

                var doc = Convert.FromBase64String(obj.GetDocument64());

                fgrid.GetBucket().UploadFromBytes(obj.docId, doc,
                                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpGet("load-att")]
        public IActionResult LoadAtt(string body)
        {
            var obj = Secured.decrypt<DmsLoadAtt>(body);
            (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new string[0]);
            if (!ok)
                return result;

            try
            {
                var lastfiles = fgrid.GetBucket().Find($"{{filename: '{obj.docId}'}}").ToList();
                if (!lastfiles.Any())
                    return new UnprocessableEntityObjectResult("document not found");

                List<DmsLoadAttPart> parts = new List<DmsLoadAttPart>();
                foreach (var lastfile in lastfiles)
                {
                    var strm = fgrid.GetBucket().OpenDownloadStream(lastfile.Id);
                    //bucket.DownloadToStream(file.Id, strm, new GridFSDownloadByNameOptions { Seekable = true });
                    //strm.Seek(0, System.IO.SeekOrigin.Begin);
                    var metadict = strm.FileInfo.Metadata.ToDictionary();
                    var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                    var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMeta>(strmeta);
                    var creator = meta.keyCreator == null ? null : context.users.FirstOrDefault(u => u.key == meta.keyCreator);
                    var doc = new byte[strm.Length];
                    strm.Read(doc, 0, doc.Length);
                    var part = new DmsLoadAttPart { creator = creator?.FullName, created = meta.created };
                    part.SetDocument64(Convert.ToBase64String(doc));
                    parts.Add(part);
                }
                var ret = new DmsLoadAttResult { docs = parts.ToArray() };
                return Ok(ret.encrypt());
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [HttpPost("save-temp")]
        [MainBundleMaterializer(Auto = false)]
        public IActionResult SaveTemp([FromBody] string body, [FromQuery] string partNo, string totalParts)
        {
            var obj = Secured.decrypt<DmsSave>(body);
            try
            {
                (var ok, IActionResult result, auth.mod.user user) = NeedTokenAttribute.DoAuthenticate(obj.token, context, new[] { "ARCHIVE_FULL", "DATA_FULL" });
                if (!ok)
                    return result;

                var doc = Convert.FromBase64String(obj.GetDocument64());

                fgrid.GetBucketTemp().UploadFromBytes(obj.docId, doc,
                                    new GridFSUploadOptions
                                    {
                                        Metadata = new DmsMetaTemp
                                        {
                                            created = DateTime.Now,
                                            keyCreator = user.key,
                                            part = Convert.ToInt32(partNo),
                                            totalpart = Convert.ToInt32(totalParts)
                                        }.ToBsonDocument()
                                    });

                if (Convert.ToInt32(partNo) == Convert.ToInt32(totalParts))
                {
                    var files = fgrid.GetBucketTemp().Find($"{{filename: '{obj.docId}'}}").ToList();
                    if (!files.Any())
                        return new UnprocessableEntityObjectResult("document not found");

                    List<DmsLoadTemp> temps = new List<DmsLoadTemp>();
                    foreach (var file in files)
                    {

                        var strm = fgrid.GetBucketTemp().OpenDownloadStream(file.Id);
                        var metadict = strm.FileInfo.Metadata.ToDictionary();
                        var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                        var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMetaTemp>(strmeta);
                        var docs = new byte[strm.Length];
                        strm.Read(docs, 0, docs.Length);
                        var sBase64 = Convert.ToBase64String(docs);

                        var dmsLoadTemp = new DmsLoadTemp
                        {
                            partNo = meta.part,
                            totalPartNo = meta.totalpart,
                            sbase64 = sBase64
                        };

                        temps.Add(dmsLoadTemp);
                    }

                    var list = temps.OrderBy(x => x.partNo).Select(x => x.sbase64).ToArray();
                    var join = string.Join("", list);
                    var docJoin = Convert.FromBase64String(join);

                    var listbundle = bhost.ChildrenList(obj.keyBundle).GetAwaiter().GetResult().OfType<TaskBundle>().ToList();
                    if (listbundle != null)
                    {
                        foreach (var item in listbundle)
                        {
                            var tbdoc = item.doclist.FirstOrDefault(d => d.keyDocType == obj.keyDocType);
                            if (tbdoc != null)
                            {
                                foreach (var val in tbdoc.docs.Values)
                                {
                                    val[5] = 1;
                                }
                            }
                        }
                    }

                    var bundle = bhost.MainGet(obj.keyBundle).GetAwaiter().GetResult() as MainBundle;
                    MethodBase.GetCurrentMethod().SetKeyValue<MainBundleMaterializerAttribute>(obj.keyBundle);

                    if (bundle == null)
                        return new UnprocessableEntityObjectResult("Bundle not found");
                    var bdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == obj.keyDocType);
                    if (bdoc == null)
                        return new UnprocessableEntityObjectResult("The doc type was not valid");

                    var lastitems = bdoc.entries.LastOrDefault()?.Item;

                    var newitems = new ParticleDocChain();

                    foreach (var key in lastitems.Keys)
                    {
                        var last = lastitems[key];
                        var dup = new ParticleDoc { exists = last.exists, props = last.props, reqs = last.reqs };
                        var exlist = dup.exists.ToList();
                        var ix = exlist.FindIndex(x => x.ex == Existence.Soft_Copy);
                        if (ix != -1)
                            dup.exists[ix].cnt = 1;
                        else
                        {
                            exlist.Add(new Existency { ex = Existence.Soft_Copy, cnt = 1 });
                            dup.exists = exlist.ToArray();
                        }
                        newitems.Add(key, dup);
                    }
                    DateTime timestamp = DateTime.Now;
                    bdoc.UpdateExistence(user.key, newitems, "Scan Document", timestamp);
                    var log = new LogBundle(user, obj.keyDocType, obj.keyBundle, timestamp, LogActivityType.Scan, LogActivityModul.Bundle);
                    contextplus.logBundle.Insert(log);
                    contextplus.SaveChanges();

                    bhost.MainUpdate(bundle, true).Wait();


                    if (listbundle.Count > 0)
                        bhost.TaskUpdateEx(bundle.key, true).Wait();

                    fgrid.GetBucket().UploadFromBytes(obj.docId, docJoin,
                                        new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });

                    foreach (var file in files)
                    {
                        fgrid.GetBucketTemp().Delete(file.Id);
                    }


                }

                return Ok();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);

                var files = fgrid.GetBucketTemp().Find($"{{filename: '{obj.docId}'}}").ToList();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        fgrid.GetBucketTemp().Delete(file.Id);
                    }
                }

                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpPost("load-temp")]
        public IActionResult LoadTemp([FromQuery] string filename)
        {
            try
            {
                var files = fgrid.GetBucketTemp().Find($"{{filename: '{filename}'}}").ToList();
                if (!files.Any())
                    return new UnprocessableEntityObjectResult("document not found");

                List<DmsLoadTemp> temps = new List<DmsLoadTemp>();
                foreach (var file in files)
                {

                    var strm = fgrid.GetBucketTemp().OpenDownloadStream(file.Id);
                    var metadict = strm.FileInfo.Metadata.ToDictionary();
                    var strmeta = System.Text.Json.JsonSerializer.Serialize(metadict);
                    var meta = string.IsNullOrEmpty(strmeta) ? null : System.Text.Json.JsonSerializer.Deserialize<DmsMetaTemp>(strmeta);
                    var docs = new byte[strm.Length];
                    strm.Read(docs, 0, docs.Length);
                    var sBase64 = Convert.ToBase64String(docs);

                    var dmsLoadTemp = new DmsLoadTemp
                    {
                        partNo = meta.part,
                        totalPartNo = meta.totalpart,
                        sbase64 = sBase64
                    };

                    temps.Add(dmsLoadTemp);
                }

                //var arrays = temps.OrderBy(x => x.partNo).Select(x => x.streams).ToArray();
                //var document = arrays.SelectMany(x => x).ToArray();
                //byte[] bytes = new byte[arrays.Sum(a => a.Length)];
                //int offset = 0;

                //foreach (byte[] array in arrays)
                //{
                //    Buffer.BlockCopy(array, 0, bytes, offset, array.Length);
                //    offset += array.Length;
                //}

                //fgrid.GetBucket().UploadFromBytes(obj.docId, document,
                //                    new GridFSUploadOptions { Metadata = new DmsMeta { created = DateTime.Now, keyCreator = user.key }.ToBsonDocument() });
                return Ok();
            }


            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpPost("split-preview")]
        public IActionResult SplitImage([FromQuery] string token, [FromBody] string body)
        {
            try
            {
                var result = new List<string>();

                MemoryStream image = new MemoryStream(Convert.FromBase64String(body));
                Bitmap bitmapImage = new Bitmap(image);
                MemoryStream resImage = new MemoryStream();

                float width = bitmapImage.Width / bitmapImage.HorizontalResolution;
                if (width > 8.5)
                {
                    Rectangle rect = new Rectangle(0, 0, bitmapImage.Width / 2, bitmapImage.Height);
                    Bitmap firstHalf = bitmapImage.Clone(rect, bitmapImage.PixelFormat);
                    firstHalf.Save(resImage, ImageFormat.Jpeg);
                    result.Add(Convert.ToBase64String(resImage.ToArray()));

                    resImage = new MemoryStream();
                    rect = new Rectangle(bitmapImage.Width / 2, 0, bitmapImage.Width / 2, bitmapImage.Height);
                    Bitmap secondHalf = bitmapImage.Clone(rect, bitmapImage.PixelFormat);
                    secondHalf.Save(resImage, ImageFormat.Jpeg);
                    result.Add(Convert.ToBase64String(resImage.ToArray()));
                }
                else
                    result.Add(body);

                return Ok(result);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }

        [HttpPost("rasterize")]
        public IActionResult RasterizePage([FromQuery] string token, [FromBody] string body)
        {
            try
            {
                List<string> result = new List<string>();

                var settings = new MagickReadSettings();
                settings.Density = new Density(300, 300);

                using (MagickImageCollection images = new MagickImageCollection())
                {
                    images.Read(Convert.FromBase64String(body), settings);

                    foreach (var image in images)
                    {
                        MemoryStream imgTemp = new MemoryStream();
                        image.Format = MagickFormat.Png;
                        image.Write(imgTemp);
                        result.Add(Convert.ToBase64String(imgTemp.ToArray()));
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                return new UnprocessableEntityObjectResult(ex.AllMessages());
            }
        }
    }
}
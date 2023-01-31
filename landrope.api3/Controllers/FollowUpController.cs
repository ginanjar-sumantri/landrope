using APIGrid;
using auth.mod;
using GenWorkflow;
using GraphConsumer;
using landrope.common;
using landrope.consumers;
using landrope.documents;
using landrope.hosts;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.mod4.classes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Tracer;

namespace landrope.api3.Controllers
{
    [Route("api/follow-up")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class FollowUpController : Controller
    {
        private ExtLandropeContext contextex = Contextual.GetContextExt();
        private LandropePayContext contextpay = Contextual.GetContextPay();
        private LandropePlusContext contextplus = Contextual.GetContextPlus();
        private GraphHostConsumer ghost;
        IServiceProvider services;

        public FollowUpController(IServiceProvider services)
        {
            this.services = services;
            ghost = services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        }

        /// <summary>
        /// Get Bidang yang akan di follow Up
        /// </summary>
        [HttpGet("marketing/list")]
        public IActionResult GetListBidang([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                List<Worker> dataWorker = GetDataWorker();
                List<Category> dataCategory = GetDataCategory();

                var bidang = contextex.GetDocuments(new BidangFollowUp(), "bidangFollowUp", "{$match:{'invalid':{$ne:true}}}", "{$project:{_id:0}}").AsParallel();
                var bidangView = bidang.Select(x => x.ToView(dataWorker, dataCategory)).OrderBy(x => x.created);

                var xlst = ExpressionFilter.Evaluate(bidangView, typeof(List<BidangFollowUpView>), typeof(BidangFollowUpView), gs);
                var data = xlst.result.Cast<BidangFollowUpView>().ToList();

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

        /// <summary>
        /// Save Follow Up Marketing Result
        /// </summary>
        [HttpPost("marketing/save")]
        public IActionResult SaveFollowUp([FromQuery] string token, [FromQuery] string opr, [FromBody] FollowUpMarketingCore core)
        {
            var predicate = opr switch
            {
                "add" => "menambah",
                "del" => "menghapus",
                _ => "memperbarui"
            } + "Follow Up";

            try
            {
                var user = contextex.FindUser(token);
                var res = opr switch
                {
                    "add" => Add(user),
                    "del" => Delete(user),
                    _ => Edit(user)
                };
                return res;
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

            IActionResult Add(user user)
            {
                if (core.keyManager == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk memilih manager !");
                if (core.followUpDate == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk memilih Tanggal follow up !");
                if (core.result == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk mengisi hasil follow up !");
                if (core.result == FollowUpResult.JualDanBukaHarga && (core.price == null || core.price == 0))
                    return new UnprocessableEntityObjectResult("Mohon untuk mengisi harga !");

                var keyCategories = core.keyCategories.Select(x => x == "" ? null : x).ToArray();
                List<category> listCat = new List<category>();
                if (keyCategories.Distinct() != null)
                {
                    category cat = new category(user)
                    {
                        tanggal = DateTime.Now,
                        keyCategory = keyCategories,
                        keyCreator = user.key
                    };
                    listCat.Add(cat);
                }
                var bidangFU = new BidangFollowUp()
                {
                    key = MongoEntity.MakeKey,
                    nomor = GenerateFollowUpNumber(core),
                    keyManager = core.keyManager,
                    keyWorker = core.keyWorker,
                    followUpDate = core.followUpDate,
                    result = core.result,
                    price = core.price,
                    note = core.note,
                    luas = core.luas,
                    desaTemp = core.desa,
                    created = DateTime.Now,
                    keyCreator = user.key,
                    categories = listCat.ToArray()
                };

                List<FollowUpEntries> listEntries = new List<FollowUpEntries>();
                var entries = new FollowUpEntries()
                {
                    keyCreator = user.key,
                    created = bidangFU.created,
                    nomor = GenerateFollowUpNumber(core),
                    keyManager = core.keyManager,
                    keyWorker = core.keyWorker,
                    desaTemp = core.desa,
                    followUpDate = core.followUpDate,
                    result = core.result,
                    price = core.price,
                    note = core.note,
                    luas = core.luas
                };
                listEntries.Add(entries);
                bidangFU.entries = listEntries.ToArray();

                contextpay.bidangFollowUp.Insert(bidangFU);
                contextpay.SaveChanges();

                return Ok(bidangFU.nomor);
            }

            IActionResult Edit(user user)
            {
                if (core.nomor == null)
                    return new UnprocessableEntityObjectResult("Follow Up yang anda pilih tidak tersedia !");

                var oldFollowUp = contextpay.bidangFollowUp.FirstOrDefault(x => x.nomor == core.nomor);

                if (oldFollowUp.result == FollowUpResult.JualDanBukaHarga && FollowUpResult.JualDanBukaHarga != core.result)
                    return new UnprocessableEntityObjectResult("Tidak dapat melakukan pembaruan !");

                if (!AnyChangeInEditForm(core, oldFollowUp))
                    return Ok();
                (
                    oldFollowUp.keyManager, oldFollowUp.keyWorker, oldFollowUp.luas, oldFollowUp.followUpDate,
                    oldFollowUp.price, oldFollowUp.result, oldFollowUp.note, oldFollowUp.nomor, oldFollowUp.desaTemp
                )
                    =
                (
                    core.keyManager, core.keyWorker, core.luas, core.followUpDate,
                    core.price, core.result, core.note, UpdateNomorFollowUp(core, oldFollowUp), core.desa
                );

                var listCat = oldFollowUp.categories.ToList();
                var lastCat = listCat.OrderBy(c => c.tanggal).LastOrDefault()?.keyCategory;
                string[] coreKeycat = core.keyCategories.Select(c => c == "" ? null : c).ToArray();

                bool isKeyCatSame = lastCat.SequenceEqual(coreKeycat);

                if (!isKeyCatSame)
                {
                    category cat = new category(user)
                    {
                        tanggal = DateTime.Now,
                        keyCategory = coreKeycat,
                        keyCreator = user.key
                    };
                    listCat.Add(cat);
                    oldFollowUp.categories = listCat.ToArray();

                    //ChangeCategoryPraDeals(oldFollowUp.key, cat);
                }

                List<FollowUpEntries> lastEntries = oldFollowUp.entries?.ToList() ?? new List<FollowUpEntries>();
                var newEntries = new FollowUpEntries()
                {
                    keyCreator = user.key,
                    created = oldFollowUp.created,
                    keyManager = core.keyManager,
                    keyWorker = core.keyWorker,
                    followUpDate = core.followUpDate,
                    result = core.result,
                    price = core.price,
                    note = core.note,
                    luas = core.luas,
                    desaTemp = core.desa
                };
                lastEntries.Add(newEntries);
                oldFollowUp.entries = lastEntries.ToArray();

                contextpay.bidangFollowUp.Update(oldFollowUp);
                contextpay.SaveChanges();

                return Ok(oldFollowUp.nomor);
            }

            IActionResult Delete(user user)
            {
                if (core.nomor == null)
                    return new UnprocessableEntityObjectResult("Follow Up yang anda pilih tidak tersedia !");

                var oldFollowUp = contextpay.bidangFollowUp.FirstOrDefault(x => x.nomor == core.nomor);
                string nomorFU = oldFollowUp.nomor;
                oldFollowUp.invalid = true;
                contextpay.bidangFollowUp.Update(oldFollowUp);
                contextpay.SaveChanges();

                return Ok(nomorFU);
            }
        }

        /// <summary>
        /// Get List Bidang Follow Up Dokumen
        /// </summary>
        [HttpGet("doc/persil/list")]
        public IActionResult GetFollowUpDoc([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);

                var dataView = contextex.GetDocuments(new BidangDocumentFollowUpView(), "praDeals",
                    "{$unwind: '$details'}",
                    "{$lookup: {from: 'persils_v2', localField: 'details.keyPersil', foreignField: 'key', as: 'persil'}}",
                    "{$unwind: {path: '$persil',preserveNullAndEmptyArrays: true}}",
                    "{$lookup:  {from: 'maps',let: { key: '$persil.basic.current.keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup:  {from: 'maps', localField: 'persil.basic.current.keyProject',foreignField: 'key',as:'projects'}}",

                   @"{$project: {
                        _id:0,
                        KeyManager: '$manager',
                        KeyWorker: '$sales',
                        RequestNo: '$identifier',
                        KeyPersil: {$ifNull: ['$persil.key', '$detailsk.key']},
                        IdBidang: {$switch : {
                                       branches:[
                                            {case: {$eq: ['$details.keyPersil', null]}, then: '(Belum Ada)'},
                                            {case: {$and : [{$ne: ['$persil.keyPersil', null]}, {$eq: ['$desas', []]}]},then: '(Ada di Server lain)'}
                                           ],
                                           default: '$persil.IdBidang'
                                   }},
                        Project: {$arrayElemAt:['$projects.identity', -1]},
                        Desa: {$switch : {
                                   branches:[
                                        {case: {$and : [{$ne: ['$persil.keyPersil', null]}, {$ne: ['$desas', []]}]},then: {$arrayElemAt:['$desas.identity', -1]}},
                                       ],
                                       default: '$details.desa'
                               }},
                        Group: {$ifNull: ['$persil.basic.current.group', '$details.group']},
                        Pemilik: {$ifNull: ['$persil.basic.current.pemilik', '$details.pemilik']},
                        AlasHak: {$ifNull: ['$persil.basic.current.surat.nomor', '$details.alasHak']},
                        NomorPeta: {$ifNull: ['$persil.basic.current.noPeta', '']},
                        LuasSurat: {$ifNull: ['$persil.basic.current.luasSurat', '$details.luasSurat']},
                        Satuan: {$ifNull: ['$persil.basic.current.satuan', '$details.satuan']}
                    }}").ToList();

                var workers = GetDataWorker();

                dataView.ForEach(x => x.FillWorker(
                     workers.FirstOrDefault(w => w.key == x.KeyManager)?.FullName,
                     workers.FirstOrDefault(w => w.key == x.KeyWorker)?.FullName
                    ));

                var xlst = ExpressionFilter.Evaluate(dataView, typeof(List<BidangDocumentFollowUpView>), typeof(BidangDocumentFollowUpView), gs);
                var result = xlst.result.Cast<BidangDocumentFollowUpView>().ToList();

                return Ok(result.GridFeed(gs));
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

        /// <summary>
        /// Get Docuemnt List to Follow Up
        /// </summary>
        [HttpGet("doc/list")]
        public IActionResult GetListDocumentToFollowUp([FromQuery] string token, [FromQuery] string keyDetail, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);

                var listJnsDoc = contextex.GetCollections(new DocType(), "jnsDok", "{invalid:{$ne:true}}", "{_id:0}").ToList().ToArray();
                var docSettings = contextpay.docSettings.All()
                                                        .Where(x => x.keyDetail == keyDetail)
                                                        .SelectMany(x => x.docsSetting,
                                                        (a, b) => new
                                                        {
                                                            key = a.key,
                                                            keyDetail = a.keyDetail,
                                                            keyDocType = b?.keyDocType,
                                                            mandatory = b?.UTJ,
                                                            DP = b?.DP,
                                                            Lunas = b?.Lunas,
                                                            //totalSet = b?.totalSet
                                                        });

                var dataJoin = listJnsDoc.GroupJoin(docSettings,
                                                    doc => doc.key,
                                                    fu => fu.keyDocType,
                                                    (a, b) => new FollowUpDocumentView()
                                                    {
                                                        key = a.key,
                                                        keyDetail = keyDetail,
                                                        keyDocType = a.key,
                                                        docName = a.identifier,
                                                        mandatory = b.FirstOrDefault()?.mandatory,
                                                        DP = b.FirstOrDefault()?.DP,
                                                        Lunas = b.FirstOrDefault()?.Lunas,
                                                        //jumlahSet = b.FirstOrDefault()?.totalSet ?? 0
                                                    }).ToList();

                var xlst = ExpressionFilter.Evaluate(dataJoin, typeof(List<FollowUpDocumentView>), typeof(FollowUpDocumentView), gs);
                var result = xlst.result.Cast<FollowUpDocumentView>().ToList();

                return Ok(result.GridFeed(gs));
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

        /// <summary>
        /// Save Follow Up Bidang Bebas
        /// </summary>
        [HttpPost("doc/persil/save")]
        public IActionResult SaveFollowUpBidangBebas([FromQuery] string token, [FromBody] FollowUpDocumentBidangCore core)
        {
            try
            {
                var user = contextex.FindUser(token);
                if (core.keyDetail == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk memilih bidang !");
                if (core.followUpDate == null)
                    return new UnprocessableEntityObjectResult("Mohon untuk memilih Tanggal follow up !");

                var existingFU = contextpay.fuDocs.FirstOrDefault(x => x.keyDetail == core.keyDetail);
                bool anyChangeInForm = !CheckFollowDocForm(core, existingFU);
                if (existingFU == null)
                {
                    var followUpDoc = new FollowUpDocument()
                    {
                        key = MongoEntity.MakeKey,
                        keyDetail = core.keyDetail,
                        followUpDate = core.followUpDate,
                        note = core.note,
                        created = DateTime.Now,
                        keyCreator = user.key
                    };
                    var newEntries = new FollowUpDocumentEntries()
                    {
                        keyCreator = user.key,
                        created = followUpDoc.created,
                        followUpDate = core.followUpDate,
                        note = core.note
                    };
                    var listEntries = new List<FollowUpDocumentEntries>();
                    listEntries.Add(newEntries);
                    followUpDoc.entries = listEntries.ToArray();

                    contextpay.fuDocs.Insert(followUpDoc);
                }
                else
                {
                    existingFU.followUpDate = core.followUpDate;
                    existingFU.note = core.note;
                    existingFU.created = DateTime.Now;
                    existingFU.keyCreator = user.key;

                    var newEntries = new FollowUpDocumentEntries()
                    {
                        keyCreator = user.key,
                        created = existingFU.created,
                        followUpDate = core.followUpDate,
                        note = core.note
                    };
                    var listExist = existingFU?.entries.OrderBy(x => x.created).ToList();

                    listExist.Add(newEntries);
                    existingFU.entries = listExist.ToArray();
                    contextpay.fuDocs.Update(existingFU);
                }
                if (anyChangeInForm)
                    contextpay.SaveChanges();

                return Ok();
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

        /// <summary>
        /// Save Follow Up Document by Selected Operation
        /// </summary>
        [HttpPost("doc/save")]
        public IActionResult SaveFollowUpDoc([FromQuery] string token, [FromBody] FollowUpDocumentCore core)
        {
            try
            {
                var user = contextex.FindUser(token);

                if (string.IsNullOrEmpty(core.keyDetail))
                    return new UnprocessableEntityObjectResult("Mohon untuk memilih Bidang !");

                var existingSetting = contextpay.docSettings.FirstOrDefault(x => x.keyDetail == core.keyDetail);
                var listDoc = core.DocsProps.Select(c => new DocumentsProperty()
                {
                    keyDocType = c.keyDocType,
                    UTJ = c.UTJ,
                    totalSetUTJ = c.totalSetUTJ,
                    DP = c.DP,
                    totalSetDP = c.totalSetDP,
                    Lunas = c.Lunas,
                    totalSetLunas = c.totalSetLunas
                });
                if (existingSetting == null)
                {
                    DocSettings fuDoc = new DocSettings()
                    {
                        key = MongoEntity.MakeKey,
                        keyDetail = core.keyDetail,
                        docsSetting = listDoc.ToArray(),
                        created = DateTime.Now,
                        keyCreator = user.key
                    };
                    List<DocSettingsEntries> listDocEntries = new List<DocSettingsEntries>();
                    var docEntries = new DocSettingsEntries()
                    {
                        keyCreator = fuDoc.keyCreator,
                        created = fuDoc.created,
                        docsSetting = listDoc.ToArray()
                    };
                    listDocEntries.Add(docEntries);
                    fuDoc.entries = listDocEntries.ToArray();

                    contextpay.docSettings.Insert(fuDoc);
                }
                else
                {
                    existingSetting.docsSetting = listDoc.ToArray();
                    existingSetting.created = DateTime.Now;
                    existingSetting.keyCreator = user.key;

                    var docEntry = new DocSettingsEntries()
                    {
                        keyCreator = existingSetting.keyCreator,
                        created = existingSetting.created,
                        docsSetting = listDoc.ToArray()
                    };

                    existingSetting.AddEntry(docEntry);
                    contextpay.docSettings.Update(existingSetting);
                }
                contextpay.SaveChanges();
                return Ok();
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

        private string GenerateFollowUpNumber(FollowUpMarketingCore core)
        {
            var dataWorker = GetDataWorker();
            string mngInitial = dataWorker.FirstOrDefault(dw => dw.key == core.keyManager)?.InitialName;
            string followUpNumber = $"FU/{mngInitial}/{DateTime.Now.ToString("yyyyMMdd")}/";
            var lastFollowUp = contextpay.bidangFollowUp.All().LastOrDefault(bfu => bfu.nomor.Contains(followUpNumber));
            var lastNumber = lastFollowUp?.nomor?.Split("/").Skip(3)?.FirstOrDefault();
            int iter = Convert.ToInt32(lastNumber) + 1;
            followUpNumber = $"{followUpNumber}{SetNumberDigit(iter)}";
            return followUpNumber;
        }

        private bool CheckFollowDocForm(FollowUpDocumentBidangCore core, FollowUpDocument existingFU)
        {
            if (
                core.keyDetail == existingFU?.keyDetail &&
                core.note == existingFU?.note &&
                core.followUpDate == existingFU?.followUpDate
              )
                return true;
            else
                return false;
        }

        private List<Worker> GetDataWorker()
        {
            var dataWorker = contextex.db.GetCollection<Worker>("workers").Find("{_t:'worker',invalid:{$ne:true}}").ToList();
            return dataWorker;
        }

        private string SetNumberDigit(int number)
        {
            string result = "";
            int digit = (int)Math.Floor(Math.Log10(number) + 1);
            if (digit == 3)
            {
                result = number.ToString();
            }
            else if (digit == 2)
            {
                result = $"0{number.ToString()}";
            }
            else
            {
                result = $"00{number.ToString()}";
            }
            return result;
        }

        private string UpdateNomorFollowUp(FollowUpMarketingCore core, BidangFollowUp oldFollowUp)
        {
            string nomorFollowUp = oldFollowUp.nomor;
            DateTime followUpDate = Convert.ToDateTime(oldFollowUp.followUpDate).ToLocalTime();
            if (oldFollowUp.keyManager != core.keyManager)
            {
                nomorFollowUp = GenerateFollowUpNumber(core);
            }

            return nomorFollowUp;
        }

        private PersilCategories GetPersilCategories(string keyPersilCat)
        {
            return contextpay.persilCat.FirstOrDefault(c => c.key == keyPersilCat);
        }

        private List<Category> GetDataCategory(int bebas = 0)
        {
            var categories = contextex.GetCollections(new Category(), "categories_new", $"<bebas: {bebas} >".MongoJs(), "{_id:0}").ToList();
            return categories;
        }

        private bool AnyChangeInEditForm(FollowUpMarketingCore core, BidangFollowUp followUp)
        {
            string[] coreKeyCat = followUp.categories.OrderBy(x => x.tanggal)
                                                     .LastOrDefault()?.keyCategory
                                                     .Select(c => c == "" ? null : c).ToArray();
            if (
            core.keyManager == followUp.keyManager &&
            core.keyWorker == followUp.keyWorker &&
            core.luas == followUp.luas &&
            core.note == followUp.note &&
            core.price == followUp.price &&
            core.result == followUp.result &&
            core.desa == followUp.desaTemp &&
            core.keyCategories.SequenceEqual(coreKeyCat))
            {
                return false;
            }
            else
                return true;
        }

        private List<PraPembebasan> GetListPraDealsByFollowUp(string keyFollowUp)
        {
            var keyPraBebas = contextpay.GetDocuments(new { keyPrabebas = "" }, "praDeals",
                "{$unwind: '$details'}",
                "{$match: {'details.keyFollowUp': '" + keyFollowUp + "'}}",
                "{$project: {_id:0, key:1 }}"
                ).ToList().Select(a => a.keyPrabebas).Distinct().ToList();

            var praBebas = contextplus.praDeals.All().Where(d => keyPraBebas.Contains(d.key)).ToList();

            return praBebas;
        }
    }
}
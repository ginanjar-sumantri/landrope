using APIGrid;
using auth.mod;
using landrope.api2.Models;
using landrope.common;
using landrope.material;
using landrope.mod.shared;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.mod4.classes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tracer;

namespace landrope.api2.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [EnableCors(nameof(landrope))]
    public class CategoryController : ControllerBase
    {
        ExtLandropeContext contextex = Contextual.GetContextExt();
        private LandropePayContext contextpay = Contextual.GetContextPay();
        private LandropePlusContext contextplus = Contextual.GetContextPlus();
        IServiceProvider services;

        [NeedToken("CATEGORY_HIST")]
        [HttpGet("list")]
        public IActionResult GetList([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                //var filter = Builders<Category>.Filter.Eq("bebas", cat);

                var categories = contextex.GetCollections(new Category(), "categories_new", "{}", "{_id:0}").ToList();
                var views = categories.Select(x => x.toView());


                var xlst = ExpressionFilter.Evaluate(views, typeof(List<CategoryView>), typeof(CategoryView), gs);
                var data = xlst.result.Cast<CategoryView>().ToList();

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

        [NeedToken("CATEGORY_HIST")]
        [HttpGet("ddl")]
        public IActionResult GetListDDL([FromQuery] string token, StatusCategory? catType)
        {
            try
            {
                var user = contextex.FindUser(token);
                int bebas = (int)(catType ?? StatusCategory._);
                var categories = contextex.GetCollections(new Category(), "categories_new", "{bebas: " + bebas + "}", "{_id:0}").ToList();
                
                if(bebas == 0)
                {
                    var data = categories.Where(x => x.desc != "DEAL" && x.desc != "DIKELUARKAN DARI PLANNING")
                                .Select(x => x.toView())
                                .ToArray();
                    return new JsonResult(data);
                }

                return new JsonResult(categories.Select(x => x.toView()).ToArray());
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

        [HttpGet("get")]
        public IActionResult GetCategory([FromQuery] string token, string key)
        {
            try
            {
                var user = contextex.FindUser(token);
                var category = GetCategory(key);
                if (category == null)
                    return new UnprocessableEntityObjectResult("Category tidak ada");

                return new JsonResult(category);
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

        [NeedToken("CATEGORY_HIST")]
        [HttpPost("edit")]
        public IActionResult EditCategory([FromQuery] string token, [FromBody] CategoryCore core)
        {
            try
            {
                var user = contextex.FindUser(token);
                var old = GetCategory(core.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Category tidak ada");

                old.FromCore(core);

                contextex.categorys.Update(old);
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
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [NeedToken("CATEGORY_HIST")]
        [HttpPost("delete")]
        public IActionResult DeleteCategory([FromQuery] string token, [FromBody] CategoryCore core)
        {
            try
            {
                var user = contextex.FindUser(token);
                var old = GetCategory(core.key);
                if (old == null)
                    return new UnprocessableEntityObjectResult("Category tidak ada");

                //var persils = contextex.GetCollections(new Category(), "persils_v2", "{}", "{_id:0}").ToList();

                old.FromCore(core);

                contextex.categorys.Remove(old);
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
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [NeedToken("CATEGORY_HIST")]
        [HttpPost("add")]
        public IActionResult AddCategory([FromQuery] string token, [FromBody] CategoryCore core)
        {
            try
            {
                var user = contextex.FindUser(token);

                var categories = contextex.GetCollections(new { segment = 1, shortDesc = "", bebas = 0 }, "categories_new", "{}", "{_id:0, segment:1, shortDesc:1, bebas:1}").ToList();

                var exists = categories.Where(x => x.segment == core.segment && x.shortDesc == core.shortDesc && (StatusCategory)x.bebas == core.bebas);
                if (exists.Count() > 0)
                    return new UnprocessableEntityObjectResult("Category sudah ada");

                var ent = new Category();
                ent.FromCore(core);
                ent.key = entity.MakeKey;


                contextex.categorys.Insert(ent);
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
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [NeedToken("CATEGORY_HIST")]
        [HttpPost("persil/add")]
        public IActionResult AddCategories([FromQuery] string token, string keyPersil, CategoriesCore Core)
        {
            try
            {
                var user = contextex.FindUser(token);
                var persil = GetPersil(keyPersil);
                if (persil == null)
                    return new UnprocessableEntityObjectResult("Bidang tidak ada");

                var lst1 = Core.keyCategory.ToList().Select(x => string.IsNullOrEmpty(x) ? null : x);

                var cat = new category(user)
                {
                    tanggal = DateTime.Now,
                    keyCategory = lst1.ToArray()
                };

                var lst = new List<category>();
                if (persil.categories != null)
                    lst = persil.categories.ToList();

                lst.Add(cat);

                persil.categories = lst.ToArray();
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
                return new UnprocessableEntityObjectResult(ex.Message); ;
            }
        }

        [NeedToken("CATEGORY_HIST")]
        [PersilMaterializer(Auto = false)]
        [HttpPost("persil/hist")]
        public IActionResult AddHistories([FromQuery] string token, string opr, [FromBody] CategoriesCore Core)
        {
            try
            {
                var user = contextex.FindUser(token);

                var res = opr switch
                {
                    "kategori" => Kategori(),
                    "kategoriMKT" => KategoriMKT(),
                    "masuk" => Masuk(),
                    "batal" => Batal(),
                    "lanjut" => Lanjutkan(),
                    _ => Keluar(),
                };

                IActionResult Kategori()
                {
                    List<Persil> listPersil = new List<Persil>();

                    var lst1 = Core.keyCategory.ToList().Select(x => string.IsNullOrEmpty(x) ? null : x);

                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var cat = new category(user)
                        {
                            tanggal = DateTime.Now,
                            keyCategory = lst1.ToArray()
                        };

                        var lst = new List<category>();
                        if (persil.categories != null)
                            lst = persil.categories.ToList();

                        lst.Add(cat);

                        persil.categories = lst.ToArray();

                        listPersil.Add(persil);

                    }

                    contextex.persils.Update(listPersil);
                    contextex.SaveChanges();

                    return Ok();
                }

                IActionResult Masuk()
                {
                    List<Persil> listPersil = new List<Persil>();
                    List<StateHistories> history = new List<StateHistories>();
                    var lastStateHistories = StatusBidang.belumbebas;

                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var currentState = persil.en_state ?? StatusBidang.belumbebas;

                        StatusBidang[] states = { StatusBidang.bebas, StatusBidang.belumbebas, StatusBidang.kampung };


                        if (persil.statehistories != null && persil.statehistories.Count() > 0)
                        {
                            history = persil.statehistories.Where(x => states.Contains(x.en_state)).ToList();
                            lastStateHistories = history.OrderByDescending(x => x.date).FirstOrDefault().en_state;
                        }
                        else
                        {
                            lastStateHistories = StatusBidang.belumbebas;
                        }

                        var hist = new StateHistories(user)
                        {
                            en_state = currentState,
                            date = DateTime.Now
                        };

                        var lst = new List<StateHistories>();
                        if (persil.statehistories != null)
                            lst = persil.statehistories.ToList();

                        lst.Add(hist);

                        persil.en_state = lastStateHistories;
                        persil.statehistories = lst.ToArray();

                        listPersil.Add(persil);
                    }

                    contextex.persils.Update(listPersil);
                    contextex.SaveChanges();

                    return Ok();
                }

                IActionResult Lanjutkan()
                {
                    List<Persil> listPersil = new List<Persil>();
                    List<StateHistories> history = new List<StateHistories>();
                    var lastStateHistories = StatusBidang.belumbebas;

                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var currentState = persil.en_state ?? StatusBidang.belumbebas;

                        StatusBidang[] states = { StatusBidang.bebas, StatusBidang.belumbebas, StatusBidang.kampung };


                        if (persil.statehistories != null && persil.statehistories.Count() > 0)
                        {
                            history = persil.statehistories.Where(x => states.Contains(x.en_state)).ToList();
                            lastStateHistories = history.OrderByDescending(x => x.date).FirstOrDefault().en_state;
                        }
                        else
                        {
                            lastStateHistories = StatusBidang.belumbebas;
                        }

                        var hist = new StateHistories(user)
                        {
                            en_state = currentState,
                            date = DateTime.Now
                        };

                        var lst = new List<StateHistories>();
                        if (persil.statehistories != null)
                            lst = persil.statehistories.ToList();

                        lst.Add(hist);

                        persil.en_state = lastStateHistories;
                        persil.statehistories = lst.ToArray();

                        listPersil.Add(persil);
                    }

                    contextex.persils.Update(listPersil);
                    contextex.SaveChanges();

                    return Ok();
                }

                IActionResult Batal()
                {
                    List<Persil> listPersil = new List<Persil>();

                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var currentState = persil.en_state ?? StatusBidang.bebas;

                        var newhist = new StateHistories(user)
                        {
                            en_state = currentState,
                            date = DateTime.Now
                        };

                        var lstnewhist = new List<StateHistories>();
                        if (persil.statehistories != null)
                            lstnewhist = persil.statehistories.ToList();

                        lstnewhist.Add(newhist);

                        persil.en_state = StatusBidang.batal;
                        persil.statehistories = lstnewhist.ToArray();

                        listPersil.Add(persil);
                    }

                    contextex.persils.Update(listPersil);
                    contextex.SaveChanges();

                    return Ok();
                }

                IActionResult Keluar()
                {
                    List<Persil> listPersil = new List<Persil>();

                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var currentState = persil.en_state ?? StatusBidang.bebas;

                        var newhist = new StateHistories(user)
                        {
                            en_state = currentState,
                            date = DateTime.Now
                        };

                        var lstnewhist = new List<StateHistories>();
                        if (persil.statehistories != null)
                            lstnewhist = persil.statehistories.ToList();

                        lstnewhist.Add(newhist);

                        persil.en_state = StatusBidang.keluar;
                        persil.statehistories = lstnewhist.ToArray();

                        listPersil.Add(persil);
                    }

                    contextex.persils.Update(listPersil);
                    contextex.SaveChanges();

                    return Ok();
                }

                IActionResult KategoriMKT()
                {
                    List<Persil> listPersil = new List<Persil>();
                    var lst1 = Core.keyCategory.ToList().Select(x => string.IsNullOrEmpty(x) ? null : x);
                    bool isKeyCatSame = false;
                    foreach (var keyPersil in Core.keyPersil)
                    {
                        var persil = GetPersil(keyPersil);
                        if (persil == null)
                            return new UnprocessableEntityObjectResult("Bidang tidak ada");

                        MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                        var cat = new category(user)
                        {
                            tanggal = DateTime.Now,
                            keyCategory = lst1.ToArray()
                        };

                        var lst = new List<category>();
                        if (persil.categories != null)
                            lst = persil.categories.ToList();

                        var lastCategories = persil.categories.LastOrDefault()?.keyCategory;
                        isKeyCatSame = lastCategories.SequenceEqual(Core.keyCategory);

                        lst.Add(cat);
                        persil.categories = lst.ToArray();
                        listPersil.Add(persil);
                    }

                    return Ok();
                }

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
        }

        [NeedToken("CATEGORY_HIST")]
        [HttpGet("persil/list")]
        public IActionResult GetListPersilCategories([FromQuery] string token, string keyPersil, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var tm0 = DateTime.Now;
                var qry = GetPersil(keyPersil);
                var allCat = contextex.GetCollections(new Category(), "categories_new", "{_t: 'category', bebas:0}", "{_id:0}").ToList();

                var xlst = ExpressionFilter.Evaluate(qry.categories, typeof(List<category>), typeof(category), gs);
                var data = xlst.result.Cast<category>().Select(a => a.toView(qry, contextex, allCat)).ToArray();

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

        [HttpGet("count-segment")]
        public IActionResult GetCountSegment(StatusCategory? catType)
        {
            try
            {
                int bebas = (int)(catType ?? StatusCategory._); // catType?.CategoryTypeDesc() ?? StatusCategory._.CategoryTypeDesc();
                var count = GetSegmentCount(bebas);
                if (count == 0)
                    return new UnprocessableEntityObjectResult("Segment tidak ada");

                return new JsonResult(count);
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

        [HttpGet("list-segment")]
        public IActionResult GetListCategoryBySegment(int segment, StatusCategory? catType)
        {
            try
            {
                int bebas = (int)(catType ?? StatusCategory._);
                var categories = contextex.GetCollections(new Category(), "categories_new", $"<segment:{segment}, bebas:{bebas}>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList();

                if (categories == null)
                    return new UnprocessableEntityObjectResult("Segment tidak ada");

                var data = categories.Select(x => x.toView()).ToArray();

                return new JsonResult(data);
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

        [HttpGet("persils/list")]
        public IActionResult GetListPersilForCategory([FromQuery] string token, string opr, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var tm0 = DateTime.Now;

                var keys = opr switch
                {
                    "kategori" => new int[] { 0, 1, 2, 3, 4, 5 },
                    "masuk" => new int[] { 5 },
                    "batal" => new int[] { 0, 1, 3, 4, 5 },
                    "lanjut" => new int[] { 2 },
                    _ => new int[] { 0, 1, 3, 4 }
                };

                var prokeys = string.Join(',', keys.Select(k => $"{k}"));

                var vill = contextex.GetVillages();

                var persils = contextex.GetDocuments(new PersilCore4(), "persils_v2",
                   $@"<$match:<$and: [<en_state:<$in:[{prokeys}]>>,<invalid:<$ne:true>>,<'basic.current':<$ne:null>>]>>".Replace("<", "{").Replace(">", "}"),
                   $@"<$project:<key:'$key',IdBidang: '$IdBidang',en_state: '$en_state',
                    alasHak : '$basic.current.surat.nomor',
                    noPeta : '$basic.current.noPeta',
                    states:
                        <$switch :<
                            branches:
                                [<case: <$eq:['$en_state',0]>,then: 'BEBAS'>,
                                    <case: <$eq:['$en_state',1]>,then: 'BELUM BEBAS'>,
                                    <case: <$eq:['$en_state',2]>,then: 'BATAL'>,
                                    <case: <$eq:['$en_state',3]>,then: 'KAMPUNG'>,
                                    <case: <$eq:['$en_state',4]>,then: 'OVERLAP'>,
                                    <case: <$eq:['$en_state',5]>,then: 'KELUAR'>], default: 'BEBAS'>>,
                    desa : '$basic.current.keyDesa',
                    project: '$basic.current.keyProject',
                    luasSurat : '$basic.current.luasSurat',
                    group: '$basic.current.group',
                    pemilik: '$basic.current.pemilik',
                    _id: 0>>".Replace("<", "{").Replace(">", "}")).ToList();

                var result = persils.Join(vill, p => p.desa, v => v.desa.key, (p, v) => p.setVillage(v.project.identity, v.desa.identity)).ToList();

                var xlst = ExpressionFilter.Evaluate(result, typeof(List<PersilCore4>), typeof(PersilCore4), gs);
                var data = xlst.result.Cast<PersilCore4>().ToArray();

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
        /// Get List Category By Bebas & Segment
        /// </summary>
        [HttpGet("ddl/bebas-segment")]
        public IActionResult GetCategoryByBebasAndSegment([FromQuery] string token, StatusCategory? catType, int? bebas, int? segment)
        {
            try
            {
                var user = contextex.FindUser(token);
                string _tValue = catType?.CategoryTypeDesc() ?? StatusCategory._.CategoryTypeDesc();
                var categories = contextex.GetCollections(new Category(), "categories_new", "{_t: '" + _tValue + "'}", "{_id:0}").ToList();

                if (bebas != null && segment != null)
                    categories = categories.Where(c => c.bebas == (StatusCategory)bebas && c.segment == segment).ToList();
                else if (bebas != null)
                    categories = categories.Where(c => c.bebas == (StatusCategory)bebas).ToList();
                else if (segment != null)
                    categories = categories.Where(c => c.segment == segment).ToList();

                var data = categories.Select(x => x.toView()).ToArray();

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

        // <summary>
        /// Get List History Category by bidang Follow Up
        /// </summary>
        [HttpGet("follow-up/list")]
        public IActionResult GetListCategoryFollowUpMkt([FromQuery] string token, string keyFollowUp, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var tm0 = DateTime.Now;
                var qry = GetBidangFollowUp(keyFollowUp);
                var allCat = contextex.GetCollections(new Category(), "categories_new", "{}", "{_id:0}").ToList();
                var dataWorker = GetDataWorker();

                var view =
                qry.categories.OrderBy(c => c.tanggal)
                   .Select(c => new CategoriesFollowUpView()
                   {
                       keyFollowUp = qry.key,
                       nomor = qry.nomor,
                       manager = dataWorker.FirstOrDefault(dw => dw.key == qry.keyManager)?.ShortName,
                       sales = dataWorker.FirstOrDefault(dw => dw.key == qry.keyWorker)?.ShortName,
                       luas = qry.luas,
                       keyCategories = c.keyCategory,
                       categories = string.Join("", c.keyCategory.Select(kc => allCat.FirstOrDefault(ac => ac.key == kc)?.shortDesc))
                   });

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<CategoriesFollowUpView>), typeof(CategoriesFollowUpView), gs);
                var filteredData = xlst.result.Cast<CategoriesFollowUpView>().ToList();

                return Ok(filteredData.GridFeed(gs));
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

        /// <summary>
        /// Save Kategori Bidang Follow Up
        /// </summary>
        [HttpPost("follow-up/add")]
        public IActionResult SaveCategoriesBidangFollowUp([FromQuery] string token, [FromBody] CategoriesFollowUpCore core)
        {
            try
            {
                var user = contextex.FindUser(token);

                if (string.IsNullOrEmpty(core.keyFollowUp))
                    return new UnprocessableEntityObjectResult("Bidang Follow Up tidak tersedia !");

                if (FollowUpApprovedOrMatchByAnalyst(core.keyFollowUp))
                    return new UnprocessableEntityObjectResult("Bidang Follow Up Sudah di Match / Approved " +
                        "                                       dengan Bidang Persil oleh Analyst !");

                var bidangFollowUp = contextpay.bidangFollowUp.FirstOrDefault(x => x.key == core.keyFollowUp);

                var lst1 = core.keyCategory.ToList().Select(x => string.IsNullOrEmpty(x) ? null : x).ToArray();

                var lastCategories = bidangFollowUp.categories.LastOrDefault()?.keyCategory ?? new string[0];
                bool isKeyCatSame = lastCategories.SequenceEqual(lst1);
                if (isKeyCatSame)
                    return new UnprocessableEntityObjectResult("Tidak ada perubahan kategori !");
                else
                {
                    var cat = new category(user)
                    {
                        tanggal = DateTime.Now,
                        keyCategory = lst1,
                        keyCreator = user.key
                    };

                    var lst = bidangFollowUp.categories.ToList();
                    lst.Add(cat);
                    bidangFollowUp.categories = lst.ToArray();

                    contextpay.bidangFollowUp.Update(bidangFollowUp);
                    contextpay.SaveChanges();

                    //ChangeCategoryPraDeals(core.keyFollowUp, cat);
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

        /// <summary>
        /// Get List Category History for Persil Follow Up
        /// </summary>
        [HttpGet("follow-up/doc/list")]
        public IActionResult GetFollowUpHistoryList([FromQuery] string token, string keyPersil, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextex.FindUser(token);
                var tm0 = DateTime.Now;
                var qry = GetPersilCategories(keyPersil);
                var allCat = contextex.GetCollections(new Category(), "categories_new", "{bebas:2}", "{_id:0}").ToList();
                var persil = GetPersil(keyPersil);
                var dataProject = contextex.GetVillages();

                var view = qry?.categories2.OrderBy(c => c?.tanggal)
                .Select(c => new CategoriesView()
                {
                    keyPersil = persil.key,
                    AlasHak = persil.basic.current.surat.nomor,
                    NamaPemilik = persil.basic.current.surat.nama,
                    Project = dataProject.FirstOrDefault(dp => dp.project.key == persil.basic.current.keyProject).project?.identity,
                    Desa = dataProject.FirstOrDefault(dp => dp.desa.key == persil.basic.current.keyDesa).desa?.identity,
                    keyCategories = c?.keyCategory,
                    Category = string.Join(" ", c?.keyCategory?.Select(kc => allCat.FirstOrDefault(ac => ac?.key == kc)?.desc)),
                    shortCategory = string.Join(" ", c?.keyCategory?.Select(kc => allCat.FirstOrDefault(ac => ac?.key == kc)?.shortDesc))
                });

                var xlst = ExpressionFilter.Evaluate(view, typeof(List<CategoriesView>), typeof(CategoriesView), gs);
                var filteredData = xlst.result.Cast<CategoriesView>().ToList();

                return Ok(filteredData.GridFeed(gs));
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

        /// <summary>
        /// add follow up categories
        /// </summary>
        [HttpPost("follow-up/doc/save")]
        public IActionResult SaveFollowUpHistCat([FromQuery] string token, CategoriesCore core)
        {
            try
            {
                var user = contextex.FindUser(token);
                var lst1 = core.keyCategory.ToList().Select(x => string.IsNullOrEmpty(x) ? null : x).ToArray();
                foreach (var key in core.keyPersil)
                {
                    var followUpCat = GetPersilCategories(key) ?? new PersilCategories();

                    var lastCategories = followUpCat.categories2.Any() ?
                                         followUpCat.categories2.LastOrDefault()?.keyCategory : new string[0];

                    bool isKeyCatSame = lastCategories.SequenceEqual(lst1);
                    var cat = new category(user)
                    {
                        tanggal = DateTime.Now,
                        keyCategory = lst1,
                        keyCreator = user.key
                    };

                    var lst = followUpCat.categories2.ToList();
                    lst.Add(cat);
                    followUpCat.categories2 = lst.ToArray();
                    if (followUpCat != null)
                        contextpay.persilCat.Update(followUpCat);
                    else
                        contextpay.persilCat.Insert(followUpCat);

                    if (!isKeyCatSame)
                        contextpay.SaveChanges();
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

        private Category GetCategory(string key)
        {
            return contextex.categorys.FirstOrDefault(c => c.key == key);
        }

        private int GetSegmentCount(int catType)
        {
            var category = contextex.GetDocuments(new Category(), "categories_new",
                "{$match: {bebas: " + catType + "}}",
                "{$project: {_id:0}}"
                ).ToList();

            return category.Count() == 0 ? 0 : category.Select(x => x.segment).Distinct().Max();
        }

        private Persil GetPersil(string key)
        {
            return contextex.persils.FirstOrDefault(p => p.key == key);
        }

        private PersilCategories GetPersilCategories(string keyPersil)
        {
            return contextpay.persilCat.FirstOrDefault(c => c.keyPersil == keyPersil);
        }

        private BidangFollowUp GetBidangFollowUp(string keyFollowUp)
        {
            return contextex.GetDocuments(new BidangFollowUp(), "bidangFollowUp", "{$match: {key: '" + keyFollowUp + "'}}", "{$project: {_id:0}}").ToList().FirstOrDefault();
        }

        private List<Worker> GetDataWorker()
        {
            return contextex.workers.All().Where(w => w.invalid != true).ToList();
        }

        private bool FollowUpApprovedOrMatchByAnalyst(string keyFollowUp)
        {
            var isFollowUpMatched = contextex.GetDocuments(new { keyFollowUp = "", keyPersil = "" }, "praDeals",
                "{$unwind: '$details'}",
                "{$match: {'details.keyFollowUp': '" + keyFollowUp + "'}}",
               @"{$project: { _id:0, keyFollowUp : '$details.keyFollowUp', keyPersil: '$details.keyPersil'}}"
                ).Any(x => !string.IsNullOrEmpty(x.keyPersil));

            return isFollowUpMatched;
        }

        private List<PraPembebasan> GetListPraDealsByFollowUp(string keyFollowUp)
        {
            var keyPraBebas = contextpay.GetDocuments(new { keyPrabebas = "" }, "praDeals",
                "{$unwind: '$details'}",
                "{$match: {'details.keyFollowUp': '" + keyFollowUp + "'}}",
                "{$project: {_id:0, keyPrabebas:'$key' }}"
                ).ToList().Select(a => a.keyPrabebas).Distinct();

            var praBebas = contextplus.praDeals.All().Where(d => keyPraBebas.Contains(d.key)).ToList();

            return praBebas;
        }
    }
}
using landrope.mod;
using landrope.common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using landrope.mod2;
using landrope.mod4;
using landrope.mod4.classes;
using APIGrid;
using landrope.material;
using System.Reflection;
using landrope.api3.Models;
using landrope.mod3;
using MongoDB.Bson;
using landrope.hosts;
using BundlerConsumer;
using System.Transactions;
using landrope.mod3.shared;
using auth.mod;

namespace landrope.api3.Controllers
{
    [Route("api/pembebasan")]
    [ApiController]
    public class PembebasanController : ControllerBase
    {
        IServiceProvider services;
        LandropePayContext paycontext;
        LandropeContext context = Contextual.GetContext();
        ExtLandropeContext contextes = Contextual.GetContextExt();
        LandropePlusContext contextplus = Contextual.GetContextPlus();

        public PembebasanController(IServiceProvider services)
        {
            this.services = services;
            context = services.GetService<LandropeContext>();
            paycontext = services.GetService<LandropePayContext>();
        }

        [HttpGet("listbidang")]
        public IActionResult GetBidangList([FromQuery] string token, string opr, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextes.FindUser(token);
                var res = opr switch
                {
                    "BelumBebasBelumDeal" => BelumBebas_BelumDeal(),
                    "BelumBebasDeal" => BelumBebas_Deal()

                };
                return res;
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
            catch (Exception ex)
            {
                return new InternalErrorResult(ex.Message);
            }

            IActionResult BelumBebas_BelumDeal()
            {
                var pcontext = Contextual.GetContextExt();
                try
                {
                    var tm0 = DateTime.Now;
                    var result = pcontext.GetDocuments(new MasterPembebasan(), "persils_v2",
                        @"{'$match':
	                        {
	                            $expr:
                                    {
	                                $and:
                                        [

                                        {$ne:['$invalid', true]},
	                                    {$eq:[{$ifNull:['$deal', null]}, null]},
	                                    {$ne:['$basic.current', null]},
	                                    {$eq:['$en_state', 1]}
	                                ]
	                            }
                              }
                           }",
                                    @"{'$project': {
		                                key : '$key',
		                                IdBidang : '$IdBidang',
		                                en_state : '$en_state',
		                                deal : '$deal',
		                                keyProject : '$basic.current.keyProject',
		                                keyDesa : '$basic.current.keyDesa',
                                        group : '$basic.current.group',
                                        Pemilik: '$basic.current.pemilik',
                                        AlasHak: '$basic.current.surat.nomor',
                                        luasDibayar : '$basic.current.luasDibayar',
                                        luasSurat : '$basic.current.luasSurat',
                                        satuan: '$basic.current.satuan',
                                        noPeta: '$basic.current.noPeta',
                                        _id : 0
		                                }
                                    }").ToList();

                    var bayar = paycontext.GetCollections(new Bayar(), "bayars", "{}").ToList()
                        .Where(x => x.details != null && x.bidangs != null)
                        .SelectMany(x => x.bidangs, (y, z) => new { noTahap = y.nomorTahap, keyPersil = z.keyPersil }).ToList();

                    var village = pcontext.GetVillages();
                    var villages = village.Where(x => x.desa != null).ToList();
                    var result2 = result.Join(villages, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToList();

                    var qry = (from c in result2
                               join p in bayar on c.key equals p.keyPersil into ps
                               from p in ps.DefaultIfEmpty()
                               select new MasterPembebasan()
                               {
                                   key = c.key,
                                   IdBidang = c.IdBidang,
                                   en_state = c.en_state,
                                   deal = c.deal,
                                   Desa = c.Desa,
                                   Project = c.Project,
                                   AlasHak = c.AlasHak,
                                   luasSurat = c.luasSurat,
                                   luasDibayar = c.luasDibayar,
                                   noPeta = c.noPeta,
                                   Pemilik = c.Pemilik,
                                   satuan = c.satuan,
                                   total = c.total,
                                   keyDesa = c.keyDesa,
                                   keyProject = c.keyProject,
                                   noTahap = p == null ? "" : p.noTahap.ToString(),
                                   @group = c.@group
                               }).ToList();

                    var xlst = ExpressionFilter.Evaluate(qry, typeof(List<MasterPembebasan>), typeof(MasterPembebasan), gs);
                    var data = xlst.result.Cast<MasterPembebasan>().Select(a => a.toViewPB(pcontext)).ToArray();

                    return Ok(data.GridFeed(gs));
                }
                catch (Exception ex)
                {
                    return new InternalErrorResult(ex.Message);
                }

                return Ok();
            }

            IActionResult BelumBebas_Deal()
            {
                var pcontext = Contextual.GetContextExt();
                try
                {

                    var tm0 = DateTime.Now;
                    var result = pcontext.GetDocuments(new MasterPembebasan(), "persils_v2",
                        @"{'$match':
	                        {
	                            $expr:
                                    {
	                                $and:
                                        [

                                        {$ne:['$invalid', true]},
	                                    {$ne:[{$ifNull:['$deal', null]}, null]},
	                                    {$ne:['$basic.current', null]},
	                                    {$eq:['$en_state', 1]}
	                                ]
	                            }
                              }
                           }",
                                    @"{'$project': {
		                                key : '$key',
		                                IdBidang : '$IdBidang',
		                                en_state : '$en_state',
		                                deal : '$deal',
		                                keyProject : '$basic.current.keyProject',
		                                keyDesa : '$basic.current.keyDesa',
                                        group : '$basic.current.group',
                                        Pemilik: '$basic.current.pemilik',
                                        AlasHak: '$basic.current.surat.nomor',
                                        luasDibayar : '$basic.current.luasDibayar',
                                        luasSurat : '$basic.current.luasSurat',
                                        satuan: '$basic.current.satuan',
                                        noPeta: '$basic.current.noPeta',
                                        _id : 0
		                                }
                                    }").ToList().ToArray();

                    var bayar = paycontext.GetCollections(new Bayar(), "bayars", "{}").ToList()
                        .Where(x => x.details != null && x.bidangs != null)
                        .SelectMany(x => x.bidangs, (y, z) => new { noTahap = y.nomorTahap, keyPersil = z.keyPersil }).ToList();

                    var village = pcontext.GetVillages();
                    var villages = village.Where(x => x.desa != null).ToList();
                    var result2 = result.Join(villages, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                            .ToList();

                    var qry = (from c in result2
                               join p in bayar on c.key equals p.keyPersil into ps
                               from p in ps.DefaultIfEmpty()
                               select new MasterPembebasan()
                               {
                                   key = c.key,
                                   IdBidang = c.IdBidang,
                                   en_state = c.en_state,
                                   deal = c.deal,
                                   Desa = c.Desa,
                                   Project = c.Project,
                                   AlasHak = c.AlasHak,
                                   luasSurat = c.luasSurat,
                                   luasDibayar = c.luasDibayar,
                                   noPeta = c.noPeta,
                                   Pemilik = c.Pemilik,
                                   satuan = c.satuan,
                                   total = c.total,
                                   keyDesa = c.keyDesa,
                                   keyProject = c.keyProject,
                                   noTahap = p == null ? "" : p.noTahap.ToString(),
                                   @group = c.@group
                               }).ToList();

                    var xlst = ExpressionFilter.Evaluate(qry, typeof(List<MasterPembebasan>), typeof(MasterPembebasan), gs);
                    var data = xlst.result.Cast<MasterPembebasan>().Select(a => a.toViewPB(pcontext)).ToArray();

                    return Ok(data.GridFeed(gs));
                }
                catch (Exception ex)
                {
                    return new InternalErrorResult(ex.Message);
                }

                return Ok();
            }
        }

        [HttpPost("update")]
        [Consumes("application/json")]
        [PersilMaterializerAttribute(Auto = false)]
        public IActionResult Update([FromQuery] string token, string Tkey, [FromBody] PersilBebasCore[] PersilCores)
        {
            try
            {
                foreach (var Core in PersilCores)
                {
                    var user = contextes.FindUser(token);
                    var persil = GetPersil(Core.key);
                    if (persil == null)
                        return new NotModifiedResult("Persil not found");

                    MethodBase.GetCurrentMethod().SetKeyValue<PersilMaterializerAttribute>(persil.key);

                    (double? luasDiBayar, double? luasInternal, double? luasNIBTemp, double? satuan, bool? fix, bool? pph21, bool? validasiPPH, double? validasiPPHValue,
                        bool? earlypay, double? satuanAkta, double? mandor, double? pembatalanNIB, double? balikNama, double? gantiBlanko, double? kompensasi, double? pajaklama,
                        double? pajakwaris, double? tunggakanPBB) = (Core.luasDibayar, Core.luasInternal, Core.luasNIBTemp, Core.satuan, Core.FgLuasFix, Core.pph21, Core.ValidasiPPH, Core.ValidasiPPHValue,
                        Core.earlypay, Core.satuanAkta,
                        Core.fgmandor == false ? Core.mandor * -1 : Core.mandor,
                        Core.fgpembatalanNIB == false ? Core.pembatalanNIB * -1 : Core.pembatalanNIB,
                        Core.fgbaliknama == false ? Core.baliknama * -1 : Core.baliknama,
                        Core.fggantiblanko == false ? Core.gantiblanko * -1 : Core.gantiblanko,
                        Core.fgkompensasi == false ? Core.kompensasi * -1 : Core.kompensasi,
                        Core.fgpajaklama == false ? Core.pajaklama * -1 : Core.pajaklama,
                        Core.fgpajakwaris == false ? Core.pajakwaris * -1 : Core.pajakwaris,
                        Core.fgtunggakanPBB == false ? Core.tunggakanPBB * -1 : Core.tunggakanPBB);

                    persil.FromCore(fix, pph21, validasiPPH, validasiPPHValue, earlypay, mandor, pembatalanNIB, balikNama, gantiBlanko, kompensasi, pajaklama,
                        pajakwaris, tunggakanPBB, Core.biayalainnya);

                    //For Entries
                    var last = persil.basic.entries.LastOrDefault();

                    var item = new PersilBasic();
                    item = persil.basic.current;
                    item.luasDibayar = luasDiBayar;
                    item.luasInternal = luasInternal;
                    item.luasNIBTemp = luasNIBTemp;
                    item.satuan = satuan;
                    item.satuanAkte = satuanAkta;

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
                    contextes.persils.Update(persil);
                    contextes.SaveChanges();

                    if (last != null && last.reviewed == null)
                    {
                        var item2 = new PersilBasic();
                        item2 = last.item;
                        item2.luasDibayar = luasDiBayar;
                        item2.luasInternal = luasInternal;
                        item2.luasNIBTemp = luasNIBTemp;
                        item2.satuan = satuan;
                        item2.satuanAkte = satuanAkta;

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

                        contextes.persils.Update(persil);
                        contextes.SaveChanges();
                    }

                    //MakeBundle(template, persil);
                    AddBayarSubDetail(persil.key, Tkey, user);
                }

                try
                {
                    contextes.SaveChanges();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return new InternalErrorResult(ex.Message);
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

        [HttpPost("persil")]
        [Consumes("application/json")]
        [PersilCSMaterializer(Auto = false)]
        public IActionResult Bebas([FromQuery] string token, string pkey)
        {
            try
            {
                var user = contextes.FindUser(token);
                var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();
                var persil = GetPersil(pkey);
                if (persil == null)
                    return new NotModifiedResult("Persil not found");

                MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);

                if (persil.en_state != StatusBidang.bebas)
                {
                    persil.en_state = StatusBidang.bebas;
                    contextes.persils.Update(persil);
                    contextes.SaveChanges();

                    MakeBundle(template, persil);
                }

                return Ok();


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

        [NeedToken("PAY_PRA_TRANS")]
        [HttpPost("deal")]
        [Consumes("application/json")]
        [PersilCSMaterializer(Auto = false)]
        public IActionResult Deal([FromQuery] string token, [FromBody] PersilDealCore Core)
        {
            try
            {
                var user = contextes.FindUser(token);
                var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();

                var persil = GetPersil(Core.key);
                if (persil == null)
                    return new NotModifiedResult("Persil not found");

                MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);

                var es = persil.en_state;
                var deal = persil.deal;
                var dealSystem = persil.dealSystem;
                var dealer = persil.dealer;
                var statechanger = persil.statechanger;
                var tgl = persil.statechanged;
                var notaris = persil.PraNotaris;

                (es, deal, dealSystem, dealer, statechanger, tgl, notaris) = Core.opr switch
                {
                    statusChange.Belum_Deal => (es, Core.deal, DateTime.Now, user.key, statechanger, tgl, Core.keyNotaris),
                    statusChange.Belum_Batal => (StatusBidang.batal, null, null, null, user.key, DateTime.Now, null),
                    statusChange.Deal_Batal => (StatusBidang.batal, null, null, null, user.key, DateTime.Now, null),
                    statusChange.Deal_Belum => (StatusBidang.belumbebas, null, null, null, user.key, DateTime.Now, Core.keyNotaris),
                    _ => (es, deal, dealSystem, dealer, statechanger, tgl, notaris)
                };

                persil.FromCore(es, deal, dealSystem, dealer, statechanger, tgl, notaris);

                paycontext.persils.Update(persil);

                //For Entries
                var last = persil.basic.entries.LastOrDefault();

                var item = new PersilBasic();
                item = persil.basic.current;
                item.deal = deal;
                item.dealSystem = dealSystem;
                item.dealer = dealer;

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
                contextes.persils.Update(persil);
                contextes.SaveChanges();

                if (last != null && last.reviewed == null)
                {
                    var item2 = new PersilBasic();
                    item2 = last.item;
                    item2.deal = deal;
                    item2.dealSystem = dealSystem;
                    item2.dealer = dealer;

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

                    contextes.persils.Update(persil);
                    contextes.SaveChanges();
                }

                try
                {
                    if (Core.opr == statusChange.Belum_Deal)
                        MakeBundle(template, persil);
                    else if (Core.opr == statusChange.Deal_Belum)
                        RemoveBundle(template, persil);

                    paycontext.SaveChanges();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return new InternalErrorResult(ex.Message);
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

        private void MakeBundle(MainBundle template, Persil persil)
        {
            if (template == null)
                return;

            template._id = ObjectId.Empty;
            template.key = persil.key;
            template.IdBidang = persil.IdBidang;
            contextplus.mainBundles.Remove(template);
            contextplus.SaveChanges();
            contextplus.mainBundles.Insert(template);
            contextplus.SaveChanges();

            var bhost = new BundlerHostConsumer();
            bhost.MainGet(persil.key);


            //var coll_b = contextplus.db.GetCollection<MainBundle2>("bundles");
            //template._id = ObjectId.GenerateNewId(); // using MongoDB.Bson
            //coll_b.FindOneAndDelete($"{{key:'{template.key}'}}"); // untuk mencegah duplikasi bundle 
            //coll_b.InsertOne(template);
        }

        private void RemoveBundle(MainBundle template, Persil persil)
        {
            if (template == null)
                return;

            template = contextplus.mainBundles.FirstOrDefault(x => x.key == persil.key);
            contextplus.mainBundles.Remove(template);
            contextplus.SaveChanges();

            contextplus.db.GetCollection<BsonDocument>("material_main_bundle_core").DeleteOne($"<key:'{persil.key}'>".Replace("<", "{").Replace(">", "}"));
        }

        double ProposionalLuas(double luasdiBayar, double jumlahPembayaran, double allLuas)
        {
            var result = Math.Round((jumlahPembayaran * luasdiBayar) / allLuas);

            if (double.IsNaN(Convert.ToDouble(result)))
            {
                result = 0;
            }

            return result;
        }

        double ProposionalRata(double jumlahPembayaran, double jumlahbidang)
        {
            var result = Math.Round(jumlahPembayaran / jumlahbidang);

            if (double.IsNaN(Convert.ToDouble(result)))
            {
                result = 0;
            }

            return result;
        }

        private void AddBayarSubDetail(string pkey, string Tkey, user user)
        {
            var host = HostServicesHelper.GetBayarHost(services);
            var byr = host.GetBayar(Tkey) as Bayar;
            var byrdtl = byr.details.FirstOrDefault(x => x.jenisBayar == JenisBayar.UTJ && x.invalid != true);

            if (byr != null && byrdtl != null)
            {
                var allLuas = byr.AllLuas(paycontext);
                foreach (var bidang in byr.bidangs)
                {
                    double _luasdiBayar = 0;
                    double _luas = 0;

                    var old = byrdtl.subdetails.Where(x => x.keyPersil == bidang.keyPersil).FirstOrDefault();
                    if (old != null)
                    {

                        var _persil = GetPersil(bidang.keyPersil);
                        var _luasNIB = PersilHelper.GetLuasBayar(_persil);

                        _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

                        if (_persil.luasFix != true)
                            _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                        else
                            _luasdiBayar = _luas;

                        var jml = (byrdtl.fgProposional ?? ProposionalBayar.Luas) == ProposionalBayar.Luas ? ProposionalLuas(_luasdiBayar, byrdtl.Jumlah, allLuas) : ProposionalRata(byrdtl.Jumlah, byr.bidangs.Count());

                        old.fromCore(jml);
                        host.Update(byr);
                    }
                    else
                    {
                        var byrSubDtl = new BayarSubDtl();
                        var _persil = GetPersil(bidang.keyPersil);

                        var _luasNIB = PersilHelper.GetLuasBayar(_persil);

                        _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

                        if (_persil.luasFix != true)
                            _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                        else
                            _luasdiBayar = _luas;

                        byrSubDtl.keyPersil = bidang.keyPersil;
                        byrSubDtl.Jumlah = (byrdtl.fgProposional ?? ProposionalBayar.Luas) == ProposionalBayar.Luas ? ProposionalLuas(_luasdiBayar, byrdtl.Jumlah, allLuas) : ProposionalRata(byrdtl.Jumlah, byr.bidangs.Count());
                        byrdtl.AddSubDetail(byrSubDtl);

                        host.Update(byr);
                    }
                }

                byr = host.GetBayar(Tkey) as Bayar;
                byrdtl = byr.details.FirstOrDefault(x => x.jenisBayar == JenisBayar.UTJ && x.invalid != true);

                byrdtl.SelisihPembulatan();
                host.Update(byr);
                contextes.SaveChanges();
            }
        }

        private Persil GetPersil(string key)
        {
            return contextes.persils.FirstOrDefault(p => p.key == key);
        }

        //Not Use, but sometimes
        //public IActionResult Update([FromQuery] string token, [FromBody] PersilBebasCore Core)
        //{
        //    try
        //    {
        //        var user = contextes.FindUser(token);
        //        var data = GetPersil(Core.key);
        //        if (data == null)
        //            return new NotModifiedResult("Persil not found");

        //        var es = data.en_state;
        //        var deal = data.deal;
        //        var dealer = data.dealer;
        //        var statechanger = data.statechanger;
        //        var tgl = data.statechanged;

        //        (es, deal, dealer, statechanger, tgl) = Core.opr switch
        //        {
        //            statusChange.Belum_Deal => (es, DateTime.Now, user.key, statechanger, tgl),
        //            statusChange.Belum_Batal => (StatusBidang.batal, null, null, user.key, DateTime.Now),
        //            statusChange.Belum_Bebas => (StatusBidang.bebas, null, null, user.key, DateTime.Now),
        //            statusChange.Deal_Bebas => (StatusBidang.bebas, deal, dealer, user.key, DateTime.Now),
        //            statusChange.Deal_Batal => (StatusBidang.batal, null, null, user.key, DateTime.Now),
        //            statusChange.Deal_Belum => (StatusBidang.belumbebas, deal, dealer, user.key, DateTime.Now),
        //            _ => (es, deal, dealer, statechanger, tgl)
        //        };

        //        (double? luas, double? satuan, double? total) = Core.opr switch
        //        {
        //            statusChange.Belum_Bebas or statusChange.Deal_Bebas
        //            => (Core.luasSurat, Core.satuan, (Core.luasSurat * Core.satuan)),

        //            _ => (null, null, null)
        //        };

        //        data.FromCore(es, deal, dealer, statechanger, tgl, luas, satuan, total);
        //        paycontext.persils.Update(data);
        //        try
        //        {
        //            paycontext.SaveChanges();
        //            return Ok();
        //        }
        //        catch (Exception ex)
        //        {
        //            return new InternalErrorResult(ex.Message);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        return new InternalErrorResult(ex.Message);
        //    }
        //}

        //public IActionResult Update2([FromQuery] string token, [FromBody] PersilBebasCore Core)
        //{
        //    try
        //    {
        //        var user = contextes.FindUser(token);
        //        var data = GetPersil(Core.key);
        //        if (data == null)
        //            return new NotModifiedResult("Persil not found");

        //        var es = data.en_state;
        //        var deal = data.deal;
        //        var dealer = data.dealer;
        //        var statechanger = data.statechanger;
        //        var tgl = data.statechanged;

        //        (es, deal, dealer, statechanger, tgl) = Core.opr switch
        //        {
        //            statusChange.Belum_Deal => (es, DateTime.Now, user.key, statechanger, tgl),
        //            statusChange.Belum_Batal => (StatusBidang.batal, null, null, user.key, DateTime.Now),
        //            statusChange.Belum_Bebas => (StatusBidang.bebas, null, null, user.key, DateTime.Now),
        //            statusChange.Deal_Bebas => (StatusBidang.bebas, deal, dealer, user.key, DateTime.Now),
        //            statusChange.Deal_Batal => (StatusBidang.batal, null, null, user.key, DateTime.Now),
        //            statusChange.Deal_Belum => (StatusBidang.belumbebas, deal, dealer, user.key, DateTime.Now),
        //            _ => (es, deal, dealer, statechanger, tgl)
        //        };

        //        (double? luasSurat, double? satuan, double? total, bool? fix, bool? pph21, bool? earlypay) = Core.opr switch
        //        {
        //            statusChange.Belum_Bebas or statusChange.Deal_Bebas
        //            => (Core.luasSurat, Core.satuan, (Core.luasSurat * Core.satuan), Core.FgLuasFix, Core.pph21, Core.earlypay),

        //            _ => (null, null, null, null, null, null)
        //        };

        //        data.FromCore2(es, deal, dealer, statechanger, tgl, luasSurat, satuan, fix, pph21, earlypay);
        //        paycontext.persils.Update(data);
        //        try
        //        {
        //            paycontext.SaveChanges();
        //            return Ok();
        //        }
        //        catch (Exception ex)
        //        {
        //            return new InternalErrorResult(ex.Message);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        return new InternalErrorResult(ex.Message);
        //    }
        //}

        //public IActionResult Update3([FromQuery] string token, string Tkey, [FromBody] PersilBebasCore[] PersilCores)
        //{
        //    try
        //    {
        //        foreach (var Core in PersilCores)
        //        {
        //            var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();

        //            var user = contextes.FindUser(token);
        //            var persil = GetPersil(Core.key);
        //            if (persil == null)
        //                return new NotModifiedResult("Persil not found");

        //            MethodBase.GetCurrentMethod().SetKeyValue<PersilCSMaterializerAttribute>(persil.key);

        //            var es = persil.en_state;
        //            var deal = persil.deal;
        //            var dealer = persil.dealer;
        //            var statechanger = persil.statechanger;
        //            var tgl = persil.statechanged;

        //            (es, deal, dealer, statechanger, tgl) = Core.opr switch
        //            {
        //                statusChange.Belum_Bebas => (StatusBidang.bebas, null, null, user.key, DateTime.Now),
        //                statusChange.Deal_Bebas => (StatusBidang.bebas, deal, dealer, user.key, DateTime.Now),
        //                _ => (es, deal, dealer, statechanger, tgl)
        //            };

        //            (double? luasDiBayar, double? luasInternal, double? satuan, bool? fix, bool? pph21, bool? validasiPPH, double? validasiPPHValue,
        //                bool? earlypay) = Core.opr switch
        //                {
        //                    statusChange.Belum_Bebas or statusChange.Deal_Bebas
        //                    => (Core.luasDibayar, Core.luasInternal, Core.satuan, Core.FgLuasFix, Core.pph21, Core.ValidasiPPH, Core.ValidasiPPHValue, Core.earlypay),
        //                    _ => (null, null, null, null, null, null, null, null)
        //                };

        //            persil.FromCore3(es, deal, dealer, statechanger, tgl, fix, pph21, validasiPPH, validasiPPHValue, earlypay);

        //            //For Entries
        //            var last = persil.basic.entries.LastOrDefault();

        //            var item = new PersilBasic();
        //            item = persil.basic.current;
        //            item.luasDibayar = luasDiBayar;
        //            item.luasInternal = luasInternal;
        //            item.satuan = satuan;

        //            var newEntries1 =
        //                new ValidatableEntry<PersilBasic>
        //                {
        //                    created = DateTime.Now,
        //                    en_kind = ChangeKind.Update,
        //                    keyCreator = user.key,
        //                    keyReviewer = user.key,
        //                    reviewed = DateTime.Now,
        //                    approved = true,
        //                    item = item
        //                };

        //            persil.basic.entries.Add(newEntries1);

        //            persil.basic.current = item;
        //            contextes.persils.Update(persil);
        //            contextes.SaveChanges();

        //            if (last != null && last.reviewed == null)
        //            {
        //                var item2 = new PersilBasic();
        //                item2 = last.item;
        //                item2.luasDibayar = luasDiBayar;
        //                item2.luasInternal = luasInternal;
        //                item2.satuan = satuan;

        //                var newEntries2 =
        //                    new ValidatableEntry<PersilBasic>
        //                    {
        //                        created = last.created,
        //                        en_kind = last.en_kind,
        //                        keyCreator = last.keyCreator,
        //                        keyReviewer = last.keyReviewer,
        //                        reviewed = last.reviewed,
        //                        approved = last.approved,
        //                        item = item2
        //                    };

        //                persil.basic.entries.Add(newEntries2);

        //                contextes.persils.Update(persil);
        //                contextes.SaveChanges();
        //            }

        //            MakeBundle(template, persil);
        //            AddBayarSubDetail(Core.key, Tkey);
        //        }

        //        try
        //        {
        //            contextes.SaveChanges();
        //            return Ok();
        //        }
        //        catch (Exception ex)
        //        {
        //            return new InternalErrorResult(ex.Message);
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

        //private void AddBayarSubDetail(string persilKey, string Tkey)
        //{
        //    var host = HostServicesHelper.GetBayarHost(services);
        //    var byr = host.GetBayar(Tkey) as Bayar;
        //    //var byr = ListByr.SelectMany(x => x.bidangs, (x, y) => new { bayar = x, keyPersil = y.keyPersil }).FirstOrDefault(z => z.keyPersil == persilKey).bayar;
        //    var byrdtl = byr.details.FirstOrDefault(x => x.jenisBayar == JenisBayar.UTJ);

        //    if (byr != null && byrdtl != null)
        //    {
        //        var allLuas = AllLuas(byr);
        //        foreach (var bidang in byr.bidangs)
        //        {
        //            double _luasdiBayar = 0;
        //            double _luas = 0;

        //            var old = byrdtl.subdetails.Where(x => x.keyPersil == bidang.keyPersil).FirstOrDefault();
        //            if (old != null)
        //            {

        //                var _persil = GetPersil(bidang.keyPersil);
        //                var _luasNIB = PersilHelper.GetLuasBayar(_persil);

        //                if (_persil.en_state == StatusBidang.belumbebas)
        //                {
        //                    _luasdiBayar = _persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(_persil.basic.current.luasSurat);
        //                }
        //                else
        //                {
        //                    _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

        //                    if (_persil.luasFix != true)
        //                        _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
        //                    else
        //                        _luasdiBayar = _luas;
        //                }

        //                var jml = ProposionalLuas(_luasdiBayar, byrdtl.Jumlah, allLuas);

        //                old.fromCore(jml);
        //                host.Update(byr);
        //            }
        //            else
        //            {
        //                var byrSubDtl = new BayarSubDtl();
        //                var _persil = GetPersil(bidang.keyPersil);

        //                var _luasNIB = PersilHelper.GetLuasBayar(_persil);

        //                if (_persil.en_state == StatusBidang.belumbebas)
        //                {
        //                    _luasdiBayar = _persil.basic.current.luasSurat == null ? 0 : Convert.ToDouble(_persil.basic.current.luasSurat);
        //                }
        //                else
        //                {
        //                    _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

        //                    if (_persil.luasFix != true)
        //                        _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
        //                    else
        //                        _luasdiBayar = _luas;
        //                }

        //                byrSubDtl.keyPersil = bidang.keyPersil;
        //                byrSubDtl.Jumlah = ProposionalLuas(_luasdiBayar, byrdtl.Jumlah, allLuas);
        //                byrdtl.AddSubDetail(byrSubDtl);

        //                host.Update(byr);
        //            }
        //        }

        //        contextes.SaveChanges();
        //    }
        //}
    }
}

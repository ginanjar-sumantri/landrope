#define GRPC

using APIGrid;
using auth.mod;
//using DocumentFormat.OpenXml.Wordprocessing;
using flow.common;
using GenWorkflow;
using GraphConsumer;
using GraphHost;
using landrope.api3.Models;
using landrope.api3.Services.PraPembebasan;
using landrope.common;
using landrope.consumers;
using landrope.documents;
using landrope.hosts;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.mod4.classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using mongospace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tracer;

namespace landrope.api3.Controllers
{
    [Route("api/laporan")]
    public class ReportController : ControllerBase
    {

        IServiceProvider services;
        GraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        LandropeContext context = Contextual.GetContext();
        LandropePayContext contextpay;
        ExtLandropeContext contextes = Contextual.GetContextExt();
        LandropePlusContext contextplus = Contextual.GetContextPlus();
        //GraphHostSvc graphhost;

        IPraPembebasanService praPembebasanService;

        public ReportController(IServiceProvider services)
        {
            this.services = services;
            context = services.GetService<LandropeContext>();
            contextpay = services.GetService<LandropePayContext>();
            //graphhost = HostServicesHelper.GetGraphHost(services);
            praPembebasanService = services.GetService<IPraPembebasanService>();
        }

        [HttpGet("tahap")]
        public IActionResult GetAllTahap([FromQuery] string token)
        {
            try
            {
                var data = contextpay.GetCollections(new BayarViewRpt(), "v_rpt_tahap", "{}", "{}").ToList().AsParallel()
                .Select(x => new BayarRpt
                {
                    NoTahap = x.NoTahap,
                    Project = x.Project,
                    Desa = x.Desa,
                    Group = x.Group,
                    PTSK = x.PTSK,
                    JumlahBidang = x.JumlahBidang,
                    TotalLuas = x.TotalLuas,
                    Dibuat = x.Dibuat,
                    Tanggal = x.Tanggal.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                });

                var sb = MakeCsv(data);
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpGet("pivot/project")]
        public IActionResult GetBayarPivot([FromQuery] string token)
        {
            try
            {
                var tm0 = DateTime.Now;
                var host = HostServicesHelper.GetBayarHost(services);
                var posts = host.OpenedBayar().Cast<Bayar>();

                var pivot = posts.SelectMany(x => x.bidangs, (y, z) => new { byr = y, bdg = z })
                   .SelectMany(x => x.byr.details, (a, b) =>
                   new BayarPivot
                   {
                       keyTahap = a.byr.key,
                       keyProject = a.byr.keyProject,
                       keyPersil = a.bdg.keyPersil,
                       tahap = a.byr.nomorTahap,
                       Kode = ((JenisBayar)b.jenisBayar).JenisByrPivot(),
                       value = b.subdetails.Any(x => x.keyPersil == a.bdg.keyPersil) == true ? b.subdetails.FirstOrDefault(x => x.keyPersil == a.bdg.keyPersil).Jumlah : 0,

                   }).Where(x => x.value != 0).OrderBy(x => x.keyProject).ToList();

                var sisa = pivot.Select(x => new { keyTahap = x.keyTahap, keyPersil = x.keyPersil, keyProject = x.keyProject, nomorTahap = x.tahap }).Distinct().ToList();
                var listpivot = new List<BayarPivot>();

                //foreach (var item in sisa)
                //{
                //    var byr = host.GetBayar(item.keyTahap) as Bayar;
                //    var persil = GetPersil(item.keyPersil);

                //    (var SisaLunas, var totalPembayaran, var pph) = byr.SisaPelunasan2(persil);

                //    var bayarPivot = new BayarPivot
                //    {
                //        keyTahap = item.keyTahap,
                //        keyProject = item.keyProject,
                //        keyPersil = item.keyPersil,
                //        tahap = item.nomorTahap,
                //        Kode = "FSisaPelunasan",
                //        value = SisaLunas
                //    };

                //    listpivot.Add(bayarPivot);
                //}

                var merge = pivot.Concat(listpivot);
                var villages = contextes.GetVillages();

                var persils = contextes.GetDocuments(new { key = "", IdBidang = "" }, "persils_v2",
                   $"<$match:<invalid:<$ne:true>,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                   "{$project:{_id:0,key:1,IdBidang:1}}").ToList();

                var data = merge.Join(persils, d => d.keyPersil, p => p.key, (d, p) => d.setIdBidang(p.IdBidang));
                var datas = data.Join(villages, d => d.keyProject, p => p.project.key, (d, p) => d.setProject(p.project.identity)).Distinct();

                //var sb = MakeCsv(datas.ToArray());
                //return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };

                return new JsonResult(datas.OrderBy(x => x.keyProject).ToArray());
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpGet("bidang/bebas")]
        public IActionResult GetBidangBebas([FromQuery] string token) // Bidang Bebas yang belum ada pembayaran
        {
            var tm0 = DateTime.Now;
            var host = HostServicesHelper.GetBayarHost(services);
            var posts = host.OpenedBayar().Cast<Bayar>().ToList();

            var bidangs = posts.SelectMany(x => x.bidangs);

            var persils = contextpay.persils.Query(x => x.invalid != true && x.en_state == 0 && x.basic.current != null);

            var datas = (from c in persils
                         where !(from o in bidangs
                                 select o.keyPersil).Contains(c.key)
                         select c).ToList();

            var villages = contextpay.GetVillages();

            var data = datas.Join(villages, d => (d.basic?.current?.keyDesa ?? ""), v => v.desa.key, (d, v) => d.toView(v.project?.identity, v.desa?.identity));

            //var data = datas.Select(a => a.toView(contextes)).ToArray();

            var sb = MakeCsv(data);
            return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
        }

        /// <summary>
        /// Get Data for Memo Pembayaran
        /// </summary>
        [NeedToken("PAY_LAND_FULL")]
        [ProducesResponseType(typeof(MemoPembayaranView), (int)HttpStatusCode.OK)]
        [HttpGet("report/memo")]
        public IActionResult GetReportMemo([FromQuery] string token, string bkey, string detailKey)
        {
            try
            {
                var user = contextplus.FindUser(token);
                List<string> tKeys = bkey.Trim().Split(",").ToList();
                List<string> bKeys = detailKey.Trim().Split(",").ToList();

                if (tKeys.Distinct().Count() > 1)
                    return new UnprocessableEntityObjectResult("Mohon memilih tahap yang sama!");

                var time = DateTime.Now.Date;
                var host = HostServicesHelper.GetBayarHost(services);
                if (host.GetBayar(tKeys.FirstOrDefault()) is not Bayar byr)
                    return new UnprocessableEntityObjectResult("Tahap tidak ada");
                if (tKeys.Count() != bKeys.Count())
                    return new UnprocessableEntityObjectResult("Detail tidak tersedia");
                if (byr.bidangs == null)
                    return new UnprocessableEntityObjectResult("Bidang belum ditentukan");

                List<DetailBidangs> listDetailBidang = new List<DetailBidangs>();
                List<DetailPembayaran> listbiayalain = new List<DetailPembayaran>();

                var jenisbyr = byr.details.Where(det => det.invalid != true && bKeys.Contains(det.key)).Select(a => a.jenisBayar).ToList();

                if (jenisbyr.Count() == 0)
                    return new UnprocessableEntityObjectResult("Pembayaran dipilih telah dibatalkan !");

                var details = GetBayarDtlByJenisBayar(jenisbyr, bKeys, byr);
                var alls = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList();
                var keyPersilBayar = details.Where(det => bKeys.Contains(det.key) && det.invalid != true)
                                            .SelectMany(det => det.subdetails, (x, y) => new { keyPersil = y.keyPersil })
                                            .Select(x => x.keyPersil).Distinct().ToList();
                var keyPersilBidang = byr.bidangs.Select(bdg => bdg.keyPersil).ToList();
                var staticCollections = contextes.GetCollections(new StaticCollection(), "static_collections", "{_t:'MemoPembayaran'}", "{_id:0}").FirstOrDefault();
                var dataCompany = context.db.GetCollection<Company>("masterdatas").Find("{_t:'pt',invalid:{$ne:true}}").ToList();
                var notaris = context.db.GetCollection<Notaris>("masterdatas").Find("{_t:'notaris', invalid:{$ne:true}}").ToList();

                foreach (var item in byr.bidangs)
                {
                    DetailBidangs dtlBidang = new DetailBidangs();
                    var persil = GetPersil(item.keyPersil);
                    var villages = contextes.GetVillage(persil.basic.current.keyDesa);
                    dtlBidang.pKey = item.keyPersil;
                    dtlBidang.IsBayar = details.SelectMany(x => x.subdetails, (x, y) => new { keyPersil = y.keyPersil }).Any(x => x.keyPersil.Contains(item.keyPersil));
                    dtlBidang.Alias = persil.basic.current.alias;
                    dtlBidang.Pemilik = persil.basic.current.pemilik;
                    dtlBidang.Desa = villages.desa.identity; ;
                    dtlBidang.Project = villages.project.identity;
                    var satuan = persil.basic.current.satuan == null || persil.basic.current.satuan == 0 ? persil.basic.current.satuanAkte : persil.basic.current.satuan;
                    var satuanAkte = persil.basic.current.satuanAkte == null || persil.basic.current.satuanAkte == 0 ? persil.basic.current.satuan : persil.basic.current.satuanAkte;
                    dtlBidang.id_bidang = persil.IdBidang;
                    dtlBidang.Perusahaan = !string.IsNullOrEmpty(persil.basic.current.keyPenampung) ? dataCompany.Where(comp => comp.key == persil.basic.current.keyPenampung).FirstOrDefault().identifier : (!string.IsNullOrEmpty(persil.basic.current.keyPTSK) ? HostServicesHelper.GetPTSKHost(services).GetPTSK(persil.basic.current.keyPTSK).identifier : "");
                    dtlBidang.NomorPeta = persil.basic.current.noPeta;
                    dtlBidang.SuratAsal = persil.basic.current.surat.nomor;
                    dtlBidang.LuasSurat = persil.basic.current.luasSurat;
                    dtlBidang.LuasUkurInternal = persil.basic.current.luasInternal;
                    dtlBidang.LuasBayar = persil.basic.current.luasDibayar ?? persil.basic.current.luasSurat;
                    dtlBidang.id_bidang = persil.IdBidang;
                    dtlBidang.Harga = satuan ?? 0;
                    dtlBidang.Jumlah = dtlBidang.LuasBayar * satuan ?? 0;
                    dtlBidang.SatuanAkte = satuanAkte;
                    dtlBidang.Satuan = satuan;
                    dtlBidang.Notaris = notaris.Where(n => n.key == persil.PraNotaris).FirstOrDefault() == null ? "" : notaris.Where(n => n.key == persil.PraNotaris).FirstOrDefault().identifier;

                    var overlap = (alls.SelectMany(x => x.overlap, (x, y) => new { bidangBintang = x.IdBidang, bidangOV = y.IdBidang, luasOV = y.luas })
                                       .Where(x => x.bidangOV == persil.IdBidang)
                                       .Select(x => new
                                       {
                                           bidangOV = x.bidangOV
                                                       ,
                                           bidangBintang = x.bidangBintang
                                                       ,
                                           luasOV = x.luasOV
                                       })
                                   ).ToList();

                    // masuk cari ke overlap
                    if (overlap.Select(ov => ov.bidangBintang).Any() && jenisbyr.Contains(JenisBayar.Lunas))
                    {
                        foreach (var ov in overlap)
                        {
                            DetailBidangs dtlBidangOV = new DetailBidangs();
                            dtlBidangOV.fillDetailBidang(dtlBidang);
                            var pesilBintang = GetPersilByIdBidang(ov.bidangBintang);
                            var nomor = PersilHelper.GetNomorPBT(pesilBintang);
                            var luasPBT = PersilHelper.GetLuasBayar(pesilBintang);
                            dtlBidangOV.NamaSurat = pesilBintang.basic.current.surat.nama.ToString();
                            dtlBidangOV.AlasHak = pesilBintang.basic.current.surat.nomor.ToString();
                            dtlBidangOV.Tahap = byr.nomorTahap.ToString();
                            dtlBidangOV.LuasPBT = luasPBT;
                            dtlBidangOV.NIB = nomor.NIB;
                            dtlBidangOV.luasBintang = pesilBintang.basic.current.luasDibayar ?? pesilBintang.basic.current.luasSurat;
                            dtlBidangOV.luasOverlap = ov.luasOV;
                            listDetailBidang.Add(dtlBidangOV);
                        }
                    }
                    else
                    {
                        listDetailBidang.Add(dtlBidang);
                    }

                    if (dtlBidang.IsBayar && (details.Any(x => (x.jenisBayar != JenisBayar.UTJ) && x.subdetails.Any(sub => sub.keyPersil == item.keyPersil))))
                    {

                        if (jenisbyr.Contains(JenisBayar.Lunas))
                        {
                            listbiayalain.Add(FillBiaya(persil, "BiayaBN", "Biaya Balik Nama"));
                            listbiayalain.Add(FillBiaya(persil, "pembatalanNIB", "Biaya Pembatalan NIB"));
                            listbiayalain.Add(FillBiaya(persil, "gantiBlanko", "Biaya Ganti Blanko"));
                            listbiayalain.Add(FillBiaya(persil, "kompensasi", "Biaya Kompensasi"));
                            listbiayalain.Add(FillBiaya(persil, "pajakLama", "Biaya Pajak Lama"));
                            listbiayalain.Add(FillBiaya(persil, "pajakWaris", "Biaya Pajak Waris"));
                            listbiayalain.Add(FillBiaya(persil, "tunggakanPBB", "Tunggakan PBB"));
                            listbiayalain.Add(FillBiaya(persil, "pph21", "PPH"));
                            listbiayalain.Add(FillBiaya(persil, "validasiPPh", "Validasi"));

                            //add biaya lainnya
                            foreach (var lain in persil.biayalainnya)
                            {
                                DetailPembayaran biayalainnya = new DetailPembayaran();
                                biayalainnya.fglainnya = lain.fgLainnya;
                                biayalainnya.identity = lain.identity;
                                biayalainnya.nilai = lain.nilai;
                                listbiayalain.Add(biayalainnya);
                            }
                        }
                        listbiayalain.Add(FillBiaya(persil, "mandor", "Biaya Mandor"));

                    }
                }

                listbiayalain = listbiayalain.GroupBy(lb => lb.identity)
                                             .Select(lbg => new DetailPembayaran
                                             {
                                                 keyOrder = lbg.First().keyOrder,
                                                 identity = lbg.First().identity,
                                                 nilai = Math.Abs(Convert.ToDouble(lbg.Sum(a => a.nilai))),
                                                 fglainnya = !(lbg.Sum(a => a.nilai) >= 0)
                                             }).ToList();
                var TotalJumlah = listDetailBidang.Select(x => new { bdg = x.id_bidang, jumlah = x.Jumlah }).Distinct().Sum(x => x.jumlah);

                if (jenisbyr.Contains(JenisBayar.UTJ))
                {
                    DetailPembayaran biayaUtj = new DetailPembayaran();
                    biayaUtj.identity = "UTJ";
                    biayaUtj.nilai = details.Where(det => det.jenisBayar == JenisBayar.UTJ).Sum(x => x.Jumlah);
                    biayaUtj.Tanggal = details.Where(det => det.jenisBayar == JenisBayar.UTJ).LastOrDefault().tglPenyerahan;
                    biayaUtj.fglainnya = false;
                    listbiayalain.Add(biayaUtj);
                }
                if (jenisbyr.Contains(JenisBayar.DP))
                {
                    listbiayalain = listbiayalain.Where(lb => lb.identity == "PPH" || lb.identity == "Validasi" || lb.identity == "Biaya Mandor").ToList();
                    var historyBayarUTJ = GetHistoryBayar(byr, JenisBayar.UTJ, keyPersilBayar);
                    var lastDate = details.Where(det => det.jenisBayar == JenisBayar.DP)
                                          .OrderByDescending(det => det.tglPenyerahan)
                                          .FirstOrDefault().tglPenyerahan;
                    var historyBayarDP = GetHistoryBayar(byr, JenisBayar.DP, keyPersilBidang).Where(dp => dp.Tanggal <= lastDate);
                    historyBayarDP.ToList().ForEach(dp => dp.keyOrder = 6);
                    listbiayalain = historyBayarUTJ.Union(listbiayalain).Union(historyBayarDP).ToList();
                }
                if (jenisbyr.Contains(JenisBayar.Lunas))
                {
                    listbiayalain = listbiayalain.Where(lb => lb.nilai < 0 || lb.identity == "PPH" || lb.identity == "Validasi" || lb.identity == "Biaya Mandor").ToList();
                    var historyBayarUTJ = GetHistoryBayar(byr, JenisBayar.UTJ, keyPersilBayar);
                    var historyBayarDP = GetHistoryBayar(byr, JenisBayar.DP, keyPersilBidang);
                    var lastDate = details.Where(det => det.jenisBayar == JenisBayar.Lunas)
                                          .OrderByDescending(det => det.tglPenyerahan)
                                          .FirstOrDefault().tglPenyerahan;

                    listbiayalain = listbiayalain.Union(historyBayarUTJ).Union(historyBayarDP).ToList();

                    var historyLunas = GetHistoryBayar(byr, JenisBayar.Lunas, keyPersilBidang).Where(lunas => lunas.Tanggal <= lastDate);
                    listbiayalain = listbiayalain.Union(historyLunas).ToList();
                }

                string defaultMemo = "/LiRealty/Ptw/" + GetRomanMonth(time.Month) + "/" + time.Year.ToString();
                var memo = details.Select(det => string.IsNullOrEmpty(det.noMemo) ? defaultMemo : det.noMemo).Distinct().ToList();
                string noMemo = string.Join(",", memo);
                string contactPersonName = string.IsNullOrEmpty(details.FirstOrDefault().contactPerson) ? staticCollections.contactPersonName : details.FirstOrDefault().contactPerson;
                string contactPersonTelp = String.IsNullOrEmpty(details.FirstOrDefault().noTlpCP) ? staticCollections.contactPersonPhone : details.FirstOrDefault().noTlpCP;
                string[] tembusan = String.IsNullOrEmpty(details.FirstOrDefault().tembusan) ? staticCollections.tembusan.Split("^_^") : details.FirstOrDefault().tembusan.Split("^_^");
                string memoSign = String.IsNullOrEmpty(details.FirstOrDefault().memoSigns) ? staticCollections.memoSign : details.FirstOrDefault().memoSigns;
                string memoTo = String.IsNullOrEmpty(details.FirstOrDefault().memoTo) ? staticCollections.memoTo : details.FirstOrDefault().memoTo;

                MemoPembayaranView result = new MemoPembayaranView();
                (result.NoMemo, result.Kepada, result.CreatedDate, result.detailBidangs,
                 result.TahapProject, result.TotalJumlah,
                 result.TotalLuasInternal, result.TotalLuasSurat,
                 result.TotalPBT, result.Pelunasan,
                 result.Note, result.NilaiAkte,
                 result.TanggalPenyerahan, result.TanggalPelunasan,
                 result.Giro,
                 result.Notaris, result.MemoSigns,
                 result.ContactPerson, result.ContactPersonPhone,
                 result.Tembusan, result.detailPembayaran

                ) =
                (noMemo, memoTo, byr.created, listDetailBidang.ToArray(),
                 "THP" + byr.nomorTahap + " / " + contextes.GetProject(byr.keyProject).identity, TotalJumlah,
                 listDetailBidang.Sum(ld => ld.LuasUkurInternal), listDetailBidang.Sum(ld => ld.LuasSurat),
                 listDetailBidang.Sum(ld => ld.LuasPBT), details.Sum(x => x.Jumlah),
                 string.Join(",", details.Select(x => x.note)), listDetailBidang.Select(ld => ld.SatuanAkte).FirstOrDefault(),
                 details.OrderByDescending(det => det.jenisBayar).ThenByDescending(det => det.tglPenyerahan).FirstOrDefault().tglPenyerahan,
                 details.OrderByDescending(det => det.jenisBayar).ThenByDescending(det => det.tglBayar).FirstOrDefault().tglBayar,
                 details.SelectMany(x => x.giro, (x, y) => new GiroCore()
                 {
                     AccountPenerima = y.accPenerima,
                     BankPenerima = y.bankPenerima,
                     NamaPenerima = y.namaPenerima,
                     Nominal = y.nominal,
                     Jenis = y.jenis,
                     key = y.key
                 }).ToArray(),
                 listDetailBidang.Select(ld => ld.Notaris).FirstOrDefault(), memoSign,
                 contactPersonName, contactPersonTelp,
                 tembusan, listbiayalain.OrderBy(lb => lb.keyOrder).ToArray()
                );

                return new JsonResult(result);
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
        /// Get Data for Report Status per Pembayaran csv
        /// </summary>
        //[NeedToken("")]
        [HttpGet("status/perbayar")]
        public IActionResult GetReportStatusPerPembayaran([FromQuery] string token, bool isAllStatus, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = contextes.GetDocuments(new RptStatusPerBayar(), "bayars",
                 "{$unwind: '$details'}",
                 "{$unwind: '$details.subdetails'}",
                  @"{$project: {
                                nomorTahap: 1,jenisPersil: 1,keyProject: 1,keyDesa: 1,group: 1,
                                jenisBayar: '$details.jenisBayar',
                                noMemo: '$details.noMemo',
                                grand: '$details.jumlah',
                                jumlahBayar: '$details.subdetails.Jumlah',
                                keyPersil: '$details.subdetails.keyPersil',
                                instkey: '$details.instkey',
                                tglBayar: '$details.tglBayar',
                                keyPenampung: 1,
                                step:'$details.instkey'
                            }}",
                 @"{$lookup: { from: 'persils_v2',localField: 'keyPersil',foreignField: 'key',as:'persil'}}",
                 @"{$unwind: { path: '$persil',preserveNullAndEmptyArrays: true}}",
                 @"{$project:
                                {
                                    _id : 0,
                                    tahap: 1,
                                    nomorTahap: '$nomorTahap',
                                    IdBidang : '$persil.IdBidang',
                                    group: '$persil.basic.current.group',
                                    jumlah: '$jumlahBayar',
                                    pemilik: '$persil.basic.current.pemilik',
                                    luasSurat: '$persil.basic.current.luasSurat',
                                    luasInternal: '$persil.basic.current.luasInternal',
                                    jenisBayar: '$jenisBayar',
                                    tglBayar: '$tglBayar',
                                    keyDesa: '$keyDesa',
                                    keyProject: '$keyProject',
                                    step: '$step'
                                }}",
                    "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",

                    "{$match : {'projects' : {$ne : []}}}",

                   @"{$project: {
                                    Group: '$group',
                                    NomorTahap: '$nomorTahap',
                                    IdBidang : '$IdBidang',
                                    Jumlah: '$jumlah',
                                    Pemilik: '$pemilik',
                                    LuasSurat: '$luasSurat',
                                    LuasInternal: '$luasInternal',
                                    JenisPembayaran: '$jenisBayar',
                                    tanggalPembayaran: '$tglBayar',
                                    Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                    Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                    Step: '$step'
                                }}"
                 ).ToList();

                var datad = contextes.GetDocuments(new RptStatusPerBayar(), "bayars",
                "{$unwind: '$deposits'}",
                "{$unwind: '$deposits.subdetails'}",
                 @"{$project: {
                                nomorTahap: 1,jenisPersil: 1,keyProject: 1,keyDesa: 1,group: 1,
                                jenisBayar: '$deposits.jenisBayar',
                                noMemo: '$deposits.noMemo',
                                grand: '$deposits.jumlah',
                                jumlahBayar: '$deposits.subdetails.Jumlah',
                                keyPersil: '$deposits.subdetails.keyPersil',
                                instkey: '$deposits.instkey',
                                tglBayar: '$deposits.tglBayar',
                                keyPenampung: 1,
                                step:'$deposits.instkey'
                            }}",
                @"{$lookup: { from: 'persils_v2',localField: 'keyPersil',foreignField: 'key',as:'persil'}}",
                @"{$unwind: { path: '$persil',preserveNullAndEmptyArrays: true}}",
                @"{$project:
                            {
                                _id : 0,
                                tahap: 1,
                                nomorTahap: '$nomorTahap',
                                IdBidang : '$persil.IdBidang',
                                group: '$persil.basic.current.group',
                                jumlah: '$jumlahBayar',
                                pemilik: '$persil.basic.current.pemilik',
                                luasSurat: '$persil.basic.current.luasSurat',
                                luasInternal: '$persil.basic.current.luasInternal',
                                jenisBayar: '$jenisBayar',
                                tglBayar: '$tglBayar',
                                keyDesa: '$keyDesa',
                                keyProject: '$keyProject',
                                step: '$step'
                                }}",
                   "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                   "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",

                   "{$match : {'projects' : {$ne : []}}}",

                  @"{$project: {
                                    Group: '$group',
                                    NomorTahap: '$nomorTahap',
                                    IdBidang : '$IdBidang',
                                    Jumlah: '$jumlah',
                                    Pemilik: '$pemilik',
                                    LuasSurat: '$luasSurat',
                                    LuasInternal: '$luasInternal',
                                    JenisPembayaran: '$jenisBayar',
                                    tanggalPembayaran: '$tglBayar',
                                    Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                    Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                    Step: '$step'
                                }}"
                ).ToList();

                datas = datas.Union(datad).ToList();

                var instanceKeys = datas.Select(d => d.Step).ToList();
                string instKeys = string.Join(",", instanceKeys);
                var stepsto = Enum.GetValues(typeof(ToDoState)).Cast<ToDoState>().Select(a => a.AsStatus()).ToList();
                if (!isAllStatus)
                    stepsto = stepsto.Where(s => s == ToDoState.complished_.AsStatus()).ToList();
                string StepsToGet = string.Join(",", stepsto);

#if GRPC
                var Steps = (ghost.GetMany(instKeys, StepsToGet).GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
#else
                var Steps = (graphhost.GetMany(instKeys, StepsToGet) ?? new GraphMainInstance[0]);
#endif

                var dataCSV = datas.Join(Steps, d => d.Step, s => s.key, (d, s) => new RptStatusPerBayarCSV()
                {

                    IdBidang = d.IdBidang,
                    NomorTahap = d.NomorTahap,
                    Group = d.Group,
                    Pemilik = d.Pemilik,
                    Jumlah = d.Jumlah,
                    LuasInternal = d.LuasInternal,
                    LuasSurat = d.LuasSurat,
                    JenisPembayaran = d.JenisPembayaran,
                    tanggalPembayaran = d.tanggalPembayaran == null ? s?.states == null ? null : Convert.ToDateTime(s?.states.LastOrDefault(ss => ss?.state == ToDoState.cashierApproved_)?.time).ToLocalTime().ToString("yyyy/MM/dd hh:mm:ss tt") : Convert.ToDateTime(d.tanggalPembayaran).ToLocalTime().ToString("yyyy/MM/dd hh:mm:ss tt"),
                    Desa = d.Desa,
                    Project = d.Project,
                    Step = s?.lastState?.state.AsStatus()
                }).ToList();

                var data = dataCSV.Evaluate(gs.where, gs.where2, gs.sortColumn, gs.sortOrder);
                var sb = MakeCsv(data.result.ToArray());
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Report Status per Pembayaran Paging
        /// </summary>
        //[NeedToken("")]
        [HttpGet("status/perbayar-paging")]
        public IActionResult GetReportStatusPerPembayaranPaging([FromQuery] string token, bool isAllStatus, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var datas = contextes.GetDocuments(new RptStatusPerBayar(), "bayars",
                 "{$unwind: '$details'}",
                 "{$unwind: '$details.subdetails'}",
                  @"{$project: {
                                nomorTahap: 1,jenisPersil: 1,keyProject: 1,keyDesa: 1,group: 1,
                                jenisBayar: '$details.jenisBayar',
                                noMemo: '$details.noMemo',
                                grand: '$details.jumlah',
                                jumlahBayar: '$details.subdetails.Jumlah',
                                keyPersil: '$details.subdetails.keyPersil',
                                instkey: '$details.instkey',
                                tglBayar: '$details.tglBayar',
                                keyPenampung: 1,
                                step:'$details.instkey'
                            }}",
                 @"{$lookup: { from: 'persils_v2',localField: 'keyPersil',foreignField: 'key',as:'persil'}}",
                 @"{$unwind: { path: '$persil',preserveNullAndEmptyArrays: true}}",
                 @"{$project:
                            {
                                _id : 0,
                                tahap: 1,
                                nomorTahap: '$nomorTahap',
                                IdBidang : '$persil.IdBidang',
                                group: '$persil.basic.current.group',
                                jumlah: '$jumlahBayar',
                                pemilik: '$persil.basic.current.pemilik',
                                luasSurat: '$persil.basic.current.luasSurat',
                                luasInternal: '$persil.basic.current.luasInternal',
                                jenisBayar: '$jenisBayar',
                                tglBayar: '$tglBayar',
                                keyDesa: '$keyDesa',
                                keyProject: '$keyProject',
                                step: '$step'
                                }}",
                    "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",

                    "{$match : {'projects' : {$ne : []}}}",

                   @"{$project: {
                                    Group: '$group',
                                    NomorTahap: '$nomorTahap',
                                    IdBidang : '$IdBidang',
                                    Jumlah: '$jumlah',
                                    Pemilik: '$pemilik',
                                    LuasSurat: '$luasSurat',
                                    LuasInternal: '$luasInternal',
                                    JenisPembayaran: '$jenisBayar',
                                    tanggalPembayaran: '$tglBayar',
                                    Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                    Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                    Step: '$step'
                                }}"
                 ).ToList();

                var datad = contextes.GetDocuments(new RptStatusPerBayar(), "bayars",
                 "{$unwind: '$deposits'}",
                 "{$unwind: '$deposits.subdetails'}",
                  @"{$project: {
                                nomorTahap: 1,jenisPersil: 1,keyProject: 1,keyDesa: 1,group: 1,
                                jenisBayar: '$deposits.jenisBayar',
                                noMemo: '$deposits.noMemo',
                                grand: '$deposits.jumlah',
                                jumlahBayar: '$deposits.subdetails.Jumlah',
                                keyPersil: '$deposits.subdetails.keyPersil',
                                instkey: '$deposits.instkey',
                                tglBayar: '$deposits.tglBayar',
                                keyPenampung: 1,
                                step:'$deposits.instkey'
                            }}",
                 @"{$lookup: { from: 'persils_v2',localField: 'keyPersil',foreignField: 'key',as:'persil'}}",
                 @"{$unwind: { path: '$persil',preserveNullAndEmptyArrays: true}}",
                 @"{$project:
                            {
                                _id : 0,
                                tahap: 1,
                                nomorTahap: '$nomorTahap',
                                IdBidang : '$persil.IdBidang',
                                group: '$persil.basic.current.group',
                                jumlah: '$jumlahBayar',
                                pemilik: '$persil.basic.current.pemilik',
                                luasSurat: '$persil.basic.current.luasSurat',
                                luasInternal: '$persil.basic.current.luasInternal',
                                jenisBayar: '$jenisBayar',
                                tglBayar: '$tglBayar',
                                keyDesa: '$keyDesa',
                                keyProject: '$keyProject',
                                step: '$step'
                                }}",
                    "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",

                    "{$match : {'projects' : {$ne : []}}}",

                   @"{$project: {
                                    Group: '$group',
                                    NomorTahap: '$nomorTahap',
                                    IdBidang : '$IdBidang',
                                    Jumlah: '$jumlah',
                                    Pemilik: '$pemilik',
                                    LuasSurat: '$luasSurat',
                                    LuasInternal: '$luasInternal',
                                    JenisPembayaran: '$jenisBayar',
                                    tanggalPembayaran: '$tglBayar',
                                    Desa: {$ifNull:[{$arrayElemAt:['$desas.identity',-1]},'']},
                                    Project: {$ifNull:[{$arrayElemAt:['$projects.identity',-1]},'']},
                                    Step: '$step'
                                }}"
                 ).ToList();

                datas = datas.Union(datad).ToList();

                var instanceKeys = datas.Select(d => d.Step).ToList();
                string instKeys = string.Join(",", instanceKeys);
                var stepsto = Enum.GetValues(typeof(ToDoState)).Cast<ToDoState>().Select(a => a.AsStatus()).ToList();
                if (!isAllStatus)
                    stepsto = stepsto.Where(s => s == ToDoState.complished_.AsStatus()).ToList();
                string StepsToGet = string.Join(",", stepsto);

#if GRPC
                var Steps = (ghost.GetMany(instKeys, StepsToGet).GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
#else
                var Steps = (graphhost.GetMany(instKeys, StepsToGet) ?? new GraphMainInstance[0]);
#endif

                datas = datas.Join(Steps, d => d.Step, s => s.key, (d, s) => new RptStatusPerBayar()
                {
                    IdBidang = d.IdBidang,
                    NomorTahap = d.NomorTahap,
                    Group = d.Group,
                    Pemilik = d.Pemilik,
                    Jumlah = d.Jumlah,
                    LuasInternal = d.LuasInternal,
                    LuasSurat = d.LuasSurat,
                    JenisPembayaran = d.JenisPembayaran,
                    tanggalPembayaran = d.tanggalPembayaran == null ? s?.states == null ? null : s?.states.LastOrDefault(ss => ss?.state == ToDoState.cashierApproved_)?.time : Convert.ToDateTime(d.tanggalPembayaran).ToLocalTime(),
                    Desa = d.Desa,
                    Project = d.Project,
                    Step = s?.lastState?.state.AsStatus()
                }).ToList();

                var xlst = ExpressionFilter.Evaluate(datas, typeof(List<RptStatusPerBayar>), typeof(RptStatusPerBayar), gs);
                var data = xlst.result.Cast<RptStatusPerBayar>().ToArray();
                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Report Pembayaran Per Deal CSV
        /// </summary>
        //[NeedToken("")]
        [HttpGet("status/per-deal")]
        public IActionResult GetReportBayarPerDeal([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var user = contextplus.FindUser(token);
                //var datas = contextes.GetDocuments(new RptStatusPerDealMod(), "rpt_bidang_deal", "{$project : {_id : 0}}");

                var persils = contextes.GetDocuments( new { key = "", IdBidang = "", group = "", pemilik = "", AlasHak = "",
                keyProject = "", keyDesa = "", StatusBidang = StatusBidang.belumbebas , StatusDeal = StatusDeal._}, "persils_v2",
                "{$match: {'invalid': {$ne: true}}}",
                @"{
                    '$project': {
                        '_id': 0,
                        'key': '$key',
                        'IdBidang': '$IdBidang',
                        'group': '$basic.current.group',
                        'pemilik': {
                                        '$ifNull': ['$basic.current.pemilik', '$basic.current.surat.nama']
                        },
                        'AlasHak': '$basic.current.surat.nomor',
                        'keyProject': '$basic.current.keyProject',
                        'keyDesa': '$basic.current.keyDesa',
                        'StatusBidang': {
                                        '$ifNull': ['$en_state', 0]
                        },
                        'StatusDeal': {
                                        '$ifNull': [
                                            {
                                            '$arrayElemAt': ['$dealStatus.status', -1]
                                },
                                7
                            ]
                        }
                    }}"
                ).ToList();

                var bayars = contextes.GetDocuments( new { keyPersil = "", nomorTahap = 0}, "bayars",
                "{$match : {invalid : {$ne:true}}}",
                "{$unwind : '$bidangs'}",
                @"{
                    $project:
                    {
                        _id: 0,
                        nomorTahap: 1,
                        keyPersil: '$bidangs.keyPersil',
                    }
                }"
                ).ToList();

                var locations = contextes.GetVillages();

                var datas =
                    (from p in persils
                     join b in bayars
                     on p.key equals b.keyPersil into pb
                     from pbs in pb.DefaultIfEmpty()
                     select new RptStatusPerDealMod()
                     {
                         key = p.key,
                         AlasHak = p.AlasHak,
                         @Group = p.@group,
                         Tahap = pbs is null ? 0 : pbs.nomorTahap,
                         IdBidang = p.IdBidang,
                         Pemilik = p.pemilik,
                         StatusBidang = p.StatusBidang,
                         StatusDeal = p.StatusDeal,
                         Project = locations.FirstOrDefault(l => l.project.key == p.keyProject).project?.identity,
                         Desa = locations.FirstOrDefault(l => l.desa.key == p.keyDesa).desa?.identity
                     })
                    .Where(d => !string.IsNullOrWhiteSpace(d.Project))
                    .ToList();

                int GetHashCode(RptStatusPerDealMod obj)
                {
                    string key = !string.IsNullOrWhiteSpace(obj.key) ? obj.key : String.Empty;
                    string alasHak = !string.IsNullOrWhiteSpace(obj.AlasHak) ? obj.AlasHak : String.Empty;
                    string group = !string.IsNullOrWhiteSpace(obj.Group) ? obj.Group : String.Empty;
                    int tahap = obj.Tahap;
                    string idBidang = !string.IsNullOrWhiteSpace(obj.IdBidang) ? obj.IdBidang : String.Empty;
                    string pemilik = !string.IsNullOrWhiteSpace(obj.Pemilik) ? obj.Pemilik : String.Empty;
                    int statusBidang = obj.StatusBidang.GetHashCode();
                    int statusDeal = obj.StatusDeal.GetHashCode();
                    string project = !string.IsNullOrWhiteSpace(obj.Project) ? obj.Project : String.Empty;
                    string desa = !string.IsNullOrWhiteSpace(obj.Desa) ? obj.Desa : String.Empty;

                    return (key + alasHak + group + tahap.ToString() + idBidang + pemilik + statusBidang.ToString() + statusDeal.ToString() +
                        project + desa).GetHashCode();
                }

                Dictionary<int, RptStatusPerDealMod> distinctDict = new();
                foreach (var daItem in datas)
                {
                    int key = GetHashCode(daItem);
                    if (!distinctDict.ContainsKey(key))
                        distinctDict.Add(key, daItem);
                }

                var dataView = distinctDict.Values.Select(x => x.toView()).ToList();
                var xlst = ExpressionFilter.Evaluate(dataView, typeof(List<RptStatusPerDeal>), typeof(RptStatusPerDeal), gs);
                var data = xlst.result.Cast<RptStatusPerDeal>().ToArray();
                var sb = MakeCsv(data.ToArray());
                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Data for Report Pembayaran Per Deal Paging
        /// </summary>
        // [NeedToken("")]
        [HttpGet("status/per-deal-paging")]
        public IActionResult GetReportBayarPerDealPaging([FromQuery] string token, [FromQuery] AgGridSettings gs)
        {
            try
            {
                var tm0 = DateTime.Now;
                var user = contextplus.FindUser(token);
                var filters = new List<string>();
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
                else
                {
                    sortstages.Add("{$sort:{'IdBidang': 1 }}");
                }

                int skip = (gs.pageIndex - 1) * gs.pageSize;
                int limit = gs.pageSize > 0 ? gs.pageSize : 0;

                var datas = CollectBidangDeal(token, prestages, poststages, sortstages, skip, limit);
                var dataView = datas.data.Select(x => x.toView()).ToList();

                int totalRecords = datas.count;
                int totalPages = limit <= 0 ? 1 : (totalRecords - 1) / limit + 1;
                var lresult = dataView;
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
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Report luas sisa Overlap
        /// </summary>
        //[NeedToken("OVL_FULL,OVL_REVIEW,OVL_VIEW")]
        [HttpGet("overlap/luas-sisa")]
        public IActionResult GetRptLuasSisaOverlap([FromQuery] string token)
        {
            try
            {
                var user = contextes.FindUser(token);
                var qry = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList();

                var overlaps = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                                  .GroupBy(x => x.id, (x, y) => new OverlapCmd
                                  {
                                      IdBidang = x,
                                      luas = y.Sum(z => z.luas)
                                  }).ToList();

                var all = contextes.GetDocuments(new PersilOvp(), "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':2,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToList();

                var bidangPersils = string.Join(",", all.Select(a => a.key));
                string bidangPersil = string.Join(",", bidangPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());
                var bayar = contextes.GetDocuments(new { nomorTahap = (int?)null, keyPersil = "" }, "bayars", "{$unwind: '$bidangs'}",
                                                                                                    @"{$project:{
                                                                                                                 keyPersil: '$bidangs.keyPersil',
                                                                                                                 nomorTahap: 1,
                                                                                                                 _id: 0
                                                                                                               }}",
                                                                                                  "{$match:{keyPersil: {$in: [" + bidangPersil + "]}}}"

                                                ).ToList();

                var locations = contextes.GetVillages();

                var bidangs = all.Join(overlaps, a => a.IdBidang, b => b.IdBidang, (a, b) => new { Idbidang = a.IdBidang, sisaLuas = a.basic.luasSurat - b.luas }).ToList(); //Bidang Overlap yang sisa luasnya habis kaarena masuk di bidang2 bintang
                var persils = all.Select(x => x.toViewOverlap(bidangs.Select(x => (x.Idbidang, x.sisaLuas)))).ToList();
                persils = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                                 .ToList();

                var data = persils.Select(p =>
                new RptSisaLuasOverlap
                {
                    IdBidang = p.IdBidang,
                    Bebas = p.bebas,
                    Project = p.project,
                    Desa = p.desa,
                    NoPeta = p.noPeta,
                    NoSurat = p.nomorSurat,
                    LuasSurat = p.luasSurat,
                    SisaLuas = p.sisaLuas,
                    Group = p.group,
                    Tahap = bayar.Where(b => b.keyPersil == p.key).FirstOrDefault() != null ? bayar.Where(b => b.keyPersil == p.key).FirstOrDefault().nomorTahap : p.tahap,
                    NamaDiSurat = p.namaSurat

                });

                var sb = MakeCsv(data.ToArray());

                return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        /// <summary>
        /// Get Report luas sisa Overlap
        /// </summary>
        //[NeedToken("OVL_FULL,OVL_REVIEW,OVL_VIEW")]
        [HttpGet("overlap/luas-sisa/paging")]
        public IActionResult GetRptLuasSisaOverlapPaging([FromQuery] string token, AgGridSettings gs)
        {
            try
            {
                var user = contextes.FindUser(token);
                var qry = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList();

                var overlaps = qry.SelectMany(x => x.overlap, (x, y) => new { id = y.IdBidang, luas = y.luas })
                                  .GroupBy(x => x.id, (x, y) => new OverlapCmd
                                  {
                                      IdBidang = x,
                                      luas = y.Sum(z => z.luas)
                                  }).ToList();

                var all = contextes.GetDocuments(new PersilOvp(), "persils_v2",
                    $"<$match:<invalid:<$ne:true>,'basic.current.en_proses':2,'basic.current':<$ne:null>>>".Replace("<", "{").Replace(">", "}"),
                    "{$project:{_id:0,key:1,IdBidang:1,en_state:1,basic:'$basic.current'}}").ToList();

                var bidangPersils = string.Join(",", all.Select(a => a.key));
                string bidangPersil = string.Join(",", bidangPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());
                var bayar = contextes.GetDocuments(new { nomorTahap = (int?)null, keyPersil = "" }, "bayars", "{$unwind: '$bidangs'}",
                                                                                                    @"{$project:{
                                                                                                                 keyPersil: '$bidangs.keyPersil',
                                                                                                                 nomorTahap: 1,
                                                                                                                 _id: 0
                                                                                                               }}",
                                                                                                  "{$match:{keyPersil: {$in: [" + bidangPersil + "]}}}"

                                                ).ToList();

                var locations = contextes.GetVillages();

                var bidangs = all.Join(overlaps, a => a.IdBidang, b => b.IdBidang, (a, b) => new { Idbidang = a.IdBidang, sisaLuas = a.basic.luasSurat - b.luas }).ToList(); //Bidang Overlap yang sisa luasnya habis kaarena masuk di bidang2 bintang
                var persils = all.Select(x => x.toViewOverlap(bidangs.Select(x => (x.Idbidang, x.sisaLuas)))).ToList();
                persils = persils.Join(locations, d => d.keyDesa, p => p.desa.key, (d, p) => d.SetLocation(p.project.identity, p.desa.identity))
                                 .ToList();

                var datas = persils.Select(p =>
                new RptSisaLuasOverlap
                {
                    IdBidang = p.IdBidang,
                    Bebas = p.bebas,
                    Project = p.project,
                    Desa = p.desa,
                    NoPeta = p.noPeta,
                    NoSurat = p.nomorSurat,
                    LuasSurat = p.luasSurat,
                    SisaLuas = p.sisaLuas,
                    Group = p.group,
                    Tahap = bayar.Where(b => b.keyPersil == p.key).FirstOrDefault() != null ? bayar
                                 .Where(b => b.keyPersil == p.key).FirstOrDefault().nomorTahap : p.tahap,
                    NamaDiSurat = p.namaSurat
                });

                var xlst = ExpressionFilter.Evaluate(datas, typeof(List<RptSisaLuasOverlap>), typeof(RptSisaLuasOverlap), gs);
                var data = xlst.result.Cast<RptSisaLuasOverlap>().ToList();

                return Ok(data.GridFeed(gs));
            }
            catch (UnauthorizedAccessException exa)
            {
                return new ContentResult { StatusCode = int.Parse(exa.Message) };
            }
        }

        //[NeedToken("PAY_LAND_FULL,PAY_LAND_REVIEW")]
        [HttpGet("bidang/lunas/dtl")]
        public IActionResult GetDtlPelunasanBidang([FromQuery] string token, string opr, DateTime? cutOffDate, SisaPelunasanType reportType = SisaPelunasanType._)
        {
            try
            {
                var user = contextes.FindUser(token);
                cutOffDate = cutOffDate == null ?
                             new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1).AddTicks(-1) :
                             new DateTime(cutOffDate.GetValueOrDefault().Year, cutOffDate.GetValueOrDefault().Month, cutOffDate.GetValueOrDefault().Day).AddDays(1).AddTicks(-1);

                var bayar = contextes.GetDocuments(new Bayar(), "bayars", "{$match : {'bidangs' : {$ne:[]}}}").ToList();
                var datas = contextes.GetDocuments(new RptDetailLunasBidang(), "bayars",
                             "{$unwind: '$bidangs'}",
                             "{$match : {'bidangs' : {$ne:[]}}}",
                             @"{$project: {      _id  :0
                                            ,tKey :'$key'
                                            ,keyPersil: '$bidangs.keyPersil'
                                            ,keyDesa : '$keyDesa'
                                            ,keyProject : '$keyProject'
                                            }
                              }",
                             "{$lookup:{ from:'persils_v2', localField: 'keyPersil', foreignField: 'key', as : 'persils'}}",
                             "{$unwind:{ path:'$persils', preserveNullAndEmptyArrays: true}}",
                             @"{$project : {
                                             tKey: '$tKey'
                                            ,keyPersil: '$keyPersil'
                                            ,IdBidang : {$ifNull: ['$persils.IdBidang', '']}
                                            ,keyDesa: '$keyDesa'
                                            ,luasFix: {$ifNull:['$persils.luasFix', true]}
                                            ,luasNibTemp : {$ifNull : ['$persils.basic.current.luasNIBTemp',0]}
                                            ,luasDibayar : {$ifNull : ['$persils.basic.current.luasDibayar',0]}
                                            ,luasPelunasan : {$ifNull : ['$persils.luasPelunasan', 0]}
                                            ,luasSurat : {$ifNull : ['$persils.basic.current.luasSurat', 0]}
                                            ,satuan : {$ifNull : ['$persils.basic.current.satuan', 0]}
                                            ,satuanAkte : {$ifNull:['$persils.basic.current.satuanAkte',0]}
                                            ,pemilik : {$ifNull : ['$persils.basic.current.pemilik', '']}
                                            ,alasHak : {$ifNull : ['$persils.basic.current.surat.nomor', '']}
                                            ,mandor: {$ifNull : ['$persils.mandor',0]}
                                            ,pembatalanNIB: {$ifNull : ['$persils.pembatalanNIB',0]}
                                            ,BiayaBN: {$ifNull : ['$persils.BiayaBN',0]}
                                            ,gantiBlanko: {$ifNull : ['$persils.gantiBlanko',0]}
                                            ,kompensasi: {$ifNull : ['$persils.kompensasi',0]}
                                            ,pajakLama: {$ifNull : ['$persils.pajakLama',0]}
                                            ,pajakWaris: {$ifNull : ['$persils.pajakWaris',0]}
                                            ,tunggakanPBB: {$ifNull : ['$persils.tunggakanPBB',0]}
                                            ,ValidasiPPH : {$ifNull : ['$persils.ValidasiPPH', false]}
                                            ,ValidasiPPHValue : {$ifNull : ['$persils.ValidasiPPHValue', 0]}
                                            ,biayaLainnya : '$persils.biayalainnya'
                                            ,pph21 : {$ifNull : ['$persils.pph21', false]}
                                           }
                               }").ToList();

                var villages = contextes.GetVillages();

                datas = datas.Join(villages, d => d.keyDesa, p => p.desa.key, (d, p) => d.setLocation(p.project.identity, p.desa.identity)).ToList();

                var bundles = contextes.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
                    "{$match: {'_t' : 'mainBundle'}}",
                    "{$unwind: '$doclist'}",
                    "{$match : {$and : [{'doclist.keyDocType':'JDOK032'}, {'doclist.entries' : {$ne : []}}]}}",
                    @"{$project : {
                                      _id: 0
                                    , key: 1
                                    , doclist: 1
                    }}").ToList();

                var type = MetadataKey.Luas.ToString("g");
                var cleanbundle = bundles.Select(x => new { key = x.key, entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value })
                    .Select(x => new { key = x.key, dyn = x.entries.props.TryGetValue(type, out Dynamic val) ? val : null })
                    .Select(x => new { key = x.key, luasNIB = Convert.ToDouble(x.dyn?.Value ?? 0) }).ToList();

                var data = new List<RptDetailLunasBidangView>();

                if (reportType != SisaPelunasanType._)
                {
                    var instKeys = string.Join(",", bayar.SelectMany(x => x.details).Select(x => x.instkey));
#if GRPC
                    var instances = (ghost.GetMany(instKeys, "").GetAwaiter().GetResult() ?? new GraphMainInstance[0]);
#else
                    var instances = (graphhost.GetMany(instKeys, "") ?? new GraphMainInstance[0]);
#endif

                    data = datas.Select(a => a.toView2(bayar.Where(b => b.key == a.tKey).FirstOrDefault(),
                                                       (cleanbundle.Select(x => (x.key, x.luasNIB)).ToList()),
                                                       instances, cutOffDate
                                       )).ToList();
                }
                else
                {
                    data = datas.Select(a => a.toView(bayar.Where(b => b.key == a.tKey).FirstOrDefault(), (cleanbundle.Select(x => (x.key, x.luasNIB)).ToList()))).ToList();
                }

                if (opr == "csv")
                {
                    var sb = MakeCsv(data);
                    return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
                }
                else
                {
                    var result = data.Where(x => Math.Round(x.SisaPelunasan ?? 0, 2) > 0 && x.Tahap != 0).ToList();
                    return Ok(result);
                }
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

        [HttpGet("pivot/pembayaran")]
        public IActionResult GetPivotPembayaran([FromQuery] string token)
        {
            try
            {
                var user = contextes.FindUser(token);

                //DataImport
                var importedProject = new string[] {"PROJECT001",
                                                    "PROJECT002",
                                                    "PROJECT003C",
                                                    "PROJECT003",
                                                    "5E76F53E20C4177BBC72251E",
                                                    "PROJECT006T",
                                                    "PROJECT006L"};

                var bayars = contextpay.bayars.Query(b => b.invalid != true && importedProject.Contains(b.keyProject));

                var keyCreators = bayars.Select(b => b.keyCreator).Distinct().ToList();
                var securities = context.users.Query(x => keyCreators.Contains(x.key) && x.identifier == "importer").ToList();

                var bayarsBidangs = bayars.SelectMany(b => b.bidangs, (b, d) =>
                new
                {
                    Key = b.key,
                    KeyProject = b.keyProject,
                    KeyDesa = b.keyDesa,
                    Sec = securities?.FirstOrDefault(s => s.key == b.keyCreator),
                    Details = b.details,
                    Bidangs = d
                }).Where(b => b.Bidangs is not null && b.Sec is not null).ToList();

                var keys = bayarsBidangs.Select(x => x.Bidangs.keyPersil).ToList();
                var persils = contextplus.persils.Query(x => keys.Contains(x.key))
                    .Select(x => new
                    {
                        key = x.key,
                        IdBidang = x.IdBidang,
                        Group = x.basic.current.group,
                        LuasDibayar = x.basic.current.luasDibayar == null ? 0 : x.basic.current.luasDibayar,
                    }).ToList();

                var villages = contextpay.GetVillages();

                var dataImport = bayarsBidangs.Select(v => new RptPivotPembayaran
                {
                    Key = v.Key,
                    KeyPersil = persils.FirstOrDefault(p => p.key == v.Bidangs?.keyPersil)?.key,
                    IdBidang = persils.FirstOrDefault(p => p.key == v.Bidangs?.keyPersil)?.IdBidang,
                    Project = villages.FirstOrDefault(d => d.desa.key == v.KeyProject).project?.identity,
                    Desa = villages.FirstOrDefault(d => d.desa.key == v.KeyProject).desa?.identity,
                    Group = persils.FirstOrDefault(p => p.key == v.Bidangs?.keyPersil)?.Group,
                    Status = "1Diupload",
                    LuasDibayar = persils.FirstOrDefault(p => p.key == v.Bidangs?.keyPersil)?.LuasDibayar * 0.0001,
                    Details = v.Details
                }).ToList();

                //DataNonImport
                var importKeysColl = dataImport.Select(d => d.Key).Distinct().ToArray();
                string importKeys = string.Join(",", importKeysColl);
                importKeys = string.Join(",", importKeys.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                var dataNonImport = contextes.GetDocuments(new RptPivotPembayaran(), "bayars",
                   @"{$match:  {$and: [{'keyProject': {$in: ['PROJECT001', 'PROJECT002', 'PROJECT003C', 'PROJECT003', '5E76F53E20C4177BBC72251E', 'PROJECT006T', 'PROJECT006L']}},
                               {$and: [{'key': {$nin: [" + importKeys + "]}}, {'invalid': {$ne:true}} ]}]}}",
                    "{$unwind: '$bidangs'}",
                    "{$match:  {'bidangs': {$ne:[]}}}",
                    "{$lookup: {from: 'persils_v2',localField: 'bidangs.keyPersil',foreignField: 'key',as:'persil'}}",
                    "{$unwind: {path: '$persil',preserveNullAndEmptyArrays: true}}",
                    "{$lookup: {from: 'maps',let: { key: '$keyDesa'},pipeline:[{$unwind: '$villages'},{$match:  {$expr: {$eq:['$villages.key','$$key']}}},{$project: {key: '$villages.key', identity: '$villages.identity'} }], as:'desas'}}",
                    "{$lookup: {from: 'maps', localField: 'keyProject',foreignField: 'key',as:'projects'}}",
                    "{$match : {'projects' : {$ne: [] }}}",
                   @"{$project: {
                                _id:0,
                                Key: '$key',
                                KeyPersil: '$persil.key',
                                IdBidang: '$persil.IdBidang',
                                Project: {$ifNull : [{$arrayElemAt:['$projects.identity', -1]}, '']},
                                Desa: {$ifNull : [{$arrayElemAt:['$desas.identity', -1]}, '']},
                                Group: {$ifNull : ['$persil.basic.current.group', '']},
                                LuasDibayar: {$multiply: [0.0001, {$ifNull : ['$persil.basic.current.luasDibayar', 0]}]},
                                Details: '$details'
                    }}").ToList();

                IEnumerable<RptPivotPembayaranView> dataNonImportView = Enumerable.Empty<RptPivotPembayaranView>();
                IEnumerable<RptPivotPembayaranView> dataImportView = Enumerable.Empty<RptPivotPembayaranView>();

                var task1 = Task.Run(() =>
                {
                    //DataNonImport
                    var keyInstance = dataNonImport.Where(dni => dni.KeyPersil != null && dni.Details?.Length > 0)
                                               .SelectMany(dni => dni.Details, (a, b) => new { inst = b.instkey })
                                               .Select(a => a.inst).ToList();
                    var instkeys = string.Join(',', keyInstance.Select(k => $"{k}"));

#if GRPC
                    var instaces = ghost.GetMany(instkeys, "").GetAwaiter().GetResult().Cast<GraphMainInstance>().AsParallel().ToArray();
#else
                    var instaces = graphhost.GetMany(instkeys, "").Cast<GraphMainInstance>().AsParallel().ToArray();
#endif

                    dataNonImportView = dataNonImport.Where(dni => dni.KeyPersil != null && dni.Details?.Length > 0)
                                                     .Select(dni => dni.ToHelper(instaces));
                });

                var task2 = Task.Run(() =>
                {
                    //DataImport
                    var keyInstanceImprt = dataImport.Where(dni => dni.KeyPersil != null && dni.Details?.Length > 0)
                               .SelectMany(dni => dni.Details, (a, b) => new { inst = b.instkey })
                               .Select(a => a.inst).ToList();
                    var instkeysImport = string.Join(',', keyInstanceImprt.Select(k => $"{k}"));

#if GRPC
                    var instacesImport = ghost.GetMany(instkeysImport, "").GetAwaiter().GetResult().Cast<GraphMainInstance>().AsParallel().ToArray();
#else
                    var instacesImport = graphhost.GetMany(instkeysImport, "").Cast<GraphMainInstance>().AsParallel().ToArray();
#endif
                    
                    dataImportView = dataImport.Where(di => di.KeyPersil != null && di.Details?.Length > 0)
                                                       .Select(a => a.ToView(instacesImport));
                });

                //All Data
                Task.WaitAll(task1, task2);

                var allData = dataImportView.Union(dataNonImportView);

                allData = allData.Where(a => a.Status != "Exclude")
                                 .OrderBy(a => a.Project)
                                 .ThenBy(a => a.IdBidang)
                                 .ToList();

                return Ok(allData);
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

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("bidang/summary/deal")]
        public IActionResult GetDealSummary([FromQuery] string token, [FromQuery] string keyProject)
        {
            try
            {
                var user = contextes.FindUser(token);
                return ReportSummaryDeal(keyProject);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ContentResult { StatusCode = int.Parse(ex.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("bidang/summary/belumbebasnormatif")]
        public IActionResult GetSummaryBelumBebasNormatif([FromQuery] string token, [FromQuery] string keyProject, string keyCategory, string keySubCategory)
        {
            try
            {
                var user = contextes.FindUser(token);

                return ReportSummaryBelumBebasNormatif(keyProject, keyCategory, keySubCategory);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ContentResult { StatusCode = int.Parse(ex.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("bidang/summary/belumbebasnormatif-noname")]
        public IActionResult GetSummaryBelumBebasNoName([FromQuery] string token, [FromQuery] string keyProject, string keyPic)
        {

            try
            {
                var user = contextes.FindUser(token);
                return ReportBelumBebasNoName(keyProject, keyPic);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ContentResult { StatusCode = int.Parse(ex.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }


        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("belum-bebas/overlap")]
        public IActionResult GetReportBelumBebasOverlap([FromQuery] string token, string keyProject, string keyCategory)
        {
            try
            {
                var user = contextes.FindUser(token);

                var dataOv = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList().AsParallel();

                var idBidangColl = dataOv.Select(d => d.IdBidang).Distinct().ToArray();
                string idBidangs = string.Join(",", idBidangColl);
                idBidangs = string.Join(",", idBidangs.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                var dataPersil = context.GetDocuments(new
                {
                    idBidang = "",
                    keyPersil = "",
                    keyProject = "",
                    category = new string[0],
                    luasBintang = (double?)0
                },
                    "persils_v2",
                    "{$match: {$and : [ {'IdBidang': {$in : [ " + idBidangs + "] }}, {'basic.current.en_proses':4} ]}}",
                    @"{$project: {
                            _id:0,
                            keyPersil: '$key',
                            keyProject: '$basic.current.keyProject',
                            idBidang: '$IdBidang',
                            luasBintang: '$basic.current.luasDibayar',
                            category: {$ifNull: [{$arrayElemAt:['$categories.keyCategory', -1]},[]]}
                    }}"
                ).AsParallel();
                var projectAllowed = context.GetDocuments(new { keyProject = "", identity = "" }, "projects_include", "{$project: {_id:0, identity:1, keyProject:'$key'}}");

                if (!string.IsNullOrEmpty(keyProject))
                    dataPersil = dataPersil.Where(dp => dp.keyProject == keyProject);
                else
                    dataPersil = dataPersil.Where(dp => projectAllowed.Select(pa => pa.keyProject).Contains(dp.keyProject));

                var categories = context.GetCollections(new Category(), "categories_new", "{bebas:0}", "{_id:0}").ToList();

                List<BelumBebasOverlap> result = dataOv.Join(dataPersil, ov => ov.IdBidang, dp => dp.idBidang
                                                         , (o, p) => new BelumBebasOverlap()
                                                         {
                                                             idBidang = o.IdBidang,
                                                             keyPersil = p.keyPersil,
                                                             luas = (p.luasBintang - o.overlap.Sum(x => x.luas)) < 0 ? 0 : (p.luasBintang - o.overlap.Sum(x => x.luas)),
                                                             pic = GetCategory(categories, p.category, 4)?.shortDesc ?? "",
                                                             keyGroup = GetCategory(categories, p.category, 2)?.key ?? "",
                                                             group = GetCategory(categories, p.category, 2)?.shortDesc ?? "",
                                                             keyCategory = GetCategory(categories, p.category, 2)?.key ?? "",
                                                             category = GetCategory(categories, p.category, 1)?.shortDesc ?? "",
                                                             subCategory = GetCategory(categories, p.category, 6)?.shortDesc ?? "",
                                                             keySubCategory = GetCategory(categories, p.category, 6)?.key ?? ""
                                                         }).ToList();

                result = keyCategory == null ? result.Where(r => (r.category.ToLower() == "group besar" || r.category == "")).ToList()
                                               : keyCategory != "" ?
                                               result.Where(r => r.category.ToLower() == "group besar").ToList() :
                                               result.Where(r => r.category == "").ToList();

                var keyPersilColl = result.Select(r => r.keyPersil).Distinct().ToArray();
                string keyPersils = string.Join(",", keyPersilColl);
                keyPersils = string.Join(",", keyPersils.Split(',').Select(x => string.Format("'{0}'", x)).ToList());

                var bundleDoc = context.GetDocuments(new
                {
                    keyBundle = "",
                    keyDocType = "",
                    doclist = new BundledDoc(),
                }, "bundles",
                    "{$match: {'key': {$in: [" + keyPersils + "]}}}",
                    "{$unwind: '$doclist'}",
                    "{$match:  {'doclist.keyDocType': {$in: ['JDOK032', 'JDOK001', 'JDOK034', 'JDOK036']} }}",
                   @"{$project: {
                        _id:0,
                        keyBundle: '$key',
                        keyDocType: '$doclist.keyDocType',
                        doclist: '$doclist',
                   }}").ToList();

                var typeLuas = MetadataKey.Luas.ToString("g");

                var dataBundle = bundleDoc.Select(b => new
                {
                    keyBundle = b.keyBundle,
                    keyDocType = b.keyDocType,
                    entries = b?.doclist?.entries.Count() != 0 ?
                             b.doclist.entries?.LastOrDefault().Item?.FirstOrDefault().Value : null
                })
                                          .Select(b => new
                                          {
                                              keyBundle = b.keyBundle,
                                              keyDocType = b.keyDocType,
                                              dyn = (b.entries == null) ? null : b.entries.props.TryGetValue(typeLuas, out Dynamic val) ? val : null
                                          })
                                          .Select(b => new
                                          {
                                              keyBundle = b.keyBundle,
                                              keyDocType = b.keyDocType,
                                              luas = 0.0001 * Convert.ToDouble(b.dyn?.Value ?? 0)
                                          }).ToList();

                var fuMarketing = GetLatestFollowUpMarketingData();

                List<BelumBebasOverlap> lastResult =
                    result.Select(r => new BelumBebasOverlap()
                    {
                        idBidang = r.idBidang,
                        keyPersil = r.keyPersil,
                        luas = r.luas,
                        pic = r.pic,
                        keyGroup = r.keyGroup,
                        group = r.group,
                        keyCategory = r.keyCategory,
                        category = string.IsNullOrEmpty(r.category) ? "BELUM CLAIM" : r.category,
                        keySubCategory = r.keySubCategory,
                        subCategory = OrderSubCategory(r.subCategory),
                        keterangan = fuMarketing.FirstOrDefault(f => f.keyPersil == r.keyPersil)?.note ?? "",
                        alasHak = dataBundle.FirstOrDefault(db => db.keyBundle == r.keyPersil
                                                               && db.keyDocType == "JDOK001"
                                                            )?.luas ?? 0,
                        nib = dataBundle.FirstOrDefault(db => db.keyBundle == r.keyPersil
                                                           && db.keyDocType == "JDOK032"
                                                            )?.luas ?? 0,
                        pengumuman = dataBundle.FirstOrDefault(db => db.keyBundle == r.keyPersil
                                                                  && db.keyDocType == "JDOK034"
                                                              )?.luas ?? 0,
                        shm = dataBundle.FirstOrDefault(db => db.keyBundle == r.keyPersil
                                                           && db.keyDocType == "JDOK036"
                                                       )?.luas ?? 0
                    }).ToList();

                lastResult = lastResult.GroupBy(x => (x.keyCategory, x.category, x.keySubCategory, x.subCategory,
                                                      x.group, x.keterangan, x.pic))
                                       .OrderBy(l => l.Key.category)
                                       .ThenBy(l => l.Key.subCategory)
                                       .Select((r, i) => new BelumBebasOverlap()
                                       {
                                           index = i++,
                                           keyCategory = r.Key.keyCategory,
                                           category = r.Key.category,
                                           keySubCategory = r.Key.keySubCategory,
                                           subCategory = r.Key.subCategory,
                                           keterangan = r.Key.keterangan,
                                           pic = r.Key.pic,
                                           group = r.Key.group,
                                           alasHak = r.Sum(x => x.alasHak) * 0.0001,
                                           nib = r.Sum(x => x.nib) * 0.0001,
                                           pengumuman = r.Sum(x => x.pengumuman) * 0.0001,
                                           shm = r.Sum(x => x.shm) * 0.0001,
                                           luas = r.Sum(x => x.luas) * 0.0001
                                       }).ToList();

                return Ok(lastResult);
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
        /// Get Data For Report Belum Bebas Group Kecil (mudah / Sulit)
        /// </summary>
        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("belum-bebas/gk/mudah-sulit")]
        public IActionResult GetBebasNormGroupKecil([FromQuery] string token, string keyProject, string keyCat, string keySubCat)
        {
            try
            {
                var user = context.FindUser(token);

                string baseUrl = this.Request.Host.Host;

                List<BelumBebasNormatif> belumBebas = GetBelumBebasNormatif(keyProject, keyCat, keySubCat).ToList();

                var followUpMkt = GetLatestFollowUpMarketingData().ToList();
                var groupKecil = belumBebas.GroupJoin(followUpMkt, bb => bb.KeyPersil, fu => fu.keyPersil,
                                                      (b, f) => new BelumBebasGroupKecilMSPerBidang()
                                                      {
                                                          _t = "",
                                                          keyPIC = b.KeyPIC,
                                                          PIC = b.PIC,
                                                          KeySubCategory = b.KeySubCategory,
                                                          SubCategory = b.SubCategory,
                                                          IdBidang = b.IdBidang,
                                                          Project = b.Project,
                                                          Desa = b.Desa,
                                                          NomorPeta = b.NoPeta,
                                                          LuasSurat = b.LuasM2,
                                                          LuasSuratHektar = b.Luas,
                                                          NamaGroup = b.Group,
                                                          Kategory = b.SubCategory,
                                                          Sales = f.FirstOrDefault()?.keyWorker,
                                                          HasilFollowUp = SetStatusMarketing(f.FirstOrDefault()),
                                                      });

                var GroupKecilSummary = groupKecil.Select(gk => new
                {
                    hasilFollowUp = gk.HasilFollowUp,
                    keySubCat = gk.KeySubCategory,
                    subCat = gk.SubCategory,
                    idBidang = gk.IdBidang,
                    LuasSuratHektar = gk.LuasSuratHektar,
                    group = gk.NamaGroup,
                    PIC = gk.PIC,
                    KeyPIC = gk.keyPIC
                }).GroupBy(gb => (gb.keySubCat, gb.subCat, gb.KeyPIC, gb.PIC, gb.hasilFollowUp))
                  .Select(gb => new BelumBebasGroupKecilMSSummary()
                  {
                      BaseUrl = baseUrl,
                      KeySubCategory = gb.Key.keySubCat,
                      SubCategory = gb.Key.subCat,
                      KeyPIC = gb.Key.KeyPIC,
                      PIC = gb.Key.PIC,
                      BukaHarga = gb.Key.hasilFollowUp,
                      JumlahOrang = gb.GroupBy(x => x.group).Count(),
                      JumlahBidang = gb.Select(x => x.idBidang).Count(),
                      JumlahLuas = gb.Select(x => x.LuasSuratHektar).Sum(),
                      IsBukaHarga = gb.Key.hasilFollowUp.Contains(".") ? true : false
                  }).ToList();

                return Ok(GroupKecilSummary);
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
        /// Get Data For Report Belum Bebas Group Kecil (mudah / Sulit) Per PIC
        /// </summary>
        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("belum-bebas/gk/perbidang-mudah-sulit")]
        public IActionResult GetBelumbebasNormGroupKecilPerBidang([FromQuery] string token, string keyProject, string keySubCat, string keyPIC)
        {
            try
            {
                var user = context.FindUser(token);

                List<BelumBebasNormatif> belumBebas = GetBelumBebasNormatif(keyProject, "60863014934CF7D90C8G3M0002", keySubCat).ToList();
                var _tPersils = context.GetDocuments(new { _t = "", keyPersil = "" }, "persils_v2", "{$project: {_id:0, _t:1, keyPersil:'$key'}}");
                var dataWorker = context.db.GetCollection<Worker>("workers").Find("{_t:'worker',invalid:{$ne:true}}").ToList();

                var followUpMkt = GetLatestFollowUpMarketingData().ToList();
                var groupKecil = belumBebas.GroupJoin(followUpMkt, bb => bb.KeyPersil, fu => fu.keyPersil,
                                                      (b, f) => new BelumBebasGroupKecilMSPerBidang()
                                                      {
                                                          _t = _tPersils.FirstOrDefault(x => x.keyPersil == b.KeyPersil)?._t ?? "",
                                                          keyPIC = b.KeyPIC,
                                                          PIC = b.PIC,
                                                          KeySubCategory = b.KeySubCategory,
                                                          SubCategory = b.SubCategory,
                                                          IdBidang = b.IdBidang,
                                                          Project = b.Project,
                                                          Desa = b.Desa,
                                                          NomorPeta = b.NoPeta,
                                                          LuasSurat = b.LuasM2,
                                                          LuasSuratHektar = b.Luas,
                                                          NamaGroup = b.Group,
                                                          NamaPemilik = b.Pemilik,
                                                          Kategory = b.SubCategory,
                                                          Sales = dataWorker.FirstOrDefault(dw => dw.key == f.FirstOrDefault()?.keyWorker)?.ShortName
                                                                  ?? b.Sales,
                                                          HasilFollowUp = SetStatusMarketing(f.FirstOrDefault()),
                                                      });

                if (!string.IsNullOrEmpty(keyPIC))
                    groupKecil = groupKecil.Where(gk => gk.keyPIC == keyPIC);

                return Ok(groupKecil);
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

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("belum-bebas/groupkecil/sudah-ketemu")]
        public IActionResult GetReportBlmBebasGkSudahKetemu([FromQuery] string token, string kp)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var projects = new List<string>();

                if (!string.IsNullOrEmpty(kp))
                    projects.Add(kp);
                else
                    projects = context.GetCollections(new { key = "" }, "projects_include", "{}", "{_id:0, key:1}").ToList().Select(x => x.key).ToList();

                var categories = context.GetCollections(new Category(), "categories_new", "{bebas:0}", "{_id:0}").ToList().AsParallel();
                var mudahSulitCat = categories.Where(x => (x.desc == "MUDAH" || x.desc == "SULIT") && x.segment == 6).Select(x => x.key);

                (var groupKecilCat, var sudahKetemuCat, var mudahCat, var sulitCat, var dealCat)
                    =
                (categories.FirstOrDefault(x => x.desc == "GROUP KECIL" && x.segment == 1)?.key,
                categories.FirstOrDefault(x => x.desc == "SUDAH ADA NAMA" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "MUDAH" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "SULIT" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "DEAL" && x.segment == 6)?.key);

                var allPersils = contextes.persils.Query(x => x.en_state == StatusBidang.belumbebas && x.basic != null && x.basic.current != null && x.deal == null)
                   .Where(x => x.IdBidang != null && x.basic.current.en_proses != null
                   && projects.Contains(x.basic.current.keyProject)
                   && x.basic.current.en_proses == JenisProses.standar
                   && x.categories.LastOrDefault()?.keyCategory.Skip(0).Take(1).FirstOrDefault() == groupKecilCat
                   && x.categories.LastOrDefault()?.keyCategory.Skip(5).Take(1).FirstOrDefault() != dealCat)
                   .Select(x => new PersilGK
                   {
                       IdBidang = x.IdBidang,
                       deal = x.deal,
                       keyProject = x.basic.current.keyProject,
                       state = x.en_state.GetValueOrDefault(),
                       kPIC = x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault(),
                       proses = x.basic.current.en_proses,
                       luasSurat = x.basic.current.luasSurat ?? 0,
                       group = x.basic.current.group ?? "",
                       kategori = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                       ketemu = x.categories.SelectMany(x => x.keyCategory, (x, y) => new cate { tgl = x.tanggal, cat = y })
                                            .Where(x => x.cat == sudahKetemuCat).OrderBy(x => x.tgl).FirstOrDefault(),
                       mudahSulit = x.categories.SelectMany(x => x.keyCategory, (x, y) => new cate { tgl = x.tanggal, cat = y })
                                            .Where(x => mudahSulitCat.Contains(x.cat)).OrderBy(x => x.tgl).FirstOrDefault(),
                       //mudah = x.categories.SelectMany(x => x.keyCategory, (x, y) => new { tgl = x.tanggal, cat = y })
                       //                     .Where(x => x.cat == mudahCat).OrderByDescending(x => x.tgl).FirstOrDefault(),
                       //sulit = x.categories.SelectMany(x => x.keyCategory, (x, y) => new { tgl = x.tanggal, cat = y })
                       //                     .Where(x => x.cat == sulitCat).OrderByDescending(x => x.tgl).FirstOrDefault(),
                       categories = x.categories
                   }).ToArray();

                var data = allPersils
                    .GroupBy(x => (x.kategori, x.kPIC), (y, z) =>
                                                    new
                                                    {
                                                        Segment1 = "GROUP KECIL",
                                                        Segment6 = "SUDAH ADA NAMA",
                                                        keyPIC = y.kPIC,
                                                        Kategory = y.kategori == null ? "LAIN - LAIN" : y.kategori,
                                                        Luas = z.Where(x => x.ketemu != null).Sum(x => x.luasSurat) / 10000,

                                                        LuasPencapaian = z.Where(x => x.ketemu != null && x.mudahSulit != null).Sum(x => x.luasSurat) / 10000,

                                                        Persentase = z.Where(x => x.ketemu != null).Sum(x => x.luasSurat) == 0 ? 0
                                                                                          : ((z.Where(x => x.ketemu != null && x.mudahSulit != null).Sum(x => x.luasSurat)) /
                                                                                          (z.Where(x => x.ketemu != null).Sum(x => x.luasSurat))) * 100,
                                                        SisaTarget = ((z.Where(x => x.ketemu != null).Sum(x => x.luasSurat)) -
                                                                        (z.Where(x => x.ketemu != null && x.mudahSulit != null).Sum(x => x.luasSurat))) < 0 ? 0 : ((z.Where(x => x.ketemu != null).Sum(x => x.luasSurat)) -
                                                                        (z.Where(x => x.ketemu != null && x.mudahSulit != null).Sum(x => x.luasSurat))) / 10000
                                                    }
                                                    ).ToArray();

                return new JsonResult(data.ToArray());
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [NeedToken("MKT_FLW_FULL,PAY_LAND_FULL,DATA_FULL,PASCA_FULL,CATEGORY_HIST,REP_VIEW,FISHBONE_VIEW")]
        [HttpGet("belum-bebas/groupkecil/sudah-ketemu/pic")]
        public IActionResult GetReportBlmBebasGkSudahKetemuPIC([FromQuery] string token, string kp, string kPIC)
        {
            try
            {
                var user = contextplus.FindUser(token);
                var projects = new List<string>();

                if (!string.IsNullOrEmpty(kp))
                    projects.Add(kp);
                else
                    projects = context.GetCollections(new { key = "" }, "projects_include", "{}", "{_id:0, key:1}").ToList().Select(x => x.key).ToList();

                var categories = context.GetCollections(new Category(), "categories_new", "{bebas:0}", "{_id:0}").ToList().AsParallel();
                var project = context.GetCollections(new { key = "", identity = "" }, "projects_include", "{}", "{_id:0, key:1, identity:1}").ToList().AsParallel();

                (var groupKecilCat, var sudahKetemuCat, var mudahCat, var sulitCat, var dealCat)
                    =
                (categories.FirstOrDefault(x => x.desc == "GROUP KECIL" && x.segment == 1)?.key,
                categories.FirstOrDefault(x => x.desc == "SUDAH ADA NAMA" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "MUDAH" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "SULIT" && x.segment == 6)?.key,
                    categories.FirstOrDefault(x => x.desc == "DEAL" && x.segment == 6)?.key);

                var mudahSulitCat = categories.Where(x => (x.desc == "MUDAH" || x.desc == "SULIT") && x.segment == 6).Select(x => x.key);

                var allPersils = contextes.persils.Query(x => x.en_state == StatusBidang.belumbebas && x.basic != null && x.basic.current != null && x.deal == null)
                   .Where(x => x.IdBidang != null && x.basic.current.en_proses != null
                   && projects.Contains(x.basic.current.keyProject)
                   && x.basic.current.en_proses == JenisProses.standar
                   && x.categories.LastOrDefault()?.keyCategory.Skip(0).Take(1).FirstOrDefault() == groupKecilCat
                   && x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault() == kPIC
                   && x.categories.LastOrDefault()?.keyCategory.Skip(5).Take(1).FirstOrDefault() != dealCat)
                   .Select(x => new PersilGK
                   {
                       IdBidang = x.IdBidang,
                       deal = x.deal,
                       keyProject = x.basic.current.keyProject,
                       state = x.en_state.GetValueOrDefault(),
                       proses = x.basic.current.en_proses,
                       luasSurat = x.basic.current.luasSurat ?? 0,
                       group = x.basic.current.group ?? "",
                       kategori = categories.Where(y => y.key == (x.categories.LastOrDefault()?.keyCategory.Skip(3).Take(1).FirstOrDefault())).FirstOrDefault()?.desc,
                       ketemu = x.categories.SelectMany(x => x.keyCategory, (x, y) => new cate { tgl = x.tanggal, cat = y })
                                            .Where(x => x.cat == sudahKetemuCat).OrderBy(x => x.tgl).FirstOrDefault(),
                       mudahSulit = x.categories.SelectMany(x => x.keyCategory, (x, y) => new cate { tgl = x.tanggal, cat = y })
                                            .Where(x => mudahSulitCat.Contains(x.cat)).OrderBy(x => x.tgl).FirstOrDefault(),
                       categories = x.categories
                   }).Where(x => x.ketemu != null && x.mudahSulit != null).ToArray();

                var ndata = allPersils.Select(x => x.setDays(x.ketemu == null ? null : x.ketemu.tgl, x.mudahSulit != null ? x.mudahSulit.tgl : null));

                var xdata = ndata.GroupBy(x => x.group).Select(z => new { g = z.Key, data = z.ToList() });
                var first = true;

                var data = new List<GroupKecilSudahKetemu>();
                var PIC = categories.FirstOrDefault(x => x.key == kPIC && x.segment == 4)?.desc;

                foreach (var item in xdata)
                {
                    if (first)
                    {
                        for (int i = 1; i <= 14; i++)
                        {
                            data.Add(new GroupKecilSudahKetemu
                            {
                                Nama = item.g,
                                Project = project.FirstOrDefault(x => x.key == item.data.FirstOrDefault()?.keyProject)?.identity,
                                progress = i,
                                Total = 0,
                                PIC = PIC
                            });
                        }
                    }
                    foreach (var dt in item.data)
                    {
                        bool updated = false;
                        if (first)
                        {
                            var dts = data.Where(x => x.Nama == dt.group && x.progress == dt.day && x.Project == project.FirstOrDefault(x => x.key == dt.keyProject)?.identity).FirstOrDefault();

                            if (dts != null)
                            {
                                dts.Project = project.FirstOrDefault(x => x.key == dt.keyProject)?.identity;
                                dts.Total += (dt.luasSurat / 10000);
                                updated = true;
                            }
                        }

                        if (!updated)
                        {
                            var tw = new GroupKecilSudahKetemu
                            {
                                Nama = dt.group,
                                Project = project.FirstOrDefault(x => x.key == dt.keyProject)?.identity,
                                Total = (dt.luasSurat / 10000),
                                progress = dt.day,
                                PIC = PIC
                            };

                            data.Add(tw);
                        }
                    }

                    first = false;
                }
                return new JsonResult(data);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpGet("spk")]
        public IActionResult CreateSPK([FromQuery] string keyPersil)
        {
            try
            {
                var persil = GetPersil(keyPersil);
                var villages = contextpay.GetVillages().FirstOrDefault(x => x.desa.key == persil.basic?.current?.keyDesa);
                var notaris = context.db.GetCollection<Notaris>("masterdatas").Find("{_t:'notaris',invalid:{$ne:true}}").ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var ptsks = contextpay.ptsk.Query(x => x.invalid != true).ToList()
                                            .Select(p => new cmnItem { key = p.key, name = p.identifier }).ToList();

                var note = new List<String>();

                if (persil.basic?.current?.en_proses == JenisProses.overlap)
                {
                    var hibahOverlapLuas = contextpay.GetCollections(new PersilOverlap(), "hibahOverlapLuas", $"<'overlap.IdBidang' :'{persil.IdBidang}' >".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList().AsParallel();

                    foreach (var item in hibahOverlapLuas)
                    {
                        var overlap = item.overlap.Where(x => x.IdBidang == persil.IdBidang).FirstOrDefault();
                        var hibah = GetPersilByIdBidang(item.IdBidang);

                        var typename = MetadataKey.Nomor.ToString("g");

                        var bundles = contextplus.GetCollections(new MainBundle(), "bundles", $"<key:'{hibah.key}'>".Replace("<", "{").Replace(">", "}"), "{_id:0}").ToList()
                            .Select(x => new
                            {
                                keyPersil = x.key,
                                PartLuas = x.doclist.FirstOrDefault(b => b.keyDocType == "JDOK032")?
                                .entries.LastOrDefault()?
                                .Item.FirstOrDefault().Value
                            }).Select(x => new
                            {
                                keyPersil = x.keyPersil,
                                DynLuas = x.PartLuas == null ? null : x.PartLuas.props.TryGetValue(typename, out Dynamic dynamic) ? dynamic : null
                            }).Select(x => new
                            {
                                keyPersil = x.keyPersil,
                                nomor = x.DynLuas?.Value != null ? Convert.ToString(x.DynLuas?.Value) : string.Empty
                            }).FirstOrDefault();


                        var nomor = string.IsNullOrEmpty(bundles?.nomor) ? "-" : bundles?.nomor;
                        note.Add($"overlap nib/idbidang {nomor}/{item.IdBidang} LO {overlap.luas}");
                    }

                }

                var result = new
                {
                    Group = persil.basic?.current?.group,
                    Nama = persil.basic?.current?.surat?.nama ?? persil.basic?.current?.pemilik,
                    AlasHak = persil.basic?.current?.surat?.nomor,
                    Desa = villages.desa.identity,
                    LuasSurat = persil.basic?.current?.luasSurat ?? 0,
                    LuasUkur = persil.basic?.current?.luasInternal ?? 0,
                    IDBidang = persil.IdBidang,
                    NoPeta = persil.basic?.current?.noPeta,
                    Notaris = notaris.FirstOrDefault(x => x.key == (persil.PraNotaris ?? ""))?.name,
                    PTSK = ptsks.FirstOrDefault(x => x.key == persil.basic?.current?.keyPTSK)?.name,
                    Project = villages.project.identity,

                    Notes = note.ToArray()
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpGet("hutang/pelunasan")]
        public IActionResult GetReportHutangPelunasan([FromQuery] string token, bool isCsv = false)
        {
            try
            {
                var user = contextpay.FindUser(token);
                var bayar = contextes.GetDocuments(new Bayar(), "bayars", "{$match : {'bidangs' : {$ne:[]}}}").ToList();
                var datas = contextes.GetDocuments(new RptDetailLunasBidang(), "bayars",
                             "{$unwind: '$bidangs'}",
                             "{$match : {'bidangs' : {$ne:[]}}}",
                             @"{$project: {      _id  :0
                                            ,tKey :'$key'
                                            ,keyPersil: '$bidangs.keyPersil'
                                            ,keyDesa : '$keyDesa'
                                            ,keyProject : '$keyProject'
                                            }
                              }",
                             "{$lookup:{ from:'persils_v2', localField: 'keyPersil', foreignField: 'key', as : 'persils'}}",
                             "{$unwind:{ path:'$persils', preserveNullAndEmptyArrays: true}}",
                             @"{$project : {
                                             tKey: '$tKey'
                                            ,keyPersil: '$keyPersil'
                                            ,IdBidang : {$ifNull: ['$persils.IdBidang', '']}
                                            ,keyDesa: '$keyDesa'
                                            ,luasFix: {$ifNull:['$persils.luasFix', true]}
                                            ,luasNibTemp : {$ifNull : ['$persils.basic.current.luasNIBTemp',0]}
                                            ,luasDibayar : {$ifNull : ['$persils.basic.current.luasDibayar',0]}
                                            ,luasPelunasan : {$ifNull : ['$persils.luasPelunasan', 0]}
                                            ,luasSurat : {$ifNull : ['$persils.basic.current.luasSurat', 0]}
                                            ,satuan : {$ifNull : ['$persils.basic.current.satuan', 0]}
                                            ,satuanAkte : {$ifNull:['$persils.basic.current.satuanAkte',0]}
                                            ,pemilik : {$ifNull : ['$persils.basic.current.pemilik', '']}
                                            ,alasHak : {$ifNull : ['$persils.basic.current.surat.nomor', '']}
                                            ,mandor: {$ifNull : ['$persils.mandor',0]}
                                            ,pembatalanNIB: {$ifNull : ['$persils.pembatalanNIB',0]}
                                            ,BiayaBN: {$ifNull : ['$persils.BiayaBN',0]}
                                            ,gantiBlanko: {$ifNull : ['$persils.gantiBlanko',0]}
                                            ,kompensasi: {$ifNull : ['$persils.kompensasi',0]}
                                            ,pajakLama: {$ifNull : ['$persils.pajakLama',0]}
                                            ,pajakWaris: {$ifNull : ['$persils.pajakWaris',0]}
                                            ,tunggakanPBB: {$ifNull : ['$persils.tunggakanPBB',0]}
                                            ,ValidasiPPH : {$ifNull : ['$persils.ValidasiPPH', false]}
                                            ,ValidasiPPHValue : {$ifNull : ['$persils.ValidasiPPHValue', 0]}
                                            ,biayaLainnya : '$persils.biayalainnya'
                                            ,pph21 : {$ifNull : ['$persils.pph21', false]}
                                           }
                               }").ToList();

                var villages = contextes.GetVillages();

                datas = datas.Join(villages, d => d.keyDesa, p => p.desa.key, (d, p) => d.setLocation(p.project.identity, p.desa.identity)).ToList();

                var bundles = contextes.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
                    "{$match: {'_t' : 'mainBundle'}}",
                    "{$unwind: '$doclist'}",
                    "{$match : {$and : [{'doclist.keyDocType':'JDOK032'}, {'doclist.entries' : {$ne : []}}]}}",
                    @"{$project : {
                                      _id: 0
                                    , key: 1
                                    , doclist: 1
                    }}").ToList();

                var type = MetadataKey.Luas.ToString("g");
                var cleanbundle = bundles.Select(x => new { key = x.key, entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value })
                    .Select(x => new { key = x.key, dyn = x.entries.props.TryGetValue(type, out Dynamic val) ? val : null })
                    .Select(x => new { key = x.key, luasNIB = Convert.ToDouble(x.dyn?.Value ?? 0) }).ToList();

                var data = new List<RptDetailLunasBidangView>();
                data = datas.Select(a => a.toView(bayar.Where(b => b.key == a.tKey).FirstOrDefault(), (cleanbundle.Select(x => (x.key, x.luasNIB)).ToList()))).ToList();

                var dataPraDeals = contextpay.GetDocuments(new RptDetailLunasBidangView(), "hutang_pelunasan_pradeals_view", "{$project : {_id : 0}}");
                data = dataPraDeals.Union(data).ToList();
                data = data.Where(x => Math.Round(x.SisaPelunasan ?? 0, 2) > 0).ToList();
                if (isCsv)
                {
                    var sb = MakeCsv(data);
                    return new ContentResult { Content = sb.ToString(), ContentType = "text/csv" };
                }
                else
                {
                    return Ok(data);
                }
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
        /// Memo 1 Tanah / Pembayaran
        /// </summary>
        [HttpGet("memo-tanah")]
        public IActionResult GetReportMemoTanah([FromQuery] string token, string tKey, string bKey)
        {
            try
            {
                var user = context.FindUser(token);
                var time = DateTime.Now.ToLocalTime();
                List<string> bKeys = bKey.Trim().Split(",").ToList();
                string bayarKeys = string.Join(", ", bKeys.Select(b => $"'{b}'").ToList());
                tKey = tKey.Split(",").FirstOrDefault();
                var host = HostServicesHelper.GetBayarHost(services);
                if (host.GetBayar(tKey) is not Bayar byr)
                    return new UnprocessableEntityObjectResult("Tahap tidak tersedia");

                var jenisbyr = byr.details.Where(det => det.invalid != true && bKeys.Contains(det.key)).Select(a => a.jenisBayar).ToList();

                if (jenisbyr.Count() == 0)
                    return new UnprocessableEntityObjectResult("Pembayaran dipilih telah dibatalkan !");

                var details = GetBayarDtlByJenisBayar(jenisbyr, bKeys, byr);
                var alls = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList();
                var keyPersilBayar = details.Where(det => bKeys.Contains(det.key) && det.invalid != true)
                                            .SelectMany(det => det.subdetails, (x, y) => new { keyPersil = y.keyPersil })
                                            .Select(x => x.keyPersil).Distinct().ToList();
                var static_collection = context.GetCollections(new { memoSigns = "" }, "static_collections", "{_t: 'Memo1Pembayaran'}", "{_id:0, _t:0}").FirstOrDefault();
                var workers = context.GetCollections(new Worker(), "workers", "{}", "{_id:0 }").ToList();

                List<RptMemoTanah> dataBidang = new List<RptMemoTanah>();
                var data = context.GetDocuments(new RptMemoTanah(), "bayars",
                        "{$match:  {'key': '" + tKey + "' }}",
                        "{$unwind: '$bidangs'}",
                        "{$lookup: {from:'persils_v2', localField: 'bidangs.keyPersil', foreignField: 'key', as: 'persil'}}",
                        "{$unwind: {path:'$persil', preserveNullAndEmptyArrays: true}}",
                        @"{$lookup:{from: 'maps',
                                    let: {key: '$keyDesa'}, 
                                    pipeline: [{$unwind: '$villages'},
                                                {$match: {$expr: {$eq: ['$villages.key','$$key']} } }], 
                                    as:'desas'}
                                    }",
                       "{$unwind: {path: '$desas', preserveNullAndEmptyArrays: true}}",
                        @"{$lookup: {
                                    from: 'masterdatas', 
                                    let: { keyPTSK: '$persil.basic.current.keyPTSK'}, 
                                    pipeline:[
                                        {$match: {$and: [{$expr: {$eq: ['$key','$$keyPTSK']} }, {$expr: {$eq: ['$_t','ptsk']} } ]} },
                                        {$project: { _id: 0, identifier: 1, code:1} }
                                    ],
                                    as: 'ptsk'}
                       }",
                       "{$unwind: {path: '$ptsk', preserveNullAndEmptyArrays: true}}",
                        @"{$lookup: { from: 'masterdatas', 
                                let: {keyNotaris: '$persil.PraNotaris'}, 
                                pipeline: [   
                                    {$match: {$and : [{$expr: {$eq:['$key','$$keyNotaris']}}, {$expr: {$eq:['$_t','notaris']}} ]}},
                                    {$project : {_id : 0, identifier : 1}}
                                ],
                                as: 'notaris'}}",
                       "{$unwind: { path: '$notaris', preserveNullAndEmptyArrays: true}}",

                      @"{$lookup: {from: 'praDeals', 
                                    let: {keyPersil: '$persil.key'},
                                    pipeline: [
                                        {$unwind: '$details'},
                                        {$match: {$expr: {$eq: ['$details.keyPersil','$$keyPersil']}}},
                                        {$project: {
                                            _id:0,
                                            keyManager: '$manager',
                                            keyMediator: '$mediator',
                                            keySales: '$sales',
                                        }}
                                ],
                                as: 'praDeal'
                        }}",

                       "{$unwind: { path: '$praDeal', preserveNullAndEmptyArrays: true}}",

                      @"{$project:{
                            _id: 0,
                            keyBayar: '$key',
                            keyPersil: '$persil.key',
                            Tahap: '$nomorTahap',
                            IdBidang: '$persil.IdBidang',
                            Alias: {$ifNull: ['$persil.basic.current.alias', '']},
                            Desa: '$desas.villages.identity',
                            Project: '$desas.identity',
                            PTSK: {$ifNull: ['$ptsk.identifier', '']},
                            PTSKCode: {$ifNull: ['$ptsk.code', '$ptsk.identifier']},
                            Pemilik: '$persil.basic.current.pemilik',
                            SuratAsal: '$persil.basic.current.surat.nomor',
                            en_jenis: '$persil.basic.current.en_jenis',
                            LuasSurat: '$persil.basic.current.luasSurat',
                            LuasDibayar: '$persil.basic.current.luasDibayar',
                            LuasInternal: '$persil.basic.current.luasInternal',
                            LuasPBT: null,
                            NoPeta: '$persil.basic.current.NoPeta',
                            Harga: '$persil.basic.current.satuan',
                            SatuanAkte: '$persil.basic.current.satuanAkte',
                            Group: '$persil.basic.current.Group',
                            Notaris: '$notaris.identifier',
                            TotalBeli:{$cond:{if: {$eq: ['$persil.basic.current.luasDibayar', null]},
                                              then: {$multiply: ['$persil.basic.current.satuan', '$persil.basic.current.luasSurat']},
                                              else: {$multiply: ['$persil.basic.current.satuan', '$persil.basic.current.luasDibayar']}
                                             }
                                        },
                            keyManager: '$praDeal.keyManager',
                            keySales: '$praDeal.keySales',
                            keyMediator: '$praDeal.keyMediator'
                        }}").ToList();
                string keyPersils = string.Join(",", data.Select(dB => $"'{dB.keyPersil}'").ToList());
                var bundles = contextes.GetDocuments(new { key = "", doclist = new documents.BundledDoc() }, "bundles",
                        "{$match: {'_t' : 'mainBundle'}}",
                        "{$match: {'key': {$in: [" + keyPersils + "]}}}",
                        "{$unwind: '$doclist'}",
                        "{$match : {$and : [{'doclist.keyDocType':'JDOK032'}, {'doclist.entries' : {$ne : []}}]}}",
                        @"{$project : {
                                                          _id: 0
                                                        , key: 1
                                                        , doclist: 1
                                        }}").ToList();

                var type = MetadataKey.Luas.ToString("g");
                var cleanbundle = bundles.Select(x => new { key = x.key, entries = x.doclist.entries.LastOrDefault().Item.FirstOrDefault().Value })
                    .Select(x => new { key = x.key, dyn = x.entries.props.TryGetValue(type, out Dynamic val) ? val : null })
                    .Select(x => new { key = x.key, luasNIB = Convert.ToDouble(x.dyn?.Value ?? 0) }).ToList();

                var pembayaran = GetHistoryPembayaran(tKey, bayarKeys);
                var villages = contextplus.GetVillages();
                var ptsks = context.GetCollections(new PTSK(), "masterdatas", "{_t:'ptsk'}", "{_id:0}").ToList();

                var allHibah = contextes.GetCollections(new PersilOverlap(), "hibahOverlapLuas", "{}", "{_id:0}").ToList();

                foreach (var bidang in data)
                {
                    var overlap = (alls.SelectMany(x => x.overlap, (x, y) => new { bidangBintang = x.IdBidang, bidangOV = y.IdBidang, luasOV = y.luas })
                       .Where(x => x.bidangOV == bidang.IdBidang)
                       .Select(x => new
                       {
                           bidangOV = x.bidangOV
                                       ,
                           bidangBintang = x.bidangBintang
                                       ,
                           luasOV = x.luasOV
                       })
                   ).ToList();

                    if (overlap.Select(ov => ov.bidangBintang).Any() && jenisbyr.Contains(JenisBayar.Lunas))
                    {
                        foreach (var ov in overlap)
                        {
                            RptMemoTanah dtlBidangOV = bidang;
                            var persilBintang = GetPersilByIdBidang(ov.bidangBintang);
                            var nomor = PersilHelper.GetNomorPBT(persilBintang);
                            dtlBidangOV.Keterangan = nomor.NIB;
                            dtlBidangOV.Nama = persilBintang.basic.current.pemilik ?? persilBintang.basic.current.surat.nama;
                            dtlBidangOV.LuasOverlap = ov.luasOV;
                            dataBidang.Add(dtlBidangOV);
                        }
                    }
                    else
                    {
                        dataBidang.Add(bidang);
                    }
                };

                List<string> keyPersilBayared = pembayaran.Where(p => p.selected == true).Select(p => p.keyPersil).ToList();
                string transaksiSelected = pembayaran.FirstOrDefault(p => p.selected == true).jenisBayar;
                string group = dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && !string.IsNullOrEmpty(dB?.Group))?.Group;
                string desa = string.IsNullOrEmpty(dataBidang.FirstOrDefault()?.Desa) ? "" : $"- {dataBidang.FirstOrDefault()?.Desa}";
                string project = string.IsNullOrEmpty(dataBidang.FirstOrDefault()?.Project) ? "" : $"- {dataBidang.FirstOrDefault()?.Project}";
                string ptsk = string.IsNullOrEmpty(dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && !string.IsNullOrEmpty(dB?.PTSK))?.PTSK) ? "" : $"- {dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && !string.IsNullOrEmpty(dB?.PTSK))?.PTSK}";
                string ptskCode = string.IsNullOrEmpty(dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && !string.IsNullOrEmpty(dB?.PTSKCode))?.PTSKCode) ? "" : $"- {dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && !string.IsNullOrEmpty(dB?.PTSKCode))?.PTSKCode}";
                string defaultMemo = $"/{transaksiSelected}{ptskCode}/{GetRomanMonth(time.Month)}/{time.Year.ToString()}";
                string noMemo = pembayaran.FirstOrDefault(p => p.selected == true)?.noMemo;
                string jenisAlasHak = string.Join(" &", dataBidang.Select(dB => dB.en_jenis.ToString().ToUpper()).Distinct().ToList());
                string romanTahap = IntToRoman(dataBidang.FirstOrDefault().Tahap);

                MemoTanahView view = new();
                view.Title = $"TRANSAKSI {transaksiSelected} GROUP {group} THP {romanTahap} - {jenisAlasHak} {desa} {project} {ptsk}";
                view.NoMemo = noMemo;
                view.NilaiAkte = dataBidang.FirstOrDefault(dB => dB.keyPersil == keyPersilBayared.FirstOrDefault())?.SatuanAkte;
                view.TahapProject = $"THP {dataBidang.FirstOrDefault()?.Tahap} / {dataBidang.FirstOrDefault()?.Desa}";
                view.Notaris = dataBidang.Where(dB => keyPersilBayared.Contains(dB.keyPersil) && string.IsNullOrEmpty(dB.Notaris)).FirstOrDefault()?.Notaris;
                view.TanggalPenyerahan = pembayaran.FirstOrDefault(p => p.selected == true && p.tglPenyerahan != null)?.tglPenyerahan;
                view.Giro = details.SelectMany(x => x.giro, (x, y) => new GiroCore()
                {
                    AccountPenerima = y.accPenerima,
                    BankPenerima = y.bankPenerima,
                    NamaPenerima = y.namaPenerima,
                    Nominal = y.nominal,
                    Jenis = y.jenis,
                    key = y.key
                }).ToArray();
                view.MemoSigns = static_collection?.memoSigns;
                view.Mng = view.Mediator = workers.FirstOrDefault(w => w.key == dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && dB.keyMediator != null)?.keyMediator)?.FullName;
                view.Sales = view.Mediator = workers.FirstOrDefault(w => w.key == dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && dB.keyManager != null)?.keyManager)?.FullName;
                view.Mediator = workers.FirstOrDefault(w => w.key == dataBidang.FirstOrDefault(dB => keyPersilBayared.Contains(dB.keyPersil) && dB.keySales != null)?.keySales)?.FullName;
                view.BidangDetails = dataBidang.Select(x => x.ToView(cleanbundle.Select(x => (x.key, x.luasNIB)).ToList(), keyPersilBayared.ToList())).ToArray();
                List<string> biayaLain = new List<string>();
                foreach (var keyPersil in dataBidang.Select(x => x.keyPersil))
                {
                    foreach (var bayar in pembayaran)
                    {
                        bool isDP = bayar.jenisBayar == "DP";
                        if (bayar.keyPersil == keyPersil && !isDP)
                        {
                            BayarBidangDetails bayarDetail = new()
                            {
                                order = bayar.jenisBayar == "UTJ" ? 1 : 99,
                                keyPersil = keyPersil,
                                ColumnName = bayar.jenisBayar,
                                Value = bayar.jumlahBayar
                            };
                            view.AddBidangDetail(bayarDetail);
                        };
                    };
                    List<DetailPembayaran> listbiayalain = new List<DetailPembayaran>();
                    var persil = GetPersil(keyPersil);
                    listbiayalain.Add(FillBiaya(persil, "BiayaBN", "BALIK NAMA"));
                    listbiayalain.Add(FillBiaya(persil, "pembatalanNIB", "PEMBATALAN NIB"));
                    listbiayalain.Add(FillBiaya(persil, "gantiBlanko", "GANTI BLANKO"));
                    listbiayalain.Add(FillBiaya(persil, "kompensasi", "KOMPENSASI"));
                    listbiayalain.Add(FillBiaya(persil, "pajakLama", "PAJAK LAMA"));
                    listbiayalain.Add(FillBiaya(persil, "pajakWaris", "PAJAK WARIS"));
                    listbiayalain.Add(FillBiaya(persil, "tunggakanPBB", "TUNGGAKAN PBB"));
                    listbiayalain.Add(FillBiaya(persil, "pph21", "PPH"));
                    listbiayalain.Add(FillBiaya(persil, "validasiPPh", "VALIDASI"));

                    foreach (var biaya in listbiayalain)
                    {
                        BayarBidangDetails bayarDetail = new()
                        {
                            keyPersil = keyPersil,
                            ColumnName = $"{biaya.identity} (" + (biaya.fglainnya.GetValueOrDefault(false) ? "BEBAN PENJUAL)" : "BEBAN PEMBELI)"),
                            Value = biaya.nilai,
                            order = 55
                        };
                        if (biaya.nilai != 0)
                        {
                            biayaLain.Add(biaya.identity);
                            view.AddBidangDetail(bayarDetail);
                        }
                    }

                    int? jumlahGiro = pembayaran.FirstOrDefault(x => x.selected == true && keyPersil == x.keyPersil)?.giro.Count();
                    BayarBidangDetails bidangRekening = new()
                    {
                        order = 100,
                        keyPersil = keyPersil,
                        ColumnName = "REKENING",
                        Value = jumlahGiro > 1 ?
                        $"Pecah {jumlahGiro} Rekening"
                        : (jumlahGiro == 0 || jumlahGiro == null) ? "" : $"{jumlahGiro} Rekening"
                    };
                    view.AddBidangDetail(bidangRekening);
                }
                var tglBayars = pembayaran.Where(p => p.jenisBayar == "DP")
                                         .OrderBy(p => p.tglCreatedBayar)
                                         .Select(p => p.tglCreatedBayar).Distinct().ToList();
                var totalBayar = dataBidang.Sum(dB => dB.TotalBeli);

                view.BayarBidangDetails = view.BayarBidangDetails
                         .GroupBy(g => new { g.order, g.keyPersil, g.Value, g.ColumnName })
                         .Select(g => g.First())
                         .ToArray();

                List<BayarBidangDetails> listBayarBidangDP = new List<BayarBidangDetails>();
                foreach (var keyPersil in pembayaran.Select(p => p.keyPersil).Distinct())
                {
                    int counterDP = 1;
                    var bayarDP = pembayaran.Where(p => p.jenisBayar == "DP" && p.keyPersil == keyPersil).ToList();
                    foreach (var item in bayarDP)
                    {
                        BayarBidangDetails bidangDP = new()
                        {
                            order = counterDP,
                            keyPersil = item.keyPersil,
                            ColumnName = $"DP",
                            Value = item.jumlahBayar
                        };
                        listBayarBidangDP.Add(bidangDP);
                        counterDP++;
                    }
                }
                listBayarBidangDP = listBayarBidangDP
                         .GroupBy(g => new { g.order, g.keyPersil, g.Value, g.ColumnName })
                         .Select(g => g.First())
                         .ToList();
                foreach (var item in listBayarBidangDP)
                {
                    var totalDP = listBayarBidangDP.Where(x => x.order == item.order).Select(x => (double)x.Value).Sum();
                    var totalUTJ = item.order == 1 ? view.BayarBidangDetails.Where(x => x.ColumnName == "UTJ").Select(x => (double)x.Value).Sum() : 0;
                    var persenDP = (totalDP + totalUTJ) / dataBidang.Sum(x => x.TotalBeli ?? 0) * 100;
                    string utj = totalUTJ != 0 ? "DIKURANGI UTJ" : "";
                    BayarBidangDetails bidangDP = new()
                    {
                        order = item.order + 1,
                        keyPersil = item.keyPersil,
                        ColumnName = $"DP {item.order} {(int)persenDP}% {utj}",
                        Value = item.Value
                    };
                    view.AddBidangDetail(bidangDP);
                }

                view.BiayaLain = string.Join(", ", biayaLain.Distinct().ToList());


                return Ok(view);
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

        [NeedToken("RPT_WORKREQUEST")]
        [HttpGet("work-request")]
        public async Task<IActionResult> GetReportWorkRequest([FromQuery] string token, [FromQuery] AgGridSettings gs, [FromQuery] bool isCsv = false)
        {
            try
            {
                string[] stagesQuery = new[]
                {
                    "{ $match: { _t: { $in: ['EnProsesRequest','ProjectRequest'] } } }",
                    "{ $unwind: '$info.details' }",
                    "{ $lookup: { from: 'persils_v2', localField: 'info.details.keyPersil', foreignField: 'key', as: 'persil' } }",
                    "{ $unwind: '$persil' }",
                    @"{ $project: { _id: 0,
                        Identifier: '$identifier',
                        IdBidang: '$info.details.IdBidang',
                        KeyProject: { $ifNull: ['$info.keyProject','$info.details.keyProject'] },
                        KeyDesa: { $ifNull: ['$info.keyDesa','$info.details.keyDesa'] },
                        KeyPTSK: { $ifNull: ['$info.keyPTSK','$info.details.keyPtsk'] },
                        EnProses: { $ifNull: ['$info.en_proses', '$persil.basic.current.en_proses'] },
                        AlasHak: { $ifNull: ['$info.details.alasHak', '$info.details.alashak'] },
                        Group: '$info.details.group',
                        Tahap: '$info.details.tahap',
                        NewTahap: '$info.details.newTahap',
                        LuasSurat: '$info.details.luasSurat',
                        LuasBayar: '$info.details.luasDibayar',
                        Created: '$created' } }",
                    "{ $sort: { Created: -1 } }"
                };

                var worklistRquests = await contextpay.GetDocumentsAsync(new WorklistReportModel(), "update_request_data", stagesQuery);

                var villages = contextpay.GetVillages();
                var ptsks = await contextpay.GetDocumentsAsync(new PTSK(), "masterdatas", "{$match: {$and: [{_t:'ptsk'}, {'invalid': {$ne:true}}]}}", "{$project: {_id: 0}}");

                var enProsesRequest = worklistRquests.Select(x => {
                    string project = villages.FirstOrDefault(y => y.desa.key == x.KeyDesa).project?.identity,
                    desa = villages.FirstOrDefault(y => y.desa.key == x.KeyDesa).desa?.identity,
                    ptsk = ptsks.FirstOrDefault(y => y.key == x.KeyPTSK)?.identifier;

                    return x.ToViewModel(project, desa, ptsk);
                }).ToList();

                if (isCsv)
                    return new ContentResult
                    {
                        Content = MakeCsv(enProsesRequest).ToString(),
                        ContentType = "text/csv"
                    };

                var xlst = ExpressionFilter.Evaluate(enProsesRequest, typeof(List<WorklistReportViewModel>), typeof(WorklistReportViewModel), gs);
                var data = xlst.result.Cast<WorklistReportViewModel>().ToList();
                var result = data.OrderByDescending(x => x.Created).GridFeed(gs);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ContentResult { StatusCode = int.Parse(ex.Message) };
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [NeedToken("RPT_PRA_DETAIL_DOC")]
        [HttpGet("prabebas/document-report/{key?}")]
        public async Task<IActionResult> GetPraPembembebasanDetailDocumentReport([FromQuery] string token, string key)
        {
            try
            {
                var user = context.FindUser(token);

                var result = await praPembebasanService.GetDetailDocumentReportPdf(key);

                return new FileContentResult(result, "application/pdf");
            }
            catch (UnauthorizedAccessException e)
            {
                return new ContentResult { StatusCode = int.Parse(e.Message) };
            }
            catch (Exception e)
            {
                MyTracer.TraceError2(e);
                return new UnprocessableEntityObjectResult(e.Message);
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

            var valueLines = reportData.Select(row => string.Join(",", header.Split(',')
                                       .Select(a => CsvFormatCorrection(row.GetType().GetProperty(a).GetValue(row, null)))
                                        ));
            lines.AddRange(valueLines);

            foreach (string item in lines)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        static StringBuilder MakeCsv<T>(IEnumerable<T> reportData)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
            var header = string.Join(",", props.ToList().Select(x => x.Name));
            lines.Add(header);

            var valueLines = reportData.Select(row => string.Join(",", header.Split(',')
                                       .Select(a => CsvFormatCorrection(row.GetType().GetProperty(a).GetValue(row, null)))
                                        ));
            lines.AddRange(valueLines);

            foreach (string item in lines)
            {
                sb.AppendLine(item);
            }

            return sb;
        }

        private Persil GetPersil(string key)
        {
            return contextes.persils.FirstOrDefault(p => p.key == key);
        }

        private Persil GetPersilByIdBidang(string IdBidang)
        {
            return contextes.persils.FirstOrDefault(p => p.IdBidang == IdBidang.Trim());
        }

        private List<BayarDtl> GetBayarDtlByJenisBayar(List<JenisBayar> jenisBayar, List<string> detailKeys, Bayar byr)
        {
            var result = byr.details.Where(dp => dp.invalid != true && jenisBayar.Contains(dp.jenisBayar) && detailKeys.Contains(dp.key)).ToList();

            return result;
        }

        private List<DetailPembayaran> GetHistoryBayar(Bayar byr, JenisBayar jenisBayar, List<string> keyPersils)
        {
            List<DetailPembayaran> listDtlPembayaran = new List<DetailPembayaran>();
            foreach (var keypersil in keyPersils)
            {
                var persil = GetPersil(keypersil);
                var luasBayarPesilHelper = PersilHelper.GetLuasBayar(persil) == 0 ? persil.basic.current.luasSurat : PersilHelper.GetLuasBayar(persil);
                var luasBayar = (persil.luasFix ?? false) ? (persil.basic.current.luasDibayar ?? luasBayarPesilHelper) : luasBayarPesilHelper;
                var satuan = persil.basic.current.satuan ?? persil.basic.current.satuanAkte ?? 0;
                var detail = byr.details.Where(det => det.jenisBayar == jenisBayar && det.invalid != true)
                                        .Where(det => det.subdetails
                                                      .Select(sub => sub.keyPersil)
                                                      .Contains(keypersil)
                                              ).ToList();
                foreach (var item in detail)
                {
                    double persenDP = Math.Round(Convert.ToDouble(item.subdetails.Where(sub => sub.keyPersil == keypersil).Select(sub => sub.Jumlah).FirstOrDefault() * 100 / satuan / luasBayar));
                    var detailPembayaran = new DetailPembayaran();
                    detailPembayaran.key = item.key;
                    detailPembayaran.Tanggal = item.tglPenyerahan;
                    detailPembayaran.identity = jenisBayar != JenisBayar.DP ? Enum.GetName(typeof(JenisBayar), jenisBayar).ToUpper() : Enum.GetName(typeof(JenisBayar), jenisBayar).ToUpper() + " " + persenDP.ToString() + "%";
                    detailPembayaran.nilai = item.Jumlah;
                    detailPembayaran.fglainnya = false;
                    listDtlPembayaran.Add(detailPembayaran);
                }
            }

            listDtlPembayaran = listDtlPembayaran.GroupBy(ld => ld.key)
                                                 .Select(ldn => new DetailPembayaran
                                                 {
                                                     keyOrder = GetBiayaOrder(Enum.GetName(typeof(JenisBayar), jenisBayar).ToUpper()),
                                                     key = ldn.First().key,
                                                     Tanggal = ldn.First().Tanggal,
                                                     identity = jenisBayar != JenisBayar.Lunas ? ldn.First().identity : "Pelunasan",
                                                     nilai = ldn.First().nilai,
                                                     fglainnya = false
                                                 }).ToList();

            foreach (var listbayar in listDtlPembayaran)
            {
                string key = "";
                var detailBayar = byr.details.Where(det => det.key == listbayar.key).FirstOrDefault();
                foreach (var sub in detailBayar.subdetails)
                {
                    key += sub.keyPersil + ",";
                }
                listbayar.key = key.EndsWith(",") ? key.Remove(key.Length - 1) : key;
            }

            return listDtlPembayaran;
        }

        private DetailPembayaran FillBiaya(Persil persil, string propertyName, string identity)
        {
            DetailPembayaran a = new DetailPembayaran();
            a.identity = identity;
            a.keyOrder = GetBiayaOrder(identity);
            if (!propertyName.ToLower().Contains("pph"))
            {

                a.nilai = (double?)persil.GetType().GetProperty(propertyName).GetValue(persil, null) ?? 0;
                a.fglainnya = ((double?)persil.GetType().GetProperty(propertyName).GetValue(persil, null) > 0) ? true : false;
            }
            else if (propertyName.ToLower() == "validasipph")
            {
                a.nilai = persil.ValidasiPPHValue.HasValue ? persil.ValidasiPPHValue.Value : 0;
                a.fglainnya = persil.ValidasiPPH.HasValue ? persil.ValidasiPPH.Value : false;
                a.nilai = (a.fglainnya ?? false) ? (-1) * a.nilai : a.nilai;
            }
            else
            {
                var luasBayarPesilHelper = PersilHelper.GetLuasBayar(persil) == 0 ? persil.basic.current.luasSurat : PersilHelper.GetLuasBayar(persil);
                var luasBayar = (persil.luasFix ?? false) ? (persil.basic.current.luasDibayar ?? luasBayarPesilHelper) : luasBayarPesilHelper;
                int multiplier = (persil.pph21 ?? false) ? -1 : 1;
                a.nilai = multiplier * (2.5 / 100) * (persil.basic.current.satuanAkte ?? persil.basic.current.satuan) * luasBayar;
                a.fglainnya = persil.pph21.HasValue ? persil.pph21.Value : false;
            }
            return a;
        }

        private int GetBiayaOrder(string biayaName)
         => biayaName switch
         {
             "UTJ" => 1,
             "Biaya Mandor" => 2,
             "DP" => 3,
             "PPH" => 4,
             "Validasi" => 5,
             "Pelunasan" => 7,
             _ => 6
         };

        private string GetRomanMonth(int number)
            => number switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                6 => "VI",
                7 => "VII",
                8 => "VIII",
                9 => "IX",
                10 => "X",
                11 => "XI",
                12 => "XII",
                _ => ""
            };

        private static string CsvFormatCorrection(object objVal)
        {
            string value = objVal != null ? objVal.ToString() : "";
            string correctedFormat = "";
            correctedFormat = value.Contains(",") ? "\"" + value + "\"" : value;
            return correctedFormat;
        }

        static Category GetCategory(List<Category> master, string[] segments, int segment)
        {
            string keyCat = segments.ElementAtOrDefault(segment - 1);
            var f = master.Where(w => w.key == keyCat).FirstOrDefault();
            return f ?? new landrope.mod2.Category();
        }

        private List<FUMarketing> GetLatestFollowUpMarketingData()
        {
            var fuMarketing = context.GetDocuments(new FUMarketing(), "followUpMarketing", "{$project:{_id:0}}").AsParallel().ToList();
            fuMarketing = fuMarketing.GroupBy(f => f.keyPersil)
                                     .Select(grp => grp.OrderByDescending(x => x.followUpDate)
                                                       .ThenByDescending(x => x.created).FirstOrDefault())
                                     .ToList();
            return fuMarketing;
        }

        private string OrderSubCategory(string subCat)
            => subCat.ToLower() switch
            {
                "mudah" => "1MUDAH",
                "urusan pidana" => "2URUSAN PIDANA",
                "urusan dki" => "3URUSAN DKI",
                "sengketa" => "4SENGKETA",
                _ => $"5{subCat}"
            };

        private List<cmnItem> GetProjectInculde()
        {
            return contextes.GetCollections(new { key = "", identity = "" }, "projects_include", "{}", "{_id:0,key:1,identity:1}")
                            .ToList()
                            .Select(p => new cmnItem { key = p.key, name = p.identity }).OrderBy(p => p.name)
                            .ToList();
        }

        private string SetStatusMarketing(FUMarketing fuMarketing)
        {
            string status = "";
            if (fuMarketing == null)
                return "BELUM FOLLOW UP";
            status = (fuMarketing?.result == FollowUpResult.JualDanBukaHarga ?
                                             string.Format("{0:C}", fuMarketing?.price).Replace("$", "") :
                                             (fuMarketing?.result ?? FollowUpResult._).FollowUpResultDescription());
            status = string.IsNullOrEmpty(status) ? "BELUM FOLLOW UP" : status;
            return status;
        }

        private List<cmnItem> GetMasterDesa()
        {
            return contextes.GetDocuments(new { key = "", identity = "" }, "villages",
                     @"{$project : {
                        _id:0,
                        key:'$village.key',
                        identity: '$village.identity'
                    }}"
                 )
                .Select(p => new cmnItem { key = p.key, name = p.identity })
                .ToList();
        }

        private IEnumerable<BelumBebasNormatif> GetBelumBebasNormatif(string keyProject = null, string keyCategory = null, string keySubCat = null)
        {
            var projectAllowed = GetProjectInculde();
            var desas = GetMasterDesa();
            var categories = contextes.GetCollections(new Category(), "categories_new", "{bebas:0}", "{_id:0}").ToList();

            var followUps = GetLatestFollowUpMarketingData();

            var query = contextes.GetDocuments(new Persil(), "persils_v2",
                    @"{$match : { 
                        deal: null, 
                        categories : {$ne:null}, 
                        en_state : 1, 
                       'basic.current.en_proses' : 0,
                    }}", //deal : {$ne:null},
                    "{$project: {_id:0}}"
                ).ToList();

            var belumBebasNormatifs = query.Where(delegate (Persil persil)
            {
                string keyProject = persil?.basic?.current?.keyProject;
                return projectAllowed.Select(s => s.key).Contains(keyProject);
            }).Select(delegate (Persil persil)
            {
                category category = persil.categories.LastOrDefault();
                cmnItem project = projectAllowed.Where(w => w.key == persil.basic?.current?.keyProject).FirstOrDefault();
                cmnItem desa = desas.Where(w => w.key == persil.basic?.current?.keyDesa).FirstOrDefault();

                BelumBebasNormatif bbn = new();

                bbn.IdBidang = persil.IdBidang;
                (bbn.KeyProject, bbn.Project) = (project.key, project.name);
                bbn.Desa = desa?.name;
                bbn.NoPeta = persil?.basic?.current?.noPeta;
                bbn.KeyPersil = persil.key;

                if (category != null)
                {
                    string[] keyCategories = category.keyCategory;

                    var s1 = GetCategory(categories, keyCategories, 1); // segment 1 => CATEGORY
                    if (s1.shortDesc?.ToLower() != "dikeluarkan dari planning")
                        (bbn.KeyCategory, bbn.Category) = (s1.key, s1.shortDesc);

                    var s2 = GetCategory(categories, keyCategories, 6); // segment 6 => SUBCATEGORY
                    if (s2.shortDesc?.ToLower() != "deal")
                        (bbn.KeySubCategory, bbn.SubCategory) = (s2.key, s2.shortDesc?.ToUpper());

                    var group = GetCategory(categories, keyCategories, 2); // segment 2 => Group
                    (bbn.KeyGroup, bbn.Group) = (group.key, group.shortDesc); // segment 2 => GROUP

                    var PIC = GetCategory(categories, keyCategories, 4); // segment 4 => PIC
                    (bbn.KeyPIC, bbn.PIC) = (PIC?.key, PIC?.shortDesc);

                    var Pemilik = GetCategory(categories, keyCategories, 3); //Segment 3 => Pemilik
                    (bbn.KeyPemilik, bbn.Pemilik) = (Pemilik?.key, Pemilik?.shortDesc);

                    var Sales = GetCategory(categories, keyCategories, 5); //Segment 5 => Sales
                    (bbn.KeySales, bbn.Sales) = (Sales?.key, Sales?.shortDesc);

                    bbn.Description = followUps.Where(w => w.keyPersil == persil.key)
                                        .Select(s => s.note)
                                        .FirstOrDefault<string>();
                }

                // luas
                double? luas = persil?.basic?.current?.luasSurat;
                bbn.Luas = (luas == null || luas == 0) ? 0 : (luas / 10000d);
                bbn.LuasM2 = (luas == null || luas == 0) ? 0 : luas;
                return bbn;
            });


            if (!string.IsNullOrEmpty(keyProject))
                belumBebasNormatifs = belumBebasNormatifs.Where(x => x.KeyProject == keyProject);
            if (!string.IsNullOrEmpty(keyCategory))
                belumBebasNormatifs = belumBebasNormatifs.Where(x => x.KeyCategory == keyCategory);
            if (!string.IsNullOrEmpty(keySubCat))
                belumBebasNormatifs = belumBebasNormatifs.Where(x => x.KeySubCategory == keySubCat);

            return belumBebasNormatifs;
        }

        private class PersilGK
        {
            public string IdBidang { get; set; }
            public DateTime? deal { get; set; }
            public string keyProject { get; set; }
            public string kPIC { get; set; }
            public StatusBidang state { get; set; }
            public JenisProses proses { get; set; }
            public double luasSurat { get; set; }
            public string group { get; set; }
            public string kategori { get; set; }
            public double day { get; set; }
            public cate ketemu { get; set; }
            public cate mudahSulit { get; set; }
            public category[] categories { get; set; }

            public PersilGK setDays(DateTime? startDate, DateTime? endDate)
            {
                if (startDate != null && endDate != null)
                {
                    day = (Convert.ToDateTime(endDate) - Convert.ToDateTime(startDate)).TotalDays;
                }
                return this;
            }
        }

        private class cate
        {
            public DateTime tgl { get; set; }
            public string cat { get; set; }
        }

        private class GroupKecilSudahKetemu
        {
            public string Nama { get; set; }
            public string Project { get; set; }
            public double Total { get; set; }
            public double progress { get; set; }

            public string PIC { get; set; }

        }


        [AllowAnonymous]
        [HttpGet("bidang/bi/summary/deal")]
        public IActionResult GetBiDealSummary([FromQuery] string keyProject)
        {
            try
            {
                return ReportSummaryDeal(keyProject);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("bidang/bi/summary/belumbebasnormatif")]
        public IActionResult GetBiSummaryBelumBebasNormatif([FromQuery] string keyProject, string keyCategory, string keySubCategory)
        {
            try
            {
                return ReportSummaryBelumBebasNormatif(keyProject, keyCategory, keySubCategory);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("bidang/bi/summary/belumbebasnormatif-noname")]
        public IActionResult GetBiSummaryBelumBebasNoName([FromQuery] string keyProject, string keyPic)
        {
            try
            {
                return ReportBelumBebasNoName(keyProject, keyPic);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
        private ActionResult ReportSummaryDeal(string keyProject)
        {
            var projectAllowed = GetProjectInculde();
            var categories = contextes.GetCollections(new Category(), "categories_new", "{}", "{_id:0}").ToList();

            var query = contextes.GetDocuments(new Persil(), "persils_v2",
                    "{$match : { categories : {$ne:null}, en_state : 1, 'basic.current.en_proses' : 0}}", //deal : {$ne:null},
                    "{$project: {_id:0}}"
                ).ToList();

            var filter = query.Where(delegate (Persil persil)
            {
                string keyProject = persil?.basic?.current?.keyProject;
                return projectAllowed.Select(s => s.key).Contains(keyProject);
            }).Select(delegate (Persil persil)
            {
                BelumBebasDeal bbd = new();
                category category = persil.categories.LastOrDefault();

                // status deal
                if (persil.deal != null)
                {
                    bbd.DealStatus = "Deal By System";
                }
                else
                {
                    if (category != null)
                    {
                        var a = GetCategory(categories, category.keyCategory, 6); // segment 6
                        if (a.shortDesc?.ToLower() == "deal")
                            bbd.DealStatus = "Deal Tambahan";
                    }
                }

                //project name
                var project = projectAllowed.Where(w => w.key == persil?.basic?.current?.keyProject).FirstOrDefault();
                (bbd.KeyProject, bbd.Project) = (project?.key, project?.name);

                if (category != null)
                {
                    bbd.GroupType = GetCategory(categories, category.keyCategory, 1).shortDesc;
                    bbd.Group = GetCategory(categories, category.keyCategory, 2).shortDesc;
                }

                // luas
                double? luas = persil?.basic?.current?.luasSurat;
                bbd.Value = (luas == null || luas == 0) ? 0 : (luas / 10000d);

                return bbd;
            })
            .Where(w => !string.IsNullOrEmpty(w.DealStatus) && !(string.IsNullOrEmpty(w.GroupType) || string.IsNullOrEmpty(w.Group)))
            .ToList();

            if (!string.IsNullOrEmpty(keyProject))
                filter = filter.Where(w => w.KeyProject == keyProject).ToList();

            return Ok(filter);
        }

        private ActionResult ReportSummaryBelumBebasNormatif(string keyProject, string keyCategory, string keySubCategory)
        {
            var filter = GetBelumBebasNormatif(keyProject, keyCategory, keySubCategory)
                            .Where(w =>
                                    !string.IsNullOrEmpty(w.Category)
                                    && !string.IsNullOrEmpty(w.SubCategory)
                                    && !string.IsNullOrEmpty(w.Group)
                            );

            List<string> groupOrder = new List<string>() { "MUDAH", "PIDANA", "DKI", "SENGKETA", "SULIT" };

            if (!string.IsNullOrEmpty(keyCategory))
            {

                var result = filter.Where(w => w.KeyCategory == keyCategory)
                                .OrderBy(o => o.Category)
                                .GroupBy(g => (g.KeyProject, g.KeyCategory, g.KeySubCategory, g.KeyGroup, g.KeyPIC))
                                .Select((s, i) =>
                                {
                                    string subCategory = s.FirstOrDefault()?.SubCategory?.ToUpper();
                                    int index = groupOrder.FindIndex(a => a.Contains(subCategory));
                                    if (index < 0)
                                    {
                                        groupOrder.Add(subCategory);
                                        index = groupOrder.Count();
                                    }
                                    else
                                    {
                                        index = index + 1;
                                    }

                                    var bbnr = new
                                    {
                                        Index = i + 1,
                                        Project = s.FirstOrDefault()?.Project,
                                        Category = s.FirstOrDefault()?.Category,
                                        Group = s.FirstOrDefault()?.Group,
                                        Luas = s.Sum(su => su.Luas),
                                        Description = s.FirstOrDefault()?.Description,
                                        PIC = s.FirstOrDefault()?.PIC,
                                        SubCategory = $"{index}" + (subCategory switch
                                        {
                                            "DKI" => "URUSAN DKI",
                                            "PIDANA" => "URUSAN PIDANA",
                                            _ => subCategory
                                        }),
                                        keyProject = s.Key.KeyProject,
                                        keyCategory = s.Key.KeyCategory,
                                        keySubCategory = s.Key.KeySubCategory,
                                        keyGroup = s.Key.KeyGroup
                                    };
                                    return bbnr;
                                }).OrderBy(x => x.SubCategory).ThenBy(x => x.Group);
                return Ok(result);
            }
            else
            {
                var result = filter.GroupBy(g => g.KeyCategory)
                                    .Select(s => new
                                    {
                                        KeyCategory = s.Key,
                                        Category = s.FirstOrDefault()?.Category,
                                        Luas = s.Sum(u => u.Luas)
                                    });
                return Ok(result);
            }
        }

        private ActionResult ReportBelumBebasNoName(string keyProject, string keyPic)
        {
            string keyCategory = "60863014934CF7D90C8G3M0002"; // GROUP BESAR
            string keySubCategory = "60863014934CF7D90C8G3M1941"; // BELUM KETEMU / BELUM ADA NAMA
            var historicalCategory = contextes.GetDocuments(new
            {
                KeyPersil = "",
                HistoricalCategory = new category[0]
            }, "persils_v2",
                @"{$match : { 
                        deal: null, 
                        categories : {$ne:null}, 
                        en_state : 1, 
                        'basic.current.en_proses' : 0,
                    }}",
                @"{$project: {
                        _id:0,
                        KeyPersil: '$key',
                        HistoricalCategory: '$categories'
                    }}"
            ).ToList();

            var filter = GetBelumBebasNormatif(keyProject, keyCategory)
                        .Where(w =>
                            !string.IsNullOrEmpty(w.Category)
                            && !string.IsNullOrEmpty(w.SubCategory)
                        //&& !string.IsNullOrEmpty(w.Group)
                        )
                        .GroupJoin(historicalCategory,
                            BelumBebasNormatif => BelumBebasNormatif.KeyPersil,
                            Persil => Persil.KeyPersil,
                            (x, y) => new
                            {
                                BelumBebasNormatif = x,
                                Persil = y
                            }
                        ).SelectMany(
                            xy => xy.Persil.DefaultIfEmpty(),
                            (x, y) => new
                            {
                                BelumBebasNormatif = x.BelumBebasNormatif,
                                HistoricalCategory = y.HistoricalCategory
                            }
                        ).Where(w => w.BelumBebasNormatif.KeyCategory == keyCategory);
            //&& w.BelumBebasNormatif.KeySubCategory == keySubCategory

            // calculate
            //60863014934CF7D90C8G3M1941 //belum ada nama
            //60863014934CF7D90C8G3M1942 //sudah ada nama
            //60863014934CF7D90C8G3M1938 // mudah
            //60863014934CF7D90C8G3M1939 // sulit
            string keyBelumKetemu = "60863014934CF7D90C8G3M1941";
            string[] keySudahKetemu = new string[] {
                                                "60863014934CF7D90C8G3M1942",
                                                "60863014934CF7D90C8G3M1938",
                                                "60863014934CF7D90C8G3M1939"
                                            };
            var calculate = filter
                .GroupBy(g => (
                   g.BelumBebasNormatif.KeyPIC,
                   g.BelumBebasNormatif.KeyProject,
                   g.BelumBebasNormatif.Desa
               ))
                .Select(data =>
                {
                    Dictionary<string, object> rows = new Dictionary<string, object>();

                    double? luas = data.Sum(sum => sum.BelumBebasNormatif.Luas);

                    rows.Add("KeyPIC", data.Key.KeyPIC);
                    rows.Add("PIC", data.FirstOrDefault()?.BelumBebasNormatif.PIC);
                    rows.Add("KeyProject", data.Key.KeyProject);
                    rows.Add("Project", data.FirstOrDefault()?.BelumBebasNormatif.Project);
                    rows.Add("Desa", data.Key.Desa);
                    rows.Add("Luas", luas);
                    rows.Add("Bidang", data.Count());

                    for (int i = 1; i <= 8; i++)
                        rows.Add($"Hari{i}", 0.0);

                    rows.Add($"Total", 0.0);

                    double? total = 0.0;

                    foreach (var bidang in data)
                    {
                        BelumBebasNormatif bbn = bidang.BelumBebasNormatif;
                        List<category> categories = bidang.HistoricalCategory.OrderBy(o => o.tanggal).ToList();

                        // find BELUM KETEMU / BELUM ADA NAMA
                        var belumketemu = categories
                                    .Where(w => w.keyCategory.Contains(keyBelumKetemu))
                                    .FirstOrDefault();

                        // find SUDAH KETEMU / SUDAH ADA NAMA / MUDAH / SULIT
                        var sudahKetemu = categories
                                    .Where(w => w.keyCategory.Any(a => keySudahKetemu.Contains(a)))
                                    .FirstOrDefault();

                        if (belumketemu != null && sudahKetemu != null)
                        {
                            int day = (sudahKetemu.tanggal - belumketemu.tanggal).Days;
                            int index = day > 7 ? 8 : (day == 0 ? 1 : day);

                            object oldValue = default(object);
                            rows.TryGetValue($"Hari{index}", out oldValue);

                            total += bbn.Luas;
                            rows[$"Hari{index}"] = (double)oldValue + bbn.Luas;

                            //Console.WriteLine($"sudah ketemu : {sudahKetemu.tanggal.ToString()} - Belum Ketemu {belumketemu.tanggal.ToString()} = {day}");
                        }
                    }

                    rows["Total"] = total;
                    return rows;
                });


            if (string.IsNullOrEmpty(keyPic))
            {
                var result = calculate
                            .GroupBy(g => g["KeyPIC"], (key, items) => new { key, items })
                            .Select(s =>
                            {
                                var item = s.items.FirstOrDefault();

                                object pic = default(object);
                                item.TryGetValue("PIC", out pic);

                                double luas = s.items.Select(si =>
                                {
                                    object x = default(object);
                                    si.TryGetValue("Luas", out x);

                                    double y = default(double);
                                    double.TryParse(x.ToString(), out y);
                                    return y;
                                }).ToArray().Sum(su => su);

                                double pencapaian = s.items.Select(si =>
                                {
                                    object x = default(object);
                                    si.TryGetValue("Total", out x);

                                    double y = default(double);
                                    double.TryParse(x.ToString(), out y);
                                    return y;
                                }).ToArray().Sum(su => su);

                                var bbn = new BelumBebasGroupKecilSudahKetemu()
                                {
                                    Segment1 = "GROUP KECIL",
                                    Segment6 = "BELUM ADA NAMA",
                                    keyPIC = s.key.ToString(),
                                    Kategory = pic.ToString(),
                                    Luas = luas,
                                    LuasPencapaian = pencapaian,
                                    Persentase = (pencapaian / luas) * 100,
                                    SisaTarget = luas - pencapaian
                                };

                                return bbn;
                            });
                return Ok(result);
            }
            else
            {
                var result = calculate.Where(w => w.Keys.Contains("KeyPIC") && (w["KeyPIC"]).ToString() == keyPic);
                return Ok(result);
            }
        }


        private string prepregx(string val)
        {
            //var st = val;//.Substring(1,val.Length-2); // remove the string marker
            var st = val.Replace(@"\", @"\\");
            st = new Regex(@"([\(\)\{\}\[\]\.\,\+\?\|\^\$\/])").Replace(st, @"\$1");
            st = new Regex(@"\s+").Replace(st, @"\s+");
            return st;
        }

        (IEnumerable<RptStatusPerDealMod> data, int count) CollectBidangDeal(string token,
                        IEnumerable<string> prestages,
                        IEnumerable<string> poststages,
                        IEnumerable<string> sortstages,
                        int skip, int limit)
        {
            var allstages = prestages.Union(poststages).ToArray();
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
            var resultpipe = PipelineDefinition<BsonDocument, RptStatusPerDealMod>.Create(allstages);
            int count = 0;
            List<RptStatusPerDealMod> result = new List<RptStatusPerDealMod>();
            var coll = context.db.GetCollection<BsonDocument>("material_report_bidang_deal");
            var task1 = Task.Run(() =>
            {
                var xcounter = coll.Aggregate<BsonDocument>(countpipe);
                var counter = xcounter.FirstOrDefault();
                if (counter != null)
                    count = counter.Names.Any(s => s == "count") ? counter.GetValue("count").AsInt32 : 0;
            });
            var task2 = Task.Run(() =>
            {
                var res = coll.Aggregate<RptStatusPerDealMod>(resultpipe);
                result = res.ToList();
            });
            Task.WaitAll(task1, task2);
            return (result, count);
        }


        private List<PembayaranBidang> GetHistoryPembayaran(string keyTahap, string keyBayar)
        {
            var pembayaranBidang = context.GetDocuments(new PembayaranBidang(), "bayars",
                "{$match: {'key': '" + keyTahap + "'}}",
                "{$unwind: '$details'}",
                "{$match: { 'details.key': {$in: [ " + keyBayar + " ]}}}",
                "{$unwind: '$bidangs'}",
                "{$lookup: {from: 'graphables', localField: 'details.instkey', foreignField: 'key', as: 'grpc'}}",
                "{$unwind: { path: '$grpc', preserveNullAndEmptyArrays: true}}",
                "{$addFields: {'bayarCreated': {$arrayElemAt: ['$grpc.states', 0]}}}",
               @"{$lookup: {from: 'bayars',
                            let: {keyTahap: '$key', byrDate:'$bayarCreated.time' , keyPersil: '$bidangs.keyPersil'},
                            pipeline: [
                                {$match: {$and: [{$expr: {$eq: ['$key', '$$keyTahap']}}] }},
                                {$unwind: '$details'},
                                {$lookup: {from: 'graphables', localField: 'details.instkey', foreignField: 'key', as: 'grpc'}},
                                {$unwind: { path: '$grpc', preserveNullAndEmptyArrays: true}},
                                {$addFields: {'bayarCreated': {$arrayElemAt: ['$grpc.states', 0]}}},
                                {$unwind: '$details.subdetails'},
                                {$match: {$and: [{$expr: {$eq: ['$details.subdetails.keyPersil', '$$keyPersil']}}]}},
                                {$match: {$expr: {$lte: ['$bayarCreated.time', '$$byrDate']}}},
                                {$project: {
                                    _id:0,
                                    noMemo: '$details.noMemo',
                                    keyPersil:'$details.subdetails.keyPersil',
                                    jumlahBayar: '$details.subdetails.Jumlah',
                                    jenisBayar: {$switch : {
                                                    branches:[
                                                        {case: {$eq: ['$details.jenisBayar', 1]}, then: 'UTJ'},
                                                        {case: {$eq: ['$details.jenisBayar', 2]}, then: 'DP'},
                                                        ],
                                                        default: 'PELUNASAN'
                                                }},
                                    tglCreatedBayar: '$bayarCreated.time',
                                    tglPenyerahan: '$details.tglPenyerahan',
                                    giro: '$details.giro',
                                    selected: {$eq: ['$bayarCreated.time', '$$byrDate']}
                                }}
                                ],
                            as: 'pembayaran'}
                }",
                "{$unwind: { path: '$pembayaran', preserveNullAndEmptyArrays: true}}",
               @"{$project: {
                                _id:0,
                                noMemo: '$pembayaran.noMemo',
                                keyPersil: '$pembayaran.keyPersil',
                                selected: '$pembayaran.selected',
                                jumlahBayar: '$pembayaran.jumlahBayar',
                                jenisBayar: '$pembayaran.jenisBayar',
                                tglCreatedBayar: '$pembayaran.tglCreatedBayar',
                                tglPenyerahan: '$pembayaran.tglPenyerahan',
                                giro: '$pembayaran.giro'
                            }}",
                "{$sort: {'pembayaran.tglCreatedBayar': -1}}"
                ).ToList();

            return pembayaranBidang;
        }

        private string IntToRoman(int num)
        {
            var result = string.Empty;
            var map = new Dictionary<string, int>
            {
                {"M", 1000 },
                {"CM", 900},
                {"D", 500},
                {"CD", 400},
                {"C", 100},
                {"XC", 90},
                {"L", 50},
                {"XL", 40},
                {"X", 10},
                {"IX", 9},
                {"V", 5},
                {"IV", 4},
                {"I", 1}
            };
            foreach (var pair in map)
            {
                result += string.Join(string.Empty, Enumerable.Repeat(pair.Key, num / pair.Value));
                num %= pair.Value;
            }
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using GraphConsumer;
using landrope.common;
using landrope.mod2;
using landrope.mod3;
using MongoDB.Bson.Serialization.Attributes;
using flow.common;
using mongospace;
using GenWorkflow;
using landrope.consumers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Tracer;
using System.Reflection;
using GraphHost;

namespace landrope.mod4
{
    [Entity("bayar", "bayars")]
    //[BsonKnownTypes(typeof)]
    public class Bayar : namedentity4, IBayar
    {
        public Bayar(user user)
        {
            //key = MakeKey;
            created = DateTime.Now;
            keyCreator = user.key;
        }

        public Bayar()
        {
            created = DateTime.Now;
        }

        public int nomorTahap { get; set; }

        public JenisPersil jenisPersil { get; set; }

        public string keyProject { get; set; }

        public string keyDesa { get; set; }

        public string group { get; set; }

        public DateTime created { get; set; }

        public string keyCreator { get; set; }
        public string? keyPTSK { get; set; }
        public string? keyPenampung { get; set; }
        public double? pph { get; set; }
        public double? biayaValidasi { get; set; }
        public BayarDtl[] details { get; set; } = new BayarDtl[0];
        public BayarDtlBidang[] bidangs { get; set; } = new BayarDtlBidang[0];
        public BayarDtlDeposit[] deposits { get; set; } = new BayarDtlDeposit[0];
        public Persil persil(LandropePayContext context, string keyPersil) => context.persils.FirstOrDefault(p => p.key == keyPersil);



        public void FromCore(BayarCore core)
        {
            (nomorTahap, jenisPersil, keyProject, keyDesa, group, keyPTSK, keyPenampung) =
                     (core.nomorTahap,
                     (Enum.TryParse<JenisPersil>(core.jenisPersil.ToString(), out JenisPersil stp) ? stp : default),
                     core.keyProject, core.keyDesa, core.group, core.keyPTSK, core.keyPenampung);
        }

        public void FromCore2(BayarCore core)
        {
            (nomorTahap, jenisPersil, keyProject, keyDesa, group, keyPTSK, keyPenampung, keyCreator) =
                     (core.nomorTahap,
                     (Enum.TryParse<JenisPersil>(core.jenisPersil.ToString(), out JenisPersil stp) ? stp : default),
                     core.keyProject, core.keyDesa, core.group, core.keyPTSK, core.keyPenampung, core.keyCreator);
        }

        public BayarRpt ToView(LandropePayContext context, List<Persil> persils)
        {
            var view = new BayarRpt();
            var creator = context.users.FirstOrDefault(y => y.key == keyCreator)?.FullName;

            (var project, var desa) = context.GetVillage(keyDesa);

            double sum = 0;
            var lst = new List<BayarDtlBidang>();
            if (bidangs != null)
            {
                lst = bidangs.ToList();
                //var persils = context.persils.Query(x => x.invalid != true);
                var qry = (from a in persils
                           where (from b in lst
                                  select b.keyPersil).Contains(a.key)
                           select new
                           {
                               luas = a.basic.current.luasDibayar == null ? 0 : a.basic.current.luasDibayar
                           }).ToList();
                sum = qry.Sum(x => Convert.ToDouble(x.luas));
            }

            var create = created.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");


            var jmlBidangs = bidangs?.Count() == null ? 0 : bidangs.Count();

            (view.NoTahap, view.Project, view.Desa, view.Group, view.JumlahBidang, view.TotalLuas, view.Dibuat, view.Tanggal) =
                (nomorTahap, project?.identity, desa?.identity, group, jmlBidangs, sum, creator, create);

            return view;
        }

        public BayarPivot ToPivot()
        {
            var view = new BayarPivot();

            //(view.keyProject, view.keyTahap, view.tahap, view.keyPersil, view.Kode, view.value) =
            //    (nomorTahap, project?.identity, desa?.identity, group, jmlBidangs, sum);

            return view;
        }

        public void AddDetailBidang(BayarDtlBidang byrdtlB)
        {
            var lst = new List<BayarDtlBidang>();
            if (bidangs != null)
                lst = bidangs.ToList();

            lst.Add(byrdtlB);
            bidangs = lst.ToArray();
        }

        public void AddDetail(BayarDtl byrdtl)
        {
            var lst = new List<BayarDtl>();
            if (details != null)
                lst = details.ToList();

            lst.Add(byrdtl);
            details = lst.ToArray();
        }

        public void AddDeposit(BayarDtlDeposit byrDeposit)
        {
            var lst = new List<BayarDtlDeposit>();
            if (deposits != null)
                lst = deposits.ToList();

            lst.Add(byrDeposit);
            deposits = lst.ToArray();
        }

        public void DelDetail(BayarDtl dtl)
        {
            var lst = new List<BayarDtl>();
            if (details != null)
                lst = details.ToList();

            lst.Remove(dtl);
            details = lst.ToArray();
        }

        public void DelDetailBidang(BayarDtlBidang dtl)
        {
            var lst = bidangs.ToList();
            lst.Remove(dtl);
            bidangs = lst.ToArray();
        }

        //public (double sisalunas, double totalpembayaran, double pphx) SisaPelunasan2(Persil persil)
        //{
        //    (double totalBayar, double realUTJ, double realLainnya, double realMandor, double realDP, double DPBidang, double? luasdiBayar, double pelunasan, double pph21, double pph, double realpph, double validasiPPH,
        //        double? luasNIB, double? luasDiBayar, double? biayaLainnya) =
        //        (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        //    (double? mandor, double? pembatalanNIB, double? balikNama, double? gantiBlanko, double? kompensasi, double? pajaklama, double? pajakwaris, double? tunggakanPBB) =
        //        (persil.mandor == null ? 0 : (persil.mandor < 0 ? 0 : persil.mandor),
        //        persil.pembatalanNIB == null ? 0 : (persil.pembatalanNIB < 0 ? 0 : persil.pembatalanNIB),
        //        persil.BiayaBN == null ? 0 : (persil.BiayaBN < 0 ? 0 : persil.BiayaBN),
        //        persil.gantiBlanko == null ? 0 : (persil.gantiBlanko < 0 ? 0 : persil.gantiBlanko),
        //        persil.kompensasi == null ? 0 : (persil.kompensasi < 0 ? 0 : persil.kompensasi),
        //        persil.pajakLama == null ? 0 : (persil.pajakLama < 0 ? 0 : persil.pajakLama),
        //        persil.pajakWaris == null ? 0 : (persil.pajakWaris < 0 ? 0 : persil.pajakWaris),
        //        persil.tunggakanPBB == null ? 0 : (persil.tunggakanPBB < 0 ? 0 : persil.tunggakanPBB));

        //    if (persil.biayalainnya != null)
        //        biayaLainnya = persil.biayalainnya.Where(x => x.fgLainnya == true).Select(x => x.nilai).Sum();

        //    luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;

        //    luasDiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

        //    var isLunas = details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
        //           .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

        //    if (isLunas)
        //        luasdiBayar = persil.luasPelunasan ?? luasdiBayar;
        //    else if (persil.luasFix != true)
        //        luasdiBayar = luasNIB == 0 ? luasDiBayar : luasNIB;
        //    else
        //        luasdiBayar = luasDiBayar;

        //    var satuan = persil.basic.current.satuan == null ? 0 : persil.basic.current.satuan;
        //    var satuanAkte = persil.basic.current.satuanAkte == null ? 0 : persil.basic.current.satuanAkte;

        //    totalBayar = Convert.ToDouble(luasdiBayar) * Convert.ToDouble(satuan);
        //    var totalBayarAkte = Convert.ToDouble(luasdiBayar) * Convert.ToDouble(satuanAkte);

        //    pph21 = (totalBayar * 2.5) / 100;
        //    var pph21akte = (totalBayarAkte * 2.5) / 100;
        //    pph = pph21akte == 0 ? pph21 : pph21akte;

        //    if (persil.pph21 == true)
        //    {
        //        realpph = pph; // Jika ditanggung penjual maka akan mengurangi pelunasan
        //    }

        //    if (persil.ValidasiPPH == true)
        //        validasiPPH = Convert.ToDouble(persil.ValidasiPPHValue);

        //    var dtl = details.Where(x => x.invalid != true).ToArray();

        //    //UTJ
        //    var UTJ = dtl.FirstOrDefault(x => x.jenisBayar == JenisBayar.UTJ);
        //    if (UTJ != null)
        //    {
        //        var subdetails = UTJ.subdetails.FirstOrDefault(x => x.keyPersil == persil.key);
        //        realUTJ = subdetails == null ? 0 : subdetails.Jumlah;
        //    }

        //    //Lainnya
        //    var Lainnya = dtl.FirstOrDefault(x => x.jenisBayar == JenisBayar.Lainnya);
        //    realLainnya = Lainnya?.subdetails.FirstOrDefault(x => x.keyPersil == persil.key)?.Jumlah ?? 0;
        //    //if (Lainnya != null)
        //    //{
        //    //    var subdetails = Lainnya.subdetails.FirstOrDefault(x => x.keyPersil == persil.key);
        //    //    realLainnya = subdetails == null ? 0 : subdetails.Jumlah;
        //    //}

        //    //Mandor
        //    var Mandor = dtl.FirstOrDefault(x => x.jenisBayar == JenisBayar.Mandor);
        //    realMandor = Mandor?.subdetails.FirstOrDefault(x => x.keyPersil == persil.key)?.Jumlah ?? 0;
        //    //if (Mandor != null)
        //    //{
        //    //    var subdetails = Mandor.subdetails.FirstOrDefault(x => x.keyPersil == persil.key);
        //    //    realMandor = subdetails == null ? 0 : subdetails.Jumlah;
        //    //}

        //    var listDPBidangs = dtl.Where(x => x.jenisBayar == JenisBayar.DP);
        //    var listDPBidang = listDPBidangs.SelectMany(x => x.subdetails).Where(x => x.keyPersil == persil.key).ToList();
        //    if (listDPBidang != null)
        //        DPBidang = listDPBidang.Sum(x => x.Jumlah);

        //    //var listLunasBidang = details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.keyPersil == persil.key);
        //    var listLunasBidangs = dtl.Where(x => x.jenisBayar == JenisBayar.Lunas);
        //    var listLunasBidang = listLunasBidangs.SelectMany(x => x.subdetails).Where(x => x.keyPersil == persil.key).ToList();
        //    if (listLunasBidang != null)
        //        pelunasan = listLunasBidang.Sum(x => x.Jumlah);

        //    //var SisaLunas = totalBayar - ((jumlahUTJ / allbidang) + (DPNoBidang / allbidang) + DPBidang);
        //    var realSisaLunas = totalBayar - (realUTJ + realDP + DPBidang + pelunasan + realLainnya + realMandor + realpph + validasiPPH + mandor + pembatalanNIB
        //        + balikNama + gantiBlanko + kompensasi + pajaklama + pajakwaris + tunggakanPBB + biayaLainnya);
        //    var totalPembayaran = (realUTJ + realDP + DPBidang + pelunasan + realLainnya);

        //    return (Convert.ToDouble(realSisaLunas), totalPembayaran, pph);
        //}

        public (double sisalunas, double totalpembayaran, double pphx) SisaPelunasan2(Persil persil)
        {
            (double totalBayar, double realUTJ, double realLainnya, double realMandor, double realDP, double DPBidang, double? luasdiBayar, double pelunasan, double pph21, double pph, double realpph, double validasiPPH,
                double? luasNIB, double? luasDiBayar, double? biayaLainnya) = (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            (double? mandor, double? pembatalanNIB, double? balikNama, double? gantiBlanko, double? kompensasi, double? pajaklama, double? pajakwaris, double? tunggakanPBB) =
                (persil.mandor == null ? 0 : (persil.mandor < 0 ? 0 : persil.mandor), persil.pembatalanNIB == null ? 0 : (persil.pembatalanNIB < 0 ? 0 : persil.pembatalanNIB),
                persil.BiayaBN == null ? 0 : (persil.BiayaBN < 0 ? 0 : persil.BiayaBN), persil.gantiBlanko == null ? 0 : (persil.gantiBlanko < 0 ? 0 : persil.gantiBlanko),
                persil.kompensasi == null ? 0 : (persil.kompensasi < 0 ? 0 : persil.kompensasi), persil.pajakLama == null ? 0 : (persil.pajakLama < 0 ? 0 : persil.pajakLama),
                persil.pajakWaris == null ? 0 : (persil.pajakWaris < 0 ? 0 : persil.pajakWaris), persil.tunggakanPBB == null ? 0 : (persil.tunggakanPBB < 0 ? 0 : persil.tunggakanPBB));

            var totalSudahdiBayar = details.Where(x => x.invalid != true).SelectMany(x => x.subdetails)
                .Where(x => x.keyPersil == persil.key).Select(x => x.Jumlah).Sum();

            if (persil.biayalainnya != null)
                biayaLainnya = persil.biayalainnya.Where(x => x.fgLainnya == true).Select(x => x.nilai).Sum();

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;

            luasDiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = IsLunas(persil.key);

            if (isLunas)
                luasdiBayar = persil.luasPelunasan ?? luasDiBayar;
            else if (persil.luasFix != true)
                luasdiBayar = luasNIB == 0 ? luasDiBayar : luasNIB;
            else
                luasdiBayar = luasDiBayar;

            var satuan = persil.basic.current.satuan == null ? 0 : persil.basic.current.satuan;
            var satuanAkte = persil.basic.current.satuanAkte == null ? 0 : persil.basic.current.satuanAkte;

            totalBayar = Convert.ToDouble(luasdiBayar) * Convert.ToDouble(satuan);
            var totalBayarAkte = Convert.ToDouble(luasdiBayar) * Convert.ToDouble(satuanAkte);

            pph21 = (totalBayar * 2.5) / 100;
            var pph21akte = (totalBayarAkte * 2.5) / 100;
            pph = pph21akte == 0 ? pph21 : pph21akte;

            if (persil.pph21 == true)
            {
                realpph = pph; // Jika ditanggung penjual maka akan mengurangi pelunasan
            }

            if (persil.ValidasiPPH == true)
                validasiPPH = Convert.ToDouble(persil.ValidasiPPHValue);

            var dtl = details.Where(x => x.invalid != true).ToArray();

            var realSisaLunas = totalBayar - (totalSudahdiBayar + realpph + validasiPPH + mandor + pembatalanNIB + balikNama + gantiBlanko + kompensasi + pajaklama + pajakwaris + tunggakanPBB + biayaLainnya);

            return (Convert.ToDouble(realSisaLunas), totalSudahdiBayar, pph);
        }

        public double AllLuas(LandropePayContext context)
        {
            (double _luasdiBayar, double allLuas, double _luas) = (0, 0, 0);

            foreach (var item in bidangs)
            {
                var bidangLunas = IsLunas(item.keyPersil);

                if (bidangLunas == false)
                {
                    var _persil = persil(context, item.keyPersil);
                    var _luasNIB = PersilHelper.GetLuasBayar(_persil);

                    _luas = _persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(_persil.basic.current.luasDibayar);

                    if (_persil.luasFix != true)
                        _luasdiBayar = _luasNIB == 0 ? _luas : _luasNIB;
                    else
                        _luasdiBayar = _luas;

                    allLuas = allLuas + _luasdiBayar;
                }
            }
            return allLuas;
        }

        public double AllHarga(LandropePayContext context, string[] persilKeys)
        {
            double luasdiBayar = 0;
            double allHarga = 0;
            double satuan = 0;
            double hargaTotal = 0;
            double luasNIB = 0;
            double luas = 0;

            var keys = string.Join(',', persilKeys.Select(k => $"'{k}'"));
            var persils = context.GetCollections(new Persil(), "persils_v2", $"<key : <$in : [{keys}]>>".MongoJs(), "{_id:0}").ToList();

            foreach (var persil in persils)
            {
                var bidangLunas = IsLunas(persil.key);

                if (bidangLunas == false)
                {
                    luasNIB = PersilHelper.GetLuasBayar(persil);

                    luas = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);
                    satuan = persil.basic.current.satuan == null ? 0 : Convert.ToDouble(persil.basic.current.satuan);

                    luasdiBayar = (persil.luasFix ?? true) ? luas : (luasNIB == 0 ? luas : luasNIB);
                    hargaTotal = luasdiBayar * satuan;

                    allHarga = allHarga + hargaTotal;
                }
            }

            return allHarga;
        }

        public double AllHarga(LandropePayContext context, Persil[] persils)
        {
            double luasdiBayar = 0;
            double allHarga = 0;
            double satuan = 0;
            double hargaTotal = 0;
            double luasNIB = 0;
            double luas = 0;

            foreach (var persil in persils)
            {
                var bidangLunas = IsLunas(persil.key);

                if (bidangLunas == false)
                {
                    luasNIB = PersilHelper.GetLuasBayar(persil);

                    luas = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);
                    satuan = persil.basic.current.satuan == null ? 0 : Convert.ToDouble(persil.basic.current.satuan);

                    luasdiBayar = (persil.luasFix ?? true) ? luas : (luasNIB == 0 ? luas : luasNIB);
                    hargaTotal = luasdiBayar * satuan;

                    allHarga = allHarga + hargaTotal;
                }
            }

            return allHarga;
        }

        public bool IsLunas(string keyPersil)
        {
            return details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true).SelectMany(x => x.subdetails).Any(d => d.keyPersil == keyPersil);
        }

        public double Deposit(Persil persil)
        {

            (double? mandor,
                double? pembatalanNIB,
                double? balikNama,
                double? gantiBlanko,
                double? kompensasi,
                double? pajaklama,
                double? pajakwaris,
                double? tunggakanPBB,
                double? pph)
                =
           (persil.mandor == null ? 0 : (persil.mandor < 0 ? 0 : persil.mandor),
                persil.pembatalanNIB == null ? 0 : (persil.pembatalanNIB < 0 ? 0 : persil.pembatalanNIB),
                persil.BiayaBN == null ? 0 : (persil.BiayaBN < 0 ? 0 : persil.BiayaBN),
                persil.gantiBlanko == null ? 0 : (persil.gantiBlanko < 0 ? 0 : persil.gantiBlanko),
                persil.kompensasi == null ? 0 : (persil.kompensasi < 0 ? 0 : persil.kompensasi),
                persil.pajakLama == null ? 0 : (persil.pajakLama < 0 ? 0 : persil.pajakLama),
                persil.pajakWaris == null ? 0 : (persil.pajakWaris < 0 ? 0 : persil.pajakWaris),
                persil.tunggakanPBB == null ? 0 : (persil.tunggakanPBB < 0 ? 0 : persil.tunggakanPBB),
                persil.pph21 == true ? 1 : 0);

            var biayaLain = persil.biayalainnya.Count() == 0 ? 0 : persil.biayalainnya.Where(x => x.fgLainnya == true).Sum(x => x.nilai);

            return (double)(mandor + pembatalanNIB + balikNama + gantiBlanko + kompensasi + pajaklama + pajakwaris + tunggakanPBB + pph + biayaLain);
        }
    }
    public class BayarDtl
    {
        public string key { get; set; }
        public bool? invalid { get; set; }
        public string instkey { get; set; }
        public string? keyPersil { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public DateTime? tglBayar { get; set; }
        public double Jumlah { get; set; }
        public double? persentase { get; set; }
        public string? noMemo { get; set; }
        public DateTime? tglPenyerahan { get; set; }
        public string contactPerson { get; set; }
        public string noTlpCP { get; set; }
        public string tembusan { get; set; }
        public string note { get; set; }
        public string memoSigns { get; set; }
        public string memoTo { get; set; }
        public ProposionalBayar? fgProposional { get; set; } //true : berdasarkan luas, false : Bagi rata
        public BayarSubDtl[] subdetails { get; set; } = new BayarSubDtl[0];
        public Giro[] giro { get; set; } = new Giro[0];
        public Reason[] reasons { get; set; } = new Reason[0];
        public deals[] deals { get; set; } = new deals[0];

        //public DateTime? createDate => instance.Core.nodes.OfType<GraphBegin>().FirstOrDefault()?.Taken.on;
        //public DateTime
        public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);

        [BsonIgnore]
        GraphMainInstance _instance = null;

        [BsonIgnore]
        public GraphMainInstance instance
        {
            get
            {
                if (_instance == null)
                    _instance = graph?.Get(instkey).GetAwaiter().GetResult();
                return _instance;
            }
        }

        GraphHostConsumer graph => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

        public void CreateGraphInstance(user user, JenisBayar jenisBayar)
        {
            if (instkey != null)
                return;

            var type = jenisBayar == JenisBayar.Lunas ? ToDoType.Payment_Land_Lunas : ToDoType.Payment_Land_NoLunas;
            instkey = graph.Create(user, type).GetAwaiter().GetResult()?.key;
        }

        public void CreateGraphInstance(user user, JenisBayar jenisBayar, GraphHostSvc ghost)
        {
            if (instkey != null)
                return;

            var type = jenisBayar == JenisBayar.Lunas ? ToDoType.Payment_Land_Lunas : ToDoType.Payment_Land_NoLunas;
            instkey = ghost.Create(user, type)?.key;
        }

        public BayarDtl(user user) : this()
        {
            //CreateGraphInstance(user);
        }

        public BayarDtl()
        {
            key = mongospace.MongoEntity.MakeKey;
            tglBayar = null;
        }

        public void AddSubDetail(BayarSubDtl byrSubDtl)
        {
            var lst = new List<BayarSubDtl>();
            if (subdetails != null)
                lst = subdetails.ToList();

            lst.Add(byrSubDtl);
            subdetails = lst.ToArray();
        }

        public void AddDeals(deals deal)
        {
            var lst = new List<deals>();
            if (deals != null)
                lst = deals.ToList();

            lst.Add(deal);
            deals = lst.ToArray();
        }

        public void AddGiro(Giro dtlgiro)
        {
            var lst = new List<Giro>();
            if (giro != null)
                lst = giro.ToList();

            lst.Add(dtlgiro);
            giro = lst.ToArray();
        }

        public void fromCore(BayarDtlCore core)
        {
            (keyPersil, jenisBayar, Jumlah, persentase, noMemo, tglPenyerahan, note, fgProposional) =
                (core.keyPersil, core.jenisBayar, core.Jumlah, core.Persentase, core.noMemo, core.TanggalPenyerahan, core.Note, core.fgProposional);
        }

        public void fillStaticInfo(StaticCollection staticCollections)
        {
            (memoSigns, memoTo, tembusan, contactPerson, noTlpCP)
                =
            (staticCollections.memoSign, staticCollections.memoTo,
             staticCollections.tembusan, staticCollections.contactPersonName,
             staticCollections.contactPersonPhone
            );
        }

        public void fromCore(string memo)
        {
            (noMemo) =
                (memo);
        }

        public void Approved()
        {
            tglBayar = DateTime.Now;
        }

        public void SelisihPembulatan()
        {
            //hitung selisih pembulatan subdetails
            var j = this.Jumlah;
            var s = this.subdetails.Sum(x => x.Jumlah);

            //kalau subs lebih besar dari jumlah
            if (s > j)
            {
                var selisih = s - j;
                var lastSub = this.subdetails.LastOrDefault();

                var subs = this.subdetails.ToList();
                subs.Remove(lastSub);

                lastSub.Jumlah = lastSub.Jumlah - selisih;

                subs.Add(lastSub);
                this.subdetails = subs.ToArray();
            }
            //kalau jumlah lebih besar dari subs
            else if (j > s)
            {
                if (s > 0)
                {
                    var selisih = j - s;
                    var lastSub = this.subdetails.LastOrDefault();

                    var subs = this.subdetails.ToList();
                    subs.Remove(lastSub);

                    lastSub.Jumlah = lastSub.Jumlah + selisih;

                    subs.Add(lastSub);
                    this.subdetails = subs.ToArray();
                }
            }
        }

        public void Abort()
        {
            invalid = true;
        }

        public void AddReason(Reason reason)
        {
            var lst = new List<Reason>();
            if (reasons != null)
                lst = reasons.ToList();

            lst.Add(reason);
            reasons = lst.ToArray();
        }

        public BayarDtlView toView(LandropePayContext contextes, GraphHostConsumer ghost)
        {
            var view = new BayarDtlView();
            var persil = this.persil(contextes);
            //var gMain = ghost.Get(instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

            var LastStatus = instance?.lastState?.state.AsStatus();
            var createdDate = instance?.states.LastOrDefault(s => s.state == ToDoState.created_)?.time;

            (view.key, view.keyPersil, view.idBidang, view.jenisBayar, view.Jml, view.laststatus, view.created, view.AlasHak) =
                (key, keyPersil,
                persil == null ? "" : persil.IdBidang,
                (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                Jumlah, LastStatus, createdDate,
                persil == null ? "" : persil.basic.current.surat.nomor);

            return view;
        }

        public BayarDtlView toView(LandropePayContext contextes, Bayar byr, StaticCollection staticCollections, List<Persil> persils, (string inskey, ToDoState status, DateTime createDate)[] instances)
        {
            var view = new BayarDtlView();
            var persil = persils.FirstOrDefault(x => x.key == keyPersil);

            var main = instances.FirstOrDefault(x => x.inskey == instkey);
            var LastStatus = main.status.AsStatus();
            var createdDate = main.createDate;

            var bidangs = string.Empty;
            string _tembusan = string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan;
            string _memoSign = string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns;
            string _contactPerson = string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson;
            string _noTlpCP = string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP;

            if (string.IsNullOrEmpty(keyPersil))
            {
                if (byr.bidangs.Count() > subdetails.Count())
                    bidangs = "...";
                else
                    bidangs = "...";
            }
            else if (!string.IsNullOrEmpty(keyPersil))
                if (persil != null)
                    bidangs = persil.IdBidang;
                else
                    bidangs = "...";

            var lstGiro = new List<GiroCore>();

            for (int i = 0; i < giro.Count(); i++)
            {
                var girocore = new GiroCore();
                girocore.key = giro[i].key;
                girocore.Jenis = giro[i].jenis;
                girocore.BankPenerima = giro[i].bankPenerima;
                girocore.NamaPenerima = giro[i].namaPenerima;
                girocore.AccountPenerima = giro[i].accPenerima;
                girocore.Nominal = giro[i].nominal;
                lstGiro.Add(girocore);
            }

            var lstReason = new List<ReasonCore>();

            foreach (var item in reasons)
            {
                var reason = new ReasonCore
                {
                    tanggal = item.tanggal,
                    description = item.description,
                    flag = item.flag,
                    keyCreator = item.keyCreator,
                    privs = item.privs,
                    state = item.state,
                    state_ = item.state.AsStatus(),
                    creator = contextes.users.FirstOrDefault(y => y.key == item.keyCreator)?.FullName
                };

                lstReason.Add(reason);
            }

            (view.key, view.keyPersil, view.idBidang, view.jenisBayar, view.Jml, view.laststatus, view.created, view.AlasHak, view.noMemo, view.instkey, view.tglPenyerahan
                , view.ContactPerson, view.noTlpCP, view.tembusan, view.note, view.memoSigns, view.giro, view.reasons) =
                (key, keyPersil, bidangs,
                (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                Jumlah, LastStatus, Convert.ToDateTime(createdDate).ToLocalTime(),
                persil == null ? "" : persil.basic.current.surat.nomor, noMemo, instkey,
                tglPenyerahan, _contactPerson, _noTlpCP,
                _tembusan, note, _memoSign, lstGiro.ToArray(), lstReason.ToArray());

            return view;
        }

        public BayarDtlViewExt2 toView(LandropePayContext contextes, Bayar byr, List<BayarDtlBidangViewSelected> bidang, StaticCollection staticCollections)
        {
            var view = new BayarDtlViewExt2();
            var lstGiro = new List<GiroCore>();

            for (int i = 0; i < giro.Count(); i++)
            {
                var girocore = new GiroCore();
                girocore.key = giro[i].key;
                girocore.Jenis = giro[i].jenis;
                girocore.BankPenerima = giro[i].bankPenerima;
                girocore.NamaPenerima = giro[i].namaPenerima;
                girocore.AccountPenerima = giro[i].accPenerima;
                girocore.Nominal = giro[i].nominal;
                lstGiro.Add(girocore);
            }

            string _tembusan = string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan;
            string _memoSign = string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns;
            string _contactPerson = string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson;
            string _noTlpCP = string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP;

            (view.key, view.keyPersil, view.jenisBayar, view.Jml,
             view.noMemo, view.tembusan, view.memoSigns, view.tglPenyerahan,
             view.ContactPerson, view.noTlpCP, view.note, view.giro,
             view.bidangs
            )
                =
            (key, keyPersil, jenisBayar, Jumlah,
             noMemo, _tembusan, _memoSign, tglPenyerahan,
             _contactPerson, _noTlpCP, note, lstGiro.ToArray(),
             bidang.ToArray()
            );

            return view;
        }

        public DtltoBundle toBundle(GraphHostConsumer ghost)
        {
            var toBundle = new DtltoBundle();

            var gMain = ghost.Get(instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
            var state = gMain.lastState?.state;

            (toBundle.key, toBundle.state) = (key, state);

            return toBundle;
        }
    }
    public class BayarDtlDeposit
    {
        public string key { get; set; }
        public bool? invalid { get; set; }
        public string instkey { get; set; }
        public string? keyPersil { get; set; }
        public JenisBayar jenisBayar { get; set; }
        public DateTime? tglBayar { get; set; }
        public double Jumlah { get; set; }
        public string? noMemo { get; set; }
        public DateTime? tglPenyerahan { get; set; }
        public string contactPerson { get; set; }
        public string noTlpCP { get; set; }
        public string tembusan { get; set; }
        public string note { get; set; }
        public string memoSigns { get; set; }
        public string memoTo { get; set; }
        public BayarSubDtlDeposit[] subdetails { get; set; } = new BayarSubDtlDeposit[0];
        public Giro[] giro { get; set; } = new Giro[0];
        public Reason[] reasons { get; set; } = new Reason[0];

        //public DateTime? createDate => instance.Core.nodes.OfType<GraphBegin>().FirstOrDefault()?.Taken.on;
        //public DateTime
        public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);

        [BsonIgnore]
        GraphMainInstance _instance = null;

        [BsonIgnore]
        public GraphMainInstance instance
        {
            get
            {
                if (_instance == null)
                    _instance = graph?.Get(instkey).GetAwaiter().GetResult();
                return _instance;
            }
        }
        GraphHostConsumer graph => ContextService.services.GetService<IGraphHostConsumer>() as GraphHostConsumer;
        public void CreateGraphInstance(user user, JenisBayar jenisBayar)
        {
            if (instkey != null)
                return;

            var type = ToDoType.Payment_Land_NoLunas;
            instkey = graph.Create(user, type).GetAwaiter().GetResult()?.key;
        }

        public void CreateGraphInstance(user user, JenisBayar jenisBayar, GraphHostSvc ghost)
        {
            if (instkey != null)
                return;

            var type = ToDoType.Payment_Land_NoLunas;
            instkey = ghost.Create(user, type)?.key;
        }
        public BayarDtlDeposit(user user) : this()
        {
            //CreateGraphInstance(user);
        }

        public BayarDtlDeposit()
        {
            key = mongospace.MongoEntity.MakeKey;
            tglBayar = null;
        }

        public void AddSubDetail(BayarSubDtlDeposit byrSubDtl)
        {
            var lst = new List<BayarSubDtlDeposit>();
            if (subdetails != null)
                lst = subdetails.ToList();

            lst.Add(byrSubDtl);
            subdetails = lst.ToArray();
        }

        public void AddGiro(Giro dtlgiro)
        {
            var lst = new List<Giro>();
            if (giro != null)
                lst = giro.ToList();

            lst.Add(dtlgiro);
            giro = lst.ToArray();
        }
        public void fromCore(BayarDtlDepositCore core)
        {
            (keyPersil, jenisBayar, Jumlah, noMemo, tglPenyerahan, note) =
                (core.keyPersil, core.jenisBayar, core.Jumlah, core.noMemo, core.TanggalPenyerahan, core.Note);
        }

        public void fillStaticInfo(StaticCollection staticCollections)
        {
            (memoSigns, memoTo, tembusan, contactPerson, noTlpCP)
                =
            (staticCollections.memoSign, staticCollections.memoTo,
             staticCollections.tembusan, staticCollections.contactPersonName,
             staticCollections.contactPersonPhone
            );
        }

        public void fromCore(string memo)
        {
            (noMemo) =
                (memo);
        }

        public void Approved()
        {
            tglBayar = DateTime.Now;
        }

        public void Abort()
        {
            invalid = true;
        }

        public void AddReason(Reason reason)
        {
            var lst = new List<Reason>();
            if (reasons != null)
                lst = reasons.ToList();

            lst.Add(reason);
            reasons = lst.ToArray();
        }

        public BayarDtlView toView(LandropePayContext contextes, GraphHostConsumer ghost)
        {
            var view = new BayarDtlView();
            var persil = this.persil(contextes);
            var gMain = ghost.Get(instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();

            var LastStatus = gMain.lastState?.state.AsStatus();
            var createdDate = gMain.states.LastOrDefault(s => s.state == ToDoState.created_)?.time;

            (view.key, view.keyPersil, view.idBidang, view.jenisBayar, view.Jml, view.laststatus, view.created, view.AlasHak) =
                (key, keyPersil,
                persil == null ? "" : persil.IdBidang,
                (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                Jumlah, LastStatus, createdDate,
                persil == null ? "" : persil.basic.current.surat.nomor);

            return view;
        }

        public BayarDtlDepositView toView(LandropePayContext contextes, Bayar byr, StaticCollection staticCollections,
            List<Persil> persils, (string inskey, ToDoState status, DateTime createDate)[] instances)
        {
            var view = new BayarDtlDepositView();
            var persil = persils.FirstOrDefault(x => x.key == keyPersil);

            var main = instances.FirstOrDefault(x => x.inskey == instkey);
            var LastStatus = main.status.AsStatus();
            var createdDate = main.createDate;

            var bidangs = string.Empty;
            string _tembusan = string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan;
            string _memoSign = string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns;
            string _contactPerson = string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson;
            string _noTlpCP = string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP;

            if (string.IsNullOrEmpty(keyPersil))
            {
                if (byr.bidangs.Count() > subdetails.Count())
                    bidangs = "...";
                else
                    bidangs = "...";
            }
            else if (!string.IsNullOrEmpty(keyPersil))
                if (persil != null)
                    bidangs = persil.IdBidang;
                else
                    bidangs = "...";

            var lstGiro = new List<GiroCore>();

            for (int i = 0; i < giro.Count(); i++)
            {
                var girocore = new GiroCore();
                girocore.key = giro[i].key;
                girocore.Jenis = giro[i].jenis;
                girocore.BankPenerima = giro[i].bankPenerima;
                girocore.NamaPenerima = giro[i].namaPenerima;
                girocore.AccountPenerima = giro[i].accPenerima;
                girocore.Nominal = giro[i].nominal;
                lstGiro.Add(girocore);
            }

            var lstReason = new List<ReasonCore>();

            foreach (var item in reasons)
            {
                var reason = new ReasonCore
                {
                    tanggal = item.tanggal,
                    description = item.description,
                    flag = item.flag,
                    keyCreator = item.keyCreator,
                    privs = item.privs,
                    state = item.state,
                    state_ = item.state.AsStatus(),
                    creator = contextes.users.FirstOrDefault(y => y.key == item.keyCreator)?.FullName
                };

                lstReason.Add(reason);
            }

            var lstSubdetails = new List<BayarSubDtlDepositView>();

            foreach (var item in subdetails)
            {
                var lstBiayaLainnya = new List<BiayalainnyaCore>();
                var subdetail = new BayarSubDtlDepositView
                {
                    key = item.key,
                    keyPersil = item.keyPersil,
                    BiayaBN = item.BiayaBN,
                    gantiBlanko = item.gantiBlanko,
                    Jumlah = item.Jumlah,
                    kompensasi = item.kompensasi,
                    mandor = item.mandor,
                    pajakLama = item.pajakLama,
                    pajakWaris = item.pajakWaris,
                    pembatalanNIB = item.pembatalanNIB,
                    pph21 = item.pph21,
                    tunggakanPBB = item.tunggakanPBB,
                    ValidasiPPH = item.ValidasiPPH
                };

                if (item.biayalainnya.Count() > 0)
                {
                    foreach (var bl in item.biayalainnya)
                    {
                        var biayaLain = new BiayalainnyaCore
                        {
                            identity = bl.identity,
                            nilai = bl.nilai,
                            fglainnya = bl.fgLainnya
                        };

                        lstBiayaLainnya.Add(biayaLain);
                    }

                    subdetail.biayaLainnya = lstBiayaLainnya.ToArray();
                }

                lstSubdetails.Add(subdetail);
            }

            (view.key,
                view.keyPersil,
                view.idBidang,
                view.jenisBayar,
                view.Jml,
                view.laststatus,
                view.created,
                view.AlasHak,
                view.Pemilik,
                view.noMemo,
                view.instkey,
                view.tglPenyerahan,
                view.ContactPerson,
                view.noTlpCP,
                view.tembusan,
                view.note,
                view.memoSigns,
                view.giro,
                view.reasons,
                view.subdetails) =
                (key,
                keyPersil,
                bidangs,
                (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                Jumlah,
                LastStatus,
                Convert.ToDateTime(createdDate).ToLocalTime(),
                persil == null ? "..." : persil.basic.current?.surat?.nomor,
                persil == null ? "..." : persil.basic.current?.pemilik,
                noMemo,
                instkey,
                tglPenyerahan,
                _contactPerson,
                _noTlpCP,
                _tembusan,
                note,
                _memoSign,
                lstGiro.ToArray(),
                lstReason.ToArray(),
                lstSubdetails.ToArray());

            return view;
        }

        public BayarDtlDepositSelected toView(LandropePayContext contextes, StaticCollection staticCollections, string Tkey)
        {
            var view = new BayarDtlDepositSelected();

            string _tembusan = string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan;
            string _memoSign = string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns;
            string _contactPerson = string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson;
            string _noTlpCP = string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP;

            var lstGiro = new List<GiroCore>();

            for (int i = 0; i < giro.Count(); i++)
            {
                var girocore = new GiroCore();
                girocore.key = giro[i].key;
                girocore.Jenis = giro[i].jenis;
                girocore.BankPenerima = giro[i].bankPenerima;
                girocore.NamaPenerima = giro[i].namaPenerima;
                girocore.AccountPenerima = giro[i].accPenerima;
                girocore.Nominal = giro[i].nominal;
                lstGiro.Add(girocore);
            }

            var lstSubdetails = new List<BayarSubDtlDepositSelected>();

            foreach (var item in subdetails)
            {
                var persil = item.persil(contextes);
                var lstBiayaLainnya = new List<BiayalainnyaCore>();
                var subdetail = new BayarSubDtlDepositSelected
                {
                    key = item.key,
                    keyPersil = item.keyPersil,
                    IdBidang = persil?.IdBidang,
                    luasDiBayar = persil?.basic?.current?.luasDibayar,
                    pemilik = string.IsNullOrEmpty(persil?.basic?.current?.pemilik) ? persil?.basic?.current?.surat?.nama : persil?.basic?.current?.pemilik,
                    alashak = persil?.basic?.current?.surat?.nomor,
                    BiayaBN = item.BiayaBN,
                    gantiBlanko = item.gantiBlanko,
                    Jumlah = item.Jumlah,
                    kompensasi = item.kompensasi,
                    mandor = item.mandor,
                    pajakLama = item.pajakLama,
                    pajakWaris = item.pajakWaris,
                    pembatalanNIB = item.pembatalanNIB,
                    pph21 = item.pph21,
                    tunggakanPBB = item.tunggakanPBB,
                    ValidasiPPH = item.ValidasiPPH,
                    vBiayaBN = item.vBiayaBN,
                    vGantiBlanko = item.vGantiBlanko,
                    vKompensasi = item.vKompensasi,
                    vMandor = item.vMandor,
                    vPajakLama = item.vPajakLama,
                    vPajakWaris = item.vPajakWaris,
                    vPembatalanNIB = item.vPembatalanNIB,
                    vpph21 = item.vpph21,
                    vTunggakanPBB = item.vTunggakanPBB,
                    vValidasiPPH = item.vValidasiPPH
                };

                if (item.biayalainnya.Count() > 0)
                {
                    foreach (var bl in item.biayalainnya)
                    {
                        var biayaLain = new BiayalainnyaCore
                        {
                            identity = bl.identity,
                            nilai = bl.nilai,
                            fglainnya = bl.fgLainnya
                        };

                        lstBiayaLainnya.Add(biayaLain);
                    }

                    subdetail.biayaLainnya = lstBiayaLainnya.ToArray();
                }

                lstSubdetails.Add(subdetail);
            }

            (view.Tkey,
                view.key,
                view.keyPersil,
                view.jenisBayar,
                view.Jumlah,
                view.noMemo,
                view.instkey,
                view.tglPenyerahan,
                view.contactPerson,
                view.noTlpCP,
                view.tembusan,
                view.note,
                view.memoSigns,
                view.giro,
                view.subdetails) =
                (Tkey,
                key,
                keyPersil,
                (Enum.TryParse<JenisBayar>(jenisBayar.ToString(), out JenisBayar stp) ? stp : default),
                Jumlah,
                noMemo,
                instkey,
                tglPenyerahan,
                _contactPerson,
                _noTlpCP,
                _tembusan,
                note,
                _memoSign,
                lstGiro.ToArray(),
                lstSubdetails.ToArray());

            return view;
        }

        public BayarDtlViewExt2 toView(LandropePayContext contextes, Bayar byr, List<BayarDtlBidangViewSelected> bidang, StaticCollection staticCollections)
        {
            var view = new BayarDtlViewExt2();
            var lstGiro = new List<GiroCore>();

            for (int i = 0; i < giro.Count(); i++)
            {
                var girocore = new GiroCore();
                girocore.key = giro[i].key;
                girocore.Jenis = giro[i].jenis;
                girocore.BankPenerima = giro[i].bankPenerima;
                girocore.NamaPenerima = giro[i].namaPenerima;
                girocore.AccountPenerima = giro[i].accPenerima;
                girocore.Nominal = giro[i].nominal;
                lstGiro.Add(girocore);
            }

            string _tembusan = string.IsNullOrEmpty(tembusan) ? staticCollections.tembusan : tembusan;
            string _memoSign = string.IsNullOrEmpty(memoSigns) ? staticCollections.memoSign : memoSigns;
            string _contactPerson = string.IsNullOrEmpty(contactPerson) ? staticCollections.contactPersonName : contactPerson;
            string _noTlpCP = string.IsNullOrEmpty(noTlpCP) ? staticCollections.contactPersonPhone : noTlpCP;

            (view.key, view.keyPersil, view.jenisBayar, view.Jml,
             view.noMemo, view.tembusan, view.memoSigns, view.tglPenyerahan,
             view.ContactPerson, view.noTlpCP, view.note, view.giro,
             view.bidangs
            )
                =
            (key, keyPersil, jenisBayar, Jumlah,
             noMemo, _tembusan, _memoSign, tglPenyerahan,
             _contactPerson, _noTlpCP, note, lstGiro.ToArray(),
             bidang.ToArray()
            );

            return view;
        }

        public DtltoBundle toBundle(GraphHostConsumer ghost)
        {
            var toBundle = new DtltoBundle();

            var gMain = ghost.Get(instkey).GetAwaiter().GetResult() ?? new GraphMainInstance();
            var state = gMain.lastState?.state;

            (toBundle.key, toBundle.state) = (key, state);

            return toBundle;
        }
    }
    public class BayarDtlBidang
    {
        public string key { get; set; }
        public string keyPersil { get; set; }

        public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);

        public BayarDtlBidangView toView(LandropePayContext context, Bayar byr, BayarSubDtlDeposit[] deposits, cmnItem[] ptsks)
        {
            var view = new BayarDtlBidangView();
            var deposit = new BayarSubDtlDepositSS();
            var persil = this.persil(context);
            var fgFix = persil.luasFix == null ? false : persil.luasFix;
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);
            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var biayalainnyas = new List<BiayalainnyaCore>();
            if (persil.biayalainnya != null)
            {
                foreach (var item in persil.biayalainnya)
                {
                    var lainnya = new BiayalainnyaCore();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fglainnya = item.fgLainnya;

                    biayalainnyas.Add(lainnya);
                }
            }

            var locations = context.GetVillage(persil.basic.current.keyDesa);

            var d = deposits.Where(x => x.keyPersil == persil.key).ToArray();
            if (d != null)
            {
                deposit.vpph21 = d.Where(x => x.vpph21 != null)?.Sum(x => x.vpph21);
                deposit.vValidasiPPH = d.Where(x => x.vValidasiPPH != null)?.Sum(x => x.vValidasiPPH);
                deposit.vMandor = d.Where(x => x.vMandor != null)?.Sum(x => x.vMandor);
                deposit.vPembatalanNIB = d.Where(x => x.vPembatalanNIB != null)?.Sum(x => x.vPembatalanNIB);
                deposit.vBiayaBN = d.Where(x => x.vBiayaBN != null)?.Sum(x => x.vBiayaBN);
                deposit.vGantiBlanko = d.Where(x => x.vGantiBlanko != null)?.Sum(x => x.vGantiBlanko);
                deposit.vKompensasi = d.Where(x => x.vKompensasi != null)?.Sum(x => x.vKompensasi);
                deposit.vPajakLama = d.Where(x => x.vPajakLama != null)?.Sum(x => x.vPajakLama);
                deposit.vPajakWaris = d.Where(x => x.vPajakWaris != null)?.Sum(x => x.vPajakWaris);
                deposit.vTunggakanPBB = d.Where(x => x.vTunggakanPBB != null)?.Sum(x => x.vTunggakanPBB);

                var bLain = d.SelectMany(x => x.biayalainnya).ToArray();

                if (persil.biayalainnya != null && persil.biayalainnya.Count() > 0)
                {
                    if (bLain != null && bLain.Count() > 0)
                    {
                        var listCore = new List<BiayalainnyaCore>();
                        foreach (var pbl in persil.biayalainnya)
                        {
                            var found = bLain.Where(x => x.identity == pbl.identity).GroupBy(x => (x.identity, x.fgLainnya), (y, z) =>
                                                                            new BiayalainnyaCore { identity = y.identity, fglainnya = y.fgLainnya, nilai = z.Sum(x => x.nilai) })
                                                                            .FirstOrDefault();

                            listCore.Add(found);
                        }

                        deposit.biayaLainnya = listCore.ToArray();
                    }
                }
            }

            (view.key,
                view.keyPersil,
                view.IdBidang,
                view.ptsk,
                view.en_state,
                view.en_proses,
                view.luasSurat,
                view.luasDibayar,
                view.luasInternal,
                view.luasNIB,
                view.luasNIBTemp,
                view.luasFix,
                view.Pemilik,
                view.AlasHak,
                view.NamaSurat,
                view.satuan,
                view.total,
                view.SisaLunas,
                view.noPeta,
                view.TotalPembayaran,
                view.fgPPH21,
                view.TotalPPH21,
                view.validasiPPH,
                view.TotalValidasiPPH21,
                view.isEdited,
                view.project,
                view.desa,
                view.Mandor,
                view.PembatalanNIB,
                view.BalikNama,
                view.GantiBlanko,
                view.Kompensasi,
                view.PajakLama,
                view.PajakWaris,
                view.TunggakanPBB,
                view.satuanAkta,
                view.biayalainnyas,
                view.deposit) =
                (key,
                persil.key,
                persil.IdBidang,
                ptsks.FirstOrDefault(x => x.key == persil.basic.current.keyPTSK)?.name,
                (Enum.TryParse<StatusBidang>(persil.en_state.ToString(), out StatusBidang stp) ? stp : default),
                (Enum.TryParse<JenisProses>(persil.basic.current.en_proses.ToString(), out JenisProses pro) ? pro : default),
                persil.basic.current.luasSurat,
                luas,
                persil.basic.current.luasInternal,
                luasNIB,
                persil.basic.current.luasNIBTemp,
                fgFix,
                persil.basic.current.pemilik,
                persil.basic.current.surat.nomor,
                persil.basic.current.surat.nama,
                persil.basic.current.satuan,
                (luas * persil.basic.current.satuan),
                sisaLunas,
                persil.basic.current.noPeta,
                totalPembayaran,
                persil.pph21,
                totalPPH,
                persil.ValidasiPPH,
                persil.ValidasiPPHValue == null ? 0 : persil.ValidasiPPHValue,
                persil.isEdited,
                locations.project.identity,
                locations.desa.identity,
                persil.mandor,
                persil.pembatalanNIB,
                persil.BiayaBN,
                persil.gantiBlanko,
                persil.kompensasi,
                persil.pajakLama,
                persil.pajakWaris,
                persil.tunggakanPBB,
                persil.basic.current.satuanAkte,
                biayalainnyas.ToArray(), deposit);

            return view;
        }

        public BayarDtlBidangView toView(LandropePayContext context, Bayar byr,
            (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] locations, cmnItem[] ptsks)
        {
            var view = new BayarDtlBidangView();
            var persil = this.persil(context);
            var fgFix = persil.luasFix == null ? false : persil.luasFix;
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);
            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var biayalainnyas = new List<BiayalainnyaCore>();
            if (persil.biayalainnya != null)
            {
                foreach (var item in persil.biayalainnya)
                {
                    var lainnya = new BiayalainnyaCore();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fglainnya = item.fgLainnya;

                    biayalainnyas.Add(lainnya);
                }
            }

            //var locations = context.GetVillage(persil.basic.current.keyDesa);

            (view.key,
                view.keyPersil,
                view.IdBidang,
                view.en_state,
                view.en_proses,
                view.luasSurat,
                view.luasDibayar,
                view.luasInternal,
                view.luasNIB,
                view.luasNIBTemp,
                view.luasFix,
                view.Pemilik,
                view.AlasHak,
                view.NamaSurat,
                view.satuan,
                view.total,
                view.SisaLunas,
                view.noPeta,
                view.TotalPembayaran,
                view.fgPPH21,
                view.TotalPPH21,
                view.validasiPPH,
                view.TotalValidasiPPH21,
                view.isEdited,
                view.project,
                view.desa,
                view.ptsk,
                view.Mandor,
                view.PembatalanNIB,
                view.BalikNama,
                view.GantiBlanko,
                view.Kompensasi,
                view.PajakLama,
                view.PajakWaris,
                view.TunggakanPBB,
                view.satuanAkta,
                view.biayalainnyas) =
                (key,
                persil.key,
                persil.IdBidang,
                (Enum.TryParse<StatusBidang>(persil.en_state.ToString(), out StatusBidang stp) ? stp : default),
                (Enum.TryParse<JenisProses>(persil.basic.current.en_proses.ToString(), out JenisProses pro) ? pro : default),
                persil.basic.current.luasSurat,
                luas,
                persil.basic.current.luasInternal,
                luasNIB,
                persil.basic.current.luasNIBTemp,
                fgFix,
                persil.basic.current.pemilik,
                persil.basic.current.surat.nomor,
                persil.basic.current.surat.nama,
                persil.basic.current.satuan,
                (luas * persil.basic.current.satuan),
                sisaLunas,
                persil.basic.current.noPeta,
                totalPembayaran,
                persil.pph21,
                persil.pph21 == true ? totalPPH : 0,
                persil.ValidasiPPH,
                persil.ValidasiPPHValue == null ? 0 : persil.ValidasiPPHValue,
                persil.isEdited,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).project.identity,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).desa.identity,
                ptsks.FirstOrDefault(x => x.key == persil.basic.current.keyPTSK)?.name,
                persil.mandor < 0 ? 0 : persil.mandor,
                persil.pembatalanNIB < 0 ? 0 : persil.pembatalanNIB,
                persil.BiayaBN < 0 ? 0 : persil.BiayaBN,
                persil.gantiBlanko < 0 ? 0 : persil.gantiBlanko,
                persil.kompensasi < 0 ? 0 : persil.kompensasi,
                persil.pajakLama < 0 ? 0 : persil.pajakLama,
                persil.pajakWaris < 0 ? 0 : persil.pajakWaris,
                persil.tunggakanPBB < 0 ? 0 : persil.tunggakanPBB,
                persil.basic.current.satuanAkte,
                biayalainnyas.ToArray());

            return view;
        }

        public BayarDtlBidangView toView(LandropePayContext context, Bayar byr, BayarSubDtlDeposit[] deposits,
            (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] locations, cmnItem[] ptsks)
        {
            var view = new BayarDtlBidangView();
            var persil = this.persil(context);
            var fgFix = persil.luasFix == null ? false : persil.luasFix;
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);
            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var subs = deposits.Where(x => x.keyPersil == persil.key).ToList();

            var biayalainnyas = new List<BiayalainnyaCore>();
            if (persil.biayalainnya != null)
            {
                foreach (var item in persil.biayalainnya)
                {
                    if (subs != null)
                    {
                        var sbl = subs.SelectMany(x => x.biayalainnya).Where(x => x.identity == item.identity).ToList();

                        var lainnya = new BiayalainnyaCore();
                        lainnya.identity = item.identity;
                        lainnya.nilai = (item.nilai - (sbl != null ? (sbl.Sum(x => x.nilai) ?? 0) : 0));
                        lainnya.fglainnya = item.fgLainnya;

                        biayalainnyas.Add(lainnya);
                    }
                    else
                    {
                        var lainnya = new BiayalainnyaCore();
                        lainnya.identity = item.identity;
                        lainnya.nilai = item.nilai;
                        lainnya.fglainnya = item.fgLainnya;

                        biayalainnyas.Add(lainnya);
                    }
                }
            }

            (view.key,
                view.keyPersil,
                view.IdBidang,
                view.en_state,
                view.en_proses,
                view.luasSurat,
                view.luasDibayar,
                view.luasInternal,
                view.luasNIB,
                view.luasNIBTemp,
                view.luasFix,
                view.Pemilik,
                view.AlasHak,
                view.NamaSurat,
                view.satuan,
                view.total,
                view.SisaLunas,
                view.noPeta,
                view.TotalPembayaran,
                view.fgPPH21,
                view.TotalPPH21,
                view.validasiPPH,
                view.TotalValidasiPPH21,
                view.isEdited,
                view.project,
                view.desa,
                view.ptsk,
                view.Mandor,
                view.PembatalanNIB,
                view.BalikNama,
                view.GantiBlanko,
                view.Kompensasi,
                view.PajakLama,
                view.PajakWaris,
                view.TunggakanPBB,
                view.satuanAkta,
                view.biayalainnyas) =
                (key,
                persil.key,
                persil.IdBidang,
                (Enum.TryParse<StatusBidang>(persil.en_state.ToString(), out StatusBidang stp) ? stp : default),
                (Enum.TryParse<JenisProses>(persil.basic.current.en_proses.ToString(), out JenisProses pro) ? pro : default),
                persil.basic.current.luasSurat,
                luas,
                persil.basic.current.luasInternal,
                luasNIB,
                persil.basic.current.luasNIBTemp,
                fgFix,
                persil.basic.current.pemilik,
                persil.basic.current.surat.nomor,
                persil.basic.current.surat.nama,
                persil.basic.current.satuan,
                (luas * persil.basic.current.satuan),
                sisaLunas,
                persil.basic.current.noPeta,
                totalPembayaran,
                persil.pph21,
                persil.pph21 == true ? (totalPPH - (subs != null ? subs.Sum(x => x.vpph21) : 0)) : 0,
                persil.ValidasiPPH,
                persil.ValidasiPPHValue == null ? 0 : (persil.ValidasiPPHValue - (subs != null ? subs.Sum(x => x.vValidasiPPH) : 0)),
                persil.isEdited,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).project.identity,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).desa.identity,
                ptsks.FirstOrDefault(x => x.key == persil.basic.current.keyPTSK)?.name,
                persil.mandor < 0 ? 0 : (persil.mandor - (subs != null ? (subs.Sum(x => x.vMandor) ?? 0) : 0)),
                persil.pembatalanNIB < 0 ? 0 : (persil.pembatalanNIB - (subs != null ? (subs.Sum(x => x.vPembatalanNIB) ?? 0) : 0)),
                persil.BiayaBN < 0 ? 0 : (persil.BiayaBN - (subs != null ? (subs.Sum(x => x.vBiayaBN) ?? 0) : 0)),
                persil.gantiBlanko < 0 ? 0 : (persil.gantiBlanko - (subs != null ? (subs.Sum(x => x.vGantiBlanko) ?? 0) : 0)),
                persil.kompensasi < 0 ? 0 : (persil.kompensasi - (subs != null ? (subs.Sum(x => x.vKompensasi) ?? 0) : 0)),
                persil.pajakLama < 0 ? 0 : (persil.pajakLama - (subs != null ? (subs.Sum(x => x.vPajakLama) ?? 0) : 0)),
                persil.pajakWaris < 0 ? 0 : (persil.pajakWaris - (subs != null ? (subs.Sum(x => x.vPajakWaris) ?? 0) : 0)),
                persil.tunggakanPBB < 0 ? 0 : (persil.tunggakanPBB - (subs != null ? (subs.Sum(x => x.vTunggakanPBB) ?? 0) : 0)),
                persil.basic.current.satuanAkte,
                biayalainnyas.ToArray());

            return view;
        }

        public BayarDtlBidangViewDeposit toViewDeposit(LandropePayContext context, Bayar byr,
            (ExtLandropeContext.entitas project, ExtLandropeContext.entitas desa)[] locations, cmnItem[] ptsks, BayarSubDtlDeposit[] subdetails)
        {
            var view = new BayarDtlBidangViewDeposit();
            var deposit = new BayarSubDtlDepositSS();
            var persil = this.persil(context);
            var subs = subdetails.FirstOrDefault(x => x.keyPersil == persil.key);

            var fgFix = persil.luasFix == null ? false : persil.luasFix;
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);

            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);


            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            string ket = "Refund ";

            (var pph21, var validasi, var mandor, var nib, var bn, var blanko, var kompensasi, var pajaklama, var pajakwaris, var pbb) =
                (subs.pph21 == true ? "pph21, " : "",
                subs.ValidasiPPH == true ? "validasi pph, " : "",
                subs.mandor == true ? "mandor, " : "",
                subs.pembatalanNIB == true ? "pembatalan NIB, " : "",
                subs.BiayaBN == true ? "biaya balik nama, " : "",
                subs.gantiBlanko == true ? "ganti blanko, " : "",
                subs.kompensasi == true ? "kompensasi, " : "",
                subs.pajakLama == true ? "pajak lama, " : "",
                subs.pajakWaris == true ? "pajak waris, " : "",
                subs.tunggakanPBB == true ? "tunggakan pbb, " : "");

            string biayaLainnya = "";
            if (subs.biayalainnya != null)
            {
                foreach (var item in subs.biayalainnya)
                {
                    if (item.fgLainnya == true)
                        biayaLainnya += item.identity + ", ";
                }
            }

            string allket = ket + pph21 + validasi + mandor + nib + bn + blanko + kompensasi + pajaklama + pajakwaris + pbb + biayaLainnya;

            allket = allket.Remove(allket.Length - 2);

            var d = subdetails.Where(x => x.keyPersil == persil.key).ToArray();
            if (d != null)
            {
                deposit.vpph21 = d.Where(x => x.vpph21 != null)?.Sum(x => x.vpph21);
                deposit.vValidasiPPH = d.Where(x => x.vValidasiPPH != null)?.Sum(x => x.vValidasiPPH);
                deposit.vMandor = d.Where(x => x.vMandor != null)?.Sum(x => x.vMandor);
                deposit.vPembatalanNIB = d.Where(x => x.vPembatalanNIB != null)?.Sum(x => x.vPembatalanNIB);
                deposit.vBiayaBN = d.Where(x => x.vBiayaBN != null)?.Sum(x => x.vBiayaBN);
                deposit.vGantiBlanko = d.Where(x => x.vGantiBlanko != null)?.Sum(x => x.vGantiBlanko);
                deposit.vKompensasi = d.Where(x => x.vKompensasi != null)?.Sum(x => x.vKompensasi);
                deposit.vPajakLama = d.Where(x => x.vPajakLama != null)?.Sum(x => x.vPajakLama);
                deposit.vPajakWaris = d.Where(x => x.vPajakWaris != null)?.Sum(x => x.vPajakWaris);
                deposit.vTunggakanPBB = d.Where(x => x.vTunggakanPBB != null)?.Sum(x => x.vTunggakanPBB);

                var bLain = d.SelectMany(x => x.biayalainnya).ToArray();

                if (persil.biayalainnya != null && persil.biayalainnya.Count() > 0)
                {
                    if (bLain != null && bLain.Count() > 0)
                    {
                        var listCore = new List<BiayalainnyaCore>();
                        foreach (var pbl in persil.biayalainnya)
                        {
                            var found = bLain.Where(x => x.identity == pbl.identity).GroupBy(x => (x.identity, x.fgLainnya), (y, z) =>
                                                                            new BiayalainnyaCore { identity = y.identity, fglainnya = y.fgLainnya, nilai = z.Sum(x => x.nilai) })
                                                                            .FirstOrDefault();

                            listCore.Add(found);
                        }

                        deposit.biayaLainnya = listCore.ToArray();
                    }
                }
            }

            (
                view.keyPersil,
                view.IdBidang,
                view.project,
                view.desa,
                view.ptsk,
                view.alasHak,
                view.satuan,
                view.jumlah,
                view.keterangan,
                view.deposit,
                view.luasDibayar,
                view.TotalPembayaran,
                view.noPeta
                ) =
                (
                persil.key,
                persil.IdBidang,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).project.identity,
                locations.FirstOrDefault(x => x.desa.key == persil.basic.current.keyDesa).desa.identity,
                ptsks.FirstOrDefault(x => x.key == persil.basic.current.keyPTSK)?.name,
                persil.basic?.current?.surat?.nomor,
                persil.basic?.current?.satuan,
                subs.Jumlah,
                allket,
                deposit,
                luas,
                totalPembayaran,
                persil.basic?.current?.noPeta
                );

            return view;
        }

        public BayarDtlBidangView toViewPay(LandropePayContext context, Bayar byr)
        {
            var view = new BayarDtlBidangView();
            var persil = this.persil(context);
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);
            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            (view.key, view.keyPersil, view.IdBidang, view.luasSurat, view.luasInternal, view.luasDibayar, view.Pemilik,
                view.satuan, view.total, view.SisaLunas) =
                (key, persil.key, persil.IdBidang, persil.basic.current.luasSurat, persil.basic.current.luasInternal, luas, persil.basic.current.pemilik, persil.basic.current.satuan,
                (luas * persil.basic.current.satuan), sisaLunas);

            return view;
        }

        public BayarDtlBidangViewSelected toViewSelected(LandropePayContext context, Bayar byr, bool Selected)
        {
            var view = new BayarDtlBidangViewSelected();
            var persil = this.persil(context);
            var fgFix = persil.luasFix == null ? false : persil.luasFix;
            (var sisaLunas, var totalPembayaran, var totalPPH) = byr.SisaPelunasan2(persil);
            double? luas = 0;
            double? luasNIB = 0;

            luasNIB = persil.basic.current.luasNIBTemp == null ? PersilHelper.GetLuasBayar(persil) : persil.basic.current.luasNIBTemp;
            var luasdiBayar = persil.basic.current.luasDibayar == null ? 0 : Convert.ToDouble(persil.basic.current.luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                 .SelectMany(x => x.subdetails).Any(x => x.keyPersil == persil.key);

            if (isLunas)
                luas = persil.luasPelunasan ?? luasdiBayar;
            else if (persil.luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var biayalainnyas = new List<BiayalainnyaCore>();
            if (persil.biayalainnya != null)
            {
                foreach (var item in persil.biayalainnya)
                {
                    var lainnya = new BiayalainnyaCore();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fglainnya = item.fgLainnya;

                    biayalainnyas.Add(lainnya);
                }
            }
            var subdetails = byr.details.Where(det => det.jenisBayar == JenisBayar.Lunas)
                                                        .Select(det => det.subdetails
                                                        .Where(sub => sub.keyPersil == persil.key)
                                                        .FirstOrDefault()
                                              ).FirstOrDefault();
            var pelunasan = subdetails != null ? subdetails.Jumlah : sisaLunas;
            var locations = context.GetVillage(persil.basic.current.keyDesa);

            (view.key,
                view.keyPersil,
                view.IdBidang,
                view.en_state,
                view.en_proses,
                view.luasSurat,
                view.luasDibayar,
                view.luasInternal,
                view.luasNIB,
                view.luasFix,
                view.Pemilik,
                view.AlasHak,
                view.NamaSurat,
                view.satuan,
                view.total,
                view.SisaLunas,
                view.noPeta,
                view.TotalPembayaran,
                view.fgPPH21,
                view.TotalPPH21,
                view.validasiPPH, view.TotalValidasiPPH21, view.isEdited, view.project, view.desa, view.Mandor,
             view.PembatalanNIB, view.BalikNama, view.GantiBlanko, view.Kompensasi, view.PajakLama, view.PajakWaris, view.TunggakanPBB, view.satuanAkta, view.biayalainnyas, view.IsBidangSelected, view.hargaPelunasan) =
             (key,
             persil.key,
             persil.IdBidang,
             (Enum.TryParse<StatusBidang>(persil.en_state.ToString(), out StatusBidang stp) ? stp : default),
             (Enum.TryParse<JenisProses>(persil.basic.current.en_proses.ToString(), out JenisProses pro) ? pro : default),
             persil.basic.current.luasSurat,
             luas,
             persil.basic.current.luasInternal,
             luasNIB,
             fgFix,
             persil.basic.current.pemilik,
             persil.basic.current.surat.nomor,
             persil.basic.current.surat.nama,
             persil.basic.current.satuan,
             (luas * persil.basic.current.satuan),
             sisaLunas, persil.basic.current.noPeta, totalPembayaran, persil.pph21,
             totalPPH,
             persil.ValidasiPPH,
             persil.ValidasiPPHValue == null ? 0 : persil.ValidasiPPHValue,
             persil.isEdited,
             locations.project.identity,
             locations.desa.identity,
             persil.mandor,
             persil.pembatalanNIB,
             persil.BiayaBN,
             persil.gantiBlanko,
             persil.kompensasi,
             persil.pajakLama,
             persil.pajakWaris,
             persil.tunggakanPBB,
             persil.basic.current.satuanAkte, biayalainnyas.ToArray(), Selected, pelunasan);

            return view;
        }

    }
    public class BayarSubDtl
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public double Jumlah { get; set; }

        public BayarSubDtl()
        {
            key = mongospace.MongoEntity.MakeKey;
        }

        public void fromCore(double jml)
        {
            (Jumlah) =
                (jml);
        }

        public void fromCore(string keypersil, double jumlah)
        {
            (keyPersil, Jumlah) =
                (keypersil, jumlah);
        }
    }
    public class BayarSubDtlDeposit
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public double Jumlah { get; set; }
        public bool? pph21 { get; set; }
        public double? vpph21 { get; set; }
        public bool? ValidasiPPH { get; set; }
        public double? vValidasiPPH { get; set; }
        public bool? mandor { get; set; }
        public double? vMandor { get; set; }
        public bool? pembatalanNIB { get; set; }
        public double? vPembatalanNIB { get; set; }
        public bool? BiayaBN { get; set; }
        public double? vBiayaBN { get; set; }
        public bool? gantiBlanko { get; set; }
        public double? vGantiBlanko { get; set; }
        public bool? kompensasi { get; set; }
        public double? vKompensasi { get; set; }
        public bool? pajakLama { get; set; }
        public double? vPajakLama { get; set; }
        public bool? pajakWaris { get; set; }
        public double? vPajakWaris { get; set; }
        public bool? tunggakanPBB { get; set; }
        public double? vTunggakanPBB { get; set; }
        public biayalainnya[] biayalainnya { get; set; } = new biayalainnya[0];
        public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);
        public BayarSubDtlDeposit()
        {
            key = mongospace.MongoEntity.MakeKey;
        }

        public void fromCore(double jml)
        {
            (Jumlah) =
                (jml);
        }

        public void fromCore(string keypersil, double jumlah)
        {
            (keyPersil, Jumlah) =
                (keypersil, jumlah);
        }

        public void fromCore(BayarSubDtlDepositCore core)
        {
            var biayalainnyas = new List<biayalainnya>();
            if (core.biayaLainnya != null)
            {
                foreach (var item in core.biayaLainnya)
                {
                    var lainnya = new biayalainnya();
                    lainnya.identity = item.identity;
                    lainnya.nilai = item.nilai;
                    lainnya.fgLainnya = item.fglainnya;

                    biayalainnyas.Add(lainnya);
                }
            }

            (keyPersil,
                Jumlah,
                pph21,
                vpph21,
                ValidasiPPH,
                vValidasiPPH,
                mandor,
                vMandor,
                pembatalanNIB,
                vPembatalanNIB,
                BiayaBN,
                vBiayaBN,
                gantiBlanko,
                vGantiBlanko,
                kompensasi,
                vKompensasi,
                pajakLama,
                vPajakLama,
                pajakWaris,
                vPajakWaris,
                tunggakanPBB,
                vTunggakanPBB,
                biayalainnya) =
            (core.keyPersil,
                core.Jumlah,
                core.pph21,
                core.vpph21,
                core.ValidasiPPH,
                core.vValidasiPPH,
                core.mandor,
                core.vMandor,
                core.pembatalanNIB,
                core.vPembatalanNIB,
                core.BiayaBN,
                core.vBiayaBN,
                core.gantiBlanko,
                core.vGantiBlanko,
                core.kompensasi,
                core.vKompensasi,
                core.pajakLama,
                core.vPajakLama,
                core.pajakWaris,
                core.vPajakWaris,
                core.tunggakanPBB,
                core.vTunggakanPBB,
                biayalainnyas.ToArray());
        }
    }

    public class Giro
    {
        public Giro()
        {
            key = mongospace.MongoEntity.MakeKey;
        }
        public string key { get; set; }
        public JenisGiro? jenis { get; set; }
        public string namaPenerima { get; set; }
        public string bankPenerima { get; set; }
        public string accPenerima { get; set; }
        public double? nominal { get; set; }

        public void fromCore(GiroCore core)
        {
            (jenis, namaPenerima, bankPenerima, accPenerima, nominal) = (core.Jenis, core.NamaPenerima, core.BankPenerima, core.AccountPenerima, core.Nominal);
        }
    }
    public class Reason
    {
        public DateTime tanggal { get; set; }
        public ToDoState state { get; set; }
        public string privs { get; set; }
        public string keyCreator { get; set; }
        public bool flag { get; set; } //True dari pembatalan, false dari reject
        public string description { get; set; }
    }

    public class StaticCollection
    {
        public string memoTo { get; set; }
        public string memoSign { get; set; }
        public string contactPersonName { get; set; }
        public string contactPersonPhone { get; set; }
        public string tembusan { get; set; }
    }

    public class RptStatusPerDealMod
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string AlasHak { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public int Tahap { get; set; }
        public StatusBidang StatusBidang { get; set; }
        public StatusDeal StatusDeal { get; set; }

        public RptStatusPerDeal toView()
        {
            RptStatusPerDeal view = new RptStatusPerDeal();
            (
                view.AlasHak, view.Desa, view.Group, view.IdBidang,
                view.Pemilik, view.Project, view.StatusDeal, view.Tahap,
                view.StatusBidang
            )
                =
            (
                AlasHak, Desa, Group, IdBidang,
                Pemilik, Project, StatusDeal.ToString().ToUpper(), Tahap,
                StatusBidang.ToString().ToUpper()
            );
            return view;
        }
    }

    public class RptDetailLunasBidang
    {
        public string tKey { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string keyDesa { get; set; }
        public string Desa { get; set; }
        public bool? luasFix { get; set; }
        public double? luasNibTemp { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasPelunasan { get; set; }
        public double? luasSurat { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkte { get; set; }
        public string pemilik { get; set; }
        public string alasHak { get; set; }
        public double? mandor { get; set; }
        public double? pembatalanNIB { get; set; }
        public double? BiayaBN { get; set; }
        public double? gantiBlanko { get; set; }
        public double? kompensasi { get; set; }
        public double? pajakLama { get; set; }
        public double? pajakWaris { get; set; }
        public double? tunggakanPBB { get; set; }
        public bool? ValidasiPPH { get; set; }
        public double? ValidasiPPHValue { get; set; }
        public bool? pph21 { get; set; }
        public biayalainnya[] biayaLainnya { get; set; } = new biayalainnya[0];

        public RptDetailLunasBidang setLocation(string project, string desa)
        {
            if (project != null)
                this.Project = project;
            if (desa != null)
                this.Desa = desa;
            return this;
        }

        public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);

        public RptDetailLunasBidangView toView(Bayar byr, List<(string key, double luas)> luasDocs)
        {
            var allowedJenisBayar = new[] { JenisBayar.DP, JenisBayar.Lunas, JenisBayar.UTJ };

            var view = new RptDetailLunasBidangView();
            var fgFix = luasFix;
            double? luas = 0;

            var jenisBayar = byr.details.SelectMany(x => x.subdetails, (a, b)
                                                => new { det = a, sub = b }
                                  ).Where(a => a.sub.keyPersil.Contains(keyPersil))
                                   .Where(a => allowedJenisBayar.Contains(a.det.jenisBayar))
                                   .OrderByDescending(a => a.det.jenisBayar)
                                   .Select(a => a.det.jenisBayar)?.FirstOrDefault();

            #region HitungSisaLunas
            (double totalBayar, double? luasdiBayar, double pelunasan,
             double pph21, double pph, double realpph, double validasiPPH,
            double? luasDiBayar, double? biayaLainnya) =
            (0, 0, 0, 0, 0, 0, 0, 0, 0);

            var luasNIB_ = luasDocs.Where(x => x.key == this.keyPersil) == null ? 0 : luasDocs.Where(x => x.key == this.keyPersil).FirstOrDefault().luas;

            (double? mandor, double? pembatalanNIB, double? balikNama, double? gantiBlanko, double? kompensasi, double? pajaklama, double? pajakwaris, double? tunggakanPBB)
            =
            (this.mandor, this.pembatalanNIB, this.BiayaBN, this.gantiBlanko, this.kompensasi, this.pajakLama, this.pajakWaris, this.tunggakanPBB);

            var totalSudahdiBayar = byr.details.Where(x => x.invalid != true && allowedJenisBayar.Contains(x.jenisBayar))
                .SelectMany(x => x.subdetails)
                .Where(x => x.keyPersil == keyPersil)
                .Select(x => x.Jumlah).Sum();

            if (this.biayaLainnya != null)
                biayaLainnya = this.biayaLainnya.Where(x => x.fgLainnya == true).Select(x => x.nilai).Sum();

            var luasNIB = luasNibTemp == 0 ? luasNIB_ : luasNibTemp;
            luasdiBayar = Convert.ToDouble(luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                             .SelectMany(x => x.subdetails).Any(x => x.keyPersil == keyPersil);

            if (isLunas)
                luas = luasPelunasan ?? luasdiBayar;
            else if (luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var satuan = this.satuan;
            var satuanAkte = this.satuanAkte;

            totalBayar = Convert.ToDouble(luas) * Convert.ToDouble(satuan);
            var totalBayarAkte = Convert.ToDouble(luas) * Convert.ToDouble(satuanAkte);

            pph21 = (totalBayar * 2.5) / 100;
            var pph21akte = (totalBayarAkte * 2.5) / 100;
            pph = pph21akte == 0 ? pph21 : pph21akte;

            if (this.pph21 == true)
            {
                realpph = pph; // Jika ditanggung penjual maka akan mengurangi pelunasan
            }

            if (this.ValidasiPPH == true)
                validasiPPH = Convert.ToDouble(ValidasiPPHValue);

            var sisaLunas = totalBayar - (totalSudahdiBayar + realpph + validasiPPH + mandor + pembatalanNIB
               + balikNama + gantiBlanko + kompensasi + pajaklama + pajakwaris + tunggakanPBB + biayaLainnya);
            #endregion
            (
                view.IdBidang, view.Project,
                view.Desa, view.Tahap,
                view.Pemilik, view.AlasHak,
                view.LuasSurat, view.LuasDibayar,
                view.HargaSatuan, view.HargaAkta,
                view.HargaTotal, view.SisaPelunasan,
                view.StatusPembayaran
            )
                =
            (
                IdBidang, Project,
                Desa, byr.nomorTahap,
                pemilik, alasHak,
                luasSurat, luas,
                satuan, satuanAkte,
                (luas * satuan), sisaLunas,
                Convert.ToInt64(jenisBayar) == 0 ? "-" : Enum.GetName(typeof(JenisBayar), jenisBayar).ToString()
            );
            return view;
        }

        public RptDetailLunasBidangView toView2(Bayar byr, List<(string key, double luas)> luasDocs, GraphMainInstance[] instances, DateTime? cutOffDate)
        {
            var allowedJenisBayar = new[] { JenisBayar.DP, JenisBayar.Lunas, JenisBayar.UTJ };
            var view = new RptDetailLunasBidangView();
            var fgFix = luasFix;
            double? luas = 0;

            var jenisBayar = byr.details.SelectMany(x => x.subdetails, (a, b)
                                                => new { det = a, sub = b })
                                   .Where(a => a.sub.keyPersil.Contains(keyPersil))
                                   .Where(a => allowedJenisBayar.Contains(a.det.jenisBayar))
                                   .OrderByDescending(a => a.det.jenisBayar)
                                   .Select(a => (a.det.jenisBayar, a.det.instkey));

            GraphMainInstance instance = instances.FirstOrDefault(i => i.key == jenisBayar.FirstOrDefault().instkey);

            DateTime? tanggalBayar = instance?.states.FirstOrDefault(s => s.state == ToDoState.cashierApproved_ || s.state == ToDoState.complished_)
                                    ?.time ?? (instance?.lastState?.state == ToDoState.complished_ ? instance?.lastState?.time : null);

            tanggalBayar = tanggalBayar != null ? Convert.ToDateTime(tanggalBayar).ToLocalTime() : tanggalBayar;
            tanggalBayar = tanggalBayar != null ? ((tanggalBayar > cutOffDate) ? null : tanggalBayar) : tanggalBayar;

            #region HitungSisaLunas
            (double totalBayar, double? luasdiBayar, double pelunasan,
             double pph21, double pph, double realpph, double validasiPPH,
            double? luasDiBayar, double? biayaLainnya) =
            (0, 0, 0, 0, 0, 0, 0, 0, 0);

            var luasNIB_ = luasDocs.Where(x => x.key == this.keyPersil) == null ? 0 : luasDocs.Where(x => x.key == this.keyPersil).FirstOrDefault().luas;

            (double? mandor, double? pembatalanNIB, double? balikNama, double? gantiBlanko, double? kompensasi, double? pajaklama, double? pajakwaris, double? tunggakanPBB)
            =
            (this.mandor, this.pembatalanNIB, this.BiayaBN, this.gantiBlanko, this.kompensasi, this.pajakLama, this.pajakWaris, this.tunggakanPBB);

            var totalSudahdiBayar = byr.details.Join(instances, d => d.instkey, i => i.key, (d, i) => new { inst = i, detail = d })
                                               .Where(x => x.detail.invalid != true
                                                            && allowedJenisBayar.Contains(x.detail.jenisBayar)
                                                            && ((x.inst.states.FirstOrDefault(s => s.state == ToDoState.cashierApproved_
                                                               || s.state == ToDoState.complished_)?.time <= cutOffDate)
                                                               || (x.inst.lastState.state == ToDoState.complished_ && x.inst.lastState?.time <= cutOffDate)
                                                      ))
                                              .SelectMany(x => x.detail.subdetails)
                                              .Where(x => x.keyPersil == keyPersil)
                                              .Select(x => x.Jumlah).Sum();

            if (this.biayaLainnya != null)
                biayaLainnya = this.biayaLainnya.Where(x => x.fgLainnya == true).Select(x => x.nilai).Sum();

            var luasNIB = luasNibTemp == 0 ? luasNIB_ : luasNibTemp;
            luasdiBayar = Convert.ToDouble(luasDibayar);

            var isLunas = byr.details.Where(x => x.jenisBayar == JenisBayar.Lunas && x.invalid != true)
                             .SelectMany(x => x.subdetails).Any(x => x.keyPersil == keyPersil);

            if (isLunas)
                luas = luasPelunasan ?? luasdiBayar;
            else if (luasFix != true)
                luas = luasNIB == 0 ? luasdiBayar : luasNIB;
            else
                luas = luasdiBayar;

            var satuan = this.satuan;
            var satuanAkte = this.satuanAkte;

            totalBayar = Convert.ToDouble(luas) * Convert.ToDouble(satuan);
            var totalBayarAkte = Convert.ToDouble(luas) * Convert.ToDouble(satuanAkte);

            pph21 = (totalBayar * 2.5) / 100;
            var pph21akte = (totalBayarAkte * 2.5) / 100;
            pph = pph21akte == 0 ? pph21 : pph21akte;

            if (this.pph21 == true)
            {
                realpph = pph; // Jika ditanggung penjual maka akan mengurangi pelunasan
            }

            if (this.ValidasiPPH == true)
                validasiPPH = Convert.ToDouble(ValidasiPPHValue);

            var sisaLunas = totalBayar - (totalSudahdiBayar + realpph + validasiPPH + mandor + pembatalanNIB
               + balikNama + gantiBlanko + kompensasi + pajaklama + pajakwaris + tunggakanPBB + biayaLainnya);
            #endregion
            (
                view.IdBidang, view.Project,
                view.Desa, view.Tahap,
                view.Pemilik, view.AlasHak,
                view.LuasSurat, view.LuasDibayar,
                view.HargaSatuan, view.HargaAkta,
                view.HargaTotal, view.SisaPelunasan,
                view.StatusPembayaran,
                view.TglBayar
            )
                =
            (
                IdBidang, Project,
                Desa, byr.nomorTahap,
                pemilik, alasHak,
                luasSurat, luas,
                satuan, satuanAkte,
                (luas * satuan), sisaLunas,
                Convert.ToInt64(jenisBayar?.FirstOrDefault().jenisBayar) == 0 ? "-" : Enum.GetName(typeof(JenisBayar), jenisBayar?.FirstOrDefault().jenisBayar).ToString(),
                tanggalBayar
            );
            return view;
        }
    }

    public class RptPivotPembayaran
    {
        public string Key { get; set; }
        public string KeyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Group { get; set; }
        public string Status { get; set; }
        public double? LuasDibayar { get; set; }
        public BayarDtl[] Details { get; set; }

        public RptPivotPembayaranView ToView(GraphMainInstance[] main)
        {
            var jenisBayar = this.Details.SelectMany(x => x.subdetails, (a, b)
                                   => new { det = a, sub = b })
                      .Where(a => a.sub.keyPersil.Contains(this.KeyPersil))
                      .Where(a => a.det.jenisBayar != JenisBayar.Mandor && a.det.jenisBayar != JenisBayar.Lainnya)
                      .Select(a => (a.det.jenisBayar, a.det.instkey, a.det.tglBayar)).ToList();

            var bayarGraph = jenisBayar.Join(main, j => j.instkey
                                                 , m => m.key
                                                 , (a, b) => (a.jenisBayar, a.tglBayar, b.creatorkey,
                                                             b.lastState, b.created, b.states.ToArray()
                                                   ))
                                       .ToList();

            (string status, bool isLunas) = SetStatusImport(bayarGraph);

            RptPivotPembayaranView view = new RptPivotPembayaranView();
            (
                view.IdBidang, view.Project, view.Desa,
                view.LuasBayar, view.Status, view.isLunas,
                view.Group
            )
                =
            (
                this.IdBidang, this.Project, this.Desa,
                (this.LuasDibayar == 0 ? null : this.LuasDibayar), status, isLunas,
                this.Group
            );
            return view;
        }

        public (string, bool) SetStatus2(List<(JenisBayar, GraphState[])> listBayarGraph)
        {
            bool isLunas = listBayarGraph.Any(x => x.Item1 == JenisBayar.Lunas);
            int jumlahBayar = listBayarGraph.Count();
            string status = "";
            if (listBayarGraph.Count() == 0)
            {
                status = "Exclude";
            }
            else if (isLunas && jumlahBayar == 1)
            {
                var firstRecord = listBayarGraph.FirstOrDefault();
                var createDate = firstRecord.Item2.FirstOrDefault(i => i.state == ToDoState.created_)?.time;
                createDate = Convert.ToDateTime(createDate).ToLocalTime();

                var bayarDate = firstRecord.Item2.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time ?? DateTime.Now;
                bayarDate = Convert.ToDateTime(bayarDate).ToLocalTime();
                bool isBayar = firstRecord.Item2.Any(i => i.state == ToDoState.cashierApproved_);

                var spentDay = bayarDate.Subtract((DateTime)createDate).TotalDays;
                status = isBayar ? "2Langsung Lunas" : SetStatusPeriod(spentDay);
                isLunas = isBayar ? isLunas : false;
            }
            else if (isLunas && jumlahBayar > 1)
            {
                var orderedBayarGraph = listBayarGraph.OrderBy(x => x.Item1)
                                                      .ThenBy(x => (x.Item2.FirstOrDefault(i => i.state == ToDoState.created_)?.time)
                                      ).ToList();

                var firstPay = orderedBayarGraph.FirstOrDefault();
                bool isFirstBayar = (firstPay.Item2.Any(i => i.state == ToDoState.complished_ ||
                                     firstPay.Item2.Any(i => i.state == ToDoState.cashierApproved_)));


                var lastPay = orderedBayarGraph.LastOrDefault();
                bool isLastBayar = (lastPay.Item2.Any(i => i.state == ToDoState.complished_ ||
                                     lastPay.Item2.Any(i => i.state == ToDoState.cashierApproved_)));

                if (!isFirstBayar)
                {
                    status = "Exclude";
                    isLunas = false;
                }
                else
                {
                    var firstPayDate = firstPay.Item2.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    firstPayDate = Convert.ToDateTime(firstPayDate).ToLocalTime();

                    var lastPayDate = lastPay.Item2.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    lastPayDate = Convert.ToDateTime(lastPayDate).ToLocalTime();

                    var spentDay = (lastPayDate ?? DateTime.Now).Subtract(firstPayDate ?? DateTime.Now).TotalDays;
                    status = (firstPayDate == null) ? "Exclude" : SetStatusPeriod(spentDay);
                    isLunas = (lastPayDate == null) ? false : isLunas;
                }
            }
            else
            {
                var orderedBayarGraph = listBayarGraph.OrderBy(x => x.Item1)
                                                      .ThenBy(x => (x.Item2.FirstOrDefault(i => i.state == ToDoState.created_)?.time)
                                      ).ToList();

                var firstPay = orderedBayarGraph.FirstOrDefault();
                bool isFirstBayar = (firstPay.Item2.Any(i => i.state == ToDoState.complished_ ||
                                     firstPay.Item2.Any(i => i.state == ToDoState.cashierApproved_)));

                if (!isFirstBayar)
                {
                    status = "Exclude";
                    isLunas = false;
                }
                else
                {
                    var tanggalBuat = firstPay.Item2.FirstOrDefault(i => i.state == ToDoState.created_)?.time;
                    tanggalBuat = Convert.ToDateTime(tanggalBuat).ToLocalTime();

                    var tanggalBayar = firstPay.Item2.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    tanggalBayar = Convert.ToDateTime(tanggalBayar).ToLocalTime();

                    var spentDay = (tanggalBayar ?? DateTime.Now).Subtract(tanggalBuat ?? DateTime.Now).TotalDays;
                    status = (tanggalBayar == null || tanggalBuat == null) ? "Exclude" : SetStatusPeriod(spentDay);
                }
            }

            return (status, isLunas);
        }

        public (string, bool) SetStatusImport(List<(JenisBayar, DateTime?, string, GraphState, DateTime?, GraphState[])> listBayarGraph)
        {
            string[] keyImporter = new[] { "BCAB674C-45E4-492B-8EDE-791C872DCC15", "2AE8E042-C124-434A-969A-9A2907F93F07" };
            bool isLunas = listBayarGraph.Any(x => x.Item1 == JenisBayar.Lunas);
            int jumlahBayar = listBayarGraph.Count();
            string status = "";

            if (listBayarGraph.Count() == 0)
            {
                status = "Exclude";
            }
            else if (isLunas && jumlahBayar == 1)
            {
                var firstRecord = listBayarGraph.FirstOrDefault();
                bool isImporter = keyImporter.Contains(firstRecord.Item3);

                var createDate = isImporter ? (firstRecord.Item2 ?? firstRecord.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time) : firstRecord.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time;
                createDate = Convert.ToDateTime(createDate).ToLocalTime();
                var bayarDate = isImporter ? firstRecord.Item4?.time : (DateTime)firstRecord.Item6.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                bayarDate = Convert.ToDateTime(bayarDate).ToLocalTime();

                var isLnsLangsung = bayarDate?.Date == createDate?.Date;
                double spentDay = (bayarDate ?? DateTime.Now).Subtract(createDate ?? DateTime.Now).TotalDays;
                status = isLnsLangsung ? "2Langsung Lunas" : SetStatusPeriod(spentDay);
                status = bayarDate == null ? "Exclude" : status;
            }
            else if (isLunas && jumlahBayar > 1)
            {
                var orderedBayarGraph = listBayarGraph.OrderBy(x => x.Item1)
                                                      .ThenBy(x => (x.Item5 ??
                                                                    x.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time ??
                                                                    x.Item2
                                                                   )
                                                      ).ToList();

                var firstPay = orderedBayarGraph.FirstOrDefault();
                bool isfirstImporter = keyImporter.Contains(firstPay.Item3);
                bool isFirstBayar = (firstPay.Item4.state == ToDoState.complished_ || firstPay.Item6.Any(i => i.state == ToDoState.cashierApproved_));

                var lastPay = orderedBayarGraph.LastOrDefault();
                bool isLastImporter = keyImporter.Contains(lastPay.Item3);
                bool isLastBayar = (lastPay.Item4.state == ToDoState.complished_ || lastPay.Item6.Any(i => i.state == ToDoState.cashierApproved_));

                DateTime? firstPayDate;
                DateTime? lastPayDate;
                if (!isFirstBayar)
                {
                    status = "Exclude";
                    isLunas = false;
                }
                else
                {
                    firstPayDate = isfirstImporter ? firstPay.Item4?.time :
                                                     firstPay.Item6.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    firstPayDate = Convert.ToDateTime(firstPayDate).ToLocalTime();

                    lastPayDate = isLastImporter ? lastPay.Item4?.time :
                                                   lastPay.Item6.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    lastPayDate = Convert.ToDateTime(lastPayDate).ToLocalTime();

                    var spentDay = (lastPayDate ?? DateTime.Now).Subtract(firstPayDate ?? DateTime.Now).TotalDays;

                    status = firstPayDate == null ? "Exclude" : SetStatusPeriod(spentDay);
                    isLunas = lastPayDate == null ? false : isLunas;

                }
            }
            else if (!isLunas)
            {
                var orderedBayarGraph = listBayarGraph.OrderBy(x => x.Item1)
                                                      .ThenBy(x => (x.Item5 ??
                                                                    x.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time ??
                                                                    x.Item2
                                                                   )
                                                      ).ToList();

                var firstPay = orderedBayarGraph.FirstOrDefault();
                bool isFirstImport = keyImporter.Contains(firstPay.Item3);
                bool isFirstBayar = (firstPay.Item4.state == ToDoState.complished_ || firstPay.Item4.state == ToDoState.cashierApproved_);

                if (!isFirstBayar)
                {
                    status = "Exclude";
                }
                else
                {
                    var tanggalBuat = isFirstImport ? (firstPay.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time ?? firstPay.Item2) :
                                                       firstPay.Item6.FirstOrDefault(i => i.state == ToDoState.created_)?.time;
                    tanggalBuat = Convert.ToDateTime(tanggalBuat).ToLocalTime();

                    var tanggalBayar = isFirstImport ? firstPay.Item4?.time : firstPay.Item6.FirstOrDefault(i => i.state == ToDoState.cashierApproved_)?.time;
                    tanggalBayar = Convert.ToDateTime(tanggalBayar).ToLocalTime();

                    var spentDay = (tanggalBayar ?? DateTime.Now).Subtract(tanggalBuat ?? DateTime.Now).TotalDays;
                    status = (tanggalBayar == null || tanggalBuat == null) ? "Exclude" : SetStatusPeriod(spentDay);
                }
            }

            return (status, isLunas);
        }

        public RptPivotPembayaranView ToHelper(GraphMainInstance[] main)
        {
            var jenisBayar = this.Details.SelectMany(x => x.subdetails, (a, b)
                                 => new { det = a, sub = b })
                    .Where(a => a.sub.keyPersil.Contains(this.KeyPersil))
                    .Where(a => a.det.jenisBayar != JenisBayar.Mandor && a.det.jenisBayar != JenisBayar.Lainnya)
                    .Select(a => (a.det.jenisBayar, a.det.instkey, a.det.tglBayar)).ToList();

            var bayarGraph = jenisBayar.Join(main, j => j.instkey
                                                 , m => m.key
                                                 , (a, b) => (a.jenisBayar, b.states.ToArray()
                                                   ))
                                       .ToList();

            (string status, bool isLunas) = SetStatus2(bayarGraph);

            RptPivotPembayaranView helper = new RptPivotPembayaranView();
            (
                helper.IdBidang, helper.Project, helper.Desa,
                helper.LuasBayar, helper.Status, helper.isLunas,
                helper.Group
            )
                =
            (
                this.IdBidang, this.Project, this.Desa,
                (this.LuasDibayar == 0 ? null : this.LuasDibayar), status, isLunas,
                this.Group
            );
            return helper;
        }

        public string SetStatusPeriod(double daySpent)
        {
            if (daySpent <= 90)
                return "3< 3 Bln";
            else if (daySpent > 90 && daySpent <= 180)
                return "43-6 Bln";
            else if (daySpent > 180 && daySpent <= 365)
                return "56-12 Bln";
            else if (daySpent > 365 && daySpent <= 730)
                return "612-24 Bln";
            else
                return "7> 24 Bln";
        }
    }

    public class RptMemoTanah
    {
        public string keyBayar { get; set; }
        public string keyPersil { get; set; }
        public int Tahap { get; set; }
        public string IdBidang { get; set; }
        public string Alias { get; set; }
        public string Desa { get; set; }
        public string Project { get; set; }
        public string PTSK { get; set; }
        public string PTSKCode { get; set; }
        public string Pemilik { get; set; }
        public string Notaris { get; set; }
        public string SuratAsal { get; set; }
        public JenisAlasHak en_jenis { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasInternal { get; set; }
        public double? LuasPBT { get; set; }
        public double? LuasDibayar { get; set; }
        public double? Harga { get; set; }
        public double? SatuanAkte { get; set; }
        public string Group { get; set; }
        public string NoPeta { get; set; }
        public double? TotalBeli { get; set; }

        public string keyManager { get; set; }
        public string keyMediator { get; set; }
        public string keySales { get; set; }

        public string Keterangan { get; set; }
        public double? LuasOverlap { get; set; }
        public string Nama { get; set; }


        public BidangDetailsMemoBayar ToView(List<(string key, double luas)> luasPBTS, List<string> keyPersils)
        {
            BidangDetailsMemoBayar view = new();
            var selectedPBT = luasPBTS.FirstOrDefault(l => l.key == this.keyPersil);
            double? luasPBT = selectedPBT.key != null ? selectedPBT.luas : null;
            bool isSelected = keyPersils.Contains(this.keyPersil);

            (view.IdBidang, view.Alias, view.Desa, view.Project,
             view.Pemilik, view.SuratAsal, view.LuasSurat, view.LuasInternal,
             view.LuasPBT, view.LuasBayar, view.NoPeta, view.Harga,
             view.Totalbeli, view.Ptsk, view.keyPersil, view.LuasPBT,
             view.Nama, view.Keterangan, view.LuasOverlap, view.IsSelected
             )
                =
            (this.IdBidang, this.Alias, this.Desa, this.Project,
             this.Pemilik, this.SuratAsal, this.LuasSurat, this.LuasInternal,
             this.LuasPBT, this.LuasDibayar, this.NoPeta, this.Harga,
             this.TotalBeli, this.PTSK, this.keyPersil, luasPBT,
             this.Nama, this.Keterangan, this.LuasOverlap, isSelected
            );

            return view;
        }
    }

    public class PembayaranBidang
    {
        public string keyPersil { get; set; }
        public bool selected { get; set; }
        public string noMemo { get; set; }
        public double? jumlahBayar { get; set; }
        public string jenisBayar { get; set; }
        public DateTime? tglCreatedBayar { get; set; }
        public DateTime? tglPenyerahan { get; set; }
        public Giro[] giro { get; set; } = new Giro[0];
    }
}
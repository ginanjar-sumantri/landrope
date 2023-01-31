
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using GenWorkflow;
using graph.mod;
using GraphConsumer;
using landrope.common;
using landrope.hosts;
using landrope.mod2;
using landrope.mod4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using flow.common;

namespace ImportPembayaran
{
    public class Pembayaran
    {
        LandropePayContext contextes;
        GraphHostConsumer ghost;

        public Pembayaran(LandropePayContext landropePayContext)
        {
            this.contextes = landropePayContext;
        }
        public string CreateTahap(BayarCore Bayars, bool OverWrite)
        {
            try
            {
                var ent = new Bayar();
                var old = contextes.bayars.FirstOrDefault(x => x.nomorTahap == Bayars.nomorTahap && x.keyProject == Bayars.keyProject);

                if (old == null)
                {
                    ent.FromCore2(Bayars);
                    ent.key = entity.MakeKey;
                    ent.created = DateTime.Now;
                    Bayars.key = ent.key;

                    contextes.bayars.Insert(ent);
                    contextes.SaveChanges();
                }
                else
                {
                    if (OverWrite)
                    {

                        contextes.bayars.Remove(old);
                        contextes.SaveChanges();

                        ent.FromCore2(Bayars);
                        ent.key = entity.MakeKey;
                        ent.created = DateTime.Now;
                        Bayars.key = ent.key;

                        contextes.bayars.Insert(ent);
                        contextes.SaveChanges();
                    }
                    else
                        Bayars.key = old.key;


                }

                return Bayars.key;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void AssignBidang(string keyTahap, string keyPersil)
        {
            try
            {
                var byr = contextes.bayars.FirstOrDefault(x => x.key == keyTahap);
                var bdg = byr.bidangs.Any(x => x.keyPersil == keyPersil);

                if (bdg != true)
                {
                    var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = keyPersil };

                    if (string.IsNullOrEmpty(byr.keyDesa))
                    {
                        var desa = GetPersil(keyPersil).basic.current.keyDesa;
                        byr.keyDesa = desa;
                    }

                    byr.AddDetailBidang(byrdtlbidang);

                    contextes.bayars.Update(byr);
                    contextes.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void SudahLunas(BayarDtlCoreExt CoreExt, GraphHostConsumer ghost, GraphContext context)
        {
            try
            {
                var AllBayars = contextes.bayars.Query(x => x.invalid != true);
                var byr = AllBayars.FirstOrDefault(x => x.nomorTahap == CoreExt.noTahap && x.keyProject == CoreExt.keyProject);
                var user = contextes.FindUser(byr.keyCreator);

                var byrdtl = new BayarDtl(user)
                {
                    keyPersil = CoreExt.keyPersil,
                    jenisBayar = JenisBayar.Lunas,
                    Jumlah = CoreExt.Jumlah,
                    noMemo = CoreExt.noMemo
                };

                var byrSubDtl = new BayarSubDtl()
                {
                    keyPersil = CoreExt.keyPersil,
                    Jumlah = CoreExt.Jumlah
                };

                byrdtl.AddSubDetail(byrSubDtl);
                byrdtl.tglBayar = CoreExt.tglBayar;
                byr.AddDetail(byrdtl);

                var bdg = byr.bidangs.Any(x => x.keyPersil == CoreExt.keyPersil);

                if (bdg == false)
                {
                    var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = CoreExt.keyPersil };
                    if (string.IsNullOrEmpty(byr.keyDesa))
                    {
                        var desa = GetPersil(CoreExt.keyPersil).basic.current.keyDesa;
                        byr.keyDesa = desa;
                    }
                    byr.AddDetailBidang(byrdtlbidang);
                }

                contextes.bayars.Update(byr);
                contextes.SaveChanges();

                var persil = GetPersil(CoreExt.keyPersil);
                persil.luasPelunasan = persil.basic.current.luasDibayar;
                contextes.persils.Update(persil);
                contextes.SaveChanges();

                var Gs = ghost.Get(byrdtl.instkey).GetAwaiter().GetResult();
                Gs.lastState.state = ToDoState.complished_;
                Gs.lastState.time = DateTime.Now;
                Gs.closed = true;

                ghost.Update(Gs, context);

                BebasBidang(CoreExt.keyPersil);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void BelumLunas(BayarDtlCoreExt CoreExt, GraphHostConsumer ghost, GraphContext context)
        {
            try
            {
                var AllBayars = contextes.bayars.Query(x => x.invalid != true);
                var byr = AllBayars.FirstOrDefault(x => x.nomorTahap == CoreExt.noTahap && x.keyProject == CoreExt.keyProject);
                var user = contextes.FindUser(byr.keyCreator);

                var byrdtl = new BayarDtl(user)
                {
                    keyPersil = CoreExt.keyPersil,
                    jenisBayar = CoreExt.jenisBayar,
                    Jumlah = CoreExt.Jumlah,
                    noMemo = CoreExt.noMemo
                };

                var byrSubDtl = new BayarSubDtl()
                {
                    keyPersil = CoreExt.keyPersil,
                    Jumlah = CoreExt.Jumlah
                };

                byrdtl.AddSubDetail(byrSubDtl);
                byrdtl.tglBayar = CoreExt.tglBayar;
                byr.AddDetail(byrdtl);

                var bdg = byr.bidangs.Any(x => x.keyPersil == CoreExt.keyPersil);

                if (bdg == false)
                {
                    var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = CoreExt.keyPersil };
                    if (string.IsNullOrEmpty(byr.keyDesa))
                    {
                        var desa = GetPersil(CoreExt.keyPersil).basic.current.keyDesa;
                        byr.keyDesa = desa;
                    }
                    byr.AddDetailBidang(byrdtlbidang);
                }

                contextes.bayars.Update(byr);
                contextes.SaveChanges();

                var Gs = ghost.Get(byrdtl.instkey).GetAwaiter().GetResult();
                Gs.lastState.state = ToDoState.complished_;
                Gs.lastState.time = DateTime.Now;
                Gs.closed = true;

                ghost.Update(Gs, context);

                BebasBidang(CoreExt.keyPersil);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void SaveLuas(BidangCommand Cmd)
        {
            try
            {
                var userKey = contextes.users.FirstOrDefault(x => x.identifier == "developer").key;
                var persil = GetPersil(Cmd.keyPersil);
                var last = persil.basic.entries.LastOrDefault();

                var item = new PersilBasic();
                item = persil.basic.current;
                item.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
                item.luasInternal = Cmd.luasInternal == null ? item.luasInternal : Cmd.luasInternal;
                item.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;

                var newEntries1 =
                       new ValidatableEntry<PersilBasic>
                       {
                           created = DateTime.Now,
                           en_kind = ChangeKind.Update,
                           keyCreator = userKey,
                           keyReviewer = userKey,
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
                    item2.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
                    item2.luasInternal = Cmd.luasInternal == null ? item.luasInternal : Cmd.luasInternal;
                    item2.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;

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
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void BatalBidang(string keyProject, int nomorTahap, string keyPersil)
        {
            try
            {
                var persil = GetPersil(keyPersil);

                if (persil != null)
                {
                    persil.en_state = StatusBidang.batal;
                    contextes.persils.Update(persil);

                    var bayar = contextes.bayars.FirstOrDefault(x => x.nomorTahap == nomorTahap && x.keyProject == keyProject);

                    var bidangs = bayar.bidangs;
                    var bidang = bidangs.FirstOrDefault(x => x.keyPersil == keyPersil);

                    if (bidang != null)
                    {
                        List<BayarDtlBidang> listBidang = new List<BayarDtlBidang>();
                        if (bidangs != null)
                            listBidang = bidangs.ToList();

                        listBidang.Remove(bidang);
                        bidangs = listBidang.ToArray();

                        bayar.bidangs = bidangs;
                    }



                    var details = bayar.details;
                    if (bidangs != null)
                    {
                        List<BayarDtl> listDetail = new List<BayarDtl>();
                        listDetail = details.ToList();

                        listDetail.RemoveAll(x => x.keyPersil == keyPersil);
                        details = listDetail.ToArray();

                        bayar.details = details;
                    }

                    contextes.bayars.Update(bayar);
                    contextes.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void BebasBidang(string keyPersil)
        {
            try
            {
                var persil = GetPersil(keyPersil);
                if (persil != null)
                {
                    if (persil.en_state != StatusBidang.bebas)
                    {
                        persil.en_state = StatusBidang.bebas;
                        contextes.persils.Update(persil);
                        contextes.SaveChanges();

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private Persil GetPersil(string key)
        {
            return contextes.persils.FirstOrDefault(p => p.key == key);
        }
    }
}

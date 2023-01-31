using auth.mod;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;
using landrope.common;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace landrope.mod2
{
    public class PersilOverlap
    {
        public PersilOverlap()
        {

        }
        public ObjectId _id { get; set; }
        public string IdBidang { get; set; }
        public double kind { get; set; }
        public string group { get; set; }
        public Overlap[] overlap { get; set; }
        public virtual double totalOverlap { get; set; }

        public Persil persil(ExtLandropeContext context) => context.persils.FirstOrDefault(p => p.IdBidang == IdBidang);

        public void AddDetail(Overlap ov)
        {
            var lst = new List<Overlap>();
            if (overlap != null)
                lst = overlap.ToList();

            lst.Add(ov);
            overlap = lst.ToArray();
        }

        public PersilHeaderView toView(ExtLandropeContext context)
        {
            var view = new PersilHeaderView();
            double? totalLuasOverlap = 0;
            double? SisaLuas = 0;
            string state = string.Empty;
            string jenisproses = string.Empty;

            var persil = this.persil(context);
            if (persil != null)
            {
                var list = new List<Overlap>();
                if (overlap != null)
                {
                    list = overlap.ToList();

                    totalLuasOverlap = list.Sum(x => x.luas);
                    if (persil != null && persil.basic.current.luasSurat != null)
                        SisaLuas = persil.basic.current.luasSurat - totalLuasOverlap;
                }

                if (persil.en_state != null)
                {
                    state = Enum.GetName(typeof(StatusBidang), persil.en_state);
                }

                if (persil.basic.current.en_proses != null)
                {
                    jenisproses = Enum.GetName(typeof(JenisProses), persil.basic.current.en_proses);
                }

                (view.key, view.IdBidang, view.en_state, view.bebas, view.en_proses, view.proses, view.group, view.tahap, view.luasSurat,
                    view.totalOverlap, view.sisaLuas, view.noPeta, view.namaSurat, view.nomorSurat) =
                    (persil.key, IdBidang, persil.en_state, state.ToUpper(), persil.basic.current.en_proses, jenisproses, persil.basic.current.group, persil.basic.current.tahap,
                    persil.basic.current.luasSurat, totalLuasOverlap, SisaLuas,
                    persil.basic.current.noPeta, persil.basic.current.surat.nama, persil.basic.current.surat.nomor);
            }

            return view;
        }

        public PersilHeaderView toView(string key, double totalOverlap, StatusBidang? en_state, JenisProses? en_proses, double? luasSurat, string keyDesa, string noPeta, string nama, string alashak)
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            double? SisaLuas = 0;

            statusBdg = Enum.GetName(typeof(StatusBidang), en_state == null ? 0 : en_state);

            if (en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), en_proses);
            }

            SisaLuas = luasSurat - totalOverlap;

            (view.key, view.IdBidang, view.luasSurat, view.keyDesa,
                view.totalOverlap, view.sisaLuas, view.noPeta, view.namaSurat, view.nomorSurat) =
                (key, IdBidang, luasSurat, keyDesa, totalOverlap, SisaLuas, noPeta, nama, alashak);

            return view;
        }

        public PersilHeaderView toView(PersilOvp persilOvp)
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            double? SisaLuas = 0;

            statusBdg = Enum.GetName(typeof(StatusBidang), persilOvp.en_state == null ? 0 : persilOvp.en_state);

            if (persilOvp.basic.en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), persilOvp.basic.en_proses);
            }

            SisaLuas = persilOvp.basic.luasSurat - totalOverlap;

            (view.key, view.IdBidang, view.luasSurat, view.keyDesa,
                view.totalOverlap, view.sisaLuas, view.noPeta, view.namaSurat, view.nomorSurat) =
                (persilOvp.key, IdBidang, persilOvp.basic.luasSurat, persilOvp.basic.keyDesa, totalOverlap, SisaLuas, persilOvp.basic.noPeta, 
                persilOvp.basic.surat?.nama, persilOvp.basic.surat?.nomor) ;

            return view;
        }

        public PersilOverlapCmd toCmd(ExtLandropeContext context)
        {
            var cmd = new PersilOverlapCmd();
            double? totalLuasOverlap = 0;
            double? SisaLuas = 0;

            var persil = this.persil(context);

            var list = new List<Overlap>();
            if (overlap != null)
            {
                list = overlap.ToList();

                totalLuasOverlap = list.Sum(x => x.luas);
                if (persil != null && persil.basic.current.luasSurat != null)
                    SisaLuas = persil.basic.current.luasSurat - totalLuasOverlap;
            }

            (cmd.IdBidang, cmd.kind, cmd.totalOverlap, cmd.sisaLuas) = (IdBidang, kind, totalLuasOverlap, SisaLuas);

            return cmd;
        }
    }


    public class Overlap
    {
        public string IdBidang { get; set; }
        public double? luas { get; set; }

        public Persil persil(ExtLandropeContext context) => context.persils.FirstOrDefault(p => p.IdBidang == IdBidang);

        public PersilDetailView toView(ExtLandropeContext context)
        {
            var view = new PersilDetailView();
            string state = string.Empty;

            var persil = this.persil(context);

            if (persil != null)
            {
                if (persil.en_state != null)
                {
                    state = Enum.GetName(typeof(StatusBidang), persil.en_state);
                }

                if (persil.en_state != null)
                {
                    state = Enum.GetName(typeof(StatusBidang), persil.en_state);
                }

                if (persil.en_state == null)
                    state = "bebas";

                (view.key, view.IdBidang, view.keyDesa, view.en_state, view.bebas, view.group, view.tahap, view.luas, view.noPeta, view.namaSurat, view.nomorSurat) =
                    (persil.key, IdBidang, persil.basic.current.keyDesa, persil.en_state, state, persil.basic?.current?.group, persil.basic?.current?.tahap, luas,
                    persil.basic?.current?.noPeta, persil.basic?.current?.surat?.nama, persil.basic?.current?.surat?.nomor);
            }

            return view;
        }
    }

    public class PersilOvp
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public PersilBasic basic { get; set; }

        public PersilHeaderView toView(Overlap[] ov)
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            double? totalLuasOverlap = 0;
            double? SisaLuas = 0;

            statusBdg = Enum.GetName(typeof(StatusBidang), en_state == null ? 0 : en_state);

            if (basic.en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), basic.en_proses);
            }

            var list = new List<Overlap>();
            if (ov != null)
            {
                list = ov.ToList();

                totalLuasOverlap = list.Sum(x => x.luas);
                if (basic.luasSurat != null)
                    SisaLuas = basic.luasSurat - totalLuasOverlap;
            }

            (view.key, view.IdBidang, view.luasSurat, view.keyDesa,
                view.totalOverlap, view.sisaLuas, view.noPeta, view.namaSurat, view.nomorSurat) =
                (key, IdBidang, basic.luasSurat, basic.keyDesa, totalLuasOverlap, SisaLuas, basic.noPeta, basic.surat.nama, basic.surat.nomor);

            return view;
        }

        public PersilHeaderView toView(double totalOverlap)
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            double? SisaLuas = 0;

            statusBdg = Enum.GetName(typeof(StatusBidang), en_state == null ? 0 : en_state);

            if (basic.en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), basic.en_proses);
            }

            SisaLuas = basic.luasSurat - totalOverlap;

            (view.key, view.IdBidang, view.luasSurat, view.keyDesa,
                view.totalOverlap, view.sisaLuas, view.noPeta, view.namaSurat, view.nomorSurat) =
                (key, IdBidang, basic.luasSurat, basic.keyDesa, totalOverlap, SisaLuas, basic.noPeta, basic.surat?.nama, basic.surat?.nomor);

            return view;
        }

        public PersilHeaderView toViewHeader()
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            var nama = string.Empty;
            var nomor = string.Empty;

            statusBdg = Enum.GetName(typeof(StatusBidang), en_state == null ? 0 : en_state);

            if (basic.en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), basic.en_proses);
            }

            if (basic.surat != null)
            {
                nama = basic.surat.nama;
                nomor = basic.surat.nomor;
            }


            (view.key, view.IdBidang, view.en_state, view.bebas, view.en_proses, view.proses, view.keyDesa, view.group, view.tahap, view.luasSurat,
                view.noPeta, view.namaSurat, view.nomorSurat) =
                (key, IdBidang, en_state,
                statusBdg.ToUpper(), basic.en_proses,
                jenisproses.ToUpper(), basic.keyDesa, basic.group, basic.tahap,
                basic.luasSurat, basic.noPeta, nama, nomor);

            return view;
        }

        public PersilHeaderView toViewOverlap(IEnumerable<(string idBidang, double? luas)> bidangs)
        {
            var view = new PersilHeaderView();
            var statusBdg = string.Empty;
            var jenisproses = string.Empty;
            var proj = string.Empty;
            var des = string.Empty;
            var nama = string.Empty;
            var nomor = string.Empty;
            double? SisaLuas = 0;

            var over = bidangs.Where(x => x.idBidang == IdBidang).FirstOrDefault();

            if (over.idBidang != null)
                SisaLuas = over.luas < 0 ? 0 : over.luas;
            else
                SisaLuas = basic.luasSurat;

            statusBdg = Enum.GetName(typeof(StatusBidang), en_state == null ? 0 : en_state);

            if (basic.en_proses != null)
            {
                jenisproses = Enum.GetName(typeof(JenisProses), basic.en_proses);
            }

            if (basic.surat != null)
            {
                nama = basic.surat.nama;
                nomor = basic.surat.nomor;
            }


            (view.key, view.IdBidang, view.keyDesa, view.en_state, view.bebas, view.group, view.tahap, view.luasSurat,
                view.noPeta, view.namaSurat, view.nomorSurat, view.sisaLuas) =
                (key, IdBidang, basic.keyDesa, en_state,
                statusBdg.ToUpper(), basic.group, basic.tahap, basic.luasSurat, basic.noPeta, nama, nomor, SisaLuas);

            return view;
        }
    }

    public class OverlapHeaderHistory
    {
        public string _t { get; set; }
        public string distinctKey { get; set; }
        public DateTime created { get; set; }
        public string creator { get; set; }
        public string action { get; set; }
        public string IdBidang { get; set; }
        public double kind { get; set; }
        public string group { get; set; }
        public double totalOverlap { get; set; }
    }

    public class OverlapDetailHistory 
    {
        public string _t { get; set; }
        public string distinctKey { get; set; }
        public DateTime created { get; set; }
        public string creator { get; set; }
        public string action { get; set; }
        public string IdBidang { get; set; }
        public string IdBidangOverlap { get; set; }
        public double? luasOverlap { get; set; }
    }
}

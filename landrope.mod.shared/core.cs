using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using auth.mod;
using DynForm.shared;
using Newtonsoft.Json;

namespace landrope.mod.shared
{
    public class PersilCore : ICoreDetail
    {
        public string key { get; set; }
        [GridColumn(Caption = "Id Bidang", Width = 60)]
        public string IdBidang { get; set; }
        public DateTime created { get; set; }
        public string status { get; set; }
        public bool? regular { get; set; }
        public int? en_state { get; set; }
        public string noNIB { get; set; }
        public string bebas { get; set; }
        public string proses { get; set; }
        public string jenis { get; set; }
        public string alias { get; set; }
        [GridColumn(Caption = "Alas Hak", Width = 120)]
        public string nomorSurat { get; set; }
        [GridColumn(Caption = "Project", Width = 120)]
        public string project { get; set; }
        [GridColumn(Caption = "Desa", Width = 120)]
        public string desa { get; set; }
        [GridColumn(Caption = "No Peta", Width = 80)]
        public string noPeta { get; set; }
        [GridColumn(Caption = "Luas Surat", Width = 80)]
        public double? luasSurat { get; set; }
        [GridColumn(Caption = "Group", Width = 120)]
        public string group { get; set; }
        public int? tahap { get; set; }
        public string mediator { get; set; }
        public string namaSurat { get; set; }
        public string penampung { get; set; }
        public string PTSK { get; set; }
        [GridColumn(Caption = "Pemilik", Width = 120)]
        public string pemilik { get; set; }
        public string map { get; set; }
        public double? luasPelunasan { get; set; }

        public ICore GetCore()
                => this;
    }

    public class PersilCore2
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string noPeta { get; set; }
        public string group { get; set; }
        public string pemilik { get; set; }
        public int? tahap { get; set; }
        public string namaSurat { get; set; }
        public string nomorSurat { get; set; }
        public double? luasSurat { get; set; }
        public double? luasDibayar { get; set; }

    }

    public class PersilCore3
    {
        public string key { get; set; }
        public double? luasPelunasan { get; set; }
        public string reason { get; set; }
    }

    public class PersilCore4
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public string alasHak { get; set; }
        public string noPeta { get; set; }
        public string states { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public double? luasSurat { get; set; }
        public string group { get; set; }
        public string pemilik { get; set; }
        public StateHistoriesCore4[] statehistories { get; set; } = new StateHistoriesCore4[0];
        public PersilCore4 setVillage(string project, string desa)
        {
            this.project = project;
            this.desa = desa;

            return this;
        }
    }

    public class PersilCore5: PersilCore4
    {
        public string ptsk { get; set; }
        public JenisProses? en_proses { get; set; }
    }

    public class PersilCore5Ext : PersilCore5
    {
        public string keyDesa { get; set; }
        public string keyProject { get; set; }
        public string keyPTSK { get; set; }

        public PersilCore5Ext setLocation(string desa, string project)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;

            return this;
        }

        public PersilCore5Ext setPT(string ptsk)
        {
            if (ptsk != null)
                this.ptsk = ptsk;

            return this;
        }
    }

    public class StateHistoriesCore4
    {
        public StatusBidang en_state { get; set; }
        public DateTime date { get; set; }
        public string keyCreator { get; set; }
    }

    public class PersilCoreRpt
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public DateTime created { get; set; }
        public string status { get; set; }
        public bool? regular { get; set; }
        public int? en_state { get; set; }
        public string noNIB { get; set; }
        public string bebas { get; set; }
        public string proses { get; set; }
        public string jenis { get; set; }
        public string alias { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public int? tahap { get; set; }
        public double? luasSurat { get; set; }
        public string mediator { get; set; }
        public string noPeta { get; set; }
        public string namaSurat { get; set; }
        public string nomorSurat { get; set; }
        public string penampung { get; set; }
        public string PTSK { get; set; }
        public string pemilik { get; set; }
        public string map { get; set; }
        public double? luasPelunasan { get; set; }
        public string creator { get; set; }
        public string approver1 { get; set; }
        public string approver2 { get; set; }

        public PersilCoreRpt SetTahapNoNIB(int? noTahap, string noPBT)
        {

            this.tahap = noTahap;
            if (noPBT != null)
                this.noNIB = noPBT;

            return this;
        }

        public PersilCoreRpt SetKeyCreator(string creator, string approver1, string approver2, List<user> listUser)
        {
            if (creator != null)
                this.creator = listUser.FirstOrDefault(lu => lu.key == creator)?.FullName;

            if (approver1 != null)
                this.approver1 = listUser.FirstOrDefault(lu => lu.key == approver1)?.FullName;

            if (approver2 != null)
                this.approver2 = listUser.FirstOrDefault(lu => lu.key == approver2)?.FullName;

            return this;
        }

        public PersilCoreRpt SetCreator(List<user> listUser)
        {


            return this;
        }

        public PersilCoreCSV toCSV()
        {
            var view = new PersilCoreCSV();

            (view.IdBidang, view.Project, view.Desa, view.NoPeta, view.AlasHak, view.Bebas, view.Proses, view.Jenis, view.NoNIB, view.LuasSurat, view.NamaSurat, view.Group,
                view.Tahap, view.Mediator) = (IdBidang, project, desa, noPeta, nomorSurat, bebas, proses, jenis, noNIB, luasSurat, namaSurat, group, tahap, mediator);

            return view;
        }

        public PersilCoreRptCsv ToCSV()
        {
            var view = new PersilCoreRptCsv();
            (
                view.key, view.IdBidang, view.created, view.status,
                view.regular, view.en_state, view.noNIB, view.bebas,
                view.proses, view.jenis, view.alias, view.project,
                view.desa, view.group, view.tahap, view.luasSurat,
                view.mediator, view.noPeta, view.namaSurat, view.nomorSurat,
                view.penampung, view.PTSK, view.pemilik, view.map,
                view.luasPelunasan, view.creator, view.approver1, view.approver2
            )
            =
            (
                this.key, this.IdBidang, Convert.ToDateTime(this.created).ToLocalTime().ToString("yyyy/MM/dd hh:mm:ss tt"), this.status,
                this.regular, this.en_state, this.noNIB, this.bebas,
                this.proses, this.jenis, this.alias, this.project,
                this.desa, this.group, this.tahap, this.luasSurat,
                this.mediator, this.noPeta, this.namaSurat, this.nomorSurat,
                this.penampung, this.PTSK, this.pemilik, this.map,
                this.luasPelunasan, this.creator, this.approver1, this.approver2
            );
            return view;
        }
    }

    public class PersilCoreCSV
    {
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string NoPeta { get; set; }
        public string AlasHak { get; set; }
        public string Bebas { get; set; }
        public string Proses { get; set; }
        public string Jenis { get; set; }
        public string NoNIB { get; set; }
        public double? LuasSurat { get; set; }
        public string NamaSurat { get; set; }
        public string Group { get; set; }
        public int? Tahap { get; set; }
        public string Mediator { get; set; }
    }

    public class PersilCoreRptCsv
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string created { get; set; }
        public string status { get; set; }
        public bool? regular { get; set; }
        public int? en_state { get; set; }
        public string noNIB { get; set; }
        public string bebas { get; set; }
        public string proses { get; set; }
        public string jenis { get; set; }
        public string alias { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public int? tahap { get; set; }
        public double? luasSurat { get; set; }
        public string mediator { get; set; }
        public string noPeta { get; set; }
        public string namaSurat { get; set; }
        public string nomorSurat { get; set; }
        public string penampung { get; set; }
        public string PTSK { get; set; }
        public string pemilik { get; set; }
        public string map { get; set; }
        public double? luasPelunasan { get; set; }
        public string creator { get; set; }
        public string approver1 { get; set; }
        public string approver2 { get; set; }
    }

    public class PersilParentCore : ICoreDetail
    {
        public string key { get; set; }
        [GridColumn(Caption = "Id Bidang", Width = 60)]
        public string IdBidang { get; set; }

        [GridColumn(Caption = "Alas Hak", Width = 120)]
        public string nomorSurat { get; set; }
        [GridColumn(Caption = "Project", Width = 120)]
        public string project { get; set; }
        [GridColumn(Caption = "Desa", Width = 120)]
        public string desa { get; set; }
        [GridColumn(Caption = "No Peta", Width = 80)]
        public string noPeta { get; set; }
        [GridColumn(Caption = "Luas Surat", Width = 80)]
        public double? luasSurat { get; set; }
        [GridColumn(Caption = "Group", Width = 120)]
        public string group { get; set; }

        [GridColumn(Caption = "Pemilik", Width = 120)]
        public string pemilik { get; set; }

        public ICore GetCore()
                => this;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using DynForm.shared;
using GenWorkflow;
using landrope.common;
using landrope.consumers;
using landrope.documents;
using landrope.mod2;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using Microsoft.Extensions.DependencyInjection;
using flow.common;

namespace landrope.mod4.classes
{
    public class MasterPembebasan
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public int en_state { get; set; }
        public DateTime? deal { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string group { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? satuan { get; set; }
        public double? total { get; set; }
        public string noPeta { get; set; }
        public string noTahap { get; set; }

        public MasterPembebasan SetLocation(string project, string desa)
        {
            if (project != null)
                this.Project = project;
            if (desa != null)
                this.Desa = desa;
            return this;
        }

        public MasterPembebasan SetTahap(string notahap)
        {
            if (notahap != null)
                this.noTahap = notahap;

            return this;
        }

        public PersilBebasView toViewPB(ExtLandropeContext context)
        {
            var pview = new PersilBebasView();
            //(var project, var desa) = context.GetVillage(keyDesa);

            (pview.key, pview.IdBidang, pview.state, pview.deal, pview.Project, pview.Desa, pview.group,
                pview.Pemilik, pview.AlasHak, pview.luasDibayar, pview.luasSurat, pview.satuan, pview.total, pview.noPeta, pview.noTahap) =
                (key,
                IdBidang,
                (Enum.TryParse<StatusBidang>(en_state.ToString(), out StatusBidang stp) ? stp : default),
                deal, Project, Desa, group, Pemilik, AlasHak, luasDibayar, luasSurat, satuan, total, noPeta,
                noTahap);

            return pview;
        }
    }

    //[Entity("prapembebasan", "pra_pembebasans")]
    //public class PraPembebasan : namedentity4, IPraPembebasan
    //{
    //    public string instkey { get; set; }

    //    [BsonIgnore]
    //    public GraphMainInstance instance => ghost?.Get(instkey).GetAwaiter().GetResult();
    //    public void CreateGraphInstance(user user)
    //    {
    //        if (ghost == null || instkey != null)
    //            return;

    //        instkey = ghost.Create(user, ToDoType.Deal_Payment, -1).GetAwaiter().GetResult()?.key;
    //    }
    //    IGraphHostConsumer ghost => ContextService.services.GetService<IGraphHostConsumer>();
    //    public string keyProject { get; set; }
    //    public string keyDesa { get; set; }
    //    public string group { get; set; }
    //    public string pemilik { get; set; }
    //    public DateTime? tglKesepakatan { get; set; }
    //    public DateTime? created { get; set; }
    //    public ToDoState currstate => instance?.lastState?.state ?? ToDoState.unknown_;
    //    public string keyCreator { get; set; }
    //}

    //public class PraPembebasanDetail : IPraPembebasanDtl
    //{
    //    public string key { get; set; }
    //    public string keyPersil { get; set; }
    //    public string keyBundle { get; set; } // TaskBundle
    //    public Persil persil(LandropePayContext context) => context.persils.FirstOrDefault(p => p.key == keyPersil);
    //}


    //public class PraPembebasanResult
    //{
    //    public ResultDoc[] infoes { get; set; }

    //    public static DynElement[] MakeLayout(ResultDoc doc, bool editable = false)
    //    {
    //        var docname = DocType.List.FirstOrDefault(d => d.invalid != true && d.key == doc.keyDT)?.identifier;
    //        var dyns = doc.props.Select(k => new DynElement
    //        {
    //            visible = "true",
    //            editable = $"{editable}".ToLower(),
    //            group = $"{docname}|Properties",
    //            label = k.mkey.ToString("g").Replace("_", " "),
    //            value = $"#props.${k.mkey:g}",
    //            nullable = true,
    //            cascade = "",
    //            dependency = "",
    //            correction = false,
    //            inittext = "",
    //            options = "",
    //            swlabels = new string[0],
    //            type = k.val.type switch
    //            {
    //                Dynamic.ValueType.Int => ElementType.Numeric,
    //                Dynamic.ValueType.Number => ElementType.Number,
    //                Dynamic.ValueType.Bool => ElementType.Check,
    //                Dynamic.ValueType.Date => ElementType.Date,
    //                _ => ElementType.Text,
    //            },
    //        });

    //        var exi = new (Existence key, string type, string label)[]
    //        {
    //            (Existence.Soft_Copy,"Check","Di-scan"),
    //            (Existence.Asli,"Check","Asli"),
    //            (Existence.Copy,"Numeric","Copy"),
    //            (Existence.Salinan,"Numeric","Salinan"),
    //            (Existence.Legalisir,"Numeric","Legalisir"),
    //            (Existence.Avoid,"Check","Tidak Diperlukan/Memo"),
    //        };

    //        var existencies = exi.Select(x => new DynElement
    //        {
    //            visible = "true",
    //            editable = $"{editable}".ToLower(),
    //            group = $"{docname}|Keberadaan",
    //            label = x.label,
    //            value = $"#{x.key:g}",
    //            nullable = false,
    //            cascade = "",
    //            dependency = "",
    //            correction = false,
    //            inittext = "",
    //            options = "",
    //            swlabels = new string[0],
    //            xtype = x.type
    //        });

    //        var res = dyns.Union(existencies).ToArray();
    //        return res;
    //    }
    //}

}

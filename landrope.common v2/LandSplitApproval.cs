using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class LandSplitApproval
    {
    }

    public class LandSplitApprovalCore
    {
        //Key Persil Header
        public string keyPersil { get; set; }
        public TypeState type { get; set; } // bebas/belum bebas
        public string remark { get; set; }
        public string[] keyPersils { get; set; } = new string[0];
    }

    public class LandSplitRequestCommad : UpdateRequestCommand
    {
        public string keyPersil { get; set; }
        public string[] keyPersils { get; set; }
    }

    public class LandSplitRequestView : UpdRequestDataView<LandSplitRequestInfoView>
    {
        public string IdBidang { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
    }

    public class LandSplitRequestInfoView : RequestInfoView
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string project { get; set; }
        public string keyProject { get; set; }
        public string desa { get; set; }
        public string keyDesa { get; set; }
        public string ptsk { get; set; }
        public string keyPTSK { get; set; }
        public string type { get; set; }
        public TypeState typeState { get; set; }
        public LandSplitRequestDetailView[] details { get; set; } = new LandSplitRequestDetailView[0];
        public LandSplitRequestInfoView() { }
        public LandSplitRequestInfoView(string keyPersil, string IdBidang,
                                        string project, string keyProject,
                                        string desa, string keyDesa,
                                        string ptsk, string keyPTSK,
                                        TypeState typeState, LandSplitRequestDetailView[] details)
        {
            (this.keyPersil, this.IdBidang, this.project, this.keyProject,
                this.desa, this.keyDesa, this.ptsk, this.keyPTSK,
                this.type, this.typeState, this.details) =
                (keyPersil, IdBidang, project, keyProject,
                desa, keyDesa, ptsk, keyPTSK,
                typeState.TypeStateDesc(), typeState, details);
        }
    }

    public class LandSplitRequestDetailView
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PTSK { get; set; }
        public string Alashak { get; set; }
        public string NoPeta { get; set; }
        public string StatusTanah { get; set; }
        public int? Tahap { get; set; }

    }

    public class PersilLandSplit
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public string alasHak { get; set; }
        public string noPeta { get; set; }
        public string states { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public string ptsk { get; set; }
        public double? luasSurat { get; set; }
        public string group { get; set; }
        public string pemilik { get; set; }
        public string keyDesa { get; set; }
        public string keyProject { get; set; }
        public string keyPTSK { get; set; }
        public PersilLandSplit setLocation(string desa, string project)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;

            return this;
        }

        public PersilLandSplit setPT(string ptsk)
        {
            if (ptsk != null)
                this.ptsk = ptsk;

            return this;
        }
    }
}

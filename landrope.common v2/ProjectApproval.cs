using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public  class ProjectApproval
    {
    }

    public class ProjectApprovalCore
    {
        public string key { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public TypeState type { get; set; } // bebas/belum bebas
        public string remark { get; set; }
        public string[] keyPersils { get; set; } = new string[0];
    }

    public class ProjectRequestView : UpdRequestDataView<ProjectRequestInfoView>
    {

    }

    public class ProjectRequestInfoView : RequestInfoView
    {
        public string project { get; set; }
        public string keyProject { get; set; }
        public string desa { get; set; }
        public string keyDesa { get; set; }
        public string ptsk { get; set; }
        public string keyPTSK { get; set; }
        public string type { get; set; }
        public TypeState typeState { get; set; }
        public ProjectRequestDetailView[] details { get; set; } = new ProjectRequestDetailView[0];

        public ProjectRequestInfoView(string keyProject, string keyDesa, string keyPTSK, TypeState type, string project, string desa, string ptsk)
        {
            (this.project,
                this.desa,
                this.ptsk,
                this.keyProject,
                this.keyDesa,
                this.keyPTSK,
                this.type,
                this.typeState) = 
            (project, 
            desa, 
            ptsk,
            keyProject,
            keyDesa,
            keyPTSK,
            EnumHelpers.TypeStateDesc(type),
            type);
        }

        //public StateRequestInfoView(RequestState req, TypeState ty)
        //{
        //    (request, type) = (EnumHelpers.RequestStateDesc(req), EnumHelpers.TypeStateDesc(ty));
        //}

        public ProjectRequestInfoView()
        {
            
        }
    }

    public class ProjectRequestDetailView
    {
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PTSK { get; set; }
        public string Alashak { get; set; }
        public string Group { get; set; }
        public string NoPeta { get; set; }
        public string StatusTanah { get; set; }
        public int Tahap { get; set; }

    }

    public class ProjectRequestCommad : UpdateRequestCommand
    {
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string KeyPTSK { get; set; }
        public string[] keyPersils { get; set; }
    }

    public class PersilProject
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
    }

    public class PersilProjectExt : PersilProject
    {
        public string keyDesa { get; set; }
        public string keyProject { get; set; }
        public string keyPTSK { get; set; }
        public PersilProjectExt setLocation(string desa, string project)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;

            return this;
        }

        public PersilProjectExt setPT(string ptsk)
        {
            if (ptsk != null)
                this.ptsk = ptsk;

            return this;
        }
    }
}

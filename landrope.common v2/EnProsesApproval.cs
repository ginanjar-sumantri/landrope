using landrope.common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace landrope.common
{
    public interface IEnProsesApproval { }

    public class EnProsesApprovalView : UpdRequestDataView<EnProsesApprovalInfoView>
    {

    }

    public class EnProsesApprovalInfoView : RequestInfoView
    {
        public string request { get; set; }
        public JenisProses enumRequest { get; set; }
        public TypeState type { get; set; }
        public EnProasesApprovalDetailView[] details { get; set; } = new EnProasesApprovalDetailView[0];

        public EnProsesApprovalInfoView()
        {

        }

        public EnProsesApprovalInfoView(JenisProses en_proses, TypeState type, EnProasesApprovalDetailView[] detailsBidang)
        {
            (this.request, this.enumRequest, this.type, this.details) 
                = 
            (en_proses.ToDesc(), en_proses, type, detailsBidang);
        }
    }

    public class EnProasesApprovalDetailView
    {
        public string keyPersil { get; set; }
        public string idBidang { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string alasHak { get; set; }
        public string nomorPeta { get; set; }
        public string group { get; set; }
        public string statusTanah { get; set; }
        public int? tahap { get; set; }
    }

    public class EnProsesApprovalCore
    {
        public string key { get; set; }
        public JenisProses en_proses { get; set; }
        public TypeState type { get; set; }
        public string remark { get; set; }
        public string[] keyPersils { get; set; } = new string[0];
    }

    public class EnProsesApprovalCoreExt: EnProsesApprovalCore
    {
        public UpdateRequestCommand cmd { get; set; }
    }

    public class EnProsesApprovalCmd
    {
        public bool approve { get; set; }
        public UpdateRequestCommand command { get; set; }
    }
}
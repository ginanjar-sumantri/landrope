using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class StateApproval
    {
    }

    public class StateApprovalCore
    {
        public string key { get; set; }

        public RequestState request { get; set; }

        public TypeState type { get; set; }

        public string[] keyPersils { get; set; } = new string[0];
    }

    public class UpdStateRequestView : UpdRequestDataView<StateRequestInfoView>
    {
        
    }

    public class StateRequestInfoView : RequestInfoView
    {
        public string request { get; set; }
        public string type { get; set; }
        public StateRequestDetailView[] details { get; set; } = new StateRequestDetailView[0];

        public StateRequestInfoView(RequestState req, TypeState ty, StateRequestDetailView[] dtls)
        {
            (request, type, details) = (EnumHelpers.RequestStateDesc(req), EnumHelpers.TypeStateDesc(ty), dtls);
        }

        public StateRequestInfoView(RequestState req, TypeState ty)
        {
            (request, type) = (EnumHelpers.RequestStateDesc(req), EnumHelpers.TypeStateDesc(ty));
        }

        public StateRequestInfoView()
        {

        }
    }

    public class StateRequestDetailView
    {
        public string key { get; set; }
        public string keyPersil { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string Alashak { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
    }

    public class StateRequestCommad : UpdateRequestCommand
    {
        public string[] keyPersils { get; set; }
    }
}

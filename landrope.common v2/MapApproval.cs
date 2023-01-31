using flow.common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class ApprovalReqCore
    {
        public string idBidang { get; set; }
        public ShapeCore[] location { get; set; } = new ShapeCore[0];
    }

    public class MapApprovalReqCore : UpdateRequestCommand
    {
        public bool bydesa { get; set; }
        public string idBidang { get; set; }
        public string note { get; set; }
        public JenisReqMapApproval requestType { get; set; }
        public ShapeCore[] location { get; set; } = new ShapeCore[0];
    }

    public class MultipleMapApprovalReq
    {
        public ApprovalReqCore[] request { get; set; } = new ApprovalReqCore[0];
        public UpdateRequestCommand command { get; set; }
    }

    public class UpdMapRequestView : UpdRequestDataView<MapRequestInfoView>
    {

    }

    public class MapRequestInfoView : RequestInfoView
    {
        public string requestType { get; set; }
        public bool bydesa { get; set; }
        public string note { get; set; }
        public InfoBidang[] bidangs { get; set; }
    }

    public class InfoBidang
    {
        public string keyProject { get; set; }
        public string project { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string keyPersil { get; set; }
        public string idBidang { get; set; }
    }

    public class geoPointCore
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class ShapeCore
    {
        public geoPointCore[] coordinates { get; set; }
    }

    public class LocationView
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public string Name { get; set; }

        public LocationView()
        {

        }

        public LocationView(string id, string parentID, string name)
        {
            (this.ID, this.ParentID, this.Name)
            =
            (id, parentID, name);
        }
    }

    public class ObjectByKeyDesaview
    {
        public string idBidang { get; set; }
        public bool bebas { get; set; }
        public string kind { get; set; }
        public double luasSurat { get; set; }
        public double luasDibayar { get; set; }
        public DateTime? created { get; set; }
        public ObjectByDesaSurat surat { get; set; }
        public ShapeCore[] map { get; set; }
    }

    public class ObjectByDesaSurat
    {
        public string nomor { get; set; }
        public string nama { get; set; }

        public ObjectByDesaSurat(string nomor, string nama)
        {
            (this.nomor, this.nama) = (nomor, nama);
        }
    }
}
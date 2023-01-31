using landrope.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public interface IPersilApproval { }

    public class PersilApprovalView : UpdRequestDataView<PersilApprovalInfoView>
    {

    }

    public class PersilApprovalInfoView : RequestInfoView
    {
        public string keyPersil { get; set; }
        public string idBidang { get; set; }
        public string keyCreator { get; set; }
        public string keyProject { get; set; }
        public string project { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string jenisProses { get; set; }
        public string jenisAlasHak { get; set; }
        public double luas { get; set; }
        public string pemilik { get; set; }
        public string alasHak { get; set; }
        public string nomorPeta { get; set; }
        public string group { get; set; }
        public string alias { get; set; }

        public PersilApprovalInfoView()
        {

        }
    }

    public class ReqPersilApprovalCore
    {
        public string keyParent { get; set; }
        public string keyCreator { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public JenisProses jenisProses { get; set; }
        public JenisAlasHak jenisAlasHak { get; set; }
        public double luas { get; set; }
        public string pemilik { get; set; }
        public string alasHak { get; set; }
        public string nomorPeta { get; set; }
        public string group { get; set; }
        public string alias { get; set; }
    }

    public class ReqUpdatePersilApprovalCore
    {
        public string requestType { get; set; }
        public string keyParent { get; set; }
        public string idBidangParent { get; set; }
        public JenisProses jenisProses { get; set; }
        public JenisLahan jenisLahan { get; set; }
        public JenisAlasHak jenisAlasHak { get; set; }
        public SifatBerkas statusBerkas { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string nomorPeta { get; set; }
        public string penampung { get; set; }
        public string ptsk { get; set; }


        public DateTime? terimaBerkas { get; set; }
        public DateTime? inputBerkas { get; set; }
        public string pemilik { get; set; }
        public string telpPemilik { get; set; }
        public string group { get; set; }
        public string mediator { get; set; }
        public string telpMediator { get; set; }
        public string alias { get; set; }

        //Surat
        public JenisAlasHak jenisSurat { get; set; }
        public string nomorSurat { get; set; }
        public string namaSurat { get; set; }
        public DateTime? tanggalTerimaSurat { get; set; }
        public string keteranganSurat { get; set; }
        public double? luasSurat { get; set; }
        public double? luasInternal { get; set; }
        public string kekurangan { get; set; }
        public string keterangan { get; set; }

        public UpdateRequestCommand command { get; set; }
    }

    public class PersilApprovalCore
    {
        public bool approve { get; set; }
        public UpdateRequestCommand command { get; set; }
    }
}
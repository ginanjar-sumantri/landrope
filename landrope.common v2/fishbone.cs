using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class FishBone
    {
        public string cat { get; set; }
        public double? value { get; set; }
        public string keySegment1 { get; set; }
        public string Segment1 { get; set; }
        public string keySegment6 { get; set; }
        public string Segment6 { get; set; }
        public FishBone[] detail { get; set; }
    }

    public class FishBoneExt : FishBone
    {
        public double? Denny { get; set; }
        public double? Deas { get; set; }
        public double? Paulus { get; set; }
    }

    public class FishBone_v2
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang en_state { get; set; }
        public DateTime? deal { get; set; }
        public JenisProses en_proses { get; set; }
        public string pemilik { get; set; }
        public string group { get; set; }
        public string nomorSurat { get; set; }
        public double? luasSurat { get; set; }
        public double? luasBayar { get; set; }
        public double? satuan { get; set; }
        public double? tahap { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public string cat1 { get; set; }
        public string cat2 { get; set; }
        public string cat3 { get; set; }
        public string cat4 { get; set; }
        public string cat5 { get; set; }
        public string cat6 { get; set; }
        public double? overlap { get; set; }
        public double? luasTotalOverlap { get; set; }
        public string G1 { get; set; }
        public string G2 { get; set; }
        public string G3 { get; set; }
        public string G4 { get; set; }
        public string G5 { get; set; }
    }

}

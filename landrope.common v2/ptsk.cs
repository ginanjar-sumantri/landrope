using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class PTSKView
    {
        public string key { get; set; }
        public string identifier { get; set; }
        public string code { get; set; }
        public DateTime? terbit { get; set; }
        public string nomor { get; set; }
        public byte[] careas { get; set; }
    }

    public class SKHistoriesView
    {
        public string keyPersil { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string keyPTSK { get; set; }
        public string PTSK { get; set; }
        public string NamaPemilik { get; set; }
        public string AlasHak { get; set; }
        public double? luasSurat { get; set; }
    }

    public class PTSKCore : CoreBase
    {
        public string key { get; set; }
        public string identifier { get; set; }
        public string code { get; set; }
        public DateTime? terbit { get; set; }
        public string nomor { get; set; }

    }
}

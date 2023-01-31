using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class WorklistReportViewModel
    {
        public string NomorRequest { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PTSK { get; set; }
        public string StatusTanah { get; set; }
        public int Tahap { get; set; }
        public int TahapBaru { get; set; }
        public string Group { get; set; }
        //public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double LuasSurat { get; set; }
        public double LuasBayar { get; set; }
        public DateTime Created { get; set; }
    }
}

using System;
using landrope.common;

namespace landrope.mod4
{
    public class WorklistReportModel
    {
        public string Identifier { get; set; }
        public string IdBidang { get; set; }
        public string KeyProject { get; set; }
        public string KeyDesa { get; set; }
        public string KeyPTSK { get; set; }
        public JenisProses? EnProses { get; set; }
        public int? Tahap { get; set; }
        public int? NewTahap { get; set; }
        public string Group { get; set; }
        //public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasBayar { get; set; }
        public DateTime Created { get; set; }

        public WorklistReportViewModel ToViewModel(string project, string desa, string ptsk)
        {
            return new WorklistReportViewModel
            {
                NomorRequest = this.Identifier,
                IdBidang = this.IdBidang,
                AlasHak = this.AlasHak,
                Project = project,
                Desa = desa,
                PTSK = ptsk,
                Group = this.Group,
                StatusTanah = this.EnProses?.ToDesc(),
                Tahap = this.Tahap ?? 0,
                TahapBaru = this.NewTahap ?? 0,
                LuasSurat = this.LuasSurat ?? 0,
                LuasBayar = this.LuasBayar ?? 0,
                Created = this.Created.ToLocalTime()
            };
        }
    }
}

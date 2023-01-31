using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class PersilHeaderView
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang? en_state { get; set; }
        public string noNIB { get; set; }
        public string bebas { get; set; }
        public JenisProses en_proses { get; set; }
        public string proses { get; set; }
        public string project { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string group { get; set; }
        public int? tahap { get; set; }
        public double? luasSurat { get; set; }
        public double? totalOverlap { get; set; }
        public double? sisaLuas { get; set; }
        public string noPeta { get; set; }
        public string namaSurat { get; set; }
        public string nomorSurat { get; set; }
        public string penampung { get; set; }
        public string PTSK { get; set; }
        public string pemilik { get; set; }
        public PersilDetailView[] overlap { get; set; } = new PersilDetailView[0];

        public PersilHeaderView SetLocation(string project, string desa)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            return this;
        }
    }

    public class PersilDetailView
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string keyDesa { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public StatusBidang? en_state { get; set; }
        public string bebas { get; set; }
        public string group { get; set; }
        public int? tahap { get; set; }
        public double? luas { get; set; }
        public string noPeta { get; set; }
        public string namaSurat { get; set; }
        public string nomorSurat { get; set; }
        public string penampung { get; set; }
        public string PTSK { get; set; }
        public string pemilik { get; set; }
        public PersilDetailView SetLocation(string project, string desa)
        {
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            return this;
        }
    }

    public class PersilHeaderCore
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string group { get; set; }
        public PersilDetailCore[] overlap { get; set; } = new PersilDetailCore[0];
    }
    public class PersilDetailCore
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public double? luas { get; set; }
    }

    public class PersilOverlapCmd
    {
        public string IdBidang { get; set; }
        public double kind { get; set; }
        public OverlapCmd[] overlap { get; set; } = new OverlapCmd[0];
        public double? totalOverlap { get; set; }
        public double? sisaLuas { get; set; }
    }

    public class OverlapCmd
    {
        public string IdBidang { get; set; }
        public double? luas { get; set; }
    }
}

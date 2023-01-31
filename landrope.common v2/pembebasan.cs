using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public class PersilBebasCore : CoreBase
    {
        public string key { get; set; }
        public statusChange opr { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? luasInternal { get; set; }
        public double? luasNIBTemp { get; set; }
        public double? satuan { get; set; }
        public double? satuanAkta { get; set; }
        public double? total { get; set; }
        public bool? FgLuasFix { get; set; }
        public bool? pph21 { get; set; } // flag pph21 ditanggung penjual
        public bool? ValidasiPPH { get; set; } // flag validasi pph ditanggung penjual
        public double? ValidasiPPHValue { get; set; } //nilai validasi pph yg ditanggung penjual
        public bool? earlypay { get; set; }
        public bool? fgmandor { get; set; }
        public double? mandor { get; set; }
        public bool? fgpembatalanNIB { get; set; }
        public double? pembatalanNIB { get; set; }
        public bool? fgbaliknama { get; set; }
        public double? baliknama { get; set; }
        public bool? fggantiblanko { get; set; }
        public double? gantiblanko { get; set; }
        public bool? fgkompensasi { get; set; }
        public double? kompensasi { get; set; }
        public bool? fgpajaklama { get; set; }
        public double? pajaklama { get; set; }
        public bool? fgpajakwaris { get; set; }
        public double? pajakwaris { get; set; }
        public bool? fgtunggakanPBB { get; set; }
        public double? tunggakanPBB { get; set; }
        public BiayalainnyaCore[] biayalainnya { get; set; } = new BiayalainnyaCore[0];
    }

    public class PersilDealCore : CoreBase
    {
        public string key { get; set; }
        public string keyNotaris { get; set; }
        public string keyWorker { get; set; }
        public string keyMediator { get; set; }
        public DateTime deal { get; set; }
        public statusChange opr { get; set; }
    }

    public class PersilBebasView
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public StatusBidang state { get; set; }
        public DateTime? deal { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string group { get; set; }
        public string Pemilik { get; set; }
        public string AlasHak { get; set; }
        public double? luasDibayar { get; set; }
        public double? luasSurat { get; set; }
        public double? satuan { get; set; }
        public double? total { get; set; }

        public string noPeta { get; set; }
        public string noTahap { get; set; }
    }

    
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.docdb.Models
{
    public class SummaryBidangSertifikat
    {
        public string IdBidang { get; set; }
        public string Seller { get; set; }
        public int Tahap { get; set; }
        public double NilaiBPHTB { get; set; }
        public string Shm { get; set; }
        public string Shgb { get; set; }
        public double LuasSurat { get; set; }
        public double Satuan { get; set; }
        public double NilaiTransaksi { get; set; }
    }

    public class SummaryBidangGirik
    {
        public string IdBidang { get; set; }
        public string Seller { get; set; }
        public int Tahap { get; set; }
        public string AlasHak { get; set; }
        public double LuasSurat { get; set; }
        public double LuasPBT { get; set; }
        public double Satuan { get; set; }
        public double NilaiTransaksi { get; set; }
    }

    public class ReportSuratTugasDyn : doclayout
    {
        public DateTime? TanggalSurat { get; set; }
        public string NomorSurat { get; set; }
        public string Project { get; set; }
        public string PTSK { get; set; }
        public string Desa { get; set; }
        public SummaryBidangSertifikat[] Summaries { get; set; } = new SummaryBidangSertifikat[0];

        public string PIC { get; set; }

        public ReportSuratTugasDyn()
        {

        }

        public ReportSuratTugasDyn(doclayout layout)
        {
            (this.infos, this.signers, this.notes)
                =
            (layout?.infos, layout?.signers, layout?.notes);
        }
    }
}
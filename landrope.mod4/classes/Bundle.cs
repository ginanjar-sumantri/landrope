using landrope.common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace landrope.mod4
{
    public class MandatoryDocumentReportModel
    {
        public string KeyPersil { get; set; }
        public string DocType { get; set; }
        public string IdBidang { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public int? Tahap { get; set; }
        public string AlasHak { get; set; }
        public string Group { get; set; }
        public string Pemilik { get; set; }
        public double? LuasSurat { get; set; }
        public string UTJ { get; set; }
        public string DP { get; set; }
        public string Lunas { get; set; }
        public Bayar? Bayar { get; set; }
        public DocSettingStatus StatusDocSetting { get; set; }
        public string Status
        {
            get
            {
                List<string> result = new();

                if (StatusDocSetting != DocSettingStatus.ready)
                    return StatusDocSetting.DocSettingStatusMandatoryDocumentDesc();

                foreach (var x in TmpBayarDetail)
                    if (x.Item2 != null)
                        if (x.Item2.tglBayar != null)
                            result.Add($"Sudah {x.Item1.JenisByr()}");
                        else
                            result.Add($"Proses {x.Item1.JenisByr()}");

                if (UTJ == "Y/L" && !result.Any(x => new[] { "UTJ", "Sudah", "Proses" }.Any(y => x.Contains(y))))
                    result.Add("Siap UTJ");
                if (DP == "Y/L" && !result.Any(x => new[] { "DP", "Sudah Pelunasan", "Proses Pelunasan" }.Any(y => x.Contains(y))))
                    result.Add("Siap DP");
                if (Lunas == "Y/L" && !result.Any(x => x.Contains("Pelunasan")))
                    result.Add("Siap Pelunasan");

                return string.Join(", ", result);
            }
        }

        private IEnumerable<(JenisBayar, BayarDtl?)> TmpBayarDetail => Bayar != null ? Bayar.details.Where(x => x.subdetails.Any(y => y.keyPersil == KeyPersil)).GroupBy(x => x.jenisBayar, x => x, (jenis, dat) =>
            {
                var val = dat.Where(y => y.keyPersil == KeyPersil || y.subdetails.Any(z => z.keyPersil == KeyPersil)).OrderByDescending(y => y.tglBayar.HasValue).FirstOrDefault();
                return (jenis, val);
            }).OrderBy(x => x.jenis) : new List<(JenisBayar, BayarDtl?)>();

        public MandatoryDocumentReportViewModel ToViewModel()
        {
            return new MandatoryDocumentReportViewModel()
            {
                IdBidang = this.IdBidang,
                AlasHak = this.AlasHak,
                Group = this.Group,
                Pemilik = this.Pemilik,
                LuasSurat = this.LuasSurat ?? 0,
                DocType = this.DocType,
                Project = this.Project,
                Desa = this.Desa,
                Tahap = this.Tahap,
                UTJ = this.UTJ,
                DP = this.DP,
                Lunas = this.Lunas,
                Status = this.Status
            };
        }
    }
}

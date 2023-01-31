using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{

    public class AssignmentViewM
    {
        public string key { get; set; }
        public string identifier { get; set; }
        public DocProcessStep? step { get; set; }
        public AssignmentCat? category { get; set; }
        public DateTime? issued { get; set; }
        public DateTime? accepted { get; set; }
        public DateTime? closed { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string company { get; set; }
        public uint? duration { get; set; }
        public DateTime? duedate { get; set; }
        public int? overdue { get; set; }
    }

    public class SLAPenugasanRPT
    {
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }

        public SLAPenugasanView[] data { get; set; } = new SLAPenugasanView[0];
    }

    public class SLAPenugasanView
    {
        public string key { get; set; }
        public AssignmentCat jenisPersil { get; set; }
        public string keyPersil { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string project { get; set; }
        public string desa { get; set; }
        public string ptsk { get; set; }
        public string IdBidang { get; set; }
        public string alashak { get; set; }
        public string pemilik { get; set; }
        public string nama { get; set; }
        public double? luas { get; set; }
        public string group { get; set; }
        public DocProcessStep? step { get; set; }
        public DateTime? mulai { get; set; }
        public DateTime? selesai { get; set; }
        public DateTime? target { get; set; }
        public uint? duration { get; set; }
        public int? overdue { get; set; }
        public int? sisaHari { get; set; }
        public SLAPenugasanView setBidang(string IdBidang, string alashak, string pemilik, string nama, double? luas, string group, string project, string desa, string ptsk)
        {
            if (IdBidang != null)
                this.IdBidang = IdBidang;
            if (alashak != null)
                this.alashak = alashak;
            if (pemilik != null)
                this.pemilik = pemilik;
            if (luas != null)
                this.luas = luas;
            if (group != null)
                this.group = group;
            if (project != null)
                this.project = project;
            if (desa != null)
                this.desa = desa;
            if (ptsk != null)
                this.ptsk = ptsk;
            if (nama != null)
                this.nama = nama;

            return this;
        }
    }

    public class SLAPenugasanViewCsv
    {
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PTSK { get; set; }
        public string Proses { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Nama { get; set; }
        public double? Luas { get; set; }
        public string Group { get; set; }
        public string Mulai { get; set; }
        public string Target { get; set; }
        public string Selesai { get; set; }
        public string Testing { get; set; }
        public double? SisaHari { get; set; }
        public double? Overdue { get; set; }

        public List<SLAPenugasanViewCsv> ConvertView(List<AssignmentSLAProject> data)
        {
            List<SLAPenugasanViewCsv> result = new();
            foreach (var project in data)
                foreach (var desa in project.Desas)
                    foreach (var ptsk in desa.ptsks)
                        foreach (var type in ptsk.types)
                            foreach (var x in type.bidangs)
                                result.Add(new SLAPenugasanViewCsv
                                {
                                    Project = project.Project,
                                    Desa = desa.Desa,
                                    PTSK = ptsk.ptsk,
                                    Proses = type.type.Substring(1),
                                    IdBidang = x.IdBidang,
                                    AlasHak = x.AlasHak,
                                    Nama = x.Nama,
                                    Luas = x.Luas,
                                    Group = x.Group,
                                    Mulai = x.Mulai.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                    Target = x.Target.GetValueOrDefault().ToString("yyyy-MM-dd"),
                                    Selesai = x.Selesai.HasValue ? x.Selesai.GetValueOrDefault().ToString("yyyy-MM-dd") : "",
                                    SisaHari = x.SisaHari,
                                    Overdue = x.Overdue
                                });

            return result;
        }
    }

    public class PersilSLA
    {
        public string key { get; set; }
        public string IdBidang { get; set; }
        public string alasHak { get; set; }
        public string keyProject { get; set; }
        public string keyDesa { get; set; }
        public string keyPTSK { get; set; }
        public string desa { get; set; }
        public string project { get; set; }
        public double? luasSurat { get; set; }
        public string group { get; set; }
        public string pemilik { get; set; }
        public string nama { get; set; }
    }
    public class AssignmentSLAProject
    {
        public string Project { get; set; }
        public AssignmentSLAVillage[] Desas { get; set; }
    }

    public class AssignmentSLAVillage
    {
        public string Desa { get; set; }
        public AssignmentSLAPTSK[] ptsks { get; set; }
    }

    public class AssignmentSLAPTSK
    {
        public string ptsk { get; set; }

        public AssignmentSLAField[] types { get; set; }
    }

    public class AssignmentSLAField
    {
        public string type { get; set; }
        public string SLA { get; set; }
        public AssigmentBidang[] bidangs { get; set; }
    }
    public class AssigmentBidang
    {
        public string jenisPersil { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Nama { get; set; }
        public double? Luas { get; set; }
        public string Group { get; set; }
        public DateTime? Mulai { get; set; }
        public DateTime? Target { get; set; }
        public DateTime? Selesai { get; set; }
        public double? SisaHari { get; set; }
        public double? Overdue { get; set; }
    }

    public class SLAAssigmentBidang
    {
        public string Project { get; set; }
        public string Desa { get; set; }
        public string ptsk { get; set; }
        public string type { get; set; }
        public string SLA { get; set; }
        public string jenisPersil { get; set; }
        public string IdBidang { get; set; }
        public string AlasHak { get; set; }
        public string Nama { get; set; }
        public double? Luas { get; set; }
        public string Group { get; set; }
        public DateTime? Mulai { get; set; }
        public DateTime? Target { get; set; }
        public DateTime? Selesai { get; set; }
        public double? SisaHari { get; set; }
        public double? Overdue { get; set; }
    }

    public class ViewAssigmentProcess
    {
        public int No { get; set; }
        public string NoSurat { get; set; }
        public DateTime? TanggalSurat { get; set; }
        public string PIC { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PT { get; set; }
        public string JenisPenugasan { get; set; }
        public int JumlahBidang { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasNib { get; set; }
        public DateTime? TargetPenugasan { get; set; }
        public int DoneBidang { get; set; }
        public double? DoneLuas { get; set; }
        public double? DoneProgress { get; set; }
        public int OutBidang { get; set; }
        public double? OutLuas { get; set; }
        public double? OutProgress { get; set; }
    }

    public class ViewAssigmentProcessDetail
    {
        public int No { get; set; }
        public string NoSurat { get; set; }
        public DateTime? TanggalSurat { get; set; }
        public string PIC { get; set; }
        public string Project { get; set; }
        public string Desa { get; set; }
        public string PT { get; set; }
        public string JenisPenugasan { get; set; }
        public string IdBidang { get; set; }
        public string NamaPemilik { get; set; }
        public string NoAlasHak { get; set; }
        public string NoProdukSebelumnya { get; set; }
        public double? LuasSurat { get; set; }
        public double? LuasNib { get; set; }
        public DateTime? TanggalTarget { get; set; }
        public DateTime? TanggalSPS { get; set; }
        public string NomorSPS { get; set; }
        public DateTime? TanggalSelesai { get; set; }
        public int? Overdue { get; set; }
        public string Kategori1 { get; set; }
        public string Kategori2 { get; set; }
    }

    public class ReportProcessPenugasanDto
    {
        public string KeyProject { get; set; }
        public string KeyDesa { get; set; }
        public string KeyPtsk { get; set; }
        public string KeyJenisPenugasan { get; set; }
        public string KeyPic { get; set; }
        public string KeyStatus { get; set; }
        public bool IsDetailMode { get; set; }

    }
}

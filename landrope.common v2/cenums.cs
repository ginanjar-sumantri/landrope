using flow.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public enum AssignmentCat
    {
        Unknown = 0,
        Girik = 1,
        HGB = 2,
        SHM = 3,
        SHP = 4,
        Hibah = 5
    }

    public enum BudgetCat
    {
        Girik = 1,
        Sertifikat = 2,
        Hibah = 5
    }

    public enum DocProcessStep
    {
        Belum_Bebas = 0,
        Akta_Notaris = 1,
        PBT_Perorangan = 2,
        SPH = 3,
        PBT_PT = 4,
        SK_BPN = 5,
        Cetak_Buku = 6,
        AJB = 7,
        AJB_Hibah = 8,
        SHM_Hibah = 9,
        Penurunan_Hak = 10,
        Peningkatan_Hak = 11,
        Balik_Nama = 12,

        GPS_Dan_Ukur = 13,
        Riwayat_Tanah = 14,
        Bayar_PPh = 15,
        Validasi_PPh = 16,
        Bayar_BPHTB = 17,
        Validasi_BPHTB = 18,
        Bayar_UTJ = 19,
        Bayar_DP = 20,
        Pelunasan = 21,
        Baru_Bebas = 22,
        Proses_Hibah = 23,

        Pengakuan_Hak = 24,
        Balik_Nama_Ahli_Waris = 25,
        Surat_Ukur = 26,
        Ganti_Blanko = 27,
        Pengantar_SK = 28,
        Kuasa_Menjual = 29,
        Kesepakatan_Bersama = 30,
        SK_Kantah = 31,
        SK_Kanwil = 32
    }

    public enum DocCompleteStep
    {
        Riwayat_Tanah = 0,
        _UTJ = 1,
        _DP = 2,
        Akta_Notaris = 3,
        _Pelunasan = 4,
        _PPH = 5,
        _BPHTB = 6,
        _Validasi_PPH = 7,
        _Validasi_BPHTB = 8,
        PBT_Perorangan = 9,
        SPH = 10,
        PBT_PT = 11,
        //SK KANTAH = 12,
        SK_BPN = 13,
        _Pengumuman_Buku = 14,
        Cetak_Buku = 15,
        AJB = 16,
        _Masuk_AJB = 17,
        _Pengumuman_SHM = 18,
        _SHM = 19,
        Turun_Hak = 20,
        Naik_Hak = 21,
        Balik_Nama = 22,

        Pengakuan_Hak = 23,
        Balik_Nama_Ahli_Waris = 24,
        Surat_Ukur = 25,
        Ganti_Blanko = 26,
        Pengantar_SK = 27,
        Kuasa_Menjual = 28,
        Kesepakatan_Bersama = 29,
        SK_Kantah = 30,
        SK_Kanwil = 31

    }

    public enum BudgetPost
    {
        Riwayat_Tanah = 1,
        NIB_Perorangan = 2,
        Pengumuman = 3,
        SHM = 4,
        Lainnya = 5
    }

    public enum BudgetShard
    {
        Percent = 0,
        Mandor = 1,
        BPN = 2,
        Jasa = 3
    }

    [Flags]
    public enum AssignmentTeam
    {
        Unknown = 0,
        Pra_Bebas = 1,
        Paska_Bebas = 2
    }

    public enum ControlLevel
    {
        Pemberi_Tugas = 0,
        Koordinator = 1,
        PIC = 2
    }

    [Flags]
    public enum QueryMode
    {
        Nothing = 0,
        Active = 1,
        Delegated = 2,
        Accepted = 4,
        Complished = 8,
        Overdue = 16
    }

    [Flags]
    public enum SortMode
    {
        Nothing = 0,
        Created = 1,
        CreatedDesc = 3,
        Issued = 4,
        IssuedDesc = 12,
        Delegated = 16,
        DelegatedDesc = 48,
        Accepted = 64,
        AcceptedDesc = 64 + 128,
        Complished = 256,
        ComplishedDesc = 256 + 512,
        Overdue = 1024,
        OverdueDesc = 1024 + 2048
    }

    public enum Existence
    {
        Avoid = 0,
        Asli = 1,
        Copy = 2,
        Legalisir = 3,
        Salinan = 4,
        Soft_Copy = 5
    }
    public enum MetadataKey
    {
        Nomor = 1,
        Tahun = 2,
        Nama = 3,
        Luas = 4,
        Nilai = 5,
        Lunas = 6,
        Jenis = 7,
        Due_Date = 8,
        NIK = 9,
        Nomor_KK = 10,
        Tanggal_Bayar = 11,
        Tanggal_Validasi = 12,
        Tanggal = 13,
        Nama_Lama = 14,
        Nama_Baru = 15,
        NOP = 16,
        Nomor_NIB = 17,
        Nomor_PBT = 18,
        NTPN = 19,
        Nama_Notaris = 20,
        Keterangan = 21,
        Lainnya = 99
    }

    public enum statusChange
    {
        Belum_Deal = 1,
        Belum_Bebas = 2,
        Belum_Batal = 3,
        Deal_Bebas = 4,
        Deal_Batal = 5,
        Deal_Belum = 6
    }

    public enum StatusDeal
    {
        Deal = 0,
        Deal1 = 1,
        Deal1A = 2,
        Deal2 = 3,
        Deal2A = 4,
        Deal3 = 5,
        Deal4 = 6,
        _ = 7,
        Bebas = 8
    }

    public enum JenisBayar
    {
		UTJ = 1,
		DP = 2,
		Lunas = 3,
		Mandor = 4,
		Lainnya = 5,
		Deposit = 6
    }

    public enum ProposionalBayar
    {
        Luas = 1,
        Bidang = 2
    }

    public enum JenisPersil
    {
        Standar = 1,
        Hibah = 2
    }

    public static class Helpers
    {
        public static string Discriminator(this AssignmentCat c) =>
            c switch
            {
                AssignmentCat.Girik => "persilGirik",
                AssignmentCat.SHM => "persilSHM",
                AssignmentCat.HGB => "persilHGB",
                AssignmentCat.SHP => "persilSHP",
                AssignmentCat.Hibah => "PersilHibah",
                _ => null
            };

        public static string SimpleDesc(this AssignmentCat c) =>
            c switch
            {
                AssignmentCat.Girik => "Girik",
                AssignmentCat.SHM => "SHM",
                AssignmentCat.HGB => "HGB",
                AssignmentCat.SHP => "SHP",
                AssignmentCat.Hibah => "Hibah",
                _ => ""
            };

        public static AssignmentCat Category(this string cat)
            => cat switch
            {
                "persilGirik" => AssignmentCat.Girik,
                "persilSHM" => AssignmentCat.SHM,
                "persilHGB" => AssignmentCat.HGB,
                "persilSHP" => AssignmentCat.SHP,
                "PersilHibah" => AssignmentCat.Hibah,
                _ => AssignmentCat.Unknown
            };

        public static JenisAlasHak CategoryToJenis(this AssignmentCat? cat)
            => cat switch
            {
                AssignmentCat.Girik => JenisAlasHak.girik,
                AssignmentCat.SHM => JenisAlasHak.shm,
                AssignmentCat.HGB => JenisAlasHak.hgb,
                AssignmentCat.SHP => JenisAlasHak.shp
            };


        public static string toDealStatus(this StatusDeal a) =>
            a switch
            {
                StatusDeal.Deal => "DEAL|Analis membuatkan peta lokasi",
                StatusDeal.Deal1 => "DEAL 1|Tim PRA sudah membuat SPK",
                StatusDeal.Deal1A => "DEAL 1A|Membuat memo pembayaran tanah",
                StatusDeal.Deal2 => "DEAL 2|Memo pembayaran tanah diterbitkan",
                StatusDeal.Deal2A => "DEAL 2A|GM melakukan pengecekan memo pembayaran tanah ",
                StatusDeal.Deal3 => "DEAL 3|Memo tanah selesai dicek tim Audit",
                _ => "DEAL 4|Tim Accounting dan Kasir telah melakukan approve memo tanah"

            };

        public static string JenisByr(this JenisBayar a) =>
            a switch
            {
                JenisBayar.UTJ => "UTJ",
                JenisBayar.DP => "DP",
                JenisBayar.Lunas => "Pelunasan",
                JenisBayar.Mandor => "Mandor",
                JenisBayar.Lainnya => "Lainnya",
                JenisBayar.Deposit => "Deposit",
                _ => "Sisa Pelunasan"

            };

        public static string JenisByrPivot(this JenisBayar a) =>
            a switch
            {
                JenisBayar.UTJ => "AUTJ",
                JenisBayar.DP => "BDP",
                JenisBayar.Lunas => "CPelunasan",
                JenisBayar.Mandor => "DMandor",
                JenisBayar.Lainnya => "ELainnya",
                JenisBayar.Deposit => "GDeposit",
                _ => "FSisa Pelunasan"

            };

        //public static bool SKNeeds(this DocProcessStep step)
        //	=> step switch
        //	{
        //		DocProcessStep.AJB_Hibah or DocProcessStep.SHM_Hibah or DocProcessStep.PBT_Perorangan or
        //		DocProcessStep.Akta_Notaris or DocProcessStep.Belum_Bebas or DocProcessStep.GPS_Dan_Ukur or
        //		DocProcessStep.Riwayat_Tanah or DocProcessStep.SPH => false,
        //		_ => true
        //	};

        public static DocCompleteStep ToCompleteStep(this DocProcessStep step)
            => step switch
            {
                //DocProcessStep.Belum_Bebas => CompleteStep.Riwayat_Tanah,
                DocProcessStep.Akta_Notaris => DocCompleteStep.Akta_Notaris,
                DocProcessStep.PBT_Perorangan => DocCompleteStep.PBT_Perorangan,
                DocProcessStep.SPH => DocCompleteStep.SPH,
                DocProcessStep.PBT_PT => DocCompleteStep.PBT_PT,
                DocProcessStep.SK_BPN => DocCompleteStep.SK_BPN,
                DocProcessStep.Cetak_Buku => DocCompleteStep.Cetak_Buku,
                DocProcessStep.AJB => DocCompleteStep.AJB,
                DocProcessStep.AJB_Hibah => DocCompleteStep._Masuk_AJB,
                DocProcessStep.SHM_Hibah => DocCompleteStep._SHM,
                DocProcessStep.Penurunan_Hak => DocCompleteStep.Turun_Hak,
                DocProcessStep.Peningkatan_Hak => DocCompleteStep.Naik_Hak,
                DocProcessStep.Balik_Nama => DocCompleteStep.Balik_Nama,
                //DocProcessStep.GPS_Dan_Ukur => CompleteStep.Riwayat_Tanah,
                //DocProcessStep.Riwayat_Tanah => CompleteStep.Riwayat_Tanah,
                DocProcessStep.Bayar_PPh => DocCompleteStep._PPH,
                DocProcessStep.Validasi_PPh => DocCompleteStep._Validasi_PPH,
                DocProcessStep.Bayar_BPHTB => DocCompleteStep._BPHTB,
                DocProcessStep.Validasi_BPHTB => DocCompleteStep._Validasi_BPHTB,

                DocProcessStep.Pengakuan_Hak => DocCompleteStep.Pengakuan_Hak,
                DocProcessStep.Balik_Nama_Ahli_Waris => DocCompleteStep.Balik_Nama_Ahli_Waris,
                DocProcessStep.Surat_Ukur => DocCompleteStep.Surat_Ukur,
                DocProcessStep.Ganti_Blanko => DocCompleteStep.Ganti_Blanko,
                DocProcessStep.Pengantar_SK => DocCompleteStep.Pengantar_SK,
                DocProcessStep.Kuasa_Menjual => DocCompleteStep.Kuasa_Menjual,
                DocProcessStep.Kesepakatan_Bersama => DocCompleteStep.Kesepakatan_Bersama,
                DocProcessStep.SK_Kantah => DocCompleteStep.SK_Kantah,
                DocProcessStep.SK_Kanwil => DocCompleteStep.SK_Kanwil,
                _ => DocCompleteStep.Riwayat_Tanah
            };

        public static DocProcessStep ToProcessStep(this DocCompleteStep step)
            => step switch
            {
                //CompleteStep.Riwayat_Tanah => DocProcessStep.Belum_Bebas ,
                DocCompleteStep.Akta_Notaris => DocProcessStep.Akta_Notaris,
                DocCompleteStep.PBT_Perorangan => DocProcessStep.PBT_Perorangan,
                DocCompleteStep.SPH => DocProcessStep.SPH,
                DocCompleteStep.PBT_PT => DocProcessStep.PBT_PT,
                DocCompleteStep.SK_BPN => DocProcessStep.SK_BPN,
                DocCompleteStep.Cetak_Buku => DocProcessStep.Cetak_Buku,
                DocCompleteStep.AJB => DocProcessStep.AJB,
                DocCompleteStep._Masuk_AJB => DocProcessStep.AJB_Hibah,
                DocCompleteStep._SHM => DocProcessStep.SHM_Hibah,
                DocCompleteStep.Turun_Hak => DocProcessStep.Penurunan_Hak,
                DocCompleteStep.Naik_Hak => DocProcessStep.Peningkatan_Hak,
                DocCompleteStep.Balik_Nama => DocProcessStep.Balik_Nama,
                //CompleteStep.Riwayat_Tanah => //DocProcessStep.GPS_Dan_Ukur ,
                //CompleteStep.Riwayat_Tanah => //DocProcessStep.Riwayat_Tanah ,
                DocCompleteStep._PPH => DocProcessStep.Bayar_PPh,
                DocCompleteStep._Validasi_PPH => DocProcessStep.Validasi_PPh,
                DocCompleteStep._BPHTB => DocProcessStep.Bayar_BPHTB,
                DocCompleteStep._Validasi_BPHTB => DocProcessStep.Validasi_BPHTB,
                DocCompleteStep._UTJ => DocProcessStep.Bayar_UTJ,
                DocCompleteStep._DP => DocProcessStep.Bayar_DP,
                DocCompleteStep._Pelunasan => DocProcessStep.Pelunasan,

                DocCompleteStep.Pengakuan_Hak => DocProcessStep.Pengakuan_Hak,
                DocCompleteStep.Balik_Nama_Ahli_Waris => DocProcessStep.Balik_Nama_Ahli_Waris,
                DocCompleteStep.Surat_Ukur => DocProcessStep.Surat_Ukur,
                DocCompleteStep.Ganti_Blanko => DocProcessStep.Ganti_Blanko,
                DocCompleteStep.Pengantar_SK => DocProcessStep.Pengantar_SK,
                DocCompleteStep.Kuasa_Menjual => DocProcessStep.Kuasa_Menjual,
                DocCompleteStep.Kesepakatan_Bersama => DocProcessStep.Kesepakatan_Bersama,
                DocCompleteStep.SK_Kantah => DocProcessStep.SK_Kantah,
                DocCompleteStep.SK_Kanwil => DocProcessStep.SK_Kanwil,
                _ => DocProcessStep.Riwayat_Tanah
            };

        public static ToDoType StepType(this DocProcessStep step) =>
            step switch
            {
                DocProcessStep.PBT_Perorangan or DocProcessStep.PBT_PT or DocProcessStep.SK_BPN or
                DocProcessStep.Cetak_Buku or DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak or
                DocProcessStep.Balik_Nama or DocProcessStep.Pengantar_SK or DocProcessStep.Pengakuan_Hak or 
                DocProcessStep.SK_Kantah or DocProcessStep.SK_Kanwil => ToDoType.Proc_BPN,
                _ => ToDoType.Proc_Non_BPN
                //DocProcessStep.Akta_Notaris => ToDoType.Proc_Non_BPN
            };
        /*		public static string GetName(this DocProcessStep step)
					=> step.ToString("g").Replace("__", "/").Replace("_", " ");
		*/

        public static int Order(this DocProcessStep step) =>
            step switch
            {
                DocProcessStep.Baru_Bebas => 0,
                DocProcessStep.Riwayat_Tanah or DocProcessStep.Proses_Hibah => 1,
                DocProcessStep.Akta_Notaris => 2,
                DocProcessStep.PBT_Perorangan or DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak => 3,
                DocProcessStep.SPH => 4,
                DocProcessStep.PBT_PT => 5,
                DocProcessStep.SK_BPN => 6,
                DocProcessStep.AJB or DocProcessStep.AJB_Hibah => 7,
                DocProcessStep.Cetak_Buku or DocProcessStep.Balik_Nama => 8,
                _ => 99
            };

        public static DocProcessStep NextStep(this DocProcessStep step, AssignmentCat cat) =>
            step switch
            {
                DocProcessStep.Riwayat_Tanah or DocProcessStep.Proses_Hibah => DocProcessStep.Akta_Notaris,
                DocProcessStep.Akta_Notaris => cat switch
                {
                    AssignmentCat.SHM or AssignmentCat.Hibah => DocProcessStep.Penurunan_Hak,
                    AssignmentCat.SHP => DocProcessStep.Peningkatan_Hak,
                    AssignmentCat.HGB => DocProcessStep.AJB,
                    _ => DocProcessStep.PBT_Perorangan,
                },
                DocProcessStep.PBT_Perorangan => DocProcessStep.SPH,
                DocProcessStep.SPH => DocProcessStep.PBT_PT,
                DocProcessStep.PBT_PT => DocProcessStep.SK_BPN,
                DocProcessStep.SK_BPN or DocProcessStep.SK_Kantah or DocProcessStep.SK_Kanwil => DocProcessStep.Cetak_Buku,
                DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak => DocProcessStep.AJB,
                DocProcessStep.AJB or DocProcessStep.AJB_Hibah => DocProcessStep.Balik_Nama,
                _ => DocProcessStep.Belum_Bebas
            };

        public static string Code(this DocProcessStep step) =>
            step switch
            {
                DocProcessStep.AJB or DocProcessStep.SPH => step.ToString("g"),
                DocProcessStep.Akta_Notaris => "PJB",
                DocProcessStep.Balik_Nama => "BN",
                DocProcessStep.Cetak_Buku => "CTBK",
                DocProcessStep.PBT_Perorangan => "PRGN",
                DocProcessStep.PBT_PT => "PPT",
                DocProcessStep.SK_BPN => "SKB",
                DocProcessStep.Penurunan_Hak => "PH",
                DocProcessStep.Peningkatan_Hak => "NKH",
                DocProcessStep.Pengantar_SK => "SPSK",
                DocProcessStep.Balik_Nama_Ahli_Waris => "BNWR",
                DocProcessStep.Surat_Ukur => "SU",
                DocProcessStep.Ganti_Blanko => "GTBL",
                DocProcessStep.SK_Kantah => "SKKT",
                DocProcessStep.SK_Kanwil => "SKKW",
                _ => "XXX"
            };

        public static string ToDescription(this DocProcessStep step)
            => step switch
            {
                //Belum_Bebas = 0,
                DocProcessStep.Akta_Notaris => "PJB",
                DocProcessStep.PBT_Perorangan => "NIB Perorangan",
                DocProcessStep.SPH => "SPH",
                DocProcessStep.PBT_PT => "PBT PT",
                DocProcessStep.SK_BPN => "SK",
                DocProcessStep.Cetak_Buku => "Cetak Buku",
                DocProcessStep.AJB => "AJB",
                //AJB_Hibah = 8,
                //SHM_Hibah = 9,
                DocProcessStep.Penurunan_Hak => "Penurunan Hak",
                DocProcessStep.Peningkatan_Hak => "Peningkatan Hak",
                DocProcessStep.Balik_Nama => "Balik Nama",

                DocProcessStep.Pengakuan_Hak => "Pengakuan Hak",
                DocProcessStep.Balik_Nama_Ahli_Waris => "Balik Nama Ahli Waris",
                DocProcessStep.Surat_Ukur => "Surat Ukur",
                DocProcessStep.Ganti_Blanko => "Ganti Blanko",
                DocProcessStep.Pengantar_SK => "Pengantar SK",

                DocProcessStep.Kuasa_Menjual => "Kuasa Menjual",
                DocProcessStep.Kesepakatan_Bersama => "Kesepakatan Bersama",

                DocProcessStep.SK_Kantah => "SK Kantah",
                DocProcessStep.SK_Kanwil => "SK Kanwil",

                //GPS_Dan_Ukur = 13,
                //Riwayat_Tanah = 14,
                //Bayar_PPh = 15,
                //Validasi_PPh = 16,
                //Bayar_BPHTB = 17,
                //Validasi_BPHTB = 18,
                //Bayar_UTJ = 19,
                //Bayar_DP = 20,
                //Pelunasan = 21,
                //Baru_Bebas = 22,
                //Proses_Hibah = 23
                _ => null
            };

        public static DocProcessStep ToDocProcessStep(this string step)
            => step switch
            {
                //Belum_Bebas = 0,
                "PJB" => DocProcessStep.Akta_Notaris,
                "NIB Perorangan" => DocProcessStep.PBT_Perorangan,
                "SPH" => DocProcessStep.SPH,
                "PBT PT" => DocProcessStep.PBT_PT,
                "SK" => DocProcessStep.SK_BPN,
                "Cetak Buku" => DocProcessStep.Cetak_Buku,
                "AJB" => DocProcessStep.AJB,
                //AJB_Hibah = 8,
                //SHM_Hibah = 9,
                "Penurunan Hak" => DocProcessStep.Penurunan_Hak,
                "Peningkatan Hak" => DocProcessStep.Peningkatan_Hak,
                "Balik Nama" => DocProcessStep.Balik_Nama,

                "Pengakuan Hak" => DocProcessStep.Pengakuan_Hak,
                "Balik Nama Ahli Waris" => DocProcessStep.Balik_Nama_Ahli_Waris,
                "Surat Ukur" => DocProcessStep.Surat_Ukur,
                "Ganti Blanko" => DocProcessStep.Ganti_Blanko,
                "Pengantar SK" => DocProcessStep.Pengantar_SK,
                "Kuasa Menjual" => DocProcessStep.Kuasa_Menjual,
                "Kesepakatan Bersama" => DocProcessStep.Kesepakatan_Bersama,
                "SK Kantah" => DocProcessStep.SK_Kantah,
                "SK Kanwil" => DocProcessStep.SK_Kanwil,

                //GPS_Dan_Ukur = 13,
                //Riwayat_Tanah = 14,
                //Bayar_PPh = 15,
                //Validasi_PPh = 16,
                //Bayar_BPHTB = 17,
                //Validasi_BPHTB = 18,
                //Bayar_UTJ = 19,
                //Bayar_DP = 20,
                //Pelunasan = 21,
                //Baru_Bebas = 22,
                //Proses_Hibah = 23
            };

        public static (DocProcessStep step, DocProcessStep? next, bool bpn)[] AllSteps(bool girik, bool hibah) =>
            (girik, hibah) switch
            {
                (true, _) => new[] {
                    (DocProcessStep.Baru_Bebas,DocProcessStep.Akta_Notaris,false),
                    (DocProcessStep.Akta_Notaris,(DocProcessStep?)null,false),
                    (DocProcessStep.Akta_Notaris,DocProcessStep.PBT_Perorangan,true),
                    (DocProcessStep.PBT_Perorangan,(DocProcessStep?)null,false),
                    (DocProcessStep.PBT_Perorangan,DocProcessStep.SPH,false),
                    (DocProcessStep.SPH,(DocProcessStep?)null,false),
                    (DocProcessStep.SPH,DocProcessStep.PBT_PT,true),
                    (DocProcessStep.PBT_PT,(DocProcessStep?)null,false),
                    (DocProcessStep.PBT_PT,DocProcessStep.SK_BPN,true),
                    (DocProcessStep.SK_BPN,(DocProcessStep?)null,false),
                    (DocProcessStep.SK_BPN,DocProcessStep.Cetak_Buku,true),
                    (DocProcessStep.SK_Kantah,(DocProcessStep?)null,false),
                    (DocProcessStep.SK_Kantah,DocProcessStep.Cetak_Buku,true),
                    (DocProcessStep.SK_Kanwil,(DocProcessStep?)null,false),
                    (DocProcessStep.SK_Kanwil,DocProcessStep.Cetak_Buku,true),
                    (DocProcessStep.Cetak_Buku,(DocProcessStep?)null,false),
                },
                (false, false) => new[] {
                    (DocProcessStep.Baru_Bebas,DocProcessStep.Akta_Notaris,false),
                    (DocProcessStep.Akta_Notaris,(DocProcessStep?)null,false),
                    (DocProcessStep.Akta_Notaris,DocProcessStep.Penurunan_Hak,true),
                    (DocProcessStep.Penurunan_Hak,(DocProcessStep?)null,false),
                    (DocProcessStep.Penurunan_Hak,DocProcessStep.AJB,false),
                    (DocProcessStep.AJB,(DocProcessStep?)null,false),
                    (DocProcessStep.AJB,DocProcessStep.Balik_Nama,true),
                    (DocProcessStep.Balik_Nama,(DocProcessStep?)null,false),
                },
                _ => new[] {
                    (DocProcessStep.Baru_Bebas,DocProcessStep.Proses_Hibah,false),
                    (DocProcessStep.Proses_Hibah,(DocProcessStep?)null,false),
                    (DocProcessStep.Proses_Hibah,DocProcessStep.Akta_Notaris,false),
                    (DocProcessStep.Akta_Notaris,(DocProcessStep?)null,false),
                    (DocProcessStep.Akta_Notaris,DocProcessStep.Penurunan_Hak,true),
                    (DocProcessStep.Penurunan_Hak,(DocProcessStep?)null,false),
                    (DocProcessStep.Penurunan_Hak,DocProcessStep.AJB,false),
                    (DocProcessStep.AJB,(DocProcessStep?)null,false),
                    (DocProcessStep.AJB,DocProcessStep.Balik_Nama,true),
                    (DocProcessStep.Balik_Nama,(DocProcessStep?)null,false),
                }
            };

        public static string DocProcessStepDesc(this DocProcessStep step)
             =>
            step switch
            {
                DocProcessStep.AJB => "Proses Akta Jual Beli",
                DocProcessStep.Akta_Notaris => "Proses Akta Notaris",
                DocProcessStep.Balik_Nama => "Proses Balik Nama",
                DocProcessStep.Cetak_Buku => "Proses Cetak Buku",
                DocProcessStep.PBT_Perorangan => "Proses Peta Bidang Tanah Perorangan",
                DocProcessStep.PBT_PT => "Proses Peta Bidang Tanah Perseroan",
                DocProcessStep.Penurunan_Hak => "Proses Penurunan Hak",
                DocProcessStep.Peningkatan_Hak => "Peningkatan Hak",
                DocProcessStep.SPH => "Proses Surat Pelepasan Hak", // sebelumnya pengakuan hak
                DocProcessStep.SK_BPN => "Proses Surat Keputusan BPN",
                DocProcessStep.AJB_Hibah => "Proses Akta Jual Beli Hibah",
                DocProcessStep.SHM_Hibah => "Proses Sertifikat Hak Milik",
                DocProcessStep.GPS_Dan_Ukur => "Proses Pengukuran",
                DocProcessStep.Riwayat_Tanah => "Proses Riwayat Tanah",
                DocProcessStep.Bayar_PPh => "Proses Bayar PPH",
                DocProcessStep.Validasi_PPh => "Proses Validasi PPH",
                DocProcessStep.Bayar_BPHTB => "Proses Bayar Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Validasi_BPHTB => "Proses Validasi Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Bayar_UTJ => "Proses Bayar Uang Tanda Jadi",
                DocProcessStep.Bayar_DP => "Proses Bayar Down Payment",
                DocProcessStep.Pelunasan => "Proses Pelunasan",
                DocProcessStep.Baru_Bebas => "Proses Baru Bebas",
                DocProcessStep.Proses_Hibah => "Proses Hibah",

                DocProcessStep.Pengakuan_Hak => "Proses Pengakuan Hak",
                DocProcessStep.Balik_Nama_Ahli_Waris => "Proses Balik Nama Ahli Waris",
                DocProcessStep.Surat_Ukur => "Proses Surat Ukur",
                DocProcessStep.Ganti_Blanko => "ProsesGanti Blanko",
                DocProcessStep.Pengantar_SK => "Proses Pengantar SK",

                DocProcessStep.Kuasa_Menjual => "Proses Kuasa Menjual",
                DocProcessStep.Kesepakatan_Bersama => "Proses Kesepakatan Bersama",

                DocProcessStep.SK_Kantah => "Proses SK Kantah",
                DocProcessStep.SK_Kanwil => "Proses SK Kanwil",
                _ => ""
            };

        public static string DocProcessStepDesc2(this DocProcessStep step)
             =>
            step switch
            {
                DocProcessStep.Akta_Notaris => "AProses Akta Notaris",
                DocProcessStep.PBT_Perorangan => "BProses Peta Bidang Tanah Perorangan",
                DocProcessStep.SPH => "CProses Surat Pelepasan Hak",
                DocProcessStep.PBT_PT => "DProses Peta Bidang Tanah Perseroan",
                DocProcessStep.SK_BPN => "EProses SK BPN",
                DocProcessStep.Cetak_Buku => "FProses Cetak Buku",
                DocProcessStep.AJB => "GProses Akta Jual Beli",
                DocProcessStep.AJB_Hibah => "HProses Akta Jual Beli Hibah",
                DocProcessStep.SHM_Hibah => "IProses Sertifikat Hak Milik",
                DocProcessStep.Penurunan_Hak => "JProses Penurunan Hak",
                DocProcessStep.Peningkatan_Hak => "KPeningkatan Hak",
                DocProcessStep.Balik_Nama => "LProses Balik Nama",
                DocProcessStep.GPS_Dan_Ukur => "MProses Pengukuran",
                DocProcessStep.Riwayat_Tanah => "NProses Riwayat Tanah",
                DocProcessStep.Bayar_PPh => "OProses Bayar PPH",
                DocProcessStep.Validasi_PPh => "PProses Validasi PPH",
                DocProcessStep.Bayar_BPHTB => "QProses Bayar Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Validasi_BPHTB => "RProses Validasi Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Bayar_UTJ => "SProses Bayar Uang Tanda Jadi",
                DocProcessStep.Bayar_DP => "TProses Bayar Down Payment",
                DocProcessStep.Pelunasan => "UProses Pelunasan",
                DocProcessStep.Baru_Bebas => "VProses Baru Bebas",
                DocProcessStep.Proses_Hibah => "WProses Hibah",

                DocProcessStep.Pengakuan_Hak => "ACProses Pengakuan Hak",
                DocProcessStep.Balik_Nama_Ahli_Waris => "YProses Balik Nama Ahli Waris",
                DocProcessStep.Surat_Ukur => "ZProses Surat Ukur",
                DocProcessStep.Ganti_Blanko => "AAProsesGanti Blanko",
                DocProcessStep.Pengantar_SK => "ABProses Pengantar SK",

                DocProcessStep.Kuasa_Menjual => "ADProses Kuasa Menjual",
                DocProcessStep.Kesepakatan_Bersama => "AFProses Kesepakatan Bersama",
                DocProcessStep.SK_Kantah => "AGProses SK Kantah",
                DocProcessStep.SK_Kanwil => "AHProses SK Kanwil",
                _ => ""
            };

        public static string LayoutSurat(this DocProcessStep step)
            =>
            step switch
            {
                DocProcessStep.Penurunan_Hak => "PenurunanHak",
                DocProcessStep.SK_BPN => "SKKantah",
                DocProcessStep.GPS_Dan_Ukur => "GPSdanUkur",
                DocProcessStep.PBT_PT => "PBTPT",
                DocProcessStep.PBT_Perorangan => "PBTPerorang",

                DocProcessStep.Balik_Nama => "BalikNama",
                DocProcessStep.SPH => "SPH",
                DocProcessStep.Akta_Notaris => "PPJB",
                DocProcessStep.Kuasa_Menjual => "KuasaMenjual",
                DocProcessStep.Kesepakatan_Bersama => "KesepakatanBersama",
                
                DocProcessStep.AJB => "AJB",
                DocProcessStep.Pengantar_SK => "PengantarSK",
                DocProcessStep.Cetak_Buku => "CetakBuku",
                DocProcessStep.Balik_Nama_Ahli_Waris => "BalikNamaAhliWaris",
                DocProcessStep.Pengakuan_Hak => "PengakuanHak",

                DocProcessStep.SK_Kantah => "SKKantah",
                DocProcessStep.SK_Kanwil => "SKKanwil",
                _ => "Default"
            };

        public static string PerihalSurat(this DocProcessStep step)
         =>
            step switch
            {
                DocProcessStep.Peningkatan_Hak => "Proses Peningkatan Hak",
                DocProcessStep.AJB_Hibah => "Proses Akta Jual Beli Hibah",
                DocProcessStep.SHM_Hibah => "Proses Sertifikat Hak Milik",
                DocProcessStep.Riwayat_Tanah => "Proses Riwayat Tanah",
                DocProcessStep.Bayar_PPh => "Proses Bayar PPH",
                DocProcessStep.Validasi_PPh => "Proses Validasi PPH",
                DocProcessStep.Bayar_BPHTB => "Proses Bayar Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Validasi_BPHTB => "Proses Validasi Bea Perolehan Hak atas Tanah dan Bangunan",
                DocProcessStep.Bayar_UTJ => "Proses Bayar Uang Tanda Jadi",
                DocProcessStep.Bayar_DP => "Proses Bayar Down Payment",
                DocProcessStep.Pelunasan => "Proses Pelunasan",
                DocProcessStep.Baru_Bebas => "Proses Baru Bebas",
                DocProcessStep.Proses_Hibah => "Proses Hibah",

                DocProcessStep.Pengakuan_Hak => "Proses Pengakuan Hak",
                DocProcessStep.Balik_Nama_Ahli_Waris => "Proses Balik Nama Ahli Waris",
                DocProcessStep.Surat_Ukur => "Proses Surat Ukur",
                DocProcessStep.Ganti_Blanko => "Proses Ganti Blanko",
                DocProcessStep.Pengantar_SK => "Proses Pengantar SK",
                _ => ""
            };
    }
}

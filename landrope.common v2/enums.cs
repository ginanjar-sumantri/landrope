using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
    public enum StatusBidang
    {
        bebas = 0,
        belumbebas = 1,
        batal = 2,
        kampung = 3,
        overlap = 4,
        keluar = 5
    }

    public enum JenisAlasHak
    {
        unknown = 0,
        girik = 1,
        shp = 2,
        hgb = 3,
        shm = 4
    }

    public enum JenisProses
    {
        standar = 0,
        batal = 1,
        overlap = 2,
        lokal = 3,
        hibah = 4,
        parent = 5
    }

    public enum SifatBerkas
    {
        unknown = 0,
        asli,
        salinan,
        legalisir,
        copy
    }

    public enum JenisLahan
    {
        unknown = 0,
        empang = 1,
        sawah = 2,
        kebon = 3
    }

    public enum PelakuProses
    {
        notaris = 0,
        bpn
    }

    public enum StatusPT
    {
        pembeli,
        penampung,
        penjual
    }

    public enum Priority
    {
        kritis,
        amatpenting,
        penting,
        biasa
    }

    public enum JenisBerkas
    {
        unknown = 0,
        ktp = 1,
        kk = 2,
        buktinikah,
        bukticerai = 3,
        npwp = 4,
        aktalahir = 5,
        gantinama = 6,
        aktakematian = 7,
        buktiwaris = 8,
        suratkuasawaris = 9,
        pajakwaris = 10,
        pbb = 11,
        alashak = 12,
        tandaterima = 13
    }

    public enum JenisKtp
    {
        ktp_others = 0,
        ktp_suami = 1,
        ktp_istri = 2
    }

    public enum JenisTrx
    {
        unknown = 0,
        masuk = 1,
        dipinjam = 2,
        keluar = 3,
        dikembalikan = 4,
        transfer = 5
    }

    public enum StatusCategory
    {
        _ = 0,
        PraPembebasan = 1,
        FollowUp = 2
    }

    public enum JenisGiro
    {
        unknown = 0,
        bg = 1,
        cek = 2
    }


    public enum WorkerType
    {
        Sales = 0,
        Manager = 1,
        Mediator = 3
    }

    public enum LogActivityType
    {
        _ = 0,
        Metadata = 1,
        Scan = 2,
        Upload = 3
    }

    public enum LogActivityModul
    {
        Bundle = 0,
        Assigment = 1,
        Pradeals = 2
    }

    public enum JenisMappingDocument
    {
        _ = 0,
        Girik = 1,
        SHM = 2
    }

    public enum BaseContent
    {
        Unknown,
        Text,
        Image
    }

    public enum UploadTaskType
    {
        _ = 0,
        PraDeal = 1,
        Assignment = 2
    }

    public enum FollowUpResult
    {
        _ = 0,
        BelumMauJual = 1,
        BelumBukaHarga = 2,
        JualDanBukaHarga = 3,
        Lainnya = 4
    }

    public enum SisaPelunasanType
    {
        _ = 0,
        ByCutOff = 1
    }

    public enum JenisReqNewPersil
    {
        _ = 0,
        BelumBebas = 1,
        SudahBebas = 2
    }

    public enum JenisReqMapApproval
    {
        _ = 0,
        New = 1,
        Update = 2,
        Kulit = 3
    }

    public enum JenisReject
    {
        _ = 0,
        Reject = 1,
        Abort = 2
    }

    public enum RequestState
    {
        Masuk = 1,
        Batal = 2,
        Keluar = 3,
        Lanjut = 4
    }

    public enum TypeState
    {
        bebas = 0,
        belumbebas = 1,
        _ = 99
    }

    public enum LandOwnershipType
    {
        perorangan = 0,
        waris = 1,
        pt = 2,
        _ = 99
    }

    public enum DocSettingStatus
    {
        notset = 1,
        onhold = 2,
        ready = 3
    }

    // 2020-08-29 -- moved to landrope.mcommon
    //public enum MetadataKey
    //{
    //	Nomor = 1,
    //	Tahun = 2,
    //	Nama = 3,
    //	Luas = 4,
    //	Nilai = 5,
    //	Lunas = 6,
    //	Jenis = 7,
    //	Due_Date = 8,
    //	NIK = 9,
    //	Nomor_KK = 10,
    //	Tanggal_Bayar = 11,
    //	Tanggal_Validasi = 12,
    //	Tanggal = 13,
    //	Nama_Lama = 14,
    //	Nama_Baru = 15,
    //	NOP = 16,
    //	Nomor_NIB = 17,
    //	Nomor_PBT = 18,
    //	NTPN = 19,
    //	Nama_Notaris = 20,
    //	Lainnya = 99
    //}

    // 2020-08-29 -- moved to landrope.mcommon
    //public enum MetadataKey
    //{
    //	Nomor = 1,
    //	Tahun = 2,
    //	Nama = 3,
    //	Luas = 4,
    //	Nilai = 5,
    //	Lunas = 6,
    //	Jenis = 7,
    //	Due_Date = 8,
    //	NIK = 9,
    //	Nomor_KK = 10,
    //	Tanggal_Bayar = 11,
    //	Tanggal_Validasi = 12,
    //	Tanggal = 13,
    //	Nama_Lama = 14,
    //	Nama_Baru = 15,
    //	NOP = 16,
    //	Nomor_NIB = 17,
    //	Nomor_PBT = 18,
    //	NTPN = 19,
    //	Nama_Notaris = 20,
    //	Lainnya = 99
    //}

    public static class EnumHelpers
    {
        public static string FollowUpResultDescription(this FollowUpResult step)
        => step switch
        {
            FollowUpResult.BelumMauJual => "Belum mau menjual",
            FollowUpResult.BelumBukaHarga => "Belum membuka harga",
            FollowUpResult.JualDanBukaHarga => "Siap menjual dan sudah buka harga",
            FollowUpResult.Lainnya => "Lainnya",
            _ => ""
        };

        public static string CategoryTypeDesc(this StatusCategory statusCat)
        => statusCat switch
        {
            StatusCategory.PraPembebasan => "Pra Pembebasan",
            StatusCategory.FollowUp => "Follow Up",
            _ => "category"
        };

        public static string SisaPelunasanTypeDesc(this SisaPelunasanType type)
             => type switch
             {
                 SisaPelunasanType.ByCutOff => "Dengan Tanggal Cut-Off",
                 _ => "Tanpa Tanggal Cut-Off"
             };
        public static string JenisRequestPersiDesc(this JenisReqNewPersil jenis)
             => jenis switch
             {
                 JenisReqNewPersil.BelumBebas => "Belum Bebas",
                 JenisReqNewPersil.SudahBebas => "Sudah Bebas",
                 _ => ""
             };

        public static string UpdateMapProsesDesc(this JenisProses proses)
            => proses switch
            {
                JenisProses.standar => "standard",
                JenisProses.overlap => "claim",
                JenisProses.hibah => "overlap",
                _ => ""
            };
        public static string ToDesc(this JenisProses proses)
            => proses switch
            {
                JenisProses.standar => "Standar",
                JenisProses.overlap => "Claim",
                JenisProses.hibah => "Bintang",
                _ => ""
            };
        public static JenisProses ToEnum(this string proses)
            => proses switch
            {
                "Standar" => JenisProses.standar,
                "Claim" => JenisProses.overlap,
                "Bintang" => JenisProses.hibah
            };
        public static string RequestStateDesc(this RequestState reqState)
            => reqState switch
            {
                RequestState.Masuk => "Masuk",
                RequestState.Batal => "Batal",
                RequestState.Keluar => "Keluar",
                RequestState.Lanjut => "Lanjut",
                _ => ""
            };

        public static string TypeStateDesc(this TypeState typeState)
           => typeState switch
           {
               TypeState.bebas => "Bebas",
               TypeState.belumbebas => "Belum Bebas",
               _ => ""
           };

        public static string RequestStateLongDesc(this RequestState reqState)
            => reqState switch
            {
                RequestState.Masuk => "Masukkan ke planing",
                RequestState.Batal => "Batal",
                RequestState.Keluar => "Keluar dari planing",
                RequestState.Lanjut => "Tidak Batal",
                _ => ""
            };

        public static List<string> JenisAlasHakDesc(this JenisAlasHak a)
            => a switch
            {
                JenisAlasHak.girik => new List<string> { "C", "HIBAH", "AJB" },
                JenisAlasHak.shp => new List<string> { "SHP" },
                JenisAlasHak.hgb => new List<string> { "HGB" },
                JenisAlasHak.shm => new List<string> { "SHM" }
            };

        public static string StatusTanahDesc(this JenisProses statusTanah)
       => statusTanah switch
       {
           JenisProses.standar => "STANDAR",
           JenisProses.batal => "BATAL",
           JenisProses.overlap => "OVERLAP",
           JenisProses.lokal => "LOKAL",
           JenisProses.hibah => "HIBAH",
           JenisProses.parent => "PARENT",
           _ => ""
       };

        public static string ToDesc(this JenisAlasHak? proses)
           => proses switch
           {
               JenisAlasHak.unknown => "Unknown",
               JenisAlasHak.hgb => "HGB",
               JenisAlasHak.shp => "SHP",
               JenisAlasHak.shm => "SHM",
               JenisAlasHak.girik => "Girik",
               _ => ""
            };

        public static string LandOwnershipTypeDesc(this LandOwnershipType landOw)
           => landOw switch
           {
               LandOwnershipType.pt => "PT",
               LandOwnershipType.perorangan => "PERORANGAN",
               LandOwnershipType.waris => "WARIS",
               _ => "NOT IDENTIFY"
           };

        public static string DocSettingStatusDesc(this DocSettingStatus DocSet)
          => DocSet switch
          {
              DocSettingStatus.notset => "NOT SET",
              DocSettingStatus.onhold => "ON HOLD",
              DocSettingStatus.ready => "READY",
              _ => "NOT IDENTIFY"
          };

        public static string DocSettingStatusMandatoryDocumentDesc(this DocSettingStatus DocSet)
          => DocSet switch
          {
              DocSettingStatus.notset => "Belum Tersedia",
              DocSettingStatus.onhold => "On Hold",
              DocSettingStatus.ready => "Ready",
              _ => "Unknown"
          };
    }
}
using flow.common;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mcommon
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
		Baru_Bebas = 22
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
		Lainnya = 99
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

		public static bool SKNeeds(this DocProcessStep step)
			=> step switch
			{
				DocProcessStep.AJB_Hibah or DocProcessStep.SHM_Hibah or DocProcessStep.PBT_Perorangan or
				DocProcessStep.Akta_Notaris or DocProcessStep.Belum_Bebas or DocProcessStep.GPS_Dan_Ukur or
				DocProcessStep.Riwayat_Tanah or DocProcessStep.SPH => false,
				_ => true
			};

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
				_ => DocProcessStep.Riwayat_Tanah
			};

		public static ToDoType StepType(this DocProcessStep step) =>
			step switch
			{
				DocProcessStep.PBT_Perorangan or DocProcessStep.PBT_PT or DocProcessStep.SK_BPN or
				DocProcessStep.Cetak_Buku or DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak or
				DocProcessStep.Balik_Nama => ToDoType.Proc_BPN,
				_ => ToDoType.Proc_Non_BPN
				//DocProcessStep.Akta_Notaris => ToDoType.Proc_Non_BPN
			};
		/*		public static string GetName(this DocProcessStep step)
					=> step.ToString("g").Replace("__", "/").Replace("_", " ");
		*/

		public static int Order(this DocProcessStep step) =>
			step switch
			{
				DocProcessStep.Riwayat_Tanah => 0,
				DocProcessStep.Akta_Notaris => 1,
				DocProcessStep.PBT_Perorangan or DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak => 2,
				DocProcessStep.SPH => 3,
				DocProcessStep.PBT_PT => 4,
				DocProcessStep.SK_BPN => 5,
				DocProcessStep.AJB or DocProcessStep.AJB_Hibah => 6,
				DocProcessStep.Cetak_Buku or DocProcessStep.Balik_Nama => 7,
				_ => 99
			};

		public static DocProcessStep NextStep(this DocProcessStep step, AssignmentCat cat) =>
			step switch
			{
				DocProcessStep.Riwayat_Tanah => DocProcessStep.Akta_Notaris,
				DocProcessStep.Akta_Notaris => cat switch {
					AssignmentCat.SHM => DocProcessStep.Penurunan_Hak,
					AssignmentCat.Hibah => DocProcessStep.Penurunan_Hak,
					AssignmentCat.SHP => DocProcessStep.Peningkatan_Hak,
					AssignmentCat.HGB => DocProcessStep.AJB,
					_ => DocProcessStep.PBT_Perorangan,
				},
				DocProcessStep.PBT_Perorangan => DocProcessStep.SPH,
				DocProcessStep.SPH => DocProcessStep.PBT_PT,
				DocProcessStep.PBT_PT => DocProcessStep.SK_BPN,
				DocProcessStep.SK_BPN => DocProcessStep.Cetak_Buku,
				DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak => DocProcessStep.AJB,
				DocProcessStep.AJB or DocProcessStep.AJB_Hibah => DocProcessStep.Balik_Nama,
				_ => DocProcessStep.Belum_Bebas
			};
	}
}

using flow.common;
using landrope.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maps.mod
{
	[Flags]
	public enum LandState
	{
		Belum_Bebas = 0,
		//Deal = 1,
		Proses_Overlap = 0x82,
		Kumpulkan_Berkas = 0x83,
		PPJB = 0x84,
		PBT_Perorangan = 0x85,
		SPH = 0x86,
		PBT_PT = 0x87,
		SK_BPN = 0x88,
		Cetak_Buku = 0x89,
		Penurunan__Peningkatan_Hak = 0x8a,
		AJB = 0x8b,
		Balik_Nama = 0x8c,

		___ = 0x00,
		Jalan_ = 0x2000,
		Sedang_ = 0x4000,
		Selesai_ = 0x8000,

		//Belum_Bebas_ = 0x00,
		Ditunda = 0x10,
		Kampung__Bengkok_Desa = 0x20,
		Ditunda_Or_Kampung = 0x30,
		Deal = 0x40,
		Sudah_Bebas_ = 0x80,
		Bebas_Or_Deal = 0xc0,

		Standar_ = 0x000,
		Overlap = 0x100,

		//___ = 0x000,
		_Damai = 0x200,
		Overlap_Or_Damai = 0x300,
		Damai_Std = 0x280,
		Kulit_ = 0x1000,
		//Girik = 0x40,
		//Sertifikat = 0x80,
		//Hibah = 0xC0,

		Bebas_Flag = 0x8f,
		State_Flag = 0xf0,
		//Overlap_Flag = 0x200,
		//Rincik_Flag = 0x240
		//Category_Flag = 0xC0,
	}

	[Flags]
	public enum BasicLandState
	{
		Belum_Bebas = 0,
		Proses_Overlap = 0x02,
		Kumpulkan_Berkas = 0x03,
		PPJB = 0x04,
		PBT_Perorangan = 0x05,
		SPH = 0x06,
		PBT_PT = 0x07,
		SK_BPN = 0x08,
		Cetak_Buku = 0x09,
		Penurunan__Peningkatan_Hak = 0x0a,
		AJB = 0x0b,
		Balik_Nama = 0x0c,
		___ = 0x00,
		//Belum_Bebas_ = 0x00,
		Ditunda = 0x10,
		Kampung__Bengkok_Desa = 0x20,
		Deal = 0x40,
		Sudah_Bebas_ = 0x80,
	}

	[Flags]
	public enum SimpleState
	{
		BelumBebas = 0,
		Deal = 1,
		Bebas = 2,
		BintangFlag = 0x10,
		ClaimFlag = 0x20,
		Damai_Std = 0x22,

		Bintang = 0x12,
		Damai =-0x32,
		CalonDamai = 0x31
	}

	public enum PersilCat
	{
		Unknown,
		Kampung,
		Belum,
		Deal,
		Sertifikat,
		Girik,
		Hibah,
		BelumSerti,
		BelumGirik,
		BelumHibah,
		BelumNonHibah,
		NonHibah
	}

	public enum StateLandState
	{
		___ = 0x00,
		Jalan_ = 0x2000,
		Sedang_ = 0x4000,
		Selesai_ = 0x8000,
	}

	public enum FreeLandState
	{
		Belum_Bebas = 0x00,
		Deal = 0x40,
		Sudah_Bebas = 0x80,
	}

	public enum StarState
	{
		Standar_ = 0x000,
		Overlap = 0x100
	}

	public enum ClaimedState
	{
		___ = 0x000,
		_Damai = 0x200,
		_Rincik = 0x400,
	}

	public enum SkinState
	{
		___ = 0,
		Kulit_ = 0x1000
	}

	public enum FlagLandState
	{
		Basic = 0xff,
		State = 0xf0000,
		Free = 0xc0,
		Star = 0x100,
		Overlap = 0x200,
		Rincik = 0x400
	}

	public static class StateExtension
	{
		public static (BasicLandState basic, StateLandState state) Split(this LandState state)
			=> ((BasicLandState)((int)state & (int)FlagLandState.Basic), (StateLandState)((int)state & (int)FlagLandState.State));

		public static string Describe(this LandState state)
		{
			(var basic, var transient) = state.Split();
			return $"{transient:g}{basic:g}".Replace("___", "").Replace("__", "/").Replace("_", " ");
		}


		public static (FreeLandState state, StarState star, ClaimedState claimed) Split3(this LandState state)
			=> ((FreeLandState)((int)state & (int)FlagLandState.Free), (StarState)((int)state & (int)FlagLandState.Star),
			(ClaimedState)((int)state & (int)FlagLandState.Overlap));

		public static (FreeLandState state, StarState star, ClaimedState claimed, SkinState skin) Split4(this LandState state)
			=> ((FreeLandState)((int)state & (int)FlagLandState.Free), (StarState)((int)state & (int)FlagLandState.Star),
			(ClaimedState)((int)state & (int)FlagLandState.Overlap), (SkinState)((int)state & (int)SkinState.Kulit_));

		//public static string Describe3(this LandState state)
		//{
		//	(var free, var star, var claimed) = state.Split3();
		//	return ((star, claimed, free) switch
		//	{
		//		(StarState.Overlap, _,_) => $"{star:g}{claimed:g}",
		//		(StarState.Standar_, ClaimedState._Damai,_) => $"{claimed:g}",
		//		_ => $"{free:g}"
		//	}).Replace("___", "").Replace("__", "/")
		//		.Replace("_", " ");
		//}

		public static string Describe3(this LandState state)
		{
			var (free, star, claimed, kulit) = state.Split4();
			if (((int)free & 0x80) == 0x80)
				free = FreeLandState.Sudah_Bebas;
			return ((star, claimed, free, kulit) switch
			{
				(StarState.Overlap, _, _,_) => $"{star:g}{claimed:g}",
				(StarState.Standar_, ClaimedState._Damai, _, _) => $"{claimed:g}",
				(_, _, _, SkinState.Kulit_) => $"{kulit:g}",
				_ => $"{free:g}"
			}).Replace("___", "").Replace("__", "/")
				.Replace("_", " ");
		}

		public static string DescribeP(this LandState state)
		{
			var stt = state & ~LandState.Sudah_Bebas_;
			return $"{stt:g}".Replace("___", "").Replace("__", "/").Replace("_", " ");
		}

		static LandState[] NoTransient = new LandState[] { LandState.Belum_Bebas, LandState.Ditunda, LandState.Deal, LandState.Kampung__Bengkok_Desa };

		public static LandState ToLandStateEx(this DocProcessStep step, bool ongoing, bool sps, bool hibah, bool pending,
					bool deal, bool kampung)
		{
			var state = (step, pending, deal, hibah, kampung) switch
			{
				(_, true, _, _, _) => LandState.Ditunda,
				(_, _, true, _, _) => LandState.Deal,
				(DocProcessStep.Belum_Bebas, _, _, _, true) => LandState.Kampung__Bengkok_Desa,
				(DocProcessStep.Belum_Bebas, _, _, _, _) => LandState.Belum_Bebas,
				(DocProcessStep.Akta_Notaris, _, _, _, _) => LandState.PPJB,

				(DocProcessStep.PBT_Perorangan, _, _, _, _) => LandState.PBT_Perorangan,
				(DocProcessStep.SPH, _, _, _, _) => LandState.SPH,
				(DocProcessStep.PBT_PT, _, _, _, _) => LandState.PBT_PT,
				(DocProcessStep.SK_BPN, _, _, _, _) => LandState.SK_BPN,
				(DocProcessStep.Cetak_Buku, _, _, _, _) => LandState.Cetak_Buku,

				(DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak, _, _, true, _) => LandState.Penurunan__Peningkatan_Hak | LandState.Overlap,
				(DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak, _, _, _, _) => LandState.Penurunan__Peningkatan_Hak,

				(DocProcessStep.AJB, _, _, true, _) => LandState.AJB | LandState.Overlap,
				(DocProcessStep.AJB, _, _, _, _) => LandState.AJB,

				(DocProcessStep.Balik_Nama, _, _, true, _) => LandState.Balik_Nama | LandState.Overlap,
				(DocProcessStep.Balik_Nama, _, _, _, _) => LandState.Balik_Nama,

				(DocProcessStep.Baru_Bebas, false, false, true, _) => LandState.Proses_Overlap,
				_ => LandState.Kumpulkan_Berkas
			};
			if ((!NoTransient.Contains(state)))
				state |= (step.StepType(), ongoing, sps) switch
				{
					(ToDoType.Proc_BPN, true, false) => LandState.Jalan_,
					(ToDoType.Proc_BPN, _, true) => LandState.Sedang_,
					(ToDoType.Proc_Non_BPN, true, _) => LandState.Sedang_,
					_ => LandState.Selesai_
				};
			return state;
		}

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
				DocProcessStep.SK_BPN => DocProcessStep.Cetak_Buku,
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
				DocProcessStep.PBT_Perorangan => "NIBO",
				DocProcessStep.PBT_PT => "NIBP",
				DocProcessStep.SK_BPN => "SKB",
				DocProcessStep.Penurunan_Hak => "TRH",
				DocProcessStep.Peningkatan_Hak => "NKH",
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
				_ => DocProcessStep.Belum_Bebas

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
				DocProcessStep.SPH => "Proses Surat Pengakuan Hak",
				DocProcessStep.SK_BPN => "Proses Surat Keterangan BPN",
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
				_ => ""
			};

	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public enum ProcessStep
	{
		basic,
		utj,
		dp,
		perjanjian,
		pelunasan,
		bayarpajak,
		pbtpersonal,
		sph,
		pbtpt,
		skkantah,
		skkanwil,
		pengumumanbuku,
		cetakbuku,
		ajb,
		masukajb,
		pengumumanshm,
		shm,
		turunhak,
		naikhak,
		baliknama
	}

	public enum ActionCat
	{
		Default,
		Login,
		GetList,
		GetInfo,
		GetStep,
		GetData,
		SaveItem,
		Approval,
		AddItem
	}

	public static class StepHelper
	{
		public static Dictionary<ProcessStep, string> ProcessDescs = new Dictionary<ProcessStep, string> {
			{ ProcessStep.basic, "Riwayat Tanah" },
			{ ProcessStep.utj, "Bayar Tanda Jadi" },
			{ ProcessStep.dp, "Bayar DP" },
			{ ProcessStep.perjanjian, "Akta Notaris" },
			{ ProcessStep.pelunasan, "Bayar Pelunasan" },
			{ ProcessStep.bayarpajak, "Bayar Pajak-pajak" },
			{ ProcessStep.pbtpersonal, "NIB Perorangan" },
			{ ProcessStep.sph, "Surat Pelepasan Hak" },
			{ ProcessStep.pbtpt, "NIB PT" },
			{ ProcessStep.skkantah, "SK Kantah" },
			{ ProcessStep.skkanwil, "SK Kanwil" },
			{ ProcessStep.cetakbuku, "Cetak Buku" },
			{ ProcessStep.ajb, "Akta Jual Beli" },
			{ ProcessStep.masukajb, "Masuk AJB" },
			{ ProcessStep.shm, "Masuk SHM" },
			{ ProcessStep.turunhak, "Penurunan Hak" },
			{ ProcessStep.naikhak, "Peningkatan Hak" },
			{ ProcessStep.baliknama,"Balik Nama" }
		};
	}

	public enum DocIncoming
	{
		Riwayat_Tanah = 1,
		Validasi_PPH_1 = 2,
		Validasi_BPHTB_1 = 3,
		Validasi_PPH_2 = 4,
		Validasi_BPHTB_2 = 5,
		Validasi_PPH_3 = 6,
		Validasi_BPHTB_3 = 7,
		Pengumuman_Buku = 8,
		Masuk_AJB = 9,
		Pengumuman_SHM = 10,
		Masuk_SHM = 11
	}

	public enum FinProcessStep
	{
		Bayar_UTJ = 1,
		Bayar_DP = 2,
		Bayar_Pelunasan = 3,
		Bayar_PPH_1 = 4,
		Bayar_BPHTB_1 = 5,
		Bayar_PPH_2 = 6,
		Bayar_BPHTB_2 = 7,
		Bayar_PPH_3 = 8,
		Bayar_BPHTB_3 = 9
	}
}
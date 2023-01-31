using flow.common;
//using landrope.mcommon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

namespace landrope.common
{
	[Flags]
	public enum LandState
	{
		Belum_Bebas = 0,
		Deal = 1,
		Proses_Hibah = 2,
		Kumpulkan_Berkas = 3,
		PPJB = 4,
		PBT_Perorangan = 5,
		SPH = 6,
		PBT_PT = 7,
		SK_BPN = 8,
		Cetak_Buku = 9,
		Penurunan__Peningkatan_Hak = 10,
		AJB = 11,
		Balik_Nama = 12,
		Ditunda = 14,
		Kampung__Bengkok_Desa = 15,

		___ = 0x00,
		Jalan_ = 0x10,
		Sedang_ = 0x20,
		Selesai_ = 0x40,

		Belum_Bebas_ = 0x00,
		Sudah_Bebas_ = 0x80,

		Standar_ = 0x000,
		Bintang_ = 0x100,

		Belum_Damai_ = 0x000,
		Damai_ = 0x200,
		//Girik = 0x40,
		//Sertifikat = 0x80,
		//Hibah = 0xC0,

		Basic_Flag = 0x0f,
		State_Flag = 0xf0,
		Star_Flag = 0x100,
		Overlap_Flag = 0x200
		//Category_Flag = 0xC0,
	}

	public enum BasicLandState
	{
		Belum_Bebas = 0,
		Deal = 1,
		Proses_Hibah = 2,
		Kumpulkan_Berkas = 3,
		PPJB = 4,
		PBT_Perorangan = 5,
		SPH = 6,
		PBT_PT = 7,
		SK_BPN = 8,
		Cetak_Buku = 9,
		Penurunan__Peningkatan_Hak = 10,
		AJB = 11,
		Balik_Nama = 12,
		Ditunda = 14,
		Kampung__Bengkok_Desa = 15,
	}

	public enum StateLandState
	{
		___ = 0x00,
		Jalan_ = 0x10,
		Sedang_ = 0x20,
		Selesai_ = 0x40,
	}

	public enum FreeLandState
	{
		Belum_Bebas = 0x00,
		Sudah_Bebas = 0x80,
	}

	public enum StarState
	{
		Standar_ = 0x000,
		Bintang_ = 0x100
	}

	public enum ClaimedState
	{
		Belum_Damai = 0x000,
		Damai = 0x200
	}

	public enum FlagLandState
	{
		Basic = 0x0f,
		State = 0x70,
		Free = 0x80,
		Star = 0x100,
		Overlap = 0x200
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


	public static class Coloring
	{
		public static Dictionary<PersilCat, (Color fill, Color line)> catg_colors = new Dictionary<PersilCat, (Color fill, Color line)>
		{
			{PersilCat.Kampung,(Color.Tan,Color.Maroon) },
			{PersilCat.Belum,(ColorExt.FromHsl(0f,255f,113f),ColorExt.FromHsl(0f,255f,56.5f))},
			{PersilCat.Sertifikat,(Color.LightGreen,Color.DarkGreen) },
			{PersilCat.Girik,(Color.CornflowerBlue,Color.MidnightBlue) },
			{PersilCat.Hibah,(Color.RosyBrown,Color.DarkRed) },
			{PersilCat.BelumSerti,(Color.FromArgb(0,255,0),ColorExt.FromHsl(120f,255f,15f))},
			{PersilCat.BelumGirik,(Color.FromArgb(255,255,0),ColorExt.FromHsl(60f,255f,15f))},
			{PersilCat.BelumHibah,(Color.FromArgb(255,0,0),ColorExt.FromHsl(0f,255f,15f))},
		};

		public static Dictionary<PersilCat, (Color fill, Color line)> catg_colors2 = new Dictionary<PersilCat, (Color fill, Color line)>
		{
			{PersilCat.Kampung,(Color.Brown,Color.Black) },
			{PersilCat.Hibah,(Color.Orchid,Color.Purple)},
			{PersilCat.NonHibah,(Color.Blue,Color.Navy)},
			{PersilCat.BelumGirik,(Color.Red,Color.Maroon) },
			{PersilCat.BelumSerti,(Color.Orange,Color.DarkRed) },
			{PersilCat.BelumHibah,(Color.Red,Color.Maroon)},
			{PersilCat.Deal,(Color.Lime,Color.DarkGreen)},
		};

		public static Dictionary<LandState, (Color fill, Color line)> stat_colors = new Dictionary<LandState, (Color fill, Color line)>
		{
			{LandState.Kampung__Bengkok_Desa,(ColorExt.FromHsl(0f,255f,113f),ColorExt.FromHsl(0f,255f,56.5f))},
			{LandState.Belum_Bebas,(ColorExt.FromHsl(0f,255f,113f),ColorExt.FromHsl(0f,255f,56.5f))},
			{LandState.Kumpulkan_Berkas,(ColorExt.FromHsl(197f,114f,157f),ColorExt.FromHsl(197f,114f,78.5f))},
			{LandState.PPJB,(ColorExt.FromHsl(10.25f,249.375f,145.625f),ColorExt.FromHsl(10.25f,249.375f,72.8125f))},
			{LandState.PBT_Perorangan,(ColorExt.FromHsl(30.75f,238.125f,146.875f),ColorExt.FromHsl(30.75f,238.125f,73.4375f))},
			{LandState.SPH,(ColorExt.FromHsl(41f,232.5f,147.5f),ColorExt.FromHsl(41f,232.5f,73.75f))},
			{LandState.PBT_PT,(ColorExt.FromHsl(82f,210f,150f),ColorExt.FromHsl(82f,210f,75f))},
			{LandState.SK_BPN,(ColorExt.FromHsl(131f,189.5f,135f),ColorExt.FromHsl(131f,189.5f,67.5f))},
			{LandState.Cetak_Buku|LandState.Selesai_,(ColorExt.FromHsl(180f,169f,66f),ColorExt.FromHsl(180f,169f,132f))},
			{LandState.Balik_Nama|LandState.Selesai_,(ColorExt.FromHsl(180f,169f,66f),ColorExt.FromHsl(180f,169f,132f))},
			{LandState.Penurunan__Peningkatan_Hak,(ColorExt.FromHsl(98.33f,203.17f,165f),ColorExt.FromHsl(98.33f,203.17f,82.5f))},
			{LandState.AJB,(ColorExt.FromHsl(131f,189.5f,135f),ColorExt.FromHsl(131f,189.5f,67.5f))}
		};
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

		public static string Describe3(this LandState state)
		{
			(var free, var star, var claimed) = state.Split3();
			return (star == StarState.Bintang_ ? $"{star:g}{claimed:g}" : $"{star:g}{free:g}")
				.Replace("___", "").Replace("__", "/")
				.Replace("_", " ");
		}


		static LandState[] NoTransient = new LandState[] { LandState.Belum_Bebas, LandState.Ditunda, LandState.Deal, LandState.Kampung__Bengkok_Desa };

		public static LandState ToLandState(this DocProcessStep step, bool ongoing, bool sps, bool hibah, bool pending, bool deal, bool kampung, bool damai)
		{
			var state = (step, pending, deal, hibah, kampung) switch
			{
				(_, true, _, _, _) => LandState.Ditunda,
				(_, _, true, _, _) => LandState.Deal,
				(DocProcessStep.Belum_Bebas, _, _, _, true) => LandState.Kampung__Bengkok_Desa,
				(DocProcessStep.Belum_Bebas, _, _, _, _) => LandState.Belum_Bebas,
				(DocProcessStep.Akta_Notaris, _, _, _, _) => LandState.PPJB,
				(DocProcessStep.PBT_Perorangan, _, _, false, _) => LandState.PBT_Perorangan,
				(DocProcessStep.SPH, _, _, false, _) => LandState.SPH,
				(DocProcessStep.PBT_PT, _, _, false, _) => LandState.PBT_PT,
				(DocProcessStep.SK_BPN, _, _, false, _) => LandState.SK_BPN,
				(DocProcessStep.Cetak_Buku, _, _, false, _) => LandState.Cetak_Buku,
				(DocProcessStep.Penurunan_Hak or DocProcessStep.Peningkatan_Hak, _, _, _, _) => LandState.Penurunan__Peningkatan_Hak,
				(DocProcessStep.AJB, _, _, _, _) => LandState.AJB,
				(DocProcessStep.Balik_Nama, _, _, _, _) => LandState.Balik_Nama,
				(DocProcessStep.Baru_Bebas, false, false, true, _) => LandState.Proses_Hibah,
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

			if (hibah)
			{
				state |= LandState.Bintang_;
				if (damai)
					state |= LandState.Damai_;
			}
			return state;
		}

	}
}

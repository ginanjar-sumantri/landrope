using System;
using System.Linq;

namespace landrope.common
{
	public enum LandStatus
	{
		Tanpa_status = 0,
		Belum_Bebas__Sertifikat = 1,
		Hibah__PBT_belum_terbit = 2,
		Hibah__PBT_sudah_terbit = 3,
		Kampung = 4,
		Bengkok_Desa = 5,
		Sudah_Bebas__murni = 6,
		Transisi_murni = 7,
		Transisi_hibah = 8,
	}

	public enum NewLandStatus
	{
		Tanpa_status = 0,
		Kampung = 1,
		Belum_Bebas__Sertifikat = 2,
		Hibah__PBT_belum_terbit = 3,
		Hibah__PBT_sudah_terbit = 4,
		Sudah_Bebas__murni = 5,
		Sudah_Bebas__Hibah__PBT_belum_terbit = 6,
		Sudah_Bebas__Hibah__PBT_sudah_terbit = 7,
		Transisi__Sertifikat = 8,
		Transisi__Hibah = 9,
		Belum_Bebas__Hibah = 10
	}

	public enum Bebastatus
	{
		Unknnown = 0,
		Kampung = 1,
		BelumBebas = 2,
		Transisi = 3,
		Bebas = 4
	}

	public enum HibahStatus
	{
		Unknown = 0,
		Murni = 1,
		Hibah = 2,
	}

	public enum PBTStatus
	{
		NA = 0,
		Belum = 1,
		Sudah = 2,
	}

	public static class Template
	{
		static (int scene, int bebas, int? hibah, int? PBT, NewLandStatus ls)[] templates =
			new (int scene, int bebas, int? hibah, int? PBT, NewLandStatus ls)[]
		{
			//requirement pak CC
			(1,1,null,null,NewLandStatus.Kampung),
			(1,4,1,null,NewLandStatus.Sudah_Bebas__murni),
			(1,4,2,1,NewLandStatus.Hibah__PBT_belum_terbit),
			(1,4,2,2,NewLandStatus.Hibah__PBT_sudah_terbit),
			(1,2,1,null,NewLandStatus.Belum_Bebas__Sertifikat),
			(1,2,2,null,NewLandStatus.Hibah__PBT_belum_terbit),
			(1,3,1,null,NewLandStatus.Sudah_Bebas__murni),
			(1,3,2,null,NewLandStatus.Hibah__PBT_belum_terbit),

			//requirement pak denny
			(2,1,null,null,NewLandStatus.Kampung),
			(2,4,1,null,NewLandStatus.Sudah_Bebas__murni),
			(2,4,2,1,NewLandStatus.Hibah__PBT_belum_terbit),
			(2,4,2,2,NewLandStatus.Hibah__PBT_sudah_terbit),
			(2,2,1,null,NewLandStatus.Belum_Bebas__Sertifikat),
			(2,2,2,null,NewLandStatus.Hibah__PBT_belum_terbit),
			(2,3,1,null,NewLandStatus.Transisi__Sertifikat),
			(2,3,2,null,NewLandStatus.Transisi__Hibah),

			//requirement bu lily
			(3,1,null,null,NewLandStatus.Kampung),
			(3,4,1,null,NewLandStatus.Sudah_Bebas__murni),
			(3,4,2,1,NewLandStatus.Sudah_Bebas__Hibah__PBT_belum_terbit),
			(3,4,2,2,NewLandStatus.Sudah_Bebas__Hibah__PBT_sudah_terbit),
			(3,2,1,null,NewLandStatus.Belum_Bebas__Sertifikat),
			(3,2,2,null,NewLandStatus.Belum_Bebas__Hibah),
			(3,3,1,null,NewLandStatus.Belum_Bebas__Sertifikat),
			(3,3,2,null,NewLandStatus.Belum_Bebas__Hibah),
		};

		public static (NewLandStatus New, LandStatus Old)[] mappers =
			new (NewLandStatus New, LandStatus Old)[]
			{
			(NewLandStatus.Tanpa_status, LandStatus.Tanpa_status),
			(NewLandStatus.Belum_Bebas__Sertifikat ,LandStatus.Belum_Bebas__Sertifikat),
			(NewLandStatus.Belum_Bebas__Hibah ,LandStatus.Hibah__PBT_belum_terbit),
			(NewLandStatus.Kampung ,LandStatus.Kampung),
			(NewLandStatus.Hibah__PBT_belum_terbit ,LandStatus.Hibah__PBT_belum_terbit),
			(NewLandStatus.Hibah__PBT_sudah_terbit ,LandStatus.Hibah__PBT_sudah_terbit),
			(NewLandStatus.Sudah_Bebas__murni ,LandStatus.Sudah_Bebas__murni),
			(NewLandStatus.Sudah_Bebas__Hibah__PBT_belum_terbit ,LandStatus.Sudah_Bebas__murni),
			(NewLandStatus.Sudah_Bebas__Hibah__PBT_sudah_terbit ,LandStatus.Hibah__PBT_sudah_terbit),
			(NewLandStatus.Transisi__Hibah ,LandStatus.Sudah_Bebas__murni),
			(NewLandStatus.Transisi__Sertifikat ,LandStatus.Sudah_Bebas__murni)
			};


		public static NewLandStatus Translate(int scene, (int bebas, int hibah, int PBT) combo)
		{
			var tmp = templates.Where(t => t.scene == scene)
				.Where(t => t.bebas == combo.bebas)
				.Where(t => (t.hibah ?? combo.hibah) == combo.hibah)
				.Where(t => (t.PBT ?? combo.PBT) == combo.PBT)
				.FirstOrDefault();
			return tmp.scene == 0 ? NewLandStatus.Tanpa_status : tmp.ls;
		}
	}
}

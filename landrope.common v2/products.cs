using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class Product
	{
		public DateTime? tanggal { get; set; }
		public string nomor { get; set; }
		public DateTime? tglInput { get; set; }
		public RiwayatArsip arsip { get; set; }
	}

	public class SPH : Product
	{
	}

	public class Akta : Product
	{
		public double? luas { get; set; }
	}

	public class AktaLain : Akta
	{
		public string jenisAkta { get; set; }
	}

	public class Perjanjian : Product
	{
		public AktaLain lainnya { get; set; }
	}

	public class PerjanjianSertifikat : Perjanjian
	{
		public Akta PJB { get; set; }
		public Akta kuasa { get; set; }
	}

	public class PerjanjianGirik : PerjanjianSertifikat
	{
		public Akta kesepakatan { get; set; }
		public Akta waris { get; set; }
	}

	public class NIB : Akta
	{
		public string noNIB { get; set; }
		public string noPBT { get; set; }
		public DateTime? tglSelesai { get; set; }
	}

	public class AJB : Product
	{
	}

	public class SKBPN : Akta
	{
	}

	public class Sertifikat : Akta
	{
		public string nama { get; set; }
	}

	public class HGB : Sertifikat
	{
		public DateTime? expired { get; set; }
	}
}

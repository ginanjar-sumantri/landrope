using landrope.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace landrope.common
{
	public class Berkas : IMultiply
	{
		public DateTime? tglTerima { get; set; }
		public int? tahun { get; set; }
		public string note { get; set; }
		public RiwayatArsip arsip { get; set; }

		public virtual bool CheckDummy() => tglTerima == null && tahun == null && string.IsNullOrEmpty(note) && arsip == null;
	}

	public class BerkasOther : Berkas
	{
		public int en_jenis { get; set; }
		public string jenis { get; set; }
		public string title { get; set; }
	}

	public class AlasHak :Berkas
	{
		public int en_jnsalas { get; set; }
		public string jnsalas { get; set; }
		public string nomor { get; set; }
		public string nama { get; set; }
		public double? luas { get; set; }

		public override bool CheckDummy() => base.CheckDummy() && en_jnsalas == 0 && string.IsNullOrEmpty(nomor) 
																			&& string.IsNullOrEmpty(nama) && luas == null;
	}

	public class RiwayatArsip
	{
		public DateTime? created { get; set; }
		public DateTime? tglMasuk { get; set; }
		public string keyEntering { get; set; }
		public List<doctrx> transactions { get; set; }
	}

	public class RiwayatArsipBasic : RiwayatArsip
	{
		public DateTime? tglCek { get; set; }
		public string note { get; set; }
	}

	public class doctrx : IMultiply
	{
		public DateTime? waktu { get; set; }
		public int en_status { get; set; }
		public string status { get; set; }
		public int en_jenis { get; set; }
		public string jenis { get; set; }
		public int? tenggat { get; set; }

		public bool CheckDummy() => waktu == null && en_status == 0 && en_jenis == 0 && tenggat == null;
	}
}

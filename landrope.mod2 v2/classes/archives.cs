using landrope.common;
//using landrope.mcommon;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace landrope.mod2
{
	public class BerkasBase : DetailBase
	{
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? tglTerima { get; set; }
		public JenisBerkas en_jenis { get; set; }
		public string jenis
		{
			get => en_jenis.ToString("g");
			set { if (Enum.TryParse<JenisBerkas>(value, out JenisBerkas jb)) en_jenis = jb; }
		}
		public RiwayatArsip arsip { get; set; }
	}
	public class Berkas : BerkasBase
	{
		public int? tahun { get; set; }
		public string note { get; set; }
	}

	public class BerkasOther : Berkas
	{
		public string title { get; set; }
	}

	public class AlasHak :Berkas
	{
		public JenisAlasHak en_jnsalas { get; set; }
		public string jnsalas
		{
			get => en_jnsalas.ToString("g");
			set { if (Enum.TryParse<JenisAlasHak>(value, out JenisAlasHak ja)) en_jnsalas = ja; }
		}
		[BundlePropMap("JDOK001", MetadataKey.Nomor, Dynamic.ValueType.String)]
		public string nomor { get; set; }
		[BundlePropMap("JDOK001", MetadataKey.Nama, Dynamic.ValueType.String)]
		public string nama { get; set; }
		[BundlePropMap("JDOK001", MetadataKey.Luas, Dynamic.ValueType.Number)]
		public double? luas { get; set; }
	}

	public class KTP : Berkas
	{
		[BundlePropMap("JDOK002", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass =typeof(KTP_Suami))]
		[BundlePropMap("JDOK003", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(KTP_IStri))]
		[BundlePropMap("JDOK028", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(KTP_Ahwar))]
		public string nomor { get; set; }
		public JenisKtp jenisktp { get; set; }
	}

	public class KTP_Suami : KTP { }
	public class KTP_IStri : KTP { }
	public class KTP_Ahwar : KTP { }

	public class PBB : Berkas
	{
		public string NOP { get; set; }
		public int nilai { get; set; }
		public bool lunas { get; set; }
	}

	public class TandaTerima : BerkasBase 
	{ 
		public string keySubstitute { get; set; }
	}
	public class RiwayatArsip : DetailBase
	{
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? created { get; set; }
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? tglMasuk { get; set; }
		public string keyEntering { get; set; }
		public string location { get; set; }
		public List<doctrx> transactions { get; set; }
	}

	public class RiwayatArsipBasic : RiwayatArsip
	{
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? tglCek { get; set; }
		public string note { get; set; }
	}

	public class doctrx : DetailBase
	{
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false /*true */)]
		public DateTime? waktu { get; set; }
		public SifatBerkas en_sifat { get; set; }
		public string sifat
		{
			get => en_sifat.ToString("g");
			set { if (Enum.TryParse<SifatBerkas>(value, out SifatBerkas sb)) en_sifat = sb; }
		}
		public JenisTrx en_jenis { get; set; }
		public string jenis
		{
			get => en_jenis.ToString("g");
			set { if (Enum.TryParse<JenisTrx>(value, out JenisTrx jt)) en_jenis = jt; }
		}
		public int? tenggat { get; set; }

		public static string ToString(doctrx trx)
			=> $"{trx.waktu}#{trx.jenis}#{trx.sifat}#{trx.tenggat}";

		public static doctrx FromString(string transaction)
		{
			if (transaction == null)
				return null;
			string[] parts = transaction.Split(new[] { "#" },StringSplitOptions.None).Select(s=>s.Trim().ToLower()).ToArray();
			if (!parts.Any())
				return null;
			var trx = new doctrx();
			trx.waktu = DateTime.TryParse(parts[0], out DateTime dt) ? (DateTime?)dt : null;
			trx.en_jenis = JenisTrx.unknown;
			trx.jenis = parts[1].ToLower();
			trx.en_sifat = SifatBerkas.unknown;
			trx.sifat = parts[2].ToLower();
			trx.tenggat = int.TryParse(parts[3], out int tg) ? (int?)tg: null;
			return trx;
		}
	}
}

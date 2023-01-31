using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{

	public class Product : DetailBase
	{
		[BundlePropMap("JDOK056", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(AJB))]
		[BundlePropMap("JDOK019", MetadataKey.Tanggal, Dynamic.ValueType.Date, nameProp = "PJB")]
		[BundlePropMap("JDOK021", MetadataKey.Tanggal, Dynamic.ValueType.Date, nameProp = "kuasa")]
		[BundlePropMap("JDOK020", MetadataKey.Tanggal, Dynamic.ValueType.Date, nameProp = "kesepakatan")]
		[BundlePropMap("JDOK024", MetadataKey.Tanggal, Dynamic.ValueType.Date, nameProp = "waris")]
		[BundlePropMap("JDOK047", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(SKBPN))]
		[BundlePropMap("JDOK022", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(SPH))]
		[BundlePropMap("JDOK036", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(SHM))]
		[BundlePropMap("JDOK038", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(SHP))]
		[BundlePropMap("JDOK037", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(HGB))]
		[BundlePropMap("JDOK054", MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(HGB_Final))]
		public DateTime? tanggal { get; set; }

		[BundlePropMap("JDOK056", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(AJB))]
		[BundlePropMap("JDOK019", MetadataKey.Nomor, Dynamic.ValueType.String, nameProp = "PJB")]
		[BundlePropMap("JDOK021", MetadataKey.Nomor, Dynamic.ValueType.String, nameProp = "kuasa")]
		[BundlePropMap("JDOK020", MetadataKey.Nomor, Dynamic.ValueType.String, nameProp = "kesepakatan")]
		[BundlePropMap("JDOK024", MetadataKey.Nomor, Dynamic.ValueType.String, nameProp = "waris")]
		[BundlePropMap("JDOK047", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(SKBPN))]
		[BundlePropMap("JDOK022", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(SPH))]
		[BundlePropMap("JDOK036", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(SHM))]
		[BundlePropMap("JDOK038", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(SHP))]
		[BundlePropMap("JDOK037", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(HGB))]
		[BundlePropMap("JDOK054", MetadataKey.Nomor, Dynamic.ValueType.String, typeClass = typeof(HGB_Final))]
		public string nomor { get; set; }
		public DateTime? tglInput { get; set; }
		public RiwayatArsip arsip { get; set; }
	}

	public class SPH : Product
	{
	}

	public class Akta : Product
	{

		[BundlePropMap("JDOK019", MetadataKey.Luas, Dynamic.ValueType.Number, nameProp = "PJB")]
		[BundlePropMap("JDOK021", MetadataKey.Luas, Dynamic.ValueType.Number, nameProp = "kuasa")]
		[BundlePropMap("JDOK020", MetadataKey.Luas, Dynamic.ValueType.Number, nameProp = "kesepakatan")]
		[BundlePropMap("JDOK024", MetadataKey.Luas, Dynamic.ValueType.Number, nameProp = "waris")]
		[BundlePropMap("JDOK047", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(SKBPN))]
		[BundlePropMap("JDOK032", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(NIB_Perorangan))]
		[BundlePropMap("JDOK050", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(NIB_PT))]
		[BundlePropMap("JDOK036", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(SHM))]
		[BundlePropMap("JDOK038", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(SHP))]
		[BundlePropMap("JDOK037", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(HGB))]
		[BundlePropMap("JDOK054", MetadataKey.Luas, Dynamic.ValueType.Number, typeClass = typeof(HGB_Final))]
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
		[BundlePropMap("JDOK032",  MetadataKey.Nomor_NIB, Dynamic.ValueType.String, typeClass = typeof(NIB_Perorangan))]
		[BundlePropMap("JDOK050",  MetadataKey.Nomor_NIB, Dynamic.ValueType.String, typeClass = typeof(NIB_PT))]
		public string noNIB { get; set; }
		// {
		// 	get => nomor;
		// 	set { nomor = value; }
		// }
		[BundlePropMap("JDOK032",  MetadataKey.Nomor_PBT, Dynamic.ValueType.String, typeClass = typeof(NIB_Perorangan))]
		[BundlePropMap("JDOK050",  MetadataKey.Nomor_PBT, Dynamic.ValueType.String, typeClass = typeof(NIB_PT))]
		public string noPBT { get; set; }
		[BundlePropMap("JDOK032",  MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(NIB_Perorangan))]
		[BundlePropMap("JDOK050",  MetadataKey.Tanggal, Dynamic.ValueType.Date, typeClass = typeof(NIB_PT))]
		public DateTime? tglSelesai { get; set; }
	}

	public class NIB_Perorangan : NIB
	{

	}

	public class NIB_PT : NIB
	{

	}

	public class AJB : Product
	{
	}

	public class SKBPN : Akta
	{
	}

	public class Sertifikat : Akta
	{
		// added 2020-04-16 05:39:00 -- for place of SHM-nama @ hibah
		[BundlePropMap("JDOK037", MetadataKey.Nama, Dynamic.ValueType.String, typeClass = typeof(HGB))]
		[BundlePropMap("JDOK054", MetadataKey.Nama, Dynamic.ValueType.String, typeClass = typeof(HGB_Final))]
		[BundlePropMap("JDOK036", MetadataKey.Nama, Dynamic.ValueType.String, typeClass = typeof(SHM))]
		[BundlePropMap("JDOK038", MetadataKey.Nama, Dynamic.ValueType.String, typeClass = typeof(SHP))]
		public string nama { get; set; }
	}

	public class HGB : Sertifikat
	{
		[BundlePropMap("JDOK037", MetadataKey.Due_Date, Dynamic.ValueType.Date, typeClass = typeof(HGB))]
		[BundlePropMap("JDOK054", MetadataKey.Due_Date, Dynamic.ValueType.Date, typeClass = typeof(HGB_Final))]
		public DateTime? expired { get; set; }
	}

	public class HGB_Final : HGB
	{ }

	[BsonIgnoreExtraElements]
	public class SHM:Sertifikat
	{	
		[BsonExtraElements]
		Dictionary<string,object> extraelems { get; set; }
	}
	public class SHP : Sertifikat
	{ }
}

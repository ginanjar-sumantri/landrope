using System;
using System.Collections.Generic;
using System.Text;
using auth.mod;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using landrope.mod2;
using landrope.common;
using System.Linq;
using landrope.documents;

namespace landrope.mod3
{

	[BsonKnownTypes(typeof(DocAlasHak), typeof(DocID), typeof(DocPBB))]
	[Entity("doc", "documents")]
	public class Doc : namedentity3
	{
		public JenisBerkas en_jenis { get; set; }
		public string keyDocType { get; set; }
		public string jenis
		{
			get => en_jenis.ToString("g");
			set { if (Enum.TryParse<JenisBerkas>(value, out JenisBerkas jb)) en_jenis = jb; }
		}
		public int? tahun { get; set; }
		public string note { get; set; }
		public string title { get; set; }
		public DocType docType() => DocType.List.FirstOrDefault(d => d.key == keyDocType);
		public string JenisDokumen() => docType()?.identifier;
	}

	public class AlasHak
	{
		public JenisAlasHak en_jenis { get; set; }
		public string jenis
		{
			get => en_jenis.ToString("g");
			set { if (Enum.TryParse<JenisAlasHak>(value, out JenisAlasHak ja)) en_jenis = ja; }
		}
		public string nomor { get; set; }
		public string nama { get; set; }
		public double? luas { get; set; }
		public DateTime? expired { get; set; }

	}

	[Entity("docAlasHak", "documents")]
	public class DocAlasHak : Doc
	{
		[BsonIgnore]
		public AlasHak current => history.FirstOrDefault();
		public List<AlasHak> history { get; set; }
	}

	[Entity("docID", "documents")]
	public class DocID : Doc
	{
		public string nomor
		{
			get => identifier;
			set => identifier = value;
		}
		public JenisKtp en_jenisid { get; set; }
		public string jenisid
		{
			get => en_jenisid.ToString("g");
			set { if (Enum.TryParse<JenisKtp>(value, out JenisKtp jns)) en_jenisid = jns; }
		}
	}

	[Entity("docPBB", "documents")]
	public class DocPBB : Doc
	{
		public string NOP
		{
			get => identifier;
			set => identifier = value;
		}
		public int nilai { get; set; }
		public PBBdetail[] details { get; set; }
		public void AddDetail(int year,bool paid)
		{
			if (details.Any(d => d.year == year))
				return;
			var dtl = new PBBdetail { year = year, paid = paid };
			details = details.Add(dtl);
		}

		public void DelDetail(int year)
		{
			var dtl = details.FirstOrDefault(d => d.year == year);
			if (dtl!=null)
				details = details.Del(dtl);
		}
	}

	public class PBBdetail
	{
		public int year { get; set; }
		public bool paid { get; set; }
	}

}

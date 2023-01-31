using Newtonsoft.Json;
using System;
using System.Linq;

namespace landrope.common
{
	public class DmsMeta
	{
		public string keyCreator { get; set; }
		public DateTime created { get; set; }
	}

	public class DmsMetaTemp
	{
		public string keyCreator { get; set; }
		public DateTime created { get; set; }
		public int part { get; set; }
		public int totalpart { get; set; }
	}

	public class Secured
	{
		public string extra { get; set; } = $"{new Random().Next(int.MaxValue)}";

		public string encrypt() =>
			encdec.encdec.encrypt2(this.extra + serial);

		public static T decrypt<T>(string mutant) where T : Secured
		{
			var st = encdec.encdec.decrypt2(mutant);
			var pos = st?.IndexOf('{', 0) ?? -1;
			if (pos <= 0)
				return null;
			st = st.Substring(pos);
			return System.Text.Json.JsonSerializer.Deserialize<T>(st);
		}

		protected virtual string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
	}

	public class SecuredDoc : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string encDocument { get; set; }

		/*		[JsonIgnore]
				[System.Text.Json.Serialization.JsonIgnore]*/
		public string GetDocument64() => encDocument;// encdec.encdec.decrypt(encDocument);
		public void SetDocument64(string value) { encDocument = value/*encdec.encdec.encrypt(value)*/; }

		public byte[] GetDocument() => Convert.FromBase64String(encDocument);// encdec.encdec.decrypt(encDocument);
		public void SetDocument(byte[] value) { encDocument = Convert.ToBase64String(value)/*encdec.encdec.encrypt(value)*/; }

		public ByteChunker GetChunker(int chunkSize)
			=> new ByteChunker(GetDocument(), chunkSize);
	}

	/*	public class SecuredDocB : Secured
		{
			protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
			public byte[] encDocument { get; set; }

			*//*		[JsonIgnore]
					[System.Text.Json.Serialization.JsonIgnore]*//*
			public byte[] GetDocument() => encDocument;// encdec.encdec.decrypt(encDocument);
			public void SetDocument(byte[] value) { encDocument = value*//*encdec.encdec.encrypt(value)*//*; }
		}
	*/
	public class DmsSave : SecuredDoc
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string keyBundle { get; set; }
		public string keyDocType { get; set; }
		public bool assign { get; set; }
		public bool praDeals { get; set; }
		public string keyAssign { get; set; }
		[JsonIgnore]
		public string docId => $"{keyBundle}{keyDocType}";
		[JsonIgnore]
		public string docIdAsgn => !praDeals ? $"A{keyAssign}-{keyBundle}{keyDocType}" : $"{keyAssign}-{keyBundle}{keyDocType}";
	}

	public class DmsLoad : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string keyBundle { get; set; }
		public string keyDocType { get; set; }
		[JsonIgnore]
		public string docId => $"{keyBundle}{keyDocType}";
	}

	public class DmsRename : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string keyBundle { get; set; }
		public string keyDocType { get; set; }
		public string keyAssign { get; set; }
		[JsonIgnore]
		public string docId => $"{keyBundle}{keyDocType}";
		[JsonIgnore]
		public string docIdAsgn => $"A{keyAssign}-{keyBundle}{keyDocType}";
	}

	public class DmsSavePro : SecuredDoc
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string prefix { get; set; }
		public string keyProses { get; set; }
		public string keyDetail { get; set; }
		public string keySubtype { get; set; }
		[JsonIgnore]
		public string docId => !string.IsNullOrEmpty(keyDetail) && !string.IsNullOrEmpty(keySubtype) ? $"{prefix}//{keyProses}-{keyDetail}{keySubtype}" : $"{prefix}//{keyProses}";
	}

	public class DmsLoadPro : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string prefix { get; set; }
		public string keyProses { get; set; }
		public string keyDetail { get; set; }
		public string keySubtype { get; set; }
		[JsonIgnore]
		public string docId => $"{prefix}//{keyProses}-{keyDetail}{keySubtype}";
	}

	public class DmsSaveAtt : SecuredDoc
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string prefix { get; set; }
		public string key { get; set; }
		[JsonIgnore]
		public string docId => $"{prefix}//{key}";
	}

	public class DmsLoadAtt : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string token { get; set; }
		public string prefix { get; set; }
		public string key { get; set; }
		[JsonIgnore]
		public string docId => $"{prefix}//{key}";
	}

	public class DmsLoadResult : SecuredDoc
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string name { get; set; }
		public string extension { get; set; }
		public BaseContent baseContent { get; set; }
		public string creator { get; set; }
		public DateTime created { get; set; }
	}

	public class DmsLoadAttPart : SecuredDoc
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public string creator { get; set; }
		public DateTime created { get; set; }
	}

	public class DmsLoadTemp
	{
		public string sbase64 { get; set; }
		public int partNo { get; set; }
		public int totalPartNo { get; set; }
	}

	public class DmsLoadAttResult : Secured
	{
		protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
		public DmsLoadAttPart[] docs { get; set; }
	}

    public class DmsSaveReq : SecuredDoc
    {
        protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
        public string token { get; set; }
        public string key { get; set; }
    }

    public class DmsLoadReq : Secured
    {
        protected override string serial => Newtonsoft.Json.JsonConvert.SerializeObject(this);
        public string token { get; set; }
        public string key { get; set; }
    }

    public class DmsExtScan : Secured
	{
	}

	public class DmsExtScanResult : SecuredDoc
	{
	}

	public class DmsExtPrint : SecuredDoc
	{

	}

	public class ByteChunker
	{
		byte[] data;
		int chunkSize;
		int pos = -1;

		public int TotalLength => data.Length;
		public ByteChunker(byte[] data, int size)
		{
			this.data = data;
			this.chunkSize = size;
			Reset();
		}
		public ByteChunker(string data64, int size)
		{
			this.data = Convert.FromBase64String(data64);
			this.chunkSize = size;
			Reset();
		}

		public void Reset()
		{
			pos = data.Length > 0 && chunkSize > 0 ? 0 : -1;
		}

		public byte[] Get()
		{
			var bpos = pos * chunkSize;
			if (bpos < 0 || bpos >= data.Length)
				return null;
			pos++;
			return data.Skip(bpos).Take(chunkSize).ToArray();
		}
	}
}
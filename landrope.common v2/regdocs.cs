//using landrope.mcommon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.common
{
	using InnerDocCoreList = List<ParticleDocCore>;

	public class docEntityCore
	{
		public Dictionary<MetadataKey, object> metadatas { get; set; } = new Dictionary<MetadataKey, object>();
	}

	public class ParticleDocCore : IMultiply
	{
		public string key { get; set; }
		public Dictionary<string, object> props { get; set; } = new Dictionary<string, object>();
		public Dictionary<string, bool> reqs { get; set; } = new Dictionary<string, bool>();
		public Existency[] exists { get; set; } = new Existency[0];

		public bool CheckDummy() => !IsFullfilled();

		public bool IsFullfilled() =>
			reqs.ToList().Where(r => r.Value).Join(props.ToList(), r => r.Key, p => p.Key, (r, p) => p)
			.All(p => p.Value != null);

		[JsonProperty("Asli")]
		[System.Text.Json.Serialization.JsonPropertyName("Asli")]
		public bool Asli
		{
			get => (exists.FirstOrDefault(x => x.ex == Existence.Asli)?.cnt ?? 0) > 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Asli, new Existency(Existence.Asli, value ? 1 : 0)); }
		}

		[JsonProperty("Salinan")]
		[System.Text.Json.Serialization.JsonPropertyName("Salinan")]
		public int Salinan
		{
			get => exists.FirstOrDefault(x => x.ex == Existence.Salinan)?.cnt ?? 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Salinan, new Existency(Existence.Salinan, value)); }
		}

		[JsonProperty("Copy")]
		[System.Text.Json.Serialization.JsonPropertyName("Copy")]
		public int Copy
		{
			get => exists.FirstOrDefault(x => x.ex == Existence.Copy)?.cnt ?? 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Copy, new Existency { ex = Existence.Copy, cnt = value }); }
		}

		[JsonProperty("Legalisir")]
		[System.Text.Json.Serialization.JsonPropertyName("Legalisir")]
		public int Legalisir
		{
			get => exists.FirstOrDefault(x => x.ex == Existence.Legalisir)?.cnt ?? 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Legalisir, new Existency { ex = Existence.Legalisir, cnt = value }); }
		}

		[JsonProperty("Soft_Copy")]
		[System.Text.Json.Serialization.JsonPropertyName("Soft_Copy")]
		public bool Soft_Copy
		{
			get => (exists.FirstOrDefault(x => x.ex == Existence.Soft_Copy)?.cnt ?? 0) > 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Soft_Copy, new Existency { ex = Existence.Soft_Copy, cnt = value ? 1 : 0 }); }
		}


		[JsonProperty("Avoid")]
		[System.Text.Json.Serialization.JsonPropertyName("Avoid")]
		public bool Avoid
		{
			get => (exists.FirstOrDefault(x => x.ex == Existence.Avoid)?.cnt ?? 0) > 0;
			set { exists = exists.AddOrReplace(x => x.ex == Existence.Avoid, new Existency { ex = Existence.Avoid, cnt = value ? 1 : 0 }); }
		}
	}

	//public class InnerDocCoreList : List<InnerDocCore>
	//{
	//	public InnerDocCoreList()
	//		: base()
	//	{ }

	//	public InnerDocCoreList(IEnumerable<InnerDocCore> src)
	//		: base(src)
	//	{
	//	}
	//}

	public class RegisteredDocCore : IMultiChild
	{
		public string keyDocType { get; set; }
		public string docType { get; set; }
		public InnerDocCoreList docs { get; set; }

		public ParticleDocCore AddDoc()
		{
			var sample = docs.First();

			var nkey = $"TMP_{DateTime.Now.Ticks}";
			var doc = new ParticleDocCore();
			doc.key = nkey;

			doc.reqs = sample.reqs.ToList().ToDictionary(l => l.Key, l => l.Value);
			doc.props = new Dictionary<string, object>();
			foreach (var key in sample.props.Keys)
				doc.props.Add(key, null);
			//doc.existas = new Existency[sample.existas.Length];
			//for (int i=0;i<sample.existas.Length;i++)
			//	doc.existas[i] = new Existency { ex=sample.existas[i].ex, cnt=0};

			docs.Add(doc);
			return doc;
		}

		public bool CanAdd(string collname) => docs.All(d => d.IsFullfilled());
		public object Add(string collname) => AddDoc();

	}

	public class RegisteredDocView
	{
		public string keyDocType { get; set; }
		public string docType { get; set; }
		public int count { get; set; }
		public string properties { get; set; }// first document properties
		public bool Asli { get; set; } = false;
		public int Copy { get; set; } = 0;
		public int Legalisir { get; set; } = 0;
		public int Salinan { get; set; } = 0;
		public bool Soft_Copy { get; set; } = false;
		public bool isScanAllow { get; set; }
		public bool Avoid { get; set; } = false;

		public string DmsId { get; set; }
		public RegisteredDocView SetExistence(Dictionary<Existence, int> dict)
		{
			Asli = (dict.TryGetValue(Existence.Asli, out int asli) ? asli : 0) > 0;
			Copy = dict.TryGetValue(Existence.Copy, out int copy) ? copy : 0;
			Legalisir = dict.TryGetValue(Existence.Legalisir, out int legal) ? legal : 0;
			Salinan = dict.TryGetValue(Existence.Salinan, out int salin) ? salin : 0;
			Soft_Copy = (dict.TryGetValue(Existence.Soft_Copy, out int soft) ? soft : 0) > 0;
			Avoid = (dict.TryGetValue(Existence.Avoid, out int avoid) ? avoid : 0) > 0;
			return this;
		}

		public RegisteredDocView SetProperties(Dictionary<string, Dynamic> props)
		{
			this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key}</b>:{p.Value.Value}"));
			return this;
		}

		public RegisteredDocView SetProperties(Dictionary<MetadataKey, Dynamic> props)
		{
			this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key.ToString("g").Replace("_", " ")}</b>:{p.Value.Value}"));
			return this;
		}

		public RegisteredDocView SetProperties2(KeyValuePair<MetadataKey, Dynamic>[] props)
		{
			this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key.ToString("g").Replace("_", " ")}</b>:{p.Value.Value}"));
			return this;
		}
	}

	public class MetadataReq
	{
		public MetadataKey key { get; set; }
		public bool? primary { get; set; }
		public bool req { get; set; }
	}

	public static class MetadataType
	{
		public static Dictionary<MetadataKey, Type> types = new Dictionary<MetadataKey, Type>
		{
			{ MetadataKey.Tahun, typeof(int?) },
			{ MetadataKey.Nomor, typeof(string) },
			{ MetadataKey.Nilai, typeof(decimal?) },
			{ MetadataKey.Nama, typeof(string) },
			{ MetadataKey.Lunas, typeof(bool?) },
			{ MetadataKey.Luas, typeof(decimal?) },
			{ MetadataKey.Lainnya, typeof(string) },
			{ MetadataKey.Jenis, typeof(string) },
			{ MetadataKey.Due_Date, typeof(DateTime?) },
			{ MetadataKey.NIK , typeof(string) },
			{ MetadataKey.Nomor_KK , typeof(string) },
			{ MetadataKey.Tanggal_Bayar, typeof(DateTime?) },
			{ MetadataKey.Tanggal_Validasi, typeof(DateTime?) },
			{ MetadataKey.Tanggal , typeof(DateTime?) },
			{ MetadataKey.Nama_Lama, typeof(string) },
			{ MetadataKey.Nama_Baru, typeof(string) },
			{ MetadataKey.NOP, typeof(string) },
			{ MetadataKey.Nomor_NIB, typeof(string) },
			{ MetadataKey.Nomor_PBT, typeof(string) },
			{ MetadataKey.NTPN, typeof(string) },
			{ MetadataKey.Nama_Notaris, typeof(string) },
		};
	}

	public class ReportDocPraDeals : RegisteredDocView
	{
		public string IdBidang { get; set; }
		public string Project { get; set; }
		public string Desa { get; set; }
		public string AlasHak { get; set; }
		public string Pemilik { get; set; }
        public string Group { get; set; }
        public double? LuasSurat { get; set; }

        public ReportDocPraDeals SetExistence(Dictionary<Existence, int> dict)
        {
            Asli = (dict.TryGetValue(Existence.Asli, out int asli) ? asli : 0) > 0;
            Copy = dict.TryGetValue(Existence.Copy, out int copy) ? copy : 0;
            Legalisir = dict.TryGetValue(Existence.Legalisir, out int legal) ? legal : 0;
            Salinan = dict.TryGetValue(Existence.Salinan, out int salin) ? salin : 0;
            Soft_Copy = (dict.TryGetValue(Existence.Soft_Copy, out int soft) ? soft : 0) > 0;
            Avoid = (dict.TryGetValue(Existence.Avoid, out int avoid) ? avoid : 0) > 0;
            return this;
        }

        public ReportDocPraDeals SetProperties(Dictionary<string, Dynamic> props)
        {
            this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key}</b>:{p.Value.Value}"));
            return this;
        }

        public ReportDocPraDeals SetProperties(Dictionary<MetadataKey, Dynamic> props)
        {
            this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key.ToString("g").Replace("_", " ")}</b>:{p.Value.Value}"));
            return this;
        }

        public ReportDocPraDeals SetProperties2(KeyValuePair<MetadataKey, Dynamic>[] props)
        {
            this.properties = string.Join("&nbsp;&nbsp;", props.Select(p => $"<b>{p.Key.ToString("g").Replace("_", " ")}</b>:{p.Value.Value}"));
            return this;
        }
    }
}

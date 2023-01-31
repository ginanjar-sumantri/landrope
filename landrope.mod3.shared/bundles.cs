using DynForm.shared;
using landrope.common;
using landrope.mod.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace landrope.mod3.shared
{
	public class BundleCore
	{
		public string key { get; set; }
		public bool? invalid { get; set; }
		public string IdBidang { get; set; }
		public string noPeta { get; set; }
	}

	public class MainBundleCore : BundleCore
	{
		//public string keyPersil { get; set; }
		public string storkey { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
	}

	public class TaskBundleCore : BundleCore
	{
		public AssignmentCore assignment { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string PTSK { get; set; }
		public string penampung { get; set; }
	}

	public class DealBundleCore : BundleCore
    {
		public PraPembebasanCore praBebas { get; set; }
        public string praDealKey { get; set; }
        public string NoRequest { get; set; }
        public string Manager { get; set; }
        public string Sales { get; set; }
        public string Mediator { get; set; }
    }

	public class BundleView
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
		public string noPeta { get; set; }
	}

	public class MainBundleView : BundleView
	{
		public string project { get; set; }
		public string desa { get; set; }
		public string storkey { get; set; }
		public bool? invalid { get; set; }
		public string noSurat { get; set; }

		public MainBundleView SetNoSurat(string nomor)
		{
			noSurat = nomor;
			return this;
		}
	}

	public class TaskBundleView : BundleView
	{
		public string noAssignment { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string PTSK { get; set; }
		public string penampung { get; set; }
		public DocProcessStep? step { get; set; }

		public string IdBundle => $"{noAssignment}/{IdBidang}";
	}

	public class DealBundleView : BundleView
    {
		public string praDealKey { get; set; }
		public string NoRequest { get; set; }
		public string Manager { get; set; }
		public string Sales { get; set; }
		public string Mediator { get; set; }
	}

	public class BundleFact : ICoreDetail
	{
		public string keyDocType { get; set; }
		[GridColumn(Caption ="Jenis Dokumen", Width =100)]
		public string doctype { get; set; }
		//public string chainkey { get; set; }
		public Dictionary<string,string> props { get; set; }
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Informasi", Width = 200)]
		public string properties => string.Join(", ", props.Select(p=>$"{p.Key.Replace("_"," ")}:{p.Value}"));
		public Existency[] exis { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Asli", Width = 80)]
		public bool Asli => exis.Any(x=>x.ex==Existence.Asli && x.cnt>0);

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Copy", Width = 80)]
		public int Copy => exis.FirstOrDefault(x=>x.ex==Existence.Copy)?.cnt??0;

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Salinan", Width = 80)]
		public int Salinan => exis.FirstOrDefault(x => x.ex == Existence.Salinan)?.cnt ?? 0;

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Legalisir", Width = 80)]
		public int Legalisir => exis.FirstOrDefault(x => x.ex == Existence.Legalisir)?.cnt ?? 0;

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		[GridColumn(Caption = "Discan", Width = 80)]
		public bool Soft_Copy => exis.Any(x => x.ex == Existence.Soft_Copy && x.cnt > 0);

		public ICore GetCore()
		{
			return this;
		}
	}
	public class DocRequirementView
	{
		public string keyDocType { get; set; }
		public string docType { get; set; }
		public Existence ex { get; set; }
		public bool req { get; set; }
	}

	public class DocSeriesView
	{
		public string keyDocType { get; set; }
		public string docType { get; set; }
		public class MyInnerDoc
		{
			public string properties { get; set; }// first document properties
			public bool Asli { get; set; } = false;
			public int Copy { get; set; } = 0;
			public int Legalisir { get; set; } = 0;
			public int Salinan { get; set; } = 0;
			public bool Soft_Copy { get; set; } = false;
			public bool Avoid { get; set; } = false;
			public void SetExistence(Dictionary<Existence, int> dict)
			{
				Asli = (dict.TryGetValue(Existence.Asli, out int asli) ? asli : 0) > 0;
				Copy = dict.TryGetValue(Existence.Copy, out int copy) ? copy : 0;
				Legalisir = dict.TryGetValue(Existence.Legalisir, out int legal) ? legal : 0;
				Salinan = dict.TryGetValue(Existence.Salinan, out int salin) ? salin : 0;
				Soft_Copy = (dict.TryGetValue(Existence.Soft_Copy, out int soft) ? soft : 0) > 0;
				Avoid = (dict.TryGetValue(Existence.Avoid, out int avoid) ? avoid : 0) > 0;
			}
		}
		public MyInnerDoc[] docs { get; set; }

	}

}

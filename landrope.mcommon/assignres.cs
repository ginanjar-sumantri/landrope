using DynForm.shared;
using flow.common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace landrope.mcommon
{
	public class PreResultDoc
	{
		public string keyDT { get; set; }
		public ResultMeta prop { get; set; }
	}

	public class ResultDoc
	{
		public string keyDT { get; set; }
		public Existency[] exs { get; set; }
		public ResultMetaD[] props { get; set; }
		public DateTime? scanned { get; set; }
		public string scannedBy { get; set; }

		public static implicit operator ResultDoc(PreResultDoc doc)
		=> new ResultDoc
		{
			keyDT = doc.keyDT,
			exs = new Existency[] { new Existency { ex = Existence.Asli, cnt = 1 } },
			props = new ResultMetaD[] { doc.prop }
		};
	}

	public class ResultMeta
	{
		public MetadataKey mkey { get; set; }
		public string val { get; set; }

	}

	public class ResultMetaD
	{
		public MetadataKey mkey { get; set; }
		public Dynamic val { get; set; }
		public static implicit operator ResultMetaD(ResultMeta meta)
			=> new ResultMetaD { mkey = meta.mkey, val = new Dynamic(Dynamic.ValueType.String, meta.val) };
		public KeyValuePair<string, Dynamic> AsKV() => new KeyValuePair<string, Dynamic>(mkey.ToString("g"), val);
	}

	public class AssignQry
	{
		public int pg { get; set; } = 0;
		public int rpp { get; set; } = 0;
		public QueryMode incl { get; set; } = QueryMode.Nothing;
		public QueryMode excl { get; set; } = QueryMode.Nothing;
		public SortMode sort { get; set; } = SortMode.Nothing;
	}

	public class AssignCommand
	{
		public string akey { get; set; }
		public string rkey { get; set; }
		public ToDoControl control { get; set; }
	}

	public class AssignDtlCommand : AssignCommand
	{
		public string dkey { get; set; }
	}

	public class AssignDelegate : AssignCommand
	{
		public string keyUser { get; set; }
	}

	public class SubmitDoc : AssignDtlCommand
	{
		public PreResultDoc info { get; set; }
	}

	public class ResultEntry
	{
		public string keyDT { get; set; }
		public string mkey { get; set; }
		public string val { get; set; }

		[JsonIgnore]
		public Dictionary<string, option[]> extras { get; set; } = new Dictionary<string, option[]>();
	}

	public class ResultDocCore
	{
		public string keyDocType { get; set; }
		public string docType { get; set; }
		public Dictionary<string, object> props { get; set; } = new Dictionary<string, object>();
		public int[] exis { get; set; } = new int[6];
		public bool Avoid { get => exis[(int)Existence.Avoid] > 0; set { exis[(int)Existence.Avoid] = value ? 1 : 0; } }
		public bool Asli { get => exis[(int)Existence.Asli] > 0; set { exis[(int)Existence.Asli] = value ? 1 : 0; } }
		public bool Soft_Copy { get => exis[(int)Existence.Soft_Copy] > 0; set { exis[(int)Existence.Soft_Copy] = value ? 1 : 0; } }
		public int Copy { get => exis[(int)Existence.Copy]; set { exis[(int)Existence.Copy] = value; } }
		public int Salinan { get => exis[(int)Existence.Salinan]; set { exis[(int)Existence.Salinan] = value; } }
		public int Legalisir { get => exis[(int)Existence.Legalisir]; set { exis[(int)Existence.Legalisir] = value; } }
	}

	public class Existency
	{
		public Existence ex { get; set; }
		public int cnt { get; set; }

		public Existency()
		{

		}

		public Existency(Existence ex, int cnt)
		{
			this.ex = ex;
			this.cnt = cnt;
		}
	}

}

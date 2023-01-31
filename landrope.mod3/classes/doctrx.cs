using GenWorkflow;
using landrope.common;
using landrope.documents;
using landrope.engines;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod3.classes
{
	[Entity("trxBatch","transactions")]
	[BsonKnownTypes(typeof(entryBatch), typeof(lendBatch), typeof(returnBatch))]
	public class trxBatch : namedentity3, ITrxBatch
	{
		GraphConsumer.GraphHostConsumer graph;
		public string keyCreator { get; set; }
		public DateTime created { get; set; }
		public string instkey { get; set; }
		public DocPartTrx[] details { get; set; } 

		public virtual trxBatch Inject(GraphConsumer.GraphHostConsumer graph)
		{
			this.graph=graph;
			return this;
		}
		[BsonIgnore]
		GraphMainInstance _instance = null;

		[BsonIgnore]
		public GraphMainInstance Instance
		{
			get
			{
				if (_instance == null) _instance = graph?.Get(instkey).GetAwaiter().GetResult();
				return _instance;
			}
		}

		[BsonIgnore]
		public TrxType type => GetType().Name switch
		{
			nameof(lendBatch) => TrxType.Peminjaman,
			nameof(returnBatch) => TrxType.Pengembalian,
			_ => TrxType.Masuk
		};
	}

	[Entity("entryBatch","transactions")]
	public class entryBatch : trxBatch 
	{
		public override trxBatch Inject(GraphConsumer.GraphHostConsumer graph)
		{
			return base.Inject(graph);
		}
	}

	[Entity("lendBatch", "transactions")]
	public class lendBatch : trxBatch 
	{
		public int duration { get; set; }
		public override trxBatch Inject(GraphConsumer.GraphHostConsumer graph)
		{
			return base.Inject(graph);
		}
	}

	[Entity("returnBatch", "transactions")]
	public class returnBatch : trxBatch
	{
		public string keyReff { get; set; }
		public override trxBatch Inject(GraphConsumer.GraphHostConsumer graph)
		{
			return base.Inject(graph);
		}
	}

/*	public class DocPartTrx : DocPart
	{
		public int[] exis { get; set; }

		[BsonIgnore]
		public Existency[] exis2
		{
			get => exis.ConvertBack2();
			set { exis = value.Convert(); }
		}

		public DocPartTrx(string key, string keyDocType, string chainkey,int[] exis)
			:base(key,keyDocType,chainkey)
		{
			this.exis = exis;
		}

		public DocPartTrx(string key, string keyDocType, string chainkey, Existency[] exis)
			: base(key, keyDocType, chainkey)
		{
			this.exis = exis.Convert();
		}
	}*/
}

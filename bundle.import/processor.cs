using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using auth.mod;
using ExcelDataReader;
using landrope.common;
using landrope.documents;
using landrope.mod2;
using landrope.mod3;
using MongoDB.Driver;
using mongospace;
using Tracer;

namespace bundle.import
{
	public class Processor
	{
		static string[] ends = { ".a", ".c", ".s", ".l", ".#" };
		public LandropePlusContext context { get; set; }
		public string userkey { get; set; }

		List<DocType> doctypes;
		public Processor(LandropePlusContext context, string userkey)
		{
			this.context = context;
			this.userkey = userkey;

			doctypes = context.GetCollections(new DocType(), "jnsDok", "{invalid:{$ne:true}}", "{_id:0}").ToList();
		}

		public void Process(string filepath, string sheetnames, ChangeKind kind = ChangeKind.Update)
		{
			var filename = Path.GetFileName(filepath);
			var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
			var xreader = ExcelReaderFactory.CreateReader(strm);
			var updDict = new Dictionary<string, (int[] exs, Dictionary<string, Dynamic> props)>();
			try
			{
				var res = xreader.AsDataSet();
				var tables = xreader.AsDataSet().Tables.Cast<DataTable>().ToList();
				if (sheetnames != "*")
				{
					var names = sheetnames.Split(',');
					var notnames = names.Where(n => n[0] == '!').Select(n => n.Substring(1)).ToArray();
					names = names.Except(notnames).ToArray();
					tables = tables.Where(t => names.Contains(t.TableName) || !notnames.Contains(t.TableName)).ToList();
				}
				try
				{
					foreach (var dt in tables)
					{
						var anymodif = false;
						Console.WriteLine($"Processing worksheet '{dt.TableName}'...");
						var cols = dt.Rows[0].ItemArray.Select((d, i) => (d, i)).Where(x => x.d != null)
										.Select(x => (tag: x.d.ToString(), x.i)).Where(x => x.tag.StartsWith("#")).ToArray();

						var exisdict = new Dictionary<int, (string keyDocType, Existence ex)>();
						var propdict = new Dictionary<int, (string keyDocType, MetadataKey mk, Type type)>();

						var exiscol = cols.Where(x => ends.Contains(x.tag.Substring(x.tag.Length - 2))).ToList();
						foreach (var v in exiscol)//=>
						{
							var parts = v.tag.Substring(1).Split(".");
							var keyDocType = parts[0];
							var dtype = doctypes.FirstOrDefault(d => d.key == keyDocType);
							if (dtype == null)
								throw new InvalidExpressionException($"Document type not known ({keyDocType}) ");
							var ex = parts[1].ToUpper() switch
							{
								"A" => Existence.Asli,
								"C" => Existence.Copy,
								"S" => Existence.Salinan,
								"L" => Existence.Legalisir,
								_ => Existence.Avoid
							};
							exisdict.Add(v.i, (keyDocType, ex));
						}//);

						var propcol = cols.Where(x => x.tag.Contains(".p.")).ToList();
						foreach (var v in propcol)//=>
						{
							var parts = v.tag.Substring(1).Split(".");
							var keyDocType = parts[0];

							var stmetakey = parts[2];
							var metakey = int.TryParse(stmetakey, out int i) ? i : 0;
							if (!Enum.IsDefined(typeof(MetadataKey), metakey))
								throw new InvalidExpressionException($"{stmetakey} is not unknown metadata key ordinal value");
							var meta = (MetadataKey)metakey;
							var dtype = doctypes.FirstOrDefault(d => d.key == keyDocType);
							if (dtype == null || !dtype.metadata.Any(m => m.key == meta))
								throw new InvalidExpressionException($"Neither document type not known ({keyDocType}) nor {stmetakey} is exists in the document type's metadata");
							var dyntype = MetadataType.types.TryGetValue(meta, out Type typ) ? typ : typeof(string);
							if (dyntype.Name.StartsWith("Nullable"))
								dyntype = dyntype.GenericTypeArguments[0];
							propdict.Add(v.i, (keyDocType, meta, dyntype));
						}

						var keycol = cols.FirstOrDefault(c => c.tag == "#key");
						var idbcol = cols.FirstOrDefault(c => c.tag == "#IdBidang");
						if (keycol.tag == null && idbcol.tag == null)
						{
							Console.WriteLine($"It has no key column nor IdBidang column defined. Skipped.");
							break;
						}

						var rows = dt.Rows.Cast<DataRow>().Select((r, i) => (r, i)).ToArray();
						rows = rows.Where(d => d.r.ItemArray[0] != null && d.r.ItemArray[0].ToString() == "1").ToArray();
						foreach (var row in rows)
						{
							Console.WriteLine($"Proessing Row #{row.i + 1}...");
							var okey = row.r.ItemArray[keycol.i];
							var idbid = row.r.ItemArray[idbcol.i];
							if (okey == null && idbid == null)
							{
								Console.WriteLine($"The key value and IdBidang value are empty. Skipped.");
								continue;
							}
							MainBundle bundle;
							if (okey != null)
							{
								var key = okey.ToString();
								bundle = context.mainBundles.FirstOrDefault(b => b.key == key);
							}
							else
							{
								var IdBidang = idbid.ToString();
								bundle = context.mainBundles.FirstOrDefault(b => b.IdBidang == IdBidang);
							}
							if (bundle == null)
							{
								Console.WriteLine($"Invalid key value or IdBidang value. No Bundle was exists for {dt.TableName}!ROW:{row.i + 1}. Skipped.");
								continue;
							}

							updDict.Clear();

							foreach (var k in exisdict.Keys)
							{
								var (keyDocType, ex) = exisdict[k];
								var xp = GetUpd(keyDocType);
											
								var obj = row.r.ItemArray[k];
								var hasvalue = obj != null && obj.ToString() != "";
								if (!hasvalue)
									continue;
								int value = obj switch
								{
									double d => (int)d,
									int i => i,
									bool b => b ? 1 : 0,
									_ => 0
								};
								xp.exs[(int)ex] = value;
							}

							foreach (var k in propdict.Keys)
							{
								(string keyDocType, MetadataKey mk, Type type) = propdict[k];
								var xp = GetUpd(keyDocType);

								var obj = row.r.ItemArray[k];
								var hasvalue = obj != null && obj.ToString() != "";
								if (!hasvalue)
									continue;

								var str = obj.ToString();
								(bool OK, Dynamic.ValueType dtype) = type.Name switch
								{
									nameof(Int32) => (int.TryParse(str, out int i), Dynamic.ValueType.Int),
									nameof(Int16) => (short.TryParse(str, out short i), Dynamic.ValueType.Int),
									nameof(DateTime) => (DateTime.TryParse(str, out DateTime d), Dynamic.ValueType.Date),
									nameof(Double) => (double.TryParse(str, out double d), Dynamic.ValueType.Number),
									nameof(Decimal) => (decimal.TryParse(str, out decimal d), Dynamic.ValueType.Number),
									nameof(Single) => (float.TryParse(str, out float d), Dynamic.ValueType.Number),
									nameof(Boolean) => (bool.TryParse(str, out bool d), Dynamic.ValueType.Bool),
									_ => (true, Dynamic.ValueType.String)
								};
								xp.props.Add(mk.ToString("g"), new Dynamic(type: dtype, val: str));
							}

							var kvs = updDict.Where(d=>d.Value.exs!=null && d.Value.exs.Any(v => v > 0) || 
													(d.Value.props!=null && d.Value.props.Keys.Any()));

							var modif = kvs.Any();
							foreach (var kv in kvs)
								AddFact(bundle, filename, kv.Key, kv.Value.exs, kv.Value.props, kind);
							if (modif)
								context.mainBundles.Update(bundle);
							anymodif = anymodif || modif;
						}
						Console.Write("Sheet processing complete...");
						if (anymodif)
						{
							Console.Write(" Saving...");
							context.SaveChanges();
							Console.WriteLine("SAVED");
						}
						else
							Console.WriteLine(" nothing imported");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.AllMessages()}");
					context.DiscardChanges();
				}
			}
			finally
			{
				xreader.Close();
				strm.Close();
			}

			(int[] exs, Dictionary<string, Dynamic> props) GetUpd(string keyDocType)
			{
				if (!updDict.ContainsKey(keyDocType))
					updDict.Add(keyDocType, (new int[6], new Dictionary<string, Dynamic>()));
				return updDict[keyDocType];
			}
		}

		public void Unprocess(string filepath, string sheetnames)
		{
			var filename = Path.GetFileName(filepath);
			var anyupdate = false;
			var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
			var xreader = ExcelReaderFactory.CreateReader(strm);
			try
			{
				var res = xreader.AsDataSet();
				var tables = xreader.AsDataSet().Tables.Cast<DataTable>().ToList();
				try
				{
					if (sheetnames != "*")
					{
						var names = sheetnames.Split(',');
						var notnames = names.Where(n => n[0] == '!').Select(n => n.Substring(1)).ToArray();
						names = names.Except(notnames).ToArray();
						tables = tables.Where(t => names.Contains(t.TableName) || !notnames.Contains(t.TableName)).ToList();
					}
					foreach (var dt in tables)
					{
						Console.WriteLine($"Processing worksheet '{dt.TableName}'...");
						var cols = dt.Rows[0].ItemArray.Select((d, i) => (d, i)).Where(x => x.d != null)
										.Select(x => (tag: x.d.ToString(), x.i)).Where(x => x.tag.StartsWith("#")).ToArray();

						var keycol = cols.FirstOrDefault(c => c.tag == "#key");
						var idbcol = cols.FirstOrDefault(c => c.tag == "#IdBidang");
						if (keycol.tag == null && idbcol.tag == null)
						{
							Console.WriteLine($"It has no key column nor IdBidang column defined. Skipped.");
							break;
						}
						var rows = dt.Rows.Cast<DataRow>().Select((r, i) => (r, i)).ToArray();
						rows = rows.Where(d => d.r.ItemArray[0] != null && d.r.ItemArray[0].ToString() == "1").ToArray();
						foreach (var row in rows)
						{
							Console.WriteLine($"Unproessing based on Row #{row.i + 1}...");
							var okey = row.r.ItemArray[keycol.i];
							var idbid = row.r.ItemArray[idbcol.i];
							if (okey == null && idbid == null)
							{
								Console.WriteLine($"The key value and IdBidang value are empty. Skipped.");
								continue;
							}
							MainBundle bundle;
							if (okey != null)
							{
								var key = okey.ToString();
								bundle = context.mainBundles.FirstOrDefault(b => b.key == key);
							}
							else
							{
								var IdBidang = idbid.ToString();
								bundle = context.mainBundles.FirstOrDefault(b => b.IdBidang == IdBidang);
							}
							if (bundle == null)
							{
								Console.WriteLine($"Invalid key value or IdBidang value. No Bundle was exists for {dt.TableName}!ROW:{row.i + 1}. Skipped.");
								continue;
							}

							var any = DelProp(bundle, filename);
							if (any)
								context.mainBundles.Update(bundle);
							anyupdate = anyupdate || any;
						}
						Console.Write("Sheet unprocessing complete...");
						if (anyupdate)
						{
							Console.Write(" Saving...");
							context.SaveChanges();
							Console.WriteLine("SAVED");
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.AllMessages()}");
					context.DiscardChanges();
				}
			}
			finally
			{
				xreader.Close();
				strm.Close();
			}
		}

		void AddExis(MainBundle bundle, string source, string key, Existency[] exists, ChangeKind kind)
		{
			var regdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == key);
			if (regdoc == null)
			{
				regdoc = new BundledDoc(key);
				bundle.doclist.Add(regdoc);
			}
			var inndoc = new ParticleDoc { exists = exists };
			var innlist = new ParticleDocChain();
			innlist.Add(MongoEntity.MakeKey, inndoc);
			regdoc.AddExistence(userkey, innlist, source, ChangeKind.Update);
		}

		void AddProp(MainBundle bundle, string source, string key, Dictionary<string, Dynamic> props, ChangeKind kind)
		{
			var regdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == key);
			if (regdoc == null)
			{
				regdoc = new BundledDoc(key);
				bundle.doclist.Add(regdoc);
			}
			var inndoc = new ParticleDoc() { props = props };
			var innlist = new ParticleDocChain();
			innlist.Add(MongoEntity.MakeKey, inndoc);
			regdoc.AddExistence(userkey, innlist, source, ChangeKind.Update);
		}

		void AddFact(MainBundle bundle, string source, string key, int[] exis, Dictionary<string, Dynamic> props, ChangeKind kind)
		{
			var regdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == key);
			if (regdoc == null)
			{
				regdoc = new BundledDoc(key);
				bundle.doclist.Add(regdoc);
			}
			var exs = new List<Existency>();
			for(int i=0;i<6;i++)
			{
				if (exis[i] > 0)
					exs.Add(new Existency((Existence)i, exis[i]));
			}
			var inndoc = new ParticleDoc() { props = props, exists=exs.ToArray() };
			var innlist = new ParticleDocChain();
			innlist.Add(MongoEntity.MakeKey, inndoc);
			regdoc.AddExistence(userkey, innlist, source, ChangeKind.Update);
		}

		//hibah outstanding oktober.xlsx
		bool DelProp(MainBundle bundle, string source, string key)
		{
			if (key == null)
				return DelProp(bundle, source);
			var regdoc = bundle.doclist.FirstOrDefault(d => d.keyDocType == key);
			var any = regdoc != null;
			if (any)
				any = regdoc.DelExistence(source) > 0;
			return any;
		}
		bool DelProp(MainBundle bundle, string source)
		{
			var cnt = 0;
			foreach (var regdoc in bundle.doclist)
				cnt += regdoc.DelExistence(source);
			Console.WriteLine($"Has removed {cnt} entries");
			return cnt>0;
		}

	}
}

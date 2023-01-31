using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ExcelDataReader;
using MongoDB.Driver;
using MongoDB.Bson;
using flow.common;
using landrope.common;
using auth.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.hosts;
using GraphHost;
using BundlerConsumer;

namespace landrope.api3.Controllers
{
	[Route("api/import")]
	[ApiController]
	[EnableCors(nameof(landrope))]

	static class Helpers
	{
		public static string ToJson(this string st) =>
			st.Replace("<", "{").Replace(">", "}");

		public static bool IsNullOrZero(this double? d) => d == null || d == 0;
		public static bool IsNullOrZero(this float? d) => d == null || d == 0;
		public static bool IsNullOrZero(this decimal? d) => d == null || d == 0;
		public static bool IsNullOrZero(this int? d) => d == null || d == 0;
		public static bool IsNullOrZero(this long? d) => d == null || d == 0;
		public static bool IsNullOrZero(this Int16? d) => d == null || d == 0;
	}

	public class ImportController : Controller
	{
		LandropePayContext context;
		LandropePlusContext contextplus;
		BayarHost bhost;
		GraphHostSvc ghost;

		public ImportController(LandropePayContext context, LandropePlusContext contextplus, IServiceProvider services)
		{
			this.context = context;
			this.contextplus = contextplus;
			bhost = HostServicesHelper.GetBayarHost(services);
			ghost = HostServicesHelper.GetGraphHost(services);
		}

		enum CollKind
		{
			Info,
			IdBidang,
			Batal,
			Luas,
			Ukur,
			Harga,
			Total,
			Utj,
			DP,
			Lunas,
			Mandor,
			PPH,
			Validasi,
			PajakLama,
			PajakWaris,
			Tunggakan,
			LainnyaValue,
			PTSK,
			SatuanAkte
		};

		abstract class ColInfo
		{
			public CollKind kind;
			public string[] captions;

			public ColInfo(CollKind kind, string caption)
			{
				this.kind = kind;
				this.captions = caption.Split('|');
			}

			public abstract (T[] data, string err) Get<T>(DataRow row);
			public abstract bool exists { get; }
		}

		class ColInfoS : ColInfo
		{
			public int number;

			public override bool exists => number != -1;

			public ColInfoS(CollKind kind, string caption)
				: base(kind, caption)
			{
				number = -1;
			}

			public override (T[] data, string err) Get<T>(DataRow row)
			{
				if (number == -1)
					return (new T[0], null);
				try
				{
					var obj = row[number];
					return (obj == DBNull.Value ? new T[0] : new T[] { (T)obj }, null);
				}
				catch (Exception ex)
				{
					return (new T[0], ex.Message);
				}
			}
		}

		class ColInfoM : ColInfo
		{
			public List<int> numbers = new List<int>();

			public override bool exists => numbers.Any();

			public ColInfoM(CollKind kind, string caption)
				: base(kind, caption)
			{
			}

			public override (T[] data, string err) Get<T>(DataRow row)
			{
				try
				{
					var Ts = new T[numbers.Count];
					Array.Fill(Ts, default);
					var objs = numbers.Select((n, i) => (i, obj: row[n])).Where(x => x.obj != DBNull.Value).ToArray();
					foreach (var x in objs)
						Ts[x.i] = (T)x.obj;
					return (Ts, null);
				}
				catch (Exception ex)
				{
					return (new T[0], ex.Message);
				}
			}
		}

		static (CollKind kind, string caption, bool many)[] ColumnFacts = {
			(CollKind.IdBidang, "idbidang", false),
			(CollKind.Info, "info", false),
			(CollKind.Batal, "batal", false),
			(CollKind.Luas, "luas", false),
			(CollKind.Ukur, "ukur", false),
			(CollKind.Harga, "harga", false),
			(CollKind.Total, "total", false),
			(CollKind.Utj, "utj", false),
			(CollKind.DP, "dp", true),
			(CollKind.Lunas, "lunas", false),
			(CollKind.Mandor, "mandor", false),
			(CollKind.PPH, "pph", false),
			(CollKind.Validasi, "val", false),
			(CollKind.Tunggakan, "tunggakan", false),
			(CollKind.PajakLama, "lama", false),
			(CollKind.PajakWaris, "waris", false),
			(CollKind.LainnyaValue, "lainnya", true),
			(CollKind.PTSK, "ptsk", false),
			(CollKind.SatuanAkte, "hargaakte", false)
		};

		[HttpPost("pembayaran-tanah")]
		public IActionResult ImportPembayaranTanah(IFormFile file, ProjectSwagger projectSwagger, bool ignoreGraphs = false, bool deleteOldGraph = true)
		{
			try
			{
				string ps = projectSwagger.ToString();
				switch (ps)
				{
					case "PIK_2_1000_Ha":
						ps = "PIK 2 (1000 Ha)";
						break;
					case "PIK_3_8_DESA":
						ps = "PIK 3 - 8 DESA";
						break;
					case "PIK_3_4_DESA":
						ps = "PIK 3 - 4 DESA";
						break;
					default:
						ps = ps.Replace("_", " ");
						break;
				}
				ps = ps.ToLower();
				DateTime start = DateTime.UtcNow.AddHours(7).ToUniversalTime();
				List<string> result = new List<string>();
				var filter = "{_t:'user', identifier:'importer'}";
				var project = "{_id:0,key:1}";
				var userkey = context.GetCollections(new { key = "" }, "securities", filter, project).ToList().FirstOrDefault()?.key;

				//GET KEYPROJECT
				var stages = new[]{
					$"<$match:<$expr:<$and:[<$in:['$inactive',[null,false]]>, <$eq:[<$toLower:'$identity'>,'{ps}']>]>>>".ToJson(),
					"{$project:{_id:0,key:1}}"};
				var projkey = context.GetDocuments(new { key = "" }, "maps", stages).ToList().FirstOrDefault()?.key;

				if (projkey == null)
				{
					return Ok(new { warning = $"Invalid project name: {ps}... Aborted" });
				}

				System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
				var ColumnInfoes = ColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

				bool noimport = false;
				var name = file.FileName;
				var strm = file.OpenReadStream();
				var data = new byte[strm.Length];
				strm.Read(data, 0, data.Length);

				Stream stream = new MemoryStream(data);
				var thpname = Path.GetFileNameWithoutExtension(name);
				var tahap = Int32.TryParse(thpname, out int thp) ? thp : -1;
				var reader = ExcelReaderFactory.CreateReader(stream).AsDataSet();

				var failures = new List<String>();
				var details = new List<String>();
				var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
				if (table == null)
				{
					return Ok(new { warning = $"file {name} tidak ditemukan" });
				}
				var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
							.Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
							.Where(x => !String.IsNullOrEmpty(x.s)).ToList();

				foreach (var (s, i) in firstrow)
				{
					var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s.Trim().ToLower()));

					if (col != null)
						switch (col)
						{
							case ColInfoS cs: cs.number = i; break;
							case ColInfoM cm: cm.numbers.Add(i); break;
						}
				}

				var colId = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.IdBidang);
				var noIdBidang = ((ColInfoS)colId).number;
				if (ColumnInfoes.Where(c => c.exists).Count() < 3 || noIdBidang == -1)
				{
					return Ok(new { warning = $"File {name} tidak dipersiapkan dengan benar... aborted" });
				}

				var rows = table.Rows.Cast<DataRow>().Skip(1).Select((r, i) => (r, i))
					.Where(x => x.r[noIdBidang] != DBNull.Value).ToArray();

				var oldbyr = context.bayars.FirstOrDefault(x => x.nomorTahap == tahap && x.keyProject == projkey);
				List<OldGraphs> oldg = new List<OldGraphs>();
				bool oldgthere = false;
				if (oldbyr != null)
				{
					var oldgraph = ghost.GetMany(String.Join(",", oldbyr.details.Select(y => y.instkey)));
					oldg = oldgraph.Select(x => new OldGraphs { key = x.key, userkey = x.creatorkey }).ToList();
					oldgthere = oldg.Any(x => x.userkey != userkey);
				}
				if (ignoreGraphs || !oldgthere)
				{
					var byrs = bhost.GetBayarsByProject(projkey) as IEnumerable<Bayar>;
					Pembayaran pembayaran = new Pembayaran(context, contextplus, ghost, bhost, byrs.FirstOrDefault(x => x.nomorTahap == tahap));
					string keytahap;
					var cmn = new BayarCore { keyProject = projkey, nomorTahap = tahap, keyCreator = userkey };
					try
					{
						keytahap = pembayaran.CreateTahap(cmn);

						if (keytahap == null)
						{
							return Ok(new { warning = $"Error saat pembuatan Tahap untuk file {name}... Aborted" });
						}
					}
					catch (Exception ex)
					{
						return Ok(new { warning = $"Error saat pembuatan Tahap untuk file {name} : {ex.Message}... Aborted" });
					}

					var memo = (string)null;
					var date = (DateTime?)null;
					var ln = new List<string>();
					string keyptsk = "";


					foreach (var (r, i) in rows)
					{
						var objidbidang = r[noIdBidang];
						var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
						if (string.IsNullOrWhiteSpace(idbidang))
							continue;

						switch (idbidang)
						{
							case "ptsk":
								var ptskname = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Info)?.Get<string>(r).data.FirstOrDefault().ToLower();
								keyptsk = context.ptsk.FirstOrDefault(x => x.identifier.ToLower() == ptskname).key;
								continue;
							case "*": memo = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Info)?.Get<string>(r).data.FirstOrDefault(); continue;
							case "**": date = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Info)?.Get<DateTime?>(r).data.FirstOrDefault(); continue;
							case "***": ln = ColumnInfoes.Where(c => c.kind == CollKind.LainnyaValue)?.Select(x => x.Get<string>(r).data.FirstOrDefault()).ToList(); continue;
						}

						if (!String.IsNullOrEmpty(keyptsk))
							pembayaran.UpdatePTSK(keyptsk);

						if (memo != null && date.HasValue)
							break;
					}

					if (memo == null)
						failures.Add($"Warning: File {name} tidak ada nomor memo yang ditemukan");

					if (date == null)
						failures.Add($"Warning: File {name} tidak ada tanggal memo yang ditemukan");


					List<BayarDetail> byrdet = new List<BayarDetail>();
					foreach (var (r, i) in rows)
					{
						var objidbidang = r[noIdBidang];
						var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
						if (string.IsNullOrWhiteSpace(idbidang))
							continue;

						if (idbidang.StartsWith("*"))
							continue;

						if (idbidang.StartsWith("ptsk"))
							continue;

						var persil = context.persils.FirstOrDefault(p => p.IdBidang == idbidang && p.en_state != StatusBidang.batal && (p.basic.current.en_proses == JenisProses.standar || p.basic.current.en_proses == JenisProses.overlap));
						if (persil == null)
						{
							noimport = true;
							failures.Add($"Error: File {name} Row {i} bidang tidak ada atau bidang batal atau bidang overlap/standar");
							continue;
						}

						var _batal = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Batal).Get<double>(r);
						var _luasSurat = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Luas).Get<double>(r);
						var _luasUkur = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Ukur).Get<double>(r);
						var _total = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Total).Get<double>(r);
						var _harga = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Harga).Get<double>(r);
						var _utj = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Utj).Get<double>(r);
						var _dp = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.DP).Get<double>(r);
						var _lunas = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Lunas).Get<double>(r);
						var _mandor = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Mandor).Get<double>(r);
						var _pph = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PPH).Get<double>(r);
						var _val = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Validasi).Get<double>(r);
						var _tunggakan = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Tunggakan).Get<double>(r);
						var _pw = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PajakWaris).Get<double>(r);
						var _pl = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PajakLama).Get<double>(r);
						var _lv = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.LainnyaValue).Get<double>(r);
						var _ptsk = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.PTSK).Get<string>(r);
						var _akte = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.SatuanAkte).Get<double>(r);

						Action<string, string> chkerr = (_name, st) => { if (!string.IsNullOrEmpty(st)) failures.Add($"Error: {_name.Substring(1)}:{st}"); };
						chkerr.Invoke(nameof(_batal), _batal.err);
						chkerr.Invoke(nameof(_luasSurat), _luasSurat.err);
						chkerr.Invoke(nameof(_luasUkur), _luasUkur.err);
						chkerr.Invoke(nameof(_total), _total.err);
						chkerr.Invoke(nameof(_harga), _harga.err);
						chkerr.Invoke(nameof(_utj), _utj.err);
						chkerr.Invoke(nameof(_dp), _dp.err);
						chkerr.Invoke(nameof(_lunas), _lunas.err);
						chkerr.Invoke(nameof(_mandor), _mandor.err);
						chkerr.Invoke(nameof(_pph), _pph.err);
						chkerr.Invoke(nameof(_val), _val.err);
						chkerr.Invoke(nameof(_pl), _pl.err);
						chkerr.Invoke(nameof(_pw), _pw.err);
						chkerr.Invoke(nameof(_tunggakan), _tunggakan.err);
						chkerr.Invoke(nameof(_lv), _lv.err);
						chkerr.Invoke(nameof(_ptsk), _ptsk.err);
						chkerr.Invoke(nameof(_akte), _akte.err);

						var batal = _batal.data.Cast<double?>().FirstOrDefault();
						var luasSurat = _luasSurat.data.Cast<double?>().FirstOrDefault();
						var luasUkur = _luasUkur.data.Cast<double?>().FirstOrDefault();
						var total = _total.data.Cast<double?>().FirstOrDefault();
						var harga = _harga.data.Cast<double?>().FirstOrDefault();
						var utj = _utj.data;
						var dp = _dp.data;
						var lunas = _lunas.data;
						var mandor = _mandor.data.Cast<double?>().FirstOrDefault();
						var pph = _pph.data.Cast<double?>().FirstOrDefault();
						var val = _val.data.Cast<double?>().FirstOrDefault();
						var tunggakan = _tunggakan.data.Cast<double?>().FirstOrDefault();
						var pl = _pl.data.Cast<double?>().FirstOrDefault();
						var pw = _pw.data.Cast<double?>().FirstOrDefault();
						var lv = _lv.data;
						var ptsk = _ptsk.data.Cast<string?>().FirstOrDefault();
						var akte = _akte.data.Cast<double?>().FirstOrDefault();

						if ((harga ?? 0) <= 0)
						{
							failures.Add($"Error: File {name} Row {i} nilai harga tidak benar");
							continue;
						}
						var luasbayar = total / harga;
						List<biayalainnya> lainnya = new List<biayalainnya>();
						var bid = new PersilUpdate
						{
							keyPersil = persil.key,
							luasDibayar = luasbayar,
							satuan = harga,
							mandor = (mandor ?? 0),
							pph = (pph ?? 0) != 0 ? true : false,
							val = (val ?? 0),
							tunggakan = (tunggakan ?? 0),
							pl = (pl ?? 0),
							pw = (pw ?? 0),
							ptsk = (ptsk ?? ""),
							satuanakte = (akte ?? 0)
						};

						BayarDtlCoreExt dtl;

						int colutj = 1;
						foreach (var uutj in utj)
						{
							if (uutj <= 0)
							{
								failures.Add($"Error: File {name} Row {i} nilai UTJ tidak benar ({uutj})");
								continue;
							}
							dtl = new BayarDtlCoreExt
							{
								jenisBayar = JenisBayar.UTJ,
								keyProject = projkey,
								keyPersil = persil.key,
								noTahap = tahap,
								tglBayar = date ?? DateTime.Today,
								noMemo = memo ?? $"Tgl {date}",
								Jumlah = uutj
							};

							if (!byrdet.Any(x => x.col == colutj && x.jenis == "utj"))
							{
								List<BayarDtlCoreExt> byrdtl = new List<BayarDtlCoreExt>();
								byrdtl.Add(dtl);
								byrdet.Add(new BayarDetail { col = colutj, jenis = "utj", detail = byrdtl });
							}
							else
							{
								byrdet.FirstOrDefault(x => x.col == colutj && x.jenis == "utj").detail.Add(dtl);
							}
							colutj++;
							//pembayaran.BelumLunas(dtl, graphhost, contextGraph);
							details.Add($"Tambah UTJ untuk bidang {persil.IdBidang}");
						}

						int coldp = 1;
						foreach (var ddp in dp)
						{
							if (ddp <= 0)
							{
								failures.Add($"Error: File {name} Row {i} nilai DP tidak benar ({ddp})");
								continue;
							}
							dtl = new BayarDtlCoreExt
							{
								jenisBayar = JenisBayar.DP,
								keyProject = projkey,
								keyPersil = persil.key,
								noTahap = tahap,
								tglBayar = date ?? DateTime.Today,
								noMemo = memo ?? $"Tgl {date}",
								Jumlah = ddp
							};

							if (!byrdet.Any(x => x.col == coldp && x.jenis == "dp"))
							{
								List<BayarDtlCoreExt> byrdtl = new List<BayarDtlCoreExt>();
								byrdtl.Add(dtl);
								byrdet.Add(new BayarDetail { col = coldp, jenis = "dp", detail = byrdtl });
							}
							else
							{
								byrdet.FirstOrDefault(x => x.col == coldp && x.jenis == "dp").detail.Add(dtl);
							}
							coldp++;
							//pembayaran.BelumLunas(dtl, graphhost, contextGraph);
							details.Add($"Tambah DP untuk bidang {persil.IdBidang}");
						}

						int collns = 1;
						foreach (var lns in lunas)
						{
							if (lns <= 0)
							{
								failures.Add($"Error: File {name} Row {i} nilai Lunas tidak benar ({lns})");
								continue;
							}
							dtl = new BayarDtlCoreExt
							{
								jenisBayar = JenisBayar.Lunas,
								keyProject = projkey,
								keyPersil = persil.key,
								noTahap = tahap,
								tglBayar = date ?? DateTime.Today,
								noMemo = memo ?? $"Tgl {date}",
								Jumlah = lns
							};

							if (!byrdet.Any(x => x.col == collns && x.jenis == "lunas"))
							{
								List<BayarDtlCoreExt> byrdtl = new List<BayarDtlCoreExt>();
								byrdtl.Add(dtl);
								byrdet.Add(new BayarDetail { col = collns, jenis = "lunas", detail = byrdtl });
							}
							else
							{
								byrdet.FirstOrDefault(x => x.col == collns && x.jenis == "lunas").detail.Add(dtl);
							}
							collns++;
							//pembayaran.SudahLunas(dtl, graphhost, contextGraph);
							details.Add($"Tambah Pelunasan untuk bidang {persil.IdBidang}");
						}

						var lname = ln.ToArray();
						int colln = 0;
						foreach (var lvalue in lv)
						{
							if (lvalue <= 0)
							{
								failures.Add($"Error: File {name} Row {i} nilai Lainnya tidak benar ({lvalue})");
								continue;
							}
							lainnya.Add(new biayalainnya { identity = lname[colln], nilai = lvalue, fgLainnya = true });
							colln++;
						}
						bid.biayalainnya = lainnya;
						pembayaran.SavePersil(bid, tahap);
					}
					//INPUT BIDANGS
					foreach (var dtl in byrdet.SelectMany(x => x.detail, (x, y) => y.keyPersil).Distinct())
					{
						pembayaran.AssignBidang(dtl);
					}

					//INPUT DETAILS GROUPING BY JENIS AND COLUMN
					foreach (var dtl in byrdet.Where(x => x.jenis == "utj" || x.jenis == "dp"))
					{
						pembayaran.BelumLunas(dtl.detail.FirstOrDefault(), dtl.detail.Select(x => new BayarSubDtl
						{
							keyPersil = x.keyPersil,
							Jumlah = x.Jumlah
						}).ToList());
					}
					foreach (var dtl in byrdet.Where(x => x.jenis == "lunas"))
					{
						pembayaran.SudahLunas(dtl.detail.FirstOrDefault(), dtl.detail.Select(x => new BayarSubDtl
						{
							keyPersil = x.keyPersil,
							Jumlah = x.Jumlah
						}).ToList());
					}

					if (deleteOldGraph && oldg.Count != 0)
						oldg.ToList().ForEach(x =>
						{
							ghost.Del(x.key);
						});
				}
				else
				{
					noimport = true;
					result.Add($"Tahap {tahap} tidak diimport karena ada graph bukan creator!\nKey Graphs: {String.Join(",", oldg.Where(x => x.userkey != userkey).Select(x => x.key))}");
				}


				if (!noimport)
				{
					if (failures != null && failures.Any())
					{
						result.Add($"Tahap {tahap} berhasil diimport dengan catatan: ");
						result.AddRange(failures);
					}
					else
						result.Add($"Tahap {tahap} berhasil diimport!");
				}
				DateTime end = DateTime.UtcNow.AddHours(7).ToUniversalTime();
				return Ok(new { result = result.ToArray(), start = start, end = end, time = ((end - start).TotalSeconds).ToString() + " second" });

			}
			catch (Exception ex)
			{
				return new UnprocessableEntityObjectResult(ex.Message);
			}
		}
		private class BayarDetail
		{
			public string persilKey { get; set; }
			public string jenis { get; set; }
			public int col { get; set; }
			public List<BayarDtlCoreExt> detail { get; set; }
		}

		private class OldGraphs
		{
			public string key { get; set; }
			public string userkey { get; set; }
		}
	}

	public class PersilUpdate
	{
		public string Tkey { get; set; }
		public string keyPersil { get; set; }
		public double? luasDibayar { get; set; }
		public double? satuan { get; set; }
		public double? satuanakte { get; set; }
		public List<biayalainnya> biayalainnya { get; set; }
		public string reason { get; set; }
		public bool lunas { get; set; }
		public bool pph { get; set; }
		public double? val { get; set; }
		public double? tunggakan { get; set; }
		public double? mandor { get; set; }
		public double? pl { get; set; }
		public double? pw { get; set; }
		public string ptsk { get; set; }
	}

	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum ProjectSwagger
	{
		PIK_6,
		PIK_6_Land_Bank,
		PIK_5,
		PIK_4,
		PIK_3_8_DESA,
		PIK_3_4_DESA,
		PIK_2_Extension,
		PIK_2_1000_Ha
	}


	public static class StringEnumerableExtensions
	{
		public static IEnumerable<T> StringsToEnums<T>(this IEnumerable<string> strs) where T : struct, IConvertible
		{
			Type t = typeof(T);

			var ret = new List<T>();

			if (t.IsEnum)
			{
				T outStr;
				foreach (var str in strs)
				{
					if (Enum.TryParse(str, out outStr))
					{
						ret.Add(outStr);
					}
				}
			}

			return ret;
		}
	}

	public class Pembayaran
	{

		LandropePayContext context;
		LandropePlusContext contextplus;
		GraphHostSvc ghost;
		BayarHost bhost;
		Bayar byr;

		public Pembayaran(LandropePayContext context, LandropePlusContext contextplus, GraphHostSvc ghost, BayarHost bhost, Bayar byr)
		{
			this.context = context;
			this.contextplus = contextplus;
			this.ghost = ghost;
			this.bhost = bhost;
			if (byr != null)
				this.byr = byr;
		}

		public string CreateTahap(BayarCore Bayars)
		{
			try
			{
				if (byr == null)
				{
					var ent = new Bayar(new user { key = Bayars.keyCreator });
					ent.FromCore(Bayars);
					ent.key = entity.MakeKey;
					byr = ent;
					bhost.Add(byr);
				}
				else
				{
					byr.keyCreator = Bayars.keyCreator;
					byr.created = DateTime.Now;
					byr.details = new List<BayarDtl>().ToArray();
					byr.bidangs = new List<BayarDtlBidang>().ToArray();
					bhost.Update(byr);
				}
				return byr.key;
			}
			catch (Exception ex)
			{
				throw ex;
			}

		}

		public void AssignBidang(string keyPersil)
		{
			try
			{
				var bdg = byr.bidangs.Any(x => x.keyPersil == keyPersil);

				if (bdg != true)
				{
					var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = keyPersil };

					if (string.IsNullOrEmpty(byr.keyDesa))
					{
						var desa = GetPersil(keyPersil).basic.current.keyDesa;
						byr.keyDesa = desa;
					}

					byr.AddDetailBidang(byrdtlbidang);
					bhost.Update(byr);
				}
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void UpdatePTSK(string keyptsk)
		{
			try
			{
				byr.keyPTSK = keyptsk;
				bhost.Update(byr);
			}
			catch (Exception ex)
			{
				throw ex;
			}

		}

		public void SudahLunas(BayarDtlCoreExt CoreExt, List<BayarSubDtl> subdtl)
		{
			try
			{
				var byrdtl = new BayarDtl()
				{
					keyPersil = CoreExt.keyPersil,
					jenisBayar = JenisBayar.Lunas,
					Jumlah = CoreExt.Jumlah,
					noMemo = CoreExt.noMemo,
					tglBayar = CoreExt.tglBayar
				};

				foreach (var byrSubDtl in subdtl)
				{
					if (byrSubDtl.Jumlah > 0)
						byrdtl.AddSubDetail(byrSubDtl);
				}
				byrdtl.keyPersil = String.Join(",", byrdtl.subdetails.Select(x => x.keyPersil));
				byrdtl.Jumlah = byrdtl.subdetails.Sum(x => x.Jumlah);
				byrdtl.CreateGraphInstance(new user { key = byr.keyCreator }, byrdtl.jenisBayar, ghost);
				string keygraph = byrdtl.instkey;
				var newgraph = ghost.Get(keygraph);
				newgraph.lastState.state = ToDoState.complished_;
				newgraph.lastState.time = DateTime.Now;
				newgraph.closed = true;
				ghost.Update(newgraph, ghost.context);
				byr.AddDetail(byrdtl);

				var bdg = byr.bidangs.Any(x => x.keyPersil == CoreExt.keyPersil);
				if (bdg == false)
				{
					var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = CoreExt.keyPersil };
					if (string.IsNullOrEmpty(byr.keyDesa))
					{
						var desa = GetPersil(CoreExt.keyPersil).basic.current.keyDesa;
						byr.keyDesa = desa;
					}
					byr.AddDetailBidang(byrdtlbidang);
				}
				bhost.Update(byr);

				var persil = GetPersil(CoreExt.keyPersil);
				persil.luasPelunasan = persil.basic.current.luasDibayar;
				context.persils.Update(persil);
				context.SaveChanges();

				BebasBidang(CoreExt.keyPersil);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void BelumLunas(BayarDtlCoreExt CoreExt, List<BayarSubDtl> subdtl)
		{
			try
			{
				var byrdtl = new BayarDtl()
				{
					keyPersil = CoreExt.keyPersil,
					jenisBayar = CoreExt.jenisBayar,
					Jumlah = CoreExt.Jumlah,
					noMemo = CoreExt.noMemo,
					tglBayar = CoreExt.tglBayar
				};

				foreach (var byrSubDtl in subdtl)
				{
					if (byrSubDtl.Jumlah > 0)
						byrdtl.AddSubDetail(byrSubDtl);
				}
				byrdtl.keyPersil = String.Join(",", byrdtl.subdetails.Select(x => x.keyPersil));
				byrdtl.Jumlah = byrdtl.subdetails.Sum(x => x.Jumlah);
				byrdtl.CreateGraphInstance(new user { key = byr.keyCreator }, byrdtl.jenisBayar, ghost);
				string keygraph = byrdtl.instkey;
				var newgraph = ghost.Get(keygraph);
				newgraph.lastState.state = ToDoState.complished_;
				newgraph.lastState.time = DateTime.Now;
				newgraph.closed = true;
				ghost.Update(newgraph, ghost.context);

				byr.AddDetail(byrdtl);

				var bdg = byr.bidangs.Any(x => x.keyPersil == CoreExt.keyPersil);

				if (bdg == false)
				{
					var byrdtlbidang = new BayarDtlBidang() { key = mongospace.MongoEntity.MakeKey, keyPersil = CoreExt.keyPersil };
					if (string.IsNullOrEmpty(byr.keyDesa))
					{
						var desa = GetPersil(CoreExt.keyPersil).basic.current.keyDesa;
						byr.keyDesa = desa;
					}
					byr.AddDetailBidang(byrdtlbidang);
				}
				bhost.Update(byr);
				BebasBidang(CoreExt.keyPersil);
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void SaveLuas(BidangCommand Cmd, int tahap)
		{
			try
			{
				var userKey = context.users.FirstOrDefault(x => x.identifier == "importer").key;
				var persil = GetPersil(Cmd.keyPersil);
				var last = persil.basic.entries.LastOrDefault();

				var item = new PersilBasic();
				item = persil.basic.current;
				item.tahap = tahap;
				item.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
				item.luasInternal = Cmd.luasInternal == null ? item.luasInternal : Cmd.luasInternal;
				item.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;

				var newEntries1 =
					   new ValidatableEntry<PersilBasic>
					   {
						   created = DateTime.Now,
						   en_kind = ChangeKind.Update,
						   keyCreator = userKey,
						   keyReviewer = userKey,
						   reviewed = DateTime.Now,
						   approved = true,
						   item = item
					   };

				persil.basic.entries.Add(newEntries1);
				persil.basic.current = item;
				persil.luasFix = true;
				context.persils.Update(persil);
				context.SaveChanges();

				if (last != null && last.reviewed == null)
				{
					var item2 = new PersilBasic();
					item2 = last.item;
					item2.tahap = tahap;
					item2.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
					item2.luasInternal = Cmd.luasInternal == null ? item.luasInternal : Cmd.luasInternal;
					item2.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;
					item2.total = (item.satuan * item.luasDibayar) ?? 0;

					var newEntries2 =
						new ValidatableEntry<PersilBasic>
						{
							created = last.created,
							en_kind = last.en_kind,
							keyCreator = last.keyCreator,
							keyReviewer = last.keyReviewer,
							reviewed = last.reviewed,
							approved = last.approved,
							item = item2
						};

					persil.basic.entries.Add(newEntries2);

					context.persils.Update(persil);
					context.SaveChanges();
				}
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void SaveLuasLunas(BidangCommand Cmd)
		{
			try
			{
				var userKey = context.users.FirstOrDefault(x => x.identifier == "importer").key;
				var persil = GetPersil(Cmd.keyPersil);
				var last = persil.basic.entries.LastOrDefault();

				var item = new PersilBasic();
				item = persil.basic.current;
				item.luasDibayar = (Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar) ?? 0;
				item.satuan = (Cmd.satuan == null ? item.satuan : Cmd.satuan) ?? 0;
				item.total = (item.satuan * item.luasDibayar) ?? 0;

				var newEntries1 =
					   new ValidatableEntry<PersilBasic>
					   {
						   created = DateTime.Now,
						   en_kind = ChangeKind.Update,
						   keyCreator = userKey,
						   keyReviewer = userKey,
						   reviewed = DateTime.Now,
						   approved = true,
						   item = item
					   };

				persil.basic.entries.Add(newEntries1);
				persil.basic.current = item;
				persil.luasPelunasan = (Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar) ?? 0;
				context.persils.Update(persil);
				context.SaveChanges();

				if (last != null && last.reviewed == null)
				{
					var item2 = new PersilBasic();
					item2 = last.item;
					item2.luasDibayar = Math.Round((Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar) ?? 0, 1);
					item2.satuan = Math.Round((Cmd.satuan == null ? item.satuan : Cmd.satuan) ?? 0, 1);
					item2.total = Math.Round((item2.luasDibayar * item2.satuan) ?? 0, 1);

					var newEntries2 =
						new ValidatableEntry<PersilBasic>
						{
							created = last.created,
							en_kind = last.en_kind,
							keyCreator = last.keyCreator,
							keyReviewer = last.keyReviewer,
							reviewed = last.reviewed,
							approved = last.approved,
							item = item2
						};

					persil.basic.entries.Add(newEntries2);

					context.persils.Update(persil);
					context.SaveChanges();
				}
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void SavePersil(PersilUpdate Cmd, int tahap)
		{
			try
			{
				var userKey = context.users.FirstOrDefault(x => x.identifier == "importer").key;
				var persil = GetPersil(Cmd.keyPersil);
				var last = persil.basic.entries.LastOrDefault();

				var item = new PersilBasic();
				item = persil.basic.current;
				item.tahap = tahap;
				item.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
				item.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;
				if (!String.IsNullOrEmpty(Cmd.ptsk))
				{
					item.keyPTSK = Cmd.ptsk;
				}
				if (Cmd.pph)
				{
					item.satuanAkte = (Cmd.satuanakte ?? 0) == 0 ? Cmd.satuan : Cmd.satuanakte;
					persil.pph21 = true;
				}

				var newEntries1 =
					   new ValidatableEntry<PersilBasic>
					   {
						   created = DateTime.Now,
						   en_kind = ChangeKind.Update,
						   keyCreator = userKey,
						   keyReviewer = userKey,
						   reviewed = DateTime.Now,
						   approved = true,
						   item = item
					   };

				persil.basic.entries.Add(newEntries1);
				persil.basic.current = item;
				persil.luasFix = true;
				if (Cmd.lunas)
				{
					persil.luasPelunasan = item.luasDibayar;
				}
				if (Cmd.val > 0)
				{
					persil.ValidasiPPH = true;
					persil.ValidasiPPHValue = Cmd.val;
				}
				if (Cmd.tunggakan > 0)
				{
					persil.tunggakanPBB = Cmd.tunggakan;
				}
				if (Cmd.pl > 0)
				{
					persil.pajakLama = Cmd.pl;
				}
				if (Cmd.pw > 0)
				{
					persil.pajakWaris = Cmd.pw;
				}
				if (Cmd.mandor > 0)
				{
					persil.mandor = Cmd.mandor;
				}
				if (Cmd.biayalainnya.Count != 0)
				{
					persil.biayalainnya = Cmd.biayalainnya.ToArray();
				}
				context.persils.Update(persil);
				context.SaveChanges();

				if (last != null && last.reviewed == null)
				{
					var item2 = new PersilBasic();
					item2 = last.item;
					item2.tahap = tahap;
					item2.luasDibayar = Cmd.luasDibayar == null ? item.luasDibayar : Cmd.luasDibayar;
					item2.satuan = Cmd.satuan == null ? item.satuan : Cmd.satuan;
					item2.total = (item.satuan * item.luasDibayar) ?? 0;
					if (!String.IsNullOrEmpty(Cmd.ptsk))
					{
						item2.keyPTSK = Cmd.ptsk;
					}
					if (Cmd.pph)
					{
						item2.satuanAkte = (Cmd.satuanakte ?? 0) == 0 ? Cmd.satuan : Cmd.satuanakte;
					}

					var newEntries2 =
						new ValidatableEntry<PersilBasic>
						{
							created = last.created,
							en_kind = last.en_kind,
							keyCreator = last.keyCreator,
							keyReviewer = last.keyReviewer,
							reviewed = last.reviewed,
							approved = last.approved,
							item = item2
						};

					persil.basic.entries.Add(newEntries2);

					context.persils.Update(persil);
					context.SaveChanges();
				}
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void BatalBidang(string keyProject, int nomorTahap, string keyPersil)
		{
			try
			{
				var persil = GetPersil(keyPersil);

				if (persil != null)
				{
					persil.en_state = StatusBidang.batal;
					context.persils.Update(persil);

					var bayar = context.bayars.FirstOrDefault(x => x.nomorTahap == nomorTahap && x.keyProject == keyProject);

					var bidangs = bayar.bidangs;
					var bidang = bidangs.FirstOrDefault(x => x.keyPersil == keyPersil);

					if (bidang != null)
					{
						List<BayarDtlBidang> listBidang = new List<BayarDtlBidang>();
						if (bidangs != null)
							listBidang = bidangs.ToList();

						listBidang.Remove(bidang);
						bidangs = listBidang.ToArray();

						bayar.bidangs = bidangs;
					}



					var details = bayar.details;
					if (bidangs != null)
					{
						List<BayarDtl> listDetail = new List<BayarDtl>();
						listDetail = details.ToList();

						listDetail.RemoveAll(x => x.keyPersil == keyPersil);
						details = listDetail.ToArray();

						bayar.details = details;
					}

					context.bayars.Update(bayar);
					context.SaveChanges();
				}
			}
			catch (Exception ex)
			{

				throw ex;
			}
		}

		public void BebasBidang(string keyPersil)
		{
			try
			{
				var persil = GetPersil(keyPersil);
				if (persil != null)
				{
					if (persil.en_state != StatusBidang.bebas && persil.en_state != null)
					{
						bool belumbebas = true;
						if (persil.en_state != StatusBidang.belumbebas)
							belumbebas = false;
						persil.en_state = StatusBidang.bebas;
						context.persils.Update(persil);
						context.SaveChanges();

						if (belumbebas)
						{
							var template = contextplus.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{}").FirstOrDefault();
							Console.WriteLine($"Bebas Bidang : {persil.IdBidang}");
							MakeBundle(template, persil);
						}
					}
				}
			}
			catch (Exception)
			{

				throw;
			}
		}

		public Persil GetPersil(string key)
		{
			return context.persils.FirstOrDefault(p => p.key == key);
		}

		public void MakeBundle(MainBundle template, Persil persil)
		{
			if (template == null)
				return;

			template._id = ObjectId.Empty;
			template.key = persil.key;
			template.IdBidang = persil.IdBidang;
			contextplus.mainBundles.Remove(template);
			contextplus.SaveChanges();
			contextplus.mainBundles.Insert(template);
			contextplus.SaveChanges();

			var bhost = new BundlerHostConsumer();
			bhost.MainGet(persil.key);

		}
	}
}

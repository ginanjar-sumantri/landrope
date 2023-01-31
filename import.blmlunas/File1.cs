namespace fimport.blmlunas
{

	using System;
	using System.Collections.Generic;
	using landrope.mod2;
	using landrope.mod4;
	using System.Linq;
	using MongoDB.Driver;
	using MongoDB.Bson;
	using System.IO;
	using ExcelDataReader;
	using ExcelDataReader.Core;
	using System.Data;
	using ImportPembayaran;
	using landrope.common;

	class ExcelHandler
	{
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
			Taxes
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
					return (new T[0],null);
				try
				{
					var obj = row[number];
				}
				catch(Exception ex)
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
				}
				catch (Exception ex)
				{
					return (new T[0],ex.Message);
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
			(CollKind.Taxes, "taxes|pph|val", true)
		};

		public static string[] ImportExcel(LandropePayContext context, Stream strm, string name, string projkey, string userkey)
		{
			var ColumnInfoes = ColumnFacts.Select(x => x.many ? (ColInfo)new ColInfoM(x.kind, x.caption) : new ColInfoS(x.kind, x.caption)).ToArray();

			var thpname = Path.GetFileNameWithoutExtension(name);
			Console.WriteLine($"Processing sheet {name}...");
			var tahap = Int32.TryParse(thpname, out int thp) ? thp : -1;
			var reader = ExcelReaderFactory.CreateReader(strm).AsDataSet();

			var pembayaran = new Pembayaran(context);

			var failures = new List<String>();
			var table = reader.Tables.Cast<DataTable>().FirstOrDefault();
			if (table == null)
			{
				failures.Add($"file {name} tidak ditemukan");
				return failures.ToArray();
			}
			var firstrow = table.Rows[0].ItemArray.Select((o, i) => (o, i))
						.Where(x => x.o != DBNull.Value).Select(x => (s: x.o?.ToString(), x.i))
						.Where(x => !String.IsNullOrEmpty(x.s)).ToList();

			foreach (var (s, i) in firstrow)
			{
				var col = ColumnInfoes.FirstOrDefault(c => c.captions.Contains(s.ToLower()));

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
				failures.Add($"File {name} tidak dipersiapkan dengan benar... aborted");
				return failures.ToArray();
			}

			var rows = table.Rows.Cast<DataRow>().Skip(1).Select((r, i) => (r, i))
				.Where(x => x.r[noIdBidang] != DBNull.Value).ToArray();

			string keytahap;
			var cmn = new BayarCore { keyProject = projkey, nomorTahap = tahap, keyCreator = userkey };
			try
			{
				keytahap = pembayaran.CreateTahap(cmn, true);

				if (keytahap == null)
				{
					failures.Add($"Error saat pembuatan Tahap untuk file {name}... Aborted");
					return failures.ToArray();
				}
			}
			catch (Exception ex)
			{
				failures.Add($"Error saat pembuatan Tahap untuk file {name} : {ex.Message}... Aborted");
				return failures.ToArray();
			}

			var memo = (string)null;
			var date = (DateTime?)null;

			foreach (var (r, i) in rows)
			{
				var objidbidang = r[noIdBidang];
				var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
				if (string.IsNullOrWhiteSpace(idbidang))
					continue;

				switch (idbidang)
				{
					case "*": memo = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Info)?.Get<string>(r).data.FirstOrDefault(); continue;
					case "**": date = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Info)?.Get<DateTime?>(r).data.FirstOrDefault(); continue;
				}

				if (memo != null && date.HasValue)
					break;
			}

			if (memo == null)
				failures.Add($"Warning: File {name} tidak ada nomor memo yang ditemukan");

			if (date == null)
				failures.Add($"Warning: File {name} tidak ada tanggal memo yang ditemukan");


			foreach (var (r, i) in rows)
			{
				var objidbidang = r[noIdBidang];
				var idbidang = objidbidang == DBNull.Value ? null : objidbidang.ToString();
				if (string.IsNullOrWhiteSpace(idbidang))
					continue;

				if (idbidang.StartsWith("*"))
					continue;

				var persil = context.persils.FirstOrDefault(p => p.IdBidang == idbidang);
				if (persil == null)
				{
					failures.Add($"Error: File {name} Row {i} bidang tidak ada");
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
				var _taxes = ColumnInfoes.FirstOrDefault(c => c.kind == CollKind.Taxes).Get<double>(r);

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
				chkerr.Invoke(nameof(_taxes), _taxes.err);

				var batal = _batal.data.Cast<double?>().FirstOrDefault();
				var luasSurat = _luasSurat.data.Cast<double?>().FirstOrDefault();
				var luasUkur = _luasUkur.data.Cast<double?>().FirstOrDefault();
				var total = _total.data.Cast<double?>().FirstOrDefault();
				var harga = _harga.data.Cast<double?>().FirstOrDefault();
				var utj = _utj.data;
				var dp = _dp.data;
				var lunas = _lunas.data;
				var mandor = _mandor.data;
				var taxes = _taxes.data;

				if (batal != null && batal == 1)
				{
					pembayaran.BatalBidang(projkey, tahap, persil.key);
					continue;
				}
				if ((harga ?? 0) <= 0)
				{
					failures.Add($"Error: File {name} Row {i} nilai harga tidak benar");
					continue;
				}
				var luasbayar = total / harga;
				var bid = new BidangCommand { keyPersil = persil.key, luasDibayar = luasbayar, luasInternal = luasUkur, satuan = harga };

				pembayaran.SaveLuas(bid);

				BayarDtlCoreExt dtl;

				//Console.Write("Cek UTJ... ");
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
					pembayaran.BelumLunas(dtl);
				}

				//Console.Write("Cek DP... ");
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
					pembayaran.BelumLunas(dtl);
				}

				//Console.Write("Cek mandor... ");
				foreach (var mdr in mandor)
				{
					if (mdr <= 0)
					{
						failures.Add($"Error: File {name} Row {i} nilai mador tidak benar ({mdr})");
						continue;
					}
					dtl = new BayarDtlCoreExt
					{
						jenisBayar = JenisBayar.Mandor,
						keyProject = projkey,
						keyPersil = persil.key,
						noTahap = tahap,
						tglBayar = date ?? DateTime.Today,
						noMemo = memo ?? $"Tgl {date}",
						Jumlah = mdr
					};
					pembayaran.BelumLunas(dtl);
				}

				//Console.Write("Cek Taxes... ");
				foreach (var tax in taxes)
				{
					if (tax <= 0)
					{
						failures.Add($"Error: File {name} Row {i} nilai taxes tidak benar ({tax})");
						continue;
					}
					dtl = new BayarDtlCoreExt
					{
						jenisBayar = JenisBayar.Lainnya,
						keyProject = projkey,
						keyPersil = persil.key,
						noTahap = tahap,
						tglBayar = date ?? DateTime.Today,
						noMemo = memo ?? $"Tgl {date}",
						Jumlah = tax
					};
					pembayaran.BelumLunas(dtl);
				}

				//Console.Write("Cek Lunas... ");
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
					pembayaran.BelumLunas(dtl);
				}
			}

			return failures.ToArray();
		}
	}
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
}
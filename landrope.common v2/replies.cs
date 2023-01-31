using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;

namespace landrope.common
{
	public class Location
	{
		public string key { get; set; }
		public string name { get; set; }
	}
	public class Enumerated
	{
		public int id { get; set; }
		public string desc { get; set; }
	}

	public class ReportWithBudget
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
		public string keyProject { get; set; }
		public int category { get; set; }
		public int status { get; set; }
		public int next_step { get; set; }
		public double luas { get; set; }
		public BudgetDtl[] budgets { get; set; }
		public bool MasukBPN { get; set; }
	}

	public class BudgetDtl
	{
		public string IdBidang { get; set; }
		public DocProcessStep step { get; set; }
		public double price { get; set; }
		public double amount { get; set; }
		public double accumulative { get; set; }
	}

	public class ProgressReport
	{
		public string key { get; set; }
		public string IdBidang { get; set; }
		public string keyProject { get; set; }
		public string keyDesa { get; set; }
		public int category { get; set; }
		public int status { get; set; }
		public int? next_step { get; set; }
		public bool bpn { get; set; }
		public decimal sps { get; set; }
		public decimal luas { get; set; }
		public string pbt { get; set; }

		public ProgressReport() { }
		public ProgressReport((AssignmentCat cat, string keyProject, DocProcessStep step, DocProcessStep? next, bool bpn) dummy)
		{
			key = ""+DateTime.Now.Ticks.GetHashCode();
			IdBidang = key;
			keyProject = dummy.keyProject;
			keyDesa = "";
			category = (int)dummy.cat;
			status = (int)dummy.step;
			next_step = (int?)dummy.next;
			bpn = dummy.bpn;
			sps = 0;
			luas = 0;
		}
	}

	public class Progressive
	{
		[BsonDateTimeOptions(Kind=DateTimeKind.Local)]
		public DateTime chrono { get; set; }
		public ProgressReport[] data { get; set; }
	}

	public class ChronoProgress : ProgressReport
	{
		public DateTime chrono { get; set; }
		public bool past { get; set; } = false;

		public static ChronoProgress FromReport(DateTime chrono, ProgressReport info)
		{
			var json = JsonConvert.SerializeObject(info);
			var obj = JsonConvert.DeserializeObject<ChronoProgress>(json);
			obj.chrono = chrono;
			return obj;
		}
	}

	public class ChronoProgessView
	{
		public DateTime chrono { get; set; }
		public bool past { get; set; } = false;
		public string key { get; set; }
		public string IdBidang { get; set; }
		public string project { get; set; }
		public string desa { get; set; }
		public string category { get; set; }
		public string status { get; set; }
		public string next_step { get; set; }
		public bool bpn { get; set; }
		public decimal sps { get; set; }
		public decimal count { get; set; } = 1;
		public decimal luas { get; set; }
		public string pbt { get; set; }
	}

	public class PivotProgressView
	{
		public DateTime chrono { get; set; }
		public bool past { get; set; } = false;
		public string project { get; set; }
		public string desa { get; set; }
		public AssignmentCat category { get; set; }
		public bool bpn { get; set; }
		public decimal count { get; set; }
		public decimal sps { get; set; }
		public decimal luas { get; set; }
		public decimal lsps { get; set; }
		public string pbt { get; set; }
		public string position { get; set; }

		public PivotProgressView() { }
		public PivotProgressView((DateTime chrono, bool past, AssignmentCat cat, string project, DocProcessStep step, DocProcessStep? next, bool bpn) dummy)
		{
			chrono = dummy.chrono;
			project = dummy.project;
			desa = "";
			category = dummy.cat;
			bpn = dummy.bpn;
			count = 0;
			sps = 0;
			luas = 0;
			lsps = 0;
			position = SetPosition(dummy.step, dummy.next, dummy.cat);
		}

		static DocProcessStep[] Finals = new[] { DocProcessStep.Balik_Nama, DocProcessStep.Cetak_Buku };

		static string GetName(DocProcessStep? step) => step == null ? "" : step.Value.ToString("g").Replace("_", " ");
		
		public static string SetPosition(DocProcessStep state, DocProcessStep? nextstep, AssignmentCat cat)
		{
			string curr = GetName(state);
			string next;
			if (state == DocProcessStep.Baru_Bebas)
				nextstep = cat==AssignmentCat.Hibah? DocProcessStep.Proses_Hibah : DocProcessStep.Riwayat_Tanah;
			
			if (nextstep == null)
			{
				curr = "Sudah " + curr;
				next = Finals.Contains(state) ? " - Selesai" : $" (Persiapan {GetName(state.NextStep(cat))})";
			}
			else
			{
				curr = "Sedang ";
				next = GetName(nextstep);
			}
			return $"{state.Order():00}{nextstep?.Order() ?? 0:00}{curr}{next}";
		}

	}

	public class PivotProgressViewExt:PivotProgressView
	{ 
		public static decimal pastSignature = 1e-10m;
		public static decimal bpnSignature = 1e-5m;

		public string catstr => category == AssignmentCat.Girik ? category.ToString("g") : "Sertifikat";
		public decimal xCount => count + (past ? pastSignature : 0);
		public decimal xLuas => luas + (past ? pastSignature : 0);
		public decimal xSps => (bpn ? sps + bpnSignature : 0) + (past ? pastSignature : 0);
		public decimal xLuassps => (bpn ? lsps + bpnSignature : 0) + (past ? pastSignature : 0);
	}

	public static class PivotHelper
	{
		public static (decimal value, bool bpn, bool past) GetAttribute(this decimal value) 
		{
			try
			{
				var val = Math.Truncate(value);
				var residu = (value - val) / PivotProgressViewExt.bpnSignature;
				var bpn = Math.Truncate(residu);
				var hist = residu - bpn;
				//Console.WriteLine($"{value}-->({val},{bpn},{hist})==>({val},{bpn != 0},{hist != 0})");
				return (val, bpn != 0, hist != 0);
			}
			catch(Exception ex)
			{
				return (0, false, false);
			}
		}
	}
	//public class Combo<T>
	//{
	//	public T value { get; set; }
	//	public int historic { get; set; }
	//	public int bpn { get; set; }

	//	public Combo(T value, int hist, int bpn)
	//	{
	//		this.value = value;
	//		this.historic = hist;
	//		this.bpn = bpn;
	//	}
	//}

	//public class ComboInt : Combo<int>
	//{
	//	public ComboInt(int value, int hist, int bpn)
	//		: base(value, hist, bpn)
	//	{ }

	//	public static ComboInt operator +(ComboInt x, ComboInt y) =>
	//		new ComboInt(x.value + y.value, x.historic + y.historic, x.bpn + y.bpn);
	//	public static ComboInt operator -(ComboInt x, ComboInt y) =>
	//		new ComboInt(x.value - y.value, x.historic + y.historic, x.bpn + y.bpn);
	//}

	//public class ComboDouble : Combo<Double>, IComparable, IComparable<ComboDouble>, IConvertible, IEquatable<ComboDouble>, IFormattable
	//{
	//	public ComboDouble(Double value, int hist, int bpn)
	//		: base(value, hist, bpn)
	//	{ }

	//	int IComparable.CompareTo(object obj) => obj is ComboDouble cd ? value.CompareTo(cd.value) : -1;

	//	int IComparable<ComboDouble>.CompareTo(ComboDouble other) => value.CompareTo(other.value);

	//	bool IEquatable<ComboDouble>.Equals(ComboDouble other) =>
	//		value == other.value && (historic == 0) == (other.historic == 0) && (bpn == 0) == (other.bpn == 0);

	//	TypeCode IConvertible.GetTypeCode() => TypeCode.Double;

	//	bool IConvertible.ToBoolean(IFormatProvider provider) => false;

	//	byte IConvertible.ToByte(IFormatProvider provider) => (byte)0;

	//	char IConvertible.ToChar(IFormatProvider provider) => '\x00';

	//	DateTime IConvertible.ToDateTime(IFormatProvider provider) => DateTime.MinValue;

	//	decimal IConvertible.ToDecimal(IFormatProvider provider) => (decimal)value + historic*0.0001m + bpn*0.0000001m
	//	{
	//		throw new NotImplementedException();
	//	}

	//	double IConvertible.ToDouble(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	short IConvertible.ToInt16(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	int IConvertible.ToInt32(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	long IConvertible.ToInt64(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	sbyte IConvertible.ToSByte(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	float IConvertible.ToSingle(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	string IConvertible.ToString(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	string IFormattable.ToString(string format, IFormatProvider formatProvider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	object IConvertible.ToType(Type conversionType, IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	ushort IConvertible.ToUInt16(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	uint IConvertible.ToUInt32(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	ulong IConvertible.ToUInt64(IFormatProvider provider)
	//	{
	//		throw new NotImplementedException();
	//	}

	//	public static ComboDouble operator +(ComboDouble x, ComboDouble y) =>
	//		new ComboDouble(x.value + y.value, x.historic + y.historic, x.bpn + y.bpn);
	//	public static ComboDouble operator -(ComboDouble x, ComboDouble y) =>
	//		new ComboDouble(x.value - y.value, x.historic + y.historic, x.bpn + y.bpn);
	//}

	public class GetCurrentReply
	{
		public Location[] projects { get; set; }
		public Location[] desas { get; set; }
		public Enumerated[] steps { get; set; }
		public Enumerated[] categories { get; set; }
		public ReportWithBudget[] items { get; set; }
	}
}

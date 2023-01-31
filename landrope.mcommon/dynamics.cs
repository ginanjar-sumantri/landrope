using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace landrope.mcommon
{
	public class Dynamic
	{
		public enum ValueType
		{
			Null = 0,
			String = 1,
			Int = 2,
			Number = 3,
			Bool = 4,
			Date = 5
		}

		public ValueType type { get; set; }
		public string val { get; set; }

		public Dynamic(object obj)
		{
			Value = obj;
		}

		public Dynamic(ValueType type, string val)
		{
			this.type=type;
			this.val=val;
		}

		public Dynamic(Type type, string val)
		{
			var type2 = type.Name.StartsWith("Nullable") ? type.GenericTypeArguments[0] : type;
			var dt = type2.Name switch
			{
				nameof(Int32) or
				nameof(Int16) or
				nameof(Int64) => ValueType.Int,
				nameof(Decimal) 
				or nameof(Double) 
				or nameof(Single) => ValueType.Number,
				nameof(DateTime) => ValueType.Date,
				nameof(Boolean) => ValueType.Bool,
				_ => ValueType.String
			};
			this.type = dt;
			this.val = val;
		}

		[BsonIgnore]
		public object Value
		{
			get
			{
				return type switch
				{
					ValueType.Int => int.TryParse(val, out int vint) ? vint : (int?)null,
					ValueType.Number => double.TryParse(val, out double vdec) ? vdec : (double?)null,
					ValueType.Bool => bool.TryParse(val, out bool vbool) ? vbool : (bool?)null,
					ValueType.Date => DateTime.TryParse(val, out DateTime vdate) ? vdate : (DateTime?)null,
					_ => val
				};
			}
			set
			{
				(type, val) = value switch
				{
					null => (ValueType.Null, null),
					int vint => (ValueType.Int, $"{vint}"),
					short vint => (ValueType.Int, $"{vint}"),
					long vint => (ValueType.Int, $"{vint}"),
					double vdec => (ValueType.Number, $"{vdec}"),
					decimal vdec => (ValueType.Number, $"{vdec}"),
					float vdec => (ValueType.Number, $"{vdec}"),
					bool vbool => (ValueType.Bool, $"{vbool}"),
					DateTime vdate => (ValueType.Date, $"{vdate}"),
					_ => (ValueType.String, value.ToString())
				};
			}
		}
		public static bool operator >(Dynamic left, Dynamic right)
		{
			if (left.type != right.type)
				return false;
			if (string.IsNullOrEmpty(left.val))
				return false;

			int ileft = int.MinValue, iright = int.MinValue;
			double dleft = double.MinValue, dright = double.MinValue;
			bool bleft = false, bright = false;
			DateTime tleft = DateTime.MinValue, tright = DateTime.MinValue;

			switch (left.type)
			{
				case ValueType.Int:
					int.TryParse(left.val, out ileft);
					int.TryParse(right.val, out iright);
					return ileft > iright;
				case ValueType.Number:
					double.TryParse(left.val, out dleft);
					double.TryParse(right.val, out dright);
					return dleft > dright;
				case ValueType.Bool:
					bool.TryParse(left.val, out bleft);
					bool.TryParse(right.val, out bright);
					return (bleft ? 1 : 0) > (bright ? 1 : 0);
				case ValueType.Date:
					DateTime.TryParse(left.val, out tleft);
					DateTime.TryParse(right.val, out tright);
					return dleft > dright;
				default: return (left.val ?? "").CompareTo(right.val ?? "") > 0;
			}
		}

		public static bool operator <(Dynamic left, Dynamic right)
		{
			if (left.type != right.type)
				return false;
			if (string.IsNullOrEmpty(right.val))
				return false;

			int ileft = int.MinValue, iright = int.MinValue;
			double dleft = double.MinValue, dright = double.MinValue;
			bool bleft = false, bright = false;
			DateTime tleft = DateTime.MinValue, tright = DateTime.MinValue;

			switch (left.type)
			{
				case ValueType.Int:
					int.TryParse(left.val, out ileft);
					int.TryParse(right.val, out iright);
					return ileft < iright;
				case ValueType.Number:
					double.TryParse(left.val, out dleft);
					double.TryParse(right.val, out dright);
					return dleft < dright;
				case ValueType.Bool:
					bool.TryParse(left.val, out bleft);
					bool.TryParse(right.val, out bright);
					return (bleft ? 1 : 0) < (bright ? 1 : 0);
				case ValueType.Date:
					DateTime.TryParse(left.val, out tleft);
					DateTime.TryParse(right.val, out tright);
					return dleft < dright;
				default: return (left.val ?? "").CompareTo(right.val ?? "") < 0;
			}
		}

		public static bool operator ==(Dynamic left, Dynamic right)
		{
			if (left?.type != right?.type)
				return false;
			if (string.IsNullOrEmpty(left.val))
				return false;

			int ileft = int.MinValue, iright = int.MaxValue;
			double dleft = double.MinValue, dright = double.MaxValue;
			bool bleft = false, bright = true;
			DateTime tleft = DateTime.MinValue, tright = DateTime.MaxValue;

			switch (left?.type)
			{
				case ValueType.Int:
					int.TryParse(left.val, out ileft);
					int.TryParse(right.val, out iright);
					return ileft == iright;
				case ValueType.Number:
					double.TryParse(left.val, out dleft);
					double.TryParse(right.val, out dright);
					return dleft == dright;
				case ValueType.Bool:
					bool.TryParse(left.val, out bleft);
					bool.TryParse(right.val, out bright);
					return (bleft ? 1 : 0) == (bright ? 1 : 0);
				case ValueType.Date:
					DateTime.TryParse(left.val, out tleft);
					DateTime.TryParse(right.val, out tright);
					return dleft == dright;
				default: return (left?.val ?? "").CompareTo(right?.val ?? "") == 0;
			}
		}

		public static bool operator !=(Dynamic left, Dynamic right)
		{
			if (left?.type != right?.type)
				return true;
			if (string.IsNullOrEmpty(left?.val))
				return false;

			int ileft = int.MinValue, iright = int.MinValue;
			double dleft = double.MinValue, dright = double.MinValue;
			bool bleft = false, bright = false;
			DateTime tleft = DateTime.MinValue, tright = DateTime.MinValue;

			switch (left?.type)
			{
				case ValueType.Int:
					int.TryParse(left.val, out ileft);
					int.TryParse(right.val, out iright);
					return ileft != iright;
				case ValueType.Number:
					double.TryParse(left.val, out dleft);
					double.TryParse(right.val, out dright);
					return dleft != dright;
				case ValueType.Bool:
					bool.TryParse(left.val, out bleft);
					bool.TryParse(right.val, out bright);
					return (bleft ? 1 : 0) != (bright ? 1 : 0);
				case ValueType.Date:
					DateTime.TryParse(left.val, out tleft);
					DateTime.TryParse(right.val, out tright);
					return dleft != dright;
				default: return (left.val ?? "").CompareTo(right.val ?? "") != 0;
			}
		}

		public override bool Equals(object obj)
		{
			return obj is Dynamic dynamic &&
						 type == dynamic.type &&
						 val == dynamic.val;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(type, val);
		}
	}

}

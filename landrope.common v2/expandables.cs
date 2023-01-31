using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynForm.shared;

namespace landrope.common
{
	public class cmnBase
	{
		public Dictionary<string, option[]> extras { get; set; }
	}


	public class Expandable
	{
		static bool IsValueType(Type type) =>
			typeof(ValueType).IsAssignableFrom(type) || type == typeof(string) ||
					(type.Name.StartsWith("Nullable") && type.GenericTypeArguments.Length > 0);

		public static void TryExpand<T>(T val) where T : class// IExpandable
		{
			if (val == null)
				return;
			var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
									.Where(p => !IsValueType(p.PropertyType)).ToList();
			props.ForEach(p => TryExpand(p, val));
		}

		public static void TryExpand<T>(PropertyInfo prop, T obj) where T : class//  IExpandable
		{
			if (prop == null || prop.Name == "extras")
				return;
			var type = prop.PropertyType;
			if (IsValueType(type))
				return;
			try
			{
				var val = prop.GetValue(obj);
				if (val == null)
				{
					val = Activator.CreateInstance(type);
					prop.SetValue(obj, val);
				}
				var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
									.Where(p => !IsValueType(p.PropertyType)).ToList();
				props.ForEach(p => TryExpand(p, val));
			}
			catch (Exception) { }
		}

		public static void TryReduce<T>(T obj) where T : class // IExpandable
		{
			Type type = typeof(T);
			var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
			props.ForEach(p => TryReduce(p, obj));
		}

		static bool IsDefault<T>(T value)
		{
			var type = typeof(T);
			var tname = type.Name;
			switch (tname)
			{
				case nameof(String): return string.IsNullOrWhiteSpace(value.ToString());
				case nameof(Int16):
				case nameof(Int32):
				case nameof(Int64):
					return value.Equals(0);
			}
			return Object.Equals(value, null);
		}

		public static bool TryReduce<T>(PropertyInfo prop, T obj) where T : class //IExpandable
		{
			Type type = prop.PropertyType;
			var val = prop.GetValue(obj);
			if (IsValueType(type))
				return IsDefault(val);

			if (val == null)
				return true;

			var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
			var equs = props.TrueForAll(p => TryReduce(p, p.GetValue(val)));
			if (equs)
				prop.SetValue(obj, null);
			return equs;
		}
	}
}

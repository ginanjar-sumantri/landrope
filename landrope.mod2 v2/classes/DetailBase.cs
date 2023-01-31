using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace landrope.mod2
{
	public class DetailBase
	{
		public static (bool vt, bool nullable, bool array) GetTypeKind(Type type)
		{
			var arr = typeof(IList).IsAssignableFrom(type);
			var nb = !arr && typeof(Nullable).IsAssignableFrom(type) && type.GenericTypeArguments.Any();
			if (nb)
				type = type.GenericTypeArguments[0];
			nb = nb || type == typeof(string);
			var vt = typeof(ValueType).IsAssignableFrom(type) || type == typeof(string);
			return (vt, nb, arr);
		}

		public static object CreateInstance(Type type)
		{
			object[] parameters = typeof(Array).IsAssignableFrom(type) ? new object[] { 0 } : null;
			return Activator.CreateInstance(type, parameters);
		}

		public static Type BaseType = typeof(DetailBase);

		public void UpdateEntries(DetailBase src)
		{
			var type = GetType();
			if (type != src.GetType())
				return;

			var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var prop in props)
			{
				var srcobj = prop.GetValue(src);
				var dstobj = prop.GetValue(this);

				var ptype = prop.PropertyType;
				var vtn = GetTypeKind(ptype);

				if (vtn.array)
				{
					if (srcobj != null && ((IList)srcobj).Count != 0)
					{
						if (dstobj == null || ((IList)dstobj).Count == 0)
							prop.SetValue(this, srcobj);
					}
				}
				else if (vtn.vt)
				{
					if (Object.Equals(dstobj, srcobj))
						continue;
					if (vtn.nullable)
					{
						if (srcobj != null && dstobj == null)
							prop.SetValue(this, srcobj);
						continue;
					}
					var def = Activator.CreateInstance(ptype);
					if (object.Equals(dstobj, def) && !object.Equals(srcobj, def))
						prop.SetValue(this, srcobj);
				}
				else if (BaseType.IsAssignableFrom(ptype) && srcobj != null)
				{
					if (dstobj == null)
						prop.SetValue(this, srcobj);
					else
						((DetailBase)dstobj).UpdateEntries((DetailBase)srcobj);
				}
			}
		}

		public List<string> FindChanges(DetailBase reff)
		{
			var res = new List<string>();

			var type = GetType();
			if (type != reff.GetType())
				return res;

			var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var prop in props)
			{
				var srcobj = prop.GetValue(reff);
				var dstobj = prop.GetValue(this);

				var ptype = prop.PropertyType;
				var vtn = GetTypeKind(ptype);
				if (vtn.array)
				{
					if (dstobj == null && srcobj == null)
						continue;
					if (dstobj == null || srcobj == null || ((IList)dstobj).Count!=((IList)srcobj).Count)
						res.Add(prop.Name);
				}
				if (vtn.vt)
				{
					if (!Object.Equals(dstobj, srcobj))
						res.Add(prop.Name);
				}
				else if (BaseType.IsAssignableFrom(ptype))
				{
					if (srcobj == null && dstobj == null)
						continue;
					if (dstobj == null || srcobj==null)
					{
						res.Add(prop.Name);
						continue;
					}
					var subres = ((DetailBase)dstobj).FindChanges((DetailBase)srcobj);
					res.AddRange(subres.Select(s => $"{prop.Name}.{s}"));
				}
			}
			return res;
		}
	}
}

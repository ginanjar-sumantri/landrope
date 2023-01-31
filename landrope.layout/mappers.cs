using landrope.mod;
using landrope.mod2;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DynForm.shared;

namespace landrope.layout
{
	public static class DynMapper
	{
		public static DynValue FromElement(DynElement elem, object data, object prevdata, LandropeContext context, ExtLandropeContext contextex)
		{
			var res = new DynValue();
			res.Group = elem.group;
			res.Label = elem.label;
			if (elem.value?.StartsWith("#") ?? false)
			{
				if (!elem.shown)
					return null;
				res.Path = elem.value.Substring(1);

				var propname = res.Path;

				var newprops = MapperHelpers.GetPropInfo(data, propname, true);
				var oldprops = MapperHelpers.GetPropInfo(prevdata, propname, true);


				object newvalue = null;
				if (newprops.props != null && newprops.props.Any())
				{
					newvalue = MapperHelpers.GetValue(newprops.props, data);
					if (newvalue != null)
					{
						if (newprops.findkey != null)
							res.Value = MapperHelpers.GetIdentity(context, contextex, newvalue.ToString(), newprops.findkey);
						else
						{
							var state = MapperHelpers.CheckType(newvalue);
							if (state.integer == null)
								res.Value = null;
							else if (state.integer == true)
								res.Value = $"!!>>{newvalue}";
							else if (state.floating == true)
								res.Value = $"!!>>{newvalue:#,##0}";
							else if (state.datetime == true)
							{
								if (((DateTime)newvalue).TimeOfDay == TimeSpan.Zero)
									res.Value = $"{newvalue:dd MMMM yyyy}";
								else
									res.Value = $"{newvalue:dd MMMM yyyy, HH:mm:ss}";
							}
							else // string or boolean
								res.Value = newvalue.ToString();
						}
					}
				}

				string oldvalue = null;
				if (oldprops.props != null && oldprops.props.Any())
					oldvalue = MapperHelpers.GetValue(oldprops.props, prevdata)?.ToString();
				res.Checking = string.IsNullOrWhiteSpace(res.Value) || !object.Equals(newvalue,oldvalue);
				res.Accepted = !res.Checking;
			}
			return res;
		}
	}

	public static class MapperHelpers
	{
		public static string GetIdentity(LandropeContext context, ExtLandropeContext contextExt,
														string key, string keyword)
		{
			switch (keyword)
			{
				case "keyProject": return GetProjectId(context, key);
				case "keyDesa": return GetDesaId(context, key);
				case "keyNotaris": return GetNotarisId(contextExt, key);
				case "keyPenampung": 
				case "keyPTSK": 
				case "keyCompany": return GetCompanyId(contextExt, key);
				case "keyUser":
				case "keyCreator":
				case "keyEntering": return GetUserId(contextExt, key);
			}
			return null;
		}

		public static string GetNotarisId(ExtLandropeContext contextExt, string key)
		{
			var notaris = contextExt.notarists.FirstOrDefault(c => c.key == key);
			return notaris?.identifier ?? key;
		}

		public static string GetCompanyId(ExtLandropeContext contextExt, string key)
		{
			var company = contextExt.companies.FirstOrDefault(c => c.key == key);
			return company?.identifier ?? key;
		}

		public static string GetUserId(ExtLandropeContext contextExt, string key)
		{
			var user = contextExt.users.FirstOrDefault(c => c.key == key);
			return user?.identifier ?? key;
		}

		public static string GetProjectId(LandropeContext context, string key)
		{
			Project p = context.GetCollections(new Project(), "maps", $"{{key:'{key}'}}").FirstOrDefault();
			return p?.identity ?? key;
		}

		public static string GetDesaId(LandropeContext context, string key)
		{
			Project p = context.GetCollections(new Project(), "maps", $"{{'villages.key':'{key}'}}").FirstOrDefault();
			if (p == null)
				return key;
			Village vil = p.villages.FirstOrDefault(v => v.key==key);
			return vil?.identity ?? key;
		}

		static string[] findkeys = new[]
		{
			"keyProject",
			"keyDesa",
			"keyNotaris",
			"keyPenampung",
			"keyPTSK",
			"keyCompany",
			"keyUser",
			"keyCreator",
			"keyEntering"
		};

		public static (bool? integer, bool? floating, bool? datetime, bool? boolean) CheckType(object value)
		{
			if (value == null)
				return (null,null,null,null);
			//var type = value.GetType();
			//if (typeof(Nullable).IsAssignableFrom(type) && type.GenericTypeArguments.Any())
			//	type = type.GenericTypeArguments[0];

			//return ((value is int || value is byte || value is short || value is long),
			//				(value is double || value is Single || value is decimal),(value is DateTime),(value is bool));
			switch(value)
			{
				case Byte bval: case Int16 sval: case Int32 val: case Int64 lval:
				case UInt16 usval: case UInt32 uval: case UInt64 ulval: return (true, false, false, false);
				case Single sval: case double val: return (false, true, false, false);
				case bool val: return (false, false, true, false);
				case DateTime val: return (false, false, false, true);
			}
			return (false, false, false, false);
		}

		public static (PropertyInfo[] props, string findkey) GetPropInfo(object data, string propname, bool create = true)
		{
			List<PropertyInfo> props = new List<PropertyInfo>();
			try
			{
				string[] propnames = propname.Split('.');
				if (propname.Length == 0 || data == null)
					return (null, null);
				string findkey = null;
				object lastobj = data;
				var objtype = lastobj.GetType();
				PropertyInfo prop = null;
				foreach (string pname in propnames)
				{
					if (lastobj == null)
						return (null, null);
					string name = pname;
					objtype = lastobj.GetType();
					try
					{
						prop = objtype.GetProperty(name);
					}
					catch (Exception exx)
					{

					}
					if (prop == null)
						return (null, null);
					props.Add(prop);
					var obj = prop.GetValue(lastobj);
					///}
					objtype = prop?.PropertyType ?? typeof(int);
					//Console.WriteLine($"objtype={objtype.Name}");
					if (obj == null && !(typeof(Nullable).IsAssignableFrom(objtype) || typeof(ValueType).IsAssignableFrom(objtype) || objtype == typeof(string)))
					{
						object[] parms = typeof(Array).IsAssignableFrom(objtype) ? new object[] { (object)0 } : null;
						var nobj = Activator.CreateInstance(objtype, parms);
						if (create)
							prop.SetValue(lastobj, nobj); // adds new object to current object
						obj = nobj;
					}
					lastobj = obj;
				}
				if (props.Any() && findkeys.Contains(props.Last().Name))
					findkey = props.Last().Name;
				//Console.WriteLine($"Before return... prop={prop?.Name ?? "null"},lastobj={lastobj?.GetType()?.Name ?? "null"}");
				return (props.ToArray(), findkey);
			}
			catch (Exception ex)
			{
				return (null, null);
			}
		}

		public static object GetValue(PropertyInfo[] props, object data)
		{
			object val = data;
			foreach (PropertyInfo prop in props)
			{
				if (val == null)
					return null;
				val = prop.GetValue(val);
			}
			return val;
		}
	}
}

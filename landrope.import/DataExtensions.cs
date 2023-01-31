using landrope.mod;
using landrope.mod2;
using Microsoft.AspNetCore.WebUtilities;
//using Microsoft.Extensions.Logging.Internal;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace landrope.import
{
	public static class DataExtensions
	{
		public static (PropertyInfo[] props, string findkey) GetPropInfo(this ValidatableItem data, string propname, bool create)
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
					//if (objtype.Name.StartsWith("Dictionary"))
					//{
					//	if (((Dictionary<string, option[]>)obj).TryGetValue(name, out option[] value))
					//		obj = new { list = value };
					//	prop = obj.GetType().GetProperty("list");
					//	Console.WriteLine($"prop={prop?.Name ?? "null"}");
					//	lastobj = obj;
					//}
					//else
					//{
					string name = pname;
					if (pname.StartsWith("#"))
					{
						name = pname.Substring(1);
						findkey = name;
					}
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
				//Console.WriteLine($"Before return... prop={prop?.Name ?? "null"},lastobj={lastobj?.GetType()?.Name ?? "null"}");
				return (props.ToArray(), findkey);
			}
			catch (Exception ex)
			{
				return (null, null);
			}
		}

		public static List<Dictionary<string, ValidatableItem>> tempos = new List<Dictionary<string, ValidatableItem>>();

		public static Dictionary<string, ValidatableItem> AddDict(Type ptype)
		{
			var dict = MakeDict(ptype);
			tempos.Add(dict);
			return dict;
		}

		public static Dictionary<string, ValidatableItem> MakeDict(Type ptype)
		{
			var except = new[] { "_id", "key", "created" };
			var props = ptype.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList()
										.Where(p => !except.Contains(p.Name)).ToList();
			var dict = new Dictionary<string, ValidatableItem>();
			props.ForEach(prop =>
			{
				var pptype = prop.PropertyType;
				if (typeof(ValidatableShell).IsAssignableFrom(pptype) && pptype.GenericTypeArguments.Any())
				{
					pptype = pptype.GenericTypeArguments[0];
					if (typeof(ValidatableItem).IsAssignableFrom(pptype))
					{
						var newobj = Activator.CreateInstance(pptype);
						dict.Add(prop.Name, (ValidatableItem)newobj);
					}
				}
			});
			return dict;
		}

		public static List<string> foundkeys = new List<string>();

		static (bool found, IEnumerable<Persil> results) FindByProp(IEnumerable<Persil> list, PersilBasic newitem, params string[] propnames)
		{
			var objtype = typeof(PersilBasic);
			var props = objtype.GetProperties(BindingFlags.Public | BindingFlags.Instance)
										.Join(propnames, p => p.Name, n => n, (p, n) => p);
			var proplist = props.Select(p => (p, p.Name, val: p.GetValue(newitem))).Where(x => x.val != null).ToList();

			var entries = list.Select(l => (l, entry: l.basic.current)).Where(l => l.entry != null).ToList();
			var sublist = entries.Select(l => (l, co: proplist.Select(p => (lval: p.p.GetValue(l.entry), p.val)).ToArray())).ToList();
			var results = sublist.Where(x => x.co.All(xx => object.Equals(xx.lval, xx.val))).Select(x => x.l.l).ToList();

			var found = results.Any();
			if (found)
				return (found, results);

			entries = list.Select(l => (l, entry: l.basic.entries.FirstOrDefault()?.item)).Where(l => l.entry != null).ToList();
			sublist = entries.Select(l => (l, co: proplist.Select(p => (lval: p.p.GetValue(l.entry), p.val)).ToArray())).ToList();
			results = sublist.Where(x => x.co.All(xx => object.Equals(xx.lval, xx.val))).Select(x => x.l.l).ToList();
			found = results.Any();
			return (found, results);
		}

		public static bool SaveDict(Dictionary<string, ValidatableItem> dict, Persil persil, bool update, bool checkonly,
																string key, ExtLandropeContext contextex, int rownum)
		{
			if (update)
			{
				int notsingle = 0;
				PersilBasic basic = null;
				if (dict.TryGetValue("basic", out ValidatableItem val))
				{
					basic = (PersilBasic)val;
				}
				if (basic == null)
					throw new InvalidOperationException("No basic information found");

				var surat = basic.surat;
				var namasurat = surat?.nama;
				bool avoidSurat = surat == null;
				var type = typeof(PersilBasic);
				Persil fpersil = null;
				if (key != null)
				{
					fpersil = contextex.persils.FirstOrDefault(p => p.key == key);
					if (fpersil == null)
						InvalidKey(key, basic, namasurat);
					else
						persil = fpersil;
				}
				if (key == null || fpersil == null)
				{
					var persils = contextex.persils.Query(p => p.basic.current.keyProject == basic.keyProject && p.basic.current.keyDesa == basic.keyDesa);

					var found = FindByProp(persils, basic, nameof(PersilBasic.tahap), nameof(PersilBasic.noPeta));
					if (!found.found)
						return NotFound(0, basic, namasurat);

					if (found.results.Count() > 1)
					{
						NotSingle(notsingle, basic, namasurat);
						found = FindByProp(found.results, basic, nameof(PersilBasic.en_proses), nameof(PersilBasic.en_jenis));
						if (!found.found)
							return NotFound(1, basic, namasurat);
						if (found.results.Count() > 1)
						{
							NotSingle(++notsingle, basic, namasurat);
							found = FindByProp(found.results, basic, nameof(PersilBasic.group), nameof(PersilBasic.tahap),
																	nameof(PersilBasic.luasSurat), nameof(PersilBasic.luasDibayar)
																				, nameof(PersilBasic.satuan), nameof(PersilBasic.total));
							if (!found.found)
								return NotFound(2, basic, namasurat);

							if (found.results.Count() > 1)
							{
								NotSingle(++notsingle, basic, namasurat);
								if (avoidSurat)
									return false;
								var results = found.results.Where(p => p.basic.current.surat?.nama == namasurat);
								if (!results.Any())
									return NotFound(3, basic, namasurat);
								if (results.Count() > 1)
									return NotSingle(++notsingle, basic, namasurat);
								found = (true, results);
							}
						}
					}
					var expers = found.results.FirstOrDefault();
					if (expers == null || expers.GetType() != persil.GetType())
						return NotFound(4, basic, namasurat);
					key = expers.key;
					if (foundkeys.Contains(key))
						return Duplicate(expers.key, basic, namasurat);
					foundkeys.Add(key);
					persil = expers;
					if (notsingle > 0)
						Solved(notsingle, persil.key, basic, namasurat);
				}
			}


			var except = new[] { "_id", "key", "created", "invalid", "en_state", "statechanged", "statechanger", "notebatal" };
			var ptype = persil.GetType();
			var props = ptype.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList()
										.Where(p => !except.Contains(p.Name)).ToList();
			props.ForEach(prop =>
			{
				if (dict.ContainsKey(prop.Name))
				{
					try
					{
						var shell = prop.GetValue(persil);
						var item = dict[prop.Name];
						if (shell != null)
						{
							if (update)
							{
								if (!checkonly)
									PutAll(shell, item);
							}
							else
								PutCurent(shell, item);
						}
					}
					catch (Exception exx)
					{
						Console.WriteLine($"Error for 'prop.name' : {exx.Message}");
					}
				}
			});
			if (update)
			{
				if (!checkonly)
					contextex.persils.Update(persil);
			}
			else
				contextex.persils.Insert(persil);
			return true;

			bool NotFound(int level, PersilBasic basic, string namasurat)
			{
				Console.WriteLine($"\"not found({level})\",{rownum},\" \", \"{basic.proses}\",\"{basic.jenis}\",\"{basic.keyProject}\",\"{basic.keyDesa}\",\"{basic.tahap}\",\"{basic.noPeta}\",\"{basic.luasSurat}\",\"{basic.luasDibayar}\",\"{basic.satuan}\",\"{basic.total}\",\"{basic.group}\",\"{namasurat}\"");
				return false;
			}

			bool NotSingle(int level, PersilBasic basic, string namasurat)
			{
				Console.WriteLine($"\"not unique({level})\",{rownum},\" \", \"{basic.proses}\",\"{basic.jenis}\",\"{basic.keyProject}\",\"{basic.keyDesa}\",\"{basic.tahap}\",\"{basic.noPeta}\",\"{basic.luasSurat}\",\"{basic.luasDibayar}\",\"{basic.satuan}\",\"{basic.total}\",\"{basic.group}\",\"{namasurat}\"");
				return false;
			}

			bool Duplicate(string key, PersilBasic basic, string namasurat)
			{
				Console.WriteLine($"\"duplicate\",{rownum},\"{key}\", \"{basic.proses}\",\"{basic.jenis}\",\"{basic.keyProject}\",\"{basic.keyDesa}\",\"{basic.tahap}\",\"{basic.noPeta}\",\"{basic.luasSurat}\",\"{basic.luasDibayar}\",\"{basic.satuan}\",\"{basic.total}\",\"{basic.group}\",\"{namasurat}\"");
				return false;
			}

			void Solved(int level, string key, PersilBasic basic, string namasurat)
			{
				Console.WriteLine($"\"solved({level})\",{rownum},\"{key}\", \"{basic.proses}\",\"{basic.jenis}\",\"{basic.keyProject}\",\"{basic.keyDesa}\",\"{basic.tahap}\",\"{basic.noPeta}\",\"{basic.luasSurat}\",\"{basic.luasDibayar}\",\"{basic.satuan}\",\"{basic.total}\",\"{basic.group}\",\"{namasurat}\"");
			}
			void InvalidKey(string key, PersilBasic basic, string namasurat)
			{
				Console.WriteLine($"\"invalid key\",{rownum},\"{key}\", \"{basic.proses}\",\"{basic.jenis}\",\"{basic.keyProject}\",\"{basic.keyDesa}\",\"{basic.tahap}\",\"{basic.noPeta}\",\"{basic.luasSurat}\",\"{basic.luasDibayar}\",\"{basic.satuan}\",\"{basic.total}\",\"{basic.group}\",\"{namasurat}\"");
			}
		}

		static void PutCurent(object xshell, ValidatableItem item)
		{
			//var shelltype = shell.GetType();
			//var innertype = shelltype.GenericTypeArguments[0];
			switch (xshell)
			{
				case ValidatableShell<PersilBasic> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<GroupUTJ> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<GroupDP> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<GroupPelunasan> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<PembayaranPajak> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesPerjanjian> shell: PutCurrent2(shell, item); break;
				//case ValidatableShell<ProsesPerjanjianGirik> shell: PutCurrent2(shell, item); break;
				//case ValidatableShell<ProsesPerjanjianSertifikat> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesSPH> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesMohonSKKantah> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesMohonSKKanwil> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesPBT<NIB_PT>> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesPBT<NIB_Perorangan>> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesCetakBuku<HGB_Final>> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesTurunHak> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesNaikHak> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<ProsesBalikNama> shell: PutCurrent2(shell, item); break;
				case ValidatableShell<MasukAJB> shell: PutCurrent2(shell, item); break;
			}
			void PutCurrent2<T>(ValidatableShell<T> shell, ValidatableItem item) where T : ValidatableItem
			{
				ValidatableEntry<T> entry = new ValidatableEntry<T>
				{
					created = DateTime.Now,
					en_kind = ChangeKind.Add,
					reviewed = DateTime.Now,
					approved = true,
					keyCreator = "BCAB674C-45E4-492B-8EDE-791C872DCC15",
					keyReviewer = "BCAB674C-45E4-492B-8EDE-791C872DCC15",
					item = (T)item
				};
				shell.current = (T)item;
				shell.entries = new List<ValidatableEntry<T>> { entry };
			}
		}

		static void PutAll(object xshell, ValidatableItem item)
		{
			//var shelltype = shell.GetType();
			//var innertype = shelltype.GenericTypeArguments[0];
			switch (xshell)
			{
				case ValidatableShell<PersilBasic> shell: PutAll(shell, item); break;
				case ValidatableShell<GroupUTJ> shell: PutAll(shell, item); break;
				case ValidatableShell<GroupDP> shell: PutAll(shell, item); break;
				case ValidatableShell<GroupPelunasan> shell: PutAll(shell, item); break;
				case ValidatableShell<PembayaranPajak> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesPerjanjian> shell: PutAll(shell, item); break;
				//case ValidatableShell<ProsesPerjanjianGirik> shell: PutAll(shell, item); break;
				//case ValidatableShell<ProsesPerjanjianSertifikat> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesSPH> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesMohonSKKantah> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesMohonSKKanwil> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesPBT<NIB_PT>> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesPBT<NIB_Perorangan>> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesCetakBuku<HGB_Final>> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesTurunHak> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesNaikHak> shell: PutAll(shell, item); break;
				case ValidatableShell<ProsesBalikNama> shell: PutAll(shell, item); break;
				case ValidatableShell<MasukAJB> shell: PutAll(shell, item); break;
			}
			void PutAll<T>(ValidatableShell<T> shell, ValidatableItem item) where T : ValidatableItem
			{
				//var shell = (ValidatableShell<T>)obj;
				if (shell.entries != null && shell.entries.Any())
				{
					var entries = shell.entries.Where(e => e.item != null && e.en_kind != ChangeKind.Delete).ToList();
					entries.ForEach(e => e.item.UpdateEntries((T)item));
					if (shell.current != null)
						shell.current.UpdateEntries((T)item);
				}
				else
				{
					ValidatableEntry<T> entry = new ValidatableEntry<T>
					{
						created = DateTime.Now,
						en_kind = ChangeKind.Add,
						reviewed = DateTime.Now,
						approved = true,
						keyCreator = "BCAB674C-45E4-492B-8EDE-791C872DCC15",
						keyReviewer = "BCAB674C-45E4-492B-8EDE-791C872DCC15",
						item = (T)item
					};
					shell.entries = new List<ValidatableEntry<T>> { entry };
					shell.current = (T)item;
				}
			}
		}
	}
}

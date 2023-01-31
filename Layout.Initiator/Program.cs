using landrope.mod2;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualBasic.CompilerServices;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;
using landrope.common;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using System.Diagnostics;
using ExcelDataReader;
using System.Data;
using System.Text;
using System.Reflection.Metadata.Ecma335;

namespace Layout.Initiator
{
	class Program
	{
		static void Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			if (args.Length == 0)
			{
				Console.Write("Collecting cmnXXX classes to .csv files...");
				ClassesToExcel();
				Console.WriteLine("DONE");
			}
			else
			{
				Console.Write("Coverting .csv files to .dfl files...");
				var useddir = args[0];
				if (!Directory.Exists(useddir))
				{
					Console.WriteLine($"folder {useddir} diesn't exists... Aborted.");
					return;
				}
				Directory.SetCurrentDirectory(useddir);
				var dstdir = "jsons";
				var exists = Directory.Exists(dstdir);
				if (!exists)
					Directory.CreateDirectory(dstdir);
				Directory.SetCurrentDirectory(dstdir);
				if (exists)
					Directory.GetFiles(".", "*.*").ToList().ForEach(f => File.Delete(f));

				ExcelsToJsons();
			}
		}

		private static void ExcelsToJsons()
		{
			var csvs = Directory.GetFiles(@"..\", "*.csv").ToList();
			Stream fsin;
			IExcelDataReader xreader;
			Stream fsout;
			StreamWriter sw;
			foreach(var csv in csvs)
			{
				var fname = Path.GetFileNameWithoutExtension(csv);
				var dfl = fname + ".dfl";
				fsin = new FileStream(csv, FileMode.Open, FileAccess.Read, FileShare.Read);
				xreader = ExcelReaderFactory.CreateCsvReader(fsin);
				try
				{
					fsout = new FileStream(dfl, FileMode.Create, FileAccess.Write);
					sw = new StreamWriter(fsout);
					try
					{
						sw.AutoFlush = true;

						var res = xreader.AsDataSet();
						var dt = res.Tables.Cast<DataTable>().FirstOrDefault();
						if (dt==null)
						{
							Console.WriteLine($"Unable to open sheet from {csv}... Skipped");
							continue;
						}
						var rows = dt.Rows.Cast<DataRow>();
						var rcols = rows.FirstOrDefault(); // get first row
						if (rcols == null)
							throw new Exception("This worksheet contains no rows");
						var cols = rcols.ItemArray.Select(c => (c?.Equals(DBNull.Value) ?? true) ? null : c.ToString()).ToList();
						var idx = cols.FindIndex(c => c == "json");
						sw.WriteLine("[");
						int i = 0;
						foreach (var row in rows.Skip(1).ToArray())
						{
							if (i > 0)
								sw.WriteLine(",");
							i++;
							var obj = row[idx];
							if (DBNull.Value.Equals(obj))
								break;
							var json = obj.ToString();
							json = json.Replace("`options`:`", "`options`:`extras.").Replace("`options`:`extras.`", "`options`:``");
							json = json.Replace("`nullable`:`true`", "`nullable`:true").Replace("`", "\"").Replace("'", "\"");
							sw.Write(json);
						}
						sw.WriteLine("\n]");
					}
					finally
					{
						fsout.Close();
					}
				}
				finally
				{
					fsin.Close();
				}
			}
		}

		static void ClassesToExcel()
		{
			var dirname = $"layouts.{DateTime.Now:yyMM.ddHH.mmss}";
			Directory.CreateDirectory(dirname);
			Directory.SetCurrentDirectory(dirname);
			Console.WriteLine($"Root path : '{Directory.GetCurrentDirectory()}'");

			//var persiltypes = new[] { typeof(PersilGirik), typeof(PersilHGB), typeof(PersilSHM), typeof(PersilSHP), typeof(PersilHibah) };
			var mothertype = typeof(cmnBase);
			//var types = persiltypes.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			//												.Select(p=>p.PropertyType) 
			//												.Where(p => mothertype.IsAssignableFrom(p) && p.GenericTypeArguments.Any())
			//												.Select(g=>g.GenericTypeArguments[0])
			//												.Distinct().ToList();
			var types = Assembly.GetAssembly(mothertype).GetExportedTypes()
																.Where(p => mothertype.IsAssignableFrom(p))
																.Distinct().ToList();

			var rgx = new Regex("^cmn");

			FileStream fs;
			StreamWriter sw;
			Stack<string> groups = new Stack<string>();
			types.ForEach(t =>
			{
				var name = rgx.Replace(t.Name, "");
				var fname = $"{name}.csv";
				fs = new FileStream(fname, FileMode.Create, FileAccess.Write);
				sw = new StreamWriter(fs);
				try
				{
					sw.AutoFlush = true;
					sw.WriteLine("`visible`,`editable`,`group`,`label`,`xtype`,`value`,`options`,`cascade`,`dependency`,`swlabels`,`nullable`");
					var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
							.Where(p => p.Name != "extras" && !typeof(Enum).IsAssignableFrom(p.GetType())).ToList();
					groups.Clear();
					foreach (var prop in props)
					{
						var ptype = prop.PropertyType;
						if (!(typeof(ValueType).IsAssignableFrom(ptype) || ptype == typeof(string)))
							processSub(prop);
						else
							WriteProp(prop);
					}
				}
				finally
				{
					fs.Flush();
					fs.Close();
				}
			});

			void processSub(PropertyInfo pinfo)
			{
				var proptype = pinfo.PropertyType;
				groups.Push(pinfo.Name);
				try
				{
					var props = proptype.GetProperties(BindingFlags.Public | BindingFlags.Instance)
							.Where(p => !typeof(Enum).IsAssignableFrom(p.GetType())).ToList();
					foreach (var prop in props)
					{
						var ptype = prop.PropertyType;
						if (!(typeof(ValueType).IsAssignableFrom(ptype) || ptype == typeof(string)))
							processSub(prop);
						else
							WriteProp(prop);
					}
				}
				finally
				{
					groups.Pop();
				}
			}

			void WriteProp(PropertyInfo pinfo)
			{
				var proptype = pinfo.PropertyType;
				var lgroups = groups.ToList();
				lgroups.Reverse();
				var lgroup = string.Join("|", lgroups.Select(g => g.ToTitle()));
				lgroups.Add(pinfo.Name);
				var lvalue = string.Join(".", lgroups);
				string xtype = "Text";
				if (proptype == typeof(double) || proptype == typeof(double?))
					xtype = "Number";
				else if (proptype == typeof(int) || proptype == typeof(int?) || proptype == typeof(long) || proptype == typeof(long?))
					xtype = "Numeric";
				if (proptype == typeof(DateTime) || proptype == typeof(DateTime?))
					xtype = "Date";
				if (proptype == typeof(bool) || proptype == typeof(bool?))
					xtype = "Switch";
				var swlabels = xtype == "Switch" ? "Ya,Tidak" : "";
				sw.WriteLine($"`true`,`true`,{lgroup},{pinfo.Name.ToTitle()},`{xtype}`,`#{lvalue}`,``,``,``,`{swlabels}`,`true`");
			}
		}
	}
}

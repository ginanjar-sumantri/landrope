using System;
using landrope.mod3;
using landrope.mod2;
using System.Linq;
using MongoDB.Driver;
using landrope.documents;
using System.IO;

namespace bundlemaker
{
	class Program
	{
		static void Main(string[] args)
		{
			//string connst = "mongodb://sa:M0ng0DB4dmin@localhost:27017/admin?ssl=false";
			string connst = "mongodb://sa:M0ng0DB4dmin@10.10.1.80:27017/admin?ssl=false";
			//string connst = "mongodb://sa:M0ng0DB4dmin@10.10.1.27:27017/admin?ssl=false";
			string dbname = "landrope_dev";
			//string dbname = "landrope";
			var contextex = new ExtLandropeContext(connst, dbname);
			var contextplus = new LandropePlusContext(connst, dbname);
			LandropePlusContext.current = contextplus;

			var persils = contextex.persils.Query(p => p.en_state == null || p.en_state == 0 && p.invalid!=true);
			var bundles = contextplus.mainBundles.Query(p => p.invalid != true).ToList();
			/*			var doctypes = contextplus.stepdocs.Query(s=>s.invalid!=true)
										.SelectMany(s=>s.receive).Select(d=>(d.keyDocType,d.);*/
			var template = contextex.GetCollections(new MainBundle(), "bundles", "{key:'template'}", "{_id:0,_t:0}").FirstOrDefault();
			if (template == null)
			{
				Console.WriteLine("Invalid setting, no template bundle found. Terminated...");
				Environment.Exit(-1);
			}

			var fname = args[0];
			if (!File.Exists(fname))
			{
				Console.WriteLine($"File {fname} doesn't exists. Terminated...");
				Environment.Exit(-1);
			}

			var ids = File.ReadAllLines(fname);
			if (ids==null || ids.Length==0)
			{
				Console.WriteLine($"Invalid setting, no IdBidang found in file {fname}. Terminated...");
				Environment.Exit(-1);
			}

			var doctypes = contextplus.GetCollections(new DocType(), "jnsDok", "{}", "{_id:0,_t:0}").ToList();
			var inexists = persils.Where(p => !bundles.Select(b => b.key).Contains(p.key));
			foreach (var p in inexists)
			{
				Console.Write($"Creating bundle for key/IdBidang {p.key}/{p.IdBidang}...");
				try
				{
					var bundle = new MainBundle(contextplus, p.key);
					bundles.Add(bundle);
					//contextplus.bundles.Insert(bundle);
					//Console.WriteLine($"IdBidang={p.IdBidang}... Done");
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(ex.GetType().Name);
					Console.ForegroundColor = ConsoleColor.White;
				}
			};
			//contextplus.SaveChanges();

			var newkeys = inexists.Select(p => p.key).ToArray();
			Console.Write("Adds missing bundle doc types...");
			bundles.ForEach(b =>
			{
				Console.Write($"IdBidang={b.IdBidang}...");
				var existing = b.doclist.Select(dd => dd.keyDocType);
				var rems = doctypes.Where(d => !existing.Contains(d.key));
				var any = rems.Any();
				foreach (var dt in rems)
				{
					b.doclist.Add(new BundledDoc(dt.key));
				}
				if (newkeys.Contains(b.key))
					contextplus.mainBundles.Insert(b);
				else if (any)
					contextplus.mainBundles.Update(b);
				Console.WriteLine($"Done");
			});
			contextplus.SaveChanges();

			Console.WriteLine($"All Done");
		}
	}
}

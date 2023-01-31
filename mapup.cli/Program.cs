using CadLoader;
using landrope.mod;
using System;
using System.Configuration;
using System.Diagnostics;

namespace mapup.cli
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: mapupp.cli <path to list-filename>");
				return;
			}
			var fname = args[0];
			process(fname);
		}
		static void process(string listfname)
		{
			Console.WriteLine($"Batch processing map files based on \"{listfname}\"...");
			var context = CreateContext();
			var proc = new processor(context);
			string result = proc.BatchProcess(listfname);
			Console.WriteLine(result);
			Console.Write("Press any key to close...");

		}

		static LandropeContext CreateContext()
		{
			var url = ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString.Replace("+", "&");
			var database = ConfigurationManager.AppSettings["database"];
			return new LandropeContext(url, database);
		}

	}
}

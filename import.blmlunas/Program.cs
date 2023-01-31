namespace fimport.blmlunas
{
	using System;
	using System.IO;
	using System.Text;
	using McMaster.Extensions.CommandLineUtils;
	using landrope.mod2;
	using landrope.mod4;
	using System.Linq;
	using MongoDB.Driver;
	using MongoDB.Bson;

	public class Program
	{
		static void Main(string[] argv)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var app = new CommandLineApplication(/*throwOnUnexpectedArg: false */);
			var serverOption = app.Option<string>("-a|--address", "Destination server address", CommandOptionType.SingleValue).IsRequired();
			var dbOption = app.Option<string>("-d|--database", "Database name on server", CommandOptionType.SingleValue).IsRequired();
			var dirOption = app.Option<string>("-f|--folder", "Path to folder to import", CommandOptionType.SingleValue).IsRequired();
			var fileOption = app.Option<string>("-w|--workbook", "Path to excel file to import (optional)", CommandOptionType.SingleValue);

			app.HelpOption("-h|--help|-?");
			app.Execute(argv);
			var server = serverOption.Value();
			var dbname = dbOption.Value();
			var dirname = dirOption.Value();
			var filename = fileOption.Value();

			if (String.IsNullOrWhiteSpace(server) || String.IsNullOrWhiteSpace(dbname) || String.IsNullOrWhiteSpace(dirname))
			{
				Console.WriteLine("Required information (server, database, folder) not provided well... Aborted");
				Environment.Exit(-1);
			}

			if (String.IsNullOrWhiteSpace(filename))
				Console.WriteLine($"Importing data from {dirname}/*.xlsx to {server}/{dbname}... ");
			else
				Console.WriteLine($"Importing data from {dirname}/{filename} to {server}/{dbname}... ");

			try
			{
				var dbconn = $"mongodb://sa:M0ng0DB4dmin@{server}:27017/admin?ssl=false";
				var context = new LandropePayContext(dbconn, "admin");
				var all_dbnames = context.db.Client.ListDatabaseNames().ToList();

				if (!all_dbnames.Contains(dbname))
				{
					Console.WriteLine("Invalid database name provided... Aborted");
					Environment.Exit(-1);
				}
				if (!Directory.Exists(dirname))
				{
					Console.WriteLine($"Folder {dirname} not exists... Aborted");
					Environment.Exit(-1);
				}

				var path = "*.xlsx";
				if (!String.IsNullOrWhiteSpace(filename))
				{
					path = Path.Combine(dirname, filename);
					if (!File.Exists(path))
					{
						Console.WriteLine($"File {path} not exists... Aborted");
						Environment.Exit(-1);
					}
					path = filename;
				}

				context.ChangeDB(dbname);

				var filter = "{_t:'user', identifier:'importer'}";
				var project = "{_id:0,key:1}";

				var userkey = context.GetCollections(new { key = "" }, "securities", filter, project).ToList().FirstOrDefault()?.key;
				var lastdir = dirname.Split(Path.DirectorySeparatorChar).Last().ToLower();
				var stages = new[]{
					$"<$match:<$expr:<$and:[<$in:['$inactive',[null,false]]>, <$eq:[<$toLower:'$identity'>,'{lastdir}']>]>>>".ToJson(),
					"{$project:{_id:0,key:1}}"};
				// same projection key as abvoe
				//var project = new JsonProjectionDefinition<xobj>("{_id:0,key:1}")
				var projkey = context.GetDocuments(new { key = "" }, "maps", stages).ToList().FirstOrDefault()?.key;

				if (projkey == null)
				{
					Console.WriteLine($"Invalid project name: {lastdir}... Aborted");
					Environment.Exit(-1);
				}

				var fnames = Directory.EnumerateFiles(dirname, path).ToArray();
				if (!fnames.Any())
				{

					Console.WriteLine($"Nothing to import...");
					Environment.Exit(-1);
				}

				foreach (var name in fnames)
				{
					var file = new FileStream(name, FileMode.Open);
					Console.WriteLine($"Importing from {name}...");
					var fails = ExcelHandler.ImportExcel(context, file, name, projkey,userkey);
					if (fails != null && fails.Any())
					{
						Console.WriteLine("Done with notes:");
						Console.WriteLine(string.Join("\n", fails));
					}
					else
						Console.WriteLine("Done successfully");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR: {ex.Message}");
			}
		}
	}
}
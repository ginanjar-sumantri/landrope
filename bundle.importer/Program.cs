using bundle.import;
using landrope.mod2;
using landrope.mod3;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace bundle.importer
{
	class Program
	{
		static void Main(string[] args)
		{
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var app = new CommandLineApplication<Program>(throwOnUnexpectedArg: false);
      var inputFileOption = app.Option<string>("-i|--input", "Input file to imported", CommandOptionType.SingleValue)
          .IsRequired();
      var inputSheetOption = app.Option<string>("-s|--sheet", "Worksheet(s) name separated by commas", CommandOptionType.SingleValue)
        .IsRequired();
      var additionOption = app.Option<string>("-a|--addition", "Data addition instead of update", CommandOptionType.NoValue);
      var CreateBundleOption = app.Option<string>("-c|--creation", "Create bundles only", CommandOptionType.NoValue)
          .IsRequired();

      app.HelpOption("-h|--help|-?");
      app.Execute(args);

      var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

      IConfigurationRoot configuration = builder.Build();
      var context = new LandropePlusContext(configuration);

      if (CreateBundleOption.HasValue())
      {
        MakeBundles(context);
        return;
			}
      if (inputFileOption.HasValue())
      {
        var fname = inputFileOption.Value();
        var sheet = "*";
        if (inputSheetOption.HasValue())
          sheet = inputSheetOption.Value();
        ChangeKind kind = ChangeKind.Update;
        if (additionOption.HasValue())
          kind = ChangeKind.Add;

        var proc = new Processor(context, context.users.FirstOrDefault(u => u.identifier == "importer").key);
        Console.Write($"Unprocessing previous entries from file \"{fname}\" worksheet(s) \"{sheet}\"... ");
        proc.Unprocess(fname, sheet);
        Console.Write($"Processing content of file \"{fname}\" worksheet(s) \"{sheet}\"... ");
        proc.Process(fname, sheet, kind);
        Console.WriteLine("Done");
      }
    }
  
    static void MakeBundles(LandropePlusContext contextplus)
		{
      var persils = contextplus.GetCollections(new { key = "", IdBidang = "" }, "persils_v2", "{en_state:{$in:[null,0]}}", "{_id:0,key:1,IdBidang:1}").ToList();
      var bundles = contextplus.mainBundles.All().Select(b => new { b.key, b.IdBidang });
      persils = persils.Except(bundles).ToList();

      persils.ForEach(p => {
        Console.Write("Creating bundle...");
        try
        {
          var bundle = new MainBundle(contextplus, p.key,p.IdBidang);
          contextplus.bundles.Insert(bundle);
          Console.WriteLine($"IdBidang={p.IdBidang}... Done");
        }
        catch (Exception ex)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine(ex.GetType().Name);
          Console.ForegroundColor = ConsoleColor.White;
        }
      });
      contextplus.SaveChanges();
      Console.WriteLine($"All Done");

    }
  }
}

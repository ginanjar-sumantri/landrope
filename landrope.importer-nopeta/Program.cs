using landrope.mod;
using landrope.mod2;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace landrope.importer_nopeta
{
  public class Program
  {
    public static void Main(string[] args)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var app = new CommandLineApplication<Program>(throwOnUnexpectedArg: false);
      var inputFileOption = app.Option<string>("-i|--input", "Input file to imported", CommandOptionType.SingleValue)
          .IsRequired();
      var inputSheetOption = app.Option<string>("-s|--sheet", "Worksheet name", CommandOptionType.SingleValue)
        .IsRequired();
      var DatabaseOption = app.Option<string>("-d|--database", "Target Database Name", CommandOptionType.SingleValue);

      app.HelpOption("-h|--help|-?");
      app.Execute(args);

      var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

      IConfigurationRoot configuration = builder.Build();
      //var context = new LandropeContext(configuration);
      var contextExt = new ExtLandropeContext(configuration);

      if (inputFileOption.HasValue())
      {
        var fname = inputFileOption.Value();
        var sheet = "nopeta";
        if (inputSheetOption.HasValue())
          sheet = inputSheetOption.Value();
        var dbname = DatabaseOption.Value();

        var DB = string.IsNullOrEmpty(dbname) ? "(default)" : dbname;
        var against = $"against db {DB}";

        if (!string.IsNullOrEmpty(dbname))
        {
          //context.ChangeDB(dbname);
          contextExt.ChangeDB(dbname);
        }

        Console.Write($"Loading & reading content of file \"{fname}\" worksheet \"{sheet}\" {against}... ");
        var result = import.DataLoader.LoadNoPeta(contextExt, fname, sheet);
        Console.WriteLine(result ?? "Done");
      }
    }
  }
}

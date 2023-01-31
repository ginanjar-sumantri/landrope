using landrope.mod;
using landrope.mod2;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace landrope.importer
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
      var MarkerOption = app.Option<string>("-m|--marker", "Row Marker", CommandOptionType.SingleValue)
        .IsRequired();
      var DatabaseOption = app.Option<string>("-d|--database", "Target Database Name", CommandOptionType.SingleValue);
      var updateOption = app.Option<bool>("-u|--update", "Update Existing", CommandOptionType.NoValue);
      var CheckOnlyOption = app.Option<string>("-c|--check", "Check Only When Updating", CommandOptionType.NoValue);

      app.HelpOption("-h|--help|-?");
      app.Execute(args);

      var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

      IConfigurationRoot configuration = builder.Build();
      var context = new LandropeContext(configuration);
      var contextExt = new ExtLandropeContext(configuration);

      if (inputFileOption.HasValue())
      {
        var fname = inputFileOption.Value();
        var sheet = "FINAL TEMPLATE";
        if (inputSheetOption.HasValue())
          sheet = inputSheetOption.Value();
        var marker = 1d;
        if (MarkerOption.HasValue())
        {
          if (!double.TryParse(MarkerOption.Value(), out double mkr))
            throw new InvalidOperationException("Please give a valid number for Marker value");
          marker = mkr;
        }
        var dbname = DatabaseOption.Value();
        var update = updateOption.HasValue();
        var checkonly = update && CheckOnlyOption.HasValue();

        var DB = string.IsNullOrEmpty(dbname) ? "(default)" : dbname;
        var against = $"against db {DB}" + (checkonly ? "(testing)" : "");

        if (!string.IsNullOrEmpty(dbname))
        {
          context.ChangeDB(dbname);
          contextExt.ChangeDB(dbname);
        }

        Console.Write($"Loading & reading content of file \"{fname}\" worksheet \"{sheet}\" {against}... ");
        var result = import.DataLoader.Load(context, contextExt, fname, sheet, update, marker, checkonly);
        Console.WriteLine(result ?? "Done");
      }
    }
  }
}

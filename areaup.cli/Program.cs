using HttpAccessor;
using landrope.mod;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace areaup.cli
{
	class Program
	{
		static void Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var app = new CommandLineApplication<Program>(throwOnUnexpectedArg: false);
			var inputFileOption = app.Option<string>("-i|--input", "Input file to convert" , CommandOptionType.SingleValue)
					.IsRequired();
			var inputSheetOption = app.Option<string>("-s|--sheet", "Worksheet name", CommandOptionType.SingleValue)
				.IsRequired();

			app.HelpOption("-h|--help|-?");
			app.Execute(args);

			var builder = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			IConfigurationRoot configuration = builder.Build();
			var context = new LandropeContext(configuration);

			if (inputFileOption.HasValue())
			{
				var fname = inputFileOption.Value();
				var sheet = "overall";
				if (inputSheetOption.HasValue())
					sheet = inputSheetOption.Value();

				Console.WriteLine($"Loading & reading content of file \"{fname}\" worksheet \"{sheet}\"... ");
				var result = AreaLoader.Load2(context, fname, sheet);
				Console.WriteLine(result ?? "Done");
			}
		}
	}
}

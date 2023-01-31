using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mongospace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tracer;

namespace assigners
{
	public class Program
	{
		public static IHostBuilder svcHostBuilder = null;
		static string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ssl", "ecdsacred.p12");

		public static void Main(string[] args)
		{
			AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.EnableWindows81Http2", true);
			MyTracer.AddListener("logs", "assigner", "log");
			//MyTracer.AddListener("ASSIGNERS");

			MyTracer.JustLog("Main method called...");
			MyTracer.JustLog($"Cert path: {path}");
			/*			var app = new CommandLineApplication<Program>();
						var installOption = app.Option<string>("-i|--install", "Install as windows service", CommandOptionType.NoValue);
						var uninstallOption = app.Option<string>("-u|--uninstall", "Uninstall windows service", CommandOptionType.NoValue);

						app.HelpOption("-h|--help|-?");
						app.Execute(args);
			*/
			/*			if (installOption.HasValue())
						{
							MyTracer.JustLog("Installing...");
							Console.Write("Installing...");
							install();
							Console.WriteLine("Done");
							return;
						}
						if (uninstallOption.HasValue())
						{
							MyTracer.JustLog("Uninstalling...");
							Console.Write("Uninstalling...");
							uninstall();
							Console.WriteLine("Done");
							return;
						}
			*/
			try
			{
				MyTracer.JustLog("Just running...");
				var host = CreateHostBuilder(args);
				MyTracer.JustLog("host built...");

				//svcHostBuilder = SvcHost.CreateHostBuilder(args);
				//svcHost.Run();
				//var cert = Properties.Resources.localhost;
				host.Build().Run();
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
			}

			IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
					.UseWindowsService()
					.UseContentRoot(AppContext.BaseDirectory)
					.ConfigureServices((hostContext, services) =>
					{
						services.AddHostedService<Worker>();
					})
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseUrls("https://*:17882");
					webBuilder.ConfigureKestrel(o =>
					{
						o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
						o.ConfigureHttpsDefaults(oo =>
						oo.ServerCertificate = new X509Certificate2(path, "987654")
						//oo.ServerCertificate = new X509Certificate2(Properties.Resources.ecdsacred, "987654")
						//oo.ClientCertificateMode = ClientCertificateMode.AllowCertificate
						//oo.AllowAnyClientCertificate()
						);
					});
					//webBuilder.UseKestrel(ko => ko.ListenLocalhost(7880, klo => klo.UseHttps()));

					webBuilder.UseStartup<Startup>();
				});
		}
	}
}

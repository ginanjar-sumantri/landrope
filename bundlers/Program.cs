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

namespace bundlers
{
	public class Program
	{
		public static IHostBuilder svcHostBuilder = null;
		static string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ssl", "ecdsacred.p12");

		public static void Main(string[] args)
		{
			AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.EnableWindows81Http2", true);
			MyTracer.AddListener("logs", "bundler", "log");
			//MyTracer.AddListener("BUNDLERS");

			MyTracer.JustLog("Main method called...");
			MyTracer.JustLog($"Cert path: {path}");
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
					webBuilder.UseUrls("https://*:17881");
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

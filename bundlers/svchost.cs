using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracer;

namespace bundlers
{
	public class SvcHost : ISvcHost
	{
		public static string _ServiceName = "BundlerGrpc";
		public static string _ServiceDesc = "Bundler gRPC";

		bool started = false;
		IHost app = null;
		CancellationTokenSource cancelor = null;

		public string ServiceName => SvcHost._ServiceName;

		public string ServiceDesc => SvcHost._ServiceDesc;

		public static IHostBuilder CreateHostBuilder(params string[] args) =>
				Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.ConfigureKestrel(o =>
					{
						o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
						o.ConfigureHttpsDefaults(oo =>
							//oo.ClientCertificateMode = ClientCertificateMode.AllowCertificate
							oo.AllowAnyClientCertificate()
						);
					});
					webBuilder.UseKestrel(ko => ko.ListenLocalhost(7880, klo => klo.UseHttps()));
					webBuilder.UseStartup<Startup>();
				});

		CancellationTokenSource stopSource = null;
		//CancellationToken StopToken;
		public void Start()
		{
			/*			if (started && app!=null)
							return;*/
			MyTracer.JustLog("Entering Start...");
			MyTracer.JustLog($"Web Host is null? {Program.svcHostBuilder == null}");
			if (Program.svcHostBuilder == null)
				throw new InvalidOperationException("Web Host not built");
			//if (app != null)
			//	return;
			//var xapp = CreateHostBuilder().Build();
			//var src = new CancellationTokenSource();
			try
			{
				app = Program.svcHostBuilder.Build();
				app.Start();
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
			}
			/*			.ContinueWith(t =>
						{
							cancelor = src;
							app = xapp; 
							started = true;
							MyTracer.JustLog("Started...");
						});
			*/      //var source = new CancellationTokenSource();
							//var token = source.Token;
							//xapp.RunAsync(token).ContinueWith(t=> 
							//{
							//	Console.WriteLine("Run Async has done...");
							//	app = xapp; stopSource = source; 
							//});
		}

		public void Stop()
		{
			/*			if (!started || app == null)// || stopSource==null)
							return;
			*/
			app?.StopAsync().Wait();
			//started = false;

			//stopSource.Cancel();
			//app.WaitForShutdownAsync().ContinueWith(t=> { app = null; stopSource = null; });
		}
	}
}

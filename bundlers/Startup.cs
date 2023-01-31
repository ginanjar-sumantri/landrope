using auth.mod;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using mongospace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tracer;
using landrope.mod3;
using landrope.engines;
using bundle.host;
using AssignerConsumer;
using landrope.consumers;
using GraphConsumer;

//using Microsoft.AspNetCore.Authentication.Certificate;

namespace bundlers
{
	public class Startup
	{

		public Startup(IConfiguration configuration, IWebHostEnvironment env)
		{
			MyTracer.TraceInfo2($"Calling assembly path={Assembly.GetCallingAssembly().Location}");
			MyTracer.TraceInfo2($"Executing assembly path={Assembly.GetExecutingAssembly().Location}");
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var cb = new ConfigurationBuilder();
			Configuration = cb.AddConfiguration(configuration)
					.SetBasePath(path)
					.AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
					.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
					.Build();
			//Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			ConfigServices(services);
		}
		public static void ConfigServices(IServiceCollection services)
		{
			MyTracer.TraceInfo2("Enter...");
			//services.AddSingleton<ILoggerProvider, DebugLoggerProvider>();
			//services.AddSingleton<ILoggerFactory, LoggerFactory>();
			//services.AddSingleton<ILogger<Worker<SvcHost>>, Logger<Worker<SvcHost>>>();

			MyTracer.TraceInfo2("Adding Services...");
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddSingleton<landrope.mod3.LandropePlusContext>();
			services.AddSingleton<BundleHost>();
			services.AddSingleton<IGraphHostConsumer, GraphHostConsumer>();
			services.AddSingleton<IAssignerHostConsumer, AssignerHostConsumer>();

			services.AddGrpc(o =>
			{
				o.MaxReceiveMessageSize = null;
				o.CompressionProviders = new List<Grpc.Net.Compression.ICompressionProvider>()
				{
				};
			});
			MyTracer.TraceInfo2("Exit...");
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			ConfigApp(app, env, Configuration);
		}
		public static void ConfigApp(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration Configuration)
		{
			MyTracer.TraceInfo2("Enter...");
			//app.UseAuthentication();

			MyTracer.TraceInfo2("Configure ContextService...");
			ContextService.Configure(app.ApplicationServices);
			MyTracer.TraceInfo2("Configure Config...");

			HttpAccessor.Config.Configure(Configuration, (IHostEnvironment)env);
			MyTracer.TraceInfo2("Configure Helper...");
			HttpAccessor.Helper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<BundlerService>();

				endpoints.MapGet("/", async context =>
							{
								await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
							});
			});

			app.ApplicationServices.GetService<BundleHost>();
			MyTracer.TraceInfo2("Exit...");
		}
	}
}

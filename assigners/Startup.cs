using auth.mod;
using GenWorkflow;
using graph.mod;
using GraphConsumer;
using GraphHost;
using landrope.consumers;
using landrope.engines;
using landrope.hosts.old;
using landrope.mod2;
using landrope.mod3;
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

//using Microsoft.AspNetCore.Authentication.Certificate;

namespace assigners
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
			services.AddSingleton<authEntities>();
			services.AddSingleton<ExtLandropeContext>();
			services.AddSingleton<LandropePlusContext>();
			services.AddSingleton<IAssignmentHost, AssignmentHost>();
			services.AddSingleton<IGraphHostConsumer, GraphHostConsumer>();

			services.AddGrpc(o =>
			{
				o.MaxReceiveMessageSize = null;
				o.CompressionProviders = new List<Grpc.Net.Compression.ICompressionProvider>()
				{
					/*					new Grpc.Net.Compression.GzipCompressionProvider(System.IO.Compression.CompressionLevel.Fastest)*/
				};
			});
			MyTracer.TraceInfo2("Exit...");
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
				endpoints.MapGrpcService<AssignerService>();

				endpoints.MapGet("/", async context =>
							{
								await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
							});
			});

			//app.ApplicationServices.GetService<GraphHostSvc>();
			var svc = app.ApplicationServices.GetService<IAssignmentHost>();
			MyTracer.TraceInfo2("Exit...");
		}
	}
}

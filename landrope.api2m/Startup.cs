using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tracer;
using landrope.mod;
using mongospace;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Primitives;
using landrope.mod3;
using landrope.mod2;
using auth.mod;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.IO;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using landrope.mod.shared;
using System.Diagnostics;
using landrope.material;
using graph.mod;
//using GraphHost;
//using GenWorkflow;
using landrope.hosts.old;
using FileRepos;
using GraphConsumer;
using AssignerConsumer;
using landrope.consumers;
using landrope.budgets;
using BundlerConsumer;

namespace landrope.api2
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			//services.AddSingleton<IConfiguration, ConfigurationRoot>();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			//services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddHttpContextAccessor();
			//services.AddSingleton<IConfiguration>(Configuration);

			services.AddScoped<IMongoEntities, authEntities>();
			services.AddScoped<authEntities>();
			services.AddScoped<LandropeContext>();
			services.AddScoped<ExtLandropeContext>();
			services.AddScoped<mod3.LandropePlusContext>();
			services.AddScoped<FileGrid>();
			services.AddScoped<GraphContext>();

			services.AddSingleton<ILandropeMaterializer, LandropeMaterializer>();
			services.AddScoped<PersilMaterializerAttribute>();
			services.AddScoped<MapMaterializerAttribute>();
			services.AddScoped<PersilCSMaterializerAttribute>();

			//services.RegisterGraphhHost();
			//services.RegisterAssignmentHost();
			//services.RegisterBundleHost();
#if !_NO_GRPCS_
			services.AddSingleton<IAssignerHostConsumer, AssignerHostConsumer>();
			services.AddSingleton<IBundlerHostConsumer, BundlerHostConsumer>();
			services.AddSingleton<IGraphHostConsumer, GraphHostConsumer>();
#endif

			services.RegisterTrxBatchHost();
			services.AddCors(c =>
			{
				c.AddDefaultPolicy(cpb => cpb.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
				c.AddPolicy(nameof(landrope),
					cpb => cpb.AllowAnyOrigin()
					.AllowAnyMethod()
					.AllowAnyHeader());
			});

			var xmlFile = Path.ChangeExtension(this.GetType().Assembly.Location, ".xml");
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc($"V2", new OpenApiInfo
				{
					Title = "landrope",
					Version = "2",
					// You can also set Description, Contact, License, TOS...
				});
				c.IncludeXmlComments(xmlFile);
			});
			services.AddSingleton<Budgetter>();
			services.AddControllers();
			//services.AddRazorPages();
			//services.AddControllersWithViews();
			//services.AddMvcCore(m=>m.EnableEndpointRouting=false);
		}


		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			string filepath = Path.Combine(env.ContentRootPath, "logs");
			MyTracer.SetListener(filepath, "landrope.api2", "log");
			MyTracer.TraceInfo2("Try to open log file...");
			Program.OpenLogFile(Path.Combine(filepath, $"log-{DateTime.Now:yyyyMMddHHmmss}.txt"));

			ContextService.Configure(app.ApplicationServices);
			HttpAccessor.Config.Configure(Configuration, (IHostEnvironment)env);
			HttpAccessor.Helper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
			//app.UseHttpsRedirection();

			//var midware = Configuration.GetValue<string>("midware");
			//if (midware?.ToLower() == "true")
			//{
			//	app.Use(async (context, next) =>
			//	{
			//		var req = context.Request;
			//		Program.Log($"scheme:{req.Scheme}, method:{req.Method}, path:{req.Path}");

			//		// Call the next delegate/middleware in the pipeline

			//		try
			//		{
			//			await next();

			//			var resp = context.Response;
			//			var hdrs = resp.Headers.ToDictionary(h => h.Key, h => h.Value);
			//			var headers = System.Text.Json.JsonSerializer.Serialize(hdrs);
			//			Program.Log($"status:{resp.StatusCode}, headers:{headers}, ctn-type:{resp.ContentType}");
			//		}
			//		catch (Exception ex)
			//		{
			//			Program.Log($"Middleware error: {ex.AllMessages()}");
			//		}
			//	});
			//}

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseCors(nameof(landrope));
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				//endpoints.MapRazorPages();
			});

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint($"/swagger/V2/swagger.json", "landrope");
			});

			//app.ApplicationServices.GetGraphHost(); // initialize Graph Host
			//app.ApplicationServices.GetAssignmentHost(); // initialize Assignment Host
			//app.ApplicationServices.GetBundleHost(); // initialize Bundle Host

#if !_NO_MATERIALIZE_
			var context = new LandropeContext((IConfigurationRoot)Configuration);
			Task.Run(() => context.MaterializeAsync("persil_core_m", "material_persil_core", false));
			Task.Run(() => context.MaterializeAsync("persilMapsProject", "material_persil_map", false));
			Task.Run(() => context.MaterializeAsync("main_bundle_core_m", "material_main_bundle_core", false));
			//Task.Run(() => context.MaterializeAsync("assignment_core_m", "material_assignment_core", false));
			app.ApplicationServices.GetService<ILandropeMaterializer>().Start(context, new TimeSpan(0, 10, 0), Materializing);
#endif
		}

		static void Materializing(LandropeContext context)
		{
			var seqs = new[] // parallel
			{
				new[]{ //sequential
					new[] { //parallel
						("pivot_base_m", "material_pivot_base"),
					},
					new[]{ //parallel
						("pivot_girik_m", "material_pivot_girik"),
						("pivot_hibah_m", "material_pivot_hibah"),
						("pivot_hgb_m", "material_pivot_hgb"),
						("pivot_shm_m", "material_pivot_shm"),
						("pivot_shp_m", "material_pivot_shp")
					},
				},
				new[]{
					new[]{
						("grouping_By_HGB", "material_group_hgb"),
						("grouping_By_PBT", "material_group_pbt"),
					}
				},
				new[]{ //sequential
					new[] { //parallel
						("status_base", "material_status_base"),
						("mapinfo_m", "material_mapinfo")
					},
					new[]{ //parallel
						("status_girik", "material_status_girik"),
						("status_hibah", "material_status_hibah"),
						("status_hgb", "material_status_hgb"),
						("status_shm", "material_status_shm"),
						("status_shp", "material_status_shp")
					}
				},new[]{
					new[]{
						("desaMapsProject","material_desa_map")
					},
					new[]
					{
						("persil_core_m", "material_persil_core"),
						("persilMapsProject", "material_persil_map"),
						("main_bundle_core_m", "material_main_bundle_core")
					}
				}
			};

		//async Task aggregator((string from, string to) couple)
		//{
		//	var stages = new[] { 
		//				"{$project:{_id:0}}",
		//				$"{{$merge:{{into:'{couple.to}',on:'key',whenMatched:'replace'}}}}" 
		//	};

		//	await context.db.GetCollection<BsonDocument>(couple.from).AggregateAsync(
		//			PipelineDefinition<BsonDocument, BsonDocument>.Create(stages)
		//		);
		//}

		// run some single aggregates in parallel
		Task paralletor((string from, string to)[] couples)
			=> Task.Run(() =>
			{
				var tasks = couples.Select(c => context.MaterializeAsync(c.from, c.to)).ToArray();
				Task.WaitAll(tasks);
			});

		// run some parallel aggregates sequentially
		async Task sequencer((string from, string to)[][] seq_couples)
		{
			foreach (var c in seq_couples)
			{
				await paralletor(c);
			}
		}

			try
			{
				var sw = new Stopwatch();
		sw.Start();
				var tasks = seqs.Select(c => sequencer(c)).ToArray();
		Task.WaitAll(tasks);
				sw.Stop();
				MyTracer.TraceInfo2($"Materialize takes total {sw.Elapsed.TotalMinutes} minutes");
			}
			catch (Exception ex)
			{
				MyTracer.TraceError2(ex);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracer;
using landrope.mod;
using mongospace;
using Newtonsoft.Json.Serialization;
//using Microsoft.AspNetCore.Http.Internal;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Primitives;
using landrope.mod2;
//using Grpc.Core;
using auth.mod;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using landrope.mod3;
using System.Threading;
using landrope.housekeeper;

namespace landrope.web
{
	//public class _Startup : WebStartup.StartupBase
	//{
	//	public Startup(IConfiguration configuration) : base(configuration)
	//	{
	//	}


	//	// This method gets called by the runtime. Use this method to add services to the container.
	//	public override void ConfigureServices(IServiceCollection services)
	//	{
	//		services.AddSession(options => {
	//			options.IdleTimeout = TimeSpan.FromMinutes(20);//You can set Time   
	//		});
	//		base.ConfigureServices(services);
	//		//services.AddScoped()
	//		services.AddScoped<IMongoEntities, authEntities>();
	//		services.AddScoped<LandropeContext>();
	//		services.AddScoped<ExtLandropeContext>();
	//		services.AddSingleton<IMemoryContext,MemoryContext>();

	//		services.AddCors(c =>
	//		{
	//			c.AddDefaultPolicy(cpb => cpb.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
	//			c.AddPolicy(nameof(landrope), cpb => cpb.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
	//		});
	//		AddSwagger(services, ("1", "1"), "landrope");
	//	}


	//	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	//	public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
	//	{
	//		string filepath = Path.Combine(env.ContentRootPath, "logs");
	//		MyTracer.SetListener(filepath, "landrope.web", "log");
	//		//app.UseHttpsRedirection();

	//		AddSwagger(app, "1", "landrope");

	//		app.UseCors(nameof(landrope));
	//		//app.UseMiddleware<RequestResponseLoggingMiddleware>();

	//		base.Configure(app, env, true);
	//		var memcon = (IMemoryContext)app.ApplicationServices.GetService(typeof(IMemoryContext));
	//		if (memcon != null)
	//		{
	//			var init = DateTime.Today.AddDays(1).AddHours(3) - DateTime.Now;
	//			memcon.SetReloadTime(init, TimeSpan.FromDays(1));
	//		}
	//	}
	//}

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
			services.AddScoped<LandropeContext>();
			services.AddScoped<ExtLandropeContext>();
			services.AddScoped<LandropePlusContext>();
			services.AddSingleton<Housekeeping>();

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
			services.AddControllers();
		}


		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			string filepath = Path.Combine(env.ContentRootPath, "logs");
			MyTracer.SetListener(filepath, "landrope.web", "log");

			Program.OpenLogFile(Path.Combine(filepath, $"log-{DateTime.Now:yyyyMMddHHmmss}.txt"));

			HttpAccessor.Config.Configure(Configuration, (IHostEnvironment)env);
			HttpAccessor.Helper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
			//app.UseHttpsRedirection();

			var midware = Configuration.GetValue<string?>("midware");
			if (midware?.ToLower() == "true")
			{
				app.Use(async (context, next) =>
				{
					var req = context.Request;
					Program.Log($"scheme:{req.Scheme}, method:{req.Method}, path:{req.Path}");

				// Call the next delegate/middleware in the pipeline

				try
					{
						await next();

						var resp = context.Response;
						var hdrs = resp.Headers.ToDictionary(h => h.Key, h => h.Value);
						var headers = System.Text.Json.JsonSerializer.Serialize(hdrs);
						Program.Log($"status:{resp.StatusCode}, headers:{headers}, ctn-type:{resp.ContentType}");
					}
					catch (Exception ex)
					{
						Program.Log($"Middleware error: {ex.AllMessages()}");
					}
				});
			}

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			//app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors(nameof(landrope));
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint($"/swagger/V2/swagger.json", "landrope");
			});

			var hk = app.ApplicationServices.GetService<Housekeeping>();
			var context = new ExtLandropeContext((IConfigurationRoot)Configuration);
			hk.Start(new TimeSpan(0,5,0), context);

			//app.UseMiddleware<RequestResponseLoggingMiddleware>();
		}


	}
}

//using Grpc.Core;
using auth.mod;
using BundlerConsumer;
using FileRepos;
using graph.mod;
using GraphConsumer;
using landrope.consumers;
using landrope.hosts;
using landrope.housekeeper;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.mod.cross;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using mongospace;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
//using Microsoft.AspNetCore.Http.Internal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tracer;
using MailSender;
using landrope.api3.Services.PraPembebasan;
using Syncfusion.Licensing;
using System.Net;

namespace landrope.api3
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
            services.AddScoped<LandropePlusContext>();
            services.AddScoped<LandropePayContext>();
            services.AddScoped<LandropeCrossContext>();
            //services.AddSingleton<Housekeeping>();
            services.AddScoped<FileGrid>();

            services.AddSingleton<IGraphHostConsumer, GraphHostConsumer>();
            services.AddSingleton<IBundlerHostConsumer, BundlerHostConsumer>();
            services.AddSingleton<MailSenderClass>();

            services.RegisterBayarHost();

            services.AddTransient<IPraPembebasanService, PraPembebasanService>();

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
            MyTracer.SetListener(filepath, "landrope.api3", "log");

            Program.OpenLogFile(Path.Combine(filepath, $"log-{DateTime.Now:yyyyMMddHHmmss}.txt"));

            ContextService.Configure(app.ApplicationServices);

            HttpAccessor.Config.Configure(Configuration, (IHostEnvironment)env);
            HttpAccessor.Helper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            //app.UseHttpsRedirection();

            SyncfusionLicenseProvider.RegisterLicense(Configuration.GetSection("SyncfusionLicense").Value);

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

            app.ApplicationServices.GetBayarHost();
            app.ApplicationServices.GetGraphHostConsumer();
            app.ApplicationServices.GetBundlerHostConsumer();
        }


    }
}
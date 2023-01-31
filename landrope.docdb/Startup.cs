using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using landrope.mod3;

namespace landrope.docdb
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			//var builder = new ConfigurationBuilder()
			//   .SetBasePath(env.ContentRootPath)
			//   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			//   .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
			//   .AddEnvironmentVariables();
			//Configuration = builder.Build();
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
            //services.AddControllersWithViews();

            services.Configure<MvcRazorRuntimeCompilationOptions>(opts => {
				opts.FileProviders.Clear();
				opts.FileProviders.Add(
						new DatabaseFileProvider(Configuration.GetValue<string>("data:uri"), Configuration.GetValue<string>("data:db"))
				); }
			);
			services.AddMvc().AddRazorRuntimeCompilation().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);
			//services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddScoped<LandropePlusContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
            HttpAccessor.Config.Configure(Configuration, (IHostEnvironment)env);

            if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                                    name: "default",
									pattern: "{controller=Home}/{action=Index}/{id?}");
				   				  //pattern: "{controller=Home}/{action=Index}");

			});
        }
	}
}

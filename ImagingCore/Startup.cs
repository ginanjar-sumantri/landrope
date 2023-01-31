using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace ImagingCore
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }
    public IConfiguration Configuration { get; }
    public void ConfigureServices(IServiceCollection services)
    {
			services.AddControllers();
			//services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
    }
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
			var midware = "true";
			if (midware?.ToLower() == "true")
			{
				app.Use(async (context, next) =>
				{
					var req = context.Request;
					Console.WriteLine($"scheme:{req.Scheme}, method:{req.Method}, path:{req.Path}");

					try
					{
						await next();

						var resp = context.Response;
						var hdrs = resp.Headers.ToDictionary(h => h.Key, h => h.Value);
						var headers = System.Text.Json.JsonSerializer.Serialize(hdrs);
						Console.WriteLine($"status:{resp.StatusCode}, headers:{headers}, ctn-type:{resp.ContentType}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Middleware error: {ex.Message}");
					}
				});
			}

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				//endpoints.MapRazorPages();
			});

			//app.UseMvc();
		}
	}
}
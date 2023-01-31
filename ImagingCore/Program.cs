using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace ImagingCore
{
	static class Program
	{
		public static MainForm mainform;
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			CreateWebHostBuilder(new string[0]).Build().RunAsync();

			Application.Run(mainform=new MainForm());
		}

		public static IHostBuilder CreateWebHostBuilder(string[] args) =>
				Host.CreateDefaultBuilder(args)
						.ConfigureWebHostDefaults(webBuilder =>
						{
							webBuilder.ConfigureKestrel(op => op.ListenLocalhost(2510));
							webBuilder.UseStartup<Startup>();
						});
	}
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace landrope.api2
{
	public class Program
	{
		static string logfilepath = null;
		static FileStream fs = null;
		static StreamWriter sw;

		public static void OpenLogFile(string name)
		{
			logfilepath = name;
			fs = new FileStream(logfilepath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			sw = new StreamWriter(fs);
		}
		public static void Log(string info)
		{
			if (fs != null && sw != null)
			{
				sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {info}");
				sw.Flush();
				fs.Flush();
			}
		}

		public static void Main(string[] args)
		{
			//AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
				Host.CreateDefaultBuilder(args)
						.ConfigureWebHostDefaults(webBuilder =>
						{
							webBuilder.UseUrls("http://0.0.0.0:7890");
							webBuilder.UseKestrel();
							//webBuilder.ConfigureKestrel(op => op.ListenAnyIP(7890));
							webBuilder.UseIISIntegration();
							webBuilder.UseStartup<Startup>();
						});
	}
}

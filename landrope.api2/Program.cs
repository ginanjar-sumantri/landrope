using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
				Host.CreateDefaultBuilder(args)
						.ConfigureWebHostDefaults(webBuilder =>
						{
							webBuilder.ConfigureKestrel(op => op.ListenAnyIP(7879));
							webBuilder.UseIIS();
							webBuilder.UseIISIntegration();
							webBuilder.UseStartup<Startup>();
						});
	}
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.api3
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
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {

            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.ConfigureKestrel(op => op.ListenAnyIP(7880));
                            webBuilder.UseIIS();
                            webBuilder.UseIISIntegration();
                            webBuilder.UseStartup<Startup>();
                        });
    }
}

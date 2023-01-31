using ConChangeStream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Tracer;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddConChangeStream();
    })
    .ConfigureAppConfiguration((hostContext, configBuilder) =>
    {
        var env = hostContext.HostingEnvironment;
        string filepath = Path.Combine(env.ContentRootPath, "logs");
        MyTracer.SetListener(filepath, "landrope.changestream", "log");

        configBuilder.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
    })
    .Build();


await host.RunAsync();
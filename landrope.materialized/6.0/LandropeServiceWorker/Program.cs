using LandropeServiceWorker;
using ConChangeStream;
using System.Reflection;
using Tracer;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opt =>
    {
        opt.ServiceName = "Landrope Service Worker";
    })
    .ConfigureServices(services =>
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

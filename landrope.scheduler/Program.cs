using landrope.scheduler;
using landrope.scheduler.Helpers;
using landrope.scheduler.Interfaces;
using landrope.scheduler.Interfaces.Common;
using landrope.scheduler.Jobs;
using landrope.scheduler.Services;
using landrope.scheduler.Utilities;
using Quartz;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((context, services) =>
    {
        RegisterConfigurations(context);

        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionScopedJobFactory();

            RegisterJobs(q, context);
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        ConfigureServices(services);
    })
    .Build();

await host.RunAsync();

void RegisterConfigurations(HostBuilderContext builder)
{
    AppSetting.Server1 = builder.Configuration.GetSection("Server1").Get<Uri>() ?? new Uri("");
    AppSetting.Server2 = builder.Configuration.GetSection("Server2").Get<Uri>() ?? new Uri("");
    AppSetting.MailSettings = builder.Configuration.GetSection("MailSettings").Get<IEnumerable<MailSetting>>() ?? new MailSetting[0];
}

void ConfigureServices(IServiceCollection services)
{
    services.AddHostedService<Worker>();
    services.AddTransient<IHttpClientService, HttpClientService>();

    services.AddTransient<IMailService, MailService>();
}

void RegisterJobs(IServiceCollectionQuartzConfigurator quartz, HostBuilderContext builder)
{
    quartz.AddJobAndTrigger<ReminderSLAPenugasanJob>(builder.Configuration);
    quartz.AddJobAndTrigger<ReminderBayarPendingAccountingJob>(builder.Configuration);
    quartz.AddJobAndTrigger<ReminderBayarPendingKasirJob>(builder.Configuration);
}
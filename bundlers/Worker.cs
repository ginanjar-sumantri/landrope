using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracer;

namespace bundlers
{
	public class Worker : BackgroundService// where T : ISvcHost
	{
		SvcHost host;
		private readonly ILogger<Worker> _logger;

		public Worker(ILogger<Worker> logger)
		{
			_logger = logger;
		}

		//protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Worker Executed Async");
			stoppingToken.Register(() => _logger.LogInformation("Worker Service is stopping."));

			await Task.Run(() => stoppingToken.WaitHandle.WaitOne());

			/*
						while (!stoppingToken.IsCancellationRequested)
						{
							_logger.LogInformation("Workee Service is doing background work.");

							await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
						}
			*/
			_logger.LogInformation("Worker Service has been stopped.");
		}

		/*		public override Task StartAsync(CancellationToken cancellationToken)
				{
					*//*			if (!System.Diagnostics.Debugger.IsAttached)
								{
									System.Diagnostics.Debugger.Launch();
									System.Diagnostics.Debugger.Break();
								}*//*
					MyTracer.TryAddListener("logs", "graph", "log");
					MyTracer.JustLog($"Starting {host.ServiceDesc} service...");
					_logger.LogInformation($"Starting {host.ServiceDesc} service...");
					host.Start();
					_logger.LogInformation($"{host.ServiceDesc} service started");
					Console.WriteLine($"{host.ServiceDesc} service started");
					return Task.CompletedTask;
				}

				public override Task StopAsync(CancellationToken cancellationToken)
				{
					MyTracer.TryAddListener("logs", "graph", "log");
					MyTracer.JustLog($"Stoppng {host.ServiceDesc} service...");
					_logger.LogInformation($"Stopping {host.ServiceDesc} service...");
					host.Stop();
					_logger.LogInformation($"{host.ServiceDesc} service stopped");
					return Task.CompletedTask;
				}*/
	}
	public interface ISvcHost
	{
		void Start();
		void Stop();
		string ServiceName { get; }
		string ServiceDesc { get; }
	}
}

using BundlerConsumer;
using GenWorkflow;
using GraphConsumer;
using GraphHost;
using landrope.consumers;
using landrope.engines;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.hosts
{
	public static class HostServicesHelper
	{
		public static void RegisterBayarHost(this IServiceCollection services)
		{
			services.AddSingleton<IBayarHost, BayarHost>();
			services.AddSingleton<PTSKHost>();
			services.AddSingleton<IProsesHost, ProsesHost>();
			services.AddSingleton<IGraphHostSvc, GraphHostSvc>();
			services.AddSingleton<IStateRequestHost, StateRequestHost>();
			services.AddSingleton<IPersilRequestHost, PersilApprovalHost>();
		}

		public static BayarHost GetBayarHost(this IServiceProvider services)
			=> services.GetService<IBayarHost>() as BayarHost;

		public static PTSKHost GetPTSKHost(this IServiceProvider services)
				=> services.GetService<PTSKHost>();

		public static ProsesHost GetProsesHost(this IServiceProvider services)
			=> services.GetService<IProsesHost>() as ProsesHost;

		public static PersilApprovalHost GetBidangHost(this IServiceProvider services)
			=> services.GetService<IPersilRequestHost>() as PersilApprovalHost;

		public static StateRequestHost GetStateRequestHost(this IServiceProvider services)
			=> services.GetService<IStateRequestHost>() as StateRequestHost;

        public static GraphHostSvc GetGraphHost(this IServiceProvider services)
            => services.GetService<IGraphHostSvc>() as GraphHostSvc;
        public static GraphHostConsumer GetGraphHostConsumer(this IServiceProvider services)
            => services.GetService<IGraphHostConsumer>() as GraphHostConsumer;

        public static BundlerHostConsumer GetBundlerHostConsumer(this IServiceProvider services)
            => services.GetService<IBundlerHostConsumer>() as BundlerHostConsumer;
    }
}

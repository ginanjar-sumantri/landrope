using landrope.engines;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.hosts.old
{
	public static class HostServicesHelper
	{
		public static void RegisterTrxBatchHost(this IServiceCollection services)
		{
			services.AddSingleton<ITrxBatchHost,TrxBatchHost>();
		}
		public static TrxBatchHost GetBatchHost(this IServiceProvider services)
			=> services.GetService<ITrxBatchHost>() as TrxBatchHost;
	}
}

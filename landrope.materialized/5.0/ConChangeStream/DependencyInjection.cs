using System;
using Microsoft.Extensions.DependencyInjection;

namespace ConChangeStream
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddConChangeStream(this IServiceCollection services)
        {
            services.AddHostedService<ChangeStreamService>();

            // services.AddOptions();
            return services;
        }
    }
}

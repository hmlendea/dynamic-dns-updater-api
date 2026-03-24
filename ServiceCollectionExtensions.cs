using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using DynamicDnsUpdater.API.Configuration;
using DynamicDnsUpdater.API.Service;
using DynamicDnsUpdater.API.Service.Integrations.Gandi;

namespace DynamicDnsUpdater.API
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigurations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            GandiSettings gandiSettings = new();
            NuciLoggerSettings nuciLoggerSettings = new();

            configuration.Bind(nameof(GandiSettings), gandiSettings);
            configuration.Bind(nameof(NuciLoggerSettings), nuciLoggerSettings);

            services.AddSingleton(gandiSettings);
            services.AddSingleton(nuciLoggerSettings);

            return services;
        }

        public static IServiceCollection AddCustomServices(
            this IServiceCollection services) => services
                .AddTransient<ILogger, NuciLogger>()
                .AddTransient<IDnsRecordService, DnsRecordService>()
                .AddTransient<IGandiService, GandiService>();
    }
}

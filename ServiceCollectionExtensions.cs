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
            SecuritySettings securitySettings = new();
            GandiSettings gandiSettings = new();

            configuration.Bind(nameof(SecuritySettings), securitySettings);
            configuration.Bind(nameof(GandiSettings), gandiSettings);

            return services
                .AddSingleton(securitySettings)
                .AddSingleton(gandiSettings)
                .AddNuciLoggerSettings(configuration);
        }

        public static IServiceCollection AddCustomServices(
            this IServiceCollection services) => services
                .AddTransient<ILogger, NuciLogger>()
                .AddTransient<IDnsRecordService, DnsRecordService>()
                .AddTransient<IGandiService, GandiService>();
    }
}

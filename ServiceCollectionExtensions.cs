using Microsoft.Extensions.DependencyInjection;

using DynamicDnsUpdater.API.Service;
using Microsoft.Extensions.Configuration;

namespace DynamicDnsUpdater.API
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomServices(
            this IServiceCollection services) => services
                .AddSingleton<IDnsRecordService, DnsRecordService>();
    }
}

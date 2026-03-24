using System.Threading.Tasks;
using NuciLog.Core;

namespace DynamicDnsUpdater.API.Service.Integrations.Gandi
{
    public class GandiService(ILogger logger) : ProviderService(logger), IGandiService
    {
        public override string ProviderName => "Gandi";

        protected override async Task PerformUpdate(
            string domainName,
            string ipAddress)
        {

        }
    }
}

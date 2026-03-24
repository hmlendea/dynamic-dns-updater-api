using System;
using System.Threading.Tasks;
using DynamicDnsUpdater.API.Service.Integrations.Gandi;
using DynamicDnsUpdater.API.Service.Models;

namespace DynamicDnsUpdater.API.Service
{
    public class DnsRecordService(
        IGandiService gandiService)
        : IDnsRecordService
    {
        public async Task Update(
            string domainName,
            string ipAddress,
            string dnsProviderName)
        {
            DnsProvider provider = DnsProvider.FromString(dnsProviderName);

            if (provider == DnsProvider.Gandi)
            {
                await gandiService.Update(domainName, ipAddress);
            }
            else
            {
                throw new NotImplementedException($"The '{dnsProviderName}' DNS provider is not supported.");
            }
        }
    }
}

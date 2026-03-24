using System.Threading.Tasks;

namespace DynamicDnsUpdater.API.Service
{
    public class DnsRecordService() : IDnsRecordService
    {
        public async Task Update(
            string domainName,
            string ipAddress,
            string dnsProvider)
        {
        }
    }
}

using System.Threading.Tasks;

namespace DynamicDnsUpdater.API.Service
{
    public interface IDnsRecordService
    {
        Task Update(
            string domainName,
            string ipAddress,
            string dnsProviderName);
    }
}

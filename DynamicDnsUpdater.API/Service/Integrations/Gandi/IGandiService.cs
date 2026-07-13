using System.Threading.Tasks;

namespace DynamicDnsUpdater.API.Service.Integrations.Gandi
{
    public interface IGandiService
    {
        Task Update(
            string domainName,
            string ipAddress);
    }
}

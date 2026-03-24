using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DynamicDnsUpdater.API.Requests;
using DynamicDnsUpdater.API.Service;
using NuciAPI.Controllers;

namespace DynamicDnsUpdater.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DnsRecordsController(DnsRecordService service) : NuciApiController
    {
        [HttpPut("{domainName}")]
        public async Task<ActionResult> Get(
            [FromRoute] string domainName,
            [FromQuery] PutDnsRecordRequest request)
            => await ProcessRequestAsync(
                request,
                () => service.Update(
                    domainName,
                    request.IpAddress,
                    request.DnsProvider),
                NuciApiAuthorisation.None);
    }
}

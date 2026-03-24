using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DynamicDnsUpdater.API.Requests;
using DynamicDnsUpdater.API.Service;
using NuciAPI.Controllers;
using System;

namespace DynamicDnsUpdater.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DnsRecordsController(
        IDnsRecordService service) : NuciApiController
    {
        [HttpPut("{domainName}")]
        public async Task<ActionResult> Update(
            [FromRoute] string domainName,
            [FromBody] PutDnsRecordRequest request)
            => await ProcessRequestAsync(
                request,
                () => service.Update(
                    domainName,
                    request.IpAddress,
                    request.DnsProvider),
                NuciApiAuthorisation.None);
    }
}

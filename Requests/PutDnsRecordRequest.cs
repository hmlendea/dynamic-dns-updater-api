using System.Text.Json.Serialization;
using NuciAPI.Requests;

namespace DynamicDnsUpdater.API.Requests
{
    public class PutDnsRecordRequest : NuciApiRequest
    {
        [JsonPropertyName("ip")]
        public string IpAddress { get; set; }

        [JsonPropertyName("provider")]
        public string DnsProvider { get; set; }
    }
}

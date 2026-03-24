using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DynamicDnsUpdater.API.Configuration;
using NuciExtensions;
using NuciLog.Core;

namespace DynamicDnsUpdater.API.Service.Integrations.Gandi
{
    public class GandiService(
        GandiSettings settings,
        ILogger logger) : ProviderService(logger), IGandiService
    {
        public override string ProviderName => "Gandi";

        protected override async Task PerformUpdate(
            string domainName,
            string ipAddress)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(domainName, nameof(domainName));
            ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress, nameof(ipAddress));

            Regex domainRegex = new Regex(@"^([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$");
            if (!domainRegex.IsMatch(domainName))
            {
                throw new FormatException("Invalid domain format.");
            }

            string[] domainParts = domainName.Split('.');

            if (domainParts.Length < 3)
            {
                throw new FormatException("The domain must include a subdomain.");
            }

            string domain = $"{domainParts[^2]}.{domainParts[^1]}";
            string subdomain = string.Join('.', domainParts, 0, domainParts.Length - 2);

            HttpClient client = new();
            client.DefaultRequestHeaders.Add("Authorization", $"Apikey {settings.ApiKey}");

            string url = $"https://api.gandi.net/v5/livedns/domains/{domain}/records/{subdomain}/A";

            var payload = new
            {
                rrset_type = "A",
                rrset_ttl = 300,
                rrset_values = new[] { ipAddress }
            };

            string json = payload.ToJson();
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = $"Failed to update DNS record. Status code: {response.StatusCode}, Response: {responseBody}";

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("message", out JsonElement message))
                {
                    errorMessage = message.GetString();
                }

                throw new HttpRequestException(errorMessage);
            }
        }
    }
}

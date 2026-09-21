using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using NuciAPI.Middleware;

using DynamicDnsUpdater.API.Requests;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    internal static class ApiRequestFactory
    {
        internal static string ClientId => "IlarionPintilie";

        internal static string DomainName => "test.nucilandia.ro";

        internal static string IpAddress => "::1";

        internal static string Provider => "Gandi";

        internal static HttpRequestMessage CreateAuthorisedPutRequest(
            string domainName,
            string ipAddress,
            string provider)
        {
            PutDnsRecordRequest requestBody = new()
            {
                IpAddress = ipAddress,
                DnsProvider = provider
            };
            string jsonContent = JsonSerializer.Serialize(requestBody);

            return CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{Uri.EscapeDataString(domainName)}",
                jsonContent);
        }

        internal static HttpRequestMessage CreateAuthorisedRequest(
            HttpMethod method,
            string requestUri)
        {
            HttpRequestMessage request = new(method, requestUri);

            AddValidProtocolHeaders(request);
            AddValidAuthorisation(request);

            return request;
        }

        internal static HttpRequestMessage CreateAuthorisedJsonRequest(
            HttpMethod method,
            string requestUri,
            string jsonContent)
        {
            HttpRequestMessage request = CreateAuthorisedRequest(method, requestUri);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            return request;
        }

        internal static void AddValidAuthorisation(HttpRequestMessage request)
            => request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                ApiTestHost.ApiKey);

        internal static void SetAuthorisation(
            HttpRequestMessage request,
            string? authorisation)
        {
            request.Headers.Remove("Authorization");

            if (authorisation is not null)
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorisation);
            }
        }

        internal static void AddValidProtocolHeaders(HttpRequestMessage request)
            => AddProtocolHeaders(
                request,
                ClientId,
                CreateRequestId(),
                CreateTimestamp());

        internal static void AddProtocolHeaders(
            HttpRequestMessage request,
            string clientId,
            string requestId,
            string timestamp)
        {
            request.Headers.TryAddWithoutValidation(NuciApiHeaderNames.ClientId, clientId);
            request.Headers.TryAddWithoutValidation(NuciApiHeaderNames.RequestId, requestId);
            request.Headers.TryAddWithoutValidation(NuciApiHeaderNames.Timestamp, timestamp);
        }

        internal static void SetHeader(
            HttpRequestMessage request,
            string headerName,
            string headerValue)
        {
            request.Headers.Remove(headerName);
            request.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        internal static string CreateRequestId()
            => Guid.NewGuid().ToString("D").ToUpperInvariant();

        internal static string CreateTimestamp()
            => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        internal static string CreateTimestamp(DateTimeOffset timestamp)
            => timestamp.ToString("O", CultureInfo.InvariantCulture);
    }
}
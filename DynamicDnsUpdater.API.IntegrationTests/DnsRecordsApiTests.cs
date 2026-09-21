using System.Net.Http;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using NuciAPI.Middleware;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class DnsRecordsApiTests : ApiIntegrationTestBase
    {
        [TestCase("test.nucilandia.ro", "::1", "Gandi")]
        [TestCase("home.dummy-domain.com", "0.0.0.0", "Gandi")]
        [TestCase("router.dummy-domain.md", "255.255.255.255", "Gandi")]
        [TestCase("gateway.dummy-domain.ro", "192.0.2.1", "Gandi")]
        [TestCase("vpn.test.url.com", "198.51.100.42", "Gandi")]
        [TestCase("nas.test.url.md", "203.0.113.42", "Gandi")]
        [TestCase("a.b.c.nucilandia.ro", "2001:db8::1", "Gandi")]
        [TestCase("WWW.DUMMY-DOMAIN.RO", "2001:DB8:0:0:0:0:0:1", "Gandi")]
        [TestCase("localhost", "127.0.0.1", "Gandi")]
        [TestCase("4", "8", "Gandi")]
        [TestCase("test domain.com", "999.999.999.999", "Gandi")]
        [TestCase("test+record.nucilandia.ro", "not-an-ip", "Gandi")]
        [TestCase("test#record.nucilandia.ro", "", "Gandi")]
        [TestCase("test%record.nucilandia.ro", "   ", "Gandi")]
        [TestCase("test.nucilandia.ro", "::ffff:192.0.2.128", "gandi")]
        [TestCase("test.nucilandia.ro", "fe80::1%1", "GANDI")]
        [TestCase("test.nucilandia.ro", "2001:db8::dead:beef", "")]
        [TestCase("test.nucilandia.ro", "192.168.1.1\r\n", "   ")]
        [TestCase("test.nucilandia.ro", "e=mc\u00B2", "NucilandiaDns")]
        [TestCase("test.\u0218upi\u0219an.ro", "Glory to Nucilandia!", "Provider\nWith\tControls")]
        public async Task GivenVariedValues_WhenUpdatingADnsRecord_ThenTheServiceReceivesEveryValueUnmodified(
            string domainName,
            string ipAddress,
            string provider)
            => await AssertRequestIsForwarded(
                ApiRequestFactory.CreateAuthorisedPutRequest(domainName, ipAddress, provider),
                domainName,
                ipAddress,
                provider);

        [TestCase("{}", null, null)]
        [TestCase("{\"ip\":\"::1\"}", "::1", null)]
        [TestCase("{\"provider\":\"Gandi\"}", null, "Gandi")]
        [TestCase("{\"ip\":null,\"provider\":null}", null, null)]
        [TestCase("{\"ip\":\"\",\"provider\":\"\"}", "", "")]
        [TestCase("{\"ip\":\"   \",\"provider\":\"   \"}", "   ", "   ")]
        [TestCase("{\"IP\":\"::1\",\"PROVIDER\":\"Gandi\"}", "::1", "Gandi")]
        [TestCase("{\"Ip\":\"192.0.2.1\",\"Provider\":\"gandi\"}", "192.0.2.1", "gandi")]
        [TestCase("{\"ip\":\"first\",\"ip\":\"second\",\"provider\":\"Gandi\"}", "second", "Gandi")]
        [TestCase("{\"ip\":\"::1\",\"provider\":\"Gandi\",\"unknown\":42}", "::1", "Gandi")]
        [TestCase("{\"ip\":\"::1\",\"provider\":\"Gandi\",\"nested\":{\"value\":true}}", "::1", "Gandi")]
        [TestCase("{\"ip\":\"\\u003A\\u003A1\",\"provider\":\"Gan\\u0064i\"}", "::1", "Gandi")]
        public async Task GivenAJsonObject_WhenUpdatingADnsRecord_ThenTheBoundPropertiesAreForwarded(
            string jsonContent,
            string? expectedIpAddress,
            string? expectedProvider)
            => await AssertRequestIsForwarded(
                ApiRequestFactory.CreateAuthorisedJsonRequest(
                    HttpMethod.Put,
                    $"/DnsRecords/{ApiRequestFactory.DomainName}",
                    jsonContent),
                ApiRequestFactory.DomainName,
                expectedIpAddress,
                expectedProvider);

        [TestCase("/dnsrecords/test.nucilandia.ro", "test.nucilandia.ro")]
        [TestCase("/DNSRECORDS/test.nucilandia.ro", "test.nucilandia.ro")]
        [TestCase("/DnsRecords/test.nucilandia.ro/", "test.nucilandia.ro")]
        [TestCase("/DnsRecords/test.nucilandia.ro?source=integration", "test.nucilandia.ro")]
        [TestCase("/DnsRecords/test%20record.nucilandia.ro", "test record.nucilandia.ro")]
        [TestCase("/DnsRecords/test%2Brecord.nucilandia.ro", "test+record.nucilandia.ro")]
        [TestCase("/DnsRecords/test%25record.nucilandia.ro", "test%record.nucilandia.ro")]
        [TestCase("/DnsRecords/test.%C8%98upi%C8%99an.ro", "test.\u0218upi\u0219an.ro")]
        public async Task GivenAVariedRoute_WhenUpdatingADnsRecord_ThenTheDecodedDomainNameIsForwarded(
            string requestUri,
            string expectedDomainName)
            => await AssertRequestIsForwarded(
                ApiRequestFactory.CreateAuthorisedJsonRequest(
                    HttpMethod.Put,
                    requestUri,
                    "{\"ip\":\"::1\",\"provider\":\"Gandi\"}"),
                expectedDomainName,
                "::1",
                "Gandi");

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("unsigned-token")]
        [TestCase("P%40ssw0rd%21")]
        [TestCase("value+with/slashes=and-padding")]
        public async Task GivenAnyOptionalHmacHeader_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string? hmacToken)
        {
            HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);

            if (hmacToken is not null)
            {
                request.Headers.TryAddWithoutValidation(NuciApiHeaderNames.HmacToken, hmacToken);
            }

            await AssertRequestIsForwarded(
                request,
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);
        }

        private async Task AssertRequestIsForwarded(
            HttpRequestMessage request,
            string expectedDomainName,
            string? expectedIpAddress,
            string? expectedProvider)
        {
            TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    expectedDomainName,
                    expectedIpAddress!,
                    expectedProvider!))
                .Returns(Task.CompletedTask);

            using (request)
            using (HttpResponseMessage response = await Client.SendAsync(request))
            {
                await ApiResponseAssertions.AssertSuccessAsync(response);
            }

            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    expectedDomainName,
                    expectedIpAddress!,
                    expectedProvider!),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }
    }
}
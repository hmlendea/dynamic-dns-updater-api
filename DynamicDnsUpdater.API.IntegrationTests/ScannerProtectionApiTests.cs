using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class ScannerProtectionApiTests : ApiIntegrationTestBase
    {
        private static string ForwardedForHeaderName => "X-Forwarded-For";

        [TestCase("/.env")]
        [TestCase("/.ENV")]
        [TestCase("/.env.production")]
        [TestCase("/.env.development.local")]
        [TestCase("/.git/config")]
        [TestCase("/.github/workflows/main.yml")]
        [TestCase("/.vscode/settings.json")]
        [TestCase("/.aws/credentials")]
        [TestCase("/.npmrc")]
        [TestCase("/.pgpass")]
        [TestCase("/.well-known/security.txt")]
        [TestCase("/_profiler")]
        [TestCase("/_profiler/phpinfo")]
        [TestCase("/__aws_leak_probe_613__")]
        [TestCase("/actuator/env")]
        [TestCase("/actuator/heapdump")]
        [TestCase("/api/env")]
        [TestCase("/api/heapdump")]
        [TestCase("/api-keys.txt")]
        [TestCase("/appsettings.json")]
        [TestCase("/appsettings.Development.json")]
        [TestCase("/application-production.yml")]
        [TestCase("/backup.sql")]
        [TestCase("/backup.sql.gz")]
        [TestCase("/backup.zip")]
        [TestCase("/config.json")]
        [TestCase("/config.php")]
        [TestCase("/config/database.yml")]
        [TestCase("/config/master.key")]
        [TestCase("/credentials.json")]
        [TestCase("/database.sql")]
        [TestCase("/docker-compose.yml")]
        [TestCase("/error.log")]
        [TestCase("/logs/error.log")]
        [TestCase("/package.json")]
        [TestCase("/phpinfo.php")]
        [TestCase("/robots.txt")]
        [TestCase("/secrets.json")]
        [TestCase("/serverless.yml")]
        [TestCase("/sitemap.xml")]
        [TestCase("/static../etc/passwd")]
        [TestCase("/storage/logs/laravel.log")]
        [TestCase("/terraform.tfstate")]
        [TestCase("/terraform.tfvars")]
        [TestCase("/var/log/system.log")]
        [TestCase("/web.config")]
        [TestCase("/wordpress/.env")]
        [TestCase("/wp-admin/")]
        [TestCase("/wp-config.php")]
        [TestCase("/wp-content/debug.log")]
        [TestCase("/xmlrpc.php")]
        public async Task GivenAForbiddenResourcePath_WhenRequestingIt_ThenForbiddenIsReturned(
            string requestUri)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("?XDEBUG_SESSION_START=phpstorm")]
        [TestCase("?xdebug_session_start=PHPSTORM")]
        [TestCase("?app_vl=1.2.3")]
        [TestCase("?app_vl=")]
        [TestCase("?file=../../../../etc/passwd")]
        [TestCase("?raw??")]
        [TestCase("?rest_route=/batch/v1")]
        [TestCase("?rest_route=/wp/v2/users")]
        public async Task GivenAForbiddenQueryPattern_WhenRequestingIt_ThenForbiddenIsReturned(
            string queryString)
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                $"/search{queryString}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("Mozilla/5.0 Chrome/143.0.0.0 Safari/537.36")]
        [TestCase("InternetMeasurement/1.0")]
        [TestCase("OAI-SearchBot/1.0")]
        [TestCase("SecurityScanner/2.0")]
        [TestCase("securityscanner/613")]
        [TestCase("Prefix-OAI-SearchBot-Suffix")]
        public async Task GivenAForbiddenUserAgent_WhenUpdatingADnsRecord_ThenForbiddenIsReturned(
            string userAgent)
        {
            using HttpRequestMessage request = CreateValidUpdateRequest();
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("oai-searchbot(at)openai.com")]
        [TestCase("OAI-SEARCHBOT(AT)OPENAI.COM")]
        [TestCase("Oai-SearchBot(at)OpenAI.Com")]
        public async Task GivenAForbiddenFromHeader_WhenUpdatingADnsRecord_ThenForbiddenIsReturned(
            string fromHeader)
        {
            using HttpRequestMessage request = CreateValidUpdateRequest();
            request.Headers.TryAddWithoutValidation("From", fromHeader);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("\".Not/A)Brand\";v=\"99\"")]
        [TestCase("Chromium;v=\"140\", \".Not/A)Brand\";v=\"99\"")]
        [TestCase("prefix.not/a)brand.suffix")]
        public async Task GivenAForbiddenClientHintsHeader_WhenUpdatingADnsRecord_ThenForbiddenIsReturned(
            string clientHints)
        {
            using HttpRequestMessage request = CreateValidUpdateRequest();
            request.Headers.TryAddWithoutValidation("sec-ch-ua", clientHints);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("HEAD")]
        [TestCase("OPTIONS")]
        [TestCase("TRACE")]
        [TestCase("CONNECT")]
        [TestCase("BREW")]
        public async Task GivenAnUnsupportedRootVerb_WhenRequestingTheRoot_ThenForbiddenIsReturned(
            string methodName)
        {
            HttpMethod method = new(methodName);
            using HttpRequestMessage request = new(method, "/");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("GET")]
        [TestCase("POST")]
        [TestCase("PUT")]
        [TestCase("DELETE")]
        [TestCase("PATCH")]
        public async Task GivenAnEmptyRootRequest_WhenUsingASupportedVerb_ThenForbiddenIsReturned(
            string methodName)
        {
            HttpMethod method = new(methodName);
            using HttpRequestMessage request = new(method, "/");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenARootGetRequestWithAQuery_WhenRequestingTheRoot_ThenScannerProtectionPermitsRouting()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                "/?search=Glory%20to%20Nucilandia");

            using HttpResponseMessage response = await Client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenARootPostRequestWithABody_WhenRequestingTheRoot_ThenScannerProtectionPermitsRouting()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Post,
                "/");
            request.Content = new StringContent(
                "{\"message\":\"Praise the Sun!\"}",
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await Client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("?app_vl=1.2.3&other=x")]
        [TestCase("?other=x&app_vl=1.2.3")]
        [TestCase("?file=etc/passwd")]
        [TestCase("?rest_route=/wp/v2/posts")]
        [TestCase("?search=wp-config.php")]
        [TestCase("?raw=value")]
        public async Task GivenAPermittedQueryNearMiss_WhenRequestingIt_ThenScannerProtectionPermitsRouting(
            string queryString)
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                $"/search{queryString}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("MyApp/1.0 (Linux; compatible)")]
        [TestCase("Mozilla/5.0 Chrome/142.0.0.0 Safari/537.36")]
        [TestCase("DynamicDnsUpdater.Client/613")]
        [TestCase("")]
        public async Task GivenAPermittedUserAgent_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string userAgent)
        {
            ConfigureSuccessfulUpdate();
            using HttpRequestMessage request = CreateValidUpdateRequest();
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAClientIpAddressIsBanned_WhenItRequestsASafePath_ThenForbiddenIsReturnedAgain()
        {
            using HttpRequestMessage maliciousRequest = new(HttpMethod.Get, "/.env");
            ApiRequestFactory.SetHeader(maliciousRequest, ForwardedForHeaderName, "198.51.100.42");
            using HttpRequestMessage subsequentRequest = CreateValidUpdateRequest();
            ApiRequestFactory.SetHeader(subsequentRequest, ForwardedForHeaderName, "198.51.100.42");

            using HttpResponseMessage maliciousResponse = await Client.SendAsync(maliciousRequest);
            using HttpResponseMessage subsequentResponse = await Client.SendAsync(subsequentRequest);

            await AssertScannerBlockedAsync(maliciousResponse);
            await AssertScannerBlockedAsync(subsequentResponse);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAClientIpAddressIsBanned_WhenAnotherClientRequestsASafePath_ThenTheOtherClientIsProcessed()
        {
            ConfigureSuccessfulUpdate();
            using HttpRequestMessage maliciousRequest = new(HttpMethod.Get, "/.env");
            ApiRequestFactory.SetHeader(maliciousRequest, ForwardedForHeaderName, "198.51.100.42");
            using HttpRequestMessage otherClientRequest = CreateValidUpdateRequest();
            ApiRequestFactory.SetHeader(otherClientRequest, ForwardedForHeaderName, "203.0.113.42");

            using HttpResponseMessage maliciousResponse = await Client.SendAsync(maliciousRequest);
            using HttpResponseMessage otherClientResponse = await Client.SendAsync(otherClientRequest);

            await AssertScannerBlockedAsync(maliciousResponse);
            await ApiResponseAssertions.AssertSuccessAsync(otherClientResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAForwardedIpAddressList_WhenItsFirstAddressIsBanned_ThenSubsequentRequestsAreForbidden()
        {
            using HttpRequestMessage maliciousRequest = new(HttpMethod.Get, "/.env");
            ApiRequestFactory.SetHeader(
                maliciousRequest,
                ForwardedForHeaderName,
                "198.51.100.42, 203.0.113.42");
            using HttpRequestMessage subsequentRequest = CreateValidUpdateRequest();
            ApiRequestFactory.SetHeader(subsequentRequest, ForwardedForHeaderName, "198.51.100.42");

            using HttpResponseMessage maliciousResponse = await Client.SendAsync(maliciousRequest);
            using HttpResponseMessage subsequentResponse = await Client.SendAsync(subsequentRequest);

            await AssertScannerBlockedAsync(maliciousResponse);
            await AssertScannerBlockedAsync(subsequentResponse);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenScannerAndProtocolValidationWouldRejectARequest_WhenRequestingAForbiddenPath_ThenScannerProtectionRunsFirst()
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/appsettings.json");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await AssertScannerBlockedAsync(response);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private void ConfigureSuccessfulUpdate()
            => TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    ApiRequestFactory.DomainName,
                    ApiRequestFactory.IpAddress,
                    ApiRequestFactory.Provider))
                .Returns(Task.CompletedTask);

        private void VerifyOneServiceInvocation()
        {
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    ApiRequestFactory.DomainName,
                    ApiRequestFactory.IpAddress,
                    ApiRequestFactory.Provider),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private static HttpRequestMessage CreateValidUpdateRequest()
            => ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);

        private static async Task AssertScannerBlockedAsync(HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(response.Content.Headers.ContentType, Is.Null);
                Assert.That(responseBody, Is.Empty);
            });
        }
    }
}
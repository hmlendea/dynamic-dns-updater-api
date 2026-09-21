using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using DynamicDnsUpdater.API.Requests;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class RequestBodyApiTests : ApiIntegrationTestBase
    {
        private static int LargeValueLength => 8192;

        [TestCase("{")]
        [TestCase("}")]
        [TestCase("{\"ip\":")]
        [TestCase("{\"ip\":\"::1\"")]
        [TestCase("{\"ip\":\"::1\",}")]
        [TestCase("{\"ip\":\"::1\" // comment\n}")]
        [TestCase("{'ip':'::1','provider':'Gandi'}")]
        [TestCase("{ip:\"::1\",provider:\"Gandi\"}")]
        public async Task GivenMalformedJsonSyntax_WhenUpdatingADnsRecord_ThenBadRequestProblemDetailsAreReturned(
            string jsonContent)
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                jsonContent);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase(" ")]
        [TestCase("\t\r\n")]
        [TestCase("null")]
        [TestCase("true")]
        [TestCase("false")]
        [TestCase("42")]
        [TestCase("3.14")]
        [TestCase("\"Gandi\"")]
        [TestCase("[]")]
        [TestCase("[{}]")]
        [TestCase("{\"ip\":42,\"provider\":\"Gandi\"}")]
        [TestCase("{\"ip\":true,\"provider\":\"Gandi\"}")]
        [TestCase("{\"ip\":{},\"provider\":\"Gandi\"}")]
        [TestCase("{\"ip\":[],\"provider\":\"Gandi\"}")]
        [TestCase("{\"ip\":\"::1\",\"provider\":42}")]
        [TestCase("{\"ip\":\"::1\",\"provider\":false}")]
        [TestCase("{\"ip\":\"::1\",\"provider\":{}}")]
        public async Task GivenAnUnbindableJsonValue_WhenUpdatingADnsRecord_ThenBadRequestProblemDetailsAreReturned(
            string jsonContent)
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                jsonContent);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAnEmptyStringBody_WhenUpdatingADnsRecord_ThenBadRequestProblemDetailsAreReturned()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                "");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenNoRequestBody_WhenUpdatingADnsRecord_ThenUnsupportedMediaTypeProblemDetailsAreReturned()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.UnsupportedMediaType);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase(null)]
        [TestCase("text/plain")]
        [TestCase("application/xml")]
        [TestCase("text/xml")]
        [TestCase("application/octet-stream")]
        [TestCase("application/x-www-form-urlencoded")]
        [TestCase("application/javascript")]
        [TestCase("image/png")]
        public async Task GivenAnUnsupportedContentType_WhenUpdatingADnsRecord_ThenUnsupportedMediaTypeProblemDetailsAreReturned(
            string? contentType)
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            request.Content = CreateContent(
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}",
                Encoding.UTF8,
                contentType);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.UnsupportedMediaType);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("application/json")]
        [TestCase("application/json; charset=utf-8")]
        [TestCase("text/json")]
        [TestCase("application/problem+json")]
        [TestCase("application/vnd.api+json")]
        [TestCase("application/vnd.nucilandia.dns-record+json; charset=utf-8")]
        [TestCase("APPLICATION/JSON")]
        public async Task GivenASupportedJsonContentType_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string contentType)
        {
            ConfigureSuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            request.Content = CreateContent(
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}",
                Encoding.UTF8,
                contentType);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifySuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
        }

        [Test]
        public async Task GivenMultipartFormData_WhenUpdatingADnsRecord_ThenBadRequestProblemDetailsAreReturned()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            request.Content = CreateContent(
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}",
                Encoding.UTF8,
                "multipart/form-data; boundary=integration-test");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenUtf16Json_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed()
        {
            ConfigureSuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            request.Content = CreateContent(
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}",
                Encoding.Unicode,
                "application/json; charset=utf-16");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifySuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
        }

        [Test]
        public async Task GivenJsonWithAUtf8ByteOrderMark_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed()
        {
            ConfigureSuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
            string jsonContent = "{\"ip\":\"::1\",\"provider\":\"Gandi\"}";
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
            byte[] contentBytes = new byte[preamble.Length + jsonBytes.Length];
            Buffer.BlockCopy(preamble, 0, contentBytes, 0, preamble.Length);
            Buffer.BlockCopy(jsonBytes, 0, contentBytes, preamble.Length, jsonBytes.Length);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            request.Content = new ByteArrayContent(contentBytes);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifySuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
        }

        [Test]
        public async Task GivenAJsonArrayAsTheProvider_WhenUpdatingADnsRecord_ThenUnsupportedMediaTypeProblemDetailsAreReturned()
        {
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                "{\"ip\":\"::1\",\"provider\":[]}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenReorderedJsonProperties_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed()
        {
            ConfigureSuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                "{\"provider\":\"Gandi\",\"ip\":\"::1\"}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifySuccessfulUpdate(ApiRequestFactory.IpAddress, ApiRequestFactory.Provider);
        }

        [Test]
        public async Task GivenALargeJsonStringValue_WhenUpdatingADnsRecord_ThenTheCompleteValueIsForwarded()
        {
            string largeIpAddress = new('a', LargeValueLength);
            PutDnsRecordRequest requestBody = new()
            {
                IpAddress = largeIpAddress,
                DnsProvider = ApiRequestFactory.Provider
            };
            string jsonContent = JsonSerializer.Serialize(requestBody);
            ConfigureSuccessfulUpdate(largeIpAddress, ApiRequestFactory.Provider);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                jsonContent);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            VerifySuccessfulUpdate(largeIpAddress, ApiRequestFactory.Provider);
        }

        private void ConfigureSuccessfulUpdate(
            string ipAddress,
            string provider)
            => TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    ApiRequestFactory.DomainName,
                    ipAddress,
                    provider))
                .Returns(Task.CompletedTask);

        private void VerifySuccessfulUpdate(
            string ipAddress,
            string provider)
        {
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    ApiRequestFactory.DomainName,
                    ipAddress,
                    provider),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private static HttpContent CreateContent(
            string content,
            Encoding encoding,
            string? contentType)
        {
            ByteArrayContent httpContent = new(encoding.GetBytes(content));

            if (contentType is not null)
            {
                httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }

            return httpContent;
        }
    }
}
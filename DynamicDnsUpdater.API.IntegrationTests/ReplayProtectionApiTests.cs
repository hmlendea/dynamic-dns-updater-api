using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using NuciAPI.Middleware;
using NuciAPI.Responses;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class ReplayProtectionApiTests : ApiIntegrationTestBase
    {
        [Test]
        public async Task GivenAnIdenticalNonce_WhenRepeatingARequest_ThenConflictIsReturned()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = CreateRequest(requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await AssertAlreadyProcessedAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAnIdenticalNonce_WhenChangingTheBody_ThenConflictIsReturned()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                "2001:db8::1",
                "NucilandiaDns");
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await AssertAlreadyProcessedAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAnIdenticalNonce_WhenChangingTheQueryString_ThenConflictIsReturned()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}?source=integration&attempt=613",
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}");
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await AssertAlreadyProcessedAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAnIdenticalNonce_WhenChangingTheHttpMethod_ThenConflictIsReturnedBeforeRouting()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                $"/DnsRecords/{ApiRequestFactory.DomainName}");
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await AssertAlreadyProcessedAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenARejectedCredential_WhenRetryingTheNonceWithValidCredentials_ThenConflictIsReturned()
        {
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            ApiRequestFactory.SetAuthorisation(firstRequest, "Bearer P@ssw0rd!");
            using HttpRequestMessage secondRequest = CreateRequest(requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertErrorAsync(
                firstResponse,
                (int)HttpStatusCode.Unauthorized,
                NuciApiResponseCodes.ErrorCodes.AuthenticationFailure,
                NuciApiResponseMessages.ErrorMessages.AuthenticationFailure);
            await AssertAlreadyProcessedAsync(secondResponse);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenMalformedJson_WhenRetryingTheNonceWithValidJson_ThenConflictIsReturned()
        {
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                $"/DnsRecords/{ApiRequestFactory.DomainName}",
                "{\"ip\":");
            ApiRequestFactory.SetHeader(firstRequest, NuciApiHeaderNames.RequestId, requestId);
            using HttpRequestMessage secondRequest = CreateRequest(requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            await AssertAlreadyProcessedAsync(secondResponse);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAnUnknownRoute_WhenRepeatingTheNonce_ThenConflictIsReturned()
        {
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                "/unknown");
            ApiRequestFactory.SetHeader(firstRequest, NuciApiHeaderNames.RequestId, requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedRequest(
                HttpMethod.Get,
                "/unknown");
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await AssertAlreadyProcessedAsync(secondResponse);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenTheSameRequestIdentifierForDifferentClientIdentifiers_WhenSendingBothRequests_ThenBothAreProcessed()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = CreateRequest(requestId);
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.ClientId, "solaire_of_astora");

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyTwoServiceInvocations();
        }

        [Test]
        public async Task GivenTheSameRequestIdentifierForDifferentPaths_WhenSendingBothRequests_ThenBothAreProcessed()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedPutRequest(
                "home.dummy-domain.com",
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyTwoServiceInvocations();
        }

        [TestCase("/dnsrecords/test.nucilandia.ro")]
        [TestCase("/DNSRECORDS/test.nucilandia.ro")]
        [TestCase("/DnsRecords/test.nucilandia.ro/")]
        public async Task GivenTheSameRequestIdentifierForTextuallyDifferentPaths_WhenSendingBothRequests_ThenBothAreProcessed(
            string secondPath)
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            using HttpRequestMessage secondRequest = ApiRequestFactory.CreateAuthorisedJsonRequest(
                HttpMethod.Put,
                secondPath,
                "{\"ip\":\"::1\",\"provider\":\"Gandi\"}");
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyTwoServiceInvocations();
        }

        [Test]
        public async Task GivenDifferentRequestIdentifiersForTheSamePath_WhenSendingBothRequests_ThenBothAreProcessed()
        {
            ConfigureSuccessfulUpdates();
            using HttpRequestMessage firstRequest = CreateRequest(ApiRequestFactory.CreateRequestId());
            using HttpRequestMessage secondRequest = CreateRequest(ApiRequestFactory.CreateRequestId());

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyTwoServiceInvocations();
        }

        [Test]
        public async Task GivenEquivalentEncodedAndDecodedClientIdentifiers_WhenRepeatingARequest_ThenConflictIsReturned()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            ApiRequestFactory.SetHeader(firstRequest, NuciApiHeaderNames.ClientId, "Ilarion%20Pintilie");
            using HttpRequestMessage secondRequest = CreateRequest(requestId);
            ApiRequestFactory.SetHeader(secondRequest, NuciApiHeaderNames.ClientId, "Ilarion Pintilie");

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertSuccessAsync(firstResponse);
            await AssertAlreadyProcessedAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAnExpiredRequest_WhenRetryingTheNonceWithACurrentTimestamp_ThenTheRetryIsProcessed()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            ApiRequestFactory.SetHeader(
                firstRequest,
                NuciApiHeaderNames.Timestamp,
                ApiRequestFactory.CreateTimestamp(DateTimeOffset.UtcNow.AddMinutes(-6)));
            using HttpRequestMessage secondRequest = CreateRequest(requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertErrorAsync(
                firstResponse,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                "This request has expired and is not acceptable anymore.");
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        [Test]
        public async Task GivenAMissingTimestamp_WhenRetryingTheNonceWithAValidTimestamp_ThenTheRetryIsProcessed()
        {
            ConfigureSuccessfulUpdates();
            string requestId = ApiRequestFactory.CreateRequestId();
            using HttpRequestMessage firstRequest = CreateRequest(requestId);
            firstRequest.Headers.Remove(NuciApiHeaderNames.Timestamp);
            using HttpRequestMessage secondRequest = CreateRequest(requestId);

            using HttpResponseMessage firstResponse = await Client.SendAsync(firstRequest);
            using HttpResponseMessage secondResponse = await Client.SendAsync(secondRequest);

            await ApiResponseAssertions.AssertErrorAsync(
                firstResponse,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                $"The '{NuciApiHeaderNames.Timestamp}' header is missing.");
            await ApiResponseAssertions.AssertSuccessAsync(secondResponse);
            VerifyOneServiceInvocation();
        }

        private void ConfigureSuccessfulUpdates()
            => TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

        private void VerifyOneServiceInvocation()
        {
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private void VerifyTwoServiceInvocations()
        {
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Exactly(2));
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private static HttpRequestMessage CreateRequest(string requestId)
        {
            HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.RequestId, requestId);

            return request;
        }

        private static async Task AssertAlreadyProcessedAsync(HttpResponseMessage response)
            => await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.Conflict,
                NuciApiResponseCodes.ErrorCodes.AlreadyProcessed,
                NuciApiResponseMessages.ErrorMessages.AlreadyProcessed);
    }
}
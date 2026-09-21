using System;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class RequestSecurityApiTests : ApiIntegrationTestBase
    {
        private static string InvalidClientIdentifierMessage
            => $"The '{NuciApiHeaderNames.ClientId}' header contains an invalid client identifier.";

        private static string InvalidRequestIdentifierMessage
            => $"The '{NuciApiHeaderNames.RequestId}' header contains an invalid identifier format.";

        private static string InvalidTimestampMessage
            => $"The '{NuciApiHeaderNames.Timestamp}' header contains an invalid timestamp format.";

        private static string ExpiredRequestMessage
            => "This request has expired and is not acceptable anymore.";

        [TestCaseSource(nameof(GetMissingHeaderCases))]
        public async Task GivenAMissingProtocolHeader_WhenUpdatingADnsRecord_ThenBadRequestIsReturned(
            string headerName,
            string expectedMessage)
        {
            using HttpRequestMessage request = CreateValidRequest();
            request.Headers.Remove(headerName);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                expectedMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase(" ")]
        [TestCase("  ")]
        [TestCase("   ")]
        [TestCase("a")]
        [TestCase("ab")]
        [TestCase("abc")]
        [TestCase("123")]
        public async Task GivenAClientIdentifierShorterThanFourCharacters_WhenUpdatingADnsRecord_ThenBadRequestIsReturned(
            string clientId)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.ClientId, clientId);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                InvalidClientIdentifierMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("abcd")]
        [TestCase("1234")]
        [TestCase("    ")]
        [TestCase("IlarionPintilie")]
        [TestCase("solaire_of_astora")]
        [TestCase("%20%20%20%20")]
        [TestCase("DynamicDnsUpdater.Client.TestyMcTestface")]
        [TestCase("Courage the Cowardly Dog uses an unusually extended client identifier")]
        public async Task GivenAClientIdentifierWithAtLeastFourCharacters_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string clientId)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.ClientId, clientId);

            await AssertRequestSucceedsAsync(request);
        }

        [TestCase(" ")]
        [TestCase("NOT-A-VALID-GUID")]
        [TestCase("550e8400-e29b-41d4-a716-446655440000")]
        [TestCase("550E8400-E29B-41D4-a716-446655440000")]
        [TestCase("550E8400-E29B-41D4-A716-44665544000")]
        [TestCase("550E8400-E29B-41D4-A716-4466554400000")]
        [TestCase("%35%35%30e8400-e29b-41d4-a716-446655440000")]
        public async Task GivenAnInvalidRequestIdentifier_WhenUpdatingADnsRecord_ThenBadRequestIsReturned(
            string requestId)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.RequestId, requestId);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                InvalidRequestIdentifierMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase("550E8400-E29B-41D4-A716-446655440000")]
        [TestCase("550E8400E29B41D4A716446655440000")]
        [TestCase("{550E8400-E29B-41D4-A716-446655440000}")]
        [TestCase("(550E8400-E29B-41D4-A716-446655440000)")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        [TestCase("12345678-1234-1234-1234-123456789012")]
        [TestCase(" 550E8400-E29B-41D4-A716-446655440000")]
        [TestCase("550E8400-E29B-41D4-A716-446655440000 ")]
        [TestCase(" 550E8400-E29B-41D4-A716-446655440000 ")]
        public async Task GivenAValidUppercaseRequestIdentifier_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string requestId)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.RequestId, requestId);

            await AssertRequestSucceedsAsync(request);
        }

        [TestCase(" ")]
        [TestCase("not-a-timestamp")]
        [TestCase("2026-13-21T00:00:00Z")]
        [TestCase("2026-09-32T00:00:00Z")]
        [TestCase("2026-09-21T25:00:00Z")]
        [TestCase("NaN")]
        public async Task GivenAnInvalidTimestamp_WhenUpdatingADnsRecord_ThenBadRequestIsReturned(
            string timestamp)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.Timestamp, timestamp);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                InvalidTimestampMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetEmptyHeaderCases))]
        public async Task GivenAnEmptyProtocolHeader_WhenUpdatingADnsRecord_ThenTheHeaderIsTreatedAsMissing(
            string headerName,
            string expectedMessage)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, headerName, string.Empty);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                expectedMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCaseSource(nameof(GetCurrentTimestampCases))]
        public async Task GivenAParsableCurrentTimestamp_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string timestamp)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.Timestamp, timestamp);

            await AssertRequestSucceedsAsync(request);
        }

        [TestCase(-1440)]
        [TestCase(-60)]
        [TestCase(-6)]
        [TestCase(6)]
        [TestCase(60)]
        [TestCase(1440)]
        public async Task GivenATimestampOutsideTheFiveMinuteWindow_WhenUpdatingADnsRecord_ThenBadRequestIsReturned(
            int minuteOffset)
        {
            using HttpRequestMessage request = CreateValidRequest();
            string timestamp = ApiRequestFactory.CreateTimestamp(
                DateTimeOffset.UtcNow.AddMinutes(minuteOffset));
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.Timestamp, timestamp);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                ExpiredRequestMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [TestCase(-4)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public async Task GivenATimestampWithinTheFiveMinuteWindow_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            int minuteOffset)
        {
            using HttpRequestMessage request = CreateValidRequest();
            string timestamp = ApiRequestFactory.CreateTimestamp(
                DateTimeOffset.UtcNow.AddMinutes(minuteOffset));
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.Timestamp, timestamp);

            await AssertRequestSucceedsAsync(request);
        }

        [Test]
        public async Task GivenLowercaseProtocolHeaderNames_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed()
        {
            using HttpRequestMessage request = CreateValidRequest();
            request.Headers.Remove(NuciApiHeaderNames.ClientId);
            request.Headers.Remove(NuciApiHeaderNames.RequestId);
            request.Headers.Remove(NuciApiHeaderNames.Timestamp);
            request.Headers.TryAddWithoutValidation(
                NuciApiHeaderNames.ClientId.ToLowerInvariant(),
                ApiRequestFactory.ClientId);
            request.Headers.TryAddWithoutValidation(
                NuciApiHeaderNames.RequestId.ToLowerInvariant(),
                ApiRequestFactory.CreateRequestId());
            request.Headers.TryAddWithoutValidation(
                NuciApiHeaderNames.Timestamp.ToLowerInvariant(),
                ApiRequestFactory.CreateTimestamp());

            await AssertRequestSucceedsAsync(request);
        }

        [TestCase("NucileRullz!")]
        [TestCase("Bearer NucileRullz!")]
        [TestCase("bearer NucileRullz!")]
        [TestCase("BEARER NucileRullz!")]
        [TestCase("Bearer  NucileRullz!")]
        [TestCase("Bearer\tNucileRullz!")]
        [TestCase("  Bearer NucileRullz!  ")]
        public async Task GivenAnAcceptedAuthorisationValue_WhenUpdatingADnsRecord_ThenTheRequestIsProcessed(
            string authorisation)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetAuthorisation(request, authorisation);

            await AssertRequestSucceedsAsync(request);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("P@ssw0rd!")]
        [TestCase("nucilerullz!")]
        [TestCase("Bearer")]
        [TestCase("Bearer ")]
        [TestCase("Bearer P@ssw0rd!")]
        [TestCase("BearerNucileRullz!")]
        [TestCase("Basic NucileRullz!")]
        [TestCase("ApiKey NucileRullz!")]
        public async Task GivenAnInvalidAuthorisationValue_WhenUpdatingADnsRecord_ThenUnauthorisedIsReturned(
            string? authorisation)
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetAuthorisation(request, authorisation);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.Unauthorized,
                NuciApiResponseCodes.ErrorCodes.AuthenticationFailure,
                NuciApiResponseMessages.ErrorMessages.AuthenticationFailure);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenEveryProtocolHeaderIsMissing_WhenUpdatingADnsRecord_ThenClientIdentifierValidationRunsFirst()
        {
            using HttpRequestMessage request = CreateValidRequest();
            request.Headers.Remove(NuciApiHeaderNames.ClientId);
            request.Headers.Remove(NuciApiHeaderNames.RequestId);
            request.Headers.Remove(NuciApiHeaderNames.Timestamp);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                $"The '{NuciApiHeaderNames.ClientId}' header is missing.");
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAnInvalidRequestIdentifierAndTimestamp_WhenUpdatingADnsRecord_ThenRequestIdentifierValidationRunsFirst()
        {
            using HttpRequestMessage request = CreateValidRequest();
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.RequestId, "NOT-A-VALID-GUID");
            ApiRequestFactory.SetHeader(request, NuciApiHeaderNames.Timestamp, "not-a-timestamp");

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                (int)HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                InvalidRequestIdentifierMessage);
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private async Task AssertRequestSucceedsAsync(HttpRequestMessage request)
        {
            TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertSuccessAsync(response);
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    ApiRequestFactory.DomainName,
                    ApiRequestFactory.IpAddress,
                    ApiRequestFactory.Provider),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private static HttpRequestMessage CreateValidRequest()
            => ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);

        private static IEnumerable<TestCaseData> GetMissingHeaderCases()
        {
            yield return new TestCaseData(
                NuciApiHeaderNames.ClientId,
                $"The '{NuciApiHeaderNames.ClientId}' header is missing.");
            yield return new TestCaseData(
                NuciApiHeaderNames.RequestId,
                $"The '{NuciApiHeaderNames.RequestId}' header is missing.");
            yield return new TestCaseData(
                NuciApiHeaderNames.Timestamp,
                $"The '{NuciApiHeaderNames.Timestamp}' header is missing.");
        }

        private static IEnumerable<TestCaseData> GetCurrentTimestampCases()
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;

            yield return new TestCaseData(
                utcNow.ToString("O", CultureInfo.InvariantCulture));
            yield return new TestCaseData(
                utcNow.ToOffset(TimeSpan.FromHours(3)).ToString("O", CultureInfo.InvariantCulture));
            yield return new TestCaseData(
                utcNow.ToOffset(TimeSpan.FromHours(-7)).ToString("O", CultureInfo.InvariantCulture));
            yield return new TestCaseData(
                utcNow.ToString("R", CultureInfo.InvariantCulture));
            yield return new TestCaseData(
                utcNow.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
        }

        private static IEnumerable<TestCaseData> GetEmptyHeaderCases()
            => GetMissingHeaderCases();
    }
}
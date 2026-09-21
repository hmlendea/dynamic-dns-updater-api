using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Authentication;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Moq;

using NuciAPI.Middleware.Security;
using NuciAPI.Responses;

using NuciDAL.Repositories;

using NUnit.Framework;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class ExceptionHandlingApiTests : ApiIntegrationTestBase
    {
        [TestCaseSource(nameof(GetExceptionCases))]
        public async Task GivenAServiceException_WhenUpdatingADnsRecord_ThenTheMappedErrorResponseIsReturned(
            Exception exception,
            int expectedStatusCode,
            string expectedCode,
            string expectedMessage)
        {
            TestHost.DnsRecordServiceMock
                .Setup(service => service.Update(
                    ApiRequestFactory.DomainName,
                    ApiRequestFactory.IpAddress,
                    ApiRequestFactory.Provider))
                .ThrowsAsync(exception);
            using HttpRequestMessage request = ApiRequestFactory.CreateAuthorisedPutRequest(
                ApiRequestFactory.DomainName,
                ApiRequestFactory.IpAddress,
                ApiRequestFactory.Provider);

            using HttpResponseMessage response = await Client.SendAsync(request);

            await ApiResponseAssertions.AssertErrorAsync(
                response,
                expectedStatusCode,
                expectedCode,
                expectedMessage);
            TestHost.DnsRecordServiceMock.Verify(
                service => service.Update(
                    ApiRequestFactory.DomainName,
                    ApiRequestFactory.IpAddress,
                    ApiRequestFactory.Provider),
                Times.Once());
            TestHost.DnsRecordServiceMock.VerifyNoOtherCalls();
        }

        private static IEnumerable<TestCaseData> GetExceptionCases()
        {
            yield return CreateExposedBadRequestCase(
                new BadHttpRequestException("The HTTP request is invalid."),
                "BadHttpRequestException");
            yield return CreateExposedBadRequestCase(
                new ArgumentException("The domain argument is invalid.", "domainName"),
                "ArgumentException");
            yield return CreateExposedBadRequestCase(
                new ArgumentNullException("ipAddress", "The IP address is absent."),
                "ArgumentNullException");
            yield return CreateExposedBadRequestCase(
                new FormatException("The domain format is invalid."),
                "FormatException");
            yield return CreateExposedBadRequestCase(
                new ValidationException("The request values are invalid."),
                "ValidationException");
            yield return CreateStandardCase(
                new SecurityException("The security policy rejected the request."),
                HttpStatusCode.Forbidden,
                NuciApiResponseCodes.ErrorCodes.Unauthorised,
                NuciApiResponseMessages.ErrorMessages.Unauthorised,
                "SecurityException");
            yield return CreateStandardCase(
                new UnauthorizedAccessException("The caller has insufficient access."),
                HttpStatusCode.Forbidden,
                NuciApiResponseCodes.ErrorCodes.Unauthorised,
                NuciApiResponseMessages.ErrorMessages.Unauthorised,
                "UnauthorizedAccessException");
            yield return CreateStandardCase(
                new HttpRequestException("The Gandi dependency is unavailable."),
                HttpStatusCode.ServiceUnavailable,
                NuciApiResponseCodes.ErrorCodes.ServiceDependencyUnavailable,
                NuciApiResponseMessages.ErrorMessages.ServiceDependencyUnavailable,
                "HttpRequestException");
            yield return CreateStandardCase(
                new TaskCanceledException("The Gandi dependency request was cancelled."),
                HttpStatusCode.ServiceUnavailable,
                NuciApiResponseCodes.ErrorCodes.ServiceDependencyUnavailable,
                NuciApiResponseMessages.ErrorMessages.ServiceDependencyUnavailable,
                "TaskCanceledException");
            yield return CreateStandardCase(
                new TimeoutException("The Gandi dependency timed out."),
                HttpStatusCode.ServiceUnavailable,
                NuciApiResponseCodes.ErrorCodes.ServiceDependencyUnavailable,
                NuciApiResponseMessages.ErrorMessages.ServiceDependencyUnavailable,
                "TimeoutException");
            yield return CreateStandardCase(
                new AuthenticationException("The API key is invalid."),
                HttpStatusCode.Unauthorized,
                NuciApiResponseCodes.ErrorCodes.AuthenticationFailure,
                NuciApiResponseMessages.ErrorMessages.AuthenticationFailure,
                "AuthenticationException");
            yield return CreateStandardCase(
                new EntityNotFoundException("613", "DnsRecord"),
                HttpStatusCode.NotFound,
                NuciApiResponseCodes.ErrorCodes.NotFound,
                NuciApiResponseMessages.ErrorMessages.NotFound,
                "EntityNotFoundException");
            yield return CreateStandardCase(
                new KeyNotFoundException("The DNS record was not found."),
                HttpStatusCode.NotFound,
                NuciApiResponseCodes.ErrorCodes.NotFound,
                NuciApiResponseMessages.ErrorMessages.NotFound,
                "KeyNotFoundException");
            yield return CreateStandardCase(
                new EntityAlreadyExistsException("613", "DnsRecord"),
                HttpStatusCode.Conflict,
                NuciApiResponseCodes.ErrorCodes.AlreadyExists,
                NuciApiResponseMessages.ErrorMessages.AlreadyExists,
                "EntityAlreadyExistsException");
            yield return CreateStandardCase(
                new RequestAlreadyProcessedException("550E8400-E29B-41D4-A716-446655440000"),
                HttpStatusCode.Conflict,
                NuciApiResponseCodes.ErrorCodes.AlreadyProcessed,
                NuciApiResponseMessages.ErrorMessages.AlreadyProcessed,
                "RequestAlreadyProcessedException");

            NotImplementedException notImplementedException =
                new("The Nucilandia DNS provider is not implemented.");
            yield return CreateStandardCase(
                notImplementedException,
                HttpStatusCode.NotImplemented,
                NuciApiResponseCodes.ErrorCodes.NotImplemented,
                notImplementedException.Message,
                "NotImplementedException");

            yield return CreateStandardCase(
                new OperationCanceledException("The client closed the request."),
                (HttpStatusCode)StatusCodes.Status499ClientClosedRequest,
                NuciApiResponseCodes.ErrorCodes.ClientClosedTheRequest,
                NuciApiResponseMessages.ErrorMessages.ClientClosedTheRequest,
                "OperationCanceledException");
            yield return CreateInternalServerErrorCase(
                new Exception("An unclassified failure occurred."),
                "Exception");
            yield return CreateInternalServerErrorCase(
                new InvalidOperationException("The service entered an invalid state."),
                "InvalidOperationException");
            yield return CreateInternalServerErrorCase(
                new NullReferenceException("The provider resolution returned null."),
                "NullReferenceException");
        }

        private static TestCaseData CreateExposedBadRequestCase(
            Exception exception,
            string exceptionName)
            => CreateStandardCase(
                exception,
                HttpStatusCode.BadRequest,
                NuciApiResponseCodes.ErrorCodes.BadRequest,
                exception.Message,
                exceptionName);

        private static TestCaseData CreateInternalServerErrorCase(
            Exception exception,
            string exceptionName)
            => CreateStandardCase(
                exception,
                HttpStatusCode.InternalServerError,
                NuciApiResponseCodes.ErrorCodes.InternalServerError,
                NuciApiResponseMessages.ErrorMessages.InternalServerError,
                exceptionName);

        private static TestCaseData CreateStandardCase(
            Exception exception,
            HttpStatusCode expectedStatusCode,
            string expectedCode,
            string expectedMessage,
            string exceptionName)
            => new TestCaseData(
                exception,
                (int)expectedStatusCode,
                expectedCode,
                expectedMessage)
                .SetName(
                    $"Given{exceptionName}_WhenUpdatingADnsRecord_ThenTheMappedErrorResponseIsReturned");
    }
}
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

using NuciAPI.Responses;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    internal static class ApiResponseAssertions
    {
        internal static async Task AssertSuccessAsync(HttpResponseMessage response)
        {
            NuciApiSuccessResponse? responseBody =
                await response.Content.ReadFromJsonAsync<NuciApiSuccessResponse>();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
                Assert.That(responseBody, Is.Not.Null);
                Assert.That(responseBody!.IsSuccessful);
                Assert.That(responseBody.Code, Is.EqualTo(NuciApiResponseCodes.SuccessCodes.Default));
                Assert.That(responseBody.Message, Is.EqualTo(NuciApiResponseMessages.SuccessMessages.Default));
            });
        }

        internal static async Task AssertErrorAsync(
            HttpResponseMessage response,
            int expectedStatusCode,
            string expectedCode,
            string expectedMessage)
        {
            NuciApiErrorResponse? responseBody =
                await response.Content.ReadFromJsonAsync<NuciApiErrorResponse>();

            Assert.Multiple(() =>
            {
                Assert.That((int)response.StatusCode, Is.EqualTo(expectedStatusCode));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
                Assert.That(responseBody, Is.Not.Null);
                Assert.That(responseBody!.IsSuccessful, Is.False);
                Assert.That(responseBody.Code, Is.EqualTo(expectedCode));
                Assert.That(responseBody.Message, Is.EqualTo(expectedMessage));
            });
        }

        internal static async Task AssertProblemDetailsAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(expectedStatusCode));
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
                Assert.That(root.GetProperty("status").GetInt32(), Is.EqualTo((int)expectedStatusCode));
                Assert.That(root.GetProperty("title").GetString(), Is.Not.Empty);
                Assert.That(root.GetProperty("traceId").GetString(), Is.Not.Empty);
            });
        }
    }
}
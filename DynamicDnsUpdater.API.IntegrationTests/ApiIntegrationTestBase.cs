using System.Net.Http;

using NUnit.Framework;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    public abstract class ApiIntegrationTestBase
    {
        protected ApiTestHost TestHost { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        [SetUp]
        protected void SetUp()
        {
            TestHost = new ApiTestHost();
            Client = TestHost.CreateHttpsClient();
        }

        [TearDown]
        protected void TearDown()
        {
            Client.Dispose();
            TestHost.Dispose();
        }
    }
}
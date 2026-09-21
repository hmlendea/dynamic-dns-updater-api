using System;
using System.Collections.Generic;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using NuciLog.Core;

using DynamicDnsUpdater.API.Service;

namespace DynamicDnsUpdater.API.IntegrationTests
{
    public sealed class ApiTestHost : WebApplicationFactory<Program>
    {
        internal static string ApiKey => "NucileRullz!";

        internal Mock<IDnsRecordService> DnsRecordServiceMock { get; } = new(MockBehavior.Strict);

        internal Mock<ILogger> LoggerMock { get; } = new(MockBehavior.Loose);

        internal HttpClient CreateHttpsClient()
        {
            WebApplicationFactoryClientOptions options = new()
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            };
            HttpClient client = CreateClient(options);
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                Dictionary<string, string?> configurationValues = new()
                {
                    ["securitySettings:apiKey"] = ApiKey,
                    ["gandiSettings:apiKey"] = "TestPassword!",
                    ["nuciLoggerSettings:logFilePath"] = "dynamic-dns-updater-api-integration-tests.log",
                    ["nuciLoggerSettings:isFileOutputEnabled"] = "false"
                };

                configurationBuilder.AddInMemoryCollection(configurationValues);
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDnsRecordService>();
                services.AddSingleton(DnsRecordServiceMock.Object);
                services.RemoveAll<ILogger>();
                services.AddSingleton(LoggerMock.Object);
            });
        }
    }
}
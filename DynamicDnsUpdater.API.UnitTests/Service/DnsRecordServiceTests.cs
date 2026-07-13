using System;
using System.Net.Http;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using DynamicDnsUpdater.API.Service;
using DynamicDnsUpdater.API.Service.Integrations.Gandi;
using NuciLog.Core;

namespace DynamicDnsUpdater.API.UnitTests.Service
{
    [TestFixture]
    public class DnsRecordServiceTests
    {
        Mock<IGandiService> gandiServiceMock;
        Mock<ILogger> loggerMock;
        DnsRecordService service;

        [SetUp]
        public void SetUp()
        {
            gandiServiceMock = new Mock<IGandiService>();
            loggerMock = new Mock<ILogger>();
            service = new DnsRecordService(gandiServiceMock.Object, loggerMock.Object);
        }

        // ── Update ─────────────────────────────────────────────────────

        [Test]
        public async Task GivenGandiProvider_WhenUpdateIsCalled_ThenCallsGandiServiceUpdate()
        {
            // IPv6 address causes IsAlreadyResolvedToRequestedIpAddress to return false
            // without making any real DNS queries, so execution reaches gandiService.Update.
            await service.Update("sub.nucilandia.ro", "::1", "Gandi");

            gandiServiceMock.Verify(
                gandiService => gandiService.Update("sub.nucilandia.ro", "::1"),
                Times.Once());
        }

        [Test]
        public void GivenGandiServiceThrows_WhenUpdateIsCalled_ThenRethrowsException()
        {
            gandiServiceMock
                .Setup(gandiService => gandiService.Update(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("The HTTP call to the Gandi API has failed."));

            Assert.That(
                async () => await service.Update("sub.nucilandia.ro", "::1", "Gandi"),
                Throws.InstanceOf<HttpRequestException>());
        }

        [Test]
        public void GivenUnknownProvider_WhenUpdateIsCalled_ThenThrowsException()
        {
            // DnsProvider.FromString returns null for unknown names.
            // The null == DnsProvider.Gandi comparison inside Update then throws.
            Assert.That(
                async () => await service.Update("sub.nucilandia.ro", "::1", "NucilandiaDns"),
                Throws.InstanceOf<Exception>());
        }
    }
}

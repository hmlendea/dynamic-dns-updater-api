using System;
using System.Threading.Tasks;

using NUnit.Framework;

using DynamicDnsUpdater.API.Configuration;
using DynamicDnsUpdater.API.Service.Integrations.Gandi;

namespace DynamicDnsUpdater.API.UnitTests.Service.Integrations.Gandi
{
    [TestFixture]
    public class GandiServiceTests
    {
        GandiService service;

        [SetUp]
        public void SetUp()
        {
            GandiSettings gandiSettings = new() { ApiKey = "test-api-key-613" };
            service = new GandiService(gandiSettings);
        }

        // ── Update ─────────────────────────────────────────────────────

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void GivenBlankDomainName_WhenCallingUpdate_ThenThrowsArgumentException(string domainName)
        {
            Assert.That(
                async () => await service.Update(domainName, "192.168.1.1"),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void GivenBlankIpAddress_WhenCallingUpdate_ThenThrowsArgumentException(string ipAddress)
        {
            Assert.That(
                async () => await service.Update("sub.nucilandia.ro", ipAddress),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("notadomain")]
        [TestCase("invalid@domain.ro")]
        [TestCase("spaces in domain.ro")]
        public void GivenInvalidDomainFormat_WhenCallingUpdate_ThenThrowsFormatException(string domainName)
        {
            Assert.That(
                async () => await service.Update(domainName, "192.168.1.1"),
                Throws.InstanceOf<FormatException>());
        }

        [Test]
        public void GivenDomainWithoutSubdomain_WhenCallingUpdate_ThenThrowsFormatException()
        {
            Assert.That(
                async () => await service.Update("nucilandia.ro", "192.168.1.1"),
                Throws.InstanceOf<FormatException>());
        }
    }
}

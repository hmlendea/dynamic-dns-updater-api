using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using DynamicDnsUpdater.API.Service.Models;

namespace DynamicDnsUpdater.API.UnitTests.Service.Models
{
    [TestFixture]
    public class DnsProviderTests
    {
        // ── FromString ─────────────────────────────────────────────────

        [Test]
        public void GivenGandiName_WhenCallingFromString_ThenReturnsGandiProvider()
        {
            DnsProvider result = DnsProvider.FromString("Gandi");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(DnsProvider.Gandi));
        }

        [Test]
        public void GivenUnknownName_WhenCallingFromString_ThenReturnsNull()
        {
            DnsProvider result = DnsProvider.FromString("NucilandiaDns");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GivenLowercaseName_WhenCallingFromString_ThenReturnsNull()
        {
            DnsProvider result = DnsProvider.FromString("gandi");

            Assert.That(result, Is.Null);
        }

        // ── ToString ───────────────────────────────────────────────────

        [Test]
        public void GivenGandiProvider_WhenCallingToString_ThenReturnsGandiName()
        {
            string result = DnsProvider.Gandi.ToString();

            Assert.That(result, Is.EqualTo("Gandi"));
        }

        // ── GetValues ──────────────────────────────────────────────────

        [Test]
        public void GivenProviders_WhenCallingGetValues_ThenReturnsOneElement()
        {
            Array result = DnsProvider.GetValues();

            Assert.That(result, Has.Length.EqualTo(1));
        }

        [Test]
        public void GivenProviders_WhenCallingGetValues_ThenContainsGandiProvider()
        {
            IEnumerable<DnsProvider> result = DnsProvider.GetValues().Cast<DnsProvider>();

            Assert.That(result.Contains(DnsProvider.Gandi));
        }

        // ── Equals ─────────────────────────────────────────────────────

        [Test]
        public void GivenSameProviders_WhenCallingTypedEquals_ThenReturnsTrue()
        {
            DnsProvider provider = DnsProvider.FromString("Gandi");

            Assert.That(DnsProvider.Gandi.Equals(provider));
        }

        [Test]
        public void GivenNullDnsProvider_WhenCallingTypedEquals_ThenReturnsFalse()
        {
            bool result = DnsProvider.Gandi.Equals((DnsProvider)null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GivenSameReference_WhenCallingObjectEquals_ThenReturnsTrue()
        {
            DnsProvider provider = DnsProvider.Gandi;

            Assert.That(provider.Equals((object)provider));
        }

        [Test]
        public void GivenNullObject_WhenCallingObjectEquals_ThenReturnsFalse()
        {
            bool result = DnsProvider.Gandi.Equals((object)null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GivenDifferentTypeObject_WhenCallingObjectEquals_ThenReturnsFalse()
        {
            bool result = DnsProvider.Gandi.Equals((object)"Gandi");

            Assert.That(result, Is.False);
        }

        // ── Equality Operators ─────────────────────────────────────────

        [Test]
        public void GivenSameProviders_WhenUsingEqualityOperator_ThenReturnsTrue()
        {
            DnsProvider providerA = DnsProvider.FromString("Gandi");
            DnsProvider providerB = DnsProvider.FromString("Gandi");

            Assert.That(providerA == providerB);
        }

        [Test]
        public void GivenSameProviders_WhenUsingInequalityOperator_ThenReturnsFalse()
        {
            DnsProvider providerA = DnsProvider.FromString("Gandi");
            DnsProvider providerB = DnsProvider.FromString("Gandi");

            bool result = providerA != providerB;

            Assert.That(result, Is.False);
        }

        // ── Implicit Conversion ────────────────────────────────────────

        [Test]
        public void GivenGandiProvider_WhenConvertingImplicitlyToString_ThenReturnsGandiName()
        {
            string result = DnsProvider.Gandi;

            Assert.That(result, Is.EqualTo("Gandi"));
        }

        // ── GetHashCode ────────────────────────────────────────────────

        [Test]
        public void GivenSameProviders_WhenCallingGetHashCode_ThenReturnsSameValue()
        {
            DnsProvider providerA = DnsProvider.FromString("Gandi");
            DnsProvider providerB = DnsProvider.FromString("Gandi");

            Assert.That(providerA.GetHashCode(), Is.EqualTo(providerB.GetHashCode()));
        }
    }
}

using System.Linq;
using System.Text.RegularExpressions;

using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DynamicDnsUpdater.API.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyBoundaryTests
    {
        [TestCase("DynamicDnsUpdater.API.Controllers", "DynamicDnsUpdater.API.Service.Integrations")]
        [TestCase("DynamicDnsUpdater.API.Controllers", "DnsClient")]
        [TestCase("DynamicDnsUpdater.API.Controllers", "System.Net.Http")]
        [TestCase("DynamicDnsUpdater.API.Controllers", "Microsoft.EntityFrameworkCore")]
        [TestCase("DynamicDnsUpdater.API.Controllers", "Amazon.DynamoDBv2")]
        [TestCase("DynamicDnsUpdater.API.Requests", "DynamicDnsUpdater.API.Service")]
        [TestCase("DynamicDnsUpdater.API.Requests", "DynamicDnsUpdater.API.Configuration")]
        [TestCase("DynamicDnsUpdater.API.Requests", "System.Net.Http")]
        [TestCase("DynamicDnsUpdater.API.Service", "DynamicDnsUpdater.API.Controllers")]
        [TestCase("DynamicDnsUpdater.API.Service", "DynamicDnsUpdater.API.Requests")]
        [TestCase("DynamicDnsUpdater.API.Service.Integrations", "DynamicDnsUpdater.API.Controllers")]
        [TestCase("DynamicDnsUpdater.API.Service.Integrations", "DynamicDnsUpdater.API.Requests")]
        [TestCase("DynamicDnsUpdater.API.Service.Models", "DynamicDnsUpdater.API.Service.Integrations")]
        [TestCase("DynamicDnsUpdater.API.Service.Models", "DynamicDnsUpdater.API.Configuration")]
        [TestCase("DynamicDnsUpdater.API.Configuration", "DynamicDnsUpdater.API.Service")]
        [TestCase("DynamicDnsUpdater.API.Logging", "DynamicDnsUpdater.API.Service")]
        public void GivenABoundary_WhenInspectingDependencies_ThenTheBoundaryIsPreserved(
            string sourceNamespace,
            string forbiddenNamespace)
        {
            IObjectProvider<IType> sourceTypes = Types().That()
                .ResideInNamespaceMatching($"^{Regex.Escape(sourceNamespace)}(\\.|$)");

            Assert.That(
                sourceTypes.GetObjects(ProductionArchitecture.Model),
                Is.Not.Empty,
                $"The {sourceNamespace} boundary must select production types.");

            Types().That().Are(sourceTypes)
                .Should().NotDependOnAny(Types().That()
                    .ResideInNamespaceMatching($"^{Regex.Escape(forbiddenNamespace)}(\\.|$)"))
                .Because($"{sourceNamespace} must remain independent of {forbiddenNamespace}")
                .Check(ProductionArchitecture.Model);
        }

        [TestCase("DynamicDnsUpdater.API.Service.DnsRecordService")]
        [TestCase("DynamicDnsUpdater.API.Service.Integrations.Gandi.GandiService")]
        public void GivenControllers_WhenInspectingDependencies_ThenConcreteServicesAreNotUsed(
            string implementationName)
        {
            IObjectProvider<IType> controllers = Types().That()
                .ResideInNamespaceMatching(@"^DynamicDnsUpdater\.API\.Controllers(\.|$)");
            IObjectProvider<IType> implementations = Types().That().HaveFullName(implementationName);

            Assert.That(controllers.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);
            Assert.That(implementations.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);

            Types().That().Are(controllers).Should().NotDependOnAny(implementations)
                .Because("Controllers must consume service contracts rather than implementations")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenTheProviderAdapter_WhenInspectingDependencies_ThenOrchestrationIsNotReferenced()
        {
            IObjectProvider<IType> adapters = Types().That()
                .ResideInNamespaceMatching(@"^DynamicDnsUpdater\.API\.Service\.Integrations(\.|$)");
            IObjectProvider<IType> orchestration = Types().That()
                .ResideInNamespace("DynamicDnsUpdater.API.Service");

            Assert.That(adapters.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);
            Assert.That(orchestration.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);

            Types().That().Are(adapters).Should().NotDependOnAny(orchestration)
                .Because("Provider adapters must not depend back on application orchestration")
                .Check(ProductionArchitecture.Model);
        }

        [TestCase("DynamicDnsUpdater.API.Service.Integrations.Gandi.GandiService")]
        [TestCase("System.Net.Http.HttpClient")]
        public void GivenOrchestration_WhenInspectingDependencies_ThenProviderDetailsAreNotReferenced(
            string forbiddenTypeName)
        {
            IObjectProvider<IType> orchestration = Types().That()
                .ResideInNamespace("DynamicDnsUpdater.API.Service");

            Assert.That(orchestration.GetObjects(ProductionArchitecture.Model), Is.Not.Empty);

            Types().That().Are(orchestration)
                .Should().NotDependOnAny(Types().That().HaveFullName(forbiddenTypeName))
                .Because("Application orchestration must use provider contracts, not HTTP implementations")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionAssemblies_WhenLoadingArchitecture_ThenTestTypesAreExcluded()
            => Assert.That(
                ProductionArchitecture.Model.Types.Select(type => type.FullName),
                Is.Not.Empty.And.None.Contains("Tests"));
    }
}
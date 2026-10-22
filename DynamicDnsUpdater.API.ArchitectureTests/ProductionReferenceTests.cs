using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using NUnit.Framework;

namespace DynamicDnsUpdater.API.ArchitectureTests
{
    [TestFixture]
    public sealed class ProductionReferenceTests
    {
        [Test]
        public void GivenTheProductionProject_WhenInspectingReferences_ThenTestProjectsAreNotReferenced()
        {
            XDocument project = XDocument.Load(
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Production.csproj"));
            string[] references = project.Descendants()
                .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference"
                    or "Reference")
                .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
                .ToArray();

            Assert.That(references, Is.Not.Empty, "The production reference check must inspect references.");
            Assert.That(
                references.Where(IsTestReference),
                Is.Empty,
                "Production project references must not depend on test projects or test frameworks.");
        }

        [Test]
        public void GivenTheProductionAssembly_WhenInspectingReferences_ThenTestAssembliesAreNotReferenced()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty);
            Assert.That(
                references.Select(reference => reference.Name ?? string.Empty).Where(IsTestReference),
                Is.Empty,
                "Production assembly references must remain independent of testing infrastructure.");
        }

        [Test]
        public void GivenProductionCode_WhenInspectingVisibility_ThenInternalsAreNotExposed()
        {
            XDocument project = XDocument.Load(
                Path.Combine(TestContext.CurrentContext.TestDirectory, "Production.csproj"));

            Assert.That(
                project.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "InternalsVisibleTo", StringComparison.Ordinal) ||
                    ((string?)element.Attribute("Include"))?.Contains(
                        nameof(InternalsVisibleToAttribute), StringComparison.Ordinal) is true),
                Is.Empty,
                "Production projects must not declare InternalsVisibleTo.");
            Assert.That(
                ProductionArchitecture.Assembly.GetCustomAttributes<InternalsVisibleToAttribute>(),
                Is.Empty,
                "Production assemblies must not expose internals to other assemblies.");
        }

        private static bool IsTestReference(string reference)
            => reference.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("TestHost", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase);
    }
}
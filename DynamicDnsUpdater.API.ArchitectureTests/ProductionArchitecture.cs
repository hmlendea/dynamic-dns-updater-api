using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using DynamicDnsUpdater.API.Service;

namespace DynamicDnsUpdater.API.ArchitectureTests
{
    internal static class ProductionArchitecture
    {
        internal static System.Reflection.Assembly Assembly => typeof(IDnsRecordService).Assembly;

        internal static Architecture Model { get; } = new ArchLoader()
            .LoadAssemblies(Assembly)
            .Build();
    }
}
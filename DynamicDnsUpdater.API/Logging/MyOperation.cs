using NuciLog.Core;

namespace DynamicDnsUpdater.API.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name) : base(name) { }

        public static Operation UpdateDnsRecord => new MyOperation(nameof(UpdateDnsRecord));
    }
}

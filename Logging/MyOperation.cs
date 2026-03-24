using NuciLog.Core;

namespace ExonymsAPI.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name) : base(name) { }

        public static Operation UpdateDnsRecord => new MyOperation(nameof(UpdateDnsRecord));
    }
}

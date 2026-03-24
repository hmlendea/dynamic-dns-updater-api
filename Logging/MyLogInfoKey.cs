using NuciLog.Core;

namespace DynamicDnsUpdater.API.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name) : base(name) { }

        public static LogInfoKey DomainName => new MyLogInfoKey(nameof(DomainName));

        public static LogInfoKey IpAddress => new MyLogInfoKey(nameof(IpAddress));

        public static LogInfoKey Provider => new MyLogInfoKey(nameof(Provider));
    }
}

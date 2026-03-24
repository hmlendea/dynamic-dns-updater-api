using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicDnsUpdater.API.Service.Models
{
    public class DnsProvider : IEquatable<DnsProvider>
    {
        static readonly Dictionary<string, DnsProvider> values = new()
        {
            { nameof(Gandi), new DnsProvider(nameof(Gandi)) },
        };

        public string Name { get; }

        DnsProvider(string name)
        {
            Name = name;
        }

        public static DnsProvider Gandi => values[nameof(Gandi)];

        public static Array GetValues() => values.Values.ToArray();

        public bool Equals(DnsProvider other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((DnsProvider)obj);
        }

        public override int GetHashCode() => $"{nameof(DnsProvider)}:{Name}".GetHashCode();

        public override string ToString() => Name;

        public static DnsProvider FromString(string name)
        {
            if (!values.ContainsKey(name))
            {
                return null;
            }

            return values[name];
        }

        public static bool operator ==(DnsProvider current, DnsProvider other) => current.Equals(other);

        public static bool operator !=(DnsProvider current, DnsProvider other) => !current.Equals(other);

        public static implicit operator string(DnsProvider provider) => provider.Name;
    }
}

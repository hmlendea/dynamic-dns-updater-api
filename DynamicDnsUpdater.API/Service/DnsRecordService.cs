using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DnsClient;
using DynamicDnsUpdater.API.Logging;
using DynamicDnsUpdater.API.Service.Integrations.Gandi;
using DynamicDnsUpdater.API.Service.Models;
using NuciLog.Core;

namespace DynamicDnsUpdater.API.Service
{
    public class DnsRecordService(
        IGandiService gandiService,
        ILogger logger)
        : IDnsRecordService
    {
        static readonly LookupClient Quad9LookupClient = new(new LookupClientOptions(
            [
                new NameServer(IPAddress.Parse("9.9.9.9")),
                new NameServer(IPAddress.Parse("149.112.112.112"))
            ])
        {
            UseCache = false
        });

        public async Task Update(
            string domainName,
            string ipAddress,
            string dnsProviderName)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Provider, dnsProviderName),
                new(MyLogInfoKey.DomainName, domainName),
                new(MyLogInfoKey.IpAddress, ipAddress)
            ];

            logger.Info(
                MyOperation.UpdateDnsRecord,
                OperationStatus.Started,
                logInfos);

            if (await IsAlreadyResolvedToRequestedIpAddress(domainName, ipAddress))
            {
                logger.Info(
                    MyOperation.UpdateDnsRecord,
                    OperationStatus.Success,
                    "The DNS record already points to the requested IP address.",
                    logInfos);

                return;
            }

            DnsProvider provider = DnsProvider.FromString(dnsProviderName);

            try
            {
                if (provider == DnsProvider.Gandi)
                {
                    await gandiService.Update(domainName, ipAddress);
                }
                else
                {
                    throw new NotImplementedException($"The requested DNS provider is not supported.");
                }

                logger.Info(
                    MyOperation.UpdateDnsRecord,
                    OperationStatus.Success,
                    logInfos);
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.UpdateDnsRecord,
                    OperationStatus.Failure,
                    exception,
                    logInfos);

                throw;
            }
        }

        static async Task<bool> IsAlreadyResolvedToRequestedIpAddress(
            string domainName,
            string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress requestedAddress) ||
                requestedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            try
            {
                IDnsQueryResponse response = await Quad9LookupClient.QueryAsync(domainName, QueryType.A);

                return response.Answers
                    .ARecords()
                    .Any(record => record.Address.Equals(requestedAddress));
            }
            catch (DnsResponseException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}

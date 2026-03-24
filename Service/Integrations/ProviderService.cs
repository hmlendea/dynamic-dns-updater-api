using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDnsUpdater.API.Logging;
using NuciLog.Core;

namespace DynamicDnsUpdater.API.Service.Integrations.Gandi
{
    public abstract class ProviderService(ILogger logger)
    {
        public abstract string ProviderName { get; }

        public async Task Update(
            string domainName,
            string ipAddress)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Provider, ProviderName),
                new(MyLogInfoKey.DomainName, domainName),
                new(MyLogInfoKey.IpAddress, ipAddress)
            ];

            logger.Info(
                MyOperation.UpdateDnsRecord,
                OperationStatus.Started,
                logInfos);

            try
            {
                await PerformUpdate(domainName, ipAddress);

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

        protected abstract Task PerformUpdate(
            string domainName,
            string ipAddress);
    }
}

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jimu.Core.Commons.Logger;
using Jimu.Core.Protocols;

namespace Jimu.Core.Client.LoadBalance
{
    /// <summary>
    ///     load balancing by polling algorithm
    /// </summary>
    public class PollingAddressSelector : AddressSelectorBase
    {
        private readonly ConcurrentDictionary<string, Lazy<ServerIndexHolder>> _addresses =
            new ConcurrentDictionary<string, Lazy<ServerIndexHolder>>();

        private readonly ILogger _logger;

        public PollingAddressSelector(ILogger logger)
        {
            _logger = logger;
        }

        public override Task<Address> GetAddressAsync(ServiceRoute serviceRoute)
        {
            var serverIndexHolder = _addresses.GetOrAdd(serviceRoute.ServiceDescriptor.Id,
                key => new Lazy<ServerIndexHolder>(() => new ServerIndexHolder()));
            var address = serverIndexHolder.Value.GetAddress(serviceRoute.Address.ToList());
            _logger.Info($"ServerSelector: {serviceRoute.ServiceDescriptor.Id}: {address.Code}");
            return Task.FromResult(address);
        }

        private class ServerIndexHolder
        {
            private int _latestIndex;
            private int _lock;

            public Address GetAddress(List<Address> addresses)
            {
                while (true)
                {
                    if (Interlocked.Exchange(ref _lock, 1) != 0)
                    {
                        default(SpinWait).SpinOnce();
                        continue;
                    }

                    _latestIndex = (_latestIndex + 1) % addresses.Count;
                    var address = addresses[_latestIndex];
                    Interlocked.Exchange(ref _lock, 0);
                    return address;
                }
            }
        }
    }
}
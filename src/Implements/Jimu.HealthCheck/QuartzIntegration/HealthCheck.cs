﻿using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jimu.Core.Client.HealthCheck;
using Jimu.Core.Commons.Discovery;
using Jimu.Core.Commons.Logger;
using Jimu.Core.Protocols;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using Quartz.Spi;

namespace Jimu.Client.HealthCheck.QuartzIntegration
{
    public class HealthCheck : IHealthCheck
    {
        private readonly ILogger _logger;
        private  IScheduler _scheduler;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly string _cron;
        private readonly int _timeout = 30000;

        public HealthCheck(ILogger logger, IServiceDiscovery serviceDiscovery, int intervalMinute)
        {
            if (intervalMinute == 0 || intervalMinute > 60)
            {
                throw new ArgumentOutOfRangeException($"intervalMinite must between 1 and 60, current is {intervalMinute}");
            }
            _cron = $"0 0/{intervalMinute} * * * ?";

            _logger = logger;
            _serviceDiscovery = serviceDiscovery;
        }

        public void Dispose()
        {
            _scheduler.Clear();
        }

        public async Task RunAsync()
        {
            _scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            IJobDetail jobDetail = JobBuilder.Create<MonitorJob>().WithIdentity("MonitorJob", "MSevice.HealthCheck").Build();
            jobDetail.JobDataMap.Put("serviceDiscovery", _serviceDiscovery);
            jobDetail.JobDataMap.Put("timeout", _timeout);
            IOperableTrigger trigger = new CronTriggerImpl("MonitorJob", "HealthCheck", _cron);
            await _scheduler.ScheduleJob(jobDetail, trigger);
            await _scheduler.Start();
        }

        [DisallowConcurrentExecution]
        private class MonitorJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                var serviceDiscovery = context.JobDetail.JobDataMap.Get("serviceDiscovery") as IServiceDiscovery;
                var timeout = (int)context.JobDetail.JobDataMap.Get("timeout");
                if (serviceDiscovery != null)
                {
                    var routes = (await serviceDiscovery.GetRoutesAsync()).ToList();
                    var servers = (from route in routes
                        from address in route.Address
                        where address.IsHealth
                        select address).Distinct().ToList();
                    foreach (var server in servers)
                    {
                        await CheckHealth(server, timeout);
                    }

                    if (servers.Any(x => !x.IsHealth))
                    {
                        foreach (var server in servers.Where(x => !x.IsHealth))
                        {
                            var updateRoutes = routes.Where(x => x.Address.Contains(server)).ToList();
                            updateRoutes.ForEach(x => x.Address.First(o => o.Code == server.Code).IsHealth = server.IsHealth);
                            await serviceDiscovery.SetRoutesAsync(updateRoutes);
                        }

                    }
                }
            }

            private async Task CheckHealth(Address address, int timeout)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { SendTimeout = timeout })
                {
                    try
                    {
                        await socket.ConnectAsync(address.CreateEndPoint());
                        address.IsHealth = true;
                    }
                    catch
                    {
                        address.IsHealth = false;
                    }
                }
            }
        }
    }
}

﻿using Jimu.Logger;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using Quartz.Spi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jimu.Client.Discovery.Implement
{
    public class ClientServiceDiscovery : IClientServiceDiscovery
    {
        private readonly List<Func<Task<List<JimuServiceRoute>>>> _routesGetters;


        private readonly ConcurrentQueue<JimuAddress> _addresses;
        private readonly ConcurrentQueue<JimuServiceRoute> _routes;

        private readonly int _updateJobIntervalMinute;
        private readonly ILogger _logger;
        public ClientServiceDiscovery(ILoggerFactory loggerFactory, int updateJobIntervalMinute = 1)
        {
            if (updateJobIntervalMinute <= 0 || updateJobIntervalMinute > 60)
            {
                throw new ArgumentOutOfRangeException($"updateJobIntervalMinute must between 1 and 60, current is {updateJobIntervalMinute}");
            }
            _updateJobIntervalMinute = updateJobIntervalMinute;
            _routesGetters = new List<Func<Task<List<JimuServiceRoute>>>>();
            _addresses = new ConcurrentQueue<JimuAddress>();
            _routes = new ConcurrentQueue<JimuServiceRoute>();
            this._logger = loggerFactory.Create(this.GetType());
        }

        public async Task RunInInit()
        {
            await UpdateRoutes();
            await RunUpdateJob();
        }

        private async Task RunUpdateJob()
        {
            string cron = $"0 0/{_updateJobIntervalMinute} * * * ?";
            if (_updateJobIntervalMinute == 60)
            {
                cron = $"0 0 0/1 * * ?";
            }

            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            IJobDetail jobDetail = JobBuilder.Create<MonitorJob>().WithIdentity("MonitorJob", "Jimu.Client.UpdateServiceJob").Build();
            jobDetail.JobDataMap.Put("serviceDiscovery", this);
            jobDetail.JobDataMap.Put("logger", _logger);
            IOperableTrigger trigger = new CronTriggerImpl("MonitorJob", "Jimu.Client.UpdateServiceJob", cron);
            await scheduler.ScheduleJob(jobDetail, trigger);
            await scheduler.Start();

        }

        private void ClearQueue<T>(ConcurrentQueue<T> queue)
        {
            foreach (var q in queue)
            {
                queue.TryDequeue(out var tmpQ);
            }
        }

        private void QueueAdd<T>(ConcurrentQueue<T> queue, List<T> elems)
        {
            foreach (var elem in elems)
            {
                if (!queue.Any(x => x.Equals(elem)))
                    queue.Enqueue(elem);
            }
        }


        public void AddRoutesGetter(Func<Task<List<JimuServiceRoute>>> getter)
        {
            _routesGetters.Add(getter);
        }

        public Task<List<JimuAddress>> GetAddressAsync()
        {
            //if (_addresses.Any())
            //    return _addresses.ToList();
            //await UpdateRoutes();
            return Task.FromResult(_addresses.ToList());
        }

        public Task<List<JimuServiceRoute>> GetRoutesAsync()
        {
            return Task.FromResult(_routes.ToList());
        }

        public Task UpdateServerHealthAsync(List<JimuAddress> addresses)
        {
            foreach (var addr in addresses)
            {
                _routes.Where(x => x.Address.Any(y => y.Code == addr.Code)).ToList()
                    .ForEach(x => x.Address.First(a => a.Code == addr.Code).IsHealth = addr.IsHealth);
                var address = _addresses.FirstOrDefault(x => x.Code == addr.Code);
                if (address != null)
                {
                    address.IsHealth = addr.IsHealth;
                }
            }

            return Task.CompletedTask;
        }
        private async Task UpdateRoutes()
        {
            var routes = new List<JimuServiceRoute>();
            foreach (var routesGetter in _routesGetters)
            {
                var list = await routesGetter();
                if (list != null && list.Any())
                    routes.AddRange(list);
            }
            // merge service and its address by service id
            ClearQueue(_routes);
            foreach (var route in routes)
            {
                var oldAddr = _addresses.FirstOrDefault(x => route.Address.Any(y => y.Code == x.Code && y.IsHealth != x.IsHealth));
                if (oldAddr != null)
                {
                    route.Address.Where(x => x.Code == oldAddr.Code).ToList().ForEach(x => x.IsHealth = oldAddr.IsHealth);
                }
                if (_routes.Any(x => x.ServiceDescriptor.Id == route.ServiceDescriptor.Id))
                {
                    var existService = _routes.First(x => x.ServiceDescriptor.Id == route.ServiceDescriptor.Id);
                    existService.Address = existService.Address.Concat(route.Address.Except(existService.Address)).ToList();
                }
                else
                {
                    _routes.Enqueue(route);
                }
            }
            // update local server address from newest routes
            ClearQueue(_addresses);
            QueueAdd(_addresses, _routes.SelectMany(x => x.Address).Distinct().ToList());
        }


        [DisallowConcurrentExecution]
        private class MonitorJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                var serviceDiscovery = context.JobDetail.JobDataMap.Get("serviceDiscovery") as ClientServiceDiscovery;
                var logger = context.JobDetail.JobDataMap.Get("logger") as ILogger;
                logger.Debug("******* start update services job *******");
                serviceDiscovery?.UpdateRoutes();
                return Task.CompletedTask;
            }
        }
    }
}

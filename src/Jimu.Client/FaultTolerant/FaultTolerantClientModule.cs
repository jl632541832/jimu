﻿using Autofac;
using Jimu.Client.LoadBalance;
using Jimu.Client.RemoteCaller;
using Jimu.Client.RemoteCaller.Implement;
using Jimu.Logger;
using Jimu.Module;
using Microsoft.Extensions.Configuration;

namespace Jimu.Client.FaultTolerant
{
    public class FaultTolerantClientModule : ClientModuleBase

    {
        private readonly FaultTolerantOptions _options;
        public FaultTolerantClientModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(FaultTolerantOptions).Name).Get<FaultTolerantOptions>();
        }

        public override void DoRegister(ContainerBuilder componentContainerBuilder)
        {
            if (_options != null)
            {
                componentContainerBuilder.RegisterType<RemoteServiceCaller>().As<IRemoteServiceCaller>().WithParameter("retryTimes", _options.RetryTimes).SingleInstance();
            }
            base.DoRegister(componentContainerBuilder);
        }

        public override void DoInit(IContainer container)
        {
            if (_options != null)
            {
                var loggerFactory = container.Resolve<ILoggerFactory>();
                var logger = loggerFactory.Create(this.GetType());
                var caller = container.Resolve<IRemoteServiceCaller>();
                var addressSelector = container.Resolve<IAddressSelector>();
                caller.UseMiddleware<RetryCallMiddleware>(addressSelector, loggerFactory, _options.RetryTimes);
                logger.Info($"[config]remote service call failure retry times: {_options.RetryTimes}");
            }
            base.DoInit(container);
        }
    }
}

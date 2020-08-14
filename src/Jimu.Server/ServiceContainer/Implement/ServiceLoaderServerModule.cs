﻿using Autofac;
using Jimu.Logger;
using Jimu.Module;
using Microsoft.Extensions.Configuration;

namespace Jimu.Server.ServiceContainer.Implement
{
    public class ServiceLoaderServerModule : ServerModuleBase
    {
        public override ModuleExecPriority Priority => ModuleExecPriority.Critical;
        public ServiceOptions _options;
        public ServiceLoaderServerModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(ServiceOptions).Name).Get<ServiceOptions>();

        }

        public override void DoRegister(ContainerBuilder componentContainerBuilder)
        {
            if (_options != null)
            {
                componentContainerBuilder.RegisterType<ServiceEntryContainer>().As<IServiceEntryContainer>().SingleInstance();
            }
            base.DoRegister(componentContainerBuilder);
        }

        public override void DoRun(IContainer container)
        {
            if (_options != null)
            {
                var serviceEntryContainer = container.Resolve<IServiceEntryContainer>();
                var loggerFactory = container.Resolve<ILoggerFactory>();
                var logger = loggerFactory.Create(this.GetType());
                ServicesLoader servicesLoader = new ServicesLoader(serviceEntryContainer, logger, _options);
                servicesLoader.LoadServices();
            }
            base.DoInit(container);
        }

    }
}

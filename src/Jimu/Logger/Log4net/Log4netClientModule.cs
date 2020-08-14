﻿using Autofac;
using Jimu.Module;
using Microsoft.Extensions.Configuration;

namespace Jimu.Logger.Log4net
{
    public class Log4netClientModule : ClientModuleBase
    {
        private readonly JimuLog4netOptions _options;
        private readonly Log4netLoggerFactory _log4NetLoggerFactory;
        public Log4netClientModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = this.JimuAppSettings.GetSection(typeof(JimuLog4netOptions).Name).Get<JimuLog4netOptions>();
            if (_options != null)
            {
                _log4NetLoggerFactory = new Log4netLoggerFactory(_options);
            }
        }

        public override void DoRegister(ContainerBuilder componentContainerBuilder)
        {
            if (_options != null)
            {
                //componentContainerBuilder.RegisterType<Log4netLogger>().WithParameter("options", _options).As<ILogger>().SingleInstance();
                //componentContainerBuilder.RegisterType<Log4netLoggerFactory>().WithParameter("options", _options).As<ILoggerFactory>().SingleInstance();
                componentContainerBuilder.RegisterInstance(_log4NetLoggerFactory).As<ILoggerFactory>().SingleInstance();
            }
            base.DoRegister(componentContainerBuilder);
        }

        public override void DoInit(IContainer container)
        {
            if (_options != null)
            {
                var loggerFactory = container.Resolve<ILoggerFactory>();
                var logger = loggerFactory.Create(this.GetType());
                logger.Info($"[config]use Log4net logger");
            }
            base.DoInit(container);
        }
    }
}

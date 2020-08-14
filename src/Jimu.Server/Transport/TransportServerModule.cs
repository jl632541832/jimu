﻿using Autofac;
using Jimu.Logger;
using Jimu.Module;
using Jimu.Server.Transport.DotNetty;
using Microsoft.Extensions.Configuration;
using System;

namespace Jimu.Server.Transport
{
    public class TransportServerModule : ServerModuleBase
    {
        private readonly TransportOptions _options;
        public TransportServerModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(TransportOptions).Name).Get<TransportOptions>();
        }

        public override void DoRegister(ContainerBuilder componentContainerBuilder)
        {
            if (_options != null)
            {
                switch (_options.Protocol)
                {

                    case "Netty":
                        componentContainerBuilder.RegisterType<DotNettyServer>().As<IServer>()
                            .WithParameter("serverIp", _options.Ip)
                            .WithParameter("serverPort", Convert.ToInt32(_options.Port))
                            .WithParameter("serviceInvokeAddress", new JimuAddress(_options.ServiceInvokeIp, Convert.ToInt32(_options.ServiceInvokePort), _options.Protocol))
                            .SingleInstance();
                        break;
                    //case "Http":
                    //    componentContainerBuilder.RegisterType<HttpServer>().As<IServer>()
                    //        .WithParameter("ip", _options.Ip)
                    //        .WithParameter("port", Convert.ToInt32(_options.Port))
                    //        .WithParameter("serviceInvokeAddress", new JimuAddress(_options.ServiceInvokeIp, Convert.ToInt32(_options.ServiceInvokePort), _options.Protocol))
                    //        .SingleInstance();
                    //    break;
                    default: break;
                }

            }
            base.DoRegister(componentContainerBuilder);
        }

        public override void DoRun(IContainer container)
        {
            if (_options != null)
            {
                var loggerFactory = container.Resolve<ILoggerFactory>();
                var logger = loggerFactory.Create(this.GetType());
                switch (_options.Protocol)
                {
                    case "Netty":
                        logger.Info($"[config]use dotnetty for transfer");
                        var nettyServer = container.Resolve<IServer>();
                        nettyServer.StartAsync();
                        break;
                    //case "Http":
                    //    logger.Info($"[config]use http for transfer");
                    //    var httpServer = container.Resolve<IServer>();
                    //    httpServer.StartAsync();
                    //    break;
                    default: break;
                }
            }
            base.DoInit(container);
        }
    }
}

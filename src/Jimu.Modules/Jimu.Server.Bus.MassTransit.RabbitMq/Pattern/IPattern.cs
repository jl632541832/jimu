﻿using Autofac;
using Jimu.Bus;
using Jimu.Logger;
using MassTransit.RabbitMqTransport;

namespace Jimu.Server.Bus.MassTransit.RabbitMq.Pattern
{
    interface IPattern
    {
        void Register(ContainerBuilder serviceContainerBuilder);
        void MasstransitConfig(IRabbitMqBusFactoryConfigurator configurator, IContainer container, ILogger logger, IJimuBus bus, MassTransitOptions options);
    }
}

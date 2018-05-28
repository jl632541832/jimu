﻿using System;
using System.Threading.Tasks;

namespace Jimu.Core.Client.HealthCheck
{
    /// <summary>
    ///     server health check monitor
    /// </summary>
    public interface IHealthCheck : IDisposable
    {
        Task RunAsync();
    }
}
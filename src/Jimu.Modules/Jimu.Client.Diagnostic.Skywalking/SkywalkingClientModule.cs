﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyApm;
using SkyApm.Agent.GeneralHost;

namespace Jimu.Client.Diagnostic.Skywalking
{
    public class SkywalkingClientModule : ClientWebModuleBase
    {
        SkywalkingOptions _options;
        public SkywalkingClientModule(IConfigurationRoot jimuAppSettings) : base(jimuAppSettings)
        {
            _options = jimuAppSettings.GetSection(typeof(SkywalkingOptions).Name).Get<SkywalkingOptions>();
            if (_options == null)
                _options = new SkywalkingOptions();
        }

        public override void DoHostBuild(IHostBuilder hostBuilder)
        {
            if (_options.Enable)
            {
                hostBuilder
                    .ConfigureServices(services => services.AddSingleton<ITracingDiagnosticProcessor, JimuClientDiagnosticProcessor>())
                    .AddSkyAPM();

            }
            base.DoHostBuild(hostBuilder);
        }

        public override void DoWebConfigureServices(IServiceCollection services)
        {
            if (_options.Enable)
            {
                services.AddSingleton<ITracingDiagnosticProcessor, JimuClientDiagnosticProcessor>();
            }

            base.DoWebConfigureServices(services);
        }

        public override void DoWebHostBuilder(IWebHostBuilder webHostBuilder)
        {
            if (_options.Enable)
            {
                webHostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "SkyAPM.Agent.AspNetCore");
            }
            base.DoWebHostBuilder(webHostBuilder);
        }
    }


}

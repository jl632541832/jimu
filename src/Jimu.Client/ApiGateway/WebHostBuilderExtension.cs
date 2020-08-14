﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace Jimu.Client.ApiGateway
{
    public static class WebHostBuilderExtension
    {
        internal static IHostBuilder UseWebHostJimu(this IHostBuilder hostBuilder, ApplicationClientBuilder jimuAppBuilder, Action<IServiceCollection> servicesAction = null, Action<WebHostBuilderContext, IApplicationBuilder> appAction = null, Action<IMvcBuilder> mvcAction = null, Action<IWebHostBuilder> webBuilderAction = null)
        {
            IApplication jimuApp = null;
            jimuAppBuilder.AddBeforeBuilder(cb =>
            {
                hostBuilder.ConfigureWebHostDefaults(web =>
                {

                    var type = typeof(ClientWebModuleBase);
                    var webModules = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(x => x.GetTypes())
                        .Where(x => x.IsClass && type.IsAssignableFrom(x) && !x.IsAbstract)
                        .Select(x => Activator.CreateInstance(x, jimuAppBuilder.JimuAppSettings) as ClientWebModuleBase)
                        .OrderBy(x => x.Priority);

                    foreach (var configure in webModules)
                    {
                        configure.DoWebHostBuilder(web);
                    }

                    webBuilderAction?.Invoke(web);


                    web
                    .ConfigureServices(services =>
                    {
                        services.AddControllersWithViews();
                        servicesAction?.Invoke(services);
                        var mvcBuilder = services.AddJimu(jimuAppBuilder.JimuAppSettings);
                        mvcAction?.Invoke(mvcBuilder);
                        cb.Populate(services);
                    })
                    .Configure((context, app) =>
                    {
                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapRazorPages();
                            endpoints.MapControllers();
                        });

                        appAction?.Invoke(context, app);
                        app.UseJimu(jimuApp);

                        jimuApp.Run();
                        JimuClient.Host = jimuApp;

                    });

                });

            });

            jimuApp = jimuAppBuilder.Build();

            return hostBuilder;
        }

        public static IMvcBuilder AddJimu(this IServiceCollection services, IConfigurationRoot config)
        {
            //services.AddCors();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            IMvcBuilder mvcBuilder = services.AddMvc(o =>
           {
               o.EnableEndpointRouting = false;
               o.ModelBinderProviders.Insert(0, new JimuQueryStringModelBinderProvider());
               o.ModelBinderProviders.Insert(1, new JimuModelBinderProvider());
               o.RespectBrowserAcceptHeader = true;
           }).AddNewtonsoftJson(options =>
           {
               options.SerializerSettings.ContractResolver = new DefaultContractResolver();
           })
            //.AddJsonOptions(options =>
            //{
            //    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            //})
            .AddXmlSerializerFormatters()
            ;

            var type = typeof(ClientWebModuleBase);
            var webModules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && type.IsAssignableFrom(x) && !x.IsAbstract)
                .Select(x => Activator.CreateInstance(x, config) as ClientWebModuleBase)
                .OrderBy(x => x.Priority);

            foreach (var configure in webModules)
            {
                configure.DoWebConfigureServices(services);
            }
            return mvcBuilder;
        }

        public static IApplicationBuilder UseJimu(this IApplicationBuilder app, IApplication host)
        {
            var type = typeof(ClientWebModuleBase);
            var webModules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && type.IsAssignableFrom(x) && !x.IsAbstract)
                .Select(x => Activator.CreateInstance(x, host.JimuAppSettings) as ClientWebModuleBase)
                .OrderBy(x => x.Priority);

            app.UseMiddleware<JimuHttpStatusCodeExceptionMiddleware>();
            //app.UseCors(builder =>
            //{
            //    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            //});


            foreach (var configure in webModules)
            {
                configure.DoWebConfigure(app);
            }

            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            JimuHttpContext.Configure(httpContextAccessor);
            app.UseMvc(routes =>
            {

                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}");
                routes.MapRoute(
                    name: "defaultApi",
                    template: "api/{controller}/{action}");
                routes.MapRoute(
                   "swagger",
                  "swagger/{*path}"
                  );
                routes.MapRoute(
                    "JimuPath",
                    "{*path:regex(^(?!swagger))}",
                    new { controller = "JimuServices", action = "JimuPath" });
            });

            JimuClient.Host = host;
            return app;
        }
    }
}

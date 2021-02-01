﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Jimu.Common
{
    public static class JimuHelper
    {
        public static T Serialize<T>(object instance)
        {
            return SerializerHelper.Serialize<T>(instance);
        }

        public static TResult Deserialize<T, TResult>(T data) where TResult : class
        {
            return SerializerHelper.Deserialize<T, TResult>(data);
        }
        public static object Deserialize<T>(T data, Type type)
        {
            return SerializerHelper.Deserialize(data, type);
        }

        public static string GenerateServiceId(MethodInfo method)
        {
            if (method.DeclaringType == null) return null;
            var id = $"{method.DeclaringType.FullName}.{method.Name}";
            var paras = method.GetParameters();
            if (paras.Any()) id += "(" + string.Join(",", paras.Select(i => i.Name)) + ")";
            return id;

        }

        public static object ConvertType(object instance, Type destinationType)
        {
            return TypeConvertProvider.Convert(instance, destinationType);
        }

        /// <summary>
        /// get config from specify file which locate in app root
        /// </summary>
        /// <param name="fileName">setting json file</param>
        /// <returns></returns>
        public static IConfigurationRoot GetConfig(string settingJson)
        {
            var provider = new JsonEnvParamParserFileProvider(settingJson);
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(provider, settingJson, true, false)
                .AddEnvironmentVariables();
                
            return builder.Build();
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
            //throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static IConfigurationRoot ReadSetting(string settingFileName)
        {
            var jimuAppSettings = $"{settingFileName}.json";
            var env = ReadJimuEvn();
            if (!string.IsNullOrEmpty(env))
            {
                jimuAppSettings = $"{settingFileName}.{env}.json";
            }
            if (!File.Exists(jimuAppSettings))
            {
                throw new FileNotFoundException($"{jimuAppSettings} not found!");
            }
            return GetConfig(jimuAppSettings);
        }

        public static string ReadJimuEvn()
        {
            var jimuSettings = "JimuSettings.json";
            var jimuEnv = "JIMU_ENV";
            string activeProfile = "";

            if (File.Exists(jimuSettings))
            {
                var config = JimuHelper.GetConfig(jimuSettings);
                if (config != null && config["ActiveProfile"] != null)
                {
                    activeProfile = config["ActiveProfile"];
                }
            }
            if (string.IsNullOrEmpty(activeProfile?.Trim()))
            {
                var builder = new ConfigurationBuilder();
                var config = builder.AddEnvironmentVariables().Build();
                if (config != null && config[jimuEnv] != null)
                {
                    activeProfile = config[jimuEnv];
                }
            }
            return activeProfile?.Trim();
        }
    }
}

﻿using Autofac;
using Jimu.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jimu.Client.ApiGateway
{
    [ModelBinder(BinderType = typeof(JimuModelBinder))]
    public class JimuModel
    {
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        private ILogger _logger;
        public JimuModel()
        {
            var loggerFactory = JimuClient.Host.Container.Resolve<ILoggerFactory>();
            _logger = loggerFactory.Create(this.GetType());

        }
        public async Task ReadFromContentAsync(Stream content, string contentType = "application/json")
        {

            using (var sr = new StreamReader(content))
            {
                var json = await sr.ReadToEndAsync();
                try
                {
                    Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
                catch (Exception ex)
                {
                    if (contentType == "text/plain" || contentType == "text/xml" || contentType == "application/xml")
                    {
                        Data = new Dictionary<string, object>();
                        Data.Add("data", json);
                    }
                    else
                    {
                        _logger.Error($"JimuModel.ReadFromContentAsync, unsupport content-type: {contentType}, ", ex);
                    }

                }

            }
        }



        public JimuModel(IFormCollection form)
        {
            foreach (var f in form)
            {
                Data.Add(f.Key, f.Value);
            }
        }
        public JimuModel(Dictionary<string, object> data)
        {
            Data = data;
        }
    }
}

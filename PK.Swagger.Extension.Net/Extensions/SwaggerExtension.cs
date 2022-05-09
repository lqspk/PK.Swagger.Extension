﻿/*
 * By PK 
*/

using System;
using System.Net.Http;
using System.Reflection;
using Swashbuckle.Application;
using System.Web.Http;
using PK.Swagger.Extension.Net.Handlers;

namespace PK.Swagger.Extension.Net.Extensions
{
    /// <summary>
    /// Swagger扩展类
    /// </summary>
    public static class SwaggerExtension
    {
        /// <summary>
        /// 启用Swagger导出
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static SwaggerEnabledConfiguration EnableSwaggerExport(this SwaggerEnabledConfiguration config)
        {
            var httpConfig = GetHttpConfiguration(config);
            httpConfig.Routes.MapHttpRoute(
                name: "SwaggerExportRoute",
                routeTemplate: "Swagger/Export/{action}/{url}", //导出地址
                defaults: new { url = RouteParameter.Optional },
                constraints: null,
                handler: (HttpMessageHandler)new SwaggerExportRequestHandler()
            );
            
            return config;
        }

        /// <summary>
        /// 注入导出的JavaScript脚本文件
        /// </summary>
        /// <param name="config"></param>
        public static void InjectExportScript(this SwaggerUiConfig config)
        {
            var thisAssembly = typeof(SwaggerExtension).Assembly;
            config.InjectJavaScript(thisAssembly, "PK.Swagger.Extension.Net.Scripts.swaggerExport.js");
        }

        /// <summary>
        /// 从SwaggerEnabledConfiguration中获取HttpConfiguration实例
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private static HttpConfiguration GetHttpConfiguration(SwaggerEnabledConfiguration instance)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField("_httpConfig", flag);
            var result = field.GetValue(instance);
            return (HttpConfiguration)result;
        }
    }
}
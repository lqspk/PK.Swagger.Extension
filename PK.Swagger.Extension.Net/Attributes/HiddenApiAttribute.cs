using System;
using System.Linq;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace PK.Swagger.Extension.Net.Attributes {
    /// <summary>
    /// 不导出接口过滤器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class HiddenApiAttribute : Attribute { }
    public class HiddenApiFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            foreach (ApiDescription apiDescription in apiExplorer.ApiDescriptions)
            {
                if (Enumerable.OfType<HiddenApiAttribute>(apiDescription
                        .GetControllerAndActionAttributes<HiddenApiAttribute>()).Any())
                {
                    string key = "/" + apiDescription.RelativePath;
                    if (key.Contains("?"))
                    {
                        int idx = key.IndexOf("?", StringComparison.Ordinal);
                        key = key.Substring(0, idx);
                    }

                    swaggerDoc.paths.Remove(key);
                }

            }
        }
    }
}
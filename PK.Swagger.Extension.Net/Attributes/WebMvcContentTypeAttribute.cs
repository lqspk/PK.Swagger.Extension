using System;

namespace PK.Swagger.Extension.Net.Attributes {
    /// <summary>
    /// 请求Content-type
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMvcContentTypeAttribute : Attribute 
    {
        public WebMvcContentTypeAttribute(string[] contentTypes)
        {

        }
    }
}

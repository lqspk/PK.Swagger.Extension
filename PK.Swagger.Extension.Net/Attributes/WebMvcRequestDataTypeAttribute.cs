using System;

namespace PK.Swagger.Extension.Net.Attributes {
    /// <summary>
    /// 请求数据类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMvcRequestDataTypeAttribute : Attribute 
    {
        public WebMvcRequestDataTypeAttribute(Type t)
        {

        }

        public WebMvcRequestDataTypeAttribute(string data, string description = null) {

        }
    }
}

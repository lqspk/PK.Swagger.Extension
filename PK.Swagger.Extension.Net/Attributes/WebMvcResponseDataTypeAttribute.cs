using System;

namespace PK.Swagger.Extension.Net.Attributes {
    /// <summary>
    /// 返回数据类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMvcResponseDataTypeAttribute : Attribute 
    {
        public WebMvcResponseDataTypeAttribute(Type t) 
        {

        }
    }
}

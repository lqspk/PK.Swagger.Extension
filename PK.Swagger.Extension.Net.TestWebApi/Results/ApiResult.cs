using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PK.Swagger.Extension.Net.TestWebApi.Results
{
    /// <summary>
    /// WebApi接口返回通用类
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 错误码：0成功，其它失败
        /// </summary>
        public int errorCode { get; set; }

        /// <summary>
        /// 返回消息
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 返回数据
        /// </summary>
        public object data { get; set; }

    }
}
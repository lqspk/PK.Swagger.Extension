using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PK.Swagger.Extension.Net.Models
{
    /// <summary>
    /// 单个接口解释
    /// </summary>
    internal class InterfaceDefinitionModel
    {
        /// <summary>
        /// 接口地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 接口描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 提交方法：Get或者Post
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 所属控制器
        /// </summary>
        public List<string> BelongControllers { get; set; }

        /// <summary>
        /// 请求头的ContentType
        /// </summary>
        public List<string> RequestContentTypes { get; set; }

        /// <summary>
        /// 响应内容类型
        /// </summary>
        public List<string> ResponseContentTypes { get; set; }

        /// <summary>
        /// 响应数据类型
        /// </summary>
        public string ResponseDataType { get; set; }

        /// <summary>
        /// 响应数据描述
        /// </summary>
        public string ResponseDataDescription { get; set; }

        /// <summary>
        /// 接口请求参数
        /// </summary>
        public List<InterfaceRequestParameter> RequestParameters { get; set; }

        /// <summary>
        /// 接口响应参数
        /// </summary>
        public List<ClassPropertyModel> ResponseProperties { get; set; }
    }

    /// <summary>
    /// 接口请求参数
    /// </summary>
    internal class InterfaceRequestParameter
    {
        /// <summary>
        /// 参数位置
        /// header、query、body
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        public string DataFormat { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool? IsRequired { get; set; }
    }
}

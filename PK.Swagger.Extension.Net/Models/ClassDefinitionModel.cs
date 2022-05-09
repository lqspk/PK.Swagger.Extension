using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PK.Swagger.Extension.Net.Models
{
    /// <summary>
    /// 单个类解释
    /// </summary>
    public class ClassDefinitionModel
    {
        /// <summary>
        /// 类名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 类类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 类描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 类属性列表
        /// </summary>
        public List<ClassPropertyModel> Properties { get; set; }
}

    /// <summary>
    /// 单个类属性
    /// </summary>
    public class ClassPropertyModel
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 属性数据类型格式
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 属性描述
        /// </summary>
        public string Description { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PK.Swagger.Extension.Net.TestModel
{
    public class BaseModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// 测试模型
    /// </summary>
    public class TestModel : BaseModel {
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public bool? Sex { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 生日
        /// </summary>
        public DateTime Birthday { get; set; }

        public List<UserModel> Users { get; set; }
    }
}

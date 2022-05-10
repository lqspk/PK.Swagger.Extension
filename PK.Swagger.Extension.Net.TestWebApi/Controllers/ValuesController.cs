using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PK.Swagger.Extension.Net.TestModel;
using PK.Swagger.Extension.Net.TestWebApi.Results;

namespace PK.Swagger.Extension.Net.TestWebApi.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        [Route("getresult")]
        [HttpGet]
        public ApiResult GetResult(int id)
        {
            return new ApiResult()
            {
                errorCode = 0,
                data = id
            };
        }

        /// <summary>
        /// 提交数据
        /// </summary>
        /// <param name="value">数据</param>
        /// <returns></returns>
        [Route("postresult")]
        [HttpPost]
        public ApiResult PostResult(string value)
        {
            return new ApiResult()
            {
                errorCode = 0,
                data = value
            };
        }

        /// <summary>
        /// 获取Time
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [Route("gettime")]
        public string GetTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        [Route("getuserinfo")]
        public TestModel.TestModel GetUserInfo()
        {
            return new TestModel.TestModel();
        }

    }
}

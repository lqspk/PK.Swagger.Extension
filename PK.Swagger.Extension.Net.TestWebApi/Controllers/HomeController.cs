using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using PK.Swagger.Extension.Net.Attributes;
using PK.Swagger.Extension.Net.Enums;
using PK.Swagger.Extension.Net.Providers;

namespace PK.Swagger.Extension.Net.TestWebApi.Controllers
{
    /// <summary>
    /// 首页控制器
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// 首页
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="time">时间</param>
        /// <returns>返回页面</returns>
        [System.Web.Mvc.HttpGet]
        public async Task<ActionResult> Index(string name, int? time = null)
        {
            ViewBag.Title = "Home Page";
            return View();
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [System.Web.Mvc.HttpPost]
        [WebMvcRequestDataType("123", "测试参数")]
        [WebMvcContentType(new string[]{ "application/x-www-form-urlencoded", "application/json" })]
        public async Task<ActionResult> Save(dynamic data)
        {
            return Json(new {@state = 1});
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="model">测试模型</param>
        /// <returns></returns>
        [WebMvcRequestDataType(typeof(TestModel.TestModel))]
        [WebMvcResponseDataType(typeof(TestModel.TestModel))]
        public async Task<ActionResult> Get(TestModel.TestModel model) {
            return Json(new { @model = new TestModel.TestModel() });
        }

        [HiddenApi]
        public async Task<ActionResult> Hide() {
            return Json(new { @model = new TestModel.TestModel() });
        }
    }
}

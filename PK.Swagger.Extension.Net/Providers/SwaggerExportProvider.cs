/*
 * By PK
 */
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PK.Swagger.Extension.Net.Models;

namespace PK.Swagger.Extension.Net.Providers
{
    public class SwaggerExportProvider
    {
        /// <summary>
        /// 创建MarkDown数据
        /// </summary>
        /// <param name="swaggerJsonUrl"></param>
        /// <returns></returns>
        public static async Task<byte[]> CreateMarkdownFile(string swaggerJsonUrl)
        {
            var httpClient = HttpClientFactory.Create();
            HttpResponseMessage responseMessage = await httpClient.GetAsync(swaggerJsonUrl);
            var responseTxt = await responseMessage.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(responseTxt);


            //获取接口的定义
            var paths = jObj["paths"];

            //获取类的定义
            var definitions = jObj["definitions"];


            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"# 接口定义 ");
            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine("");

            //接口序号
            int i = 0;

            foreach (var jPath in paths)
            {
                i++;

                InterfaceDefinitionModel interfaceDefinition = GetInterfaceDefinition(jPath, definitions);

                sb.Append(InterfaceDefinitionToMarkDown(interfaceDefinition, i));
            }


            //类定义
            if (definitions.Any())
            {
                sb.AppendLine("");
                sb.AppendLine("");
                sb.AppendLine("");
                sb.AppendLine($"# 类定义 ");

                foreach (var definition in definitions)
                {
                    sb.Append(ClassDefinitionToMarkDown(GetClassDefinition(definition)));
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// 获取单个接口解释
        /// </summary>
        /// <param name="interfaceToken"></param>
        /// <returns></returns>
        private static InterfaceDefinitionModel GetInterfaceDefinition(JToken interfaceToken, JToken definitions)
        {
            InterfaceDefinitionModel model = new InterfaceDefinitionModel();

            JProperty property = interfaceToken.ToObject<JProperty>();

            //接口路径
            model.Path = property.Name;

            JProperty methodProperty = property.Values().FirstOrDefault()?.ToObject<JProperty>();

            //请求方法
            model.Method = methodProperty?.Name;

            //描述
            model.Description = methodProperty?.Value["summary"]?.ToString();

            //接口所在控制器
            model.BelongControllers = methodProperty?.Value["tags"]?.ToObject<List<string>>();

            //请求头的ContentType
            model.RequestContentTypes = methodProperty?.Value["consumes"]?.ToObject<List<string>>();

            //请求参数
            model.RequestParameters = new List<InterfaceRequestParameter>();
            var requestParams = JArray.FromObject(methodProperty?.Value["parameters"] ?? JToken.FromObject(new string[] { }));
            foreach (var requestParam in requestParams)
            {
                InterfaceRequestParameter parameter = new InterfaceRequestParameter()
                {
                    Position = requestParam["in"]?.ToString(),
                    Name = requestParam["name"]?.ToString(),
                    Description = requestParam["description"]?.ToString(),
                    DataType = (requestParam["schema"]?["$ref"] ?? requestParam["schema"]?["type"] ?? requestParam["type"])?.ToString() ?? "",
                    DataFormat = requestParam["format"]?.ToString(),
                    IsRequired = requestParam["required"]?.ToObject<bool?>(),
                };
                model.RequestParameters.Add(parameter);
            }

            //响应内容类型
            model.ResponseContentTypes = methodProperty?.Value["produces"]?.ToObject<List<string>>();

            //响应数据类型
            model.ResponseDataType = (methodProperty?.Value["responses"]?["200"]?["schema"]?["$ref"] ??
                                      methodProperty?.Value["responses"]?["200"]?["schema"]?["type"])?.ToString();

            //如果响应数据是类
            if (model.ResponseDataType.StartsWith("#/definitions/"))
            {
                //获取类名
                string className = model.ResponseDataType.Replace("#/definitions/", "");

                JToken classToken = definitions.Children().FirstOrDefault(s => s.ToObject<JProperty>().Name == className);

                ClassDefinitionModel classDefinition = GetClassDefinition(classToken);

                //响应数据
                model.ResponseProperties = classDefinition.Properties;
            }

            return model;
        }

        /// <summary>
        /// 接口解释转MarkDown字符串
        /// </summary>
        /// <param name="interfaceDefinition">接口解释</param>
        /// <param name="index">接口序号</param>
        /// <returns></returns>
        private static StringBuilder InterfaceDefinitionToMarkDown(InterfaceDefinitionModel interfaceDefinition, int index)
        {
            StringBuilder sb = new StringBuilder();

            //接口标题
            sb.AppendLine($"## 接口{index}、{interfaceDefinition.Description}");
            sb.AppendLine("");


            //Controller
            sb.AppendLine($"- **所属控制器：** {string.Join("、", interfaceDefinition.BelongControllers)}");
            sb.AppendLine("");

            //url
            sb.AppendLine($"- **接口地址：** {interfaceDefinition.Path}");
            sb.AppendLine("");

            //Method
            sb.AppendLine($"- **请求方式：**{interfaceDefinition.Method}  ");
            sb.AppendLine("");

            //Content-Type
            if (interfaceDefinition.RequestContentTypes != null && interfaceDefinition.RequestContentTypes.Any())
            {
                sb.AppendLine("- **Content-Type：**");
                sb.AppendLine("");

                foreach (var consume in interfaceDefinition.RequestContentTypes)
                {
                    sb.AppendLine($"  {consume}");
                    sb.AppendLine("");
                }
            }

            //请求参数
            if (interfaceDefinition.RequestParameters != null && interfaceDefinition.RequestParameters.Any())
            {
                sb.AppendLine($"- **请求参数**：");
                sb.AppendLine("");

                var groups = interfaceDefinition.RequestParameters.GroupBy(s => s.Position)
                    .Select(s => s.Key)
                    .ToList();
                foreach (var group in groups)
                {
                    if (group == "header")
                    {
                        sb.AppendLine($"- **请求头参数**：");
                        sb.AppendLine("");
                    }

                    sb.AppendLine("|参数名|数据类型|数据格式|描述|是否必填|");
                    sb.AppendLine("|-|-|-|-|-|");
                    foreach (var item in interfaceDefinition.RequestParameters.Where(s => s.Position == group))
                    {
                        //数据类型
                        string refClass = item.DataType;
                        if (refClass.StartsWith("#/definitions/"))
                            refClass = refClass.Replace("#/definitions/", "");

                        //添加表格行
                        sb.AppendLine($"|{item.Name}|{refClass}|{item.DataFormat}|{item.Description?.Replace("\r\n", "<br/>")}|{(item.IsRequired == true ? "是" : "否")}|");
                    }

                    sb.AppendLine("");
                }
            }

            //response
            sb.AppendLine($"- **响应数据**：");
            sb.AppendLine($"");

            var responseType = interfaceDefinition.ResponseDataType;
            sb.AppendLine($"  - **数据类型：**");
            sb.AppendLine("");
            sb.AppendLine($"    {responseType.Replace("#/definitions/", "")}");
            sb.AppendLine("");

            if (interfaceDefinition.ResponseProperties != null && interfaceDefinition.ResponseProperties.Any())
            {
                //返回数据示例
                sb.AppendLine($"  - **示例：**");
                sb.AppendLine("");
                sb.AppendLine("```json");
                sb.AppendLine("{");
                sb.AppendLine("");

                //遍历类属性
                foreach (var prop in interfaceDefinition.ResponseProperties)
                {
                    if (!string.IsNullOrWhiteSpace(prop.Description))
                    {
                        sb.AppendLine($"  //{prop.Description.Replace("\r\n", "<br/>")}");
                    }
                    sb.AppendLine($"  \"{prop.Name}\": null, //数据类型：{prop.Type}");
                    sb.AppendLine("");
                }
                sb.AppendLine("}");
                sb.AppendLine("```");

                //返回数据字段说明
                sb.AppendLine($"  - **返回字段说明：**");
                sb.AppendLine("");
                sb.AppendLine("|字段名|数据类型|数据格式|描述|");
                sb.AppendLine("|-|-|-|-|");
                foreach (var prop in interfaceDefinition.ResponseProperties)
                {
                    sb.AppendLine($"|{prop.Name}|{prop.Type}|{prop.Format}|{prop.Description?.Replace("\r\n", "<br/>")}|");
                }

                sb.AppendLine("");
            }

            return sb;
        }

        /// <summary>
        /// 获取单个类解释
        /// </summary>
        /// <param name="classDefinition">单个类解释</param>
        /// <returns></returns>
        private static ClassDefinitionModel GetClassDefinition(JToken classToken)
        {
            JProperty property = classToken.ToObject<JProperty>();

            ClassDefinitionModel classDefinition = new ClassDefinitionModel()
            {
                Name = property.Name,
                Type = property.Value["type"]?.ToString() ?? "",
                Description = property.Value["description"]?.ToString() ?? "",
                Properties = new List<ClassPropertyModel>()
            };

            //遍历类属性
            foreach (var prop in property.Value["properties"] ?? JToken.FromObject(new string[] { }))
            {
                var propObj = prop.ToObject<JProperty>();

                ClassPropertyModel classProperty = new ClassPropertyModel()
                {
                    Name = propObj.Name,
                    Type = propObj.Value["type"]?.ToString() ?? "",
                    Format = propObj.Value["format"]?.ToString() ?? "",
                    Description = propObj.Value["description"]?.ToString() ?? ""
                };
                classDefinition.Properties.Add(classProperty);
            }

            return classDefinition;
        }

        /// <summary>
        /// 类解释转MarkDown字符串
        /// </summary>
        /// <param name="classDefinition"></param>
        /// <returns></returns>
        private static StringBuilder ClassDefinitionToMarkDown(ClassDefinitionModel classDefinition)
        {
            StringBuilder sb = new StringBuilder();

            //类名称
            sb.AppendLine($"## {classDefinition.Name}");
            sb.AppendLine("");

            //类描述
            sb.AppendLine($"  - **描述**：{classDefinition.Description}");
            sb.AppendLine("");

            //类属性
            sb.AppendLine($"  - **类属性说明**：");
            sb.AppendLine("");

            //生成属性表格
            sb.Append(ClassPropertyToMarkDown(classDefinition.Properties));

            return sb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static StringBuilder ClassPropertyToMarkDown(List<ClassPropertyModel> properties)
        {
            StringBuilder sb = new StringBuilder();
            if (properties.Any())
            {
                //表格头
                sb.AppendLine("|属性名|数据类型|数据格式|描述|");
                sb.AppendLine("|-|-|-|-|");

                foreach (var prop in properties)
                {
                    //表格行
                    sb.AppendLine($"|{prop.Name}|{prop.Type}|{prop.Format}|{prop.Description}|");
                }

                sb.AppendLine("");
            }

            return sb;
        }
    }
}
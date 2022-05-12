using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PK.Swagger.Extension.Net.Enums;
using PK.Swagger.Extension.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;

namespace PK.Swagger.Extension.Net.Providers {
    /// <summary>
    /// 导出Web.Mvc方法
    /// </summary>
    public class WebMvcExportProvider {
        private static Type[] resultTypes = new[]
        {
            typeof(ActionResult), typeof(ContentResult), typeof(FileResult), typeof(FileStreamResult),
            typeof(JsonResult), typeof(JavaScriptResult), typeof(Task<ActionResult>), typeof(Task<ContentResult>),
            typeof(Task<FileResult>), typeof(Task<FileStreamResult>),
            typeof(Task<JsonResult>), typeof(Task<JavaScriptResult>)
        };

        private static XmlDocument[] xmlDocuments = GetXMLFiles();

        /// <summary>
        /// 获取控制器信息
        /// </summary>
        /// <returns></returns>
        internal static List<ControllerInfo> GetControllers() {
            List<ControllerInfo> controllerNames = new List<ControllerInfo>();

            foreach (var t in GetSubClasses<Controller>()) {
                //控制器相关信息
                ControllerInfo controllerInfo = new ControllerInfo() {
                    FullName = t.FullName,
                    Name = t.Name,
                    CustomAttributes = t.CustomAttributes,
                    Summary = GetSummaryFromXML(t.FullName, MemberType.Class)
                };

                controllerNames.Add(controllerInfo);

                controllerInfo.ActionInfos = new List<ActionInfo>();

                //获取控制器下的方法列表
                List<MethodInfo> methodList = GetSubMethods(t);

                foreach (var method in methodList)
                {
                    string actionFullName = $"{method.DeclaringType.FullName}.{method.Name}";
                    var returnTypeResult = GetReturnType(method);
                    controllerInfo.ActionInfos.Add(new ActionInfo()
                    {
                        FullName = actionFullName,
                        Name = method.Name,
                        Path = $"/{controllerInfo.Name.Replace("Controller", "")}/{method.Name}",
                        CustomAttributes = method.CustomAttributes,
                        Summary =
                            GetSummaryFromXML(actionFullName, MemberType.Action),
                        Parameters =
                            GetParameters(GetParameterInfos(method), GetActionSummaryFromXML(actionFullName)),
                        ReturnTypeName = returnTypeResult.Item1,
                        ReturnTypeFullName = returnTypeResult.Item2
                    });
                }

            }
            return controllerNames;
        }

        /// <summary>
        /// 将控制器信息生成json
        /// </summary>
        /// <param name="controllerInfos"></param>
        /// <returns></returns>
        internal static string CreateJson(List<ControllerInfo> controllerInfos)
        {
            StringBuilder sb = new StringBuilder();

            JObject paths = new JObject();
            //遍历控制器
            foreach (var controllerInfo in controllerInfos) {

                //遍历控制器下的方法
                foreach (var actionInfo in controllerInfo.ActionInfos) {
                    //请求方式
                    string requestMethod = GetRequestMethod(actionInfo.CustomAttributes);

                    //控制器名称
                    var controllerName = controllerInfo.Name;

                    var pathStr = $"\"{actionInfo.Path}\"";
                    var tagsStr = $"\"tags\":[\"{controllerName}\"],";

                    var summaryStr = $"\"summary\": \"{actionInfo.Summary}\",";

                    var parametersStr = JsonConvert.SerializeObject(new { @parameters = actionInfo.Parameters }).Trim(new char[]{'{', '}'}) + ",";

                    var responseStr = "\"responses\":{ \"200\": { \"description\": \"OK\", \"schema\": { \"$ref\": \"#/definitions/" + actionInfo.ReturnTypeName + "\"}}}";

                    var requestMethodStr = $"{requestMethod}\":" + "{" + tagsStr + summaryStr + parametersStr + responseStr + "}";

                    var pathInfo = $"{pathStr}:" + "{\"" + requestMethodStr + " },";

                    sb.Append(pathInfo);
                }
            }

            //类定义
            var definitions = "\"definitions\":{}";

            var pathsStr = "{\"paths\":{" + sb.ToString().TrimEnd(',') + "}," + definitions + "}";

            return pathsStr;
        }

        /// <summary>
        /// 获取某一个程序集下所有的类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<Type> GetSubClasses<T>() {
            List<Type> types = new List<Type>();
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                types.AddRange(assembly.GetTypes().Where(
                        type => type.IsSubclassOf(typeof(T))).ToList());
            }

            return types;
        }

        /// <summary>
        /// 获取Controller下的action
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static List<MethodInfo> GetSubMethods(Type t) {
            return t.GetMethods().Where(m => resultTypes.Contains(m.ReturnType) && m.IsPublic == true).ToList();
        }

        /// <summary>
        /// 获取方法参数
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private static System.Reflection.ParameterInfo[] GetParameterInfos(object instance)
        {
            return ((System.Reflection.MethodBase)instance).GetParameters();
        }

        /// <summary>
        /// 获取私有字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        private static T GetPrivateField<T>(object instance, string fieldname) {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.GetField;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            T to = default(T);
            try {
                to = (T)field.GetValue(instance);
            } catch { }
            return to;
        }

        /// <summary>
        /// 获取请求方式
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <returns></returns>
        private static string GetRequestMethod(IEnumerable<CustomAttributeData> customAttributes)
        {
            if (customAttributes.Any(s => s.AttributeType == typeof(HttpPostAttribute)))
            {
                return "post";
            }

            if (customAttributes.Any(s => s.AttributeType == typeof(HttpPutAttribute))) {
                return "put";
            }

            if (customAttributes.Any(s => s.AttributeType == typeof(HttpDeleteAttribute))) {
                return "delete";
            }

            return "get";
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="parameterInfos">参数列表</param>
        /// <param name="node">XML注释</param>
        /// <returns></returns>
        private static List<ActionParameterInfo> GetParameters(System.Reflection.ParameterInfo[] parameterInfos, XmlNode node)
        {
            if (parameterInfos == null)
                return new List<ActionParameterInfo>();

            List<ActionParameterInfo> actionParameterInfos = new List<ActionParameterInfo>();
            foreach (var parameterInfo in parameterInfos)
            {
                var paramterType = GetParamterType(parameterInfo);
                ActionParameterInfo actionParameterInfo = new ActionParameterInfo()
                {
                    name = parameterInfo.Name,
                    @in = "query",
                    description = node.SelectSingleNode($"//param[@name='{parameterInfo.Name}']")?.InnerText,
                    required = (parameterInfo.HasDefaultValue == false && parameterInfo.ParameterType.IsValueType == true) 
                               && parameterInfo.ParameterType.Name.Contains("Nullable`1") == false,
                    type = paramterType.Item1,
                    format = ""//paramterType.Item2
                };

                actionParameterInfos.Add(actionParameterInfo);
            }

            return actionParameterInfos;
        }

        /// <summary>
        /// 获取参数类型
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        private static Tuple<string, string> GetParamterType(System.Reflection.ParameterInfo parameterInfo)
        {
            if (parameterInfo.ParameterType.Name.Contains("Nullable`1") == false)
            {
                var type = Type.GetType($"{parameterInfo.ParameterType.Namespace}.{parameterInfo.ParameterType.Name}");
                return new Tuple<string, string>(type.Name, type.FullName);
            }
            else
            {
                var type = parameterInfo.ParameterType.GenericTypeArguments[0];
                return new Tuple<string, string>(type.Name, type.FullName);
            }
        }

        /// <summary>
        /// 获取返回类型
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        private static Tuple<string, string> GetReturnType(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType.Name.Contains("Task`1"))
            {
                return new Tuple<string, string>(methodInfo.ReturnType.GenericTypeArguments[0].Name, methodInfo.ReturnType.GenericTypeArguments[0].FullName);
            }

            return new Tuple<string, string>(methodInfo.ReturnType.Name, methodInfo.ReturnType.FullName);
        }

        /// <summary>
        /// 从XML文件读取注释
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="memberType">注释类型</param>
        /// <returns></returns>
        private static string GetSummaryFromXML(string name, MemberType memberType)
        {
            string prefix = "T";
            switch (memberType)
            {
                case MemberType.Action:
                {
                    prefix = "M";
                    break;
                }
                case MemberType.Property:
                {
                    prefix = "P";
                    break;
                }
            }
            foreach (var xmlDocument in xmlDocuments)
            {
                if (memberType == MemberType.Class || memberType == MemberType.Property)
                {
                    var node = xmlDocument.SelectSingleNode($"//member[@name='{prefix}:{name}']");
                    if (node != null)
                    {
                        return node.SelectSingleNode("summary")?.InnerText.Trim().Trim(new char[]{'\r', '\n'}).Trim();
                    }
                }
                else if (memberType == MemberType.Action) {
                    var nodes = xmlDocument.SelectNodes("//member");
                    foreach (XmlNode node in nodes)
                    {
                        if (node.Attributes["name"]?.Value.Contains($"{prefix}:{name}(") == true)
                        {
                            return node.SelectSingleNode("summary")?.InnerText.Trim().Trim(new char[] { '\r', '\n' }).Trim();
                        }
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 根据方法名称获取所有注释
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        private static XmlNode GetActionSummaryFromXML(string actionName)
        {
            foreach (var xmlDocument in xmlDocuments) {
                var nodes = xmlDocument.SelectNodes("//member");
                foreach (XmlNode node in nodes) {
                    if (node.Attributes["name"]?.Value.Contains($"M:{actionName}(") == true) {
                        return node;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 读取XML文件
        /// </summary>
        /// <returns></returns>
        private static XmlDocument[] GetXMLFiles()
        {
            List<XmlDocument> xmlDocuments = new List<XmlDocument>();
            string[] xmlFiles = SwaggerExtension.GetXmlCommentsPaths();
            foreach (var xmlFile in xmlFiles)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlFile);

                xmlDocuments.Add(xmlDocument);
            }

            return xmlDocuments.ToArray();
        }
    }

    internal class ControllerInfo {
        public string FullName { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 注释
        /// </summary>
        public string Summary { get; set; }

        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; }

        public List<ActionInfo> ActionInfos { get; set; }
    }

    internal class ActionInfo {
        public string FullName { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Summary { get; set; }

        public string ReturnTypeName { get; set; }

        public string ReturnTypeFullName { get; set; }

        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; }

        public List<ActionParameterInfo> Parameters { get; set; }
    }

    internal class ActionParameterInfo
    {
        public string name { get; set; }

        public string @in { get; set; }

        public string description { get; set; }

        public bool required { get; set; }

        public string type { get; set; }

        public string format { get; set; }
    }
}

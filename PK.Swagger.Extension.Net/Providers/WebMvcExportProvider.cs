using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PK.Swagger.Extension.Net.Enums;
using PK.Swagger.Extension.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;
using PK.Swagger.Extension.Net.Attributes;

namespace PK.Swagger.Extension.Net.Providers {
    /// <summary>
    /// 导出Web.Mvc方法
    /// </summary>
    public class WebMvcExportProvider {
        /// <summary>
        /// web.mvc返回类型定义
        /// </summary>
        private static Type[] resultTypes = new[]
        {
            typeof(ActionResult), typeof(ContentResult), typeof(FileResult), typeof(FileStreamResult),
            typeof(JsonResult), typeof(JavaScriptResult), typeof(Task<ActionResult>), typeof(Task<ContentResult>),
            typeof(Task<FileResult>), typeof(Task<FileStreamResult>),
            typeof(Task<JsonResult>), typeof(Task<JavaScriptResult>)
        };

        /// <summary>
        /// 所有注释XML文件列表
        /// </summary>
        private static XmlDocument[] xmlDocuments = GetXMLFiles();

        /// <summary>
        /// 所有参数类、返回数据类列表
        /// </summary>
        private static List<Type> classList = null;

        /// <summary>
        /// 获取控制器信息
        /// </summary>
        /// <returns></returns>
        internal static List<ControllerInfo> GetControllers()
        {
            classList = new List<Type>();

            List<ControllerInfo> controllerNames = new List<ControllerInfo>();

            foreach (var t in GetSubClasses<Controller>()) {
                if (t.CustomAttributes.Any(s => s.AttributeType == typeof(HiddenApiAttribute)))
                    continue;

                //控制器相关信息
                ControllerInfo controllerInfo = new ControllerInfo() {
                    FullName = t.FullName,
                    Name = t.Name,
                    CustomAttributes = t.CustomAttributes,
                    Summary = GetSummaryFromXML(t.FullName, MemberType.Class),
                    RoutePrefixAttr = SwaggerExtension.UsedMapMvcAttributeRoutes ? GetRoutePrefix(t.CustomAttributes) : ""
                };

                controllerNames.Add(controllerInfo);

                controllerInfo.ActionInfos = new List<ActionInfo>();

                //获取控制器下的方法列表
                List<MethodInfo> methodList = GetSubMethods(t);

                foreach (var method in methodList)
                {
                    if (method.CustomAttributes.Any(s => s.AttributeType == typeof(HiddenApiAttribute)))
                        continue;

                    var webMvcRequestDataType =
                        method.CustomAttributes.FirstOrDefault(s => s.AttributeType == typeof(WebMvcRequestDataTypeAttribute));

                    string actionFullName = $"{method.DeclaringType.FullName}.{method.Name}";
                    var returnTypeResult = GetReturnType(method);
                    var action = new ActionInfo()
                    {
                        FullName = actionFullName,
                        Name = method.Name,
                        Path = $"/{controllerInfo.Name.Replace("Controller", "")}/{method.Name}",
                        CustomAttributes = method.CustomAttributes,
                        Summary =
                            GetSummaryFromXML(actionFullName, MemberType.Action),
                        Parameters = webMvcRequestDataType == null ?
                            GetParameters(GetParameterInfos(method), GetActionSummaryFromXML(actionFullName))
                            : GetParameters(webMvcRequestDataType),
                        ReturnTypeName = returnTypeResult.Item1,
                        ReturnTypeFullName = returnTypeResult.Item2,
                        ReturnDescription = returnTypeResult.Item3,
                        RouteAttr = SwaggerExtension.UsedMapMvcAttributeRoutes
                            ? GetActionRoute(method.CustomAttributes)
                            : ""
                    };
                    action.RoutePath = (!string.IsNullOrWhiteSpace(action.RouteAttr) ? $"/{controllerInfo.RoutePrefixAttr}/{action.RouteAttr}" : "").Replace("//", "/");
                    controllerInfo.ActionInfos.Add(action);
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

                    if (SwaggerExtension.UsedMapMvcAttributeRoutes && string.IsNullOrWhiteSpace(actionInfo.RoutePath))
                        continue;

                    sb.Append(CreateActionJson(actionInfo, controllerInfo.Name));
                }
            }

            //类定义
            //遍历类
            StringBuilder classStringBuilder = new StringBuilder();

            //子类列表
            List<Type> childClassTypeList = new List<Type>();

            foreach (var classType in classList)
            {
                string json = CreateClassJson(classType, ref childClassTypeList);

                classStringBuilder.Append(json);
            }

            //递归生成子类json
            while (childClassTypeList.Any())
            {
                classList.Concat(childClassTypeList);

                //子类列表
                List<Type> childClassTypeList1 = new List<Type>();

                foreach (var classType in childClassTypeList) {
                    string json = CreateClassJson(classType, ref childClassTypeList1);

                    classStringBuilder.Append(json);
                }

                childClassTypeList = childClassTypeList1;
            }

            var definitions = "\"definitions\":{" + classStringBuilder.ToString().TrimEnd(',') + "}";

            var pathsStr = "{\"paths\":{" + sb.ToString().TrimEnd(',') + "}," + definitions + "}";

            return pathsStr;
        }

        /// <summary>
        /// 生成控制器方法json
        /// </summary>
        /// <param name="actionInfo">方法</param>
        /// <param name="controllerName">控制器名称</param>
        /// <returns></returns>
        private static string CreateActionJson(ActionInfo actionInfo, string controllerName)
        {
            //请求方式
            string requestMethod = GetRequestMethod(actionInfo.CustomAttributes);

            var pathStr = $"\"{(SwaggerExtension.UsedMapMvcAttributeRoutes ? actionInfo.RoutePath : actionInfo.Path)}\"";
            var tagsStr = $"\"tags\":[\"{controllerName}\"],";

            var summaryStr = $"\"summary\": \"{actionInfo.Summary}\",";

            var contentTypes = GetActionContentType(actionInfo.CustomAttributes);
            if (contentTypes == null)
            {
                contentTypes = requestMethod != "get"
                    ? new string[] { "application/x-www-form-urlencode" }
                    : new string[] { };
            }

            var consumesStr = JsonConvert.SerializeObject(new { consumes = contentTypes }).Trim('{', '}') + ",";

            var parametersStr = JsonConvert.SerializeObject(new { @parameters = actionInfo.Parameters }).Trim(new char[] { '{', '}' }) + ",";

            var responseStr = "\"responses\":{ \"200\": { \"description\": \"" + (!string.IsNullOrWhiteSpace(actionInfo.ReturnDescription) ? actionInfo.ReturnDescription : "OK") + "\", \"schema\": { \"$ref\": \"#/definitions/" + actionInfo.ReturnTypeName + "\"}}}";

            var requestMethodStr = $"{requestMethod}\":" + "{" + tagsStr + summaryStr + consumesStr + parametersStr + responseStr + "}";

            return $"{pathStr}:" + "{\"" + requestMethodStr + " },";
        }

        /// <summary>
        /// 生成类的json
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="childClassTypeList"></param>
        /// <returns></returns>
        private static string CreateClassJson(Type classType, ref List<Type> childClassTypeList)
        {
            var descriptionStr = "\"description\": \"" + GetSummaryFromXML(classType.FullName, MemberType.Class) + "\",";
            var typeStr = "\"type\": \"object\",";


            //获取属性注释列表
            var propertiesXml = GetClassPropertiesFromXML(classType.FullName);
            Type baseType = classType.BaseType;
            while (baseType != null && baseType.IsClass && baseType.Name != "Object")
            {
                var parentPropertiesXml = GetClassPropertiesFromXML(baseType.FullName);
                foreach (string key in parentPropertiesXml.AllKeys)
                {
                    propertiesXml.Add(key, parentPropertiesXml[key]);
                }

                baseType = baseType.BaseType;
            }

            //获取属性列表
            var properties = GetClassProperties(classType.FullName);

            StringBuilder propertyStringBuilder = new StringBuilder();
            foreach (var property in properties) {
                var propertyDescription = propertiesXml.Get(property.Name);

                var propertyType = GetParamterType(property.PropertyType, ref childClassTypeList);

                var propertyTypeStr = string.IsNullOrWhiteSpace(propertyType.Item3)
                    ? propertyType.Item1
                    : $"{propertyType.Item3}<{propertyType.Item1}>";

                propertyStringBuilder.Append($"\"{property.Name}\": " + "{ \"format\": \"\", \"description\":\"" + propertyDescription + "\",\"type\": \"" + propertyTypeStr + "\"},");
            }

            var propertiesStr = "\"properties\": {" + propertyStringBuilder.ToString().TrimEnd(',') + "}";

            return $"\"{classType.Name}\": " + "{" + descriptionStr + typeStr + propertiesStr + "},";
        }

        /// <summary>
        /// 获取某一个程序集下所有的类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<Type> GetSubClasses<T>() {
            List<Type> types = new List<Type>();
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    types.AddRange(assembly.GetTypes().Where(
                        type => type.IsSubclassOf(typeof(T))).ToList());
                }
                catch
                {
                }
            }

            return types;
        }

        private static Type GetSubClasses(string typeFulleName) {
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                try
                {
                    var type = assembly.GetTypes().FirstOrDefault(s => s.FullName == typeFulleName);
                    if (type != null)
                        return type;

                } catch {
                }
            }

            return null;
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
        /// 获取RoutePrefix属性
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <returns></returns>
        private static string GetRoutePrefix(IEnumerable<CustomAttributeData> customAttributes)
        {
            var attr = customAttributes.FirstOrDefault(s =>
                s.AttributeType == typeof(System.Web.Mvc.RoutePrefixAttribute));

            if (attr != null)
            {
                return attr.ConstructorArguments[0].Value?.ToString();
            }

            return "";
        }

        /// <summary>
        /// 获取Action路由属性
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <returns></returns>
        private static string GetActionRoute(IEnumerable<CustomAttributeData> customAttributes) {
            var attr = customAttributes.FirstOrDefault(s =>
                s.AttributeType == typeof(System.Web.Mvc.RouteAttribute));

            if (attr != null) {
                return attr.ConstructorArguments[0].Value?.ToString();
            }

            return "";
        }

        /// <summary>
        /// 获取Action的Content-Type
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <returns></returns>
        private static string[] GetActionContentType(IEnumerable<CustomAttributeData> customAttributes) {
            var attr = customAttributes.FirstOrDefault(s =>
                s.AttributeType == typeof(WebMvcContentTypeAttribute));

            if (attr != null)
            {
                var arr = (System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value;

                return arr?.Select(s => s.Value?.ToString()).ToArray();
            }

            return new string[]{};
        }


        /// <summary>
        /// 获取参数及注释
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
                    description = node?.SelectSingleNode($"param[@name='{parameterInfo.Name}']")?.InnerText,
                    required = (parameterInfo.HasDefaultValue == false && parameterInfo.ParameterType.IsValueType == true) 
                               && parameterInfo.ParameterType.Name.Contains("`1") == false,
                    type = string.IsNullOrWhiteSpace(paramterType.Item3) ? paramterType?.Item1 : $"{paramterType.Item3}<{paramterType.Item1}>",
                    format = ""//paramterType.Item2
                };

                actionParameterInfos.Add(actionParameterInfo);
            }

            return actionParameterInfos;
        }

        private static List<ActionParameterInfo> GetParameters(CustomAttributeData attr)
        {
            List<ActionParameterInfo> actionParameterInfos = new List<ActionParameterInfo>();
            if (attr != null)
            {
                if (attr.ConstructorArguments.Count == 2)
                {
                    string data = attr.ConstructorArguments[0].Value?.ToString();
                    string description = attr.ConstructorArguments[1].Value?.ToString();

                    ActionParameterInfo actionParameterInfo = new ActionParameterInfo() {
                        name = data,
                        @in = "query",
                        description = description
                    };

                    actionParameterInfos.Add(actionParameterInfo);
                }
                else
                {
                    var value = attr.ConstructorArguments[0].Value as Type;

                    List<Type> childClassTypeList = new List<Type>();
                    var paramterType = GetParamterType(value, ref childClassTypeList);
                    classList.AddRange(childClassTypeList);

                    ActionParameterInfo actionParameterInfo = new ActionParameterInfo() {
                        name = value.Name,
                        @in = "query",
                        description = GetSummaryFromXML(value.FullName, MemberType.Class),
                        required = false,
                        type = string.IsNullOrWhiteSpace(paramterType.Item3) ? paramterType?.Item1 : $"{paramterType.Item3}<{paramterType.Item1}>",
                        format = ""//paramterType.Item2
                    };

                    actionParameterInfos.Add(actionParameterInfo);
                }
            }

            return actionParameterInfos;
        }

        /// <summary>
        /// 根据类名称获取类属性
        /// </summary>
        /// <param name="classFullName"></param>
        /// <returns></returns>
        private static PropertyInfo[] GetClassProperties(string classFullName)
        {
            var type = Type.GetType(classFullName);
            if (type == null)
                type = GetSubClasses(classFullName);

            if (type == null)
            {
                return new PropertyInfo[]{};
            }

            return type.GetProperties();
        }

        /// <summary>
        /// 获取参数类型
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns>Item1：实际类型名称, Item2：实际类型完整名称，Item3：属性类型名称</returns>
        private static Tuple<string, string, string> GetParamterType(System.Reflection.ParameterInfo parameterInfo)
        {
            if (parameterInfo.ParameterType.Name.Contains("`1") == false)
            {
                string typeFullName = $"{parameterInfo.ParameterType.Namespace}.{parameterInfo.ParameterType.Name}";
                var type = Type.GetType(typeFullName);
                if (type == null)
                    type = GetSubClasses(typeFullName);

                if (type == null)
                {
                    return new Tuple<string, string, string>(parameterInfo.ParameterType.Name,
                        $"{parameterInfo.ParameterType.Namespace}.{parameterInfo.ParameterType.Name}", "");
                }

                if (type.IsClass && type.Name != "String" && type.Name != "Object") {
                    if (classList.Any(s => s.FullName == type.FullName) == false)
                        classList.Add(type);
                }

                return new Tuple<string, string, string>(type.Name, type.FullName, "");
            }
            else
            {
                var type = parameterInfo.ParameterType.GenericTypeArguments[0];
                if (type.IsClass && type.Name != "String" && type.Name != "Object")
                {
                    if (classList.Any(s => s.FullName == type.FullName) == false)
                        classList.Add(type);
                }

                if (parameterInfo.ParameterType.Name.Contains("Nullable"))
                {
                    return new Tuple<string, string, string>(type.Name, type.FullName, "");
                }

                return new Tuple<string, string, string>(type.Name, type.FullName, parameterInfo.ParameterType.Name.Replace("`1", ""));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="childClassTypeList"></param>
        /// <returns>Item1：实际类型名称, Item2：实际类型完整名称，Item3：属性类型名称</returns>
        private static Tuple<string, string, string> GetParamterType(Type propertyType, ref List<Type> childClassTypeList) {
            if (propertyType.Name.Contains("`1") == false) {
                string typeFullName = $"{propertyType.Namespace}.{propertyType.Name}";
                var type = Type.GetType(typeFullName);
                if (type == null)
                    type = GetSubClasses(typeFullName);

                if (type == null) {
                    return new Tuple<string, string, string>(propertyType.Name,
                        $"{propertyType.Namespace}.{propertyType.Name}", "");
                }

                if (childClassTypeList != null && type.IsClass && type.Name != "String" && type.Name != "Object") {
                    if (classList.Any(s => s.FullName == type.FullName) == false && childClassTypeList.Any(s => s.FullName == type.FullName) == false)
                        childClassTypeList.Add(type);
                }

                return new Tuple<string, string, string>(type.Name, type.FullName, "");
            } else {
                var type = propertyType.GenericTypeArguments[0];
                if (childClassTypeList != null && type.IsClass && type.Name != "String" && type.Name != "Object") {
                    if (classList.Any(s => s.FullName == type.FullName) == false && childClassTypeList.Any(s => s.FullName == type.FullName) == false)
                        childClassTypeList.Add(type);
                }

                if (propertyType.Name.Contains("Nullable")) {
                    return new Tuple<string, string, string>(type.Name, type.FullName, "");
                }

                return new Tuple<string, string, string>(type.Name, type.FullName, propertyType.Name.Replace("`1", ""));
            }
        }

        /// <summary>
        /// 获取返回类型
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns>Item1：类型简称；Item2，类型完全名称；Item3：描述</returns>
        private static Tuple<string, string, string> GetReturnType(MethodInfo methodInfo)
        {
            var responseDataTypeAttr =
                methodInfo.CustomAttributes.FirstOrDefault(s => s.AttributeType == typeof(WebMvcResponseDataTypeAttribute));
            if (responseDataTypeAttr != null)
            {
                if (responseDataTypeAttr.ConstructorArguments.Count == 1)
                {
                    var type = responseDataTypeAttr.ConstructorArguments[0].Value as Type;

                    if (type.IsClass && type.Name != "String" && type.Name != "Object") {
                        if (classList.Any(s => s.FullName == type.FullName) == false)
                            classList.Add(type);
                    }

                    return new Tuple<string, string, string>(type.Name, type.FullName, null);
                }
                else
                {
                    List<Type> typeList = new List<Type>();
                    var typeArguments = responseDataTypeAttr.ConstructorArguments[0].Value as System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.CustomAttributeTypedArgument>;
                    var description = responseDataTypeAttr.ConstructorArguments[1].Value?.ToString();
                    foreach (var typeArgument in typeArguments)
                    {
                        var type = typeArgument.Value as Type;
                        typeList.Add(type);

                        if (type.IsClass && type.Name != "String" && type.Name != "Object") {
                            if (classList.Any(s => s.FullName == type.FullName) == false)
                                classList.Add(type);
                        }
                    }

                    return new Tuple<string, string, string>(string.Join("、", typeList.Select(s => s.Name)), string.Join("、", typeList.Select(s => s.FullName)), description);
                }
            }

            if (methodInfo.ReturnType.Name.Contains("`1"))
            {
                return new Tuple<string, string, string>(methodInfo.ReturnType.GenericTypeArguments[0].Name, methodInfo.ReturnType.GenericTypeArguments[0].FullName, null);
            }

            return new Tuple<string, string, string>(methodInfo.ReturnType.Name, methodInfo.ReturnType.FullName, null);
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
        /// 获取类下的属性
        /// </summary>
        /// <param name="classFullName"></param>
        /// <returns></returns>
        private static NameValueCollection GetClassPropertiesFromXML(string classFullName)
        {
            NameValueCollection nameValueCollection = new NameValueCollection();

            foreach (var xmlDocument in xmlDocuments) 
            {
                var classNode = xmlDocument.SelectSingleNode($"//member[@name='T:{classFullName}']");
                if (classNode != null)
                {
                    var nextNode = classNode.NextSibling;
                    var nextNodeValue = nextNode.Attributes["name"]?.Value;
                    while (nextNodeValue?.StartsWith("P:") == true)
                    {
                        var propertiesName = nextNodeValue.Replace("P:", "").Replace(classFullName, "")
                            .TrimStart('.');
                        var summary = nextNode.SelectSingleNode("summary")?.InnerText.Trim().Trim(new char[] { '\r', '\n' }).Trim();

                        nameValueCollection.Add(propertiesName, summary);

                        nextNode = nextNode.NextSibling;

                        nextNodeValue = nextNode?.Attributes["name"]?.Value;
                    }
                }
                else 
                {
                    var propertyNodes = xmlDocument.SelectNodes($"//member[contains(@name, 'P:{classFullName}.')]");
                    foreach (XmlNode node in propertyNodes)
                    {
                        var nodeValue = node.Attributes["name"]?.Value;
                        var propertiesName = nodeValue.Replace("P:", "").Replace(classFullName, "")
                            .TrimStart('.');
                        var summary = node.SelectSingleNode("summary")?.InnerText.Trim().Trim(new char[] { '\r', '\n' }).Trim();

                        nameValueCollection.Add(propertiesName, summary);
                    }
                }
            }

            return nameValueCollection;
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

    /// <summary>
    /// 控制器信息
    /// </summary>
    internal class ControllerInfo {
        public string FullName { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 注释
        /// </summary>
        public string Summary { get; set; }

        public string RoutePrefixAttr { get; set; }

        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; }

        public List<ActionInfo> ActionInfos { get; set; }
    }

    /// <summary>
    /// 控制器下的方法信息
    /// </summary>
    internal class ActionInfo {
        public string FullName { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        /// <summary>
        /// 路由路径
        /// </summary>
        public string RoutePath { get; set; }

        public string Summary { get; set; }

        public string ReturnTypeName { get; set; }

        public string ReturnTypeFullName { get; set; }

        public string ReturnDescription { get; set; }

        public string RouteAttr { get; set; }

        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; }

        public List<ActionParameterInfo> Parameters { get; set; }
    }

    /// <summary>
    /// 方法参数信息
    /// </summary>
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

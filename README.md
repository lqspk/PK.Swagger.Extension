# PK.Swagger.Extension.Net
Swagger接口导出到MarkDown文档

**使用方法：**

```C#
GlobalConfiguration.Configuration
    .EnableSwagger(c =>
                   {
                       c.AutoIncludeXmlComments(); //自动包括bin目录下的所有XML文件
                   })
    .EnableSwaggerExport() //启用导出功能
    .EnableSwaggerUi(c =>
                    {
                        c.InjectExportScript(); //添加swagger页面导出按钮
                    });
```


**注意：**

EnableSwaggerExport()方法有个usedMapMvcAttributeRoutes参数，如果项目已启用routes.MapMvcAttributeRoutes()，则必须把参数设置为true。



2.1版以上增加了**四个属性过滤器：**

HiddenApiAttribute：隐藏接口；

WebMvcContentTypeAttribute：只适用于System.Web.Mvc的方法设置Content-Type；

WebMvcRequestDataTypeAttribute：只适用于System.Web.Mvc的方法设置导出的请求参数；

WebMvcResponseDataTypeAttribute：只适用于System.Web.Mvc的方法设置导出的返回数据。



**导出方法：**
项目运行后，访问swagger页面，有两个导出按钮，点击即可。




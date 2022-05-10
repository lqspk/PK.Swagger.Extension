# PK.Swagger.Extension
Swagger接口导出到MarkDown文档

使用方法：

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


导出方法：
项目运行后，访问swagger页面，有一个“导出到markdown”按钮，点击即可。
